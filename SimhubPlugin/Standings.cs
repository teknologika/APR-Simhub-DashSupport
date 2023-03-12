using FMOD;
using GameReaderCommon;
using IRacingReader;
using iRacingSDK;
using MahApps.Metro.Controls;
using SimHub.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using Opponent = GameReaderCommon.Opponent;

namespace APR.DashSupport {

    public partial class APRDashPlugin : IPlugin, IDataPlugin, IWPFSettingsV2 {
        static int iRacingMaxCars = 63;
        static int iRacingMaxClasses = 5;

        public List<RaceCar> CompetingCars = new List<RaceCar>();
        public List<RaceCar> OpponentCars = new List<RaceCar>();
        public int NumberOfCarsInSession = 0;
        public List<CarClass> CarClasses = new List<CarClass>();
        public int LeaderIdx = 0;
        public List<int> ClassLeaderIdx = new List<int>();
        public double LeaderLastLap { get; set; } = 0;
        public double LeaderExpectedLapTime { get; set; } = 0;
        public double LeaderBestLap { get; set; } = 0;
        public int LeaderCurrentLap { get; set; } = 0;
        public double LeaderLapDistancePercent { get; set; } = 0;
        public double SessionBestLapTime { get; set; } = 0;

        //public List<double> iRSectorStartDistancePercent { get; set; } = 0;
        //irData.SessionData.SplitTimeInfo.Sectors[3].SectorNum;
        //irData.SessionData.SplitTimeInfo.Sectors[3].SectorStartPct;
        public RaceCar GetCompositeCarForIDx(int id, ref GameData data) {
            SessionData._DriverInfo._Drivers[] competitiors = irData.SessionData.DriverInfo.CompetingDrivers;

            // Check the id is not too big
            if (id > competitiors.Length) {
                return null;
            }

            // get the competitor
            SessionData._DriverInfo._Drivers competitor = competitiors[id];
            if (competitor.UserName == string.Empty) {
                return null;
            }

            RaceCar car = new RaceCar {

                CarIDx = (int)competitor.CarIdx,
                CarClass = competitor.CarClassID,
                CarNumber = competitor.CarNumber,

                // Get Position
                Position = irData.Telemetry.CarIdxPosition[competitor.CarIdx],
                EstimatedLapTime = irData.Telemetry.CarIdxEstTime[competitor.CarIdx],
                LapDistancePercent = irData.Telemetry.CarIdxLapDistPct[competitor.CarIdx],
                Lap = irData.Telemetry.CarIdxLap[competitor.CarIdx],

            };

            car.LapsBehindLeader = LeaderCurrentLap - car.Lap;

            car.Driver.DriverFullName = competitor.UserName;
            car.Driver.DriverCustomerID = competitor.UserID;


            car.Driver.DriverIRating = competitor.IRating;
            car.Driver.DriverSafetyRating = competitor.LicString;
            car.Driver.DriverLicenseLevel = competitor.LicLevel;
            car.Driver.Nationality = competitor.ClubName;

            // chop up the drivers name(s)
            // TODO: Don't store everything, just chop once and store what we need ... maybe
            string[] names = car.Driver.DriverFullName.Split(' ');
            int numberOfNames = names.Length;
            if (numberOfNames > 0) {

                car.Driver.DriverFirstName = car.Driver.DriverFullName.Split(' ')[0];
                car.Driver.DriverLastName = car.Driver.DriverFullName.Split(' ')[numberOfNames - 1];

                if (car.Driver.DriverFirstName.Length > 3) {
                    car.Driver.DriverFirstNameShort = car.Driver.DriverFirstName.Substring(0, 3).ToUpper();
                }
                else {
                    car.Driver.DriverFirstNameShort = car.Driver.DriverFirstName;
                }

                if (car.Driver.DriverLastName.Length > 3) {
                    car.Driver.DriverLastNameShort = car.Driver.DriverLastName.Substring(0, 3).ToUpper();
                }
                else {
                    car.Driver.DriverLastNameShort = car.Driver.DriverLastName;
                }


                car.Driver.DriverFirstNameInitial = car.Driver.DriverFirstName.Substring(0, 1).ToUpper();
                car.Driver.DriverLastNameInitial = car.Driver.DriverLastName.Substring(0, 1).ToUpper();

                UpdateStandingsNameSetting();

                // Update the driver's display name
                switch (Settings.DriverNameStyle) {

                    case 0: // Firstname Middle Lastname
                        car.Driver.DriverDisplayName = car.Driver.DriverFullName;
                        break;

                    case 1: // Firstname Lastname
                        car.Driver.DriverDisplayName = car.Driver.DriverFirstName + " " + car.Driver.DriverLastName;
                        break;

                    case 2: // Lastname, Firstname
                        car.Driver.DriverDisplayName = car.Driver.DriverLastName + ", " + car.Driver.DriverFirstName;
                        break;

                    case 3: // F. Lastname
                        car.Driver.DriverDisplayName = car.Driver.DriverFirstNameInitial + ". " + car.Driver.DriverLastName;
                        break;

                    case 4: // Firstname L.
                        car.Driver.DriverDisplayName = car.Driver.DriverFirstName + " " + car.Driver.DriverLastNameInitial + ". ";
                        break;

                    case 5: // Lastname, F.
                        car.Driver.DriverDisplayName = car.Driver.DriverLastName + ", " + car.Driver.DriverFirstNameInitial + ". ";
                        break;

                    case 6: // LAS
                        car.Driver.DriverDisplayName = car.Driver.DriverLastNameShort;
                        break;

                    default: //   Firstname Middle Lastname

                        car.Driver.DriverDisplayName = car.Driver.DriverFullName;
                        break;
                }
            }


            bool setLastLap = false;
            bool setBestLap = false;

            // try with the overall best laps array
            object _CarIdxF2Times = null;
            float[] CarIdxF2Times = null;
            irData.Telemetry.TryGetValue("CarIdxF2Time", out _CarIdxF2Times);
            if (_CarIdxF2Times.GetType() == typeof(float[])) {
                CarIdxF2Times = _CarIdxF2Times as float[];
                if (CarIdxF2Times != null) {
                    if (TimeSpan.FromMinutes((float)CarIdxF2Times[competitor.CarIdx]) > TimeSpan.Zero) {
                        car.CarIdxF2Time = CarIdxF2Times[competitor.CarIdx];

                    }
                }
            }

            // try with the overall best laps array
            object _bestlaptimes = null;
            float[] bestlapTimes = null;
            irData.Telemetry.TryGetValue("CarIdxBestLapTime", out _bestlaptimes);
            if (_bestlaptimes.GetType() == typeof(float[])) {
                bestlapTimes = _bestlaptimes as float[];
                if (bestlapTimes != null) {
                    if (TimeSpan.FromMinutes((float)bestlapTimes[competitor.CarIdx]) > TimeSpan.Zero) {
                        car.BestLap = bestlapTimes[competitor.CarIdx];
                        setBestLap = true;
                    }
                }
            }

            // try with the overall best laps array
            object _lastlaptimes = null;
            float[] lastlapTimes = null;
            irData.Telemetry.TryGetValue("CarIdxLastLapTime", out _lastlaptimes);
            if (_lastlaptimes.GetType() == typeof(float[])) {
                lastlapTimes = _lastlaptimes as float[];
                if (lastlapTimes != null) {
                    if (TimeSpan.FromMinutes((float)lastlapTimes[competitor.CarIdx]) > TimeSpan.Zero) {

                        setLastLap = true;
                        car.LastLap = lastlapTimes[competitor.CarIdx];
                        if (lastlapTimes[competitor.CarIdx] < bestlapTimes[competitor.CarIdx]) {
                            if (lastlapTimes[competitor.CarIdx] != 0) {
                                setBestLap = true;
                                car.BestLap = lastlapTimes[competitor.CarIdx];
                            }
                        }
                    }
                }
            }

            // If we aren't in watch mode, grab opponents
            if (!data.NewData.Spectating) {

                // After the best laps, try and overwrite it with the data from the opponents data
                // There may not be an opponent, if not, don't add the opponent data
                List<Opponent> opponents = data.NewData.Opponents;
                Opponent opponent = opponents.Find(Opponent => Opponent.Id == competitiors[id].UserName);
                if (opponent != null) {
                    if (!setLastLap) {
                        car.LastLap = opponent.LastLapTime.TotalSeconds;
                    }
                    if (!setBestLap) {
                        if (opponent.LastLapTime < opponent.BestLapTime) {
                            car.BestLap = opponent.LastLapTime.TotalSeconds;
                        }
                        else {
                            car.BestLap = opponent.BestLapTime.TotalSeconds;
                        }
                    }
                }
            }

            // If car is P1 we are the leader
            if (car.Position == 1) {
                LeaderIdx = car.CarIDx;
                LeaderBestLap = car.BestLap;
                LeaderExpectedLapTime = car.EstimatedLapTime;
                LeaderLapDistancePercent = car.LapDistancePercent;

            }

            return car;
        }

