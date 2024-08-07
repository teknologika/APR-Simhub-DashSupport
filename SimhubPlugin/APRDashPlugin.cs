﻿using GameReaderCommon;
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

namespace APR.DashSupport
{

    [PluginDescription("Support for APR Dashes and Overlays")]
    [PluginAuthor("Bruce McLeod")]
    [PluginName("APR Dash Support")]
    public partial class APRDashPlugin : IPlugin, IDataPlugin, IWPFSettingsV2
    {
        public DashPluginSettings Settings;
        public int frameCounter = 0;

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


        private void UpdateSessionData(GameData data) {
            SessionType = data.NewData.SessionTypeName;
            PreviousSessionTick = (double)this.PluginManager.GetPropertyValue("DataCorePlugin.GameRawData.Telemetry.SessionTime");
            PreviousSessionID = (long)this.PluginManager.GetPropertyValue("DataCorePlugin.GameRawData.SessionData.WeekendInfo.SessionID");
            IsV8VetsLeagueSession();

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
            if (2 == (long)this.PluginManager.GetPropertyValue("DataCorePlugin.GameRawData.Telemetry.PaceMode")) {
                IsUnderSafetyCar = true;
            }
            else {
                if (IsV8VetsRaceSession) {
                    for (int i = 1; i < Settings.MaxCars; i++) {
                        string carName = (string)this.PluginManager.GetPropertyValue($"IRacingExtraProperties.iRacing_Leaderboard_Driver_{i:00}_CarName");
                        if (V8VetsSafetyCarNames.Contains(carName)) {
                            bool isInPit = (bool)this.PluginManager.GetPropertyValue($"IRacingExtraProperties.iRacing_Leaderboard_Driver_{i:00}_IsInPit");
                            if (!isInPit) {
                                IsUnderSafetyCar = true;
                            }
                            else {
                                IsUnderSafetyCar = false;
                            }
                        }
                    }
                }
                else {
                    IsUnderSafetyCar = false;
                }
            }
            SetProp("Strategy.Vets.IsUnderSafetyCar", IsUnderSafetyCar);
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

            //HERE

            //InitRotaryButtons(pluginManager);
            //InitOtherButtons(pluginManager);


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
                    }
                    this.PreviousSessionState = num;
                }

                bool sessionStartSetupCompleted = false;
                if (!sessionStartSetupCompleted) {
                    InitStandings(ref data);
                    sessionStartSetupCompleted = true;
                }

                // Frames are used to reduce calculation frequency

                if (frameCounter == 1) {

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
                }

                if (frameCounter == 40) {
                    UpdateBrakeBar();
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
