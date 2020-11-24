using GTA;
using GTA.Native;
using GTA.Math;
using GTA.UI;
using GTA.NaturalMotion;
using System;
using System.Windows.Forms;
using System.Collections.Generic;
using System.IO;
using System.Drawing;
using System.Media;

public class HeatPolice : Script
{
    // Where you initialize all your variables for use.
    private string modName = "Heat Police";
    private string modCreatorName = "Madatek, with the help of many awesom people";
    private List<HeatCopCar> HeatCopCars = new List<HeatCopCar>();
    private Ped playerPed = Game.Player.Character;
    private Player player = Game.Player;

    // Where you initialize the events or do anything when the mod starts.
    public HeatPolice()
    {
        Tick += OnTick;
        KeyDown += OnKeyDown;
        KeyUp += OnKeyUp;
        Interval = 10; //milliseconds
    }

    // This is where loops/things are run every frame.
    //This is the main OnTick, where the cop communicates with the world, for the actual action, refer in the Ontick in the HeatCopCar class
    private void OnTick(object sender, EventArgs e)
    {
        foreach (HeatCopCar cop in HeatCopCars)
        {
            if (cop.status != "Mark for Removal")
            {
                //If this cop is not going to be deleted
                cop.OnTick(HeatCopCars);
            }
            else
            {
                //delete from the list and don't care about this cop anymore
                HeatCopCars.Remove(cop);
                cop.msg = "";
            }

            if (cop.msg != "")
            {
                //If the cop has something to say, display, then clear the notification so it doesn't keep saying it every tick
                GTA.UI.Notification.Show("HeatPolice Message: " + cop.msg + "STATUS: " + cop.status);
                cop.msg = "";
            }
        }
    }

    // When you press a key down or hold it.
    private void OnKeyDown(object sender, KeyEventArgs e)
    {
       
    }

    // When you press a key up or release it.
    private void OnKeyUp(object sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.NumPad7)
        {
            //needs to be moved and changed based on heat.
            HeatCopCar copCar = new HeatCopCar("c8cop");
            HeatCopCars.Add(copCar);
        }
    }
}

