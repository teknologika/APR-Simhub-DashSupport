
using FMOD;
using GameReaderCommon;
using IRacingReader;
using iRacingSDK;
using MahApps.Metro.Controls;
using SimHub.Plugins;
using SimHub.Plugins.DataPlugins.DataCore;
using SimHub.Plugins.DataPlugins.RGBDriver.UI.LedBehaviourEditors;
using SimHub.Plugins.OutputPlugins.Dash.GLCDTemplating;
using SimHub.Plugins.OutputPlugins.GraphicalDash.Behaviors.DoubleText.Imp;
using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Xml.Linq;
using static iRacingSDK.SessionData._DriverInfo;

namespace APR.DashSupport {
    public partial class APRDashPlugin : IPlugin, IDataPlugin, IWPFSettingsV2 {

        public double Strategy_NumberOfRaceLaps { get; set; } = 0;
        public double Strategy_NumberOfRaceLapsRemaining { get; set; } = 0;
        public double Strategy_AverageFuelPerLap { get; set; } = 0;
        public double Strategy_AvailableTankSize { get; set; } = 0;
        public double Strategy_SetupFuel { get; set; } = 0;
        public double Strategy_TotalFuelNeeded { get; set; } = 0;
        public double Strategy_FuelToAdd { get; set; } = 0;
        public double Strategy_LastStopFuelToAdd { get; set; } = 0;
        public double Strategy_NumberOfStops { get; set; } = 0;
        public double Strategy_EarliestStopToMakeFinish { get; set; } = 0;
        public double Strategy_LatestStopToMakeFinish { get; set; } = 0;
        public double Strategy_MinimumPerStop { get; set; } = 0;
        
        public double Strategy_CoughAllowance = 0.4;
        public double Strategy_LapsMargin = 1;


        public void ClearPitCalculations() {
            if (Settings.EnableStrategyCalculation) {

                SetProp("Strategy.Dahl.FuelDelta", 0);
                SetProp("Strategy.Dahl.FuelDeltaAverage", 0);
                SetProp("Strategy.Dahl.FuelDeltaOG", 0);

                SetProp("Strategy.Basic.TotalLaps", 0);
                SetProp("Strategy.Basic.AverageFuelPerLap", 0);
                SetProp("Strategy.Basic.StartingFuel", 0);
                SetProp("Strategy.Basic.AvailableTankSize", 0);
                SetProp("Strategy.Basic.TotalFuelRequired", 0);
                SetProp("Strategy.Basic.TotalFuelToAdd", 0);
                SetProp("Strategy.Basic.MinimumFuelPerStop", 0);
                SetProp("Strategy.Basic.NumberOfStops", 0);
                SetProp("Strategy.Basic.EarliestStopToMakeFinish", 0);
                SetProp("Strategy.Basic.LatestStopToMakeFinish", 0);
                SetProp("Strategy.Basic.LastStopFuelToAdd", 0);

                SetProp("Strategy.A.EarlyStopLaps", "");
                SetProp("Strategy.B.EarlyStopLaps", "");
                SetProp("Strategy.C.EarlyStopLaps", "");
                SetProp("Strategy.D.EarlyStopLaps", "");
               
                SetProp("Strategy.A.LateStopLaps", "");
                SetProp("Strategy.B.LateStopLaps", "");
                SetProp("Strategy.C.LateStopLaps", "");
                SetProp("Strategy.D.LateStopLaps", "");
               
                SetProp("Strategy.A.FuelPerStop", "");
                SetProp("Strategy.B.FuelPerStop", "");
                SetProp("Strategy.C.FuelPerStop", "");
                SetProp("Strategy.D.FuelPerStop", "");
            
                SetProp("Strategy.A.FuelModes", ""); // F or A where fixed or auto
                SetProp("Strategy.B.FuelModes", "");
                SetProp("Strategy.C.FuelModes", "");
                SetProp("Strategy.D.FuelModes", "");

            }
        }

