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

            public static void Reset() { instance = new StrategyBundle(); }

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

            // Saftey car
            public bool IsUnderSC;
            public bool IsSafetyCarMovingInPitane;
            public bool SafetyCarCountLock;

            public int SafetyCarPeriodCount;
            public bool FirstSCPeriodBreaksEarlySCRule;

            public int SafetyCarIdx;
            public double SafetyCarTrackDistancePercent;


            // these need to be injected on creation for calcs to work
            public float TrackLength;

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

            // Get the starting fuel
            StrategyObserver.StartingFuel = GetProp("DataCorePlugin.GameRawData.SessionData.CarSetup.Chassis.Rear.FuelLevel") ?? 0.0;

            // Get the average fuel per lap
            StrategyObserver.FuelLitersPerLap = GetProp("DataCorePlugin.Computed.Fuel_LitersPerLap") ?? 0.0;

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



        }

       
    }
}
