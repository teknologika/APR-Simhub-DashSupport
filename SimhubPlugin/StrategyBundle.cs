using GameReaderCommon;
using IRacingReader;
using iRacingSDK;
using MahApps.Metro.Controls;
using SimHub.Plugins;
using SimHub.Plugins.OutputPlugins.Dash.GLCDTemplating;
using SimHub.Plugins.OutputPlugins.GraphicalDash.Behaviors.DoubleText.Imp;
using SimHub.Plugins.OutputPlugins.GraphicalDash.BitmapDisplay.TurnTDU;
using SimHub.Plugins.OutputPlugins.GraphicalDash.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media.Animation;
using static APR.DashSupport.APRDashPlugin;
using static iRacingSDK.SessionData._DriverInfo;

namespace APR.DashSupport {

    public partial class APRDashPlugin : IPlugin, IDataPlugin, IWPFSettingsV2 {


        // A singleton class that handles all the Strategy info
        private static StrategyBundle instance = null;

        public class StrategyBundle {
            private static readonly object padlock = new object();

            StrategyBundle() {
            }

            public static StrategyBundle Instance {
                get {
                    lock (padlock) {
                        if (instance == null) {
                            instance = new StrategyBundle();
                            instance.FirstSCPeriodBreaksEarlySCRule = false;


                        }
                        return instance;
                    }
                }
            }

            public double TotaLaps;
            public double CurrentLap;
            public double CoughAllowance = 1.0;
            public double FuelFillRateLitresPerSecond;
            public double StartingFuel;

            public double FuelLitersPerLap;
            public double AvailableTankSize;

            double RequiredFuelBasedOnLapsRemaing {
                get {
                    return (TotaLaps - CurrentLap) * FuelLitersPerLap;
                }
            }

            public double EstimatedTotalFuel { get { return (TotaLaps * FuelLitersPerLap) + CoughAllowance; } }
            public bool IsUnderSC;
            public int SafetyCarPeriodCount;
            public bool FirstSCPeriodBreaksEarlySCRule;
        }

        public void UpdateStrategyBundle(GameData data) {
            StrategyBundle StrategyObserver = StrategyBundle.Instance;


            // Get the starting fuel
            StrategyObserver.StartingFuel = GetProp("DataCorePlugin.GameRawData.SessionData.CarSetup.Chassis.Rear.FuelLevel") ?? 0.0;

            // Get the average fuel per lap
            StrategyObserver.FuelLitersPerLap = GetProp("DataCorePlugin.Computed.Fuel_LitersPerLap") ?? 0.0;

            //Supercar Gen2 hardcode FIXME
            StrategyObserver.FuelFillRateLitresPerSecond = 2.4; // Supercar Gen2

            // For vets, are we under SC
            StrategyObserver.IsUnderSC = IsUnderSafetyCar;

            StrategyObserver.SafetyCarPeriodCount = SafetyCarPeriodCount;

            StrategyObserver.TotaLaps = data.NewData.TotalLaps; // Might break for timed races

            StrategyObserver.CurrentLap = data.NewData.CurrentLap;

            // SC Rule
            if (StrategyObserver.CurrentLap < 2) {
                if (StrategyObserver.IsUnderSC ) {
                    StrategyObserver.FirstSCPeriodBreaksEarlySCRule = true;
                }
            }

                // Available tank size
                var AvailableFuelTankPercent = irData.SessionData.DriverInfo.DriverCarMaxFuelPct;
            var UnrestrictedTankSizeInLtr = irData.SessionData.DriverInfo.DriverCarFuelMaxLtr;
            StrategyObserver.AvailableTankSize = AvailableFuelTankPercent * UnrestrictedTankSizeInLtr;



        }

       
    }
}
