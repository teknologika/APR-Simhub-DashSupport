﻿using GameReaderCommon;
using iRacingSDK;
using SimHub.Plugins;
using SimHub.Plugins.OutputPlugins.Dash.GLCDTemplating;
using SimHub.Plugins.OutputPlugins.GraphicalDash.Behaviors.DoubleText.Imp;
using SimHub.Plugins.OutputPlugins.GraphicalDash.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Markup;
using System.Windows.Media.Animation;
using static System.Net.Mime.MediaTypeNames;

namespace APR.DashSupport {

    public partial class APRDashPlugin : IPlugin, IDataPlugin, IWPFSettingsV2 {


        private class RelativePositions {

            public Dictionary<string, List<int>> PositionCarIdx { get; private set; } = new Dictionary<string, List<int>>();
            public List<int[]> TrackPosCarIdx { get; private set; } = new List<int[]>();



            public List<ExtendedOpponent> SortInWorldOpponentsByTrackPct(List<ExtendedOpponent> opponents) {


                // loop the time around based on if the driver is 'in front' or not
                // technically we are on a loop so no one is in front or behind, so we take half the cars
                // and mark them in front, and half marked as behind.
                // we do this by adding or subtracting g_sessionObj.DriverInfo.DriverCarEstLapTime from time

                foreach (var car in opponents) {


                    double refLapTime;
                    if (car.CarEstTime > 0) {
                        refLapTime = car.CarEstTime;
                    }
                    else {
                        refLapTime = car.CarClassReferenceLapTime;
                    }

                    // if the gap is more than 50 of a lap ahead, they are actually behind
                    if (car.GapSpectatedCar > refLapTime / 2) {

                        // this just changes the sign from + to -
                        car.SortingRelativeGapToSpectator = -(car.GapSpectatedCar * 2);
                    }

                    // if the gap is more than 50 of a lap behind, they are actually ahead
                    else if (car.GapSpectatedCar < -(refLapTime / 2)) {
                        // this just changes the sign from - to +
                        car.SortingRelativeGapToSpectator = +(car.GapSpectatedCar * 2);
                    }
                    else {
                        car.SortingRelativeGapToSpectator = car.GapSpectatedCar;
                    }
                }

                // Ensure the car is in world
                List<ExtendedOpponent> opponentsInWorld = opponents.FindAll(a => a.IsInWorld).ToList();

                // Sort by position around track in descending order
                List<ExtendedOpponent> SortedOpponents = opponentsInWorld.OrderByDescending(a => a.TrackPositionPercent).ToList();

                return SortedOpponents;
            }

            private List<ExtendedOpponent> _relativePositions = new List<ExtendedOpponent>();
            private RelativeTable _relativeTable = new RelativeTable();
            public List<ExtendedOpponent> Ahead = new List<ExtendedOpponent>();
            public List<ExtendedOpponent> Behind = new List<ExtendedOpponent>();

            public void Clear() {
                _relativeTable.Clear();
                _relativePositions.Clear();
                Ahead.Clear();
                Behind.Clear();
            }

            public void Add(ExtendedOpponent item) {
                _relativePositions.Add(item);
            }

            public RelativeTable Get() {
                return _relativeTable;
            }

            private string TimeToStr_ms(double time, int precision) {
                // Convert time to a formatted string
                return time.ToString($"F{precision}");
            }

            private string FormatDriverNumber(int carIdx, bool pitRoad) {
                // Format driver number string based on carIdx and pitRoad status
                return pitRoad ? $"#{carIdx} (P)" : $"#{carIdx}";
            }

            private int DetermineIfLapAheadBedhind(ExtendedOpponent target, ExtendedOpponent spectator) {
                return DetermineIfLapAheadBedhind(target.CarIdx, spectator.CarIdx, target.Lap, spectator.Lap);
            }

