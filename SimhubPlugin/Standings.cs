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
            SetProp("Common.IsLeagueSession", IsLeagueSession);
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



                // get the spected car
                ExtendedOpponent spectator = OpponentsExtended.Find(x => x.CarIdx == irData.Telemetry.CamCarIdx);

                // only do the spectated driver's class
                List<ExtendedOpponent> classOpponents = OpponentsInClass(spectator.CarClassID);

                classOpponents = classOpponents.OrderBy(x => x.Position).ToList();
                int counter = 1;
                for (int i = 0; i < classOpponents.Count; i++)
                {
                    ExtendedOpponent item = classOpponents[i];
                    

                    SetProp("Standings.Spectated.Class.Position" + counter.ToString() + ".LivePosition", item.Position);
                    SetProp("Standings.Spectated.Class.Position" + counter.ToString() + ".Live", item.LivePosition);
                    SetProp("Standings.Spectated.Class.Position" + counter.ToString() + ".Class.ClassId", item.CarClassID);
                    SetProp("Standings.Spectated.Class.Position" + counter.ToString() + ".Class.Name", item.CarClass);
                    SetProp("Standings.Spectated.Class.Position" + counter.ToString() + ".Class.Color", item.CarClassColor);
                    SetProp("Standings.Spectated.Class.Position" + counter.ToString() + ".Class.TextColor", item.CarClassColor);
                    SetProp("Standings.Spectated.Class.Position" + counter.ToString() + ".Class.Position", item.Position);
                    SetProp("Standings.Spectated.Class.Position" + counter.ToString() + ".Class.LivePosition", item.LivePosition);
                    SetProp("Standings.Spectated.Class.Position" + counter.ToString() + ".Class.GapToLeader", item.GapToClassLeaderString);
                    SetProp("Standings.Spectated.Class.Position" + counter.ToString() + ".Class.GapToCarAhead", item.GapToPositionInClassAheadString);
                    SetProp("Standings.Spectated.Class.Position" + counter.ToString() + ".Number", item.CarNumber);
                    SetProp("Standings.Spectated.Class.Position" + counter.ToString() + ".DriverName", item.DriverName);
                    SetProp("Standings.Spectated.Class.Position" + counter.ToString() + ".GapToLeader", item.GapToClassLeaderString);
                    SetProp("Standings.Spectated.Class.Position" + counter.ToString() + ".GapToCarAhead", item.GapToPositionInClassAheadString);
                    SetProp("Standings.Spectated.Class.Position" + counter.ToString() + ".IsInPit", item.IsCarInPit);
                    SetProp("Standings.Spectated.Class.Position" + counter.ToString() + ".BestLap", item.BestLapTime);
                    SetProp("Standings.Spectated.Class.Position" + counter.ToString() + ".LastLap", item.LastLapTime);
                    SetProp("Standings.Spectated.Class.Position" + counter.ToString() + ".IsPlayer", item.IsSpectator);
                    SetProp("Standings.Spectated.Class.Position" + counter.ToString() + ".LapsBehindLeader", item.LapsToLeader);
                    SetProp("Standings.Spectated.Class.Position" + counter.ToString() + ".LastLapIsPersonalBestLap", false);
                    SetProp("Standings.Spectated.Class.Position" + counter.ToString() + ".LastLapIsOverallBestLap", false);
                    SetProp("Standings.Spectated.Class.Position" + counter.ToString() + ".BestLapIsOverallBest", false);
                    SetProp("Standings.Spectated.Class.Position" + counter.ToString() + ".RowIsVisible", item.DriverName != "");
                    SetProp("Standings.Overall" + counter.ToString() + ".BestLap", 0);
                    counter++;
                }
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


                for (int i = 1; i < Settings.MaxCars + 1; i++) {
                    //string iString = string.Format("{0:00}", i);
                    string iString = i.ToString();
                    AddProp("Standings.Spectated.Class.Position" + iString + ".Position", 0);
                    AddProp("Standings.Spectated.Class.Position" + iString + ".LivePosition", 0);
                    AddProp("Standings.Spectated.Class.Position" + iString + ".Class.ClassId", 0);
                    AddProp("Standings.Spectated.Class.Position" + iString + ".Class.Name", 0);
                    AddProp("Standings.Spectated.Class.Position" + iString + ".Class.Color", 0);
                    AddProp("Standings.Spectated.Class.Position" + iString + ".Class.TextColor", 0);
                    AddProp("Standings.Spectated.Class.Position" + iString + ".Class.Position", 0);
                    AddProp("Standings.Spectated.Class.Position" + iString + ".Class.LivePosition", 0);
                    AddProp("Standings.Spectated.Class.Position" + iString + ".Class.GapToLeader", 0);
                    AddProp("Standings.Spectated.Class.Position" + iString + ".Class.GapToCarAhead", 0);
                    AddProp("Standings.Spectated.Class.Position" + iString + ".Number", 0);
                    AddProp("Standings.Spectated.Class.Position" + iString + ".DriverName", string.Empty);
                    AddProp("Standings.Spectated.Class.Position" + iString + ".GapToLeader", 0);
                    AddProp("Standings.Spectated.Class.Position" + iString + ".GapToCarAhead", 0);
                    AddProp("Standings.Spectated.Class.Position" + iString + ".IsInPit", 0);
                    AddProp("Standings.Spectated.Class.Position" + iString + ".BestLap", 0);
                    AddProp("Standings.Spectated.Class.Position" + iString + ".LastLap", 0);
                    AddProp("Standings.Spectated.Class.Position" + iString + ".IsPlayer", false);
                    SetProp("Standings.Spectated.Class.Position" + iString + ".LapsBehindLeader", 0);
                    AddProp("Standings.Spectated.Class.Position" + iString + ".LastLapIsPersonalBestLap", false);
                    AddProp("Standings.Spectated.Class.Position" + iString + ".LastLapIsOverallBestLap", false);
                    AddProp("Standings.Spectated.Class.Position" + iString + ".BestLapIsOverallBest", false);
                    AddProp("Standings.Spectated.Class.Position" + iString + ".RowIsVisible", false);
                    AddProp("Standings.Overall" + iString + ".BestLap", 0);
                }
                
            }
        }

        public class Standings {

        }
    }
}