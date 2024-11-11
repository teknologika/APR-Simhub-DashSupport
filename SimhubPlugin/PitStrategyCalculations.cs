
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

            }
        }

        public void InitPitCalculations() {
            if (Settings.EnableStrategyCalculation) {

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

                AddProp("Strategy.A.StopLaps", "" );
                AddProp("Strategy.A.StopLapsPct", "" );
                AddProp("Strategy.A.FuelPerStop", "" );
                AddProp("Strategy.A.FuelModes", "" );
                AddProp("Strategy.A.StopDuration", "" );

                AddProp("Strategy.B.StopLaps", "" );
                AddProp("Strategy.B.StopLapsPct", "" );
                AddProp("Strategy.B.FuelPerStop", "" );
                AddProp("Strategy.B.FuelModes", "" );
                AddProp("Strategy.B.StopDuration", "" );

                AddProp("Strategy.C.StopLaps", "" );
                AddProp("Strategy.C.StopLapsPct", "" );
                AddProp("Strategy.C.FuelPerStop", "" );
                AddProp("Strategy.C.FuelModes", "" );
                AddProp("Strategy.C.StopDuration", "" );

                AddProp("Strategy.D.StopLaps", "" );
                AddProp("Strategy.D.StopLapsPct", "" );
                AddProp("Strategy.D.FuelPerStop", "" );
                AddProp("Strategy.D.FuelModes", "" );
                AddProp("Strategy.D.StopDuration", "" );
            }
        }

        public void UpdatePitCalculations(ref GameData data) {

            // Get the iRacing Session state
            irData.Telemetry.TryGetValue("SessionState", out object rawSessionState);
            int sessionState = Convert.ToInt32(rawSessionState);

            // Add Standings Properties
            if (Settings.EnableStrategyCalculation) {

                StrategyBundle.Update();

                SetProp("Strategy.A.StopLaps", StrategyBundle.Instance.StratA_Stops);
                SetProp("Strategy.A.StopLapsPct", StrategyBundle.Instance.StratA_StopsPct);
                SetProp("Strategy.A.FuelPerStop", StrategyBundle.Instance.StratA_FuelToAdd);
                SetProp("Strategy.A.FuelModes", StrategyBundle.Instance.StratA_FuelMode);
                SetProp("Strategy.A.StopDuration", StrategyBundle.Instance.StratA_StopDuration);

                SetProp("Strategy.B.StopLaps", StrategyBundle.Instance.StratB_Stops);
                SetProp("Strategy.B.StopLapsPct", StrategyBundle.Instance.StratB_StopsPct);
                SetProp("Strategy.B.FuelPerStop", StrategyBundle.Instance.StratB_FuelToAdd);
                SetProp("Strategy.B.FuelModes", StrategyBundle.Instance.StratB_FuelMode);
         
                SetProp("Strategy.C.StopLaps", StrategyBundle.Instance.StratC_Stops);
                SetProp("Strategy.C.StopLapsPct", StrategyBundle.Instance.StratC_StopsPct);
                SetProp("Strategy.C.FuelPerStop", StrategyBundle.Instance.StratC_FuelToAdd);
                SetProp("Strategy.C.FuelModes", StrategyBundle.Instance.StratC_FuelMode);
                SetProp("Strategy.C.StopDuration", StrategyBundle.Instance.StratC_StopDuration);

                SetProp("Strategy.D.StopLaps", StrategyBundle.Instance.StratD_Stops);
                SetProp("Strategy.D.StopLapsPct", StrategyBundle.Instance.StratD_StopsPct);
                SetProp("Strategy.D.FuelPerStop", StrategyBundle.Instance.StratD_FuelToAdd);
                SetProp("Strategy.D.FuelModes", StrategyBundle.Instance.StratD_FuelMode);
                SetProp("Strategy.D.StopDuration", StrategyBundle.Instance.StratC_StopDuration);
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