public class HeatCopCar
{
    public List<HeatCopCar> colleagues;
    public Ped driver;  //The pilot
    public Vehicle vehicle; //The car
    public Vehicle violatorvehicle; //the violator's car
    public Ped violator; //The violator
    public string status = ""; //Status, useful for cops to interact with each other and with the system. Determines what actions this cop will take
    public Vector3 currentpos; //This cop's position
    public string msg = ""; //Notification message. Starts empty, as soon as something fills it, it gets displayed
    Vehicle[] aroundme = new Vehicle[30]; //Gets the vehicles near me
    public HeatCopCar(string copcar){
        //Constructor for the copcar, starting from the model name. Spawning, setting base properties and beginning of patrol are managed here
        this.vehicle = World.CreateVehicle(Game.GenerateHash(copcar), Game.Player.Character.Position + Game.Player.Character.ForwardVector * 20);
        this.driver = vehicle.CreateRandomPedOnSeat(VehicleSeat.Driver);
        this.driver.MaxDrivingSpeed = Function.Call<float>(GTA.Native.Hash.GET_VEHICLE_ESTIMATED_MAX_SPEED, this.vehicle.GetHashCode()) - 20;
        this.vehicle.IsRecordingCollisions = true;
        this.violator = null;
        this.violatorvehicle = null;
        this.currentpos = this.vehicle.Position;
        this.setStatusNormal();
    }
    public void OnTick(List<HeatCopCar> import)
        //The pulsating heart of the script, here, at every tick, we monitor if the cop is alive, its status, and, based on many parameters, what they have to do.
    {
        colleagues = import;

        //if (!this.CheckAlive())
        //{
        //    //this cop has been marked for removal, leave the on tick immediately
        //    return;
        //}

        //Current cops' position
        this.currentpos = this.vehicle.Position;

        if (status == "Chase") 
            //Whenever I am chasing. Needs cleanup            
        {
            this.CheckBusted();
            this.CheckEscaped();
        }
        if (status == "Normal")
            //When I am not chasing, check for violations
        {
            this.aroundme = World.GetNearbyVehicles(this.driver, 20);
            //Have I been touched?
            this.CollisionCheck();
        }
    }
    private void setStatusNormal()
    {
        //When being created or ending the chase
        this.driver.Task.CruiseWithVehicle(this.vehicle, 70);
        this.vehicle.IsSirenActive = false;
        this.driver.DrivingStyle = DrivingStyle.Normal;
        this.violator = null;
        this.violatorvehicle = null;
        this.status = "Normal";
        msg = "UNIT: Resuming patrol";
    }
    private bool StartChase(Ped violator)
    {
        //Once I have an active violator, I start giving chase
        try
        {
            msg = "UNIT: In pursuit";
            this.driver.Task.ChaseWithGroundVehicle(violator);
        }
        catch
        {
            msg = "ERROR: Suspect detected and identified, Error in starting chase routine";
            return false;
        }
        this.driver.DrivingStyle = DrivingStyle.Rushed;
        this.driver.VehicleDrivingFlags = VehicleDrivingFlags.AvoidObjects;
        this.driver.VehicleDrivingFlags = VehicleDrivingFlags.IgnorePathFinding;
        this.driver.VehicleDrivingFlags = VehicleDrivingFlags.AvoidVehicles;
        this.driver.VehicleDrivingFlags = VehicleDrivingFlags.AvoidPeds;
        this.driver.VehicleDrivingFlags = VehicleDrivingFlags.AllowGoingWrongWay;
        this.driver.VehicleDrivingFlags = VehicleDrivingFlags.AllowMedianCrossing;
        this.vehicle.IsSirenActive = true;
        this.status = "Chase";
        return true;
    }
    private void Remove()
    {
        //If, for whatever reason, this unit needs to be deleted.
        try
        {
            vehicle.MarkAsNoLongerNeeded();
            driver.MarkAsNoLongerNeeded();
            this.status = "Marked for Removal";
            this.msg = "INFO: Unit successfully marked for removal.";
        }
        catch
        {
            this.status = "Marked for Removal";
            this.msg = "WARN: Marked for Removal from game failed, will only remove from list";
        }
    }
    private void CollisionCheck(){
        //Have I been touched?
        try
        {
            if (this.vehicle.HasCollided)
            {
                try {
                    foreach (Vehicle v in this.aroundme)
                    {
                        if (this.vehicle.HasBeenDamagedBy(v) || this.vehicle.HasBeenDamagedBy(v.Driver))
                        {
                            try
                            {
                                if (!v.IsSeatFree(VehicleSeat.Driver))
                                {
                                    this.violator = v.Driver;
                                    this.violatorvehicle = v;
                                    msg = "UNIT: Dispactch, someone has crashed into me, they are not stopping. Code 3.";
                                    if (this.StartChase(this.violator))
                                    {
                                        return;
                                    }
                                    else
                                    {
                                        msg = "WARN: Suspect detected and identified, chase couldn't start";
                                        return;
                                    }
                                }
                            }
                            catch
                            {
                                msg = "ERROR: Collision detected, Suspect identified, couldn't start Chase Routine.";
                                return;
                            }
                        }
                    }
                }
                catch
                {
                    msg = "ERROR: Collision has been detected, error in detecting suspect.";
                    return;
                }
                }
            else
            {
                return;
            }
        }
        catch
        {
            msg = "ERROR: Error while checking for collision";
            return;
        }
    }
    private void CheckBusted()
    {
        if (violator.CurrentVehicle == null || violator.IsDead || !violatorvehicle.IsEngineRunning)
        {
            if (violator == Game.Player.Character)
            {
                try
                {
                    Game.Player.Money = 0;
                    msg = "UNIT: Dispatch, we have the suspect.";
                }
                catch
                {
                    msg = "WARNING: Can't apply fine, maybe you aren't Trevor, Franklin or Michael";
                }
            }
            this.driver.Task.ClearAllImmediately();
            this.setStatusNormal();
            violatorvehicle = violator.CurrentVehicle;
        }
        else
        {
            violatorvehicle = violator.CurrentVehicle;
        }
    }
    private void CheckEscaped()
    {
        if (!violator.IsInRange(currentpos, 500))
        {
            msg = "UNIT: Lost sight of the suspect, dropping pursuit";
            this.setStatusNormal();
        }
    }
    private bool CheckAlive()
        //Gives true if the cop is alive and the car can be driven. If false, then mark this unit for removal and forget about them
    {
        if (driver.IsDead || !vehicle.IsDriveable)
        {
            msg = "UNIT: Can't continue to operate";
            this.Remove();
            return false;
        }
        else
        {
            return true;
        }
    }
}

public static class DispatchHandler
{
    public static void DispatchCops(Ped violator, List<HeatCopCar> list)
    {
        
    }
}