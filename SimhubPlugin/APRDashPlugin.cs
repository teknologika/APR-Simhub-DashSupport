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


            // Loop through iRacing competitors and simhub Opponents and create a shiny new structure


            this.opponents = data.NewData.Opponents;

            /*
            this.opponentsOld = data.OldData.Opponents;
            this.opponentsClass = data.NewData.OpponentsPlayerClass;
            this.opponentsAhead = data.NewData.OpponentsAheadOnTrack;
            this.opponentsBehind = data.NewData.OpponentsBehindOnTrack;
            this.opponentsAheadInClass = data.NewData.OpponentsAheadOnTrackPlayerClass;
            this.opponentsBehindInClass = data.NewData.OpponentsBehindOnTrackPlayerClass;

            */

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
                            _specatedCarLapDistPct = spectatedCarLapDistPct
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

            var bob = this.OpponentsInClass();
            var tim = this.GetReferenceClassLaptime();
            var fred = this.RelativeGapToSpectatedCar(0);
            var ahead = this.OpponentsAhead;
            var behind = this.OpponentsBehind;

            SetProp("Relative.Behind.1.Position", "999");

            for (int i = 1; i < Settings.RelativeShowCarsAhead; i++)
            {
                SetProp("Relative.Ahead." + i + ".Position", "");
                SetProp("Relative.Ahead." + i + ".Distance","");
                SetProp("Relative.Ahead." + i + ".Gap", "");
            }

            int count = 1;
            foreach (var opponent in OpponentsAhead) {
                SetProp("Relative.Ahead." + count + ".Position", opponent.Position.ToString());
                SetProp("Relative.Ahead." + count + ".Distance", Math.Abs(opponent.LapDistSpectatedCar).ToString("0.0"));
                SetProp("Relative.Ahead." + count + ".Gap", Math.Abs(opponent.GapSpectatedCar).ToString("0.0"));
                count++;
            }

            for (int i = 1; i < Settings.RelativeShowCarsBehind; i++) {
                SetProp("Relative.Behind." + i + ".Position", "");
                SetProp("Relative.Behind." + i + ".Distance", "");
                SetProp("Relative.Behind." + i + ".Gap", "");
            }

            count = 1;
            foreach (var opponent in OpponentsBehind) {
                SetProp("Relative.Behind." + count + ".Position", opponent.Position.ToString());
                SetProp("Relative.Behind." + count + ".Distance", Math.Abs(opponent.LapDistSpectatedCar).ToString("0.0"));
                SetProp("Relative.Behind." + count + ".Gap", Math.Abs(opponent.GapSpectatedCar).ToString("0.0"));
                count++;
            }

            AddProp("Relative.Position", OpponentsExtended[spectatedCarIdx].Position.ToString());
            AddProp("Relative.Gap", "");
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

            for (int i = 1; i < Settings.RelativeShowCarsAhead; i++) {
                AddProp("Relative.Ahead." + i + ".Position", "");
                AddProp("Relative.Ahead." + i + ".Distance", "");
                AddProp("Relative.Ahead." + i + ".Gap", "");
            }

            for (int i = 1; i < Settings.RelativeShowCarsBehind; i++) {
                AddProp("Relative.Behind." + i + ".Position","") ;
                AddProp("Relative.Behind." + i + ".Distance","" );
                AddProp("Relative.Behind." + i + ".Gap", "");
            }

            AddProp("Relative.Position","");
            AddProp("Relative.Gap", "");

        

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
                        SetProp("Strategy.Indicator.RCMode", Settings.Strategy_RCMode);

                        // Check if we are an iRacing Admin
                        CheckIfiRacingAdmin(data);
                        SetProp("Strategy.Indicator.isiRacingAdmin", IsIRacingAdmin);

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
                    UpdateRelatives(data);
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
