using FMOD;
using GameReaderCommon;
using IRacingReader;
using iRacingSDK;
using MahApps.Metro.Controls;
using SimHub.Plugins;
using SimHub.Plugins.DataPlugins.RGBDriver.LedsContainers.Status;
using SimHub.Plugins.OutputPlugins.Dash.GLCDTemplating;
using SimHub.Plugins.OutputPlugins.GraphicalDash.Behaviors.DoubleText;
using SimHub.Plugins.OutputPlugins.GraphicalDash.Models.BuiltIn;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Markup.Localizer;

namespace APR.DashSupport {

    public partial class APRDashPlugin : IPlugin, IDataPlugin, IWPFSettingsV2 {
        static int iRacingMaxCars = 63;


        public List<RaceCar> CompetingCars = new List<RaceCar>();
        public int LeaderIdx = 0;
        public List<int> ClassLeaderIdx = new List<int>();
        public double LeaderLastLap { get; set; } = 0;
        public double LeaderExpectedLapTime { get; set; } = 0;
        public double LeaderBestLap { get; set; } = 0;
        public int LeaderCurrentLap { get; set; } = 0;
        public double LeaderTrackDistancePercent { get; set; } = 0;

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

        public void UpdateGapTiming() {



            // loop through the cars
            for (int i = 0; i < CompetingCars.Count; i++) {
                // TODO : check the car is on track

                // Get the current TrackDistancePercent
                CompetingCars[i].EstimatedLapTime = irData.Telemetry.CarIdxEstTime[i];

                // Get the current lap elapsed time
                CompetingCars[i].EstimatedLapTime = irData.Telemetry.CarIdxEstTime[i];
                CompetingCars[i].LapDistancePercent = irData.Telemetry.CarIdxLapDistPct[i];

                CompetingCars[i].Position = irData.Telemetry.CarIdxPosition[i];
                CompetingCars[i].PositionInClass = irData.Telemetry.CarIdxClassPosition[i];

                bool SimpleGap = true; //TODO: Make this a config setting
                if (SimpleGap) {
                    if (CompetingCars[i].Position < 2) {
                        CompetingCars[i].GapBehindLeader = 0;
                        CompetingCars[i].IntervalGap = 0;
                    }
                    else {

                        double leaderTime = LeaderExpectedLapTime * LeaderTrackDistancePercent;
                        double mytime = CompetingCars[i].EstimatedLapTime * CompetingCars[i].LapDistancePercent;
                        double deltaTimeInSeconds; ;
                        // if I am on the lead lap, the gap is the leaders pace * percentge around track - my pace * percentage around track.
                        if (LeaderLastLap == CompetingCars[i].Lap) {
                            deltaTimeInSeconds = (leaderTime - mytime);
                        }
                        // if I am not on the lead lap, add the extra laps at the leaders pace as well.
                        else {
                            deltaTimeInSeconds = (leaderTime - mytime) + (CompetingCars[i].LapsBehindLeader * LeaderExpectedLapTime);
                        }
                        CompetingCars[i].GapBehindLeader = deltaTimeInSeconds;
                    }

                }
                else {


                    if (CompetingCars[i].LapDistancePercent > 0) {

                        CompetingCars[i].CurrentTrackSection = TrackSections.GetATrackSectionForAGivenPercentageAroundTrack(CompetingCars[i].LapDistancePercent);

                        // if the current section time is 0, sector time is the current lap time
                        if (CompetingCars[i].CurrentTrackSection == 0) {
                            CompetingCars[i].track.Sections[CompetingCars[i].CurrentTrackSection].TrackSectionTime = CompetingCars[i].EstimatedLapTime;
                        }

                        // if the section is > 1, the current sector time is the current lap time,
                        // minus all the previous sections added up
                        else {
                            // Add all the previous track sections
                            double cumulativeLapTime = 0;
                            for (int j = 0; j < CompetingCars[i].CurrentTrackSection - 1; j++) {
                                cumulativeLapTime = cumulativeLapTime + CompetingCars[i].track.Sections[j].TrackSectionTime;
                            }

                            CompetingCars[i].track.Sections[CompetingCars[i].CurrentTrackSection].TrackSectionTime = CompetingCars[i].EstimatedLapTime - cumulativeLapTime;
                           
                        }
                    }
                }
               
            }
        }



