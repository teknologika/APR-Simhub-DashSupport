﻿using GameReaderCommon;
using iRacingSDK;
using SimHub.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace APR.DashSupport {

    public partial class APRDashPlugin : IPlugin, IDataPlugin, IWPFSettingsV2 {

        private List<Opponent> opponents;
        private List<ExtendedOpponent> OpponentsExtended;
        private List<carClass> carClasses = new List<carClass>();

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
                return OpponentsExtended.FindAll(a => a.LapDistSpectatedCar < 0);
            }
        }

        private List<ExtendedOpponent> OpponentsBehind {
            get {
                return OpponentsExtended.FindAll(a => a.LapDistSpectatedCar > 0);
            }
        }

        private double GetReferenceClassLaptime() {
            double averageLapTime = 0;
            int count = 0;
            foreach (var item in this.OpponentsInClass()) {

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
            
            return averageLapTime;
         }

        private double ReferenceClassLaptime {
            get {
                double averageLapTime = 0;
                int count = 0;
                foreach (var item in this.OpponentsInClass()) {

                    // use the  last lap time
                    if (item.LastLapTimeSeconds > 0 &&
                         (item.LastLapTimeSeconds < (item.LastLapTimeSeconds * 1.05)) &&
                         (item.LastLapTimeSeconds > (item.LastLapTimeSeconds * 0.95))) {
                        averageLapTime += item.LastLapTimeSeconds;
                        count++;
                        averageLapTime = averageLapTime / count;
                    }
                    // if the last lap time is empty, try and use the best
                    else if (item.BestLapTimeSeconds > 0 &&
                         (item.BestLapTimeSeconds < (item.BestLapTimeSeconds * 1.05)) &&
                         (item.BestLapTimeSeconds > (item.BestLapTimeSeconds * 0.95))) {
                        averageLapTime += item.BestLapTimeSeconds;
                        count++;
                        averageLapTime = averageLapTime / count;
                    }
                }
                return averageLapTime;
            }
        }
    }

    public class ExtendedOpponent {
        public Opponent _opponent;
        public SessionData._DriverInfo._Drivers _competitor;
        public float _trackLength;
        public float _specatedCarLapDistPct;
        public int _spectatedCarCurrentLap;
        public int CarIdx { get { return Convert.ToInt32(_competitor.CarIdx); } }
        public string DriverName { get { return _opponent.Name; } }
        public string TeamName { get { return _opponent.TeamName; } }
        public string CarClass { get { return _opponent.CarClass; } }
        public int CarClassID { get { return (int)_competitor.CarClassID; } }

        public int Position { get { return _opponent.Position; } }
        public int PositionInClass { get { return _opponent.PositionInClass; } }

        public int CurrentLap { get { return _opponent.CurrentLap ?? -1; } }
        public int LapsToLeader { get { return _opponent.LapsToLeader ?? -1; } }
        public double TrackPositionPercent { get { return _opponent.TrackPositionPercent ?? 0.0; } }
        public string TrackPositionPercentString { get { return TrackPositionPercent.ToString("0.000"); } }
        public double LapDist { get { return TrackPositionPercent * _trackLength; } }

        public double LapDistSpectatedCar {
            get {

                // Do we need to add or subtract a lap
                var lapDifference = Convert.ToInt32(CurrentLap - _spectatedCarCurrentLap);
                var cappedLapDifference = Math.Max(Math.Min(lapDifference, 1), -1);
                double distanceAdjustment = 0;

                if (cappedLapDifference == 1) {
                    distanceAdjustment = -_trackLength * 1000;
                }
                else if (cappedLapDifference == -1) {
                    distanceAdjustment = _trackLength * 1000;
                }

                return ((_specatedCarLapDistPct * _trackLength) - LapDist) * 1000 + distanceAdjustment;
            }
        }

        public TimeSpan LastLapTime { get { return _opponent.LastLapTime; } }
        public double LastLapTimeSeconds { get { return LastLapTime.TotalSeconds; } }
        public TimeSpan BestLapTime { get { return _opponent.BestLapTime; } }
        public double BestLapTimeSeconds { get { return BestLapTime.TotalSeconds; } }

        public TimeSpan? CurrentLapTime { get { return _opponent.CurrentLapTime; } }
        public double CurrentLapTimeSeonds { get { return CurrentLapTime.GetValueOrDefault().TotalSeconds; } }

        public override string ToString() {
            return "Idx: " + CarIdx + " P:" + Position + " " + DriverName + " " + LapDistSpectatedCar.ToString("0.00");
        }
    }
}
