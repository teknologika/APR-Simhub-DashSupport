using GameReaderCommon;
using IRacingReader;
using iRacingSDK;
using MahApps.Metro.Controls;
using SimHub.Plugins;
using SimHub.Plugins.DataPlugins.DataCore;
using SimHub.Plugins.OutputPlugins.Dash.GLCDTemplating;
using SimHub.Plugins.OutputPlugins.GraphicalDash.Behaviors.DoubleText;
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

            public static void Reset() { instance = new StrategyBundle(); }

            public bool PlayerIsDriving;
            public int TotaLaps;
            public int CurrentLap;
            public double CoughAllowance = 1.0;
            public double FuelFillRateLitresPerSecond;
            public double StartingFuel;
            public double SimHubFuelLitersPerLap;
            private double _fuelLitersPerLap;

            private double GetFuelBurnDefault() {

                switch (StrategyBundle.Instance.TrackNameWithConfig) {
                    case "aragon moto-Moto Grand Prix":  return double.MaxValue;

                    case "bathurst": return 4.55;

                    case "roadamerica full-Full Course": return double.MaxValue;

                    case "homestead roadb-Road Course B":  return double.MaxValue;

                    case "longbeach": return 2.7;

                    default: return double.MaxValue;
                       
                }
            }

            public double FuelLitersPerLap {
                get {
                    double defaultBurn = this.GetFuelBurnDefault();
                    if (defaultBurn != double.MaxValue) {
                        if (SimHubFuelLitersPerLap == 0) {
                            return defaultBurn;
                        }
                        if ((defaultBurn * 1.2 > SimHubFuelLitersPerLap) || (defaultBurn * 0.8 < SimHubFuelLitersPerLap)) {
                            return defaultBurn;
                        }
                    }
                    return SimHubFuelLitersPerLap;
                }
            }

            public double AvailableTankSize;

            double RequiredFuelBasedOnLapsRemaing {
                get {
                    return (TotaLaps - CurrentLap) * FuelLitersPerLap;
                }
            }

            public double EstimatedTotalFuel { get { return (TotaLaps * FuelLitersPerLap) + CoughAllowance; } }

            // Saftey car
            public bool IsUnderSC;
            public bool IsSafetyCarMovingInPitane;
            public bool SafetyCarCountLock;

            public int SafetyCarPeriodCount;
            public bool FirstSCPeriodBreaksEarlySCRule;

            public int SafetyCarIdx;
            public double SafetyCarTrackDistancePercent;

            // these need to be injected on creation for calcs to work
            public double TrackLength;
            public string TrackNameWithConfig;

            public string SessionType;

            // Car being driven / spectating from
            public long SpectatedCarIdx;
            public float SpecatedCarLapDistPct;
            public int SpectatedCarCurrentLap;

            public int SlowOpponentIdx;
            public double SlowOpponentLapDistPct;

        }

        public void UpdateStrategyBundle(GameData data) {
            StrategyBundle StrategyObserver = StrategyBundle.Instance;

            StrategyObserver.SessionType = data.NewData.SessionTypeName;

            // Get the amount of fuel in the setup aka starting fuel
            if (GetProp("DataCorePlugin.GameRawData.SessionData.CarSetup.Chassis.Rear.FuelLevel") != null) {
                string setupFuelString = GetProp("DataCorePlugin.GameRawData.SessionData.CarSetup.Chassis.Rear.FuelLevel");
                StrategyObserver.StartingFuel = double.Parse(setupFuelString.Replace(" L", ""));
            }


            // Get the average fuel per lap - don't default to zero >> fuel measuremet calcs need go here.
            StrategyObserver.SimHubFuelLitersPerLap = GetProp("DataCorePlugin.Computed.Fuel_LitersPerLap") ?? 0.0;

            //Supercar Gen2 hardcode FIXME
            StrategyObserver.FuelFillRateLitresPerSecond = 2.4; // Supercar Gen2

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

            // Trackdata
            StrategyObserver.TrackNameWithConfig =  data.NewData.TrackNameWithConfig;
            StrategyObserver.TrackLength = data.NewData.TrackLength;

            // Get the Vets SC  FIXME


        }

       
    }
}