        public void InitPitCalculations() {
            if (Settings.EnableStrategyCalculation) {

                AddProp("Strategy.A.EarlyStopLaps", "");
                AddProp("Strategy.B.EarlyStopLaps", "");
                AddProp("Strategy.C.EarlyStopLaps", "");
                AddProp("Strategy.D.EarlyStopLaps", "");

                AddProp("Strategy.A.LateStopLaps", "");
                AddProp("Strategy.B.LateStopLaps", "");
                AddProp("Strategy.C.LateStopLaps", "");
                AddProp("Strategy.D.LateStopLaps", "");

                AddProp("Strategy.A.FuelPerStop", "");
                AddProp("Strategy.B.FuelPerStop", "");
                AddProp("Strategy.C.FuelPerStop", "");
                AddProp("Strategy.D.FuelPerStop", "");

                AddProp("Strategy.A.FuelModes", ""); // F or A where fixed or auto
                AddProp("Strategy.B.FuelModes", "");
                AddProp("Strategy.C.FuelModes", "");
                AddProp("Strategy.D.FuelModes", "");

                AddProp("Strategy.NextStop.FuelToAdd", "");
                AddProp("Strategy.NextStop.FuelMode", ""); // Auto or Fixed Manual
                AddProp("Strategy.AutoFuelMargin", ""); // Auto fuel margin based on risk mode

                AddProp("Strategy.Dahl.FuelDelta",0);
                AddProp("Strategy.Dahl.FuelDeltaAverage",0);
                AddProp("Strategy.Dahl.FuelDeltaOG", 0);

                AddProp("Strategy.Basic.TotalLaps", 0);
                AddProp("Strategy.Basic.TotalLapsRemaining", 0);
                AddProp("Strategy.Basic.AverageFuelPerLap", 0);
                AddProp("Strategy.Basic.StartingFuel", 0);
                AddProp("Strategy.Basic.NumberOfStops", 0);
                AddProp("Strategy.Basic.TotalFuelRequired", 0);
                AddProp("Strategy.Basic.TotalFuelToAdd", 0);
                AddProp("Strategy.Basic.MinimumFuelPerStop", 0);
                AddProp("Strategy.Basic.EarliestStopToMakeFinish", 0);
                AddProp("Strategy.Basic.AvailableTankSize", 0);
                AddProp("Strategy.Basic.LatestStopToMakeFinish", 0);
                AddProp("Strategy.Basic.LastStopFuelToAdd", 0);

                AddProp("Strategy.Vets.IsVetsSession", false);
                AddProp("Strategy.Vets.IsVetsRaceSession", false);
                AddProp("Strategy.Vets.IsUnderSafetyCar", false);

            }
        }

        public void UpdatePitCalculations(ref GameData data) {

            // Get the iRacing Session state
            irData.Telemetry.TryGetValue("SessionState", out object rawSessionState);
            int sessionState = Convert.ToInt32(rawSessionState);

            // Add Standings Properties
            if (Settings.EnableStrategyCalculation) {

                StrategyBundle.Update();

                AddProp("Strategy.A.EarlyStopLaps", StrategyBundle.Instance.StratA_Stops);
                AddProp("Strategy.B.EarlyStopLaps", "");
                AddProp("Strategy.C.EarlyStopLaps", "");
                AddProp("Strategy.D.EarlyStopLaps", "");

                AddProp("Strategy.A.StopLaps", StrategyBundle.Instance.StratA_Stops);
                AddProp("Strategy.B.StopLaps", "");
                AddProp("Strategy.C.StopLaps", "");
                AddProp("Strategy.D.StopLaps", "");

                AddProp("Strategy.A.StopLapsPct", StrategyBundle.Instance.StratA_StopsPct);
                AddProp("Strategy.B.StopLapsPct", "");
                AddProp("Strategy.C.StopLapsPct", "");
                AddProp("Strategy.D.StopLapsPct", "");


                AddProp("Strategy.A.FuelPerStop", StrategyBundle.Instance.StratA_FuelToAdd);
                AddProp("Strategy.B.FuelPerStop", "");
                AddProp("Strategy.C.FuelPerStop", "");
                AddProp("Strategy.D.FuelPerStop", "");

                AddProp("Strategy.A.FuelModes", ""); // F or A where fixed or auto
                AddProp("Strategy.B.FuelModes", "");
                AddProp("Strategy.C.FuelModes", "");
                AddProp("Strategy.D.FuelModes", "");


               // StratA();
            }
        }

