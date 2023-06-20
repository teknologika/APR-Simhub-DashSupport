
using FMOD;
using GameReaderCommon;
using IRacingReader;
using iRacingSDK;
using MahApps.Metro.Controls;
using SimHub.Plugins;
using SimHub.Plugins.DataPlugins.DataCore;
using SimHub.Plugins.OutputPlugins.GraphicalDash.Behaviors.DoubleText.Imp;
using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Xml.Linq;

namespace APR.DashSupport {
    public partial class APRDashPlugin : IPlugin, IDataPlugin, IWPFSettingsV2 {

        public double Strategy_NumberOfRaceLaps { get; set; } = 0;
        public double Strategy_AverageFuelPerLap { get; set; } = 0;
        public double Strategy_AvailableTankSize { get; set; } = 0;
        public double Strategy_SetupFuel { get; set; } = 0;

        public void ClearPitCalculations() {
            if (Settings.EnableStrategyCalculation) {

                SetProp("Strategy.Dahl.FuelDelta", 0);
                SetProp("Strategy.Dahl.FuelDeltaAverage", 0);
            }
        }

        public void InitPitCalculations() {
            if (Settings.EnableStrategyCalculation) {
               
                AddProp("Strategy.Dahl.FuelDelta",0);
                AddProp("Strategy.Dahl.FuelDeltaAverage",0);
                AddProp("Strategy.Dahl.FuelDeltaOG", 0);
                
            }
        }

        public void UpdatePitCalculations(ref GameData data) {

            // Get the iRacing Session state
            irData.Telemetry.TryGetValue("SessionState", out object rawSessionState);
            int sessionState = Convert.ToInt32(rawSessionState);

            // Add Standings Properties
            if (Settings.EnableStrategyCalculation) {

                // Setup all the values for our calculations
                
                // Number of race laps
                Strategy_NumberOfRaceLaps = data.NewData.TotalLaps;
                if (Settings.Strategy_OverrideNumberOfRaceLaps > 0) {
                    Strategy_NumberOfRaceLaps = Settings.Strategy_OverrideNumberOfRaceLaps;
                }

                // Fuel used per lap
                Strategy_AverageFuelPerLap =  GetProp("DataCorePlugin.Computed.Fuel_LitersPerLap");
                if (Settings.Strategy_OverrideFuelPerLap > 0) {
                    Strategy_AverageFuelPerLap = Settings.Strategy_OverrideFuelPerLap;
                }

                // Available tank size
                var AvailableFuelTankPercent = GetProp("DataCorePlugin.GameRawData.SessionData.DriverInfo.DriverCarMaxFuelPct");
                Strategy_AvailableTankSize = GetProp("DataCorePlugin.GameData.MaxFuel") * AvailableFuelTankPercent;
                if (Settings.Strategy_OverrideAvailableTankSize > 0 ) {
                    Strategy_AvailableTankSize = Settings.Strategy_OverrideAvailableTankSize;
                }

                // Get the amount of fuel in the setup aka starting fuel
                if (GetProp("DataCorePlugin.GameRawData.SessionData.CarSetup.BrakesDriveUnit.Fuel.FuelLevel") != null) {
                    Strategy_SetupFuel = GetProp("DataCorePlugin.GameRawData.SessionData.CarSetup.BrakesDriveUnit.Fuel.FuelLevel");
                }
                
                // Get the average fuel per lap
                Strategy_AverageFuelPerLap = GetProp("DataCorePlugin.Computed.Fuel_LitersPerLap");
                if (Settings.Strategy_OverrideFuelPerLap > 0) {
                    Strategy_AverageFuelPerLap = Settings.Strategy_OverrideFuelPerLap;
                }

                // update session properties
                CalculateAverageDelta(ref data);
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



        public void GetSetupFuel() {

  

        }

    }
}


