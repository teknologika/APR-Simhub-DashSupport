using GameReaderCommon;
using SimHub.Plugins;
using System;
using System.Windows.Media;
using IRacingReader;
using iRacingSDK;
using System.Diagnostics.Eventing.Reader;
using System.Windows.Markup;
using System.Net.Http.Headers;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using MahApps.Metro.Controls;
using SimRaceX.Telemetry.Comparer.Model;
using SimHub.Plugins.UI.DeviceScannerModels;
using System.Collections.Generic;
using SimHub.Plugins.DataPlugins.PersistantTracker;
using System.Linq;
using System.Xml.Linq;
using Newtonsoft.Json;
using System.Windows.Media.Animation;
using static iRacingSDK.SessionData._RadioInfo;
using static APR.DashSupport.APRDashPlugin;
using SimHub.Plugins.OutputPlugins.GraphicalDash.Behaviors.DoubleText;
using System.Runtime.InteropServices.ComTypes;
using FMOD;
using static SimHub.Plugins.DataPlugins.PersistantTracker.PersistantTrackerPluginAttachedData;
using SimHub.Plugins.OutputPlugins.GraphicalDash.Models;

namespace APR.DashSupport
{

    [PluginDescription("Support for APR Dashes and Overlays")]
    [PluginAuthor("Bruce McLeod")]
    [PluginName("APR Dash Support")]
    public partial class APRDashPlugin : IPlugin, IDataPlugin, IWPFSettingsV2
    {
        public DashPluginSettings Settings;

        // Timers
        public int frameCounter = 0;

        DateTime now;
        private long time;

        private long endTime1Sec;
        private readonly int every1sec = 10000000;
        private bool runEvery1Sec;


        private long endTime5Sec;
        private readonly int every5sec = 50000000;
        private bool runEvery5Sec;


        private bool IsRaceSession;


        DataSampleEx irData;
       
        public PluginManager PluginManager { get; set; }
        public ImageSource PictureIcon => this.ToIcon(Properties.Resources.sdkmenuicon);
        public string LeftMenuTitle => "APR Dash Support";
        public int PreviousSessionState = 0;
        public double PreviousSessionTick;
        public long PreviousSessionID;
        public string SessionType;
        public bool IsV8VetsSession = false;
        public bool IsV8VetsRaceSession = false;
        public bool IsUnderSafetyCar = false;
        public string[] V8VetsSafetyCarNames = { "BMW M4 GT4", "Mercedes AMG GT3" };
        public bool IsIRacingAdmin = false;

        public bool LogTelemetery = false;
        public string _telemFeed = "";
        public bool lineCrossed = false;
        private double _lineCrossThreshold = 0.2;
        private List<TelemetryData> _CurrentLapTelemetry;
        private CarTrackTelemetry _LastLapTelemetery;
        public double trackPosition = 0.0;
        public bool IsFixedSetupSession = false;
        public double SteeringAngle;
        public int IncidentCount = 0;
        public bool CurrentLapHasIncidents;
        public bool IsCurrentLapValid = true;

        // This is used for sending messages
        public string DriverAheadId = string.Empty;
        public string DriverAheadName = string.Empty;
        public string DriverBehindId = string.Empty;
        public string DriverBehindName = string.Empty;

        

        // this is used for something :-)
        /*
        
        private List<Opponent> opponentsOld;
        private List<Opponent> opponentsClass;
        private List<Opponent> opponentsAhead;
        private List<Opponent> opponentsBehind;
        private List<Opponent> opponentsAheadInClass;
        private List<Opponent> opponentsBehindInClass;
        private List<ExtendedOpponent> OpponentsExtended;

        
        private readonly Dictionary<int, int> opponentsInClassCarIdx = new Dictionary<int, int>();
        private readonly Dictionary<int, int> opponentsAheadCarIdx = new Dictionary<int, int>();
        private readonly Dictionary<int, int> opponentsBehindCarIdx = new Dictionary<int, int>();
        private readonly Dictionary<int, int> opponentsAheadInClassCarIdx = new Dictionary<int, int>();
        private readonly Dictionary<int, int> opponentsBehindInClassCarIdx = new Dictionary<int, int>();

        */

      

            // Calculate the lap difference
            
            

                       




        public class relative {
            public int position;
            public double trackPositionPercent;
            public bool onPitRoad;
            public double relativeGap;

            public override string ToString() {
                return "P: " + position + " " + trackPositionPercent + " " + onPitRoad;
            }
        }

   
        public class holder {
            public long CarIdx;
            public double value;
        }

        public List<relative> Relatves = new List<relative>();

        public float GetTrackLength() {
            if (irData != null) {
                return float.Parse(irData.SessionData.WeekendInfo.TrackLength.Split(' ')[0]) * 1000;
            }
            else {
                return 0;
            }
            
        }