            private int DetermineIfLapAheadBedhind(int targetCarIdx, int spectatorCarIdx, int targetCurrentLap, int spectatorCurrentLap) {
                if (targetCarIdx == spectatorCarIdx) {
                    return 0; // car is spectator / player
                }
                else if (targetCurrentLap > spectatorCurrentLap) {
                    return 1; // Lapping you
                }
                else if (targetCurrentLap == spectatorCurrentLap) {
                    return 0; // Same lap as you
                }
                else {
                    return -1; // Being lapped by you
                }
            }

            private string DetermineLapColor(int AheadOrBehind, bool pitRoad) {

                if (AheadOrBehind == 1) {
                    return pitRoad ? "#7F1818" : "#FE3030"; // Lapping you
                }
                else if (AheadOrBehind == -1) {
                    return pitRoad ? "#00607F" : "#00C0FF"; // Being lapped by you
                }
                else {
                    return pitRoad ? "#7F7F7F" : "#FFFFFF"; // Same lap as you
                }
            }

            private string DetermineColor(int i, int playerCarIdx, int lap, int playerLap, bool pitRoad) {
                if (i == playerCarIdx) {
                    return "#FFB923"; // Player car color
                }
                else if (lap > playerLap) {
                    return pitRoad ? "#7F1818" : "#FE3030"; // Lapping you
                }
                else if (lap == playerLap) {
                    return pitRoad ? "#7F7F7F" : "#FFFFFF"; // Same lap as you
                }
                else {
                    return pitRoad ? "#00607F" : "#00C0FF"; // Being lapped by you
                }
            }

            public void Update(List<ExtendedOpponent> opponents, ExtendedOpponent spectator) {
                this.Clear();
                List<ExtendedOpponent> sortedOponents = SortInWorldOpponentsByTrackPct(opponents);

                int spectatorIdx = spectator.CarIdx;
                double spectatorTime = spectatorIdx >= 0 ? spectator.CarEstTime : 0;
                int spectatorLap = spectatorIdx >= 0 ? spectator.CurrentLap : 0;

                for (int i = 0; i < sortedOponents.Count; i++) {

                    int carIdx = sortedOponents[i].CarIdx;
                    int lap = sortedOponents[i].Lap;
                    bool pitRoad = sortedOponents[i].IsCarInPitLane;
                    int racePos = sortedOponents[i].Position;
                    double remoteTime;

                    // FIXME:Handle this !!
                    remoteTime = sortedOponents[i].CarEstTime;
                    double simpleGapTime = (remoteTime - spectatorTime);
                    string simpleGapTimeString = TimeToStr_ms(remoteTime - spectatorTime, 1);
                    string numStr = FormatDriverNumber(carIdx, pitRoad);
                    string nameStr = sortedOponents[i].DriverName;
                    int aheadBehind = DetermineIfLapAheadBedhind(i, spectatorIdx, lap, spectatorLap);
                    //string color = DetermineColor


                    _relativeTable.Add(carIdx, racePos, numStr, nameStr, simpleGapTime, aheadBehind);
                }

                if (true) {

                    //****FixMe, need to loop the time around based on if the driver is 'in front' or not
                    // technically we are on a loop so no one is in front or behind, so we take half the cars
                    // and mark them in front, and half marked as behind.
                    // we do this by adding or subtracting g_sessionObj.DriverInfo.DriverCarEstLapTime from time

                    foreach (var item in _relativeTable.Get()) {
                        var car = opponents.Find(a => a.CarIdx == item.carIdx);

                        double refLapTime;
                        if (car.CarEstTime > 0) {
                            refLapTime = car.CarEstTime;
                        }
                        else {
                            refLapTime = car.CarClassReferenceLapTime;
                        }
                        // if the gap is more than 50 of a lap ahead, they are actually behind
                        if (item.simpleRelativeGapToSpectator > refLapTime / 2) {

                            // this just changes the sign from + to -
                            item.sortingRelativeGapToSpectator = -(item.simpleRelativeGapToSpectator * 2);
                        }

                        // if the gap is more than 50 of a lap behind, they are actually ahead
                        else if (item.simpleRelativeGapToSpectator < refLapTime / 2) {
                            // this just changes the sign from - to +
                            item.sortingRelativeGapToSpectator = +(item.simpleRelativeGapToSpectator * 2);
                        }
                        else {
                            item.sortingRelativeGapToSpectator = item.simpleRelativeGapToSpectator;
                        }
                    }
                }
                // Now we have the relative table loop through and get the cars ahead and behind
                foreach (var item in _relativeTable.Get()) {
                    // Add the cars ahead
                    if (item.sortingRelativeGapToSpectator > 0) {
                        var aheadCar = opponents.Find(a => a.CarIdx == item.carIdx);
                        aheadCar.SimpleRelativeGapTimeString = item.simpleRelativeGapToSpectatorString;
                        aheadCar.SortingRelativeGapToSpectator = item.sortingRelativeGapToSpectator;
                        //aheadCar.AheadBehind = DetermineIfLapAheadBedhind(aheadCar, spectator);
                        //aheadCar.AheadBehind = 1;
                        Ahead.Add(aheadCar);
                        // to do need to sort in reverse
                    }

                    // add the cars behind
                    else if (item.sortingRelativeGapToSpectator < 0) {
                        var behindCar = opponents.Find(a => a.CarIdx == item.carIdx);
                        behindCar.SimpleRelativeGapTimeString = item.simpleRelativeGapToSpectatorString;
                        behindCar.SortingRelativeGapToSpectator = item.sortingRelativeGapToSpectator;
                        //behindCar.AheadBehind = DetermineIfLapAheadBedhind(behindCar, spectator);
                        //aheadCar.AheadBehind = -1;
                        Behind.Add(behindCar);
                    }

                }
            }

        }


