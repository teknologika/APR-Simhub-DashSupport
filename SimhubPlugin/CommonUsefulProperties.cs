using System;
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
using SimHub.Plugins;
using GameReaderCommon;
using System.Windows.Controls;
using SimHub.Plugins.DataPlugins.PersistantTracker;
using SimHub.Plugins.OutputPlugins.GraphicalDash.Models.BuiltIn;

namespace APR.DashSupport {

    public partial class APRDashPlugin : IPlugin, IDataPlugin, IWPFSettingsV2 {


        // General colours and layout settings
       


        // Current Lap information
        public bool LapDisplayVisibility = false;
        public int CurrentLap;
        public string CurrentLapText = "";

        // Lap information
        public string LastLapTimeText = "";
        public bool LastLapDisplayVisibility = false;

        TimeSpan LastLapTime;
        TimeSpan PersonalBestLapTime;
        TimeSpan ClassBestLapTime;
        TimeSpan OverallBestLapTime;
        TimeSpan EstimatedLapTime;
        string LastLapDynamicColor;
        string BestLapDynamicColor;

        private void UpdateLapOrTimeString(GameData data) {
            if (SessionType != "Offline Testing" && SessionType != "Lone Practice" && SessionType != "Practice") {
                LapDisplayVisibility = true;

                // TODO: Get this for the leading car
                if (IsSpectating) {
                    CurrentLap = LeadingCar.Lap;
                }
                CurrentLap = irData.Telemetry.Lap;
                CurrentLapText =  $"{CurrentLap} / {SessionLapsString}";
            }
            else {
                TimeSpan sessionTimeLeft = data.NewData.SessionTimeLeft;
                if ( sessionTimeLeft.TotalSeconds /60 > 60) {
                    CurrentLapText = sessionTimeLeft.Hours +"h " + sessionTimeLeft.Minutes + "m";
                }
                else {
                    CurrentLapText = sessionTimeLeft.Minutes + "m " + sessionTimeLeft.Seconds + "s";
                }
            }
        }

        private void UpdateLapTimesForDisplay(GameData data) {
            //  return '--:--.---';
            // return isnull(driverbestlap($prop('DataCorePlugin.GameData.BestLapOpponentSameClassPosition') + 1), '0:00.000')
            if (IsSpectating) {
                LastLapTime = SpectatedCar.LastLapTime;
                PersonalBestLapTime = SpectatedCar.BestLapTime;
                EstimatedLapTime = SpectatedCar.LastLapTime;
            }
            else {
                LastLapTime = data.NewData.LastLapTime;
                PersonalBestLapTime = data.NewData.BestLapTime;

                if (GetProp("PersistantTrackerPlugin.EstimatedLapTime_SessionBestBasedSimhub") != null) {
                    EstimatedLapTime = GetProp("PersistantTrackerPlugin.EstimatedLapTime_SessionBestBasedSimhub");
                }
                else if (GetProp("PersistantTrackerPlugin.EstimatedLapTime_AllTimeBestBased") != null) {
                    EstimatedLapTime = GetProp("PersistantTrackerPlugin.EstimatedLapTime_SessionBestBasedSimhub");
                }
                else {
                    EstimatedLapTime = data.NewData.CurrentLapTime;
                }
            }
        }

        private void CheckIfLeagueSession() {
            var leagueID = irData.SessionData.WeekendInfo.LeagueID;
            if (leagueID > 0) {
                IsLeagueSession = true;
            }
            IsLeagueSession = false;
            SetProp("Common.IsLeagueSession", IsLeagueSession);
        }

        private void CreateCommonProperties() {
            AddProp("Common.LapOrTimeString", "");
            AddProp("Common.SessionTypeString", "");
            AddProp("Common.LastLapTime", "");
            AddProp("Common.ClassBestLapTime", "");
            AddProp("Common.OverallBestLapTime", "");
            AddProp("Common.PersonalBestLapTime", "");
            AddProp("Common.EstimatedLapTime", "");

            AddProp("Dash.Styles.Colors.Lap.SingleDynamic", Settings.Color_LightGrey); 
            AddProp("Dash.Styles.Colors.Lap.SessionBest", Settings.Color_Purple); 
            AddProp("Dash.Styles.Colors.Lap.PersonalBest", Settings.Color_Green); 
            AddProp("Dash.Styles.Colors.Lap.Latest", Settings.Color_White);
            AddProp("Dash.Styles.Colors.Lap.Estimated", Settings.Color_White); 
            AddProp("Dash.Styles.Colors.Lap.Default", Settings.Color_Yellow); 
            AddProp("Dash.Styles.Colors.Lap.Default", Settings.Color_Yellow);


            AddProp("Dash.Styles.Colors.VeryDarkGrey", Settings.Color_VeryDarkGrey);
            AddProp("Dash.Styles.Colors.DarkGrey", Settings.Color_DarkGrey);
            AddProp("Dash.Styles.Colors.LightGrey", Settings.Color_LightGrey);
            AddProp("Dash.Styles.Colors.Purple", Settings.Color_Purple);
            AddProp("Dash.Styles.Colors.Green", Settings.Color_Green);
            AddProp("Dash.Styles.Colors.White", Settings.Color_White);
            AddProp("Dash.Styles.Colors.Black", Settings.Color_Black);
            AddProp("Dash.Styles.Colors.Yellow", Settings.Color_Yellow);
            AddProp("Dash.Styles.Colors.Red", Settings.Color_Red);
            AddProp("Dash.Styles.Colors.RedLineFlash", Settings.Color_RedLineFlash);
            AddProp("Dash.Styles.Colors.Transparent", Settings.Color_Transparent);
            AddProp("Dash.Styles.Colors.DarkBackground", Settings.Color_DarkBackground);
            AddProp("Dash.Styles.Colors.LightBlue", Settings.Color_LightBlue);
            AddProp("Dash.Styles.Colors.LimiterOn", Settings.Color_LightBlue);
            AddProp("Dash.Styles.Colors.LimiterWarning", Settings.Color_Red);
            AddProp("Dash.Styles.Colors.IgnitionOff", Settings.Color_Yellow);
            AddProp("Dash.Styles.Colors.EngineOff", Settings.Color_RedLineFlash);

            AddProp("Common.IsLeagueSession", false);

        }


        private void UpdateCommonProperties(GameData data ) {

            SetProp("Common.LapOrTimeString", CurrentLapText);
            SetProp("Common.SessionTypeString", SessionType);
            SetProp("Common.LastLapTime", NiceTime(LastLapTime));
            SetProp("Common.ClassBestLapTime", NiceTime(ClassBestLapTime));
            SetProp("Common.OverallBestLapTime", NiceTime(OverallBestLapTime));
            SetProp("Common.PersonalBestLapTime", NiceTime(PersonalBestLapTime));
            SetProp("Common.EstimatedLapTime", NiceTime(EstimatedLapTime));

            SetProp("Dash.Styles.Colors.Lap.SingleDynamic", LastLapDynamicColor);
            SetProp("Dash.Styles.Colors.Lap.SessionBest", Settings.Color_Purple); // Purple
            SetProp("Dash.Styles.Colors.Lap.PersonalBest", Settings.Color_Green); // Green
            SetProp("Dash.Styles.Colors.Lap.Latest", "#FFFFFFFF"); // white
            SetProp("Dash.Styles.Colors.Lap.Estimated", "#FFFFFFFF"); // white
            SetProp("Dash.Styles.Colors.Lap.Default", Settings.Color_Yellow); // yellow

            SetProp("Common.IsLeagueSession", IsLeagueSession);
        }
    }
}