        private void UpdateRelatives(GameData data) {

            this.opponents = data.NewData.Opponents;

            SessionData._DriverInfo._Drivers[] competitors = irData.SessionData.DriverInfo.CompetingDrivers;
            this.opponents = data.NewData.Opponents;
            this.OpponentsExtended = new List<ExtendedOpponent>();
            

            // Get the Spectated car info
            int spectatedCarIdx = irData.Telemetry.CamCarIdx;
            float spectatedCarLapDistPct = irData.Telemetry.CarIdxLapDistPct[spectatedCarIdx];
            float spectatedCarEstTime = irData.Telemetry.CarIdxEstTime[spectatedCarIdx];
            int spectatedCarCurrentLap = irData.Telemetry.CarIdxLap[spectatedCarIdx];
            

            for (int i = 0; i < competitors.Length; ++i) {
                for (int j = 0; j < opponents.Count; ++j) {
                    // Add the aligned Opponents and Competitor data to our ExtendedOpponent list
                    if (string.Equals(competitors[i].CarNumber, opponents[j].CarNumber)) {

                        // Add to the Extended Opponents class
                        OpponentsExtended.Add(new ExtendedOpponent() {
                            _opponent = opponents[j],
                            _competitor = competitors[i],
                            _trackLength = trackLength,
                            _spectatedCarCurrentLap = spectatedCarCurrentLap,
                            _specatedCarLapDistPct = spectatedCarLapDistPct,
                            LicenseColor = LicenseColor(opponents[j].LicenceString)
                        });

                       
                        // Update the car class info
                        CheckAndAddCarClass((int)competitors[i].CarClassID, competitors[i].CarClassShortName);

                    }
                }
            }
     
            // update car reference lap time
            foreach (var item in OpponentsExtended) {
                item.CarClassReferenceLapTime = GetReferenceClassLaptime(item.CarClassID);
            }

            if (IsRaceSession) {
                // update iRating gain / loss
                foreach (var item in OpponentsExtended) {
                    item.iRatingChange = CalculateMultiClassIREstimation(item);
                }
            }

            ClearRelatives();

            var bob = this.OpponentsInClass();
            var tim = this.GetReferenceClassLaptime();
            var fred = this.RelativeGapToSpectatedCar(0);
            var ahead = this.OpponentsAhead;
            var behind = this.OpponentsBehind;

            

            int count = 1;
            foreach (var opponent in OpponentsAhead) {
                SetProp("Relative.Ahead." + count + ".Position", opponent.Position.ToString());
                SetProp("Relative.Ahead." + count + ".Name", opponent.DriverName);
                SetProp("Relative.Ahead." + count + ".Distance", Math.Abs(opponent.LapDistSpectatedCar).ToString("0.0"));
                SetProp("Relative.Ahead." + count + ".Gap", Math.Abs(opponent.GapSpectatedCar).ToString("0.0"));
                SetProp("Relative.Ahead." + count + ".AheadBehind", opponent.AheadBehind.ToString());


                SetProp("Relative.Ahead." + count + ".DriverNameColor", opponent.DriverNameColour);
                SetProp("Relative.Ahead." + count + ".CarClassColor", opponent.CarClassColor);
                SetProp("Relative.Ahead." + count + ".CarClassTextColor", opponent.CarClassTextColor);
                SetProp("Relative.Ahead." + count + ".LicenseColor", opponent.LicenseColor);

                SetProp("Relative.Ahead." + count + ".SR", opponent.SafetyRating);
                SetProp("Relative.Ahead." + count + ".IR", opponent.iRatingString);
                SetProp("Relative.Ahead." + count + ".IRChange", opponent.iRatingChange);
                SetProp("Relative.Ahead." + count + ".PitInfo", opponent.PitInfo);



                count++;
            }

            count = 1;
            foreach (var opponent in OpponentsBehind) {
                SetProp("Relative.Behind." + count + ".Position", opponent.Position.ToString());
                SetProp("Relative.Behind." + count + ".Name", opponent.DriverName);
                SetProp("Relative.Behind." + count + ".Distance", Math.Abs(opponent.LapDistSpectatedCar).ToString("0.0"));
                SetProp("Relative.Behind." + count + ".Gap", Math.Abs(opponent.GapSpectatedCar).ToString("0.0"));
                SetProp("Relative.Behind." + count + ".AheadBehind", opponent.AheadBehind.ToString());

                SetProp("Relative.Behind." + count + ".DriverNameColor", opponent.DriverNameColour);
                SetProp("Relative.Behind." + count + ".CarClassColor", opponent.CarClassColor);
                SetProp("Relative.Behind." + count + ".CarClassTextColor", opponent.CarClassTextColor);
                SetProp("Relative.Behind." + count + ".LicenseColor", opponent.LicenseColor);

                SetProp("Relative.Behind." + count + ".SR", opponent.SafetyRating);
                SetProp("Relative.Behind." + count + ".IR", opponent.iRatingString);
                SetProp("Relative.Behind." + count + ".IRChange", opponent.iRatingChange);
                SetProp("Relative.Behind." + count + ".PitInfo", opponent.PitInfo);
                count++;
            }

            ExtendedOpponent spectator = OpponentsExtended[spectatedCarIdx];
            SetProp("Relative.Spectated.Position", spectator.Position.ToString());
            SetProp("Relative.Spectated.Gap", 0.0);
            SetProp("Relative.Spectated.AheadBehind", "0");

            SetProp("Relative.Spectated.SR", spectator.SafetyRating);
            SetProp("Relative.Spectated.IR", spectator.iRatingString);
            SetProp("Relative.Spectated.IRChange", spectator.iRatingChange);

            SetProp("Relative.Spectated.DriverNameColor", spectator.DriverNameColour);
            SetProp("Relative.Spectated.CarClassColor", spectator.CarClassColor);
            SetProp("Relative.Spectated.CarClassTextColor", spectator.CarClassTextColor);
            SetProp("Relative.Spectated.LicenseColor", spectator.LicenseColor);

        }

