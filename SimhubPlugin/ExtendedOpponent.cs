﻿using GameReaderCommon;
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
using System.Text;
using System.Threading.Tasks;
using System.Windows.Markup;
using System.Windows.Media.Animation;

namespace APR.DashSupport {

    public partial class APRDashPlugin : IPlugin, IDataPlugin, IWPFSettingsV2 {

        // Data required to make Extended Opponent work in the function

        private List<GameReaderCommon.Opponent> opponents;
        private List<ExtendedOpponent> OpponentsExtended = new List<ExtendedOpponent>();
        
        private List<carClass> carClasses = new List<carClass>();
        private float trackLength;


        public void UpdateLivePositions() {
            List<ExtendedOpponent> opponentsSortedLivePosition = OpponentsExtended.OrderBy(x => x.Lap).ThenByDescending(x => x.LapDist).ToList();
            int livePosition = 1;
            foreach (var item in opponentsSortedLivePosition) {
                item.LivePosition = livePosition;
                livePosition++;
            }

            foreach (var item in OpponentsExtended) {
                item.LivePosition = opponentsSortedLivePosition.Find(x => x.CarIdx == item.CarIdx).LivePosition;
            }

            foreach (var carClass in carClasses)
            {
                List<ExtendedOpponent> carsInClass = opponentsSortedLivePosition.FindAll(x => x.CarClassID == carClass.carClassID);
                int classPosition = 1;
                foreach (var item in carsInClass)
                {
                    item.CarClassLivePosition = classPosition;
                    classPosition++;
                }
            }

        }

    public class ExtendedOpponent {
            public GameReaderCommon.Opponent _opponent;
            public SessionData._DriverInfo._Drivers _competitor;
          

            // FIXME
            public bool _showTeamNames = false;

            // these need to be injected on creation for calcs to work
            public float _trackLength;
            public int _trackSurface;
            public string _sessionType;

            public double _carEstTime;
            public double CarEstTime { get { return _carEstTime; } }
            public double _CarIdxF2Time;
            public double CarF2Time { get { return _CarIdxF2Time; } }

            // Car being driven / spectating from
            public long _spectatedCarIdx;
            public float _specatedCarLapDistPct;
            public int _spectatedCarCurrentLap;

            // This is used for the SC
            public int _safetyCarIdx;
            public double _safetyCarLapDistPct;
            public bool _IsunderSafetyCar;

            public int _classleaderCarIdx;
            public double _classleaderLapDistPct;
            public int _gridPosition;

            // to do add multi-classes and overall leader

            public bool IsOnTrack { get { return (_trackSurface == 3); } }
            public bool IsOffTrack { get { return (_trackSurface == 0); } }
            public bool IsInWorld { get { return (_trackSurface > -1); } }
            public bool IsSlow { get { return (IsOnTrack && (Speed > 30.0)); } }

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
                    return _competitor.CarClassColor.ToLower().Replace("0x", "#ff"); ;
                }
            }
            public string CarClassTextColor {
                get {
                    if (CarClassColor == "#ff000000") {
                        return "#ffffffff";
                    }
                    else {
                        return "#ff000000";
                    }
                }
            }
            public int LivePosition;
            public int CarClassLivePosition;
            public int PositionRaw { get { return _opponent.Position; } }

