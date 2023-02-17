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

		public void GetSetupBias() {
			var setupBias = 0.0;
			if (GetProp("GameRawData.SessionData.CarSetup.Chassis.BrakesInCar.BrakePressureBias") != null) {
				setupBias = GetProp("GameRawData.SessionData.CarSetup.Chassis.BrakesInCar.BrakePressureBias");
			}
			else if (GetProp("GameRawData.SessionData.CarSetup.Chassis.BrakesInCar.BrakePressureBias") != null) {
				setupBias = GetProp("GameRawData.SessionData.CarSetup.Chassis.BrakesInCar.BrakePressureBias");
			}
			else if (GetProp("DataCorePlugin.GameRawData.SessionData.CarSetup.BrakesDriveUnit.BrakeSpec.BrakePressureBias") != null){
				setupBias = GetProp("DataCorePlugin.GameRawData.SessionData.CarSetup.BrakesDriveUnit.BrakeSpec.BrakePressureBias");
			}
			else if (GetProp("DataCorePlugin.GameRawData.SessionData.CarSetup.Chassis.Front.BrakePressureBias") != null){
				setupBias = GetProp("DataCorePlugin.GameRawData.SessionData.CarSetup.Chassis.Front.BrakePressureBias");
			}
			else if (GetProp("DataCorePlugin.GameRawData.SessionData.CarSetup.Chassis.InCarDials.BrakePressureBias") != null){
				setupBias = GetProp("DataCorePlugin.GameRawData.SessionData.CarSetup.Chassis.InCarDials.BrakePressureBias");
			}
			else if (GetProp("DataCorePlugin.GameRawData.SessionData.CarSetup.BrakesDriveUnit.BrakeSpec.BrakePressureBias") != null){
				setupBias = GetProp("DataCorePlugin.GameRawData.SessionData.CarSetup.BrakesDriveUnit.BrakeSpec.BrakePressureBias");
			}
			else if (GetProp("DataCorePlugin.GameRawData.SessionData.CarSetup.Chassis.General.BrakePressureBias") != null){
				setupBias = GetProp("DataCorePlugin.GameRawData.SessionData.CarSetup.Chassis.General.BrakePressureBias");
			}
			SetProp("BrakeBiasSetup", setupBias);
		}
	}
}
