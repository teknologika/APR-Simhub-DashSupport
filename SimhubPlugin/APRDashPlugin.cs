﻿using GameReaderCommon;
using SimHub.Plugins;
using System;
using System.Windows.Media;
using IRacingReader;
using iRacingSDK;

namespace APR.DashSupport
{

    [PluginDescription("Support for APR Dashes and Overlays")]
    [PluginAuthor("Bruce McLeod")]
    [PluginName("APR Dash Support")]
    public partial class APRDashPlugin : IPlugin, IDataPlugin, IWPFSettingsV2
    {
        public DashPluginSettings Settings;

        DataSampleEx irData;
       
        public PluginManager PluginManager { get; set; }
        public ImageSource PictureIcon => this.ToIcon(Properties.Resources.sdkmenuicon);
        public string LeftMenuTitle => "APR Dash Support";

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

            this.OnSessionChange(pluginManager);

            InitRotaries(pluginManager);

            this.AttachDelegate("EnableBrakeAndThrottleBars", () => Settings.EnableBrakeAndThrottleBars);
            this.AttachDelegate("EnableRPMBar", () => Settings.EnableRPMBar);
            this.AttachDelegate("EnablePitWindowPopup", () => Settings.EnablePitWindowPopup);
            this.AttachDelegate("EnableFuelPopup", () => Settings.EnableFuelPopup);

            this.AttachDelegate("DriverNameStyle_0", () => Settings.DriverNameStyle_0);
            this.AttachDelegate("DriverNameStyle_1", () => Settings.DriverNameStyle_1);

            //InitRotaryButtons(pluginManager);
            //InitOtherButtons(pluginManager);


            pluginManager.AddProperty<double>("Version", this.GetType(), 1.1);
            pluginManager.AddProperty<string>("MainMenuSelected", this.GetType(), "none");

            AddProp("BrakeBarColour", "Red");
            AddProp("BrakeBiasColour", "black");

            AddProp("ARBColourFront", "White");
            AddProp("ARBColourRear", "White");

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

            // Add Standings Properties
            if (Settings.EnableStandings) {
                AddProp("Standings.test", Settings.DriverNameStyle);

                int length = 63; // number of cars
                for (int i = 0; i < length; i++) {
                    AddProp("Standings.test", Settings.DriverNameStyle);
                }



            }

            

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
            if (Settings.SettingsUpdated) {
                
                // Update Standings Properties
                if (Settings.EnableStandings) {
                    Settings.DriverNameStyle = Settings.DriverNameStyle_0 ? 0 : Settings.DriverNameStyle_1 ? 1 : Settings.DriverNameStyle_2 ? 2 : Settings.DriverNameStyle_3 ? 3 : (Settings.DriverNameStyle_4 ? 1 : 4);
                    SetProp("Standings.test", Settings.DriverNameStyle);
                }
                Settings.SettingsUpdated = false;
            }

            // Confirm the sim is up and running at it is iRacing
            if (data.GameRunning && data.GameName == "IRacing")
            {
                // Make sure we are getting a telemetry feed
                if (data.OldData != null && data.NewData != null)
                {
                    //Gaining access to raw data
                    if (data?.NewData?.GetRawDataObject() is DataSampleEx) { irData = data.NewData.GetRawDataObject() as DataSampleEx; }

                    float[] times = irData.Telemetry.CarIdxF2Time;
                    for (int i = 0; i < times.Length; i++) {
                        if (times[i] > 0) {
                            DebugMessage("Position: " + i + " Gap:" + times[i]);
                        }

                    }


                    GetSetupBias();
                    GetSetupTC();
                    GetSetupABS();
                    UpdateFrontARBColour();
                    UpdateRearARBColour();
                    UpdateTCValues();
                    UpdateBrakeBarColour();
                    UpdateMAPValues();
                    UpdateBitePointRecommendation();
                    UpdatePitWindowMessage();
                    UpdatePopupPositions();
                   


                    

                    
                }
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
