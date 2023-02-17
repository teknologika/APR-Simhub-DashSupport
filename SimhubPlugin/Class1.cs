using SimHub.Plugins;
using System;

namespace APR.DashSupport {
	public partial class APRDashPlugin : IPlugin, IDataPlugin, IWPFSettingsV2 {



		public void UpdateBrakeBarColour() {

			var brakePercentage = (double)GetProp("DataCorePlugin.GameData.Brake");
			string brakeBarColour = "Red";
			
			if (brakePercentage < Settings.BrakeTrailStartPercentage) {
				brakeBarColour = "Red";
			}
			else if (brakePercentage < Settings.BrakeTrailEndPercentage ) {
				brakeBarColour = "Magenta";
			}
			else if (brakePercentage < Settings.BrakeTrailEndPercentage) {
				brakeBarColour = "Magenta";
			}
			else if (brakePercentage <  Settings.BrakeTargetPercentage) {
				brakeBarColour = "Red";
			}
			else if (brakePercentage <= Settings.BrakeMaxPercentage) {
				brakeBarColour = "Magenta";
			}
			else {
				brakeBarColour = "Red";
			}
			SetProp("BrakeBarColour", brakeBarColour);
		}

		public void GetSetupBias() {
			var setupBias = "0.0";
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

			Settings.SetupBrakeBiasPercentage = double.Parse(setupBias.Replace("%", ""));
			double bias = GetProp("BrakeBias");

			if (bias == Settings.SetupBrakeBiasPercentage) {
				SetProp("BrakeBiasColour", "Green");
			}
			else if (bias == Settings.PreferredBrakeBiasPercentage) {
				SetProp("BrakeBiasColour", "DeepSkyBlue");
			}
			else {
				SetProp("BrakeBiasColour", "White");
			}
		}
	}
}