        public string GetTrackDistPctForPosition(int position) {
            int index = Relatves.FindIndex(a => a.position == position);
            return Relatves[index].trackPositionPercent.ToString("0.00");
        }

        public string GetPositionforDriverAhead(int carAhead, List<ExtendedOpponent> opponentsAhead) {
            return "";
        }

        public string GetGapforDriverAhead(int index, List<ExtendedOpponent> opponentsAhead) {
            index = index - 1;
            if (index + 1 > opponentsAhead.Count || index < 0 ) {
                return "";
            }
 
            var carAhead = opponentsAhead[index];
        
            return RelativeGapToSpectatedCar(carAhead.CarIdx).ToString("0.00");
       
        }


        private void UpdateSessionData(GameData data) {
            SessionType = data.NewData.SessionTypeName;
            PreviousSessionTick = (double)this.PluginManager.GetPropertyValue("DataCorePlugin.GameRawData.Telemetry.SessionTime");
            PreviousSessionID = (long)this.PluginManager.GetPropertyValue("DataCorePlugin.GameRawData.SessionData.WeekendInfo.SessionID");
            IsV8VetsLeagueSession();
            trackLength = GetTrackLength();

        }

        // check if the user can transmit on @RACECONTROL to see if we are an admin
        private void CheckIfiRacingAdmin(GameData data) {
            IsIRacingAdmin = false;
            if (data.NewData != null ) {
                SessionData._RadioInfo._Radios[] radios = this.irData.SessionData.RadioInfo.Radios;
                foreach (var radio in radios) {
                    SessionData._RadioInfo._Radios._Frequencies[] frequencies = radio.Frequencies;
                    foreach (var frequency in frequencies) {
                        if (frequency.FrequencyName.Contains("RACECONTROL")) {
                            if (frequency.CanSquawk == 1) {
                                IsIRacingAdmin = true;
                                break;
                            }
                        }
                    }
                }
            }         
        }


        private void IsV8VetsLeagueSession() {
            var leagueID = (long)this.PluginManager.GetPropertyValue("DataCorePlugin.GameRawData.SessionData.WeekendInfo.LeagueID");

            if ((leagueID == 6455) || (leagueID == 10129) || (leagueID == 6788)) {
                IsV8VetsSession = true;
            }
            else {
                IsV8VetsSession = false;
            }

            if (IsV8VetsSession) {
                if (SessionType == "Race") {
                    IsV8VetsRaceSession = true;
                }
                else {
                    IsV8VetsRaceSession = false;
                }
            }

        }


        private void CheckIfUnderSafetyCar() {
            
            int paceMode = Convert.ToInt32(PluginManager.GetPropertyValue("DataCorePlugin.GameRawData.Telemetry.PaceMode"));
            if (paceMode == 2 ) {
                IsUnderSafetyCar = true;
                SetProp("Strategy.Indicator.UnderSC", IsUnderSafetyCar);
                return;
            }
            else {
                if (IsV8VetsRaceSession) {
                    for (int i = 1; i < Settings.MaxCars; i++) {
                        string carName = (string)this.PluginManager.GetPropertyValue($"IRacingExtraProperties.iRacing_Leaderboard_Driver_{i:00}_CarName");
                        if (V8VetsSafetyCarNames.Contains(carName)) {
                            bool isInPit = (bool)this.PluginManager.GetPropertyValue($"IRacingExtraProperties.iRacing_Leaderboard_Driver_{i:00}_IsInPit");
                            if (!isInPit) {
                                IsUnderSafetyCar = true;
                                SetProp("Strategy.Indicator.UnderSC", IsUnderSafetyCar);
                                return;
                            }
                        }
                    }
                }
                else {
                    IsUnderSafetyCar = false;
                    SetProp("Strategy.Indicator.UnderSC", IsUnderSafetyCar);
                }
            }
            IsUnderSafetyCar = false;
            SetProp("Strategy.Indicator.UnderSC", IsUnderSafetyCar);
        }