        // Create and clear data structures.
        // Call at the start of every session.
        public void InitClassDataStrucutres() {
            CarClasses.Clear();
            for (int i = 0; i < iRacingMaxClasses; i++) {
                CarClasses[i] = new CarClass();
            }
        }

        // Create the simhub properties
        // Call before updates, after init has been called
        public void AddCarClassRelatedProperties(ref GameData data) {

        }

        // Update the simhub properties
        // call once per second
        public void UpdateCarClassRelatedProperties(ref GameData data) {

        }

        public string LaptimeAsDisplayString(Double seconds) {


            TimeSpan interval = TimeSpan.FromSeconds(seconds);
            return interval.ToString(@"m\:ss\.fff", null);

            //string timeInterval = interval.ToString();
            // Pad the end of the TimeSpan string with spaces if it 
            // does not contain milliseconds.
            //int pIndex = timeInterval.IndexOf(':');
            //pIndex = timeInterval.IndexOf('.', pIndex);
            // if (pIndex < 0) timeInterval += "        ";

        }

        public List<RaceCar> CompetingCarsSortedbyPosition {
            get {
                return CompetingCars.OrderBy(o => o.Position).ToList();
            }
        }
        public List<RaceCar> CompetingCarsSortedbyGapToLeader {
            get {
                return CompetingCars.OrderBy(o => o.GapBehindLeader).ToList();
            }
        }

