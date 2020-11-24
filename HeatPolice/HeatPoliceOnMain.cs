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
        Interval = 50; //milliseconds
    }

    // This is where loops/things are run every frame.
    private void OnTick(object sender, EventArgs e)
    {
       foreach (HeatCopCar cop in HeatCopCars)
        {
            if (!Game.Player.IsPlaying)
            {
                cop.Remove();
            }



            if (cop.status == "Normal" && cop.vehicle.HasCollided)
            {
                cop.violatorvehicle = World.GetClosestVehicle(cop.vehicle.Position, 100);
                if (cop.vehicle.HasBeenDamagedBy(cop.violatorvehicle)) {
                    cop.violator = cop.violatorvehicle.Driver;
                    cop.StartChase();
                }
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
    public Ped driver;
    public Vehicle violatorvehicle;
    public Ped violator;
    public Vehicle vehicle;
    public string status = "";

    public HeatCopCar(string copcar) {
        this.vehicle = World.CreateVehicle(Game.GenerateHash(copcar), Game.Player.Character.Position + Game.Player.Character.ForwardVector * 20);
        this.driver = vehicle.CreateRandomPedOnSeat(VehicleSeat.Driver);
        this.driver.MaxDrivingSpeed = 99999;
        this.setStatusNormal();
        this.vehicle.IsRecordingCollisions = true;
    }

    public void OnTick()
    {
        //if the status is Normal, look for violators, if they find them, start following them. If they keep racing, start the chase
        //first thing, we get the nearby vehicles, so that we can detect them in case of collision
        //if (this.status == "Normal" || this.driver.IsNearEntity(playerPed, playerPed.Position))
        //{
        //    StartChase(playerPed);
        //}
    



        //when the player dies or is arrested, remove all the cops
        if (!Game.Player.IsPlaying)
        {
            this.Remove();
        }
        


        if (status =="Normal" && this.vehicle.HasCollided)
        {
            this.violatorvehicle = World.GetClosestVehicle(this.vehicle.Position, 100);
            if (this.vehicle.HasBeenDamagedBy(violatorvehicle))
                this.violator = violatorvehicle.Driver;
            StartChase();
        }


    }
    public void setStatusNormal()
    {
        this.driver.Task.CruiseWithVehicle(this.vehicle, 70);
        this.vehicle.IsSirenActive = false;
        //this.driver.VehicleDrivingFlags = VehicleDrivingFlags.StopAtTrafficLights;
        //this.driver.VehicleDrivingFlags = VehicleDrivingFlags.UseBlinkers;
        //this.driver.VehicleDrivingFlags = VehicleDrivingFlags.YieldToPeds;
        //this.driver.VehicleDrivingFlags = VehicleDrivingFlags.FollowTraffic;
        this.status = "Normal";
        
    }

    //public void getReady(Ped violator)
    //{
    //    if (!this.driver.IsInVehicle(this.vehicle))
    //        this.driver.Task.EnterVehicle(this.vehicle, VehicleSeat.Driver);
    //    this.vehicle.IsSirenActive = false;
    //    this.status = "Ready";
    //    //follow violator
    //}

    public bool StartChase()
    {
        this.driver.Task.ChaseWithGroundVehicle(this.violator);
        this.driver.DrivingStyle = DrivingStyle.Rushed;
        this.driver.VehicleDrivingFlags = VehicleDrivingFlags.AvoidObjects;
        this.driver.VehicleDrivingFlags = VehicleDrivingFlags.IgnorePathFinding;
        this.driver.VehicleDrivingFlags = VehicleDrivingFlags.AvoidVehicles;
        this.driver.VehicleDrivingFlags = VehicleDrivingFlags.AvoidPeds;
        this.driver.VehicleDrivingFlags = VehicleDrivingFlags.AllowGoingWrongWay;
        this.driver.VehicleDrivingFlags = VehicleDrivingFlags.AllowMedianCrossing;
        this.vehicle.IsSirenActive = true;
        this.status = "Chase";

        //success
        return true;
    }

    public void StopChase()
    {
        this.setStatusNormal();
        this.vehicle.IsSirenActive = false;
    }

    public bool Arrest(Ped violator)
    {
        this.driver.Task.Arrest(violator);
        return true;
    }

    public bool Remove()
    {
        this.vehicle.Delete();
        this.driver.Delete();
        return true;
    }
}