        /// <summary>
        /// Called once after plugins startup
        /// Plugins are rebuilt at game change
        /// </summary>
        /// <param name="pluginManager"></param>
        public void Init(PluginManager pluginManager) {

            SimHub.Logging.Current.Info("Starting APR plugin");


            // Load settings
            Settings = this.ReadCommonSettings<DashPluginSettings>("GeneralSettings", () => new DashPluginSettings());

            // Setup event handlers
            pluginManager.GameStateChanged += new PluginManager.GameRunningChangedDelegate(this.PluginManager_GameStateChanged);
            pluginManager.NewLap += new PluginManager.NewLapDelegate(this.PluginManager_NewLap);


            this.OnSessionChange(pluginManager);

            InitRotaries(pluginManager);
            InitStrategyButtons(pluginManager);


            this.AttachDelegate("EnableBrakeAndThrottleBars", () => Settings.EnableBrakeAndThrottleBars);
            this.AttachDelegate("EnableRPMBar", () => Settings.EnableRPMBar);
            this.AttachDelegate("EnablePitWindowPopup", () => Settings.EnablePitWindowPopup);
            this.AttachDelegate("EnableFuelPopup", () => Settings.EnableFuelPopup);

            this.AttachDelegate("DriverNameStyle_0", () => Settings.DriverNameStyle_0);
            this.AttachDelegate("DriverNameStyle_1", () => Settings.DriverNameStyle_1);



            pluginManager.AddProperty<double>("Version", this.GetType(), 1.1);
            pluginManager.AddProperty<string>("MainMenuSelected", this.GetType(), "none");

            AddProp("BrakeBarColour", "Red");
            AddProp("BrakeBiasColour", "Black");
            AddProp("BrakeBarTargetTrailPercentage",0);
            AddProp("BrakeBarTargetPercentage", 0);

            AddProp("ARBColourFront", "White");
            AddProp("ARBColourRear", "White");
            AddProp("JackerColour", "White");


            AddProp("TCHighValueLabel", "HI AID");
            AddProp("TCLowValueLabel", "OFF");
            AddProp("TCIsOff", false);
            AddProp("TCColour", "White");

            AddProp("ABSColour", "White");
            AddProp("ABSHighValueLabel", "OFF");
            AddProp("ABSLowValueLabel", "HI AID");
            AddProp("ABSIsOff", false);

            AddProp("MAPLabelColour", "White");
            AddProp("MAPLabel", "1");
            AddProp("MAPHighValueLabel", "RACE");
            AddProp("MAPLowValueLabel", "SAVE           SC");

            AddProp("PitWindowMessage", "");
            AddProp("PitWindowTextColour", "Transparent");
            AddProp("pitWindowBackGroundColour", "Transparent");

            AddProp("FuelPopupPercentage", Settings.FuelPopupPercentage);
            AddProp("PitWindowPopupPercentage", Settings.PitWindowPopupPercentage);

            AddProp("LaunchBitePointAdjusted", 0);
            AddProp("LaunchPreferFullThrottleStarts", Settings.PreferFullThrottleStarts);
            AddProp("LaunchUsingDualClutchPaddles", Settings.LaunchUsingDualClutchPaddles);


            AddProp("Strategy.Indicator.StratMode", "A");
            AddProp("Strategy.Indicator.UnderSC", false);
            AddProp("Strategy.Indicator.CPS1Served", false);
            AddProp("Strategy.Indicator.CPS2Served", false);

            AddProp("Strategy.Indicator.RCMode", false);
            AddProp("Strategy.Indicator.isiRacingAdmin", IsIRacingAdmin = false);

            for (int i = 1; i < Settings.RelativeNumberOfCarsAheadToShow+1; i++) {
                AddProp("Relative.Ahead." + i + ".Position", "");
                AddProp("Relative.Ahead." + i + ".Name", "");
                AddProp("Relative.Ahead." + i + ".Distance", "");
                AddProp("Relative.Ahead." + i + ".Gap", "");
                AddProp("Relative.Ahead." + i + ".AheadBehind", "");
                AddProp("Relative.Ahead." + i + ".CarClassTextColor", "");
                AddProp("Relative.Ahead." + i + ".DriverNameColor", "");
                AddProp("Relative.Ahead." + i + ".CarClassColor", "");
                AddProp("Relative.Ahead." + i + ".LicenseColor", "");
                AddProp("Relative.Ahead." + i + ".Show", "False");
                AddProp("Relative.Ahead." + i + ".SR", "False");
                AddProp("Relative.Ahead." + i + ".IR", "False");
                AddProp("Relative.Ahead." + i + ".IRChange", "False");
                AddProp("Relative.Ahead." + i + ".PitInfo", "");
            }

            for (int i = 1; i < Settings.RelativeNumberOfCarsBehindToShow+1; i++) {
                AddProp("Relative.Behind." + i + ".Position","") ;
                AddProp("Relative.Behind." + i + ".Name", "");
                AddProp("Relative.Behind." + i + ".Distance","" );
                AddProp("Relative.Behind." + i + ".Gap", "");
                AddProp("Relative.Behind." + i + ".AheadBehind", "");
                AddProp("Relative.Behind." + i + ".CarClassTextColor", "");
                AddProp("Relative.Behind." + i + ".DriverNameColor", "");
                AddProp("Relative.Behind." + i + ".CarClassColor", "");
                AddProp("Relative.Behind." + i + ".LicenseColor", "");
                AddProp("Relative.Behind." + i + ".Show", "False");
                AddProp("Relative.Behind." + i + ".SR", "");
                AddProp("Relative.Behind." + i + ".IR", "");
                AddProp("Relative.Behind." + i + ".IRChange", "");
                AddProp("Relative.Behind." + i + ".PitInfo", "");
            }

            AddProp("Relative.Spectated.Position","");
            AddProp("Relative.Spectated.Name", "");
            AddProp("Relative.Spectated.Gap", "0.0");
            AddProp("Relative.Spectated.AheadBehind", "");
            AddProp("Relative.Spectated.CarClassTextColor", "");
            AddProp("Relative.Spectated.Distance", "0.0");
            AddProp("Relative.Spectated.DriverNameColor", "#FFFFFF");
            AddProp("Relative.Spectated.CarClassColor", "#000000");
            AddProp("Relative.Spectated.LicenseColor", "#FFFFFF");

            AddProp("Relative.Spectated.Position", "");
            AddProp("Relative.Spectated.Show", "False");
            AddProp("Relative.Spectated.SR", "");
            AddProp("Relative.Spectated.IR", "");
            AddProp("Relative.Spectated.IRChange", "");
            AddProp("Relative.Spectated.PitInfo", "");

            AddProp("Relative.Layout.FontSize", Settings.RelativeFontSize);
            AddProp("Relative.Layout.RowHeight", Settings.RelativeRowHeight);
            AddProp("Relative.Layout.RowOffset", Settings.RelativeRowOffset);

            AddProp("Relative.Layout.NumberOfCarsAhead", Settings.RelativeNumberOfCarsAheadToShow);
            AddProp("Relative.Layout.NumbersOfCarsBehind", Settings.RelativeNumberOfCarsBehindToShow);

            int totalRowHeight = (Settings.RelativeRowHeight + Settings.RelativeRowOffset);
            int headerHeight = 50;
            int headerTop = 0;
            int aheadTop = headerTop + headerHeight + (totalRowHeight * Settings.RelativeNumberOfCarsAheadToShow);
            int spectatorTop = aheadTop + totalRowHeight;
            int behindTop = spectatorTop + totalRowHeight;
            int footerHeight = 50;
            int footerTop = behindTop + (totalRowHeight * Settings.RelativeNumberOfCarsAheadToShow);

            int windowHeight = footerTop + footerHeight;

            AddProp("Relative.Layout.HeaderTop", "0");
            AddProp("Relative.Layout.HeaderHeight", headerHeight);
            AddProp("Relative.Layout.AheadTop", aheadTop);
            AddProp("Relative.Layout.SpectatorTop", spectatorTop);
            AddProp("Relative.Layout.BehindTop", behindTop);
            AddProp("Relative.Layout.FooterTop", footerTop);
            AddProp("Relative.Layout.FooterHeight", footerHeight);
            AddProp("Relative.Layout.WindowHeight", windowHeight);


            //HERE

            //InitRotaryButtons(pluginManager);
            //InitOtherButtons(pluginManager);


            ClearStandings();
            AddStandingsRelatedProperties();

            InitPitCalculations();
        }

