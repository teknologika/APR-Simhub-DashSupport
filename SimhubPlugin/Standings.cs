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
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Markup.Localizer;

namespace APR.DashSupport {

    public partial class APRDashPlugin : IPlugin, IDataPlugin, IWPFSettingsV2 {

        public List<RaceCar> CompetingCars = new List<RaceCar>();
        public double LeaderLastLap { get; set; } = 0;
        public double LeaderBestLap { get; set; } = 0;
        public int LeaderCurrentLap { get; set; } = 0;
        public double LeaderTrackPosition { get; set; } = 0;

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


        public void AddStandingsRelatedProperties() {


        }

        public void UpdateStandingsRelatedProperties(ref GameData data) {

            // The cars are all stored in the iRacing Data structures in CarIDx order
            // lets start by getting all the drivers and putting their info into a list of cars

            // Add Standings Properties
            if (Settings.EnableStandings) {

                SessionData._DriverInfo._Drivers[] competitiors = irData.SessionData.DriverInfo.CompetingDrivers;
                CompetingCars = new List<RaceCar>();
                for (int i = 0; i < competitiors.Length; i++) {

                    RaceCar car = new RaceCar();
                    car.CarIDx = i;
                    car.CarClass = competitiors[i].CarClassID;
                    car.Driver.DriverFullName = competitiors[i].UserName;
                    car.Driver.DriverCustomerID = competitiors[i].UserID;

                    // chop up the drivers name(s)
                    // TODO: Don't store everything, just chop once and store what we need ... maybe
                    string[] names = car.Driver.DriverFullName.Split(' ');
                    int numberOfNames = names.Length;
                    if (numberOfNames > 0) {

                        car.Driver.DriverFirstName = car.Driver.DriverFullName.Split(' ')[0];
                        car.Driver.DriverLastName = car.Driver.DriverFullName.Split(' ')[numberOfNames - 1];

                        car.Driver.DriverFirstNameShort = car.Driver.DriverFullName.Substring(0, 3).ToUpper();
                        car.Driver.DriverLastNameShort = car.Driver.DriverLastName.Substring(0, 3).ToUpper();

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

                    // Get Position
                    car.Position = irData.Telemetry.CarIdxPosition[i];
                    car.PositionInClass = irData.Telemetry.CarIdxClassPosition[i];
                    car.Lap = irData.Telemetry.CarIdxLap[i];
                    car.LapDistancePercent = irData.Telemetry.CarIdxLapDistPct[i];

                    bool isLeader = car.Position == 1;
                    bool isClassLeader = car.PositionInClass == 1;

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
                                if (isLeader) {
                                    LeaderBestLap = car.BestLap;
                                }
                                if (isClassLeader) {
                                    //TODO: Add class stuff here
                                }
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
                                if (isLeader) {
                                    LeaderLastLap = car.LastLap;
                                }
                                if (car.PositionInClass == 1) {
                                    //TODO: Add class stuff here
                                }
                            }
                        }
                    }

                    // calculate the expected for the leader
                    double leaderExpectedLapTime = (LeaderLastLap * 2 + LeaderBestLap) / 3;
                    if (LeaderLastLap == 0) {
                        leaderExpectedLapTime = LeaderBestLap * 1.01;
                    }
                    if (LeaderBestLap == 0) {
                        leaderExpectedLapTime = LeaderLastLap;
                    }


                    // calculate the expected for the current car
                    // Andreas Dahl's Foruyla
                    //car.EstimatedLapTime = (car.LastLap * 2 + car.BestLap) / 3;

                    // this is iRacing's formula - Estimated time to reach current location on track
                    car.EstimatedLapTime = irData.Telemetry.CarIdxEstTime[i];

                    if (LeaderLastLap == 0) {
                        leaderExpectedLapTime = LeaderBestLap * 1.01;
                    }
                    if (LeaderBestLap == 0) {
                        leaderExpectedLapTime = LeaderLastLap;
                    }

                    car.UpdateGapTiming(ref CompetingCars, ref irData);


                    CompetingCars.Insert(i, car);
                }

                //   if (GameData.PlayerName == irData.SessionData.DriverInfo.CompetingDrivers[i].UserName) {
                //  myClassColor = irData.SessionData.DriverInfo.CompetingDrivers[i].CarClassColor;
                //   myClassColorIndex = classColors.IndexOf(myClassColor);
                //   myIR = Convert.ToInt32(irData.SessionData.DriverInfo.CompetingDrivers[i].IRating);
                //   break;
                // }

            }

            //AddProp("Standings.test", Settings.DriverNameStyle);

            // int length = 63; // number of cars
            // for (int i = 0; i < length; i++) {
            //   AddProp("Standings.test" + i.ToString(), Settings.DriverNameStyle);
            //  }

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

        public void UpdateGapTiming(ref List<RaceCar> CompetingCars, ref DataSampleEx irData) {

            // loop through the cars
            for (int i = 0; i < CompetingCars.Count; i++) {
                // TODO : check the car is on track

                // Get the current TrackDistancePercent

                EstimatedLapTime = irData.Telemetry.CarIdxEstTime[i];

                // Get the current lap elapsed time
                EstimatedLapTime = irData.Telemetry.CarIdxEstTime[i];
                LapDistancePercent = irData.Telemetry.CarIdxLapDistPct[i];

                if (LapDistancePercent > 0) {

                    CurrentTrackSection = TrackSections.GetATrackSectionForAGivenPercentageAroundTrack(LapDistancePercent);

                    // if the current section time is 0, sector time is the current lap time
                    if (CurrentTrackSection == 0) {
                        CompetingCars[i].track.Sections[CurrentTrackSection].TrackSectionTime = EstimatedLapTime;
                    }

                    // if the section is > 1, the current sector time is the current lap time,
                    // minus all the previous sections added up
                    else {
                        // Add all the previous track sections
                        double cumulativeLapTime = 0;
                        for (int j = 0; j < CurrentTrackSection - 1; j++) {
                            cumulativeLapTime = cumulativeLapTime + track.Sections[j].TrackSectionTime;
                        }
                        
                        CompetingCars[i].track.Sections[CurrentTrackSection].TrackSectionTime = EstimatedLapTime - cumulativeLapTime;
                    }
                }
            }
        }



            

            

            //  update the sector time by adding up all the secctions in the current sector
            // All cars sectors should now be updated
        

        public int CarIDx { get; set; } = int.MinValue;

        public long CarClass { get; set; } = long.MinValue;
        public Driver Driver { get; set; } = null;
        public int CurrentTrackSection { get; private set; } = 0;


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
        public int DriverIRating { get; set; } = 0;
        public int DriverSafetyRating { get; set; } = 0;
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

        public void UpdateTimeForSectionAtPercentage(double trackDistancePercentage, double CurrentElapsedLapTime)
        {
            // we might need to reset here, but for now, let's just keep rolling and overwriting
            int currentSection = GetATrackSectionForAGivenPercentageAroundTrack(trackDistancePercentage);
            if (currentSection == 0) {
                Sections[currentSection].TrackSectionTime = CurrentElapsedLapTime;
            }
            else {
                double timeCurrentlySaved = 0;
                for (int i = 0; i < currentSection -1; i++) {
                    timeCurrentlySaved = timeCurrentlySaved + Sections[i].TrackSectionTime;
                }
                Sections[currentSection].TrackSectionTime = CurrentElapsedLapTime - timeCurrentlySaved;
            }
        }

        public double GetTimeInSection (int section) {
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
            int pct = Convert.ToInt32(Math.Floor( ( (PercentAroundTrack * 100) / div) ));
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
