using GameReaderCommon;
using iRacingSDK;
using SimHub.Plugins;
using SimHub.Plugins.DataPlugins.PersistantTracker;
using SimHub.Plugins.OutputPlugins.Dash.GLCDTemplating;
using SimHub.Plugins.OutputPlugins.GraphicalDash.Behaviors.DoubleText;
using SimHub.Plugins.OutputPlugins.GraphicalDash.Behaviors.DoubleText.Imp;
using SimHub.Plugins.OutputPlugins.GraphicalDash.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media.Animation;
using static SimHub.Plugins.UI.SupportedGamePicker;
using static System.Net.Mime.MediaTypeNames;
using APR.DashSupport.Themes;

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
        private List<ExtendedOpponent> _opponentsInPitlane = new List<ExtendedOpponent>();
        private List<ExtendedOpponent> _opponentsInPitBox = new List<ExtendedOpponent>();

        public List<ExtendedOpponent> OpponentsAhead {
            get {
                // if the distance is negative they are ahead
                return OpponentsExtended.FindAll(a => a.LapDistSpectatedCar < 0 && a.IsConnected).OrderByDescending(a => a.LapDistSpectatedCar).ToList();
            }
            set {
                if (Settings.RelativeShowCarsInPits) {
                    _opponentsAhead = value.FindAll(a => (!a.IsCarInPitLane || !a.IsCarInPitBox || !a.IsCarInGarage));
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
                    _opponentsBehind = value.FindAll(a => (!a.IsCarInPitLane || !a.IsCarInPitBox || !a.IsCarInGarage));
                }
                else {
                    _opponentsBehind = value;
                }
            }
        }

        private double GetReferenceClassLaptime() {
            return GetReferenceClassLaptime(this.SpectatedCar.CarClassID);
        }

        private TimeSpan GetBestClassLaptime(int CarClassID) {
            TimeSpan BestLapTime = TimeSpan.MaxValue;
            foreach (var item in this.OpponentsInClass(CarClassID)) {
                if (item.BestLapTime < BestLapTime) {
                    BestLapTime = item.BestLapTime;
                }
                
            }
            return BestLapTime;
        }

        private TimeSpan GetBestLaptime() {
            TimeSpan BestLapTime = TimeSpan.MaxValue;
            foreach (var item in OpponentsExtended) {
                if (item.BestLapTime < BestLapTime) {
                    BestLapTime = item.BestLapTime;
                }
            }
            return BestLapTime;
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
                StrategyBundle StrategicObserer = StrategyBundle.Instance;

                this.opponents = data.NewData.Opponents;

                SessionData._DriverInfo._Drivers[] competitors = irData.SessionData.DriverInfo.CompetingDrivers;
                this.opponents = data.NewData.Opponents;
                this.OpponentsExtended = new List<ExtendedOpponent>();

                // Get the Spectated car info
                StrategicObserer.CameraCarIdx = irData.Telemetry.CamCarIdx;
                StrategicObserer.CameraCarLapDistPct = irData.Telemetry.CarIdxLapDistPct[StrategicObserer.CameraCarIdx];
                StrategicObserer.CameraCarCurrentLap = irData.Telemetry.CarIdxLap[StrategicObserer.CameraCarIdx];

                // TODO: make LastLapIsPersonalBest work

                // NewRawData().Telemetry["CarIdxBestLapTime"]
                //
                //NewRawData().Telemetry["CarIdxLastLapTime"]
                for (int i = 0; i < competitors.Length; ++i) {
                    for (int j = 0; j < opponents.Count; ++j) {
                        // Add the aligned Opponents and Competitor data to our ExtendedOpponent list
                        if (string.Equals(competitors[i].CarNumber, opponents[j].CarNumber)) {

                            float[] bestLapTimes = (float[])irData.Telemetry.FirstOrDefault(x => x.Key == "CarIdxBestLapTime").Value;
                            float[] lastLapTimes = (float[])irData.Telemetry.FirstOrDefault(x => x.Key == "CarIdxLastLapTime").Value;

                            // Add to the Extended Opponents class
                            var driver = new ExtendedOpponent() {
                                _opponent = opponents[j],
                                _competitor = competitors[i],
                                _carEstTime = irData.Telemetry.CarIdxEstTime[competitors[i].CarIdx],
                                _carBestLapTime = bestLapTimes[competitors[i].CarIdx],
                                _carLastLapTime = lastLapTimes[competitors[i].CarIdx],
                                _trackSurface = (int)irData.Telemetry.CarIdxTrackSurface[competitors[i].CarIdx],
                                LicenseColor = LicenseColor(opponents[j].LicenceString),
                                LicenseTextColor = LicenseTextColor(opponents[j].LicenceString),
                                LicenseBorderColor = LicenseBorderColor(opponents[j].LicenceString),
                            };
                            
                            driver.CalculatePitInfo(SessionTime);

                            OpponentsExtended.Add(driver);

                            // Update the car class info
                            CheckAndAddCarClass((int)competitors[i].CarClassID, competitors[i].CarClassShortName);

                        }
                    }
                }

                // Create the spectator
                ExtendedOpponent spectator = OpponentsExtended.Find(a => a.CarIdx == StrategicObserer.CameraCarIdx);

                // update car reference lap time
                foreach (var item in OpponentsExtended) {
                    item.CarClassReferenceLapTime = GetReferenceClassLaptime(item.CarClassID);
                }

                // Get the opponents in pitlane
                _opponentsInPitlane = OpponentsExtended.FindAll(a => a.IsCarInPitLane == true);
                _opponentsInPitBox = OpponentsExtended.FindAll(a => a.IsCarInPitBox == true);


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
                        double referenceLapTime = GetReferenceClassLaptime(item.carClassID);
                        item.ReferenceLapTime = referenceLapTime;
                        List<ExtendedOpponent> classbyPosition = OpponentsExtended.FindAll(a => a.CarClassID == item.carClassID).OrderBy(a => a.Position).ToList();
                       
                        ExtendedOpponent classLeader = classbyPosition[0];
                        item.LeaderTotalTime = (classLeader.Lap * referenceLapTime) + (referenceLapTime * classLeader.TrackPositionPercent);
                    }

                    // update car reference lap time
                    foreach (var item in OpponentsExtended) {
                        item.CarClassReferenceLapTime = GetReferenceClassLaptime(item.CarClassID);
                    }

  
                    // Find the best lap time for each class
                    foreach (var item in carClasses) {
                        TimeSpan bestLapTime = GetBestClassLaptime(item.carClassID);
                        item.BestLapTime = bestLapTime;
                    }

                    // set the lap time flags
                    foreach (var item in OpponentsExtended)
                    {
                        // Set the IsBestLapClassBestLap the OpponentsExtended
                        if (item.BestLapTime == carClasses.Find(x => x.carClassID == item.CarClassID).BestLapTime) {
                            if(item.BestLapTime > TimeSpan.Zero) {
                                item.IsBestLapClassBestLap = true;
                            }
                            else {
                                item.IsBestLapClassBestLap = false;
                            }
                            
                        }
                        else {
                            item.IsBestLapClassBestLap = false;
                        }

                        // Set the IsBestLapOverallBest the OpponentsExtended
                        if (item.BestLapTime == GetBestLaptime()) {
                            if (item.BestLapTime > TimeSpan.Zero) {
                                item.IsBestLapOverallBest = true;
                            }
                            else {
                                item.IsBestLapOverallBest = false;
                            }
                        }
                        else {
                            item.IsBestLapOverallBest = false;
                        }

                        if (item.LastLapTime == item.BestLapTime) {
                            if (item.LastLapTime > TimeSpan.Zero) {
                                item.IsLastLapPersonalBest = true;
                            }
                            else {
                                item.IsLastLapPersonalBest = false;
                            }
                        }
                        else {
                            item.IsLastLapPersonalBest = false;
                        }
                    }

                    // Add the car class leader to the OpponentsExtended
                    var sortedOpponents = OpponentsExtended.OrderBy(a => a.Position).ToList();
                    if (sortedOpponents.Count > 0) {
                        int overallLeaderIdx = sortedOpponents[0].CarIdx;
                        double overallLeaderReferenceTime = carClasses.Find(x => x.carClassID == sortedOpponents[0].CarClassID).ReferenceLapTime;
                        double overallLeaderTotalTime =  (sortedOpponents[0].Lap * overallLeaderReferenceTime) + (overallLeaderReferenceTime * sortedOpponents[0].TrackPositionPercent);

                        // Car Ahead
                        for (int i = 0; i < sortedOpponents.Count; i++) {
                            var item = sortedOpponents[i];

                            // find the class leader  
                            CarClass cc = carClasses.Find(x => x.carClassID == item.CarClassID);
                            item._classleaderCarIdx = ClassLeadingCar(item.CarClassID).CarIdx;

                            // update the gaps to the leaders
                            double carTotalTime = (item.Lap * cc.ReferenceLapTime) + (cc.ReferenceLapTime * item.TrackPositionPercent);
                           
                            // Get the gap to the overall leader and push it in the copy and master
                            double gap = overallLeaderTotalTime - carTotalTime;
                            item.GapToOverallLeader = gap;
                            OpponentsExtended.Find(x => x.CarIdx == item.CarIdx).GapToOverallLeader = gap;

                            // Get the gap to the class leader and push it in the copy and master
                            gap = cc.LeaderTotalTime - carTotalTime;
                            item.GapToClassLeader = gap;
                            OpponentsExtended.Find(x => x.CarIdx == item.CarIdx).GapToClassLeader = gap;

                            // Get class position in class ahead
                            if (item.PositionInClass <= 1) {
                                OpponentsExtended.Find(x => x.CarIdx == item.CarIdx).CarAheadInClassGap = 0;
                            }
                            else {

                                var carAhead = sortedOpponents.Find(x => x.PositionInClass == sortedOpponents[i].PositionInClass - 1);
                                if (carAhead != null) {

                                    gap = item.GapToClassLeader - carAhead.GapToClassLeader;
                                    OpponentsExtended.Find(x => x.CarIdx == item.CarIdx).CarInClassAhead = carAhead;
                                    OpponentsExtended.Find(x => x.CarIdx == item.CarIdx).CarAheadInClassGap = gap;

                                }
                                else {
                                    OpponentsExtended.Find(x => x.CarIdx == item.CarIdx).CarAheadInClassGap = 0;
                                }
                            }
                        }

                        // Car behind
                        for (int i = 0; i < sortedOpponents.Count; i++) {
                            var item = sortedOpponents[i];

                            // find the class leader  
                            CarClass cc = carClasses.Find(x => x.carClassID == item.CarClassID);
                            item._classleaderCarIdx = ClassLeadingCar(item.CarClassID).CarIdx;

                            // update the gaps to the leaders
                            double carTotalTime = (item.Lap * cc.ReferenceLapTime) + (cc.ReferenceLapTime * item.TrackPositionPercent);
                            item.GapToOverallLeader = overallLeaderTotalTime - carTotalTime;
                            item.GapToClassLeader = cc.LeaderTotalTime - carTotalTime;

                            var carBehind = sortedOpponents.Find(x => x.PositionInClass == sortedOpponents[i].PositionInClass + 1 && i + 1 < sortedOpponents.Count);

                            if (carBehind != null) {

                                double gap = carBehind.GapToClassLeader - item.GapToClassLeader;
                                OpponentsExtended.Find(x => x.CarIdx == item.CarIdx).CarInClassBehind = carBehind;
                                OpponentsExtended.Find(x => x.CarIdx == item.CarIdx).CarBehindInClassGap = gap;

                            }
                            else {
                                OpponentsExtended.Find(x => x.CarIdx == item.CarIdx).CarBehindInClassGap = 0;
                            }
                          
                            if (item.Position <= 1) {
                                OpponentsExtended.Find(x => x.CarIdx == item.CarIdx).CarAheadOverallGap = 0;
                            }
                            else {
                                if (sortedOpponents.Count > i+1) {
                                    carBehind =  sortedOpponents[i + 1];
                                }

                                if (carBehind != null) {
                                    double gap =  carBehind.GapToOverallLeader - item.GapToOverallLeader;
                                    OpponentsExtended.Find(x => x.CarIdx == item.CarIdx).CarBehindOverallGap = gap;
                                }
                                else {
                                    OpponentsExtended.Find(x => x.CarIdx == item.CarIdx).CarBehindOverallGap = 0;
                                }
                            }
                        }
                    }
                }

                // Grab the slow poke
                var _slowOpponent = OpponentsAhead.Find(x => x.IsSlow) ?? null;
                if (_slowOpponent != null && spectator.Speed > 40) {
                    StrategicObserer.SlowOpponentIdx = _slowOpponent.CarIdx;
                    StrategicObserer.SlowOpponentLapDistPct = _slowOpponent.TrackPositionPercent;
                }
      
                UpdateRelativeProperties();
                UpdateStandingsRelatedProperties(ref data);

                // Update current lap information
                UpdateLapOrTimeString(data);
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
                        SetProp("Relative.Ahead." + count + ".LapAheadBehind", opponent.LapAheadBehind.ToString());
                        SetProp("Relative.Ahead." + count + ".DriverNameColor", opponent.DriverNameColour);
                        SetProp("Relative.Ahead." + count + ".CarClassColor", opponent.CarClassColor);
                        SetProp("Relative.Ahead." + count + ".CarClassColorSemiTransparent", opponent.CarClassColorSemiTransparent);
                        SetProp("Relative.Ahead." + count + ".CarClassTextColor", opponent.CarClassTextColor);
                        SetProp("Relative.Ahead." + count + ".LicenseColor", opponent.LicenseColor);
                        SetProp("Relative.Ahead." + count + ".LicenseTextColor", opponent.LicenseTextColor);
                        SetProp("Relative.Ahead." + count + ".LicenseBorderColor", opponent.LicenseBorderColor);

                        SetProp("Relative.Ahead." + count + ".SR", opponent.SafetyRating);
                        SetProp("Relative.Ahead." + count + ".IR", opponent.iRatingString);
                        SetProp("Relative.Ahead." + count + ".SRSimple", opponent.SafetyRatingSimple);
                        SetProp("Relative.Ahead." + count + ".IRChange", opponent.iRatingChange);
                        SetProp("Relative.Ahead." + count + ".PitInfo", opponent.PitStatusString);
                        SetProp("Relative.Ahead." + count + ".IsSlow", opponent.IsSlow);

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
                        SetProp("Relative.Behind." + count + ".LapAheadBehind", opponent.LapAheadBehind.ToString());

                        SetProp("Relative.Behind." + count + ".DriverNameColor", opponent.DriverNameColour);
                        SetProp("Relative.Behind." + count + ".CarClassColor", opponent.CarClassColor);
                        SetProp("Relative.Behind." + count + ".CarClassColorSemiTransparent", opponent.CarClassColorSemiTransparent);
                        SetProp("Relative.Behind." + count + ".CarClassTextColor", opponent.CarClassTextColor);
                        SetProp("Relative.Behind." + count + ".LicenseColor", opponent.LicenseColor);
                        SetProp("Relative.Behind." + count + ".LicenseTextColor", opponent.LicenseTextColor);
                        SetProp("Relative.Behind." + count + ".LicenseBorderColor", opponent.LicenseBorderColor);

                        SetProp("Relative.Behind." + count + ".SR", opponent.SafetyRating);
                        SetProp("Relative.Behind." + count + ".SRSimple", opponent.SafetyRatingSimple);
                        SetProp("Relative.Behind." + count + ".IR", opponent.iRatingString);
                        SetProp("Relative.Behind." + count + ".IRChange", opponent.iRatingChange);
                        SetProp("Relative.Behind." + count + ".PitInfo", opponent.PitStatusString);
                        SetProp("Relative.Behind." + count + ".IsSlow", opponent.IsSlow);

                        count++;
                    }
                }

                if (! String.IsNullOrEmpty(SpectatedCar.PositionString) ) {

                    SetProp("Spectated.DistanceToSC", SpectatedCar.LapDistSafetyCarString);
                    SetProp("Spectated.Position", SpectatedCar.PositionString);
                    SetProp("Spectated.Name", SpectatedCar.DriverName);
                    SetProp("Spectated.Lap", SpectatedCar.Lap);
                    SetProp("Spectated.Speed", SpectatedCar._opponent.Speed);
                    SetProp("Spectated.IsSlowCarAhead", SpectatedCar.IsSlowCarAhead);
                    SetProp("Spectated.SlowCarAheadString", SpectatedCar.LapDistanceSlowCarAheadString);

                    double SessionBestLiveDeltaSeconds = 0;
                    if (GetProp("PersistantTrackerPlugin.SessionBestLiveDeltaSeconds") != null) {
                        SessionBestLiveDeltaSeconds = (double)GetProp("PersistantTrackerPlugin.SessionBestLiveDeltaSeconds");
                    }

                    double AllTimeBestLastLapDelta = 0;
                    if (GetProp("PersistantTrackerPlugin.AllTimeBestLastLapDelta") != null) {
                        AllTimeBestLastLapDelta = (double)GetProp("PersistantTrackerPlugin.AllTimeBestLastLapDelta");
                    }
                    /*
                    double AllTimeBestLastLapDelta = 0;
                    if (GetProp("PersistantTrackerPlugin.AllTimeBestLastLapDelta") != null) {
                        AllTimeBestLastLapDelta = (double)GetProp("PersistantTrackerPlugin.AllTimeBestLastLapDelta");
                    }

                    PersistantTrackerPlugin.SessionBestLastLapDelta

                    PersistantTrackerPlugin.SessionBestLiveDeltaProgressSeconds
                    PersistantTrackerPlugin.SessionBestLiveDeltaSeconds
                    PersistantTrackerPlugin.EstimatedLapTime_SessionBestBasedSimhub_EstimatedDelta
                    PersistantTrackerPlugin.AllTimeBestLiveDeltaProgressSeconds
                    PersistantTrackerPlugin.AllTimeBestLiveDeltaSeconds
                    PersistantTrackerPlugin.AllTimeBestLastLapDelta

                    IRacingExtraProperties.SpectatedCar_DeltaToBest
                    //PersistantTrackerPlugin.SessionBestLiveDeltaSeconds
                    */

                    SetProp("Spectated.Delta.Mode", "Personal Session Best");
                    SetProp("Spectated.Delta.Mode", SessionBestLiveDeltaSeconds);

                    SetProp("Spectated.Delta.PersonalSessionBest", SessionBestLiveDeltaSeconds);
                    SetProp("Spectated.Delta.AllTimeBest", AllTimeBestLastLapDelta);


                    // Future properties
                    
                    SetProp("Spectated.Delta.OverallSessionBest", "");
                    SetProp("Spectated.Delta.Leader.LastLap", "");
                    SetProp("Spectated.Delta.CarAhead.LastLap", "");
                    SetProp("Spectated.LiveDelta.PersonalBest", "");
                    SetProp("Spectated.LiveDelta.SessionBest", "");
                    SetProp("Spectated.LiveDelta.Leader", "");
                    SetProp("Spectated.Show", SpectatedCar.DriverName != "");
                    SetProp("Spectated.CarNumber", SpectatedCar.CarNumber);

                    SetProp("Spectated.PitInfo", SpectatedCar.PitStatusString);
                    SetProp("Spectated.IsInPitLane", SpectatedCar.IsCarInPitLane);
                    SetProp("Spectated.IsInPit", SpectatedCar.IsCarInPitBox);
                    SetProp("Spectated.IsConnected", SpectatedCar.IsConnected);
                    SetProp("Spectated.IsOffTrack", SpectatedCar.IsOffTrack);
                    SetProp("Spectated.IsOnTrack", SpectatedCar.IsOnTrack);


                    SetProp("Spectated.Gap", 0.0);
                    SetProp("Spectated.AheadBehind", "0");

                    SetProp("Spectated.SR", SpectatedCar.SafetyRating);
                    SetProp("Spectated.SRSimple", SpectatedCar.SafetyRatingSimple);
                    SetProp("Spectated.IR", SpectatedCar.iRatingString);
                    SetProp("Spectated.IRChange", SpectatedCar.iRatingChange);

                    SetProp("Spectated.DriverNameColor", SpectatedCar.DriverNameColour);
                    SetProp("Spectated.CarClassColor", SpectatedCar.CarClassColor);
                    SetProp("Spectated.CarClassTextColor", SpectatedCar.CarClassTextColor);
                    SetProp("Spectated.LicenseColor", SpectatedCar.LicenseColor);
                    SetProp("Spectated.LicenseTextColor", SpectatedCar.LicenseTextColor);
                    SetProp("Spectated.LicenseBorderColor", SpectatedCar.LicenseBorderColor);

                    SetProp("Specteted.IsPlayer", SpectatedCar.IsPlayer);

                    SetProp("Spectated.LastLap", SpectatedCar.LastLapTimeString);
                    SetProp("Spectated.BestLap", SpectatedCar.BestLapTimeString);

                    SetProp("Spectated.CarAhead.LastLapDelta", SpectatedCar.CarAheadInClassLastLapDeltaString);
                    SetProp("Spectated.CarBehind.LastLapDelta", SpectatedCar.CarBehindInClassLastLapDeltaString);
                    SetProp("Spectated.CarAhead.BestLapDelta", SpectatedCar.CarAheadInClassBestLapDelta);
                    SetProp("Spectated.CarBehind.BestLapDelta", SpectatedCar.CarBehindInClassBestLapDelta);

                    SetProp("Spectated.CarAhead.Pos", SpectatedCar.CarAheadPositionInClass);
                    SetProp("Spectated.CarBehind.Pos", SpectatedCar.CarBehindPositionInClass);
                    SetProp("Spectated.CarAhead.Name", SpectatedCar.CarAheadInClassDriverName);
                    SetProp("Spectated.CarBehind.Name", SpectatedCar.CarBehindInClassDriverName);

                    SetProp("Spectated.CarAhead.Gap", SpectatedCar.ClassAheadInClassGapString);
                    SetProp("Spectated.CarBehind.Gap", SpectatedCar.CarBehindInClassGapString);

                    SetProp("Spectated.Lap.Colors.LastLap", SpectatedCar.LastLapDynamicColor);
                    SetProp("Spectated.Lap.Colors.BestLap", SpectatedCar.BestLapDynamicColor);
                    SetProp("Spectated.LastLapIsPersonalBestLap", SpectatedCar.IsLastLapPersonalBest);
                    SetProp("Spectated.BestLapIsClassBestLap", SpectatedCar.IsBestLapClassBestLap);
                    SetProp("Spectated.BestLapIsOverallBestLap", SpectatedCar.IsBestLapOverallBest);

                    SetProp("Spectated.PitStops.CPS1Served", SpectatedCar.PitStops_CPS1Served);
                    SetProp("Spectated.PitStops.CPS2Served", SpectatedCar.PitStops_CPS2Served);
                    SetProp("Spectated.PitStops.CPS1IndicatorColor", SpectatedCar.PitStops_CPS1IndicatorColor);
                    SetProp("Spectated.PitStops.CPS2IndicatorColor", SpectatedCar.PitStops_CPS2IndicatorColor);
                    SetProp("Spectated.PitStops.PitStatusColor", SpectatedCar.PitStops_PitStatusColor);

                    SetProp("Spectated.PitStops.NumberOfCPSStops", SpectatedCar.PitStops_NumberOfCPSStops);
                    SetProp("Spectated.PitStops.NumberOfStops", SpectatedCar.PitStops_NumberOfStops);
                    SetProp("Spectated.PitStops.EstimatedNextStop", SpectatedCar.PitStops_EstimatedNextStop);
                    SetProp("Spectated.PitStops.LastStopEstimatedRange", SpectatedCar.PitStops_LastStopEstimatedRange);
                    SetProp("Spectated.PitStops.NumberOfStops", SpectatedCar.PitStops_NumberOfStops);

                    SetProp("Spectated.PitStops.Delimited.LastStopLap", SpectatedCar.PitStops_AllStopsLapDelimitedString);
                    SetProp("Spectated.PitStops.Delimited.LastStopLapPct", SpectatedCar.PitStops_AllStopsLastDelimitedStringPct);
                    SetProp("Spectated.PitStops.Delimited.EstimatedRange", SpectatedCar.PitStops_AllStopsEstimatedRangeDelimitedString);
                    SetProp("Spectated.PitStops.Delimited.EstimatedRangePct", SpectatedCar.PitStops_AllStopsEstimatedRangeDelimitedStringPct);
                    

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
                    AddProp("Relative.Ahead." + i + ".CarClassColorSemiTransparent", "");
                    AddProp("Relative.Ahead." + i + ".DriverNameColor", "");
                    AddProp("Relative.Ahead." + i + ".CarClassColor", "");
                    AddProp("Relative.Ahead." + i + ".LicenseColor", "");
                    AddProp("Relative.Ahead." + i + ".LicenseTextColor", "");
                    AddProp("Relative.Ahead." + i + ".LicenseBorderColor", "");
                    AddProp("Relative.Ahead." + i + ".Show", "");
                    AddProp("Relative.Ahead." + i + ".SR", "");
                    AddProp("Relative.Ahead." + i + ".SRSimple", "");
                    AddProp("Relative.Ahead." + i + ".IR", "");
                    AddProp("Relative.Ahead." + i + ".IRChange", "");
                    AddProp("Relative.Ahead." + i + ".PitInfo", "");
                    AddProp("Relative.Ahead." + i + ".IsSlow", "");
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
                    AddProp("Relative.Behind." + i + ".CarClassColorSemiTransparent", "");
                    AddProp("Relative.Behind." + i + ".LicenseColor", "");
                    AddProp("Relative.Behind." + i + ".LicenseTextColor", "");
                    AddProp("Relative.Behind." + i + ".LicenseBorderColor", "");

                    AddProp("Relative.Behind." + i + ".Show", "False");
                    AddProp("Relative.Behind." + i + ".SR", "");
                    AddProp("Relative.Behind." + i + ".SRSimple", "");
                    AddProp("Relative.Behind." + i + ".IR", "");
                    AddProp("Relative.Behind." + i + ".IRChange", "");
                    AddProp("Relative.Behind." + i + ".PitInfo", "");
                    AddProp("Relative.Behind." + i + ".IsSlow", "");
                }

                AddProp("Relative.Layout.FontSize", Settings.RelativeFontSize);
                AddProp("Relative.Layout.RowHeight", Settings.RelativeRowHeight);
                AddProp("Relative.Layout.RowOffset", Settings.RelativeRowOffset);

                AddProp("Relative.Layout.NumberOfCarsAhead", Settings.RelativeNumberOfCarsAheadToShow);
                AddProp("Relative.Layout.NumbersOfCarsBehind", Settings.RelativeNumberOfCarsBehindToShow);

                AddProp("Spectated.DistanceToSC", "");
                AddProp("Spectated.Position", "");
                AddProp("Spectated.Speed", "");
                AddProp("Spectated.Delta.PersonalSessionBest", "");
                AddProp("Spectated.Delta.OverallSessionBest", "");
                AddProp("Spectated.Delta.Leader.LastLap", "");
                AddProp("Spectated.Delta.CarAhead.LastLap", "");
                AddProp("Spectated.LiveDelta.PersonalBest", "");
                AddProp("Spectated.LiveDelta.SessionBest", "");
                AddProp("Spectated.LiveDelta.Leader", "");
                AddProp("Spectated.IsSlow", "");
                AddProp("Spectated.IsSlowCarAhead", "");
                AddProp("Spectated.SlowCarAheadString", "");

                AddProp("Spectated.Name", "");
                AddProp("Spectated.CarNumber", "");
                AddProp("Spectated.PitInfo", "");
                AddProp("Spectated.Gap", "0.0");
                AddProp("Spectated.AheadBehind", "");
                AddProp("Spectated.CarClassTextColor", "");
                AddProp("Spectated.Distance", "0.0");
                AddProp("Spectated.DriverNameColor", IRacing.Colors.White);
                AddProp("Spectated.CarClassColor", IRacing.Colors.White);
                AddProp("Spectated.CarClassColorTransparent", "#96FFFFFF");

                AddProp("Spectated.LicenseColor", IRacing.Colors.Transparent);
                AddProp("Spectated.LicenseTextColor", IRacing.Colors.Transparent);
                AddProp("Spectated.LicenseBorderColor", IRacing.Colors.Transparent);
                AddProp("Specteted.IsPlayer", true);

                AddProp("Spectated.IsInPitLane", false);
                AddProp("Spectated.IsInPit", false);
                AddProp("Spectated.IsConnected", false);
                AddProp("Spectated.IsOffTrack", false);
                AddProp("Spectated.IsOnTrack", false);

                AddProp("Spectated.LastLap", "-.---");
                AddProp("Spectated.BestLap", "-.---");

                AddProp("Spectated.Position", "");
                AddProp("Spectated.Show", "False");
                AddProp("Spectated.SR", "");
                AddProp("Spectated.SRSimple", "");
                AddProp("Spectated.IR", "");
                AddProp("Spectated.IRChange", "");
                AddProp("Spectated.PitInfo", "");

                AddProp("Spectated.CarAhead.LastLapDelta", "");
                AddProp("Spectated.CarBehind.LastLapDelta", "");

                AddProp("Spectated.CarAhead.Pos", "");
                AddProp("Spectated.CarBehind.Pos", "");

                AddProp("Spectated.CarAhead.Name", "");
                AddProp("Spectated.CarBehind.Name", "");

                AddProp("Spectated.CarAhead.Gap","");
                AddProp("Spectated.CarBehind.Gap","");

                AddProp("Spectated.Lap.Colors.LastLap",IRacing.Colors.GreyLightText);
                AddProp("Spectated.Lap.Colors.BestLap", IRacing.Colors.GreyLightText);
                AddProp("Spectated.LastLapIsPersonalBestLap","");
                AddProp("Spectated.BestLapIsClassBestLap", "");
                AddProp("Spectated.BestLapIsOverallBestLap", "");

                AddProp("Spectated.PitStops.CPS1Served", "");
                AddProp("Spectated.PitStops.CPS2Served", "");
                AddProp("Spectated.PitStops.NumberOfCPSStops", "");
                AddProp("Spectated.PitStops.NumberOfStops", "");
                AddProp("Spectated.PitStops.EstimatedNextStop", "");
                AddProp("Spectated.PitStops.LastStopEstimatedRange", "");
                AddProp("Spectated.PitStops.NumberOfStops", "");

                AddProp("Spectated.PitStops.Delimited.LastStopLap","" );
                AddProp("Spectated.PitStops.Delimited.LastStopLapPct", "");
                AddProp("Spectated.PitStops.Delimited.EstimatedRange", "");
                AddProp("Spectated.PitStops.Delimited.EstimatedRangePct", "");


                int totalRowHeight = (Settings.RelativeRowHeight + Settings.RelativeRowOffset );
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
                    SetProp("Relative.Ahead." + i + ".LicenseTextColor", "");
                    SetProp("Relative.Ahead." + i + ".LicenseBorderColor", "");
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
                    SetProp("Relative.Behind." + i + ".LicenseTextColor", "");
                    SetProp("Relative.Behind." + i + ".LicenseBorderColor", "");
                    SetProp("Relative.Behind." + i + ".Show", false);
                    SetProp("Relative.Behind." + i + ".SR", "");
                    SetProp("Relative.Behind." + i + ".SRSimple", "");
                    SetProp("Relative.Behind." + i + ".IR", "");
                    SetProp("Relative.Behind." + i + ".IRChange", "");
                    SetProp("Relative.Behind." + i + ".PitInfo", "");
                }

                SetProp("Spectated.Position", "");
                SetProp("Spectated.Name", "");
                SetProp("Spectated.CarNumber", "");
                SetProp("Spectated.PitInfo", "");
                SetProp("Spectated.Gap", "0.0");
                SetProp("Spectated.AheadBehind", "");
                SetProp("Spectated.CarClassTextColor", "");
                SetProp("Spectated.Distance", "0.0");
                SetProp("Spectated.DriverNameColor", IRacing.Colors.YellowText);
                if (SpectatedCar != null) {
                    SetProp("Spectated.CarClassColor", SpectatedCar.CarClassColor);
                    SetProp("Spectated.CarClassColorTransparent", SpectatedCar.CarClassColorSemiTransparent);
                }
                SetProp("Spectated.LicenseColor", IRacing.Colors.Transparent);
                SetProp("Spectated.LicenseTextColor", IRacing.Colors.Transparent);
                SetProp("Spectated.LicenseBorderColor", IRacing.Colors.Transparent);

                SetProp("Spectated.Position", "");
                SetProp("Spectated.Show", false);
                SetProp("Spectated.SR", "");
                SetProp("Spectated.SRSimple", "");
                SetProp("Spectated.IR", "");
                SetProp("Spectated.IRChange", "");
                SetProp("Spectated.PitInfo", "");

                SetProp("Spectated.PitStops.CPS1Served", "");
                SetProp("Spectated.PitStops.CPS2Served", "");
                SetProp("Spectated.PitStops.NumberOfCPSStops", "");
                SetProp("Spectated.PitStops.NumberOfStops", "");
                SetProp("Spectated.PitStops.EstimatedNextStop", "");
                SetProp("Spectated.PitStops.LastStopEstimatedRange", "");
                SetProp("Spectated.PitStops.NumberOfStops", "");

                SetProp("Relative.Layout.NumberOfCarsAhead", Settings.RelativeNumberOfCarsAheadToShow);
                SetProp("Relative.Layout.NumbersOfCarsBehind", Settings.RelativeNumberOfCarsBehindToShow);
            }
        }

        public void ResetRelativeAndStandingsData() {
            OpponentsExtended = new List<ExtendedOpponent>();
            carClasses = new List<CarClass> { };
        }

    }
}