        public void InitStandings() {
            if (Settings.EnableStandings) {


                SessionData._DriverInfo._Drivers[] competitiors = irData.SessionData.DriverInfo.CompetingDrivers;
                if (competitiors != null) {
                    CompetingCars = new List<RaceCar>();
                    for (int i = 0; i < competitiors.Length; i++) {
                        if (competitiors[i].CarClassID > 0)
                        {

                            RaceCar car = new RaceCar();
                            car.CarIDx = Convert.ToInt32(competitiors[i].CarIdx);
                            if (car.CarIDx != i) {
                                DebugMessage("Warning IDs do not match!!");
                            }

                            car.CarClass = competitiors[i].CarClassID;

                            car.Driver.DriverFullName = competitiors[i].UserName;
                            car.Driver.DriverCustomerID = competitiors[i].UserID;
                            car.CarNumber = (int)(competitiors[i].CarNumberRaw);
                            // car.CarNumberDesignString = competitiors[i].CarNumberDesignStr;

                            //TODO: Make this a flag that can be set to ignore these in standings
                            // car.CarIsPaceCart = competitiors[i].CarIsPaceCar;
                            // car.IsAi = competitiors[i].CarIsAI;
                            car.Driver.DriverIRating = competitiors[i].IRating;
                            car.Driver.DriverSafetyRating = competitiors[i].LicString;
                            car.Driver.DriverLicenseLevel = competitiors[i].LicLevel;
                            car.Driver.Nationality = competitiors[i].ClubName;



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

                                if ( car.Driver.DriverLastName.Length > 3 ) {
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

                    // Get the best lap times
                    object _bestlaptimes = null;
                    float[] bestlapTimes = null;
                    irData.Telemetry.TryGetValue("CarIdxBestLapTime", out _bestlaptimes);
                    if (_bestlaptimes.GetType() == typeof(float[])) {
                        bestlapTimes = _bestlaptimes as float[];
                        if (bestlapTimes != null) {
                            if (bestlapTimes[i] >= 0) {
                                car.BestLap = bestlapTimes[i];

                                // If this is the overall leader, get their best lap for gap calcs
                                if (LeaderIdx == car.CarIDx) {
                                    LeaderCurrentLap = car.Lap;
                                }
                                // TODO: Class leader logic goes here
                            }
                        }
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

                    // calculate the expected for the leader
                   // car.EstimatedLapTime = irData.Telemetry.CarIdxEstTime[LeaderIdx];
                    //  LeaderExpectedLapTime = (LeaderLastLap * 2 + LeaderBestLap) / 3;
                    LeaderExpectedLapTime = irData.Telemetry.CarIdxEstTime[LeaderIdx];


                    // calculate the expected for the current car
                    // Andreas Dahl's Foruyla
                    //car.EstimatedLapTime = (car.LastLap * 2 + car.BestLap) / 3;

                    // this is iRacing's formula - Estimated time to reach current location on track
                    car.EstimatedLapTime = irData.Telemetry.CarIdxEstTime[i];
                  

                    i++;
                }

                // After the Loop calculate everything

                LeaderTrackDistancePercent = CompetingCars[LeaderIdx].LapDistancePercent;
                UpdateGapTiming();

                foreach (var car in CompetingCarsSortedbyGapToLeader) {
                    string iString = string.Format("{0:00}", car.Position);
                    SetProp("Standings.Overall.Position" + iString + ".Position", car.Position);
                    SetProp("Standings.Overall.Position" + iString + ".Number", car.CarNumber);
                    SetProp("Standings.Overall.Position" + iString + ".DriverName", car.Driver.DriverDisplayName);
                    SetProp("Standings.Overall.Position" + iString + ".GapToLeader", car.GapBehindLeader);
                    SetProp("Standings.Overall.Position" + iString + ".GapToCarAhead", car.IntervalGap);
                    SetProp("Standings.Overall.Position" + iString + ".IsInPit", car.PitInPitLane);

                }
            }

        }

        public void AddStandingsRelatedProperties() {
            if (Settings.EnableStandings) {
                for (int i = 1; i < iRacingMaxCars + 1; i++) {
                    string iString = string.Format("{0:00}", i);
                    AddProp("Standings.Overall.Position" + iString + ".Position", 0);
                    AddProp("Standings.Overall.Position" + iString + ".Number", 0);
                    AddProp("Standings.Overall.Position" + iString + ".DriverName", string.Empty);
                    AddProp("Standings.Overall.Position" + iString + ".GapToLeader", 0);
                    AddProp("Standings.Overall.Position" + iString + ".GapToCarAhead", 0);
                    AddProp("Standings.Overall.Position" + iString + ".IsInPit", 0);
                }
            }
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

        public class CarClass {
            public int CarClassID { get; set; } = 0;
            public string CarClassName { get; set; } = string.Empty;
            public string CarClassColour { get; set; } = string.Empty;
            public string CarClassDisplayName { get; set; } = string.Empty;
        }

        public class RaceCar {

            public TrackSections track;

            public RaceCar() {
                this.Driver = new Driver();
                this.track = new TrackSections();
            }







            //  update the sector time by adding up all the secctions in the current sector
            // All cars sectors should now be updated


            public int CarIDx { get; set; } = int.MinValue;
            public int CarNumber { get; set; } = int.MinValue;   
            public long CarClass { get; set; } = long.MinValue;
            public Driver Driver { get; set; } = null;
            public int CurrentTrackSection { get; set; } = 0;


            // Lap timing info
            public double BestLap { get; set; } = 0;
            public double LastLap { get; set; } = 0;
            public int Lap { get; set; } = 0;



            public double BestLapSector1 { get; set; } = 0;
            public double BestLapSector2 { get; set; } = 0;
            public double BestLapSector3 { get; set; } = 0;
            public double CurrentSector1Time { get; set; } = 0;
            public double CurrentSector2Time { get; set; } = 0;
            public double CurrentSector3Time { get; set; } = 0;
            public int CurrentSectorNumber { get; set; } = 0;
            public double EstimatedLapTime { get; set; } = 0;

            public double IntervalGap { get; set; } = 0;
            public double IntervalGapDelayed { get; set; } = 0;
            public double LapDistancePercent { get; set; } = 0;
            public int LapsBehindLeader { get; set; } = 0;
            public double GapBehindLeader { get; set; } = 0;
            public int LapsBehindNext { get; set; } = 0;
            public int Position { get; set; } = 0;
            public int PositionInClass { get; set; } = 0;
            public int PositionsGainedLost { get; set; } = 0;
            public int SpeedCurrent { get; set; } = 0;
            public int SpeedMax { get; set; } = 0;
            public double TimeBehindLeader { get; set; } = 0;
            public double TimeBehindNext { get; set; } = 0;
            public int TotalLaps { get; set; } = 0;
            public int LapsDown { get; set; } = 0;


            // Pit info
            public bool PitInPitBox { get; set; } = false;
            public int PitInPitLane { get; set; } = 0;
            public int PitLastLapPitted { get; set; } = 0;
            public int PitLastStopDuration { get; set; } = 0;

            public int PitCount { get; set; } = 0;


            // Flags
            public bool HasFinished { get; set; } = false;
            public bool HasRetired { get; set; } = false;
            public bool HasBlueFlag { get; set; } = false;
            public bool HasOfftrack { get; set; } = false;
            public int IncidentCount { get; set; } = 0;
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

        // Used for teams races
        //internal class Team {}

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
            static int NumberOfSections = 60;
            public TrackSector(int trackSectionID) {

            }




            public int TrackSectionID { get; set; }
            public double TrackDistancePercent { get; set; }
            public double TrackSectionTime { get; set; }

            public void Reset() {
                TrackSectionTime = 0;
            }
        }

    }
}