        public List<RaceCar> CompetingCarsSortedbyBestLapTime {
            get {
                return CompetingCars.OrderBy(o => o.BestLap).ToList();
            }
        }

        public void UpdateStandingsNameSetting() {
            if (Settings.SettingsUpdated) {
                if (Settings.DriverNameStyle_0) {
                    Settings.DriverNameStyle = 0;
                }
                else if (Settings.DriverNameStyle_1) {
                    Settings.DriverNameStyle = 1;
                }
                else if (Settings.DriverNameStyle_2) {
                    Settings.DriverNameStyle = 2;
                }
                else if (Settings.DriverNameStyle_3) {
                    Settings.DriverNameStyle = 3;
                }
                else if (Settings.DriverNameStyle_4) {
                    Settings.DriverNameStyle = 4;
                }
                else if (Settings.DriverNameStyle_5) {
                    Settings.DriverNameStyle = 5;
                }
                else if (Settings.DriverNameStyle_6) {
                    Settings.DriverNameStyle = 6;
                }
                else {
                    Settings.DriverNameStyle = 0;
                }
                Settings.SettingsUpdated = false;
            }
        }

        public void UpdateGapTiming(ref GameData data) {

            irData.Telemetry.TryGetValue("SessionState", out object rawSessionState);
            int sessionState = Convert.ToInt32(rawSessionState);

            // loop through the cars to set the best laps
            foreach (RaceCar theCar in CompetingCars) {
                theCar.EstimatedLapTime = irData.Telemetry.CarIdxEstTime[theCar.CarIDx];
                theCar.LapDistancePercent = irData.Telemetry.CarIdxLapDistPct[theCar.CarIDx];
                theCar.Position = irData.Telemetry.CarIdxPosition[theCar.CarIDx];
                theCar.PositionInClass = irData.Telemetry.CarIdxClassPosition[theCar.CarIDx];

                if (TimeSpan.FromSeconds(LeaderBestLap) == TimeSpan.Zero) {
                    LeaderBestLap = theCar.BestLap;
                }

                // If the best lap is faster, make it the leader's best lap
                if (TimeSpan.FromSeconds(theCar.BestLap) != TimeSpan.Zero) {
                    if (LapIsfaster(theCar.BestLap, LeaderBestLap)) {
                        LeaderBestLap = theCar.BestLap;
                    }
                }

                // If the best lap is faster, make it the fastest lap
                if (TimeSpan.FromSeconds(theCar.LastLap) != TimeSpan.Zero) {
                    if ((LapIsfaster(theCar.LastLap, theCar.BestLap)) || (TimeSpan.FromSeconds(theCar.BestLap) == TimeSpan.Zero)) {

                        theCar.BestLap = theCar.LastLap;
                    }
                }
            }

            bool SimpleGap = true; //TODO: Make this a config setting
            if (SimpleGap) {
                // in practice the leader has the fastest time in the race it is P1
                if ((SessionType == "Practice" || SessionType == "Open Qualify" || SessionType == "Lone Qualify") && sessionState > 3) {

                    // loop through the cars again, adding in the gaps
                    double lastNonZeroGapBehindLeader = 0.0;

                    // In practice gaps are based on lap time
                    foreach (RaceCar anotherCar in CompetingCarsSortedbyBestLapTime) {
                        if (anotherCar.Position > 1) {
                            if (TimeSpan.FromSeconds(anotherCar.BestLap) == TimeSpan.Zero) {
                                anotherCar.GapBehindLeader = 0.0;
                            }
                            else {
                                anotherCar.GapBehindLeader = Math.Abs(LeaderBestLap - anotherCar.BestLap);
                                anotherCar.IntervalGap = anotherCar.GapBehindLeader - lastNonZeroGapBehindLeader;
                                lastNonZeroGapBehindLeader = anotherCar.GapBehindLeader;
                            }

                        }
                    }
                }
                else if (SessionType == "Race") {

                    // in the race gaps are based on position
                    foreach (RaceCar anotherCar in CompetingCarsSortedbyPosition) {

                        // if in watch mode, get the official gaps.
                        if (data.NewData.Spectating) {
                            DebugMessage("Watch mode");
                        }

                        // if we are in the first sector, use the iRacing end of lap delta
                        if (anotherCar.LapDistancePercent < irData.SessionData.SplitTimeInfo.Sectors[1].SectorStartPct) {
                            if (anotherCar.CarIdxF2Time != 0) {
                                anotherCar.GapBehindLeader = anotherCar.CarIdxF2Time;
                            }
                        }
                        else {
                            double lapPace = (LeaderExpectedLapTime + anotherCar.EstimatedLapTime) / 2;
                            double delta = 0.0;
                            if (anotherCar.LapsBehindLeader == 0) {
                                delta = (LeaderLapDistancePercent - anotherCar.LapDistancePercent) * lapPace;
                            }
                            else {
                                delta = ((1 - anotherCar.LapDistancePercent) + LeaderLapDistancePercent) * lapPace;
                                delta = delta + (anotherCar.LapsBehindLeader * lapPace);
                            }
                            anotherCar.GapBehindLeader = delta;
                        }

                        // get the delta to the car in front, not the leader
                        if (CompetingCarsSortedbyPosition.Count() > 2) {
                            if (anotherCar.Position < 2) {
                                anotherCar.IntervalGap = 0.0;
                            }
                            else {
                                anotherCar.IntervalGap = (anotherCar.GapBehindLeader - CompetingCarsSortedbyPosition[anotherCar.Position - 1].GapBehindLeader);
                            }
                        }

                    }

                }
            }

            // Update the gap based positions
            List<RaceCar> loopy = CompetingCarsSortedbyGapToLeader;
            for (int i = 0; i < CompetingCarsSortedbyGapToLeader.Count(); i++) {
                loopy[i].GapBasedPosition = i;
            }
        }
    

