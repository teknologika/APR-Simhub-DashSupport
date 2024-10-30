using GameReaderCommon;
using SimHub.Plugins;
using System;
using System.Windows.Media;
using IRacingReader;
using iRacingSDK;
using System.Diagnostics.Eventing.Reader;
using System.Windows.Markup;
using System.Net.Http.Headers;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using MahApps.Metro.Controls;
using SimRaceX.Telemetry.Comparer.Model;
using SimHub.Plugins.UI.DeviceScannerModels;
using System.Collections.Generic;
using SimHub.Plugins.DataPlugins.PersistantTracker;
using System.Linq;
using System.Xml.Linq;
using Newtonsoft.Json;
using System.Windows.Media.Animation;
using static iRacingSDK.SessionData._RadioInfo;
using static APR.DashSupport.APRDashPlugin;
using SimHub.Plugins.OutputPlugins.GraphicalDash.Behaviors.DoubleText;
using System.Runtime.InteropServices.ComTypes;
using FMOD;
using static SimHub.Plugins.DataPlugins.PersistantTracker.PersistantTrackerPluginAttachedData;
using SimHub.Plugins.OutputPlugins.GraphicalDash.Models;
using System.Net.Security;
using static SimHub.Plugins.UI.SupportedGamePicker;
using System.Windows.Forms;

namespace APR.DashSupport {

    public partial class APRDashPlugin : IPlugin, IDataPlugin, IWPFSettingsV2 {
        
    }

    // Pitstore in a singleton class
    public sealed class PitStore {
        private const int MAXCARS = 64;

        

      //  public double FuelPerLap = 3.0; // Just a random default
      //  public double FuelFillPerSecond = 2.4; // Supercar Gen2 as default
      //  public int TotalLapsInRace = 0;

        private static PitStore instance = null;
        private static readonly object padlock = new object();
        private List<PitStop> stopList = new List<PitStop>();

        PitStore() {
        }

        public static PitStore Instance {
            get {
                lock (padlock) {
                    if (instance == null) {
                        instance = new PitStore();
                        instance.stopList = new List<PitStop>();
                    }
                    return instance;
                }
            }
        }

        public static void Reset() {
            instance = new PitStore();
            instance.stopList = new List<PitStop>();
        }

        public PitStop GetLatestStopForCar(int carIdx) {

            var lastStop = PitStore.instance.stopList.FindLast(x => x.CarIdx == carIdx) ?? new PitStop();
            return lastStop;
        }

        public PitStop GetInProgressStopForCar(int carIdx) {

            var lastStop = PitStore.instance.stopList.FindLast(x => x.CarIdx == carIdx) ?? new PitStop();
            return lastStop;
        }

        public List<PitStop> GetAllStopsForCar (int carIdx) {
            var allStops = PitStore.instance.stopList.FindAll(x => x.CarIdx == carIdx && x.LastPitStallTimeSeconds > 0);
            return allStops;
        }

        public List<PitStop> GetAllCarsInPitlane() {
            var allStops = PitStore.instance.stopList.FindAll(x => x.CurrentPitLaneTimeSeconds > 0);
            allStops.Find(x => x.CarIdx == StrategyBundle.Instance.SafetyCarIdx);

            return allStops;
        }

       // public List<PitStop> GetAllCarsInPitlaneAsDeliitedString() {
            //var carsIinLane;
           // return allStops;
      //  }

        public List<PitStop> GetCountOfStopsUnderSCPeriodForCar(int carIdx, int SafetyCarPeriodNumber) {
            var allStops = PitStore.instance.stopList.FindAll(x => x.CarIdx == carIdx && x.LastPitStallTimeSeconds > 0 && x.SafetyCarPeriodNumber == SafetyCarPeriodNumber);
            return allStops;
        }

        public List<PitStop> GetAllCPSStopsForCar(int carIdx) {
            var allStops = PitStore.instance.stopList.FindAll(x => x.CarIdx == carIdx && x.LastPitLap > 0 && x.LastPitStallTimeSeconds > 0);

            foreach (var item in allStops) {
                // Was the Stop a valid CPS for vets?
                bool isValid = true;

                // Does the SC come early?
                if (item.FirstSCPeriodBreaksEarlySCRule && item.SafetyCarPeriodNumber == 1) {
                    isValid = false;
                }
                // Was it too short
                if (item.LastPitStallTimeSeconds < 0.5) {
                    isValid = false;
                }

                if (item.Lap < 2) {
                    isValid = false;
                }
                item.IsCPSStop = isValid;
            }
            return allStops;

        }

        public void AddStop(PitStop stop) {

            if (stop.CarIdx != StrategyBundle.Instance.SafetyCarIdx) {
                instance.stopList.Add(stop);
            }
        }

        public void UpdateLastStop(PitStop stop) {

            // Check if our stop is in the store
            var tmpStop = instance.stopList.Find(x => (x.Lap == stop.Lap) && (x.CarIdx == stop.CarIdx));
            //var tmpStop = instance.stopList.FindLast(x => x.CarIdx == stop.CarIdx);

            // Update the lap
            if (tmpStop.PitLaneEntryTime >= 0) {
                tmpStop.Lap = StrategyBundle.Instance.CurrentLap;
            }

            // update the stop details
            tmpStop._PitCounterHasIncremented = stop._PitCounterHasIncremented;
            tmpStop.NumberOfPitstops = stop.NumberOfPitstops;

            tmpStop.PitLaneEntryTime = stop.PitLaneEntryTime;
            tmpStop.PitLaneExitTime = stop.PitLaneExitTime;

            tmpStop.PitStallEntryTime = stop.PitStallEntryTime;
            tmpStop.PitStallExitTime = stop.PitStallExitTime;

            tmpStop.LastPitLaneTimeSeconds = stop.LastPitLaneTimeSeconds;
            tmpStop.LastPitStallTimeSeconds = stop.LastPitStallTimeSeconds;

            tmpStop.CurrentPitLaneTimeSeconds = stop.CurrentPitLaneTimeSeconds;
            tmpStop.CurrentPitStallTimeSeconds = stop.CurrentPitStallTimeSeconds;

            tmpStop.LastPitLap = stop.LastPitLap;
            tmpStop.CurrentStint = stop.CurrentStint;
        }
    }



