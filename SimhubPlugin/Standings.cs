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

    /*    

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
   
            }
        }


        public void InitStandings(ref GameData data) {
            if (Settings.EnableStandings) {
              
            }
        }

        public void UpdateStandingsRelatedProperties(ref GameData data) {

            // Is this a Vets session?
            CheckIfLeagueSession();
            SetProp("General.IsLeagueSession", IsLeagueSession);
            CheckIfV8VetsLeagueSession();
            SetProp("Strategy.Vets.IsVetsSession", IsV8VetsSession);
            SetProp("Strategy.Vets.IsVetsRaceSession", IsV8VetsRaceSession);

            // Get the iRacing Session state
            irData.Telemetry.TryGetValue("SessionState", out object rawSessionState);
            int sessionState = Convert.ToInt32(rawSessionState);


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