        public void ClearStandings() {
            if (Settings.EnableStandings) {
                CompetingCars.Clear();
            }
        }

        public void InitStandings(ref GameData data) {
            if (Settings.EnableStandings) {
                SessionData._DriverInfo._Drivers[] competitiors = irData.SessionData.DriverInfo.CompetingDrivers;
               // List<Opponent> opponents = data.NewData.Opponents;

                if (competitiors != null) {
                    CompetingCars = new List<RaceCar>();
                    NumberOfCarsInSession = 0;
                    for (int i = 0; i < competitiors.Length; i++) {
                        if (competitiors[i].CarNumberRaw > 0)
                        {
                            NumberOfCarsInSession++;

                            RaceCar car = GetCompositeCarForIDx(i, ref data);
                            CompetingCars.Add(car);
                        }

                    }
                }
            }
        }

        public void UpdateStandingsRelatedProperties(ref GameData data) {

            // The cars are all stored in the iRacing Data structures in CarIDx order
            // lets start by getting all the drivers and putting their info into a list of cars

            // Add Standings Properties
            if (Settings.EnableStandings) {
                int i = 0;

                // Loop 1 update everything
                foreach (var car in CompetingCars) {

                    // Get Position
                    car.Position = irData.Telemetry.CarIdxPosition[i];
                    if (car.Position == 1) {
                        LeaderIdx = car.CarIDx;
                    }

                    car.PositionInClass = irData.Telemetry.CarIdxClassPosition[i];
                    car.Lap = irData.Telemetry.CarIdxLap[i];
                    car.LapDistancePercent = irData.Telemetry.CarIdxLapDistPct[i];


                    List<Opponent> opponents = data.NewData.Opponents;
                    Opponent opponent = opponents.Find(Opponent => Opponent.Id == car.Driver.DriverFullName);
                    if (opponent != null) {
                        car.LastLap = opponent.LastLapTime.TotalSeconds;
                        car.BestLap = opponent.BestLapTime.TotalSeconds;

                        if (opponent.LastLapTime < opponent.BestLapTime) {
                            if (TimeSpan.FromSeconds(car.LastLap) != TimeSpan.Zero) {
                                car.BestLap = opponent.LastLapTime.TotalSeconds;
                            }
                        }
                    }

                    // If car is P1 we are the leader
                    if (car.Position == 1) {
                        LeaderIdx = car.CarIDx;
                        LeaderBestLap = car.BestLap;
                        LeaderExpectedLapTime = car.EstimatedLapTime;
                    }

                    // get the last lap times
                    object _lastlaptimes = null;
                    float[] lastlapTimes = null;
                    irData.Telemetry.TryGetValue("CarIdxLastLapTime", out _lastlaptimes);
                    if (_lastlaptimes.GetType() == typeof(float[])) {
                        lastlapTimes = _lastlaptimes as float[];
                        if (lastlapTimes != null) {
                            if (lastlapTimes[i] >= 0) {
                                car.LastLap = lastlapTimes[i];

                                // If this is the overall leader, get their last lap for gap calcs
 
                                if (LeaderIdx == car.CarIDx) {
                                    LeaderLastLap = car.LastLap;
                                    LeaderCurrentLap = car.Lap;
                                }
                                // TODO: Class leader logic goes here
                            }
                        }
                    }

                    if (TimeSpan.FromSeconds(car.LastLap) < TimeSpan.FromSeconds(car.BestLap)) {
                        if (TimeSpan.FromSeconds(car.LastLap) > TimeSpan.Zero) {
                            car.BestLap = car.LastLap;
                        }
                    }

                    // calculate the expected for the current car
                    // Andreas Dahl's Foruyla
                    // LeaderExpectedLapTime = (LeaderLastLap * 2 + LeaderBestLap) / 3;
                    // car.EstimatedLapTime = (car.LastLap * 2 + car.BestLap) / 3;

                    // this is iRacing's formula - Estimated time to reach current location on track
                    LeaderExpectedLapTime = irData.Telemetry.CarIdxEstTime[LeaderIdx];
                    car.EstimatedLapTime = irData.Telemetry.CarIdxEstTime[i];
                  

                    i++;
                }

                UpdateGapTiming(ref data);

                // After the Loop calculate everything

               // LeaderTrackDistancePercent = CompetingCars[LeaderIdx].LapDistancePercent;

                double PreviousGap = 0;
                int CarWithFastestOverallLapTime = 0;

                foreach (var car in CompetingCarsSortedbyGapToLeader) {
                    if (car.Position == 1) {
                        car.GapBehindLeader = 0;
                    }
                    else {
                        // car.IntervalGap = PreviousGap - car.GapBehindLeader;
                        car.IntervalGap = car.GapBehindLeader - PreviousGap;
                        PreviousGap = car.GapBehindLeader;
                    }

                    // get the best lap time because we can't trust Simhub
                    SessionBestLapTime = double.MaxValue;
                    foreach (var fastestCar in CompetingCarsSortedbyBestLapTime) {
                        if (fastestCar.BestLap < SessionBestLapTime) {
                            if (fastestCar.BestLap > 0) {
                                SessionBestLapTime = fastestCar.BestLap;
                                CarWithFastestOverallLapTime = fastestCar.CarIDx;
                            }
                            }
                    }
                    if (SessionBestLapTime == double.MaxValue) {
                        SessionBestLapTime = 0;
                    }

                   //bool BestLapIsOverallBest = false;
                    bool LastLapIsOverallBestLap = false;
                    bool LastLapIsPersonalBestLap = false;

                    if (SessionBestLapTime > 0) {
                        if (car.BestLap == 0 && car.LastLap > 0) {
                            car.BestLap = car.LastLap;
                        }

                        double bob = Math.Abs(SessionBestLapTime - car.BestLap);

                        if (Math.Abs(SessionBestLapTime - car.BestLap) > 0.001) {
                            //BestLapIsOverallBest = true;
                        }

                        if (car.BestLap > 0) {
                            if ((SessionBestLapTime - car.BestLap) > 0.001) {
                                SessionBestLapTime = car.BestLap;
                                //BestLapIsOverallBest = true;
                            }

                        }

                    
                        if (Math.Abs(SessionBestLapTime - car.LastLap) < 0.001) {
                            LastLapIsOverallBestLap = true;
                        }

                        if (Math.Abs(car.BestLap - car.LastLap) < 0.001) {
                            LastLapIsPersonalBestLap = true;
                        }
                    }


                    string iString = string.Format("{0:00}", car.Position);
                    SetProp("Standings.Overall.Position" + iString + ".Position", car.Position);
       
                    
                    if (car.CarIDx == irData.SessionData.DriverInfo.DriverCarIdx) {
                        car.IsPlayer = true;
                    }
                    else {
                        car.IsPlayer = false;
                    }


                    SetProp("Standings.Overall.Position" + iString + ".Number", car.CarNumber);
                    SetProp("Standings.Overall.Position" + iString + ".DriverName", car.Driver.DriverDisplayName);
                    if (car.GapBehindLeader < 0) {
                        SetProp("Standings.Overall.Position" + iString + ".GapToLeader", 0);
                    }
                    else {
                        SetProp("Standings.Overall.Position" + iString + ".GapToLeader", car.GapBehindLeader);
                    }
                    
                    SetProp("Standings.Overall.Position" + iString + ".GapToCarAhead", car.IntervalGap);
                    SetProp("Standings.Overall.Position" + iString + ".IsInPit", car.PitInPitLane);
                    SetProp("Standings.Overall.Position" + iString + ".BestLap", car.BestLap);
                    SetProp("Standings.Overall.Position" + iString + ".LastLap", car.LastLap);
                    SetProp("Standings.Overall.Position" + iString + ".IsPlayer", car.IsPlayer);
                    SetProp("Standings.Overall.Position" + iString + ".LapsBehindLeader", car.LapsBehindLeader);
                    SetProp("Standings.Overall.Position" + iString + ".LastLapIsPersonalBestLap", LastLapIsPersonalBestLap);
                    SetProp("Standings.Overall.Position" + iString + ".LastLapIsOverallBestLap", LastLapIsOverallBestLap);
                    if (CarWithFastestOverallLapTime == car.CarIDx) {
                        SetProp("Standings.Overall.Position" + iString + ".BestLapIsOverallBest", true);
                    }
                    else {
                        SetProp("Standings.Overall.Position" + iString + ".BestLapIsOverallBest", false);
                    }
                    
                    SetProp("Standings.Overall" + iString + ".BestLap", (SessionBestLapTime));

                }
            }

            // update session properties
            SetProp("Standings.NumberOfCarsInSession" , NumberOfCarsInSession);
            SetProp("Standings.Colours.Background", Settings.StandingsBackgroundRowColourWithTransparency);
            SetProp("Standings.Colours.BackgroundAlternate", Settings.StandingsBackgroundRowAlternateColourWithTransparency);
            SetProp("Standings.Colours.BackgroundDriverHighlight", Settings.StandingsBackgroundDriverReferenceRowColourWithTransparency);

            SetProp("Standings.Columns.Position.Left", Settings.ColumnStartPosition);
            SetProp("Standings.Columns.Position.Width", Settings.ColumnWidthPosition);
            SetProp("Standings.Columns.Position.Visible", Settings.ColumnShowPosition);

            SetProp("Standings.Columns.CarNumber.Left", Settings.ColumnStartCarNumber);
            SetProp("Standings.Columns.CarNumber.Width", Settings.ColumnWidthCarNumber);
            SetProp("Standings.Columns.CarNumber.Visible", Settings.ColumnShowCarNumber);

            SetProp("Standings.Columns.DriverName.Left", Settings.ColumnStartDriverName);
            SetProp("Standings.Columns.DriverName.Width", Settings.ColumnWidthDriverName);
            SetProp("Standings.Columns.DriverName.Visible", Settings.ColumnShowDriverName);
            
            SetProp("Standings.Columns.GapToLeader.Left", Settings.ColumnStartGapToLeader);
            SetProp("Standings.Columns.GapToLeader.Width", Settings.ColumnWidthGapToLeader);
            SetProp("Standings.Columns.GapToLeader.Visible", Settings.ColumnShowGapToLeader);
            
            SetProp("Standings.Columns.GapToCarInFront.Left", Settings.ColumnStartGapToCarInFront);
            SetProp("Standings.Columns.GapToCarInFront.Width", Settings.ColumnWidthGapToCarInFront);
            SetProp("Standings.Columns.GapToCarInFront.Visible", Settings.ColumnShowGapToCarInFront);
            
            SetProp("Standings.Columns.FastestLap.Left", Settings.ColumnStartFastestLap);
            SetProp("Standings.Columns.FastestLap.Width", Settings.ColumnWidthFastestLap);
            SetProp("Standings.Columns.FastestLap.Visible", Settings.ColumnShowFastestLap);
            SetProp("Standings.Columns.FastestLap.Slider.Left", Settings.ColumnStartFastestLapSlider);

            SetProp("Standings.Columns.LastLap.Left", Settings.ColumnStartLastLap);
            SetProp("Standings.Columns.LastLap.Width", Settings.ColumnWidthLastLap);
            SetProp("Standings.Columns.LastLap.Visible", Settings.ColumnShowLastLap);
            
        }

