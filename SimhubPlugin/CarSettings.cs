using iRacingSDK;
using SimHub.Plugins;
using System;
using System.Globalization;

namespace APR.DashSupport {
	public partial class APRDashPlugin : IPlugin, IDataPlugin, IWPFSettingsV2 {


		public void UpdateStrategy() {

		}

        public void UpdateBrakeBar() {

			var brakePercentage = (double)GetProp("DataCorePlugin.GameData.Brake");
			string brakeBarColour;

			if (brakePercentage < Settings.BrakeTrailStartPercentage) {
				brakeBarColour = "Red";
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
            SetProp("BrakeBarTargetTrailPercentage", ((Settings.BrakeTrailEndPercentage + Settings.BrakeTrailStartPercentage)/2)/100);
            SetProp("BrakeBarTargetPercentage", Settings.BrakeTargetPercentage/100);
        }

		public void UpdateFrontARBColour() {

			//float inCar = irData.Telemetry.dcAntiRollFront;
			int inCar = 0;
			if (GetProp("GameRawData.Telemetry.dcAntiRollFront") != null) {
				inCar = Convert.ToInt32(GetProp("GameRawData.Telemetry.dcAntiRollFront"));
			}
			int inSetup = -1;
			try {
                if (GetProp("DataCorePlugin.GameRawData.SessionData.CarSetup.Chassis.Front.ArbBladeSetting") != null) {
                    inSetup = Convert.ToInt32(GetProp("DataCorePlugin.GameRawData.SessionData.CarSetup.Chassis.Front.ArbBladeSetting"));
                }
                else if (GetProp("DataCorePlugin.GameRawData.SessionData.CarSetup.Chassis.Front.ArbBlades") != null) {
                    inSetup = Convert.ToInt32(GetProp("DataCorePlugin.GameRawData.SessionData.CarSetup.Chassis.Front.ArbBlades"));
                }
                else if (GetProp("DataCorePlugin.GameRawData.SessionData.CarSetup.Chassis.Front.BarBladePosition") != null) {
                    inSetup = Convert.ToInt32(GetProp("DataCorePlugin.GameRawData.SessionData.CarSetup.Chassis.Front.BarBladePosition"));
                }
            }
			catch (Exception) {

			}
			

			if (inCar == inSetup) {
				SetProp("ARBColourFront", "Green");
			}
			else {
				SetProp("ARBColourFront", Settings.Color_White);
			}
		}

        public void UpdateJackerColour() {

            int inCar = 0;
            if (GetProp("GameRawData.Telemetry.dcWeightJackerRight") != null) {
                inCar = Convert.ToInt32(GetProp("GameRawData.Telemetry.dcWeightJackerRight"));
            }
            int inSetup = 0;
            if (GetProp("DataCorePlugin.GameRawData.SessionData.CarSetup.Chassis.Rear.WeightJacker") != null) {
                inSetup = Convert.ToInt32(GetProp("DataCorePlugin.GameRawData.SessionData.CarSetup.Chassis.Rear.WeightJacker"));
            }



            if (inCar == inSetup) {
                SetProp("JackerColour", "Green");
            }
            else {
                SetProp("JackerColour", Settings.Color_White);
            }
        }

        public void UpdateRearARBColour() {

			int inCar = 0;
			if (GetProp("GameRawData.Telemetry.dcAntiRollRear") != null) {
				inCar = Convert.ToInt32(GetProp("GameRawData.Telemetry.dcAntiRollRear"));
			}
			int inSetup = -1;
			try {

                if (GetProp("DataCorePlugin.GameRawData.SessionData.CarSetup.Chassis.Rear.ArbBladeSetting") != null) {
                    inSetup = Convert.ToInt32(GetProp("DataCorePlugin.GameRawData.SessionData.CarSetup.Chassis.Rear.ArbBladeSetting"));
                }
                else if (GetProp("DataCorePlugin.GameRawData.SessionData.CarSetup.Chassis.Rear.ArbBlades") != null) {
                    inSetup = Convert.ToInt32(GetProp("DataCorePlugin.GameRawData.SessionData.CarSetup.Chassis.Rear.ArbBlades"));
                }
                else if (GetProp("DataCorePlugin.GameRawData.SessionData.CarSetup.Chassis.RearArb.ArbBlades") != null) {
                    inSetup = Convert.ToInt32(GetProp("DataCorePlugin.GameRawData.SessionData.CarSetup.Chassis.RearArb.ArbBlades"));
                }
            }
			catch (Exception) {
			}
            if (inCar == inSetup) {
				SetProp("ARBColourRear", "Green");
			}
			else {
				SetProp("ARBColourRear", Settings.Color_White);
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
            else if (GetProp("DataCorePlugin.GameRawData.SessionData.CarSetup.Chassis.InCarAdjustments.BrakePressureBias") != null) {
                setupBias = GetProp("DataCorePlugin.GameRawData.SessionData.CarSetup.Chassis.InCarAdjustments.BrakePressureBias");
            }

            Settings.SetupBrakeBiasPercentage = double.Parse(setupBias.Replace("%", ""));
            double bias = GetProp("BrakeBias");

            SetProp("Common.Bias.Setup", Settings.SetupBrakeBiasPercentage);
			SetProp("Common.Bias.Preferred", Settings.PreferredBrakeBiasPercentage);
            SetProp("Common.Bias.Delta", (bias - Settings.SetupBrakeBiasPercentage).ToString("-0.0"));

			if (bias < Settings.SetupBrakeBiasPercentage) {
                SetProp("Common.Bias.Color", Settings.Color_Green);
                SetProp("BrakeBiasColour", Settings.Color_Green);
			}
			else if (bias == Settings.PreferredBrakeBiasPercentage) {
                SetProp("Common.Bias.Color", Settings.Color_LightBlue);
                SetProp("BrakeBiasColour", "DeepSkyBlue");
			}
            else if (bias > Settings.SetupBrakeBiasPercentage) {
                SetProp("Common.Bias.Color", Settings.Color_Red);
                SetProp("BrakeBiasColour", Settings.Color_Red);
            }

            else {
                SetProp("Common.Bias.Color", Settings.Color_White);
                SetProp("BrakeBiasColour", Settings.Color_White);
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
            else if (GetProp("DataCorePlugin.GameRawData.SessionData.CarSetup.Chassis.InCarAdjustments.TcSetting") != null) {
                string[] splitter = GetProp("DataCorePlugin.GameRawData.SessionData.CarSetup.Chassis.InCarAdjustments.TcSetting").Split(' ');
                setupVal = Convert.ToString(splitter[0]);
            }
            string tc = Convert.ToString(GetProp("DataCorePlugin.GameData.TCLevel"));
			if (tc == setupVal) {
				SetProp("TCColour", "Green");
			}
			else {
				SetProp("TCColour", Settings.Color_White);
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
            else if (GetProp("Added") != null) {
                string[] splitter = GetProp("DataCorePlugin.GameRawData.SessionData.CarSetup.Chassis.InCarAdjustments.AbsSetting").Split(' ');
                setupVal = Convert.ToString(splitter[0]);
            }

            string absLevel = Convert.ToString(GetProp("DataCorePlugin.GameData.ABSLevel"));
			if (absLevel == setupVal) {
				SetProp("ABSColour", "Green");
			}
			else {
				SetProp("ABSColour", Settings.Color_White);
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
				SetProp("MAPColour", Settings.Color_White);
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
				SetProp("ThrottleShapeColour", Settings.Color_White);
			}
		}

		public void UpdateTCValues() {

            SetProp("TCLabelHighValue", "HI OFF");
            SetProp("TCLabelLowValue", "LOW");

            switch (GetProp("DataCorePlugin.GameData.CarModel")) {
				case "Ford GT GT3":
				case "Audi R8 LMS":
				case "BMW M4 GT3":
                    SetProp("TCLabelLowValue", "LOW");
                    SetProp("TCLabelHighValue", "HI OFF");
                    if (GetProp("TCLevel") == 10) {
                        SetProp("TCIsOff", true);
                    }
                    else {
                        SetProp("TCIsOff", false);
                    }
                    break;
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

            SetProp("ABSHighValueLabel", "HI AID");
            SetProp("ABSLowValueLabel", "OFF");

            switch (GetProp("DataCorePlugin.GameData.CarModel")) {
				case "Ford GT GT3":
				case "Audi R8 LMS":
				case "BMW M4 GT3":
                    SetProp("ABSHighValueLabel", "HI OFF");
                    SetProp("ABSLowValueLabel", "LOW");
                    if (GetProp("ABSLevel") == 12) {
                        SetProp("ABSIsOff", true);
                    }
                    else {
                        SetProp("ABSIsOff", false);
                    }

                    break;
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
                case "Dallara IR18":
                    switch (mapVal) {
                        case 1:
                            SetProp("MAPLabel", "1 - RACE");
                            SetProp("MAPLabelColour", "GREEN");
                            break;
                        case 2:
                            SetProp("MAPLabel", "2 - SAVE");
                            SetProp("MAPLabelColour", "Orange");
                            break;
                        case 3:
                            SetProp("MAPLabel", "3 - SAVE");
                            SetProp("MAPLabelColour", "Orange");
                            break;
                        case 4:
                            SetProp("MAPLabel", "4 - SAVE");
                            SetProp("MAPLabelColour", "Orange");
                            break;
                        case 5:
                            SetProp("MAPLabel", "5 - SAVE");
                            SetProp("MAPLabelColour", "Orange");
                            break;
                        case 6:
                            SetProp("MAPLabel", "6 - RACE");
                            SetProp("MAPLabelColour", "Green");
                            break;
                        case 7:
                            SetProp("MAPLabel", "7 - RACE");
                            SetProp("MAPLabelColour", "Green");
                            break;
                        case 8:
                            SetProp("MAPLabel", "8 - PACING");
                            SetProp("MAPLabelColour", "Orange");
                            break;
                        default:
                            SetProp("MAPLabel", mapVal.ToString());
                            break;
                    }
                    break;
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

		public void UpdateBitePointRecommendation() {
			int value = Convert.ToInt16(GetProp("DahlDesign.LaunchBitePoint"));
            if (!Settings.LaunchUsingDualClutchPaddles) {
				value = value + 2;
            }

			if (Settings.PreferFullThrottleStarts) {
				value = value + 2;
			}
			SetProp("LaunchPreferFullThrottleStarts", Settings.PreferFullThrottleStarts);
			SetProp("LaunchUsingDualClutchPaddles", Settings.LaunchUsingDualClutchPaddles);

			if (Settings.AdjustBiteRecommendationForTrackTemp) {
				double temp = Convert.ToDouble(GetProp("DataCorePlugin.GameData.RoadTemperature"));
				if (temp > 25) {
					value = value +1 ;
				}
                else if (temp > 30){
					value = value + 2;
				}
				else if (temp > 40) {
					value = value + 3;
				}
			}
			SetProp("LaunchBitePointAdjusted", value);

		}



	}
}
