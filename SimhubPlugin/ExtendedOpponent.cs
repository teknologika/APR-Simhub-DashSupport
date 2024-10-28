﻿using GameReaderCommon;
using IRacingReader;
using iRacingSDK;
using MahApps.Metro.Controls;
using SimHub.Plugins;
using SimHub.Plugins.OutputPlugins.Dash.GLCDTemplating;
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

        public StrategyBundle StrategyObserver = new StrategyBundle();

        public class StrategyBundle {
            // todo make this a singleton

            public double FuelFillRateLitresPerSecond;
            public double StartingFuel;
            public double FuelLitersPerLap;
            public bool IsUnderSC;
            public int SafetyCarPeriodCount;
        }

        public void UpdateStrategyBundle() {

            // Get the starting fuel
            StrategyObserver.StartingFuel = GetProp("DataCorePlugin.GameRawData.SessionData.CarSetup.Chassis.Rear.FuelLevel") ?? 0.0;

            // Get the average fuel per lap
            StrategyObserver.FuelLitersPerLap = GetProp("DataCorePlugin.Computed.Fuel_LitersPerLap") ?? 0.0;

            //Supercar Gen2 hardcode FIXME
            StrategyObserver.FuelFillRateLitresPerSecond = 2.4;

            // For vets, are we under SC
            StrategyObserver.IsUnderSC = IsUnderSafetyCar;

            StrategyObserver.SafetyCarPeriodCount = SafetyCarPeriodCount;

        }

        public void UpdateLivePositions() {
            List<ExtendedOpponent> opponentsSortedLivePosition = OpponentsExtended.OrderBy(x => x.Lap).ThenByDescending(x => x.LapDisanceInMeters).ToList();
            List<ExtendedOpponent> opponentsSortedLivePositionInClass = OpponentsExtended.OrderBy(x => x.CarClass).ThenByDescending(x => x.Lap).ThenByDescending(x => x.LapDisanceInMeters).ToList();

            int livePosition = 1;
            foreach (var item in opponentsSortedLivePosition) {
                item.LivePosition = livePosition;

                livePosition++;
            }
          
            foreach (var item in carClasses)
            {
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
            public StrategyBundle StrategyOberver;

            private const float PIT_MINSPEED = 0.01f;

            public void FuelDataUpdate() {
                // FIXME with strat update


            }

            public void CalculatePitInfo(double time, bool underSC) {

                // restore any old pit stop
                LatestPitInfo = PitStore.Instance.GetLatestStopForCar(this.CarIdx);
                LatestPitInfo.CarIdx = this.CarIdx;

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
                        // We have only just now entered pitlane
                        LatestPitInfo.PitLaneEntryTime = time;

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

                        PitStore.Instance.AddOrUpdateStop(LatestPitInfo);

                        // Reset
                        LatestPitInfo.PitLaneEntryTime = null;

                        PitStore.Instance.AddOrUpdateStop(LatestPitInfo);
                    }
                }
                PitStore.Instance.AddOrUpdateStop(LatestPitInfo);
            }

            public int PitStops_NumberOfStops {
                get {
                    return LatestPitInfo.NumberOfPitstops;
                }
            }

            public int PitStops_LastPitLap {
                get {
                    return LatestPitInfo.LastPitLap;
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

            public double LastStopEstimatedRange {
                get {
                    if (LatestPitInfo.NumberOfPitstops == 0)
                        return 0;
                    else
                        return (LatestPitInfo.LastPitStallTimeSeconds * StrategyOberver.FuelFillRateLitresPerSecond) / StrategyOberver.FuelLitersPerLap;
                }
            }

            /*
public class PitStops {
      
        public double fuelPerLap = 3.0;
        public double fuelFillPerSecond = 2.4;
        public int totalLapsInRace = 1;

        public List<PitStop> Stops = new List<PitStop>();


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



            // FIXME
            public bool _showTeamNames = false;

            // these need to be injected on creation for calcs to work
            public float _trackLength;
            public int _trackSurface;
            public string _sessionType;

            public double _carEstTime;
            public double _carBestLapTime;
            public double _carLastLapTime;

            public double CarEstTime { get { return _carEstTime; } }
            public double _CarIdxF2Time;
            public double CarF2Time { get { return _CarIdxF2Time; } }

            // Car being driven / spectating from
            public long _spectatedCarIdx;
            public float _specatedCarLapDistPct;
            public int _spectatedCarCurrentLap;

            public int _slowOpponentIdx;
            public double _slowOpponentLapDistPct;

            // This is used for the SC
            public int _safetyCarIdx;
            public double _safetyCarLapDistPct;
            public bool _IsunderSafetyCar;

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
                    return (IsOnTrack && (Speed < 20.0)) || (IsOffTrack && (Speed > 20.0));
                }
            }

            public bool IsConnected { get { return _opponent.IsConnected; } }
            public bool IsCarInGarage { get { return _opponent.IsCarInGarage.GetValueOrDefault(); } }
            public bool IsOutlap { get { return _opponent.IsOutLap; } }

            public bool IsSpectator { get { return this.CarIdx == _spectatedCarIdx; } }
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

                var lapDifference = Convert.ToInt32(_spectatedCarCurrentLap - opponent.CurrentLap);
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
                    if (_spectatedCarCurrentLap > Lap) {
                        return 1;
                    }
                    else if (_spectatedCarCurrentLap == Lap) {
                        return 0;
                    }
                    return -1;
                }
            }

            public double TrackPositionPercent { get { return _opponent.TrackPositionPercent ?? 0.0; } }
            public string TrackPositionPercentString { get { return TrackPositionPercent.ToString("0.000"); } }
            public double LapDisanceInMeters { get { return TrackPositionPercent * _trackLength; } }

            public double LapDistPctSpectatedCar {
                get {
                    // calculate the difference between the two cars
                    var pctGap = _specatedCarLapDistPct - _opponent.TrackPositionPercent.Value;
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
                    if (_safetyCarLapDistPct == 0) {
                        return 0;
                    }

                    var pctGap = _safetyCarLapDistPct - _opponent.TrackPositionPercent.Value;
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
                    var distance = (_safetyCarLapDistPct * _trackLength) - (_opponent.TrackPositionPercent.Value * _trackLength);
                    if (distance > _trackLength / 2) {
                        distance -= _trackLength;
                    }
                    else if (distance < -_trackLength / 2) {
                        distance += _trackLength;
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
                    var distance = (_slowOpponentLapDistPct * _trackLength) - (_opponent.TrackPositionPercent.Value * _trackLength);
                    if (distance > _trackLength / 2) {
                        distance -= _trackLength;
                    }
                    else if (distance < -_trackLength / 2) {
                        distance += _trackLength;
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
            
            public string LapDistanceSlowCarheadString {
                get {
                    if (LapDistanceSlowCar > 0) {
                        return LapDistanceSlowCar.ToString("0") + "m AHEAD";
                    }
                    return LapDistanceSlowCar.ToString("0") + "m AHEAD"; ;
                }
            }

            public double LapDistSpectatedCar {
                get {
                     // calculate the difference between the two cars
                     var distance = (_specatedCarLapDistPct * _trackLength) - (_opponent.TrackPositionPercent.Value * _trackLength);
                     if (distance > _trackLength /2) {
                        distance -= _trackLength;
                     }
                     else if (distance < -_trackLength/2) {
                        distance += _trackLength;
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
                    var distance = (_classleaderLapDistPct * _trackLength) - (_opponent.TrackPositionPercent.Value * _trackLength);
                    if (distance > _trackLength / 2) {
                        distance -= _trackLength;
                    }
                    else if (distance < -_trackLength / 2) {
                        distance += _trackLength;
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
                    return CarClassReferenceLapTime / _trackLength * LapDistSpectatedCar;
                }
            }

            public int LapSpectatedCar {
                get {
                    return _spectatedCarCurrentLap;
                }
            }

            public string DriverNameColour {
                get {
                    if (IsCarInPitBox || IsCarInPitLane || IsCarInGarage || !IsConnected) {
                        return "#FF808080";
                    }

                    if (_sessionType == "Race") {

                        if (CurrentLap > _spectatedCarCurrentLap) {
                            return IsCarInPitLane ? "#7F1818" : "#FE3030"; // Lapping you
                        }
                        else if (CurrentLap == _spectatedCarCurrentLap) {
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

            public bool CPS1Served;
            public bool CPS2Served;

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
