using FMOD;
using GameReaderCommon;
using IRacingReader;
using iRacingSDK;
using MahApps.Metro.Controls;
using SimHub.Plugins;
using SimHub.Plugins.OutputPlugins.GraphicalDash.Behaviors.DoubleText.Imp;
using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Windows.Documents;
using System.Windows.Markup;
using Opponent = GameReaderCommon.Opponent;

namespace APR.DashSupport {

    public partial class APRDashPlugin : IPlugin, IDataPlugin, IWPFSettingsV2 {
/*      static int iRacingMaxCars = 63;
        static int iRacingMaxClasses = 5;

        public List<RaceCar> CompetingCars = new List<RaceCar>();
        public List<RaceCar> OpponentCars = new List<RaceCar>();
        public int NumberOfCarsInSession = 0;
        public long NumOfCarClasses = 0;
        public int LeaderIdx = 0;
        public List<int> ClassLeaderIdx = new List<int>();
        public double LeaderLastLap { get; set; } = 0;
        public double LeaderExpectedLapTime { get; set; } = 0;
        public double LeaderExtTime { get; set; } = 0;
        public double LeaderBestLap { get; set; } = 0;
        public int LeaderCurrentLap { get; set; } = 0;
        public double LeaderLapDistancePercent { get; set; } = 0;
        public double SessionBestLapTime { get; set; } = 0;
        public double CameraDriverIdx { get; set; } = 0;
        public double PlayerIdx { get; set; } = 0;
        public double PlayerClassId { get; set; } = 0;

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
                ClassId = competitor.CarClassID,
                CarNumber = competitor.CarNumber,
                
                // Get Position
                Position = irData.Telemetry.CarIdxPosition[competitor.CarIdx],
                ClassPosition = irData.Telemetry.CarIdxClassPosition[competitor.CarIdx],
                EstimatedLapTime = irData.Telemetry.CarIdxEstTime[competitor.CarIdx],
                LapDistancePercent = irData.Telemetry.CarIdxLapDistPct[competitor.CarIdx],
                Lap = irData.Telemetry.CarIdxLap[competitor.CarIdx],

            };

            if (car.Position > 1) {
                car.LapsBehindLeader = LeaderCurrentLap - car.Lap;
            }
            else {
                car.LapsBehindLeader = 0;
            }
            

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
                LeaderCurrentLap = car.Lap;
            }

            return car;
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

        public Opponent GetOpponentWithName(ref GameData data, string name) {
            return data.NewData.Opponents.Find(x => x.Name == name);
        }

        public List<RaceCar> CompetingCarsSortedbyGapToLeader {
            get {
                return CompetingCars.OrderBy(o => o.GapBehindLeader).ToList();
            }
        }

        public List<RaceCar> CompetingCarsSortedbyRaceDistancePercent {
            get {
                return CompetingCars.OrderByDescending(o => o.RaceDistancePercent).ToList();
            }
        }

        public List<RaceCar> CompetingCarsSortedbyBestLapTime {
            get {
                return CompetingCars.OrderBy(o => o.BestLap).ToList();
            }
        }
*/

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


        public void ClearStandings() {
            if (Settings.EnableStandings) {
                /*
                    for (int i = 1; i < iRacingMaxCars + 1; i++) {
                        string iString = string.Format("{0:00}", i);
                        SetProp("Standings.Overall.Position" + iString + ".Position", 0);
                        SetProp("Standings.Overall.Position" + iString + ".Position", 0);
                        SetProp("Standings.Overall.Position" + iString + ".Number", 0);
                        SetProp("Standings.Overall.Position" + iString + ".DriverName", string.Empty);
                        SetProp("Standings.Overall.Position" + iString + ".GapToLeader", 0);
                        SetProp("Standings.Overall.Position" + iString + ".GapToCarAhead", 0);
                        SetProp("Standings.Overall.Position" + iString + ".IsInPit", 0);
                        SetProp("Standings.Overall.Position" + iString + ".BestLap", 0);
                        SetProp("Standings.Overall.Position" + iString + ".LastLap", 0);
                        SetProp("Standings.Overall.Position" + iString + ".IsPlayer", false);
                        SetProp("Standings.Overall.Position" + iString + ".LapsBehindLeader", 0);
                        SetProp("Standings.Overall.Position" + iString + ".LastLapIsPersonalBestLap", false);
                        SetProp("Standings.Overall.Position" + iString + ".LastLapIsOverallBestLap", false);
                        SetProp("Standings.Overall.Position" + iString + ".BestLapIsOverallBest", false);
                        SetProp("Standings.Overall.Position" + iString + ".RowIsVisible", false);
                        SetProp("Standings.Overall" + iString + ".BestLap", 0);

                        SetProp("Standings.Overall.Position" + iString + ".Class.ClassId",0D);
                        SetProp("Standings.Overall.Position" + iString + ".Class.Name", string.Empty);
                        SetProp("Standings.Overall.Position" + iString + ".Class.Color", string.Empty);
                        SetProp("Standings.Overall.Position" + iString + ".Class.Position", 0);
                        SetProp("Standings.Overall.Position" + iString + ".Class.GapToLeader", 0);
                    }
                */
            }
        }