        private void ClearRelatives() {
            for (int i = 1; i < Settings.RelativeNumberOfCarsAheadToShow+1; i++) {
                SetProp("Relative.Ahead." + i + ".Position", "");
                SetProp("Relative.Ahead." + i + ".Name", "");
                SetProp("Relative.Ahead." + i + ".Distance", "");
                SetProp("Relative.Ahead." + i + ".Gap", "");
                SetProp("Relative.Ahead." + i + ".DriverNameColor", "");
                SetProp("Relative.Ahead." + i + ".CarClassColor", "");
                SetProp("Relative.Ahead." + i + ".LicenseColor", "");
                SetProp("Relative.Ahead." + i + ".Show", "False");
                SetProp("Relative.Ahead." + i + ".SR", "False");
                SetProp("Relative.Ahead." + i + ".IR", "False");
                SetProp("Relative.Ahead." + i + ".IRChange", "False");
                SetProp("Relative.Ahead." + i + ".PitInfo", "");
            }

            for (int i = 1; i < Settings.RelativeNumberOfCarsBehindToShow+1; i++) {
                SetProp("Relative.Behind." + i + ".Position", "");
                SetProp("Relative.Behind." + i + ".Name", "");
                SetProp("Relative.Behind." + i + ".Distance", "");
                SetProp("Relative.Behind." + i + ".Gap", "");
                SetProp("Relative.Behind." + i + ".DriverNameColor", "");
                SetProp("Relative.Behind." + i + ".CarClassColor", "");
                SetProp("Relative.Behind." + i + ".LicenseColor", "");
                SetProp("Relative.Behind." + i + ".Show", "False");
                SetProp("Relative.Behind." + i + ".SR", "");
                SetProp("Relative.Behind." + i + ".IR", "");
                SetProp("Relative.Behind." + i + ".IRChange", "");
                SetProp("Relative.Behind." + i + ".PitInfo", "");
            }

            SetProp("Relative.Position", "");
            SetProp("Relative.Gap", "");

        }


