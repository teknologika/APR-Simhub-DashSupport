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

		public void UpdateFrontARBColour() {

			var inCar = (int)GetProp("GameRawData.Telemetry.dcAntiRollFront");
			var inSetup = int.Parse(GetProp("DataCorePlugin.GameRawData.SessionData.CarSetup.Chassis.Front.ArbBladeSetting"));

			if (inCar == inSetup) {
				SetProp("ARBColourFront", "Green");
			}
			else {
				SetProp("ARBColourFront", "White");
			}
		}

		public void UpdateRearARBColour() {

			var inCar = (int)GetProp("GameRawData.Telemetry.dcAntiRollRear");
			var inSetup = int.Parse(GetProp("DataCorePlugin.GameRawData.SessionData.CarSetup.Chassis.Rear.ArbBladeSetting"));

			if (inCar == inSetup) {
				SetProp("ARBColourRear", "Green");
			}
			else {
				SetProp("ARBColourRear", "White");
			}
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

		public void UpdateTCValues() {
			
            switch (GetProp("DataCorePlugin.GameData.CarModel")) {
				case "Ford GT GT3":
				case "Audi R8 LMS":
				case "BMW M4 GT3":
				case "Lamborghini Hurracan GT3 EVO":
				case "McLaren MP4-12C GT3":
				case "Mercedes-AMG GT3 2020":
				case "Porsche 911 GT3 R":
					SetProp("TCLabelLowValue", "HI AID");
					SetProp("TCLabelHighValue", "OFF");
					if (GetProp("TCLevel") == 12) {
						SetProp("TCIsOff", true);
					}
					break;
				case "Ferrari 488 GT3 Evo 2020":
				default:
					SetProp("TCLabelHighValue", "HI AID");
					SetProp("TCLabelLowValue", "OFF");
					if (GetProp("TCLevel") == 1) {
						SetProp("TCIsOff", true);
					}
					else {
						SetProp("TCIsOff", false);
					}
					break;
            }



        }


	}
}