    public class PitStop {
        public int Lap;
        public int CarIdx;
        public string DriverName;
        public bool IsUnderSC;
        public int SafetyCarPeriodNumber;
        public bool FirstSCPeriodBreaksEarlySCRule;
        public bool IsCPSStop;
        public bool SCFirstStop = true;

        // this all needs to be saved and restored
        public bool _PitCounterHasIncremented;
        public int NumberOfPitstops { get; set; }

        public double? PitLaneEntryTime { get; set; }
        public double? PitLaneExitTime { get; set; }

        public double? PitStallEntryTime { get; set; }
        public double? PitStallExitTime { get; set; }

        public double LastPitLaneTimeSeconds { get; set; }
        public double LastPitStallTimeSeconds { get; set; }

        public double CurrentPitLaneTimeSeconds { get; set; }
        public double CurrentPitStallTimeSeconds { get; set; }

        public int LastPitLap { get; set; }
        public int CurrentStint { get; set; }

        public override string ToString() {
            return $"{DriverName} L:{Lap} BT:{CurrentPitStallTimeSeconds} PT:{CurrentPitLaneTimeSeconds} LBT:{LastPitStallTimeSeconds} LPT:{LastPitLaneTimeSeconds} CPS{IsCPSStop}";
        }

    }
}

/*
public class PitStops {
        public int carIdx;
        public int carClassId = 0;
        public double fuelPerLap = 3.0;
        public double fuelFillPerSecond = 2.4;
        public int totalLapsInRace = 1;

        public List<PitStop> Stops = new List<PitStop>();

        public void PitStop(int Lap, bool InPitLane, bool InPitBox, double PitLaneDuration, double BoxDuration, bool StoppedUnderSC) {

            // Check if the stop exists, if so create it
            if (Stops.Count > 0) {
                var stop = Stops.Find(x => x.Lap == Lap);
                if (stop == null) {
                    PitStop newStop = new PitStop() {
                        Lap = Lap,
                        Completed = !(InPitLane && InPitBox),
                        StoppedUnderSC = StoppedUnderSC,
                        TimeInBox = BoxDuration,
                        TimeInLane = PitLaneDuration,
                    };
                }
                else {
                    stop.Completed = !(InPitLane && InPitBox);
                    stop.TimeInBox = BoxDuration;
                    stop.TimeInLane = PitLaneDuration;
                }
            }
        }


        public int Number { get { return Stops.Count; } }
        
        public int LastStopOnLap {
            get {
                if (Stops.Count == 0)
                    return 0;
                else
                    return Stops.Last().Lap;
            }
        }

        public double LastStopTimeInPitBox {
            get {
                if (Stops.Count == 0)
                    return 0;
                else
                    return Stops.Last().TimeInBox;
            }
        }

        public double LastStopEstimatedRange {
            get {
                if (Stops.Count == 0)
                    return 0;
                else
                    return (Stops.Last().TimeInBox*fuelFillPerSecond)/fuelPerLap;
            }
        }

        public double AllStopsEstimatedRange {
            get {
                if (Stops.Count == 0)
                    return 0;
                else {
                    double totalTimeStoppped = 0;
                    foreach (PitStop stop in Stops) {
                        totalTimeStoppped += stop.TimeInBox;
                    }
                    return (totalTimeStoppped * fuelFillPerSecond) / fuelPerLap;
                }
            }
        }

        public double LastStopTimeInPitLane {
            get {
                if (Stops.Count == 0)
                    return 0;
                else
                    return Stops.Last().TimeInLane;
            }
        }

        public string StopOnLapsDelimitedString {
            get {
                if (Stops.Count == 0)
                    return "";
                else
                    return string.Join(",", Stops);
            }
        }

        public string TimeInPitBoxDelimitedString {
            get {
                if (Stops.Count == 0)
                    return "";
                else {
                    List<double> stopTimes = new List<double>();
                    foreach (var item in Stops) {
                        stopTimes.Add(item.TimeInBox);
                    }
                    return string.Join(",", stopTimes);
                }
            }
        }

        public string EstimatedRangeDelimitedString {
            get {
                if (Stops.Count == 0)
                    return "";
                else {
                    List<double> estimatedRanges = new List<double>();
                    foreach (var item in Stops) {
                        estimatedRanges.Add((item.TimeInBox * fuelFillPerSecond) / fuelPerLap);
                    }
                    return string.Join(",", estimatedRanges);
                }
            }
        }

        public string TimeInPitLaneDelimitedString {
            get {
                if (Stops.Count == 0)
                    return "";
                else {
                    List<double> laneTimes = new List<double>();
                    foreach (var item in Stops) {
                        laneTimes.Add(item.TimeInLane);
                    }
                    return string.Join(",", laneTimes);
                }
            }
        }

        public int StopsTotalNumber {
            get {
                int stopCount = Stops.Count();
                return Stops.Count;

            }
        }

        public int StopsCPSNumber {
            get {
                int CPSCount = Stops.FindAll(x => x.CountsAsCPS).Count();
                return CPSCount;
            }
        }

        public int NummberOfStops {  get { return Stops.Count; } }

       
        
    }

  

}
*/