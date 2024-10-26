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
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Windows.Documents;
using System.Windows.Markup;
using Opponent = GameReaderCommon.Opponent;

namespace APR.DashSupport {

    public partial class APRDashPlugin : IPlugin, IDataPlugin, IWPFSettingsV2 {

        public string FormatName(string DriverFullName) {
            // chop up the drivers name(s)
            // TODO: Don't store everything, just chop once and store what we need ... maybe
            string[] names = DriverFullName.Split(' ');
            int numberOfNames = names.Length;
            if (numberOfNames > 0) {

                string DriverFirstName = DriverFullName.Split(' ')[0];
                string DriverLastName = DriverFullName.Split(' ')[numberOfNames - 1];
                string DriverFirstNameShort, DriverLastNameShort;

                if (DriverFirstName.Length > 3) {
                    DriverFirstNameShort = DriverFirstName.Substring(0, 3).ToUpper();
                }
                else {
                    DriverFirstNameShort = DriverFirstName;
                }

                if (DriverLastName.Length > 3) {
                    DriverLastNameShort = DriverLastName.Substring(0, 3).ToUpper();
                }
                else {
                    DriverLastNameShort = DriverLastName;
                }

                string DriverFirstNameInitial = DriverFirstName.Substring(0, 1).ToUpper();
                string DriverLastNameInitial = DriverLastName.Substring(0, 1).ToUpper();

                // Update the driver's display name
                switch (Settings.DriverNameStyle) {
                    case 0: // Firstname Middle Lastname
                        return DriverFullName;

                    case 1: // Firstname Lastname
                        return DriverFirstName + " " + DriverLastName;
                       
                    case 2: // Lastname, Firstname
                        return DriverLastName + ", " + DriverFirstName;
                       
                    case 3: // F. Lastname
                        return DriverFirstNameInitial + ". " + DriverLastName;
                  
                    case 4: // Firstname L.
                        return DriverFirstName + " " + DriverLastNameInitial + ". ";

                    case 5: // Lastname, F.
                        return DriverLastName + ", " + DriverFirstNameInitial + ". ";

                    case 6: // LAS
                        return DriverLastNameShort;

                    default: //   Firstname Middle Lastname
                        return DriverFullName;
                }
                
            }
            return DriverFullName;
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
            if (Settings.EnableStandings && data != null && data.GameRunning) {

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
               // if ((SessionType == "Practice" || SessionType == "Open Qualify" || SessionType == "Lone Qualify") && sessionState > 3) {
                    //Settings.ColumnShowGapToCarInFront = true;
               // }
              //  else {
                    //Settings.ColumnShowGapToCarInFront = false;
              //  }

                SetProp("Standings.Columns.GapToCarInFront.Left", Settings.ColumnStartGapToCarInFront);
                SetProp("Standings.Columns.GapToCarInFront.Visible", Settings.ColumnShowGapToCarInFront);
                SetProp("Standings.Columns.GapToCarInFront.Width", Settings.ColumnWidthGapToCarInFront);

                SetProp("Standings.Columns.LastLap.Left", Settings.ColumnStartLastLap);
                SetProp("Standings.Columns.LastLap.Width", Settings.ColumnWidthLastLap);
                SetProp("Standings.Columns.LastLap.Visible", Settings.ColumnShowLastLap);

                SetProp("Standings.Columns.FastestLap.Left", Settings.ColumnStartFastestLap);
                SetProp("Standings.Columns.FastestLap.Width", Settings.ColumnWidthFastestLap);
                SetProp("Standings.Columns.FastestLap.Visible", Settings.ColumnShowFastestLap);
                SetProp("Standings.Columns.FastestLap.Slider.Left", Settings.ColumnStartFastestLapSlider);

                if (OpponentsExtended.Count > 0) {

                    // Update the gap to the class leader
                    // Find the overall leader for each class
                    foreach (var item in carClasses) {
                        ExtendedOpponent classLeader = OpponentsExtended.Find(a => a.CarClassID == item.carClassID && a.CarClassLivePosition == 1);
                        if(classLeader == null) {
                            break;
                        }
                        item.LeaderCarIdx = classLeader.CarIdx;
                    }

                    // get the spected car
                    ExtendedOpponent spectator = OpponentsExtended.Find(x => x.CarIdx == irData.Telemetry.CamCarIdx);

                    // only do the spectated driver's class
                    List<ExtendedOpponent> classOpponents = OpponentsInClass(spectator.CarClassID);

                    // Updte Lap time colors
                    // UpdateLapOrTimeString(data);
                    foreach (var item in carClasses) {


                        ExtendedOpponent classLeader = OpponentsExtended.Find(a => a.CarClassID == item.carClassID && a.CarClassLivePosition == 1);
                        if (classLeader == null) {
                            break;
                        }
                        item.LeaderCarIdx = classLeader.CarIdx;
                    }


                    classOpponents = classOpponents.FindAll(x => x.IsConnected).OrderBy(x => x.Position).ToList();
                    int counter = 1;
                    for (int i = 0; i < classOpponents.Count; i++) {
                        ExtendedOpponent item = classOpponents[i];

                        SetProp("Standings.Position" + counter.ToString() + ".Position", item.PositionString);
                        SetProp("Standings.Position" + counter.ToString() + ".PositionsGained", item._opponent.RacePositionGain);
                        SetProp("Standings.Position" + counter.ToString() + ".LivePosition", item.LivePosition);
                        SetProp("Standings.Position" + counter.ToString() + ".Class.ClassId", item.CarClassID);
                        SetProp("Standings.Position" + counter.ToString() + ".Class.Name", item.CarClass);
                        SetProp("Standings.Position" + counter.ToString() + ".Class.Color", item.CarClassColor);
                        SetProp("Standings.Position" + counter.ToString() + ".Class.TextColor", item.CarClassTextColor);
                        SetProp("Standings.Position" + counter.ToString() + ".Class.Position", item.Position);
                        SetProp("Standings.Position" + counter.ToString() + ".Class.PositionsGained", item._opponent.RacePositionClassGain);
                        SetProp("Standings.Position" + counter.ToString() + ".Class.LivePosition", item.LivePosition);
                        SetProp("Standings.Position" + counter.ToString() + ".Class.GapToLeader", item.GapToClassLeaderString);
                        SetProp("Standings.Position" + counter.ToString() + ".Class.GapToCarAhead", item.ClassAheadInClassGapString);
                        SetProp("Standings.Position" + counter.ToString() + ".Number", item.CarNumber);
                        SetProp("Standings.Position" + counter.ToString() + ".Name", FormatName(item.Name));
                        SetProp("Standings.Position" + counter.ToString() + ".DriverName", item.DriverName);
                        SetProp("Standings.Position" + counter.ToString() + ".TeamName", item.TeamName);
                        SetProp("Standings.Position" + counter.ToString() + ".GapToLeader", item.GapToClassLeaderString);
                        SetProp("Standings.Position" + counter.ToString() + ".GapToCarAhead", item.ClassAheadInClassGapString);
                        SetProp("Standings.Position" + counter.ToString() + ".GapToCarBehind", item.CarBehindInClassGapString);
                        SetProp("Standings.Position" + counter.ToString() + ".IsInPit", item.IsCarInPit);
                        SetProp("Standings.Position" + counter.ToString() + ".IsPlayer", item.IsSpectator);
                        SetProp("Standings.Position" + counter.ToString() + ".Lap.LastLap", item.LastLapTimeString);
                        SetProp("Standings.Position" + counter.ToString() + ".Lap.BestLap", item.BestLapTimeString);
                        SetProp("Standings.Position" + counter.ToString() + ".Lap.Colors.LastLap", item.LastLapDynamicColor);
                        SetProp("Standings.Position" + counter.ToString() + ".Lap.Colors.BestLap", item.BestLapDynamicColor);
                        SetProp("Standings.Position" + counter.ToString() + ".LapsBehindLeader", item.LapsToLeader);
                        SetProp("Standings.Position" + counter.ToString() + ".LastLapIsPersonalBestLap", item.IsLastLapPersonalBest);
                        SetProp("Standings.Position" + counter.ToString() + ".BestLapIsClassBestLap", item.IsBestLapClassBestLap);
                        SetProp("Standings.Position" + counter.ToString() + ".BestLapIsOverallBest", item.IsBestLapOverallBest);
                        SetProp("Standings.Position" + counter.ToString() + ".RowIsVisible", item.DriverName != "");
                        SetProp("Standings.Position" + counter.ToString() + ".CPS1Served", item.CPS1Served);
                        SetProp("Standings.Position" + counter.ToString() + ".CPS2Served", item.CPS2Served);


                        if (item.IsPlayer || item.IsSpectator) {
                            SetProp("Standings.Position" + counter.ToString() + ".Class.Color", Settings.Color_Black);
                            SetProp("Standings.Position" + counter.ToString() + ".Class.TextColor", Settings.Color_White);
                            SetProp("Standings.Position" + counter.ToString() + ".Row.Background", Settings.StandingsBackgroundDriverReferenceRowColourWithTransparency);
                        }
                        else {
                            //SetProp("Standings.Position" + counter.ToString() + ".Class.Color", Settings.Color_Black);
                           // SetProp("Standings.Position" + counter.ToString() + ".Class.TextColor", Settings.Color_White);        
                           SetProp("Standings.Position" + counter.ToString() + ".Row.Background", Settings.StandingsBackgroundRowColourWithTransparency);
                        }

                        counter++;
                    }
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

                AddProp("Standings.Columns.LastLap.Left", Settings.ColumnStartLastLap);
                AddProp("Standings.Columns.LastLap.Width", Settings.ColumnWidthLastLap);
                AddProp("Standings.Columns.LastLap.Visible", Settings.ColumnShowLastLap);

                AddProp("Standings.Columns.FastestLap.Left", Settings.ColumnStartFastestLap);
                AddProp("Standings.Columns.FastestLap.Width", Settings.ColumnWidthFastestLap);
                AddProp("Standings.Columns.FastestLap.Visible", Settings.ColumnShowFastestLap);
                AddProp("Standings.Columns.FastestLap.Slider.Left", Settings.ColumnStartFastestLapSlider);

                for (int i = 1; i < Settings.MaxCars + 1; i++) {
                    //string iString = string.Format("{0:00}", i);
                    string iString = i.ToString();
                    AddProp("Standings.Position" + iString + ".Position", 0);
                    AddProp("Standings.Position" + iString + ".PositionsGained", 0);
                    AddProp("Standings.Position" + iString + ".LivePosition", 0);
                    AddProp("Standings.Position" + iString + ".Class.ClassId", 0);
                    AddProp("Standings.Position" + iString + ".Class.Name", 0);
                    AddProp("Standings.Position" + iString + ".Class.Color", 0);
                    AddProp("Standings.Position" + iString + ".Class.TextColor", 0);
                    AddProp("Standings.Position" + iString + ".Class.Position", 0);
                    AddProp("Standings.Position" + iString + ".Class.PositionsGained", 0);
                    AddProp("Standings.Position" + iString + ".Class.LivePosition", 0);
                    AddProp("Standings.Position" + iString + ".Class.GapToLeader", 0);
                    AddProp("Standings.Position" + iString + ".Class.GapToCarAhead", 0);
                    AddProp("Standings.Position" + iString + ".Number", 0);
                    AddProp("Standings.Position" + iString + ".Name", string.Empty);
                    AddProp("Standings.Position" + iString + ".DriverName", string.Empty);
                    AddProp("Standings.Position" + iString + ".TeamName", string.Empty);
                    AddProp("Standings.Position" + iString + ".GapToLeader", 0);
                    AddProp("Standings.Position" + iString + ".GapToCarAhead", 0);
                    AddProp("Standings.Position" + iString + ".IsInPit", 0);
                    AddProp("Standings.Position" + iString + ".BestLap", 0);
                    AddProp("Standings.Position" + iString + ".LastLap", 0);
                    AddProp("Standings.Position" + iString + ".IsPlayer", false);
                    AddProp("Standings.Position" + iString + ".Lap.LastLap", "-:--:---");
                    AddProp("Standings.Position" + iString + ".Lap.BestLap", "-:--:---");
                    AddProp("Standings.Position" + iString + ".Lap.Colors.LastLap", Settings.Color_LightGrey);
                    AddProp("Standings.Position" + iString + ".Lap.Colors.BestLap", Settings.Color_LightGrey);

                    AddProp("Standings.Position" + iString + ".LapsBehindLeader", "");
                    AddProp("Standings.Position" + iString + ".LastLapIsPersonalBestLap", false);
                    AddProp("Standings.Position" + iString + ".BestLapIsClassBestLap", false);
                    AddProp("Standings.Position" + iString + ".BestLapIsOverallBest", false);

                    AddProp("Standings.Position" + iString + ".RowIsVisible", false);
                    AddProp("Standings.Position" + iString + ".CPS1Served", false);
                    AddProp("Standings.Position" + iString + ".CPS2Served", false);

                }
                
            }
        }

        public class Standings {

        }
    }
}