        public void AddStandingsRelatedProperties() {
            if (Settings.EnableStandings) {

                AddProp("Standings.Colours.Background", Settings.StandingsBackgroundRowColourWithTransparency);
                AddProp("Standings.Colours.BackgroundAlternate", Settings.StandingsBackgroundRowAlternateColourWithTransparency);
                AddProp("Standings.Colours.BackgroundDriverHighlight", Settings.StandingsBackgroundDriverReferenceRowColourWithTransparency);

                AddProp("Standings.Columns.Position.Left", Settings.ColumnStartPosition);
                AddProp("Standings.Columns.Position.Width", Settings.ColumnWidthPosition);
                AddProp("Standings.Columns.Position.Visible", Settings.ColumnShowPosition);

                AddProp("Standings.Columns.CarNumber.Left", Settings.ColumnStartCarNumber);
                AddProp("Standings.Columns.CarNumber.Width", Settings.ColumnWidthCarNumber);
                AddProp("Standings.Columns.CarNumber.Visible", Settings.ColumnShowCarNumber);

                AddProp("Standings.Columns.DriverName.Left", Settings.ColumnStartDriverName);
                AddProp("Standings.Columns.DriverName.Width", Settings.ColumnWidthDriverName);
                AddProp("Standings.Columns.DriverName.Visible", Settings.ColumnShowDriverName);

                AddProp("Standings.Columns.GapToLeader.Left", Settings.ColumnStartGapToLeader);
                AddProp("Standings.Columns.GapToLeader.Width", Settings.ColumnWidthGapToLeader);
                AddProp("Standings.Columns.GapToLeader.Visible", Settings.ColumnShowGapToLeader);
                                  
                AddProp("Standings.Columns.GapToCarInFront.Left", Settings.ColumnStartGapToCarInFront);
                AddProp("Standings.Columns.GapToCarInFront.Width", Settings.ColumnWidthGapToCarInFront);
                AddProp("Standings.Columns.GapToCarInFront.Visible", Settings.ColumnShowGapToCarInFront);

                AddProp("Standings.Columns.FastestLap.Left", Settings.ColumnStartFastestLap);
                AddProp("Standings.Columns.FastestLap.Width", Settings.ColumnWidthFastestLap);
                AddProp("Standings.Columns.FastestLap.Visible", Settings.ColumnShowFastestLap);
                AddProp("Standings.Columns.FastestLap.Slider.Left", Settings.ColumnStartFastestLapSlider);

                AddProp("Standings.Columns.LastLap.Left", Settings.ColumnStartLastLap);
                AddProp("Standings.Columns.LastLap.Width", Settings.ColumnWidthLastLap);
                AddProp("Standings.Columns.LastLap.Visible", Settings.ColumnShowLastLap);


                

                AddProp("Standings.NumberOfCarsInSession", 0);
                
                for (int i = 1; i < iRacingMaxCars + 1; i++) {
                    string iString = string.Format("{0:00}", i);
                    AddProp("Standings.Overall.Position" + iString + ".Position", 0);
                    AddProp("Standings.Overall.Position" + iString + ".Number", 0);
                    AddProp("Standings.Overall.Position" + iString + ".DriverName", string.Empty);
                    AddProp("Standings.Overall.Position" + iString + ".GapToLeader", 0);
                    AddProp("Standings.Overall.Position" + iString + ".GapToCarAhead", 0);
                    AddProp("Standings.Overall.Position" + iString + ".IsInPit", 0);
                    AddProp("Standings.Overall.Position" + iString + ".BestLap", 0);
                    AddProp("Standings.Overall.Position" + iString + ".LastLap", 0);
                    AddProp("Standings.Overall.Position" + iString + ".IsPlayer", false);
                    SetProp("Standings.Overall.Position" + iString + ".LapsBehindLeader", 0);
                    AddProp("Standings.Overall.Position" + iString + ".LastLapIsPersonalBestLap", false);
                    AddProp("Standings.Overall.Position" + iString + ".LastLapIsOverallBestLap", false);
                    AddProp("Standings.Overall.Position" + iString + ".BestLapIsOverallBest", false);
                    AddProp("Standings.Overall" + iString + ".BestLap", 0);
                }
            }
        }