        /// <summary>
        /// Called one time per game data update, contains all normalized game data,
        /// raw data are intentionnally "hidden" under a generic object type (A plugin SHOULD NOT USE IT)
        ///
        /// This method is on the critical path, it must execute as fast as possible and avoid throwing any error
        ///
        /// </summary>
        /// <param name="pluginManager"></param>
        /// <param name="data">Current game data, including current and previous data frame.</param>
        public void DataUpdate(PluginManager pluginManager, ref GameData data)
        {
            //use a frame counter to not update everything every frame
            // as this is iRacing, this loop runs 60x per second
            frameCounter++;

            // reset the counter every 60hz
            if (frameCounter > 59) {
                frameCounter = 0;
            }

            if (data.GameName != "IRacing") {
                return;
            }

            // Confirm the sim is up and running at it is iRacing


            // Make sure we are getting a telemetry feed
            if (data.GameRunning &&  data.NewData != null)
            {
                //Gaining access to raw data
                if (data?.NewData?.GetRawDataObject() is DataSampleEx) { irData = data.NewData.GetRawDataObject() as DataSampleEx; }

                // Setup timers
                this.time = DateTime.Now.Ticks;
                this.runEvery1Sec = this.time - this.endTime1Sec >= (long)this.every1sec;
                this.runEvery5Sec = this.time - this.endTime5Sec >= (long)this.every5sec;


                // TODO: Add logic to reset everything when the session changes
                int CurrentSessionState = (int)GetProp("DataCorePlugin.GameRawData.Telemetry.SessionState");
                int num = CurrentSessionState % 3 == 0 || CurrentSessionState >= 5 ? PreviousSessionState : CurrentSessionState;
                double SessionTime = (double)GetProp("DataCorePlugin.GameRawData.Telemetry.SessionTime");
                long CurrentSessionID = (long)GetProp("DataCorePlugin.GameRawData.SessionData.WeekendInfo.SessionID");
                bool flag = SessionType == null || this.SessionType != data.NewData.SessionTypeName || SessionTime < PreviousSessionTick || CurrentSessionID != PreviousSessionID ;
                if (flag || this.PreviousSessionState != num) {
                    if (((num == 4 ? 0 : (this.PreviousSessionState != 1 ? 1 : 0)) | (flag ? 1 : 0)) != 0) {
                        DebugMessage("APR: Session reset or session changed.");

                        this.UpdateSessionData(data);
                        SetProp("Strategy.Vets.IsVetsSession", IsV8VetsSession);
                        SetProp("Strategy.Vets.IsVetsRaceSession", IsV8VetsRaceSession);
                        SetProp("Strategy.Indicator.RCMode", Settings.Strategy_RCMode);

                        // Check if we are an iRacing Admin
                        CheckIfiRacingAdmin(data);
                        SetProp("Strategy.Indicator.isiRacingAdmin", IsIRacingAdmin);

                    }
                    this.PreviousSessionState = num;
                    this.IsRaceSession = data.NewData.SessionTypeName == "Race";
                }

                bool sessionStartSetupCompleted = false;
                if (!sessionStartSetupCompleted) {
                    InitStandings(ref data);
                    sessionStartSetupCompleted = true;
                }

                // Frames are used to reduce calculation frequency

                if (frameCounter == 1) {

                    // Timer
                    if (this.runEvery5Sec) {
                        now = DateTime.Now;
                        this.endTime5Sec = now.Ticks;
                    }

                    UpdateStandingsRelatedProperties(ref data);

                    UpdateBrakeBar();
                    trackPosition = irData.Telemetry.LapDistPct;
                    // if we crossed the line, set line cross to true
                    if (trackPosition < 0.02 ) {
                        lineCrossed = true;
                    }
                    // if the threshold is greater set to false
                    if (trackPosition > 0.02)
                    {
                        lineCrossed = false;
                    }
                }

                if (frameCounter == 4) {

                    GetSetupBias();
                    GetSetupTC();
                    GetSetupABS();
                    UpdateFrontARBColour();
                    UpdateRearARBColour();
                  
                    UpdateBitePointRecommendation();
                    UpdatePitWindowMessage();
                    UpdatePopupPositions();
                }

                if (frameCounter == 5) {
                    UpdateTCValues();
                    UpdateABSValues();
                    UpdateMAPValues();
                }


                if (frameCounter == 10) {
                    UpdateBrakeBar();
                    CheckIfUnderSafetyCar();
                }

                if (frameCounter == 20) {
                    UpdateBrakeBar();
                    UpdatePitCalculations(ref data);
                }

                if (frameCounter == 25) {
                    UpdateStrategy();
                    SetProp("Strategy.Indicator.RCMode", Settings.Strategy_RCMode);
                }
                /*
                if (frameCounter == 25) {

                    for (int i = 1; i < Settings.MaxCars ; i++) {

                    }

                    // Write the lap time to the json file
                    /*
                    Lap lastLap = new Lap {
                        SessionID = 1,
                        TrackID = data.NewData.TrackId,
                        CarID = data.NewData.CarId,
                        LapID = data.NewData.CurrentLap - 1,
                        DriverID = data.NewData.PlayerName,
                        LapTime = data.NewData.LastLapTime,
                        TrackState = (string)pluginManager.GetPropertyValue("DataCorePlugin.GameRawData.SessionData.SessionInfo.Sessions01.SessionTrackRubberState"),
                        TrackTemp = data.NewData.RoadTemperature
                    };
                    LapData.AddLapAsync(lastLap);
                    
                }           

                */


                if (frameCounter == 30) {
                    UpdateBrakeBar();

                    // Update driver behind and ahead
                    if (data.NewData.OpponentsAheadOnTrack.Count > 0) {
                        if (data.NewData.OpponentsAheadOnTrack[0].RelativeGapToPlayer < 1.0) {
                            DriverAheadId = data.NewData.OpponentsAheadOnTrack[0].CarNumber;
                            DriverAheadName = data.NewData.OpponentsAheadOnTrack[0].Name.Split(' ')[0];
                        }
                        else {
                            DriverAheadId = string.Empty;
                            DriverAheadName = string.Empty;
                        }
                    }
                    else {
                        DriverAheadId = string.Empty;
                        DriverAheadName = string.Empty;
                    }

                    if (data.NewData.OpponentsBehindOnTrack.Count > 0 ) {
                        if (data.NewData.OpponentsBehindOnTrack[0].RelativeGapToPlayer < 1.0) {
                            DriverBehindId = data.NewData.OpponentsBehindOnTrack[0].CarNumber;
                            DriverBehindName = data.NewData.OpponentsBehindOnTrack[0].Name.Split(' ')[0];
                        }
                        else {
                            DriverBehindId = string.Empty;
                            DriverBehindName = string.Empty;
                        }
                    }
                    else {
                        DriverBehindId = string.Empty;
                        DriverBehindName = string.Empty;
                    }
                }

                if (frameCounter == 40) {
                    UpdateBrakeBar();
                }

                if (frameCounter == 40) {
                    if (runEvery1Sec) {
                        UpdateRelatives(data);
                    }
                }



                if (LogTelemetery) {
                    if ((frameCounter % 3) == 0) {

                        _telemFeed = _telemFeed + TelemeteryDataUpdate(pluginManager, ref data);

                        if ( (data.NewData.ReplayMode == "Replay") || (data.NewData.CurrentLapTime.TotalMilliseconds > 0) ) {
                            if (_CurrentLapTelemetry.Count == 0 || _CurrentLapTelemetry.Last().LapDistance < data.NewData.TrackPositionPercent)
                                _CurrentLapTelemetry.Add(new TelemetryData {
                                    Throttle = data.NewData.Throttle,
                                    Brake = data.NewData.Brake,
                                    Clutch = data.NewData.Clutch,
                                    LapDistance = data.NewData.TrackPositionPercent,
                                    Speed = data.NewData.SpeedKmh,
                                    Gear = data.NewData.Gear,
                                    SteeringAngle = Convert.ToDouble(pluginManager.GetPropertyValue("DataCorePlugin.GameRawData.Telemetry.SteeringWheelAngle")) * -58.0,
                                    LapTime = data.NewData.CurrentLapTime
                                });
                        }
                    }

                    if (frameCounter == 55) {

                        SteeringAngle = Convert.ToDouble(pluginManager.GetPropertyValue("DataCorePlugin.GameRawData.Telemetry.SteeringWheelAngle")) * -58.0;
                        var incidentCount = Convert.ToInt32(pluginManager.GetPropertyValue("DataCorePlugin.GameRawData.Telemetry.PlayerCarMyIncidentCount"));
                        if (incidentCount > IncidentCount) {
                            CurrentLapHasIncidents = true;
                            IncidentCount = incidentCount;
                        }

                        if (lineCrossed) {

                            if (_CurrentLapTelemetry is null)
                                ResetCurrentLapTelemetry();

                            if (_CurrentLapTelemetry != null && _CurrentLapTelemetry.Count > 0 && IsCurrentLapValid) {
                                if (PluginManager.GameName == "IRacing")
                                    IsFixedSetupSession = GetProp("DataCorePlugin.GameRawData.SessionData.DriverInfo.DriverSetupLoadTypeName").ToString() == "fixed";
                                else
                                    IsFixedSetupSession = false;
                                
                                double firstDataDistance = _CurrentLapTelemetry.First().LapDistance;
                                double lastDataDistance = _CurrentLapTelemetry.Last().LapDistance;
                              

                                //   (new System.Collections.Generic.Mscorlib_DictionaryDebugView<string, object>(((GameReaderCommon.StatusData<IRacingReader.DataSampleEx>)data.NewData).RawData.Telemetry).Items[68]).Key
                             
                                //SessionData._DriverInfo._Drivers[] competingDrivers = this.irData.SessionData.DriverInfo.CompetingDrivers;
                           
                                float[] lastLaps = GetProp("DataCorePlugin.GameRawData.Telemetry.CarIdxLastLapTime");
                                double obsTimeInSeconds = lastLaps[irData.Telemetry.CamCarIdx];
                                TimeSpan observerdLapTime =  TimeSpan.FromSeconds(obsTimeInSeconds);
                                var _telemeteryToWrite = _CurrentLapTelemetry;
                                //Try to check if a complete lap has be done
                                if (firstDataDistance < 0.1 && lastDataDistance > 0.9 && data.OldData.IsLapValid && obsTimeInSeconds > 0.0 ) {
                                    var latestLapTelemetry = new CarTrackTelemetry {
                                        GameName = "IRacing",
                                        CarName = data.NewData.CarModel,
                                        TrackName = data.NewData.TrackName,
                                        PlayerName = data.NewData.PlayerName,
                                        TrackCode = data.NewData.TrackCode,
                                        //LapTime = data.NewData.LastLapTime,
                                        LapTime = observerdLapTime,
                                        TelemetryDatas = _telemeteryToWrite,
                                        Created = DateTime.Now,
                                        IsFixedSetup = _IsFixedSetupSession,
                                        PluginVersion = "SimRaceX Telemetry Comparer - v1.6.0.0"
                                    };
                                    latestLapTelemetry.Type = "Personal best";
                                    ExportCarTrackTelemetry(latestLapTelemetry);
                                    _CurrentLapTelemetry = new List<TelemetryData>();

                                }
                            }

                        }

     
                    }

                }
 
               
                // Todo - remove this
                if (frameCounter == 55) {
                    if (LogTelemetery) {
                      //  ProcessWrite(_telemFeed).Wait();
                        _telemFeed = "";
                    }
                }

                if (frameCounter == 50) {
                    UpdateBrakeBar();
                }
            }

            else {
                // Standings support removed 18/06/2023
                ClearStandings();
            }


            pluginManager.SetPropertyValue("MainMenuSelected", this.GetType(), (object)MainMenuValue());
            
        }

