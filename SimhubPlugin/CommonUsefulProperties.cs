using System;
using SimHub.Plugins.OutputPlugins.Dash.GLCDTemplating;
using SimHub.Plugins.OutputPlugins.GraphicalDash.Behaviors.DoubleText.Imp;
using SimHub.Plugins.OutputPlugins.GraphicalDash.Models;
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
using APR.DashSupport.Themes;
using iRacingSDK;

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
            else {
                IsLeagueSession = false;
            }
            
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

            AddProp("Dash.Styles.Colors.Lap.SingleDynamic", IRacing.Colors.GreyLightText); 
            AddProp("Dash.Styles.Colors.Lap.SessionBest", IRacing.Colors.GreyLightText); 
            AddProp("Dash.Styles.Colors.Lap.PersonalBest", IRacing.Colors.GreyLightText); 
            AddProp("Dash.Styles.Colors.Lap.Latest", IRacing.Colors.RelativeTextWhite);
            AddProp("Dash.Styles.Colors.Lap.Estimated", IRacing.Colors.RelativeTextWhite); 
            AddProp("Dash.Styles.Colors.Lap.Default", IRacing.Colors.Yellow); 
            AddProp("Dash.Styles.Colors.Lap.Default", IRacing.Colors.Yellow);

            AddProp("Dash.Styles.Colors.VeryDarkGrey", IRacing.Colors.GreyBackgroundVeryDarkGrey);
            AddProp("Dash.Styles.Colors.DarkGrey", IRacing.Colors.GreyBackgroundDarkGrey);
            AddProp("Dash.Styles.Colors.MidGrey", IRacing.Colors.GreyBackgroundMidGrey);
            AddProp("Dash.Styles.Colors.LightGrey", IRacing.Colors.GreyLight);
            AddProp("Dash.Styles.Colors.GreyText", IRacing.Colors.GreyLightText);
            AddProp("Dash.Styles.Colors.Purple", IRacing.Colors.Purple);
            AddProp("Dash.Styles.Colors.Green", IRacing.Colors.Green);
            AddProp("Dash.Styles.Colors.GreenText", IRacing.Colors.GreenText);

            AddProp("Dash.Styles.Colors.White", IRacing.Colors.RelativeTextWhite);
            
            AddProp("Dash.Styles.Colors.Black", IRacing.Colors.Black);
            AddProp("Dash.Styles.Colors.Yellow", IRacing.Colors.Yellow);
            AddProp("Dash.Styles.Colors.YellowText", IRacing.Colors.YellowText);
            AddProp("Dash.Styles.Colors.Red", IRacing.Colors.Red);
            AddProp("Dash.Styles.Colors.RedLineFlash", IRacing.Colors.RelativeTextRed);
            AddProp("Dash.Styles.Colors.Transparent", IRacing.Colors.Transparent);
            AddProp("Dash.Styles.Colors.DarkBackground", IRacing.Colors.GreyBackgroundVeryDarkGrey);
            AddProp("Dash.Styles.Colors.LightBlue", IRacing.Colors.LightBlue);
            AddProp("Dash.Styles.Colors.LimiterOn", IRacing.Colors.LightBlue);
            AddProp("Dash.Styles.Colors.LimiterWarning", IRacing.Colors.RelativeTextBlue);
            AddProp("Dash.Styles.Colors.IgnitionOff", IRacing.Colors.YellowText);
            AddProp("Dash.Styles.Colors.EngineOff", IRacing.Colors.RelativeTextRed);

            


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
            SetProp("Dash.Styles.Colors.Lap.SessionBest", IRacing.Colors.Purple); // Purple
            SetProp("Dash.Styles.Colors.Lap.PersonalBest", IRacing.Colors.Green); // Green
            SetProp("Dash.Styles.Colors.Lap.Latest", "#FFFFFFFF"); // white
            SetProp("Dash.Styles.Colors.Lap.Estimated", "#FFFFFFFF"); // white
            SetProp("Dash.Styles.Colors.Lap.Default", IRacing.Colors.Yellow); // yellow
            
            SetProp("Common.IsLeagueSession", IsLeagueSession);
        }
    }
}
