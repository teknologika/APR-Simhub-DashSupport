﻿using GameReaderCommon;
using iRacingSDK;
using SimHub.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Markup;
using System.Windows.Media.Animation;

namespace APR.DashSupport {

    public partial class APRDashPlugin : IPlugin, IDataPlugin, IWPFSettingsV2 {

        private List<Opponent> opponents;
        private List<ExtendedOpponent> OpponentsExtended = new List<ExtendedOpponent>();
        private List<carClass> carClasses = new List<carClass>();
        private float trackLength;

        public class carClass {
            public int carClassID;
            public string carClassShortName;
        }

        public void CheckAndAddCarClass(int CarClassID, string CarClassShortName) {
            bool has = this.carClasses.Any(a  => a.carClassID == CarClassID);

            if (has == false) {

                this.carClasses.Add(new carClass() {carClassID = CarClassID, carClassShortName = CarClassShortName});
            }
        }

        private ExtendedOpponent SpectatedCar {
            get { return this.OpponentsExtended.Find(a => a.CarIdx == irData.Telemetry.CamCarIdx); }
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
            return this.OpponentsExtended.FindAll(a => a.CarClassID == this.SpectatedCar.CarClassID);
        }

        private List<ExtendedOpponent> OpponentsAhead {
            get {
                return OpponentsExtended.FindAll(a => a.LapDistSpectatedCar < 0).OrderByDescending(a => a.LapDistSpectatedCar).ToList();  
            }
        }

        private List<ExtendedOpponent> OpponentsBehind {
            get {
                return OpponentsExtended.FindAll(a => a.LapDistSpectatedCar > 0).OrderBy(a => a.LapDistSpectatedCar).ToList();
            }
        }

        private double GetReferenceClassLaptime() {
            return GetReferenceClassLaptime(this.SpectatedCar.CarClassID);
        } 

        private double GetReferenceClassLaptime(int CarClassID) {
            double averageLapTime = 0;
            int count = 0;
            foreach (var item in this.OpponentsInClass(CarClassID)) {

                // use the  last lap time
                if (item.LastLapTimeSeconds > 0 &&
                        (item.LastLapTimeSeconds < (item.LastLapTimeSeconds * 1.05)) &&
                        (item.LastLapTimeSeconds > (item.LastLapTimeSeconds * 0.95))) {
                    averageLapTime += item.LastLapTimeSeconds;
                    count++;
                }
                // if the last lap time is empty, try and use the best
                else if (item.BestLapTimeSeconds > 0 &&
                        (item.BestLapTimeSeconds < (item.BestLapTimeSeconds * 1.05)) &&
                        (item.BestLapTimeSeconds > (item.BestLapTimeSeconds * 0.95))) {
                    averageLapTime += item.BestLapTimeSeconds;
                    count++;
                    
                }
            }
            if (count > 0) {
                averageLapTime = averageLapTime / count;
            }
            // if no time, just use 2 mins
            if (averageLapTime == 0) {
                return 120.0;
            }
            return averageLapTime;
         }

        private double ReferenceClassLaptime {
            get {
                return GetReferenceClassLaptime();
            }
        }

        private double GetGapAsTimeForClass(int CarClassID, double DistanceToTarget) {
            return (GetReferenceClassLaptime(CarClassID) / trackLength) * DistanceToTarget;

        }

        public double RelativeGapToSpectatedCar(int CarIdx) {
            return this.GetGapAsTimeForClass(this.SpectatedCar.CarClassID, this.OpponentsExtended[CarIdx].LapDistSpectatedCar );
        }
    }

    public class ExtendedOpponent {
        public Opponent _opponent;
        public string _sessionType;
        public SessionData._DriverInfo._Drivers _competitor;
        public float _trackLength;
        public float _specatedCarLapDistPct;
        public int _spectatedCarCurrentLap;
        public int CarIdx { get { return Convert.ToInt32(_competitor.CarIdx); } }
        public string DriverName { get { return _opponent.Name; } }
        public string TeamName { get { return _opponent.TeamName; } }
        public string CarClass { get { return _opponent.CarClass; } }
        public int CarClassID { get { return (int)_competitor.CarClassID; } }
        public double CarClassReferenceLapTime { get;set; }
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
        public int Position { get { return _opponent.Position; } }
        public string PositionString {
            get {
                if (_opponent.Position < 1 ) {
                    return "";
                }
                return _opponent.Position.ToString();
            }
        }

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
        public int LapsToLeader { get { return _opponent.LapsToLeader ?? -1; } }
        public double TrackPositionPercent { get { return _opponent.TrackPositionPercent ?? 0.0; } }
        public string TrackPositionPercentString { get { return TrackPositionPercent.ToString("0.000"); } }
        public double LapDist { get { return TrackPositionPercent * _trackLength; } }
        