        /// <summary>
        /// Called at plugin manager stop, close/dispose anything needed here !
        /// Plugins are rebuilt at game change
        /// </summary>
        /// <param name="pluginManager"></param>
        public void End(PluginManager pluginManager)
        {
            // Save settings
            this.SaveCommonSettings("GeneralSettings", Settings);
        }

        private void ResetCurrentLapTelemetry() {
            _CurrentLapTelemetry = new List<TelemetryData>();
            CurrentLapHasIncidents = false;
        }


        /// <summary>
        /// Returns the settings control, return null if no settings control is required
        /// </summary>
        /// <param name="pluginManager"></param>
        /// <returns></returns>
        public System.Windows.Controls.Control GetWPFSettingsControl(PluginManager pluginManager)
        {
            return new SettingsControl(this) { DataContext = Settings};
        }

       

        private void PluginManager_GameStateChanged(bool running, PluginManager pluginManager) {
            if (running) {
                return;
            }
            else {
                this.OnSessionChange(pluginManager);
            }
        }

        private void OnSessionChange(PluginManager pluginManager) {
            // Standins support removed 18/06/2023
            ClearStandings();
            ClearRelatives();

           



        }

        private void PluginManager_NewLap(int completedLapNumber, bool testLap, PluginManager manager, ref GameData data) {
           
        }

