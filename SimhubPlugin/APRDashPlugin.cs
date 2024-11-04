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
using NAudio.Dmo;

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

        public double SessionTime;
        DateTime now;
        private long time;
        

        private long endTime1Sec;
        private readonly int every1sec = 10000000;
        private bool runEvery1Sec;


        private long endTime5Sec;
        private readonly int every5sec = 50000000;
        private bool runEvery5Sec;


        private bool IsRaceSession;
        private bool IsSpectating;


        DataSampleEx irData;
       
        public PluginManager PluginManager { get; set; }
        public ImageSource PictureIcon => this.ToIcon(Properties.Resources.sdkmenuicon);
        public string LeftMenuTitle => "APR Dash Support";
       
        // Session information
        public int PreviousSessionState = 0;
        public double PreviousSessionTick;
        public long PreviousSessionID;
        public string SessionType;
        public int SessionIndexNumber = 0;
        public string SessionLapsString;

       
        public bool IsV8VetsSession = false;
        public bool IsV8VetsRaceSession = false;
       // public bool IsUnderSafetyCar_legacy = false;
       // public bool IsSafetyCarMovingInPitane = false;
       // public int SafetyCarPeriodCount = 0;
       // public bool SafetyCarCountLock = false;

        public string[] V8VetsSafetyCarNames = { "BMW M4 GT4", "Mercedes AMG GT3", "McLaren 720S GT3 EVO" };
        
        
        public bool IsIRacingAdmin = false;
        public bool IsLeagueSession = false;

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

        //public int SafetyCarIdx;
        //public double SafetyCarLapDistPct;


        public class relative {
            public int position;
            public double trackPositionPercent;
            public bool onPitRoad;
            public double relativeGap;

            public override string ToString() {
                return "P: " + position + " " + trackPositionPercent + " " + onPitRoad;
            }
        }

   
       

        public float GetTrackLength() {
            if (irData != null) {
                return float.Parse(irData.SessionData.WeekendInfo.TrackLength.Split(' ')[0]) * 1000;
            }
            else {
                return 0;
            }
            
        }

        private void UpdateSessionData(GameData data) {
            SessionType = data.NewData.SessionTypeName;
            
            int sessionCount = this.irData.SessionData.SessionInfo.Sessions.Count();
            if (sessionCount > 0) {
                SessionIndexNumber = sessionCount -1;
            }

            SessionLapsString = this.irData.SessionData.SessionInfo.Sessions[SessionIndexNumber].SessionLaps;
              
            PreviousSessionTick = (double)irData.Telemetry.SessionTime;
            PreviousSessionID = (long)this.irData.SessionData.WeekendInfo.SessionID;


            //Reset values
            IsV8VetsSession = false;
            IsV8VetsRaceSession = false;
            //IsUnderSafetyCar_legacy = false;
           // IsSafetyCarMovingInPitane = false;
           // SafetyCarPeriodCount = 0;
           // SafetyCarCountLock = false;

            StrategyBundle.Reset();

            CheckIfV8VetsLeagueSession();
            CheckIfLeagueSession();


   

            SetProp("Strategy.Vets.IsVetsSession", IsV8VetsSession);
            SetProp("Strategy.Vets.IsVetsRaceSession", IsV8VetsRaceSession);

            //UpdateCommonProperties(data);
            

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


        private void CheckIfV8VetsLeagueSession() {
            var leagueID = irData.SessionData.WeekendInfo.LeagueID;

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
            StrategyBundle StrategyObserver = StrategyBundle.Instance;

            StrategyObserver.IsUnderSC = irData.Telemetry.UnderPaceCar;
            if (StrategyObserver.SafetyCarCountLock == false && StrategyObserver.IsUnderSC) {
                StrategyObserver.SafetyCarPeriodCount++;
                StrategyObserver.SafetyCarPeriodCount++;
                StrategyObserver.SafetyCarCountLock =  true;
            }

            if (IsV8VetsRaceSession) {
                foreach (var item in OpponentsExtended) {
                    if (V8VetsSafetyCarNames.Contains(item._competitor.CarScreenName)) {
                        if (!item.IsCarInPitLane && item.Speed > 0.01f && SessionType == "Race") {  

                            StrategyObserver.SafetyCarIdx = item.CarIdx;
                            StrategyObserver.SafetyCarTrackDistancePercent = item.TrackPositionPercent;

                            StrategyObserver.IsUnderSC = true;
                            StrategyObserver.IsSafetyCarMovingInPitane = false;
                        }

                        if (!item.IsCarInPitBox && item.IsCarInPitLane && item.Speed > 0.01f && SessionType == "Race") {

                            StrategyObserver.IsUnderSC = true;
                            StrategyObserver.IsSafetyCarMovingInPitane = true;

                        }
                    }
                }
            }
            else {
                StrategyObserver.IsUnderSC = false;
                StrategyObserver.SafetyCarCountLock = false;
            }
            if(!StrategyObserver.SafetyCarCountLock && StrategyObserver.IsUnderSC) {
                StrategyObserver.SafetyCarPeriodCount++;
                StrategyObserver.SafetyCarCountLock = true;
            }

            if (StrategyObserver.SafetyCarCountLock && !StrategyObserver.IsUnderSC) {
                StrategyObserver.SafetyCarCountLock = false;
            }

            SetProp("Strategy.Indicator.UnderSC", StrategyObserver.IsUnderSC);
            SetProp("Strategy.Indicator.SCMovingInPitlane", StrategyObserver.IsSafetyCarMovingInPitane);
            SetProp("Strategy.Indicator.SCPeriodCount", StrategyObserver.SafetyCarPeriodCount);
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

            this.AttachDelegate("Standings.DriverNameStyle", () => Settings.DriverNameStyle);
            this.AttachDelegate("Standings.Columns.DriverName.Width", () => Settings.DriverNameWidth);


            this.AttachDelegate("Standings.DataToShowInColumn", () => Settings.Standings_MiscDataToShow);
            this.AttachDelegate("Standings.DataToShowInColumnString", () => Settings.Standings_MiscDataToShowString);


            this.AttachDelegate("Common.EnableStandings", () => Settings.EnableStandings);
            this.AttachDelegate("Common.EnableRelative", () => Settings.EnableRelatives);
            this.AttachDelegate("Relative.ShowCarsInPits", () => Settings.RelativeShowCarsInPits);
            
            pluginManager.AddProperty<double>("Version", this.GetType(), 1.1);
            pluginManager.AddProperty<string>("MainMenuSelected", this.GetType(), "none");

            CreateCommonProperties();

            SetProp("Common.IsLeagueSession", false);

            // Dashboard specific properties > move to a dashboard Styles Class
            AddProp("Dash.Styles.BoxWithBorder.Border.Color", Settings.Color_LightGrey); // Gray
            AddProp("Dash.Styles.BoxWithBorder.Border.LineThickness", 4); 
            AddProp("Dash.Styles.BoxWithBorder.Border.CornerRadius", 12);
            AddProp("Dash.Styles.BoxWithBorder.Background.Color", Settings.Color_Transparent); // Transparent
            AddProp("Dash.Styles.BoxWithOutBorder.Border.Color", Settings.Color_Transparent); // Transparent

            AddProp("Dash.Styles.BoxWithOutBorder.Border.LineThickness", 0);
            AddProp("Dash.Styles.BoxWithOutBorder.Border.CornerRadius", 12);

            AddProp("Dash.Styles.BoxWithOutBorder.Background.Color", Settings.Color_DarkBackground); // Transparent
            AddProp("Dash.Styles.Colors.Lap.SessionBest", Settings.Color_Purple); // Purple
            AddProp("Dash.Styles.Colors.Lap.PersonalBest", Settings.Color_Green); // Green
            AddProp("Dash.Styles.Colors.Lap.Latest", Settings.Color_White); // white
            AddProp("Dash.Styles.Colors.Lap.Default", Settings.Color_Yellow); // yellow


            AddProp("Dash.Styles.Strategy.BarsWidth", Settings.Strategy_BarsWidth);
            AddProp("Dash.Styles.BarsStart", Settings.Strategy_BarsStart);

            AddProp("Dash.Mode.NightMode", false);

            AddProp("Common.Bias.Preferred", Settings.PreferredBrakeBiasPercentage);
            AddProp("Common.Bias.Setup", Settings.SetupBrakeBiasPercentage);
            AddProp("Common.Bias.Delta", "0.0");
            AddProp("Common.Bias.Color", "");

            AddProp("BrakeBarColour", Settings.Color_Red);
            AddProp("BrakeBiasColour", Settings.Color_DarkBackground);
            AddProp("BrakeBarTargetTrailPercentage",0);
            AddProp("BrakeBarTargetPercentage", 0);

            AddProp("ARBColourFront", Settings.Color_White);
            AddProp("ARBColourRear", Settings.Color_White);
            AddProp("JackerColour", Settings.Color_White);

            AddProp("TCHighValueLabel", "HI AID");
            AddProp("TCLowValueLabel", "OFF");
            AddProp("TCIsOff", false);
            AddProp("TCColour", Settings.Color_White);

            AddProp("ABSColour", Settings.Color_White);
            AddProp("ABSHighValueLabel", "OFF");
            AddProp("ABSLowValueLabel", "HI AID");
            AddProp("ABSIsOff", false);

            AddProp("MAPLabelColour", Settings.Color_White);
            AddProp("MAPLabel", "1");
            AddProp("MAPHighValueLabel", "RACE");
            AddProp("MAPLowValueLabel", "SAVE           SC");

            AddProp("PitWindowMessage", "");
            AddProp("PitWindowTextColour", Settings.Color_Transparent);
            AddProp("pitWindowBackGroundColour", Settings.Color_Transparent);

            AddProp("FuelPopupPercentage", Settings.FuelPopupPercentage);
            AddProp("PitWindowPopupPercentage", Settings.PitWindowPopupPercentage);

            AddProp("LaunchBitePointAdjusted", 0);
            AddProp("LaunchPreferFullThrottleStarts", Settings.PreferFullThrottleStarts);
            AddProp("LaunchUsingDualClutchPaddles", Settings.LaunchUsingDualClutchPaddles);

            // Strategy Specific Properties
            AddProp("Strategy.Indicator.StratMode", "A");
            AddProp("Strategy.Indicator.UnderSC", false);
            AddProp("Strategy.Indicator.SCMovingInPitlane", false);
            AddProp("Strategy.Indicator.SCPeriodCount", 0);


            AddProp("Strategy.Indicator.CPS1Served", false);
            AddProp("Strategy.Indicator.CPS2Served", false);

            AddProp("Strategy.Indicator.RCMode", false);
            AddProp("Strategy.Indicator.isiRacingAdmin", IsIRacingAdmin = false);


            AddRelativeProperties();

            ClearStandings();
            AddStandingsRelatedProperties();

            InitPitCalculations();
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
            // Use a frame counter to not update everything every frame
            // Simhub  runs this loop runs 60x per second
            frameCounter++;

            // reset the counter every 60hz
            // not sure what happens if you are on the free version ???
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
                this.SessionTime = irData.Telemetry.SessionTime;
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

                        // Clear standings and Relatives
                        ClearStandings();
                        ClearRelativeProperties();

                    }
                    this.PreviousSessionState = num;
                    this.IsRaceSession = data.NewData.SessionTypeName == "Race";
#pragma warning disable CS0612 // Type or member is obsolete
                    this.IsSpectating = data.Spectating;
#pragma warning restore CS0612 // Type or member is obsolete
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

                if (frameCounter == 9) {
                    UpdateStrategyBundle(data);
                    CheckIfUnderSafetyCar();
                    if (runEvery1Sec) {
                        if (data.GameRunning && data.NewData != null) {
                            try {
                                if (data.NewData.SessionTimeLeft.TotalSeconds > 0) {
                                    UpdateRelativesAndStandings(data);
                                    UpdateCommonProperties(data);
                                }
                            }
                            catch (Exception) {
                            }
                        }

                                
                    }
                }

                if (frameCounter == 10) {
                    UpdateBrakeBar();
                    
                }

                if (frameCounter == 20) {
                    UpdateBrakeBar();
                    UpdatePitCalculations(ref data);
                }

                if (frameCounter == 25) {
                    if (runEvery5Sec) {
                        if (data != null) {
                            try {
                                UpdateStrategy();
                                SetProp("Strategy.Indicator.RCMode", Settings.Strategy_RCMode);
                            }
                            catch (Exception) {
                            }
                        }
                    }
                }

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
                PitStore.Reset();
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

            ClearStandings();
            ClearRelativeProperties();
            ResetRelativeAndStandingsData();
            PitStore.Reset();
            StrategyBundle.Reset();
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


        public string NiceTime(TimeSpan timeToFormat) {

            if (timeToFormat == TimeSpan.Zero) {
                return "-.---";
            }
            if (timeToFormat.Minutes == 0) {
                return timeToFormat.ToString("s'.'fff");
            }
            else {
                return timeToFormat.ToString("m':'ss'.'fff");
            }
        }

    }
}
