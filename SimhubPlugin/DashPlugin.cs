using GameReaderCommon;
using SimHub.Plugins;
using System;
using System.Windows.Media;

namespace APR.DashSupport
{
    [PluginDescription("Support for APR Dashes and Overlays")]
    [PluginAuthor("Bruce McLeod")]
    [PluginName("APR Dash Support")]
    public partial class DashPlugin : IPlugin, IDataPlugin, IWPFSettingsV2
    {
        public DashPluginSettings Settings;

        public PluginManager PluginManager { get; set; }
        public ImageSource PictureIcon => this.ToIcon(Properties.Resources.sdkmenuicon);
        public string LeftMenuTitle => "APR Dash Support";

        // Function for easy debuggung
        public static void DebugMessage(string s) => SimHub.Logging.Current.Info((object)s);

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
            // Confirm the sim is up and running at it is iRacing
            if (data.GameRunning && data.GameName == "IRacing")
            {
                // Make sure we are getting a telemetry feed
                if (data.OldData != null && data.NewData != null)
                {
                    // Data updates go here
                   
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

        /// <summary>
        /// Called once after plugins startup
        /// Plugins are rebuilt at game change
        /// </summary>
        /// <param name="pluginManager"></param>
        public void Init(PluginManager pluginManager)
        {
            SimHub.Logging.Current.Info("Starting APR plugin");
          

            // Load settings
            Settings = this.ReadCommonSettings<DashPluginSettings>("GeneralSettings", () => new DashPluginSettings());
            DebugMessage("EnableBars:" + Settings.EnableRPMBrakeThrottle.ToString());

            // Setup event handlers
            pluginManager.GameStateChanged += new PluginManager.GameRunningChangedDelegate(this.PluginManager_GameStateChanged);

            this.OnSessionChange(pluginManager);

            InitRotaries(pluginManager);

            this.AttachDelegate("EnableRPMBrakeThrottle", () => Settings.EnableRPMBrakeThrottle);

            //InitRotaryButtons(pluginManager);
            //InitOtherButtons(pluginManager);


            pluginManager.AddProperty<double>("Version", this.GetType(), 1.1);
            pluginManager.AddProperty<string>("MainMenuSelected", this.GetType(), "none");
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

    }
}
