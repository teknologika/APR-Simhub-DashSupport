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
using System.Diagnostics;
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

        // Data required to make Extended Opponent work in the function

        private List<GameReaderCommon.Opponent> opponents;
        private List<ExtendedOpponent> OpponentsExtended = new List<ExtendedOpponent>();


        private List<CarClass> carClasses = new List<CarClass>();
        private float trackLength;

        public void UpdateLivePositions() {
            List<ExtendedOpponent> opponentsSortedLivePosition = OpponentsExtended.OrderBy(x => x.Lap).ThenByDescending(x => x.LapDisanceInMeters).ToList();
            List<ExtendedOpponent> opponentsSortedLivePositionInClass = OpponentsExtended.OrderBy(x => x.CarClass).ThenByDescending(x => x.Lap).ThenByDescending(x => x.LapDisanceInMeters).ToList();

            int livePosition = 1;
            foreach (var item in opponentsSortedLivePosition) {
                item.LivePosition = livePosition;

                livePosition++;
            }

            foreach (var item in carClasses) {
                int CarClasslivePosition = 1;
                foreach (var car in opponentsSortedLivePosition) {
                    if (car.CarClassID == item.carClassID) {
                        car.CarClassLivePosition = CarClasslivePosition;
                        CarClasslivePosition++;
                    }
                }
            }
        }

        public class ExtendedOpponent {
            public GameReaderCommon.Opponent _opponent;
            public SessionData._DriverInfo._Drivers _competitor;
            public GameData data;
            public DataSampleEx irdata;

            public Telemetry telemetry;
            public PitStop LatestPitInfo;
            public StrategyBundle StrategyObserver;

            private const float PIT_MINSPEED = 0.01f;

            public void FuelDataUpdate() {
                // FIXME with strat update


            }

            public void CalculatePitInfo(double time) {

                StrategyObserver = StrategyBundle.Instance;

                // restore any old pit stop
                LatestPitInfo = PitStore.Instance.GetLatestStopForCar(this.CarIdx);
                LatestPitInfo.CarIdx = this.CarIdx;
                LatestPitInfo.DriverName = this.DriverName;
               

#if DEBUG
                var name = LatestPitInfo.DriverName;
#endif

                // If we are not in the world (blinking?), stop checking
                if (!IsInWorld) {
                    return;
                }

                // Are we NOW in pit lane (pitstall includes pitlane)
                //  InPitLane = IsApproachingPits || IsInPitStall;

                // Are we NOW in pit stall?
                //IsInPitStall = IsInPitStall;
                LatestPitInfo.CurrentStint = Lap - LatestPitInfo.LastPitLap;

                // Were we already in pitlane previously?
                if (LatestPitInfo.PitLaneEntryTime == null) {
                    // We were not previously in pitlane
                     if (IsCarInPitLane) {

                        Debug.WriteLine(LatestPitInfo.DriverName + " in lane");    
                      
                        // We have only just now entered pitlane
                        LatestPitInfo.Lap = Lap;
                        LatestPitInfo.PitLaneEntryTime = time;
                        LatestPitInfo.SafetyCarPeriodNumber = StrategyObserver.SafetyCarPeriodCount;
                        LatestPitInfo.IsUnderSC = StrategyObserver.IsUnderSC || StrategyObserver.IsSafetyCarMovingInPitane;

                        LatestPitInfo.CurrentPitLaneTimeSeconds = 0;

                        PitStore.Instance.AddOrUpdateStop(LatestPitInfo);
                    }
                }
                else {

  
                    // We were already in pitlane but have not exited yet
                    LatestPitInfo.CurrentPitLaneTimeSeconds = time - LatestPitInfo.PitLaneEntryTime.Value;

                    // Were we already in pit stall?
                    if (LatestPitInfo.PitStallEntryTime == null) {
                        // We were not previously in our pit stall yet
                        if (IsInPitStall) {
                            if (Math.Abs(Speed) > PIT_MINSPEED) {
                                // Debug.WriteLine("PIT: did not stop in pit stall, ignored.");
                            }
                            else {

                                Debug.WriteLine(LatestPitInfo.DriverName + " time to box was " + LatestPitInfo.CurrentPitLaneTimeSeconds.ToString("0.0"));

                                // We have only just now entered our pit stall
                                LatestPitInfo.PitStallEntryTime = time;
                                LatestPitInfo.CurrentPitStallTimeSeconds = 0;
                            }
                        }
                    }
                    else {
                        // We already were in our pit stall
                        LatestPitInfo.CurrentPitStallTimeSeconds = time - LatestPitInfo.PitStallEntryTime.Value;



                        if (!IsInPitStall) {

                            // We have now left our pit stall


                            LatestPitInfo.LastPitStallTimeSeconds = time - LatestPitInfo.PitStallEntryTime.Value;
                            LatestPitInfo.CurrentPitStallTimeSeconds = 0;

                            Debug.WriteLine(LatestPitInfo.DriverName + " was in box for " + LatestPitInfo.LastPitStallTimeSeconds.ToString("0.0"));


                            if (LatestPitInfo.PitStallExitTime != null) {
                                var diff = LatestPitInfo.PitStallExitTime.Value - time;
                                if (Math.Abs(diff) < 5) {
                                    // Sim detected pit stall exit again less than 5 seconds after previous exit.
                                    // This is not possible?
                                    return;
                                }
                            }

                            // Did we already count this stop?
                            if (!LatestPitInfo._PitCounterHasIncremented) {
                                // Now increment pitstop count
                                LatestPitInfo.NumberOfPitstops += 1;
                                LatestPitInfo._PitCounterHasIncremented = true;
                            }

                            LatestPitInfo.LastPitLap = Lap;
                            LatestPitInfo.CurrentStint = 0;

                            // Reset
                            LatestPitInfo.PitStallEntryTime = null;
                            LatestPitInfo.PitStallExitTime = time;
                        }

                    }

                    if (!IsCarInPitLane) {
                        // We have now left pitlane
                        LatestPitInfo.PitLaneExitTime = time;
                        LatestPitInfo._PitCounterHasIncremented = false;

                        LatestPitInfo.LastPitLaneTimeSeconds = LatestPitInfo.PitLaneExitTime.Value - LatestPitInfo.PitLaneEntryTime.Value;
                        LatestPitInfo.CurrentPitLaneTimeSeconds = 0;


                        // Reset
                        LatestPitInfo.PitLaneEntryTime = null;
                        PitStore.Instance.AddOrUpdateStop(LatestPitInfo);

                        Debug.WriteLine(LatestPitInfo.DriverName + " was in Lane for " + LatestPitInfo.LastPitLaneTimeSeconds.ToString("0.0"));
                        Debug.WriteLine("Transit time was: " + (LatestPitInfo.LastPitLaneTimeSeconds - LatestPitInfo.LastPitStallTimeSeconds).ToString("0.0"));
                        Debug.WriteLine("Stop was underSC: " + LatestPitInfo.IsUnderSC + " and is a CPS:" + LatestPitInfo.IsCPSStop);
                        Debug.WriteLine("Current CPS Count is : " + this.PitStops_NumberOfCPSStops);

                    }
                }
                PitStore.Instance.AddOrUpdateStop(LatestPitInfo);
            }

            public int PitStops_NumberOfStops {
                get {
                    return LatestPitInfo.NumberOfPitstops;
                }
            }

            public int PitStops_LastStopOnLap {
                get {
                    return LatestPitInfo.LastPitLap;
                }
            }

            public string PitStops_AllStopsStopLastStopOnLapDelimitedString {
                get {
                    List<double> stopLap = new List<double>();
                    if (LatestPitInfo.NumberOfPitstops == 0)
                        return "";
                    else {

                        var Stops = PitStore.Instance.GetAllStopsForCar(CarIdx);

                        foreach (PitStop stop in Stops) {
                            stopLap.Add(stop.Lap);
                        }
                        return string.Join(",", stopLap);
                    }
                }
            }

            public string PitStops_AllCPSStopsStopLastStopOnLapDelimitedString {
                get {
                    List<double> stopLap = new List<double>();
                    if (LatestPitInfo.NumberOfPitstops == 0)
                        return "";
                    else {

                        var Stops = PitStore.Instance.GetAllCPSStopsForCar(CarIdx);

                        foreach (PitStop stop in Stops) {
                            stopLap.Add(stop.Lap);
                        }
                        return string.Join(",", stopLap);
                    }
                }
            }

            public double PitStops_LastStopTimeInPitBox {
                get {
                    return LatestPitInfo.LastPitStallTimeSeconds;
                }
            }

            public double PitStops_LastStopTimeInPitLane {
                get {
                    return LatestPitInfo.LastPitLaneTimeSeconds;
                }
            }

            public double PitStops_LastStopTimeInTransit {
                get {
                    return LatestPitInfo.LastPitStallTimeSeconds - LatestPitInfo.LastPitLaneTimeSeconds;
                }
            }

            public double PitStops_LastStopEstimatedRange {
                get {
                    if (LatestPitInfo.NumberOfPitstops == 0)
                        return 0;
                    else
                        return (LatestPitInfo.LastPitStallTimeSeconds * StrategyObserver.FuelFillRateLitresPerSecond) / StrategyObserver.FuelLitersPerLap;
                }
            }

            public double PitStops_EstimatedNextStop {
                get {
                    if (LatestPitInfo.NumberOfPitstops == 0)
                        return 0;
                    else
                        return Lap + Math.Floor(PitStops_LastStopEstimatedRange / StrategyObserver.FuelLitersPerLap);
                }
            }

            public double PitStops_EstimatedNextStopTime {
                get { 
                    // NEXT
                        return Lap + Math.Floor( StrategyObserver.EstimatedTotalFuel);
                }
            }

            public double PitStops_AllStopsStopTimeInPitBox {
                get {
                    if (LatestPitInfo.NumberOfPitstops == 0)
                        return 0;
                    else {
                        double totalTimeStoppped = 0;
                        var Stops = PitStore.Instance.GetAllStopsForCar(CarIdx);

                        foreach (PitStop stop in Stops) {
                            totalTimeStoppped += stop.LastPitStallTimeSeconds;
                        }
                        return totalTimeStoppped;
                    }
                }
            }

            public string PitStops_AllStopsStopTimeInPitBoxDelimitedString {
                get {
                    List<double> stopTimes = new List<double>();
                    if (LatestPitInfo.NumberOfPitstops == 0)
                        return "";
                    else {
                        
                        var Stops = PitStore.Instance.GetAllStopsForCar(CarIdx);

                        foreach (PitStop stop in Stops) {
                            stopTimes.Add(stop.LastPitStallTimeSeconds);
                        }
                        return string.Join(",", stopTimes);
                    }
                }
            }

            public double PitStops_AllStopsEstimatedRange {
                get {
                    if (LatestPitInfo.NumberOfPitstops == StrategyObserver.StartingFuel / StrategyObserver.FuelLitersPerLap)
                        return 0;
                    else {
                        return (PitStops_AllStopsStopTimeInPitBox * (StrategyObserver.FuelFillRateLitresPerSecond) + StrategyObserver.StartingFuel / StrategyObserver.FuelLitersPerLap);
                    }
                }
            }

            public int PitStops_NumberOfCPSStops {
                get {
                        var Stops = PitStore.Instance.GetAllCPSStopsForCar(CarIdx);
                        return Stops.Count -1;
                }
            }

            public bool PitStops_CPS1Served {
                get {
                    return (PitStops_NumberOfCPSStops == 1);
                }
            }

            public bool PitStops_CPS2Served {
                get {
                    return (PitStops_NumberOfCPSStops == 2);
                }
            }


            // FIXME
            public bool _showTeamNames = false;

            public int _trackSurface;

            public double _carEstTime;
            public double _carBestLapTime;
            public double _carLastLapTime;

            public double CarEstTime { get { return _carEstTime; } }
            public double _CarIdxF2Time;
            public double CarF2Time { get { return _CarIdxF2Time; } }



            // This is used for the SC
            // public int _safetyCarIdx;
            // public double StrategyObserver.SafetyCarTrackDistancePercent;
            // public bool _IsunderSafetyCar;

            public int _classleaderCarIdx;
            public double _classleaderLapDistPct;
            public int _gridPosition;

            // to do add multi-classes and overall leader

            public bool IsInWorld { get { return (_trackSurface > -1); } }
            public bool IsNotInWorld { get { return (_trackSurface == -1); } }
            public bool IsOffTrack { get { return (_trackSurface == 0); } }
            public bool IsInPitStall { get { return (_trackSurface == 1); } }
            public bool IsCarInPitBox { get { return _opponent.IsCarInPit; } }
            public bool IsCarInPitLane { get { return _opponent.IsCarInPitLane; } }
            public bool IsApproachingPits { get { return (_trackSurface == 2); } }
            public bool IsOnTrack { get { return (_trackSurface == 3); } }

            public bool IsSlow {
                get {
                    return ( IsOnTrack && (Speed < 20.0)) || (IsOffTrack && (Speed > 20.0));
                }
            }

            public bool IsConnected { get { return _opponent.IsConnected; } }
            public bool IsCarInGarage { get { return _opponent.IsCarInGarage.GetValueOrDefault(); } }
            public bool IsOutlap { get { return _opponent.IsOutLap; } }

            public bool IsSpectator { get { return this.CarIdx == StrategyObserver.SpectatedCarIdx; } }
            public bool IsPlayer { get { return _opponent.IsPlayer; } }

            public double Speed { get { return _opponent.Speed.GetValueOrDefault(); } }

            public string TrackLocation {
                get {

                    switch (_trackSurface) {
                        case 0:
                            return "Off Track";

                        case 1:
                            return "In Pit Stall";

                        case 2:
                            return "Approaching Pits";

                        case 3:
                            return "On Track";

                        default:
                            return "Not In World";
                    }
                }
            }

            public int CarIdx { get { return Convert.ToInt32(_competitor.CarIdx); } }
            public string Name {
                get {
                    // Need to know if this is a teams event and we should show team names
                    if (_showTeamNames) {
                        return TeamName;
                    }
                    return DriverName;
                }
            }
            public string DriverName { get { return _opponent.Name; } }
            public string TeamName { get { return _opponent.TeamName; } }
            public string CarClass { get { return _opponent.CarClass; } }
            public int CarClassID { get { return (int)_competitor.CarClassID; } }
            public double CarClassReferenceLapTime { get; set; }
            public string CarClassColor {
                get {
                    return _competitor.CarClassColor.ToLower().Replace("0x", "#FF"); ;
                }
            }
            public string CarClassTextColor {
                get {
                    if (CarClassColor == "#FF000000") {
                        return "#FFFFFFFF";
                    }
                    else {
                        return "#FF000000";
                    }
                }
            }

            public int LivePosition;
            public int CarClassLivePosition;
            public int PositionRaw { get { return _opponent.Position; } }

            public int Position {
                get {
                    // If we have not completed a lap
                    if (Lap < 1) {
                        // If we have start position, use that otherwise use live position
                        if (_opponent.StartPosition.GetValueOrDefault() < 1) {
                            return LivePosition;
                        }
                        else {
                            return _opponent.StartPosition.GetValueOrDefault();
                        }
                    }
                    return _opponent.Position;
                }
            }

            public string PositionString {
                get {
                    if (Position < 1) {
                        return "";
                    }
                    return Position.ToString();
                }
            }



            public int PositionInClass {
                get {

                    // If we have not completed a lap
                    if (Lap < 1) {
                        // This is buggy as on lap 1 use live position
                        return CarClassLivePosition;
                    }
                    return _opponent.PositionInClass;
                }
            }


            public string PositionInClassString {
                get {
                    if (_opponent.PositionInClass < 1) {
                        return "";
                    }
                    return _opponent.PositionInClass.ToString();
                }
            }

            public string CarNumber { get { return _opponent.CarNumber; } }


            public int CurrentLap { get { return _opponent.CurrentLap ?? -1; } }
            public int Lap { get { return CurrentLap; } }
            public int LapsToLeader { get { return _opponent.LapsToLeader ?? -1; } }


            // Relative specific fields
            public int CappedLapToSpectatedCar(ExtendedOpponent opponent) {

                var lapDifference = Convert.ToInt32(StrategyObserver.SpectatedCarCurrentLap - opponent.CurrentLap);
                var cappedLapDifference = Math.Max(Math.Min(lapDifference, 1), -1);

                // -1 is behind, 0 same, +1 ahead  
                return cappedLapDifference;
            }
            public string SimpleRelativeGapTimeString;
            public double SortingRelativeGapToSpectator;

            public int AheadBehind {
                get {
                    if (SortingRelativeGapToSpectator < 0) {
                        return 1;
                    }
                    else if (SortingRelativeGapToSpectator > 0) {
                        return -1;
                    }
                    return 0;
                }
            }

            public int LapAheadBehind {
                get {
                    if (StrategyObserver.SpectatedCarCurrentLap > Lap) {
                        return 1;
                    }
                    else if (StrategyObserver.SpectatedCarCurrentLap == Lap) {
                        return 0;
                    }
                    return -1;
                }
            }

            public double TrackPositionPercent { get { return _opponent.TrackPositionPercent ?? 0.0; } }
            public string TrackPositionPercentString { get { return TrackPositionPercent.ToString("0.000"); } }
            public double LapDisanceInMeters { get { return TrackPositionPercent * StrategyObserver.TrackLength; } }

            public double LapDistPctSpectatedCar {
                get {
                    // calculate the difference between the two cars
                    var pctGap = StrategyObserver.SpecatedCarLapDistPct - _opponent.TrackPositionPercent.Value;
                    if (pctGap > 50.0) {
                        pctGap -= 50.0;
                    }
                    else if (pctGap < -50.0) {
                        pctGap += 50;
                    }

                    return pctGap;
                }
            }

            public double LapDistPctSafetyCar {
                get {
                    // calculate the difference between the two cars
                    if (StrategyObserver.SafetyCarTrackDistancePercent == 0) {
                        return 0;
                    }

                    var pctGap = StrategyObserver.SafetyCarTrackDistancePercent - _opponent.TrackPositionPercent.Value;
                    if (pctGap > 50.0) {
                        pctGap -= 50.0;
                    }
                    else if (pctGap < -50.0) {
                        pctGap += 50;
                    }

                    return pctGap;
                }
            }

            public double LapDistSafetyCar {
                get {
                    // calculate the difference between the two cars
                    var distance = (StrategyObserver.SafetyCarTrackDistancePercent * StrategyObserver.TrackLength) - (_opponent.TrackPositionPercent.Value * StrategyObserver.TrackLength);
                    if (distance > StrategyObserver.TrackLength / 2) {
                        distance -= StrategyObserver.TrackLength;
                    }
                    else if (distance < -StrategyObserver.TrackLength / 2) {
                        distance += StrategyObserver.TrackLength;
                    }

                    return distance;
                }
            }

            public string LapDistSafetyCarString {
                get {
                    if (LapDistSafetyCar > 0) {
                        return  LapDistSafetyCar.ToString("0") + "m AHEAD";
                    }
                    return  LapDistSafetyCar.ToString("0") + "m BEHIND";
                }
            }

            public double LapDistanceSlowCar {
                get {
                    // calculate the difference between the two cars
                    var distance = (StrategyObserver.SlowOpponentLapDistPct * StrategyObserver.TrackLength) - (_opponent.TrackPositionPercent.Value * StrategyObserver.TrackLength);
                    if (distance > StrategyObserver.TrackLength / 2) {
                        distance -= StrategyObserver.TrackLength;
                    }
                    else if (distance < -StrategyObserver.TrackLength / 2) {
                        distance += StrategyObserver.TrackLength;
                    }

                    return distance;
                }
            }

            public bool IsSlowCarAhead {
                get {
                    if (LapDistanceSlowCar > 0)
                        return true;
                    return false;
                }
            }
            
            public string LapDistanceSlowCarAheadString {
                get {
                    if (LapDistanceSlowCar > 0 ) {
                        return LapDistanceSlowCar.ToString("0") + "m AHEAD";
                    }
                    return "" ;
                }
            }

            public double LapDistSpectatedCar {
                get {
                     // calculate the difference between the two cars
                     var distance = (StrategyObserver.SpecatedCarLapDistPct * StrategyObserver.TrackLength) - (_opponent.TrackPositionPercent.Value * StrategyObserver.TrackLength);
                     if (distance > StrategyObserver.TrackLength /2) {
                        distance -= StrategyObserver.TrackLength;
                     }
                     else if (distance < -StrategyObserver.TrackLength/2) {
                        distance += StrategyObserver.TrackLength;
                     }

                    return distance; 
                }
            }
            
            public double LapDistPctClassLeader {
                get {
                    // calculate the difference between the two cars
                    var pctGap = _classleaderLapDistPct - _opponent.TrackPositionPercent.Value;
                    if (pctGap > 50.0) {
                        pctGap -= 50.0;
                    }
                    else if (pctGap < -50.0) {
                        pctGap += 50;
                    }

                    return pctGap;
                }
            }

            public double LapDistClassLeader {
                get {
                    // calculate the absolute difference between the two cars
                    var distance = (_classleaderLapDistPct * StrategyObserver.TrackLength) - (_opponent.TrackPositionPercent.Value * StrategyObserver.TrackLength);
                    if (distance > StrategyObserver.TrackLength / 2) {
                        distance -= StrategyObserver.TrackLength;
                    }
                    else if (distance < -StrategyObserver.TrackLength / 2) {
                        distance += StrategyObserver.TrackLength;
                    }

                    return distance;
                }
            }
           
            public string GapToClassLeaderString {
                get {
                    if (LapsToLeader > 0) {
                        return LapsToLeader.ToString() + "L";
                    }
                    return GapToClassLeader.ToString("0.0");
                }
            }

            public bool IsLastLapPersonalBest;
            public bool IsBestLapOverallBest;
            public bool IsBestLapClassBestLap;


            // these gets pushed in
            // CarAheadInClassBestLapDelta
            public double CarAheadInClassGap;
            public double CarBehindInClassGap;
            public double GapToClassLeader;
            public double GapToOverallLeader;
            public double CarAheadOverallGap; 
            public double CarBehindOverallGap;
            public ExtendedOpponent CarInClassAhead;
            public ExtendedOpponent CarInClassBehind;


            // Pit Info Properties
            // private readonly Driver _driver;
            /*
            public bool _PitCounterHasIncremented;

            public int Pitstops { get; set; }

         //   public bool InPitLane { get; set; }
        //    public bool InPitStall { get; set; }

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
            */
            public double CarAheadInClassBestLapDelta {
                get {
                    if (CarInClassAhead != null) {
                        return CarInClassAhead.BestLapTimeSeconds - BestLapTimeSeconds;
                    }
                    else return 0;
                }
            }

            public double CarBehindInClassBestLapDelta {
                get {
                    if (CarInClassBehind != null) {
                        return CarInClassBehind.BestLapTimeSeconds - BestLapTimeSeconds;
                    }
                    else return 0;
                }
            }


            public double CarAheadInClassLastLapDelta {
                get {
                    if (CarInClassAhead != null) {
                        return CarInClassAhead.LastLapTimeSeconds - LastLapTimeSeconds;
                    }
                    else return 0;
                }
            }

            public String CarAheadInClassLastLapDeltaString {
                get {
                    if(CarAheadInClassBestLapDelta < 0) {
                        return "-" + Math.Abs(CarAheadInClassBestLapDelta);
                    }
                    else {
                        return "+" + Math.Abs(CarAheadInClassBestLapDelta);
                    }
                }
            }

            public double CarBehindInClassLastLapDelta {
                get {
                    if (CarInClassBehind != null) {
                        return CarInClassBehind.LastLapTimeSeconds - LastLapTimeSeconds;
                    }
                    else return 0;
                }
            }

            public String CarBehindInClassLastLapDeltaString {
                get {
                    if (CarBehindInClassBestLapDelta < 0) {
                        return "+" + Math.Abs(CarAheadInClassBestLapDelta);
                    }
                    else {
                        return "-" + Math.Abs(CarAheadInClassBestLapDelta);
                    }
                }
            }

            public string CarAheadPositionInClass {
                get {
                    if (CarInClassAhead != null) {
                        return CarInClassAhead.PositionInClassString;
                    }
                    else return "";
                }
            }

            public string CarBehindPositionInClass {
                get {
                    if (CarInClassBehind != null) {
                        return CarInClassBehind.PositionInClassString;
                    }
                    else return "";
                }
            }

            public string CarAheadInClassDriverName {
                get {
                    if (CarInClassAhead != null) {
                        return CarInClassAhead.Name;
                    }
                    else return "";
                }
            }

            public string CarBehindInClassDriverName {
                get {
                    if (CarInClassBehind != null) {
                        return CarInClassBehind.Name;
                    }
                    else return "";
                }
            }

            public string ClassAheadInClassGapString {
                get {
                    return Math.Abs(CarAheadInClassGap).ToString("0.0");
                }
            }

            public string CarBehindInClassGapString {
                get {
                    return "-" + Math.Abs(CarBehindInClassGap).ToString("0.0");
                }
            }

            public string GapToOverallPositionAheadString {
                get {
                    return CarAheadOverallGap.ToString("0.0");
                }
            }

            public string GapToOverallPositionBehindString {
                get {
                    return Math.Abs(CarBehindOverallGap).ToString("0.0");
                }
            }

            public double GapSpectatedCar {
                get {
                    return CarClassReferenceLapTime / StrategyObserver.TrackLength * LapDistSpectatedCar;
                }
            }

            public int LapSpectatedCar {
                get {
                    return StrategyObserver.SpectatedCarCurrentLap;
                }
            }

            public string DriverNameColour {
                get {
                    if (IsCarInPitBox || IsCarInPitLane || IsCarInGarage || !IsConnected) {
                        return "#FF808080";
                    }

                    if (StrategyObserver.SessionType == "Race") {

                        if (CurrentLap > StrategyObserver.SpectatedCarCurrentLap) {
                            return IsCarInPitLane ? "#7F1818" : "#FE3030"; // Lapping you
                        }
                        else if (CurrentLap == StrategyObserver.SpectatedCarCurrentLap) {
                            return IsCarInPitLane ? "#7F7F7F" : "#FFFFFF"; // Same lap as you
                        }
                        else {
                            return IsCarInPitLane ? "#00607F" : "#00C0FF"; // Being lapped by you
                        }
                    }
                    else {
                        return "#ffffffff";
                    }
                }
            }

            public string SafetyRating {
                get {
                    return _opponent.LicenceString.Remove(5, 1).Replace(" ", "");
                }
            }
            public string SafetyRatingSimple {
                get {
                    return _opponent.LicenceString.Substring(0, 1);
                }
            }
            public string LicenseColor { get; set; }
            public int iRating {
                get {
                    return (int)_competitor.IRating;
                }
            }
            public string iRatingString {
                get {
                    if (iRating < 1) {
                        return "";
                    }
                    return (iRating / 1000d).ToString("0.0") + "k";
                }
            }
            public string iRatingChange { get; set; }


            public TimeSpan LastLapTime { get { return TimeSpan.FromSeconds(_carLastLapTime); } }
           
            public double LastLapTimeSeconds {
                get {
                    if (_carLastLapTime > 0) {
                        return _carLastLapTime;
                    }
                    else {
                        return 0;
                    }
                }
            }

            public string LastLapTimeString {
                get {
                    if (_carLastLapTime > 0) {
                        return NiceTime(LastLapTime);
                    }
                    else {
                        return "-.---";
                    }
                }
            }

            public TimeSpan BestLapTime { get { return TimeSpan.FromSeconds(_carBestLapTime); } }
            
            public double BestLapTimeSeconds {
                get {
                    if (_carBestLapTime > 0) {
                        return _carBestLapTime;
                    }
                    else {
                        return 0;
                    }
                }
            }

            public string LastLapDynamicColor {
                get {
                    if (Lap < 1) {
                        return Color_White;
                    }
                    if (IsBestLapClassBestLap || IsBestLapOverallBest ) {
                        return Color_Purple;
                    }
                    else if (IsLastLapPersonalBest) {
                        return Color_Green;
                    }
                    else return Color_White;
                }
            }

            public string BestLapDynamicColor {
                get {
                    if (Lap < 1 ) {
                        return Color_White;
                    }
                    if (IsBestLapClassBestLap || IsBestLapOverallBest) {
                        return Color_Purple;
                    }
                    else return Color_White;
                }
            }

            public string BestLapTimeString {
                get {
                    if (_carBestLapTime > 0) {
                        return NiceTime(BestLapTime);
                    }
                    else {
                        return "-.---"; ;
                    }
                }
            }


            public string NiceTime(TimeSpan timeToFormat) {

                if (timeToFormat == TimeSpan.Zero) {
                    return "-.---";
                }
                if (timeToFormat.Minutes == 0) {
                    return timeToFormat.ToString("s'.'fff");
                }
                else {
                    return timeToFormat.ToString("m':'ss'.'fff");
                }
            }

            public TimeSpan? CurrentLapTime { get { return _opponent.CurrentLapTime; } }
            public double CurrentLapTimeSeonds { get { return CurrentLapTime.GetValueOrDefault().TotalSeconds; } }

            public string PitStatusString {
                get {
                    if (_opponent.IsCarInPit) {
                        return "BOX";
                    }
                  
                    if (_trackSurface == 2) {
                        return "APPROACH";
                    }

                    if (_opponent.IsCarInPitLane) {
                        return "LANE";
                    }

                    if (_opponent.IsOutLap) {
                        return "OUT";
                    }

                    return "";
                }
            }

            public override string ToString() {
                return "Idx: " + CarIdx + " " + DriverName + " P:" + Position + " L:" + Lap + " A:" + LapAheadBehind +" Gap:" + ClassAheadInClassGapString + " Ldr:" + GapToClassLeaderString + " lst:" + LastLapTime;
            }

            public string StandingsToString() {
                return "Idx: " + CarIdx + " " + DriverName + " P:" + Position + " Gap" + ClassAheadInClassGapString + " Ldr" +  GapToClassLeaderString + "lst" + LastLapTime;
            }

            public string RelativeToString() {
                return "Idx: " + CarIdx + " " + DriverName + " A:" + LapAheadBehind +  " P:" + Position + " PL::" + LivePosition + " %:" + TrackPositionPercent.ToString("0.00") + " %S:" + LapDistPctSpectatedCar.ToString("0.00") + " d:" + LapDistSpectatedCar.ToString("0.00") + " d1:";
            }

            public string Color_DarkGrey = "#FF898989";
            public string Color_LightGrey = "#FF808080";
            public string Color_Purple = "#Ff990099";
            public string Color_Green = "#FF009933";
            public string Color_White = "#FFFFFFFF";
            public string Color_Black = "#FF000000";
            public string Color_Yellow = "#FFFFFF70";
            public string Color_Red = "#FFFF0000";
            public string Color_Transparent = "#00000000";
            public string Color_DarkBackground = "#FF2D2D2D";
            public string Color_LightBlue = "DeepSkyBlue";
        }


        public class CarClass {
            public int carClassID;
            public string carClassShortName;
            public int LeaderCarIdx;
            public double LeaderTotalTime;
            public double ReferenceLapTime;
            public TimeSpan BestLapTime;
        }

        public void CheckAndAddCarClass(int CarClassID, string CarClassShortName) {
            bool has = this.carClasses.Any(a => a.carClassID == CarClassID);

            if (has == false) {
                this.carClasses.Add(new CarClass() { carClassID = CarClassID, carClassShortName = CarClassShortName });
            }
        }

        private ExtendedOpponent SpectatedCar {
            get { return this.OpponentsExtended.Find(a => a.CarIdx == irData.Telemetry.CamCarIdx); }
        }

        private ExtendedOpponent LeadingCar {
            get {
                return this.OpponentsExtended.Find(a => a.Position == 1 ); }
        }

        private ExtendedOpponent ClassLeadingCar(int CarClassID) {
            var classOnly = OpponentsExtended.FindAll(a => a.CarClassID == CarClassID).OrderBy(a => a.Position).ToList();
            if (classOnly != null) {
                return classOnly[0];
            }
            return new ExtendedOpponent();
        }

        private List<ExtendedOpponent> OpponentsInClasss {
            get {
                // TODO : add logic so if driving use that Id instead of spectated car
                return OpponentsExtended.FindAll(a => a.CarClassID == this.SpectatedCar.CarClassID);
            }
        }
        private List<ExtendedOpponent> OpponentsInClass() {
            return this.OpponentsInClass(this.SpectatedCar.CarClassID);
        }

        private List<ExtendedOpponent> OpponentsInClass(int CarClassID) {
            try {
                return OpponentsExtended.FindAll(a => a.CarClassID == this.SpectatedCar.CarClassID);
            }
            catch (Exception) {
                return new List<ExtendedOpponent>();
            }
        }

        private List<ExtendedOpponent> OpponentsInClassSortedByPosition(int CarClassID) {
            try {
                List<ExtendedOpponent> tmp = OpponentsInClass(CarClassID).OrderBy(a => a.Position).ToList();
                return tmp;
            }
            catch (Exception) {
                return new List<ExtendedOpponent>();
            }

        }

     
        
    }
}