        public void CalculateAverageDelta(ref GameData data) {

            int currentLap = data.NewData.CurrentLap;
            int totalLaps = data.NewData.TotalLaps;
            double fuel = data.NewData.Fuel;
            double trackPosition = irData.Telemetry.LapDistPct;

            int lapLapsRemaining = totalLaps - currentLap;
            int remainingLaps = lapLapsRemaining;
            int truncRemainingLaps = ((int)(remainingLaps * 100)) / 100;

            int DahlCalculatedLapsRemaining = GetProp("DahlDesign.LapsRemaining");
            double distanceLeft = DahlCalculatedLapsRemaining + 1 - trackPosition;

            double DahlCalculatedFuelPerLap = GetProp("DahlDesign.CalcFuelPerLap");
            double DahlCalculatedAverageFuelPerLap = GetProp("DahlDesign.CalcAverageFuel");
            int builtInFuelMargin = 0;
            //double builtInFuelMargin = 1 + (0.2 / DahlCalculatedFuelPerLap); //0.2 liters to finish since cars start stuttering around there, also 1 whole lap for the one where you run out of fuel.
            double fuelDelta = fuel - (DahlCalculatedFuelPerLap * distanceLeft) - builtInFuelMargin;

            // double builtInAverageFuelMargin = 1 + (0.2 / DahlCalculatedAverageFuelPerLap); //0.2 liters to finish since cars start stuttering around there, also 1 whole lap for the one where you run out of fuel.
            double fuelDeltaAverage = fuel - (DahlCalculatedAverageFuelPerLap * distanceLeft) - builtInFuelMargin;
            SetProp("Strategy.Dahl.FuelDeltaOG", GetProp("DahlDesign.FuelDelta"));
            SetProp("Strategy.Dahl.FuelDelta", fuelDelta);
            SetProp("Strategy.Dahl.FuelDeltaAverage", fuelDeltaAverage);
           
        }

        public class StrategyPitStop{
            public int lap;
            public double fuelToAdd;
            public StrategyPitStop(int Lap,double FuelToAdd) {
                lap = Lap;
                fuelToAdd = FuelToAdd;
            }

            public override string ToString() {
                return string.Format("Lap:{0} Fuel:{1}L", lap,fuelToAdd);
            }



        }

        // Strat A - Flat out stretch long, fill tank, short second fill
        public void StratA() {
            List <StrategyPitStop> StratAStops = new List <StrategyPitStop>();

           
            // get our starting positions
            double fuel = Strategy_SetupFuel;
            double fuelNeededToEnd = Strategy_TotalFuelNeeded;

            for (int i = 1; i < Strategy_NumberOfRaceLaps; i++) {
                  
                fuel = fuel - Strategy_AverageFuelPerLap;
                fuelNeededToEnd = fuelNeededToEnd - Strategy_AverageFuelPerLap;


                if (fuel - Strategy_AverageFuelPerLap <= Strategy_CoughAllowance) {

                    // Ignore the last lap as we can go below one lap of fuel
                    if (i == (Strategy_NumberOfRaceLaps - 1)) {
                        fuel = fuel - Strategy_AverageFuelPerLap;
                        fuelNeededToEnd = fuelNeededToEnd - Strategy_AverageFuelPerLap;
                    }
                    else {
                        // this is strat A, fill to the brim
                        double calcFuelToAdd = Strategy_AvailableTankSize - (fuel);
                        calcFuelToAdd = Math.Ceiling(calcFuelToAdd / 0.5) * 0.5;

                        // If we can add more than we need, only add what we need
                        if (calcFuelToAdd > fuelNeededToEnd + Strategy_CoughAllowance) {
                            // to be exta safe we need to add the margin twice which gets us home without a cough.
                            calcFuelToAdd = Math.Ceiling(((fuelNeededToEnd + Strategy_CoughAllowance - Strategy_AverageFuelPerLap) / 0.5) * 0.5) + (Math.Ceiling(Strategy_CoughAllowance/0.5) *0.5);
                        }
                        StratAStops.Add(new StrategyPitStop(i, calcFuelToAdd));
                        fuel = fuel + calcFuelToAdd;

                        // If we try and fuel more than the tank size, we will only fill the tank
                        if (fuel > Strategy_AvailableTankSize) {
                            fuel = Strategy_AvailableTankSize;
                        }
                    }
                }

            }

            // Burn the last lap
            fuel = fuel - Strategy_AverageFuelPerLap;
            fuelNeededToEnd = fuelNeededToEnd - Strategy_AverageFuelPerLap;

            // Strat B - Reverse of strat 1 .. short first stop then full fill
            // Now we have strat A, let's run it in reverse and see when we can fill
       
        }

    }
}