        public void InitStandings(ref GameData data) {
            if (Settings.EnableStandings) {
                /*
                SessionData._DriverInfo._Drivers[] competitiors = irData.SessionData.DriverInfo.CompetingDrivers;
                SessionData._WeekendInfo weekendInfo = irData.SessionData.WeekendInfo;

                NumOfCarClasses = weekendInfo.NumCarClasses;
                // InitClassDataStrucutres();

                // List<Opponent> opponents = data.NewData.Opponents;

                if (competitiors != null) {
                    CompetingCars = new List<RaceCar>();
                    NumberOfCarsInSession = 0;
                    CameraDriverIdx = irData.Telemetry.CamCarIdx;
                    PlayerIdx = irData.SessionData.DriverInfo.DriverCarIdx;

                    for (int i = 0; i < competitiors.Length; i++) {
                        if (competitiors[i].CarNumberRaw > 0)
                        {
                            NumberOfCarsInSession++;
                            RaceCar car = GetCompositeCarForIDx(i, ref data);
                            CompetingCars.Add(car);
                            if (car.CarIDx == PlayerIdx) {
                                PlayerClassId = car.ClassId;
                            }
                        }
                    }
                }*/
            }
        }

        public void UpdateStandingsRelatedProperties(ref GameData data) {

            // Get the iRacing Session state
            irData.Telemetry.TryGetValue("SessionState", out object rawSessionState);
            int sessionState = Convert.ToInt32(rawSessionState);

            // The cars are all stored in the iRacing Data structures in CarIDx order
            // lets start by getting all the drivers and putting their info into a list of cars

            // Add Standings Properties
            /*
            if (Settings.EnableStandings) {

                // Calculate the session best laptime
                SessionBestLapTime = double.MaxValue;
                int CarWithFastestOverallLapTime = 0;

                // Loop 1 update everything
                foreach (var car in CompetingCars) {

                    // Get Position overall and in class
                    car.Position = irData.Telemetry.CarIdxPosition[car.CarIDx];
                    car.ClassPosition = irData.Telemetry.CarIdxClassPosition[car.CarIDx];

                    // Get class info
                    car.ClassId = irData.SessionData.DriverInfo.CompetingDrivers[car.CarIDx].CarClassID;
                    car.ClassName = irData.SessionData.DriverInfo.CompetingDrivers[car.CarIDx].CarClassShortName;
                    var tmpColor = irData.SessionData.DriverInfo.CompetingDrivers[car.CarIDx].CarClassColor;
                    car.ClassColor = tmpColor.Replace("0x","#FF");
                    car.EstimatedLapTime = irData.Telemetry.CarIdxEstTime[car.CarIDx];
                    car.LapDistancePercent = irData.Telemetry.CarIdxLapDistPct[car.CarIDx];

                    car.Lap = irData.Telemetry.CarIdxLap[car.CarIDx];

                    if (car.Position > 0) {

                        // Get the last lap times
                        object _lastlaptimes = null;
                        float[] lastlapTimes = null;
                        irData.Telemetry.TryGetValue("CarIdxLastLapTime", out _lastlaptimes);
                        if (_lastlaptimes.GetType() == typeof(float[])) {
                            lastlapTimes = _lastlaptimes as float[];
                            if (lastlapTimes != null) {
                                if (lastlapTimes[car.CarIDx] >= 0) {
                                    car.LastLap = lastlapTimes[car.CarIDx];
                                }
                            }
                        }

                        // Get the best lap times
                        object _bestlaptimes = null;
                        float[] bestlapTimes = null;
                        irData.Telemetry.TryGetValue("CarIdxBestLapTime", out _bestlaptimes);
                        if (_bestlaptimes.GetType() == typeof(float[])) {
                            bestlapTimes = _bestlaptimes as float[];
                            if (bestlapTimes != null) {
                                if (bestlapTimes[car.CarIDx] >= 0) {
                                    car.BestLap = bestlapTimes[car.CarIDx];
                                }
                                else {
                                    Opponent oppy = GetOpponentWithName(ref data, car.Driver.DriverFullName);
                                    double bestLapFromOpponents = oppy.BestLapTime.TotalSeconds;
                                    if (bestLapFromOpponents > 0.0) {
                                        car.BestLap = bestLapFromOpponents;
                                    }
                                }
                            }
                        }

                        // If our lap is the fastest overall, record it as the sesion best
                        if (car.BestLap < SessionBestLapTime) {
                            if (car.BestLap > 0.0) {
                                SessionBestLapTime = car.BestLap;
                                CarWithFastestOverallLapTime = car.CarIDx;

                            }
                        }

                        // If car is P1 we are the leader
                        // If this is the overall leader, get their data for calcs
                        if (car.Position == 1) {
                            LeaderIdx = car.CarIDx;
                            LeaderBestLap = car.BestLap;
                            LeaderLastLap = car.LastLap;
                            LeaderCurrentLap = car.Lap;
                            LeaderExpectedLapTime = car.EstimatedLapTime;
                        }
                       
                        // calculate the expected for the current car
                        // Andreas Dahl's Foruyla
                        LeaderExpectedLapTime = (LeaderLastLap * 2 + LeaderBestLap) / 3;
                        car.EstimatedLapTime = (car.LastLap * 2 + car.BestLap) / 3;

                        // this is iRacing's formula - Estimated time to reach current location on track
                        LeaderExtTime = irData.Telemetry.CarIdxEstTime[LeaderIdx];
                        car.EstTime = irData.Telemetry.CarIdxEstTime[car.CarIDx];
                    }
                }

                // if there is no session best set it to zero
                if (SessionBestLapTime == double.MaxValue) {
                    SessionBestLapTime = 0.0;
                }

                bool SimpleGap = true; //TODO: Make this a config setting
                if (SimpleGap) {
                    // in practice the leader has the fastest time in the race it is P1
                    if ((SessionType == "Practice" || SessionType == "Open Qualify" || SessionType == "Lone Qualify") && sessionState > 3) {

                        // In practice gaps are based on lap time and we don't do intervals
                        foreach (var car in CompetingCars) {
                            if (car.Position > 1) {
                                if (TimeSpan.FromSeconds(car.BestLap) == TimeSpan.Zero) {
                                    car.GapBehindLeader = 0.0;
                                }
                                else {
                                    car.GapBehindLeader = Math.Abs(LeaderBestLap - car.BestLap);
                                }
                            }
                        }
                    }
                    else if (SessionType == "Race") {

                        foreach (var car in CompetingCars) {
                            if (car.Position > 0) {
                                // if in watch mode, get the official gaps.
                                if (data.NewData.Spectating) {
                                    //DebugMessage("Watch mode");
                                }

                                if (car.Position == 1) {
                                    car.GapBehindLeader = 0.0;
                                    LeaderCurrentLap = car.Lap;
                                }
                                else {
                                   if (LeaderLapDistancePercent > 0.1) {
                                        double lapPace = (LeaderExpectedLapTime + car.EstimatedLapTime) / 2;
                                        double carEst = LeaderExtTime - car.EstTime;

                                        double delta = 0.0;
                                        if (car.LapsBehindLeader == 0) {
                                            delta = (LeaderLapDistancePercent - car.LapDistancePercent) * lapPace;
                                        }
                                        else {
                                            delta = (((1 - car.LapDistancePercent) * lapPace) + (LeaderLapDistancePercent * lapPace));

                                        }
                                        car.GapBehindLeader = delta;
                                   }
                                }
                            }
                        }

                        foreach (var car in CompetingCars) {
                            // get the delta to the car in front, not the leader
                            if (car.Position <= 1) {
                                car.IntervalGap = 0.0;
                            }
                            else if (car.Position == 2) {
                                car.IntervalGap = car.GapBehindLeader;
                            }
                            else {
                                var carBehind = CompetingCars.Find(x => x.Position == (car.Position - 1));
                                double carBehindGap = carBehind.GapBehindLeader;
                                if (carBehindGap > 0.0) {
                                    car.IntervalGap = (car.GapBehindLeader - carBehindGap);
                                }
                            }
                        }
                    }
                }
                // lap distance percentage based standings and gaps
                var cars = CompetingCars;
                for (int i = 0; i < cars.Count; i++) {
                    cars[i].RaceDistancePercent = cars[i].LapDistancePercent + cars[i].Lap;
                }


                if ((SessionType == "Practice" || SessionType == "Open Qualify" || SessionType == "Lone Qualify") && sessionState > 3) {
                    cars = CompetingCarsSortedbyPosition;
                }
                else {
                    cars = CompetingCarsSortedbyRaceDistancePercent;
                }

                bool multiClassView = true;
                List<RaceCar> MyClassOnlyCars = new List<RaceCar>();
                List<RaceCar> CarsForDisplay = new List<RaceCar>();
                if (multiClassView) {
                    MyClassOnlyCars = CompetingCarsForClassSortedByPosition(cars, PlayerClassId);
                    for (int i = 0; i < MyClassOnlyCars.Count; i++) {
                        var car = MyClassOnlyCars[i];
                        if (car.Position > 0) {
                            CarsForDisplay.Add(car);
                        }
                    }
                }
                else {
                    for (int i = 0; i < cars.Count; i++) {
                        var car = cars[i];
                        if (car.Position > 0) {
                            CarsForDisplay.Add(car);
                        }
                    }
                }
                    
                
                
                for (int i = 0; i < CarsForDisplay.Count; i++) {
                    var car = CarsForDisplay[i];

                    //bool BestLapIsOverallBest = false;
                    bool LastLapIsOverallBestLap = false;
                    bool LastLapIsPersonalBestLap = false;

                    if (SessionBestLapTime > 0) {
                        if (Math.Abs(SessionBestLapTime - car.LastLap) < 0.001) {
                            LastLapIsOverallBestLap = true;
                        }

                        if (Math.Abs(car.BestLap - car.LastLap) < 0.001) {
                            LastLapIsPersonalBestLap = true;
                        }
                    }

                    string iString = string.Format("{0:00}", i+1);
                    SetProp("Standings.Overall.Position" + iString + ".Position", car.Position);
                    SetProp("Standings.Overall.Position" + iString + ".RowIsVisible", true);

                    if ( car.CarIDx == irData.SessionData.DriverInfo.DriverCarIdx) {
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


                    // Class specifics
                    SetProp("Standings.Overall.Position" + iString + ".Class.ClassId", car.ClassId);
                    SetProp("Standings.Overall.Position" + iString + ".Class.Name", car.ClassName);
                    SetProp("Standings.Overall.Position" + iString + ".Class.Color", car.ClassColor);
                    SetProp("Standings.Overall.Position" + iString + ".Class.Position", car.ClassPosition);
                    SetProp("Standings.Overall.Position" + iString + ".Class.GapToLeader", car.ClassGapToClassLeader);
                
                }
                */



            // update session properties
            //SetProp("Standings.NumberOfCarsInSession" , NumberOfCarsInSession);
            if (Settings.EnableStandings) {

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

                // in practice the leader has the fastest time in the race it is P1
                if ((SessionType == "Practice" || SessionType == "Open Qualify" || SessionType == "Lone Qualify") && sessionState > 3) {
                    Settings.HideGapToCarInFront = true;
                }
                else {
                    Settings.HideGapToCarInFront = false;
                }

                SetProp("Standings.Columns.GapToCarInFront.Left", Settings.ColumnStartGapToCarInFront);
                if (Settings.HideGapToCarInFront) {
                    SetProp("Standings.Columns.GapToCarInFront.Visible", false);
                }
                else {
                    SetProp("Standings.Columns.GapToCarInFront.Visible", Settings.ColumnShowGapToCarInFront);
                }

                SetProp("Standings.Columns.GapToCarInFront.Width", Settings.ColumnWidthGapToCarInFront);

                SetProp("Standings.Columns.FastestLap.Left", Settings.ColumnStartFastestLap);
                SetProp("Standings.Columns.FastestLap.Width", Settings.ColumnWidthFastestLap);
                SetProp("Standings.Columns.FastestLap.Visible", Settings.ColumnShowFastestLap);
                SetProp("Standings.Columns.FastestLap.Slider.Left", Settings.ColumnStartFastestLapSlider);

                SetProp("Standings.Columns.LastLap.Left", Settings.ColumnStartLastLap);
                SetProp("Standings.Columns.LastLap.Width", Settings.ColumnWidthLastLap);
                SetProp("Standings.Columns.LastLap.Visible", Settings.ColumnShowLastLap);
            }
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


                /*

                AddProp("Standings.NumberOfCarsInSession", 0);
                
                for (int i = 1; i < iRacingMaxCars + 1; i++) {
                    string iString = string.Format("{0:00}", i);
                    AddProp("Standings.Overall.Position" + iString + ".Position", 0);
                    AddProp("Standings.Overall.Position" + iString + ".Class.ClassId", 0);
                    AddProp("Standings.Overall.Position" + iString + ".Class.Name", 0);
                    AddProp("Standings.Overall.Position" + iString + ".Class.Color", 0);
                    AddProp("Standings.Overall.Position" + iString + ".Class.Position", 0);
                    AddProp("Standings.Overall.Position" + iString + ".Class.GapToLeader", 0);
                    AddProp("Standings.Overall.Position" + iString + ".Class.GapToCarAhead", 0);
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
                    AddProp("Standings.Overall.Position" + iString + ".RowIsVisible", false);
                    AddProp("Standings.Overall" + iString + ".BestLap", 0);
                }
                */
            }
        }