        static Task ProcessWrite(string data) {
            string filePath = @"c:\temp.json";
            return WriteTextAsync(filePath, data);
        }

        static async Task WriteTextAsync(string filePath, string text) {
            byte[] encodedText = Encoding.Unicode.GetBytes(text);

            using (FileStream sourceStream = new FileStream(filePath,
                FileMode.Append, FileAccess.Write, FileShare.None,
                bufferSize: 4096, useAsync: true)) {
                await sourceStream.WriteAsync(encodedText, 0, encodedText.Length);
            };
        }

       

        public void ExportCarTrackTelemetry(CarTrackTelemetry exportTelemetery ) {
            string exportDir = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Exports");
            if (!Directory.Exists(exportDir))
                Directory.CreateDirectory(exportDir);
            string fileName = System.IO.Path.Combine(exportDir, $"{exportTelemetery.PlayerName}_{exportTelemetery.TrackName}_{exportTelemetery.CarName}_{exportTelemetery.SetupType}_{exportTelemetery.LapTime.ToString(@"mm\.ss\.fff")}.json");
            using (StreamWriter file = File.CreateText(fileName)) {
                JsonSerializer serializer = new JsonSerializer();
                serializer.Serialize(file, exportTelemetery);
            }
            Process.Start(exportDir);

        }


        // Helper functions to deal with SimhubProperties
        public void AddProp(string PropertyName, dynamic defaultValue) => PluginManager.AddProperty(PropertyName, GetType(), defaultValue);
        public void SetProp(string PropertyName, dynamic value) => PluginManager.SetPropertyValue(PropertyName, GetType(), value);
        public dynamic GetProp(string PropertyName) => PluginManager.GetPropertyValue(PropertyName);
        public bool HasProp(string PropertyName) => PluginManager.GetAllPropertiesNames().Contains(PropertyName);
        public void AddEvent(string EventName) => PluginManager.AddEvent(EventName, GetType());
        public void TriggerEvent(string EventName) => PluginManager.TriggerEvent(EventName, GetType());
        public void AddAction(string ActionName, Action<PluginManager, string> ActionBody)
            => PluginManager.AddAction(ActionName, GetType(), ActionBody);

        public void TriggerAction(string ActionName) => PluginManager.TriggerAction(ActionName);


        // Function for easy debuggung
        public static void DebugMessage(string s) => SimHub.Logging.Current.Info((object)s);

    }
}
