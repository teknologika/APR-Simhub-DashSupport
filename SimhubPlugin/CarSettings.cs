using SimHub.Plugins;
using System;
using System.Globalization;

namespace APR.DashSupport {
	public partial class APRDashPlugin : IPlugin, IDataPlugin, IWPFSettingsV2 {

		public void UpdateBrakeBarColour() {

			var brakePercentage = (double)GetProp("DataCorePlugin.GameData.Brake");
			string brakeBarColour = "Red";

			if (brakePercentage < Settings.BrakeTrailStartPercentage) {
				brakeBarColour = "Red";
			}
			else if (brakePercentage < Settings.BrakeTrailEndPercentage) {
				brakeBarColour = "Magenta";
			}
			else if (brakePercentage < Settings.BrakeTrailEndPercentage) {
				brakeBarColour = "Magenta";
			}
			else if (brakePercentage < Settings.BrakeTargetPercentage) {
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

			int inCar = 0;
			if (GetProp("GameRawData.Telemetry.dcAntiRollFront") != null) {
				inCar = Convert.ToInt32(GetProp("GameRawData.Telemetry.dcAntiRollFront"));
			}
			int inSetup = 0;
			if (GetProp("DataCorePlugin.GameRawData.SessionData.CarSetup.Chassis.Front.ArbBladeSetting") != null) {
				inSetup = inSetup = Convert.ToInt32(GetProp("DataCorePlugin.GameRawData.SessionData.CarSetup.Chassis.Front.ArbBladeSetting"));
			}
			else if (GetProp("DataCorePlugin.GameRawData.SessionData.CarSetup.Chassis.Front.ArbBlades") != null) {
				inSetup = Convert.ToInt32(GetProp("DataCorePlugin.GameRawData.SessionData.CarSetup.Chassis.Front.ArbBlades"));
			}

			if (inCar == inSetup) {
				SetProp("ARBColourFront", "Green");
			}
			else {
				SetProp("ARBColourFront", "White");
			}
		}

		public void UpdateRearARBColour() {

			int inCar = 0;
			if (GetProp("GameRawData.Telemetry.dcAntiRollRear") != null) {
				inCar = Convert.ToInt32(GetProp("GameRawData.Telemetry.dcAntiRollRear"));
			}
			int inSetup = 0;
			if (GetProp("DataCorePlugin.GameRawData.SessionData.CarSetup.Chassis.Rear.ArbBladeSetting") != null) {
				inSetup = Convert.ToInt32(GetProp("DataCorePlugin.GameRawData.SessionData.CarSetup.Chassis.Rear.ArbBladeSetting"));
			}
			else if (GetProp("DataCorePlugin.GameRawData.SessionData.CarSetup.Chassis.Rear.ArbBlades") != null) {
				inSetup = Convert.ToInt32(GetProp("DataCorePlugin.GameRawData.SessionData.CarSetup.Chassis.Rear.ArbBlades"));
			}
			

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
			else if (GetProp("DataCorePlugin.GameRawData.SessionData.CarSetup.BrakesDriveUnit.BrakeSpec.BrakePressureBias") != null) {
				setupBias = GetProp("DataCorePlugin.GameRawData.SessionData.CarSetup.BrakesDriveUnit.BrakeSpec.BrakePressureBias");
			}
			else if (GetProp("DataCorePlugin.GameRawData.SessionData.CarSetup.Chassis.Front.BrakePressureBias") != null) {
				setupBias = GetProp("DataCorePlugin.GameRawData.SessionData.CarSetup.Chassis.Front.BrakePressureBias");
			}
			else if (GetProp("DataCorePlugin.GameRawData.SessionData.CarSetup.Chassis.InCarDials.BrakePressureBias") != null) {
				setupBias = GetProp("DataCorePlugin.GameRawData.SessionData.CarSetup.Chassis.InCarDials.BrakePressureBias");
			}
			else if (GetProp("DataCorePlugin.GameRawData.SessionData.CarSetup.BrakesDriveUnit.BrakeSpec.BrakePressureBias") != null) {
				setupBias = GetProp("DataCorePlugin.GameRawData.SessionData.CarSetup.BrakesDriveUnit.BrakeSpec.BrakePressureBias");
			}
			else if (GetProp("DataCorePlugin.GameRawData.SessionData.CarSetup.Chassis.General.BrakePressureBias") != null) {
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

		public void GetSetupTC() {
			string setupVal = "";
			if (GetProp("GameRawData.SessionData.CarSetup.Chassis.BrakesInCar.TcSetting") != null) {
				string[] splitter = GetProp("GameRawData.SessionData.CarSetup.Chassis.BrakesInCar.TCSetting").Split(' ');
				setupVal = Convert.ToString(splitter[0]);
			}
			else if (GetProp("DataCorePlugin.GameRawData.SessionData.CarSetup.Chassis.Front.TcSetting") != null) {
				string[] splitter = GetProp("DataCorePlugin.GameRawData.SessionData.CarSetup.Chassis.Front.TcSetting").Split(' ');
				setupVal = Convert.ToString(splitter[0]);
			}
			else if (GetProp("DataCorePlugin.GameRawData.SessionData.CarSetup.Chassis.InCarDials.TractionControlSetting") != null) {
				string[] splitter = GetProp("DataCorePlugin.GameRawData.SessionData.CarSetup.Chassis.InCarDials.TractionControlSetting").Split(' ');
				setupVal = Convert.ToString(splitter[0]);
			}
			else if (GetProp("DataCorePlugin.GameRawData.SessionData.CarSetup.Chassis.InCarDials.TractionControlSetting1") != null) {
				string[] splitter = GetProp("DataCorePlugin.GameRawData.SessionData.CarSetup.Chassis.InCarDials.TractionControlSetting1").Split(' ');
				setupVal = Convert.ToString(splitter[0]);
			}
			else if (GetProp("DataCorePlugin.GameRawData.SessionData.CarSetup.BrakesDriveUnit.TractionControl.TractionControlSlip") != null) {
				string[] splitter = GetProp("DataCorePlugin.GameRawData.SessionData.CarSetup.BrakesDriveUnit.TractionControl.TractionControlSlip").Split(' ');
				setupVal = Convert.ToString(splitter[0]);
			}
			string tc = Convert.ToString(GetProp("DataCorePlugin.GameData.TCLevel"));
			if (tc == setupVal) {
				SetProp("TCColour", "Green");
			}
			else {
				SetProp("TCColour", "White");
			}
		}

		public void GetSetupABS() {

			string setupVal = "";

			if (GetProp("DataCorePlugin.GameRawData.SessionData.CarSetup.Chassis.BrakesInCar.AbsSetting") != null) {
				string[] splitter = GetProp("DataCorePlugin.GameRawData.SessionData.CarSetup.Chassis.BrakesInCar.AbsSetting").Split(' ');
				setupVal = Convert.ToString(splitter[0]);
			}
			else if (GetProp("DataCorePlugin.GameRawData.SessionData.CarSetup.Chassis.Front.AbsSetting") != null) {
				string[] splitter = GetProp("DataCorePlugin.GameRawData.SessionData.CarSetup.Chassis.Front.AbsSetting").Split(' ');
				setupVal = Convert.ToString(splitter[0]);

			}
			else if (GetProp("DataCorePlugin.GameRawData.SessionData.CarSetup.Chassis.InCarDials.AbsSetting") != null) {
				string[] splitter = GetProp("DataCorePlugin.GameRawData.SessionData.CarSetup.Chassis.InCarDials.AbsSetting").Split(' ');
				setupVal = Convert.ToString(splitter[0]);
			}

			string absLevel = Convert.ToString(GetProp("DataCorePlugin.GameData.ABSLevel"));
			if (absLevel == setupVal) {
				SetProp("ABSColour", "Green");
			}
			else {
				SetProp("ABSColour", "White");
			}
		}

		public void GetSetupEngineMap() {

			string setupEngineMap = "";
			string setupThrottleShape = ""; 

			//Engine map (MIX)
			if (GetProp("DataCorePlugin.GameRawData.SessionData.CarSetup.Chassis.BrakesInCar.EngineMapSetting") != null) {
				string[] splitter = GetProp("DataCorePlugin.GameRawData.SessionData.CarSetup.Chassis.BrakesInCar.EngineMapSetting").Split(' ');
				setupEngineMap = Convert.ToString(splitter[0]);
			}
			else if (GetProp("DataCorePlugin.GameRawData.SessionData.CarSetup.Drivetrain.Engine.EngineMapSetting") != null) {
				string[] splitter = GetProp("DataCorePlugin.GameRawData.SessionData.CarSetup.Drivetrain.Engine.EngineMapSetting").Split(' ');
				setupEngineMap = Convert.ToString(splitter[0]);

			}
			else if (GetProp("DataCorePlugin.GameRawData.SessionData.CarSetup.Chassis.InCarDials.EngineMapSetting") != null) {
				string[] splitter = GetProp("DataCorePlugin.GameRawData.SessionData.CarSetup.Chassis.InCarDials.EngineMapSetting").Split(' ');
				setupEngineMap = Convert.ToString(splitter[0]);
			}


			string engineMap = Convert.ToString(GetProp("DataCorePlugin.GameRawData.Telemetry.dcThrottleShape"));
			if (engineMap == setupEngineMap) {
				SetProp("MAPColour", "Green");
			}
			else {
				SetProp("MAPColour", "White");
			}

			//Throttle Shape (MAP)
			if (GetProp("GameRawData.SessionData.CarSetup.Chassis.BrakesInCar.ThrottleShapeSetting") != null) {
				string[] splitter = GetProp("GameRawData.SessionData.CarSetup.Chassis.BrakesInCar.ThrottleShapeSetting").Split(' ');
				setupThrottleShape = Convert.ToString(splitter[0]);
			}
			else if (GetProp("DataCorePlugin.GameRawData.SessionData.CarSetup.Drivetrain.Engine.ThrottleShapeSetting") != null) {
				string[] splitter = GetProp("DataCorePlugin.GameRawData.SessionData.CarSetup.Drivetrain.Engine.ThrottleShapeSetting").Split(' ');
				setupThrottleShape = Convert.ToString(splitter[0]);

			}
			else if (GetProp("DataCorePlugin.GameRawData.SessionData.CarSetup.Chassis.InCarDials.ThrottleShapeSetting") != null) {
				string[] splitter = GetProp("DataCorePlugin.GameRawData.SessionData.CarSetup.Chassis.InCarDials.ThrottleShapeSetting").Split(' ');
				setupThrottleShape = Convert.ToString(splitter[0]);

			}

			string throttleShape = Convert.ToString(GetProp("DataCorePlugin.GameRawData.Telemetry.dcThrottleShape"));
			if (throttleShape == setupThrottleShape) {
				SetProp("ThrottleShapeColour", "Green");
			}
			else {
				SetProp("ThrottleShapeColour", "White");
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
					else {
						SetProp("TCIsOff", false);
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

		public void UpdateABSValues() {

			switch (GetProp("DataCorePlugin.GameData.CarModel")) {
				case "Ford GT GT3":
				case "Audi R8 LMS":
				case "BMW M4 GT3":
				case "Lamborghini Hurracan GT3 EVO":
				case "McLaren MP4-12C GT3":
				case "Mercedes-AMG GT3 2020":
				case "Porsche 911 GT3 R":
					SetProp("ABSHighValueLabel", "OFF");
					SetProp("ABSLowValueLabel", "HI AID");
					if (GetProp("ABSLevel") == 12) {
						SetProp("ABSIsOff", true);
					}
                    else {
						SetProp("ABSIsOff", false);
					}

					break;
				case "Ferrari 488 GT3 Evo 2020":
				default:
					SetProp("ABSHighValueLabel", "HI AID");
					SetProp("ABSLowValueLabel", "OFF");
					if (GetProp("ABSLevel") == 1) {
						SetProp("ABSIsOff", true);
					}
					else {
						SetProp("ABSIsOff", false);
					}
					break;
			}

		}

		public void UpdateMAPValues() {

			int mapVal = 0;

			if (GetProp("DataCorePlugin.GameRawData.Telemetry.dcFuelMixture") != null) {

				mapVal = Convert.ToInt32(GetProp("DataCorePlugin.GameRawData.Telemetry.dcFuelMixture"));
			}

			switch (GetProp("DataCorePlugin.GameData.CarModel")) {
				case "Ford GT GT3":
				case "Audi R8 LMS":
				case "BMW M4 GT3":
				case "Lamborghini Hurracan GT3 EVO":
				case "McLaren MP4-12C GT3":
				case "Mercedes-AMG GT3 2020":
					switch (mapVal) {
						case 1:
							SetProp("MAPLabel", "1 - PACE");
							SetProp("MAPLabelColour", "Orange");
							
							break;
						case 2:
							SetProp("MAPLabel", "2 - SAVE");
							SetProp("MAPLabelColour", "Orange");
							break;
						case 3:
							SetProp("MAPLabel", "3 - RACE");
							SetProp("MAPLabelColour", "Green");
							break;
						default:
							SetProp("MAPLabel",mapVal.ToString());
							break;
					}
					break;
				case "Porsche 911 GT3 R":
				case "Ferrari 488 GT3 Evo 2020":
					switch (mapVal) {
						case 1:
                            SetProp("MAPLabel", "1 - RACE");
							SetProp("MAPLabelColour", "Green");
							break;
						case 12:
							SetProp("MAPLabel", "12 - PACE");
							SetProp("MAPLabelColour", "Orange");
							break;
						default:
							SetProp("MAPLabel", mapVal.ToString() + " - SAVE");
							SetProp("MAPLabelColour", "Orange");
							break;
					}
					break;
				default:
					SetProp("MAPLabelLowValue", "RACE");
					SetProp("MAPLabelHighValue", "SAVE           SC");
					SetProp("MAPLabel",mapVal.ToString());
					switch (mapVal) {
						case 1:
							SetProp("MAPLabelColour", "Green");
							SetProp("MAPLabel", mapVal.ToString() + " - RACE");
							break;
						case 12:
							SetProp("MAPLabel", "12 - PACE");
							SetProp("MAPLabelColour", "Orange");
							break;
						default:
							SetProp("MAPLabelColour", "Orange");
							SetProp("MAPLabel", mapVal.ToString() + " - SAVE");
							break;
					}
					break;
			}



		}

		public void UpdatePopupPositions() {
			SetProp("FuelPopupPercentage", Settings.FuelPopupPercentage/100);
			SetProp("PitWindowPopupPercentage", Settings.PitWindowPopupPercentage/100);

		}
	}
}
