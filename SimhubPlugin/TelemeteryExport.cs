using FMOD;
using GameReaderCommon;
using IRacingReader;
using iRacingSDK;
using MahApps.Metro.Controls;
using SimHub.Plugins;
using SimHub.Plugins.DataPlugins.DataCore;
using SimHub.Plugins.DataPlugins.RGBDriver.UI.LedBehaviourEditors;
using SimHub.Plugins.OutputPlugins.Dash.GLCDTemplating;
using SimHub.Plugins.OutputPlugins.GraphicalDash.Behaviors.DoubleText.Imp;
using SimHub.Plugins.OutputPlugins.GraphicalDash.Behaviors.TimespanText.Imp;
using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Xml.Linq;
using static iRacingSDK.SessionData._DriverInfo;

namespace APR.DashSupport {

    public partial class APRDashPlugin : IPlugin, IDataPlugin, IWPFSettingsV2 {

        private Guid _CurrentSessionId;
        private double? _SteeringAngle;
        private int _IncidentCount;
        private bool _CurrentLapHasIncidents;
        private int _IsInpit = 1;
        private bool _IsFixedSetupSession;
        private string _SelectedGame = "";
        private string _SelectedTrack = "";
        private string _SelectedCar = "";
        
        public string TelemeteryDataUpdate(PluginManager pluginManager, ref GameData data) {

        
            var incidentCount = Convert.ToInt32(pluginManager.GetPropertyValue("DataCorePlugin.GameRawData.Telemetry.PlayerCarMyIncidentCount"));
            if (incidentCount > _IncidentCount) {
                _CurrentLapHasIncidents = true;
                _IncidentCount = incidentCount;
            }
            var Throttle = data.NewData.Throttle;
            string throttle = "\"Throttle\":" + Throttle.ToString("0.0") + "," ;

            var Brake = data.NewData.Brake;
            string brake = "\"Brake\":" + Brake.ToString("0.0") + ",";

            var Clutch = data.NewData.Clutch;
            string clutch = "\"Clutch\":" + Clutch.ToString("0.0") + ",";

            var LapDistance = data.NewData.TrackPositionPercent;
            string lapDistance = "\"LapDistance\":" + LapDistance.ToString("0.000000000000") + ",";

            var Speed = data.NewData.SpeedKmh;
            string speed = "\"Speed\":" + Speed.ToString("0.0") + ",";

            var Gear = data.NewData.Gear;
            string gear = "\"Gear\": \"" + Gear + "\",";

            var SteeringAngle = Convert.ToDouble(pluginManager.GetPropertyValue("DataCorePlugin.GameRawData.Telemetry.SteeringWheelAngle")) * -58.0;
            string steeringAngle = "\"SteeringAngle\":" + SteeringAngle.ToString("0.000000000") + ",";

            // var LapTime = data.NewData.CurrentLapTime;
            string lapTime = "\"LapTime\":" + "\"00:00:00.0000000\"";

            return "{ " + throttle + brake + clutch + lapDistance + speed + gear + steeringAngle + lapTime + "},"  ;
        }

    }

}