            public int Position {
                get {
                    if ( _opponent.Position == 0) {
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

            public bool IsConnected { get { return _opponent.IsConnected; } }
            public bool IsCarInPit { get { return _opponent.IsCarInPit; } }
            public bool IsCarInPitLane { get { return _opponent.IsCarInPitLane; } }
            public bool IsCarInGarage { get { return _opponent.IsCarInGarage.GetValueOrDefault(); } }
            public bool IsOutlap { get { return _opponent.IsOutLap; } }
            public bool IsPlayer { get { return _opponent.IsPlayer; } }
            public bool IsSpectator { get { return _spectatedCarIdx == _competitor.CarIdx; } }

            public string CarNumber { get { return _opponent.CarNumber; } }

            public int PositionInClass { get { return _opponent.PositionInClass; } }
            public string PositionInClassString {
                get {
                    if (_opponent.PositionInClass < 1) {
                        return "";
                    }
                    return _opponent.PositionInClass.ToString();
                }
            }
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

            public double TrackPositionPercent { get { return _opponent.TrackPositionPercent ?? 0.0; } }
            public string TrackPositionPercentString { get { return TrackPositionPercent.ToString("0.000"); } }
            public double LapDist { get { return TrackPositionPercent * _trackLength; } }
            
            
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
                        return "      " + LapDistSafetyCar.ToString("0") + "m AHEAD      ";
                    }
                    return "      " + LapDistSafetyCar.ToString("0") + "m BEHIND      ";
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
                    // calculate the difference between the two cars
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

            public double GapToClassLeader {
                get {
                   return CarClassReferenceLapTime / _trackLength * LapDistClassLeader;
                }
            }

            public string GapToClassLeaderString {
                get {
                    return GapToClassLeader.ToString("0.0");
                }
            }

            // this gets pushed in
            public double GapToPositionInClassAhead;

            public string GapToPositionInClassAheadString {
                get {
                    return GapToPositionInClassAhead.ToString("0.0");
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
                    if (IsCarInPit || IsCarInPitLane || IsCarInGarage || !IsConnected) {
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

            public TimeSpan LastLapTime { get { return _opponent.LastLapTime; } }
            public double LastLapTimeSeconds { get { return LastLapTime.TotalSeconds; } }
            public TimeSpan BestLapTime { get { return _opponent.BestLapTime; } }
            public double BestLapTimeSeconds { get { return BestLapTime.TotalSeconds; } }

            public TimeSpan? CurrentLapTime { get { return _opponent.CurrentLapTime; } }
            public double CurrentLapTimeSeonds { get { return CurrentLapTime.GetValueOrDefault().TotalSeconds; } }

            public string PitInfo {
                get {
                    if (_opponent.IsCarInPit) {
                        return "BOX";
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
                return "Idx: " + CarIdx + " " + DriverName + " A:" + AheadBehind + " %:" + " P:" + Position + " PL::" + LivePosition + " %:" + TrackPositionPercent.ToString("0.00") + " %S:" + LapDistPctSpectatedCar.ToString("0.00") +  " d:" + LapDistSpectatedCar.ToString("0.00") + " d1:";
            }

            public string StandingsToString() {
                return "Idx: " + CarIdx + " " + DriverName + " A:" + AheadBehind + " %:" + " P:" + Position + " %:" + TrackPositionPercent.ToString("0.00") + " %S:" + LapDistPctSpectatedCar.ToString("0.00") + " %S1:" +  " d:" + LapDistSpectatedCar.ToString("0.00") + " d1:";
            }

            public string RelativeToString() {
                return "Idx: " + CarIdx + " " + DriverName + " A:" + AheadBehind + " %:" + " P:" + Position + " %:" + TrackPositionPercent.ToString("0.00") + " %S:" + LapDistPctSpectatedCar.ToString("0.00") + " %S1:" + " d:" + LapDistSpectatedCar.ToString("0.00") + " d1:";
            }
        }


        public class carClass {
            public int carClassID;
            public string carClassShortName;
            public int LeaderCarIdx;
        }

        public void CheckAndAddCarClass(int CarClassID, string CarClassShortName) {
            bool has = this.carClasses.Any(a => a.carClassID == CarClassID);

            if (has == false) {
                this.carClasses.Add(new carClass() { carClassID = CarClassID, carClassShortName = CarClassShortName });
            }
        }

        private ExtendedOpponent SpectatedCar {
            get { return this.OpponentsExtended.Find(a => a.CarIdx == irData.Telemetry.CamCarIdx); }
        }

        private ExtendedOpponent LeadingCar {
            get {
                return this.OpponentsExtended.Find(a => a.Position == 1 ); }
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

        private List<ExtendedOpponent> OpponentsInClassSortedByPositionInClass(int CarClassID) {
            try {
                List<ExtendedOpponent> tmp = OpponentsInClass(CarClassID);
                return tmp.OrderBy(a => a.PositionInClass).ToList();
            }
            catch (Exception) {
                return new List<ExtendedOpponent>();
            }

        }

 


    }
}
