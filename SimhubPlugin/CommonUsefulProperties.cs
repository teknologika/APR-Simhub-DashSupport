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
            AddProp("Common.Laps.LastLapTime", "");
            AddProp("Common.Laps.ClassBestLapTime", "");
            AddProp("Common.Laps.OverallBestLapTime", "");
            AddProp("Common.Laps.PersonalBestLapTime", "");
            AddProp("Common.Laps.EstimatedLapTime", "");


            AddProp("Dash.Styles.Colors.Lap.SingleDynamic", ""); // Purple
            AddProp("Dash.Styles.Colors.Lap.SessionBest", "#Ff990099"); // Purple
            AddProp("Dash.Styles.Colors.Lap.PersonalBest", "#FF009933"); // Green
            AddProp("Dash.Styles.Colors.Lap.Latest", "#FFFFFFFF"); // white
            AddProp("Dash.Styles.Colors.Lap.Estimated", "#FFFFFFFF"); // white
            AddProp("Dash.Styles.Colors.Lap.Default", "#FFFFFF00"); // yellow

            AddProp("Common.IsLeagueSession", false);

        }

        private void UpdateCommonProperties(GameData data ) {

            SetProp("Common.LapOrTimeString", CurrentLapText);
            SetProp("Common.SessionTypeString", SessionType);
            SetProp("Common.Laps.LastLapTime", LastLapTime);
            SetProp("Common.Laps.ClassBestLapTime", ClassBestLapTime);
            SetProp("Common.Laps.OverallBestLapTime", OverallBestLapTime);
            SetProp("Common.Laps.PersonalBestLapTime", PersonalBestLapTime);
            SetProp("Common.Laps.EstimatedLapTime", EstimatedLapTime);

            SetProp("Dash.Styles.Colors.Lap.SingleDynamic", LastLapDynamicColor);
            SetProp("Dash.Styles.Colors.Lap.SessionBest", "#Ff990099"); // Purple
            SetProp("Dash.Styles.Colors.Lap.PersonalBest", "#FF009933"); // Green
            SetProp("Dash.Styles.Colors.Lap.Latest", "#FFFFFFFF"); // white
            SetProp("Dash.Styles.Colors.Lap.Estimated", "#FFFFFFFF"); // white
            SetProp("Dash.Styles.Colors.Lap.Default", "#FFFFFF00"); // yellow

            SetProp("Common.IsLeagueSession", IsLeagueSession);
        }
    }
}