        public bool LapIsEqual(double first,  double second) {
            TimeSpan firstTime = TimeSpan.FromSeconds(first);
            TimeSpan secondTime = TimeSpan.FromSeconds(second);
            return firstTime == secondTime;
        }

        public bool LapIsfaster(double first, double second) {
            TimeSpan firstTime = TimeSpan.FromSeconds(first);
            TimeSpan secondTime = TimeSpan.FromSeconds(second);
            return firstTime < secondTime;
        }
        

        

        public class Standings {
            public int CurrentlyObservedDriver { get; set; } = 0;

            public string BattleBoxDisplayString { get; set; } = string.Empty;
            public double BattleBoxGap { get; set; }
            public int BattleBoxDriver1Position { get; set; } = 0;
            public int BattleBoxDriver2Position { get; set; } = 0;
            public string BattleBoxDriver1Name { get; set; } = string.Empty;
            public string BattleBoxDriver2Name { get; set; } = string.Empty;
            public int EstimatedOvertakeLaps { get; set; } = 0;
            public double EstimatedOvertakePercentage { get; set; } = 0.0;

        }

        
        public class CarClass {
            public int CarClassID { get; set; } = 0;
            public string CarClassName { get; set; } = string.Empty;
            public string CarClassColour { get; set; } = string.Empty;
            public string CarClassDisplayName { get; set; } = string.Empty;
            public double CarClassBestLapTime { get; set; } = 0;
            public double CarClassLeaderID { get; set; } = 0;
        }