        public string RelativeDebug() {
            StringBuilder sb = new StringBuilder();
            foreach (var item in OpponentsExtended) {
                sb.AppendLine(item.ToString());
            }
            return sb.ToString();
        }

   
        private List<ExtendedOpponent> _opponentsAhead = new List<ExtendedOpponent>();
        private List<ExtendedOpponent> _opponentsBehind = new List<ExtendedOpponent>();

        public List<ExtendedOpponent> OpponentsAhead {
            get {
                // if the distance is negative they are ahead
                return OpponentsExtended.FindAll(a => a.LapDistSpectatedCar < 0 && a.IsConnected).OrderByDescending(a => a.LapDistSpectatedCar).ToList();
            }
            set {
                if (Settings.RelativeShowCarsInPits) {
                    _opponentsAhead = value.FindAll(a => (!a.IsCarInPitLane || !a.IsCarInPit || !a.IsCarInGarage));
                }
                else {
                    _opponentsAhead = value;
                }
            }
        }

        public List<ExtendedOpponent> OpponentsBehind {
            get {
                // if the distance is positive they are ahead
                return OpponentsExtended.FindAll(a => a.LapDistSpectatedCar > 0 && a.IsConnected).OrderBy(a => a.LapDistSpectatedCar).ToList();
            }
            set {
                if (Settings.RelativeShowCarsInPits) {
                    _opponentsBehind = value.FindAll(a => (!a.IsCarInPitLane || !a.IsCarInPit || !a.IsCarInGarage));
                }
                else {
                    _opponentsBehind = value;
                }
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
            return this.GetGapAsTimeForClass(this.SpectatedCar.CarClassID, this.OpponentsExtended[CarIdx].LapDistSpectatedCar);
        }

        private void UpdateRelativesAndStandings(GameData data) {

            if (Settings.EnableRelatives || Settings.EnableStandings) {

                this.opponents = data.NewData.Opponents;

                SessionData._DriverInfo._Drivers[] competitors = irData.SessionData.DriverInfo.CompetingDrivers;
                this.opponents = data.NewData.Opponents;
                this.OpponentsExtended = new List<ExtendedOpponent>();
                
                // Get the Spectated car info
                int spectatedCarIdx = irData.Telemetry.CamCarIdx;
                float spectatedCarLapDistPct = irData.Telemetry.CarIdxLapDistPct[spectatedCarIdx];
                int spectatedCarCurrentLap = irData.Telemetry.CarIdxLap[spectatedCarIdx];

                for (int i = 0; i < competitors.Length; ++i) {
                    for (int j = 0; j < opponents.Count; ++j) {
                        // Add the aligned Opponents and Competitor data to our ExtendedOpponent list
                        if (string.Equals(competitors[i].CarNumber, opponents[j].CarNumber)) {

                            // Add to the Extended Opponents class
                            OpponentsExtended.Add(new ExtendedOpponent() {
                                _sessionType = SessionType,
                                _opponent = opponents[j],
                                _competitor = competitors[i],
                                _carEstTime = irData.Telemetry.CarIdxEstTime[competitors[i].CarIdx],
                                _trackSurface = (int)irData.Telemetry.CarIdxTrackSurface[competitors[i].CarIdx],
                                _trackLength = trackLength,
                                _spectatedCarIdx = spectatedCarIdx,
                                _spectatedCarCurrentLap = spectatedCarCurrentLap,
                                _specatedCarLapDistPct = spectatedCarLapDistPct,
                                _IsunderSafetyCar = IsUnderSafetyCar,
                                _safetyCarIdx = SafetyCarIdx,
                                _safetyCarLapDistPct = SafetyCarLapDistPct,
                                LicenseColor = LicenseColor(opponents[j].LicenceString)
                            });

                            // Update the car class info
                            CheckAndAddCarClass((int)competitors[i].CarClassID, competitors[i].CarClassShortName);

                        }
                    }
                }

                // Create the spectator
                ExtendedOpponent spectator = OpponentsExtended.Find(a => a.CarIdx == spectatedCarIdx);

                // update car reference lap time
                foreach (var item in OpponentsExtended) {
                    item.CarClassReferenceLapTime = GetReferenceClassLaptime(item.CarClassID);
                }

                if (IsRaceSession) {
                    // update iRating gain / loss
                    foreach (var item in OpponentsExtended) {
                        item.iRatingChange = CalculateMultiClassIREstimation(item);
                    }

                    // Update live positions
                    UpdateLivePositions();

                    // Find the overall Leader           
                    ExtendedOpponent overallLeader = OpponentsExtended.Find(a => a.Position == 1);

                    // Find the overall leader for each class
                    foreach (var item in carClasses)
                    {
                        ExtendedOpponent classLeader =  OpponentsExtended.Find(a => a.CarClassID == item.carClassID && a.CarClassLivePosition == 1);
                        item.LeaderCarIdx = classLeader.CarIdx;
                    }

                    // Add the car class leader to the OpponentsExtended
                    foreach (var item in OpponentsExtended) {
                        item._classleaderCarIdx = carClasses.Find(x => x.carClassID == item.CarClassID).carClassID;
                    }

                    // Update the gap to the class leader
                    // Find the overall leader for each class
                    foreach (var item in carClasses) {
                        ExtendedOpponent classLeader = OpponentsExtended.Find(a => a.CarClassID == item.carClassID && a.CarClassLivePosition == 1);
                        item.LeaderCarIdx = classLeader.CarIdx;
                    }

                    foreach (var item in carClasses) {
                        List<ExtendedOpponent> carsInClass = OpponentsInClassSortedByPositionInClass(item.carClassID);
                        double previousCarGapToClassLeader = 0;
                        foreach (var car in carsInClass)
                        {
                            if (car.CarClassLivePosition <= 1) {
                                car.GapToPositionInClassAhead = 0;
                            }
                            else {
                                car.GapToPositionInClassAhead = car.GapToClassLeader - previousCarGapToClassLeader;
                                previousCarGapToClassLeader = car.GapToPositionInClassAhead;
                                OpponentsExtended.Find(x=> x.CarIdx == car.CarIdx).GapToPositionInClassAhead = car.GapToPositionInClassAhead;
                            }
                        }
                    }
                }

                //  RelativePositions relpos = new RelativePositions();
                //  relpos.Update(OpponentsExtended, spectator);
                //  this.OpponentsAhead = relpos.Ahead;
                //  this.OpponentsBehind = relpos.Behind;


#if DEBUG


                // this is for debugging only

                var ahead = this.OpponentsAhead;
                var behind = this.OpponentsBehind;
#endif
                UpdateRelativeProperties();
                UpdateStandingsRelatedProperties(ref data);

            }
        }


        public string GetGapforDriverAhead(int index, List<ExtendedOpponent> opponentsAhead) {
            index = index - 1;
            if (index + 1 > opponentsAhead.Count || index < 0) {
                return "";
            }

            var carAhead = opponentsAhead[index];

            return RelativeGapToSpectatedCar(carAhead.CarIdx).ToString("0.00");

        }

        public void UpdateRelativeProperties() {

            if (Settings.EnableRelatives) {
                ClearRelativeProperties();

                SetProp("Debug", RelativeDebug());

                // Header and properties
 



                int count = 1;
                foreach (var opponent in OpponentsAhead) {

                    if (Settings.RelativeShowCarsInPits || (!Settings.RelativeShowCarsInPits && !opponent._opponent.IsCarInPitLane)) {
                        SetProp("Relative.Ahead." + count + ".Position", opponent.PositionString);
                        SetProp("Relative.Ahead." + count + ".Name", opponent.DriverName);
                        SetProp("Relative.Ahead." + count + ".Show", opponent.DriverName != "");
                        SetProp("Relative.Ahead." + count + ".CarNumber", opponent.CarNumber);
                        SetProp("Relative.Ahead." + count + ".TrackPct", Math.Abs(opponent.LapDistPctSpectatedCar).ToString("0.0"));
                        SetProp("Relative.Ahead." + count + ".Distance", Math.Abs(opponent.LapDistSpectatedCar).ToString("0.0"));
                        SetProp("Relative.Ahead." + count + ".Gap", Math.Abs(opponent.GapSpectatedCar).ToString("0.0"));
                        SetProp("Relative.Ahead." + count + ".AheadBehind", opponent.AheadBehind.ToString());
                        SetProp("Relative.Ahead." + count + ".DriverNameColor", opponent.DriverNameColour);
                        SetProp("Relative.Ahead." + count + ".CarClassColor", opponent.CarClassColor);
                        SetProp("Relative.Ahead." + count + ".CarClassTextColor", opponent.CarClassTextColor);
                        SetProp("Relative.Ahead." + count + ".LicenseColor", opponent.LicenseColor);

                        SetProp("Relative.Ahead." + count + ".SR", opponent.SafetyRating);
                        SetProp("Relative.Ahead." + count + ".IR", opponent.iRatingString);
                        SetProp("Relative.Ahead." + count + ".SRSimple", opponent.SafetyRatingSimple);
                        SetProp("Relative.Ahead." + count + ".IRChange", opponent.iRatingChange);
                        SetProp("Relative.Ahead." + count + ".PitInfo", opponent.PitInfo);
                        count++;
                    }
                }

                count = 1;
                foreach (var opponent in OpponentsBehind) {
                    if (Settings.RelativeShowCarsInPits || (!Settings.RelativeShowCarsInPits && !opponent._opponent.IsCarInPitLane)) {
                        SetProp("Relative.Behind." + count + ".Position", opponent.PositionString);
                        SetProp("Relative.Behind." + count + ".Name", opponent.DriverName);
                        SetProp("Relative.Behind." + count + ".Show", opponent.DriverName != "");
                        SetProp("Relative.Behind." + count + ".CarNumber", opponent.CarNumber);
                        SetProp("Relative.Behind." + count + ".TrackPct", Math.Abs(opponent.LapDistPctSpectatedCar).ToString("0.0"));

                        SetProp("Relative.Behind." + count + ".Distance", Math.Abs(opponent.LapDistSpectatedCar).ToString("0.0"));
                        SetProp("Relative.Behind." + count + ".Gap", Math.Abs(opponent.GapSpectatedCar).ToString("0.0"));
                        SetProp("Relative.Behind." + count + ".AheadBehind", opponent.AheadBehind.ToString());

                        SetProp("Relative.Behind." + count + ".DriverNameColor", opponent.DriverNameColour);
                        SetProp("Relative.Behind." + count + ".CarClassColor", opponent.CarClassColor);
                        SetProp("Relative.Behind." + count + ".CarClassTextColor", opponent.CarClassTextColor);
                        SetProp("Relative.Behind." + count + ".LicenseColor", opponent.LicenseColor);

                        SetProp("Relative.Behind." + count + ".SR", opponent.SafetyRating);
                        SetProp("Relative.Behind." + count + ".SRSimple", opponent.SafetyRatingSimple);
                        SetProp("Relative.Behind." + count + ".IR", opponent.iRatingString);
                        SetProp("Relative.Behind." + count + ".IRChange", opponent.iRatingChange);
                        SetProp("Relative.Behind." + count + ".PitInfo", opponent.PitInfo);
                        count++;
                    }
                }

                if (SpectatedCar.PositionString != null) {

                    SetProp("Relative.Spectated.DistanceToSC", SpectatedCar.LapDistSafetyCarString);

                    SetProp("Relative.Spectated.Position", SpectatedCar.PositionString);
                    SetProp("Relative.Spectated.Name", SpectatedCar.DriverName);
                    SetProp("Relative.Spectated.Lap", SpectatedCar.Lap);
                    SetProp("Relative.Spectated.Show", SpectatedCar.DriverName != "");
                    SetProp("Relative.Spectated.CarNumber", SpectatedCar.CarNumber);
                    SetProp("Relative.Spectated.PitInfo", SpectatedCar.PitInfo);
                    SetProp("Relative.Spectated.Gap", 0.0);
                    SetProp("Relative.Spectated.AheadBehind", "0");

                    SetProp("Relative.Spectated.SR", SpectatedCar.SafetyRating);
                    SetProp("Relative.Spectated.SRSimple", SpectatedCar.SafetyRatingSimple);
                    SetProp("Relative.Spectated.IR", SpectatedCar.iRatingString);
                    SetProp("Relative.Spectated.IRChange", SpectatedCar.iRatingChange);

                    SetProp("Relative.Spectated.DriverNameColor", SpectatedCar.DriverNameColour);
                    SetProp("Relative.Spectated.CarClassColor", SpectatedCar.CarClassColor);
                    SetProp("Relative.Spectated.CarClassTextColor", SpectatedCar.CarClassTextColor);
                    SetProp("Relative.Spectated.LicenseColor", SpectatedCar.LicenseColor);
                }
            }
        }

        public void AddRelativeProperties() {
            if (Settings.EnableRelatives) {

                AddProp("Debug","");

                for (int i = 1; i < Settings.RelativeNumberOfCarsAheadToShow + 1; i++) {
                    AddProp("Relative.Ahead." + i + ".Position", "");
                    AddProp("Relative.Ahead." + i + ".Name", "");
                    AddProp("Relative.Ahead." + i + ".CarNumber", "");
                    AddProp("Relative.Ahead." + i + ".Distance", "");
                    AddProp("Relative.Ahead." + i + ".Gap", "");
                    AddProp("Relative.Ahead." + i + ".AheadBehind", "");
                    AddProp("Relative.Ahead." + i + ".CarClassTextColor", "");
                    AddProp("Relative.Ahead." + i + ".DriverNameColor", "");
                    AddProp("Relative.Ahead." + i + ".CarClassColor", "");
                    AddProp("Relative.Ahead." + i + ".LicenseColor", "");
                    AddProp("Relative.Ahead." + i + ".Show", "");
                    AddProp("Relative.Ahead." + i + ".SR", "");
                    AddProp("Relative.Ahead." + i + ".SRSimple", "");
                    AddProp("Relative.Ahead." + i + ".IR", "");
                    AddProp("Relative.Ahead." + i + ".IRChange", "");
                    AddProp("Relative.Ahead." + i + ".PitInfo", "");
                }

                for (int i = 1; i < Settings.RelativeNumberOfCarsBehindToShow + 1; i++) {
                    AddProp("Relative.Behind." + i + ".Position", "");
                    AddProp("Relative.Behind." + i + ".Name", "");
                    AddProp("Relative.Behind." + i + ".CarNumber", "");
                    AddProp("Relative.Behind." + i + ".Distance", "");
                    AddProp("Relative.Behind." + i + ".Gap", "");
                    AddProp("Relative.Behind." + i + ".AheadBehind", "");
                    AddProp("Relative.Behind." + i + ".CarClassTextColor", "");
                    AddProp("Relative.Behind." + i + ".DriverNameColor", "");
                    AddProp("Relative.Behind." + i + ".CarClassColor", "");
                    AddProp("Relative.Behind." + i + ".LicenseColor", "");
                    AddProp("Relative.Behind." + i + ".Show", "False");
                    AddProp("Relative.Behind." + i + ".SR", "");
                    AddProp("Relative.Behind." + i + ".SRSimple", "");
                    AddProp("Relative.Behind." + i + ".IR", "");
                    AddProp("Relative.Behind." + i + ".IRChange", "");
                    AddProp("Relative.Behind." + i + ".PitInfo", "");
                }
                AddProp("Relative.Spectated.DistanceToSC", "");
                AddProp("Relative.Spectated.Position", "");
                AddProp("Relative.Spectated.Name", "");
                AddProp("Relative.Spectated.CarNumber", "");
                AddProp("Relative.Spectated.PitInfo", "");
                AddProp("Relative.Spectated.Gap", "0.0");
                AddProp("Relative.Spectated.AheadBehind", "");
                AddProp("Relative.Spectated.CarClassTextColor", "");
                AddProp("Relative.Spectated.Distance", "0.0");
                AddProp("Relative.Spectated.DriverNameColor", "#FFFFFF");
                AddProp("Relative.Spectated.CarClassColor", "#000000");
                AddProp("Relative.Spectated.LicenseColor", "#FFFFFF");

                AddProp("Relative.Spectated.Position", "");
                AddProp("Relative.Spectated.Show", "False");
                AddProp("Relative.Spectated.SR", "");
                AddProp("Relative.Spectated.SRSimple", "");
                AddProp("Relative.Spectated.IR", "");
                AddProp("Relative.Spectated.IRChange", "");
                AddProp("Relative.Spectated.PitInfo", "");

                AddProp("Relative.Layout.FontSize", Settings.RelativeFontSize);
                AddProp("Relative.Layout.RowHeight", Settings.RelativeRowHeight);
                AddProp("Relative.Layout.RowOffset", Settings.RelativeRowOffset);

                AddProp("Relative.Layout.NumberOfCarsAhead", Settings.RelativeNumberOfCarsAheadToShow);
                AddProp("Relative.Layout.NumbersOfCarsBehind", Settings.RelativeNumberOfCarsBehindToShow);

                int totalRowHeight = (Settings.RelativeRowHeight + Settings.RelativeRowOffset);
                int headerHeight = 50;
                int headerTop = 0;
                int aheadTop = (totalRowHeight * Settings.RelativeNumberOfCarsAheadToShow);
                int spectatorTop = aheadTop + totalRowHeight;
                int behindTop = spectatorTop + totalRowHeight;
                int footerHeight = 50;
                int footerTop = behindTop + (totalRowHeight * Settings.RelativeNumberOfCarsAheadToShow);

                int windowHeight = footerTop + footerHeight;

                AddProp("Relative.Layout.HeaderTop", headerTop);
                AddProp("Relative.Layout.HeaderHeight", headerHeight);
                AddProp("Relative.Layout.AheadTop", aheadTop);
                AddProp("Relative.Layout.SpectatorTop", spectatorTop);
                AddProp("Relative.Layout.BehindTop", behindTop);
                AddProp("Relative.Layout.FooterTop", footerTop);
                AddProp("Relative.Layout.FooterHeight", footerHeight);
                AddProp("Relative.Layout.WindowHeight", windowHeight);
            }
        }
        public void ClearRelativeProperties() {

            if (Settings.EnableRelatives) {
                for (int i = 1; i < Settings.RelativeNumberOfCarsAheadToShow + 1; i++) {
                    SetProp("Relative.Ahead." + i + ".Position", "");
                    SetProp("Relative.Ahead." + i + ".Name", "");
                    SetProp("Relative.Ahead." + i + ".CarNumber", "");
                    SetProp("Relative.Ahead." + i + ".Distance", "");
                    SetProp("Relative.Ahead." + i + ".Gap", "");
                    SetProp("Relative.Ahead." + i + ".AheadBehind", "");
                    SetProp("Relative.Ahead." + i + ".CarClassTextColor", "");
                    SetProp("Relative.Ahead." + i + ".DriverNameColor", "");
                    SetProp("Relative.Ahead." + i + ".CarClassColor", "");
                    SetProp("Relative.Ahead." + i + ".LicenseColor", "");
                    SetProp("Relative.Ahead." + i + ".Show", false);
                    SetProp("Relative.Ahead." + i + ".SR", "");
                    SetProp("Relative.Ahead." + i + ".SRSimple", "");
                    SetProp("Relative.Ahead." + i + ".IR", "");
                    SetProp("Relative.Ahead." + i + ".IRChange", "");
                    SetProp("Relative.Ahead." + i + ".PitInfo", "");
                }

                for (int i = 1; i < Settings.RelativeNumberOfCarsBehindToShow + 1; i++) {
                    SetProp("Relative.Behind." + i + ".Position", "");
                    SetProp("Relative.Behind." + i + ".Name", "");
                    SetProp("Relative.Behind." + i + ".CarNumber", "");
                    SetProp("Relative.Behind." + i + ".Distance", "");
                    SetProp("Relative.Behind." + i + ".Gap", "");
                    SetProp("Relative.Behind." + i + ".AheadBehind", "");
                    SetProp("Relative.Behind." + i + ".CarClassTextColor", "");
                    SetProp("Relative.Behind." + i + ".DriverNameColor", "");
                    SetProp("Relative.Behind." + i + ".CarClassColor", "");
                    SetProp("Relative.Behind." + i + ".LicenseColor", "");
                    SetProp("Relative.Behind." + i + ".Show", false);
                    SetProp("Relative.Behind." + i + ".SR", "");
                    SetProp("Relative.Behind." + i + ".SRSimple", "");
                    SetProp("Relative.Behind." + i + ".IR", "");
                    SetProp("Relative.Behind." + i + ".IRChange", "");
                    SetProp("Relative.Behind." + i + ".PitInfo", "");
                }

                SetProp("Relative.Spectated.Position", "");
                SetProp("Relative.Spectated.Name", "");
                SetProp("Relative.Spectated.CarNumber", "");
                SetProp("Relative.Spectated.PitInfo", "");
                SetProp("Relative.Spectated.Gap", "0.0");
                SetProp("Relative.Spectated.AheadBehind", "");
                SetProp("Relative.Spectated.CarClassTextColor", "");
                SetProp("Relative.Spectated.Distance", "0.0");
                SetProp("Relative.Spectated.DriverNameColor", "#FFFFFF");
                SetProp("Relative.Spectated.CarClassColor", "#000000");
                SetProp("Relative.Spectated.LicenseColor", "#FFFFFF");

                SetProp("Relative.Spectated.Position", "");
                SetProp("Relative.Spectated.Show", false);
                SetProp("Relative.Spectated.SR", "");
                SetProp("Relative.Spectated.SRSimple", "");
                SetProp("Relative.Spectated.IR", "");
                SetProp("Relative.Spectated.IRChange", "");
                SetProp("Relative.Spectated.PitInfo", "");

                SetProp("Relative.Layout.NumberOfCarsAhead", Settings.RelativeNumberOfCarsAheadToShow);
                SetProp("Relative.Layout.NumbersOfCarsBehind", Settings.RelativeNumberOfCarsBehindToShow);
            }
        }
    }
}