        /*
        public List<RaceCar> CompetingCarsForClassSortedByPosition(List<RaceCar> cars, double classID) {
            List < RaceCar > tmp = cars.FindAll(x => x.ClassId == classID);
            return tmp.OrderBy(o => o.ClassPosition).ToList();
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
        
        */

        public class Standings {

            /*
            public int CurrentlyObservedDriver { get; set; } = 0;
            public string BattleBoxDisplayString { get; set; } = string.Empty;
            public double BattleBoxGap { get; set; }
            public int BattleBoxDriver1Position { get; set; } = 0;
            public int BattleBoxDriver2Position { get; set; } = 0;
            public string BattleBoxDriver1Name { get; set; } = string.Empty;
            public string BattleBoxDriver2Name { get; set; } = string.Empty;
            public int EstimatedOvertakeLaps { get; set; } = 0;
            public double EstimatedOvertakePercentage { get; set; } = 0.0;
            */
        }

        /*
        public class RaceCar {

            public override string ToString() {
                return string.Format("P{0} {1} {2}", Position, CarNumber, Driver.DriverDisplayName);
            }

            public TrackSections track;

            public RaceCar() {
                this.Driver = new Driver();
                this.track = new TrackSections();
            }

            public int CarIDx { get; set; } = int.MinValue;
            public string CarNumber { get; set; } = "";

            public Driver Driver { get; set; } = null;
            public int CurrentTrackSection { get; set; } = 0;

            // Lap timing info
            public double BestLap { get; set; } = 0;
            public double LastLap { get; set; } = 0;
            public int Lap { get; set; } = 0;
            public float CarIdxF2Time { get; set; } = 0;

            public double EstimatedLapTime { get; set; } = 0;
            public double EstTime { get; set; } = 0;

            public double IntervalGap { get; set; } = 0;
            public double IntervalGapDelayed { get; set; } = 0;
            public double LapDistancePercent { get; set; } = 0;
            public double RaceDistancePercent { get; set; } = 0;
            public int LapsBehindLeader { get; set; } = 0;
            public double GapBehindLeader { get; set; } = 0;
            public int LapsBehindNext { get; set; } = 0;
            public int Position { get; set; } = 0;
            public int GapBasedPosition { get; set; } = 0;


            //Pit info
            public int PitInPitLane { get; set; } = 0;
            public bool IsPlayer { get; set; } = false;

            // Class info
            public long ClassId { get; set; } = long.MinValue;
            public string ClassName { get; set; } = string.Empty;
            public string ClassColor { get; set; } = string.Empty;
            public int ClassPosition { get; set; } = 0;
            public int ClassGapToClassLeader { get; set; } = 0;
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

        */
    }
}