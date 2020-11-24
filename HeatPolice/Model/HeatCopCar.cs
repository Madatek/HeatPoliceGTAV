using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GTA;
using GTA.Math;
namespace HeatPolice
{
    class HeatCopCar
    {
        private Ped playerPed = Game.Player.Character;
        private Player player = Game.Player;
        public Ped driver;
        public Vehicle violatorvehicle;
        public Ped violator;
        public Vehicle vehicle;
        public string status = "";

        public HeatCopCar(string copcar, Vector3 position)
        {
            //spawn della Cop Car e assegnazione del veicolo
            this.vehicle = World.CreateVehicle(Game.GenerateHash(copcar), position);
            this.driver = vehicle.CreateRandomPedOnSeat(VehicleSeat.Driver);

            //assegno stato normale di pattuglia e attivo registrazione collisioni per rilevamento
            this.driver.MaxDrivingSpeed = 99999;
            this.setStatusNormal();
            this.vehicle.IsRecordingCollisions = true;
        }

        //A ogni tick
        public void OnTick()
        {
            //Se il giocatore è arrestato o muore, rimuovo tutti gli heatcop
            if (!Game.Player.IsPlaying)
            {
                this.Remove();
            }

            //Avvio inseguimento in caso di danni con sospetto
            if (this.status == "Normal" && this.vehicle.IsTouching(Game.Player.LastVehicle))
            {
                //Prendo veicolo più vicino e lo imposto come "Violator"
                this.violatorvehicle = World.GetClosestVehicle(this.vehicle.Position, 0);
                this.violator = this.violatorvehicle.Driver;
                this.driver.Task.ChaseWithGroundVehicle(this.violator);
                this.StartChase();
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
}