        public class RaceCar {

            public TrackSections track;

            public RaceCar() {
                this.Driver = new Driver();
                this.track = new TrackSections();
            }

            public int CarIDx { get; set; } = int.MinValue;
            public string CarNumber { get; set; } = "";   
            public long CarClass { get; set; } = long.MinValue;
            public Driver Driver { get; set; } = null;
            public int CurrentTrackSection { get; set; } = 0;


            // Lap timing info
            public double BestLap { get; set; } = 0;
            public double LastLap { get; set; } = 0;
            public int Lap { get; set; } = 0;
            public float CarIdxF2Time { get; set; } = 0;


            public double EstimatedLapTime { get; set; } = 0;

            public double IntervalGap { get; set; } = 0;
            public double IntervalGapDelayed { get; set; } = 0;
            public double LapDistancePercent { get; set; } = 0;
            public int LapsBehindLeader { get; set; } = 0;
            public double GapBehindLeader { get; set; } = 0;
            public int LapsBehindNext { get; set; } = 0;
            public int Position { get; set; } = 0;
            public int GapBasedPosition { get; set; } = 0;
            public int PositionInClass { get; set; } = 0;
            public int GapBasedPositionInClass { get; set; } = 0;

            //Pit info
            public int PitInPitLane { get; set; } = 0;

