using SimHub.Plugins;
using System;

namespace APR.DashSupport {
	public partial class APRDashPlugin : IPlugin, IDataPlugin, IWPFSettingsV2 {



		public void UpdateBrakeBarColour() {

			var brakePercentage = (double)GetProp("DataCorePlugin.GameData.Brake");
			string brakeBarColour = "red";
			
			if (brakePercentage < Settings.BrakeTrailStartPercentage) {
				brakeBarColour = "red";
			}
			else if (brakePercentage < Settings.BrakeTrailEndPercentage ) {
				brakeBarColour = "magenta";
			}
			else if (brakePercentage < Settings.BrakeTrailEndPercentage) {
				brakeBarColour = "magenta";
			}
			else if (brakePercentage <  Settings.BrakeTargetPercentage) {
				brakeBarColour = "red";
			}
			else if (brakePercentage <= Settings.BrakeMaxPercentage) {
				brakeBarColour = "magenta";
			}
			else {
				brakeBarColour = "red";
			}
			
			SetProp("BrakeBarColour", brakeBarColour);
			
		}
	}
}