        public int CappedLapToSpectatedCar(ExtendedOpponent opponent) {
            
           var lapDifference = Convert.ToInt32(_spectatedCarCurrentLap - opponent.CurrentLap);
           var cappedLapDifference = Math.Max(Math.Min(lapDifference, 1), -1);

           // -1 is behind, 0 same, +1 ahead  
           return cappedLapDifference;
        }

        public int AheadBehind {
            get {
                return CappedLapToSpectatedCar(this);
            }
        }

        public string DriverNameColour {
            get {
                if(_opponent.IsCarInPit || _opponent.IsCarInPitLane) {
                    return "#FF808080";
                }

                if (_sessionType == "Race") {

                    // driver is behind so LightSkyBlue
                    if (AheadBehind > 0) {
                        return "#FF87CEFA";
                    }
                    // driver is ahead so Salmon
                    else if (AheadBehind < 0) {
                        return "#FFFA8072";
                    }
                }
                return "#ffffffff";
            }
        }

        public string SafetyRating {
            get {
                return _opponent.LicenceString.Remove(5,1).Replace(" ","");
            }
        }

        public string LicenseColor { get; set;}
       
        public int iRating {
            get {
                return (int)_competitor.IRating;
            }
        }
    
        public string iRatingString {
            get {
                if(iRating < 1) {
                    return "";
                }
                return (iRating / 1000d).ToString("0.0") + "k";
            }
        }
               
        public string iRatingChange { get; set; }

        public double LapDistPctSpectatedCar {
            get {
                Console.WriteLine(this.DriverName);
                // Do we need to add or subtract a lap
                var lapDifference = Convert.ToInt32(_spectatedCarCurrentLap - CurrentLap);
                var cappedLapDifference = Math.Max(Math.Min(lapDifference, 1), -1);
                double percentAdjustment = 0;

                //if ((_opponent.TrackPositionPercent.Value - _specatedCarLapDistPct) < -0.50 ) {
                //    percentAdjustment = 1;
               // }

                if (_specatedCarLapDistPct < 0 && _specatedCarLapDistPct > -50) {
                    return 1d - _specatedCarLapDistPct;
                }

                return _opponent.TrackPositionPercent.Value - _specatedCarLapDistPct;
            }
        }

        public double LapDistSpectatedCar {
            get {
                Console.WriteLine(this.DriverName);
                // Do we need to add or subtract a lap
                var lapDifference = Convert.ToInt32(_spectatedCarCurrentLap - CurrentLap);
                var cappedLapDifference = Math.Max(Math.Min(lapDifference, 1), -1);
                double distanceAdjustment = 0;

                if (cappedLapDifference == 1) {
                    //distanceAdjustment = +_trackLength;
                 }
                else if (cappedLapDifference == -1) {
                  //distanceAdjustment = -_trackLength;
                }

                if(_specatedCarLapDistPct < 0 && _specatedCarLapDistPct > -50) {
                    return 1d - _specatedCarLapDistPct;
                }

                return ((_specatedCarLapDistPct * _trackLength) - LapDist) + distanceAdjustment;
            }
        }

        public double GapSpectatedCar {
            get {
                return CarClassReferenceLapTime / _trackLength  * LapDistSpectatedCar;
            }
        }

        public int LapSpectatedCar { 
            get {
                return _spectatedCarCurrentLap;
            }
        }

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
            return "Idx: " + CarIdx + " P:" + Position + " " + DriverName + " " + LapDistSpectatedCar.ToString("0.00") + " " + GapSpectatedCar.ToString("0.00");
        }

    }
}