            public bool IsPlayer { get;  set; } = false;
          
        }

        // Holds driver information
        public class Driver {
            public long DriverCustomerID { get; set; } = 0;
            public string DriverFullName { get; set; } = string.Empty;
            public string DriverFirstName { get; set; } = string.Empty;
            public string DriverFirstNameInitial { get; set; } = string.Empty;
            public string DriverFirstNameShort { get; set; } = string.Empty;
            public string DriverLastName { get; set; } = string.Empty;
            public string DriverLastNameInitial { get; set; } = string.Empty;
            public string DriverLastNameShort { get; set; } = string.Empty;
            public long DriverIRating { get; set; } = 0;
            public string DriverSafetyRating { get; set; } = string.Empty;
            public long DriverLicenseLevel { get; set; } = 0;
            public string Nationality { get; set; } = string.Empty;

            public string DriverDisplayName { get; set; } = string.Empty;

        }

        public class TrackSections {
            static int NumberOfSections = 60;
            public TrackSector[] Sections;

            public TrackSections() {

                // 60 sections numbered from from 0 to 59
                Sections = new TrackSector[NumberOfSections];
                for (int i = 0; i < Sections.Length; i++) {
                    Sections[i] = new TrackSector(i);
                }
            }

            public void UpdateTimeForSectionAtPercentage(double trackDistancePercentage, double CurrentElapsedLapTime) {
                // we might need to reset here, but for now, let's just keep rolling and overwriting
                int currentSection = GetATrackSectionForAGivenPercentageAroundTrack(trackDistancePercentage);
                if (currentSection == 0) {
                    Sections[currentSection].TrackSectionTime = CurrentElapsedLapTime;
                }
                else {
                    double timeCurrentlySaved = 0;
                    for (int i = 0; i < currentSection - 1; i++) {
                        timeCurrentlySaved = timeCurrentlySaved + Sections[i].TrackSectionTime;
                    }
                    Sections[currentSection].TrackSectionTime = CurrentElapsedLapTime - timeCurrentlySaved;
                }
            }

            public double GetTimeInSection(int section) {
                return Sections[section].TrackSectionTime;
            }

            public void Reset() {
                for (int i = 0; i < Sections.Length; i++) {
                    Sections[i].Reset();
                }
            }

            // Return the tracksector for a given percentage around the track
            public static int GetATrackSectionForAGivenPercentageAroundTrack(double PercentAroundTrack) {

                // reurn the sector where we will be for a given percentate
                // this calc works because we use a staic number of track sections
                double div = (100.0000 / Convert.ToDouble(NumberOfSections));
                int pct = Convert.ToInt32(Math.Floor(((PercentAroundTrack * 100) / div)));
                return pct;
            }

            public int Tracksector(int TrackSection) {
                if (TrackSection < (NumberOfSections / 3)) {
                    return 1;
                }
                else if (TrackSection < ((NumberOfSections / 3) * 2)) {
                    return 2;
                }
                else {
                    return 3;
                }
            }




        }

        public class TrackSector {
            // We are hardcoded to NumberOfSections
           // static int NumberOfSections = 60;
            public TrackSector(int trackSectionID) {

            }




            public int TrackSectionID { get; set; }
            public double TrackDistancePercent { get; set; }
            public double TrackSectionTime { get; set; }

            public void Reset() {
                TrackSectionTime = 0;
            }
        }


        /* Currently not used
        
        public double BestLapSector1 { get; set; } = 0;
        public double BestLapSector2 { get; set; } = 0;
        public double BestLapSector3 { get; set; } = 0;
        public double CurrentSector1Time { get; set; } = 0;
        public double CurrentSector2Time { get; set; } = 0;
        public double CurrentSector3Time { get; set; } = 0;
        public int CurrentSectorNumber { get; set; } = 0;

        public int PositionsGainedLost { get; set; } = 0;
        public int SpeedCurrent { get; set; } = 0;
        public int SpeedMax { get; set; } = 0;
        public double TimeBehindLeader { get; set; } = 0;
        public double TimeBehindNext { get; set; } = 0;
        public int TotalLaps { get; set; } = 0;
        public int LapsDown { get; set; } = 0;


        // Pit info
        public bool PitInPitBox { get; set; } = false;
        
        public int PitLastLapPitted { get; set; } = 0;
        public int PitLastStopDuration { get; set; } = 0;
        public int PitCount { get; set; } = 0;

        // Flags
        public bool HasFinished { get; set; } = false;
        public bool HasRetired { get; set; } = false;
        public bool HasBlueFlag { get; set; } = false;
        public bool HasOfftrack { get; set; } = false;
        public int IncidentCount { get; set; } = 0;


        public class Championship {
            // Leadercar
            // Leader Photo
            // Leader team
            // Championship name
            // Next event
            // Current round number
            // Championship Sponsor

            // Championship Standings
        }
        */
    }
}
