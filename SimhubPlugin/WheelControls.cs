using GameReaderCommon;
using GameReaderCommon.Replays;
using SimHub.Plugins;
using System;
using System.Diagnostics.Eventing.Reader;
using static System.Net.Mime.MediaTypeNames;

namespace APR.DashSupport {
    public partial class APRDashPlugin : IPlugin, IDataPlugin, IWPFSettingsV2 {

        // Rotaries match GSI Wheel names
        private int menuRotary = 1;
        private int scrlRotary = 1;

        // Rotary buttons
        private bool menuRotaryPressed = false;
        private bool scrlRotaryPressed = false;
        private bool optRotaryPressed = false;
        private bool biasRotaryPressed = false;
        private bool centreRotaryPressed = false;

        // Buttons match GSI Wheel names
        private bool flashButtonPressed = false;
        private bool pitLimitPressed = false;
        private bool enablePressed = false;
        private bool radioPressed = false;
        private bool volUpPressed = false;
        private bool volDownPressed = false;
        private bool prevButtonPressed = false;
        private bool nextButtonPressed = false;
        private bool thanksButtonPressed = false;
        private bool sorryButtonPressed = false;
        private bool randomInsultAheadPressed = false;
        private bool randomInsultBehindPressed = false;
        private bool passLeftPressed = false;
        private bool passRightPressed = false;

        public void RaceControlAction(string ChatText, string AudioSampleName) {
            
            RadioAndTextChat.pressVJoyButton(Settings.ControlMapperVoyInstance, Settings.ControlMapperVoyPushToTalkButtonId);
            RadioAndTextChat.iRacingChat(ChatText,IsIRacingAdmin);
            RadioAndTextChat.playSound(Settings.AudioSamplesFolder + AudioSampleName, Settings.AudioSamplesOutputDevice);
            RadioAndTextChat.releaseVJoyButton(Settings.ControlMapperVoyInstance, Settings.ControlMapperVoyPushToTalkButtonId);
        }

        public void RaceControlAction(string ChatTextOne, string ChatTextTwo, string AudioSampleName) {
            RadioAndTextChat.pressVJoyButton(Settings.ControlMapperVoyInstance, Settings.ControlMapperVoyPushToTalkButtonId);
            RadioAndTextChat.iRacingChat(ChatTextOne, IsIRacingAdmin);
            RadioAndTextChat.iRacingChat(ChatTextTwo, IsIRacingAdmin);
            RadioAndTextChat.playSound(Settings.AudioSamplesFolder + AudioSampleName, Settings.AudioSamplesOutputDevice);
            RadioAndTextChat.releaseVJoyButton(Settings.ControlMapperVoyInstance, Settings.ControlMapperVoyPushToTalkButtonId);
        }

        public void PrivateChatCarBehindAction(string Text) {
            if (DriverBehindId != string.Empty) {
                Text = "/" + DriverBehindId + " " + Text;
                RadioAndTextChat.iRacingChat(Text, false);
            }
            
        }

        public void PrivateChatCarAheadAction(string Text) {
            if (DriverAheadId != string.Empty) {
                Text = "/" + DriverAheadId + " " + Text;
                RadioAndTextChat.iRacingChat(Text, false);
            } 
        }

        public void InitRotaries(PluginManager pluginManager) {

            // this.AttachDelegate("Rotary_MenuValue", () => this.menuRotary);
            pluginManager.AddAction("MenuRotaryIncremented", this.GetType(), (Action<PluginManager, string>)((a, b) => {
                ++this.menuRotary;
                if (this.menuRotary > 7)
                    this.menuRotary = 1;
            }));

            pluginManager.AddAction("MenuRotaryDecremented", this.GetType(), (Action<PluginManager, string>)((a, b) => {
                --this.menuRotary;
                if (this.menuRotary < 1)
                    this.menuRotary = 7;
            }));

            // this.AttachDelegate("Rotary_ScrlValue", () => this.scrlRotary);
            pluginManager.AddAction("ScrlRotaryIncremented", this.GetType(), (Action<PluginManager, string>)((a, b) => {
                ++this.scrlRotary;
                if (this.scrlRotary > 12)
                    this.scrlRotary = 1;
            }));
            pluginManager.AddAction("ScrlRotaryDecremented", this.GetType(), (Action<PluginManager, string>)((a, b) => {
                --this.scrlRotary;
                if (this.scrlRotary < 1)
                    this.scrlRotary = 12;
            }));
        }

        public void InitRotaryButtons(PluginManager pluginManager) {

            // Rotary encoder "buttons"
            this.AttachDelegate("Rotary_MenuPressed", () => this.menuRotaryPressed);
            pluginManager.AddAction("MenuRotaryPressed", this.GetType(), (Action<PluginManager, string>)((a, b) => this.menuRotaryPressed = true));
            pluginManager.AddAction("MenuRotaryReleased", this.GetType(), (Action<PluginManager, string>)((a, b) => this.menuRotaryPressed = false));

            this.AttachDelegate("Rotary_ScrlPressed", () => this.scrlRotaryPressed);
            pluginManager.AddAction("ScrlRotaryPressed", this.GetType(), (Action<PluginManager, string>)((a, b) => this.scrlRotaryPressed = true));
            pluginManager.AddAction("ScrlRotaryReleased", this.GetType(), (Action<PluginManager, string>)((a, b) => this.scrlRotaryPressed = false));

            this.AttachDelegate("Rotary_OptPressed", () => this.optRotaryPressed);
            pluginManager.AddAction("OptRotaryPressed", this.GetType(), (Action<PluginManager, string>)((a, b) => this.optRotaryPressed = true));
            pluginManager.AddAction("OptRotaryReleased", this.GetType(), (Action<PluginManager, string>)((a, b) => this.optRotaryPressed = false));

            this.AttachDelegate("Rotary_BiasPressed", () => this.biasRotaryPressed);
            pluginManager.AddAction("BiasRotaryPressed", this.GetType(), (Action<PluginManager, string>)((a, b) => this.biasRotaryPressed = true));
            pluginManager.AddAction("BiasRotaryReleased", this.GetType(), (Action<PluginManager, string>)((a, b) => this.biasRotaryPressed = false));

            this.AttachDelegate("Rotary_CentrePressed", () => this.centreRotaryPressed);
            pluginManager.AddAction("CentreRotaryPressed", this.GetType(), (Action<PluginManager, string>)((a, b) => this.centreRotaryPressed = true));
            pluginManager.AddAction("CentreRotaryReleased", this.GetType(), (Action<PluginManager, string>)((a, b) => this.centreRotaryPressed = false));
        }

        public void InitStrategyButtons(PluginManager pluginManager) {

            pluginManager.AddAction("Strategy.btnStratAPressed", this.GetType(), (Action<PluginManager, string>)((a, b) => {
                Settings.Strategy_SelectedStrategy = "A";
                SetProp("Strategy.Indicator.StratMode", Settings.Strategy_SelectedStrategy);
            }));
  
            pluginManager.AddAction("Strategy.btnStratBPressed", this.GetType(), (Action<PluginManager, string>)((a, b) => {
                Settings.Strategy_SelectedStrategy = "B";
                SetProp("Strategy.Indicator.StratMode", Settings.Strategy_SelectedStrategy);
            }));

            pluginManager.AddAction("Strategy.btnStratCPressed", this.GetType(), (Action<PluginManager, string>)((a, b) => {
                Settings.Strategy_SelectedStrategy = "C";
                SetProp("Strategy.Indicator.StratMode", Settings.Strategy_SelectedStrategy);
            }));
   
            pluginManager.AddAction("Strategy.btnStratDPressed", this.GetType(), (Action<PluginManager, string>)((a, b) => {
                Settings.Strategy_SelectedStrategy = "D";
                SetProp("Strategy.Indicator.StratMode", Settings.Strategy_SelectedStrategy);
            }));

            pluginManager.AddAction("Strategy.btnRiskLow", this.GetType(), (Action<PluginManager, string>)((a, b) => {
                Settings.Strategy_SelectedRiskLevel = "low";
                SetProp("Strategy.Indicator.RiskLevel", Settings.Strategy_SelectedRiskLevel);
            }));

            pluginManager.AddAction("Strategy.btnRiskMedium", this.GetType(), (Action<PluginManager, string>)((a, b) => {
                Settings.Strategy_SelectedRiskLevel = "med";
                SetProp("Strategy.Indicator.RiskLevel", Settings.Strategy_SelectedRiskLevel);
            }));

            pluginManager.AddAction("Strategy.btnRiskHigh", this.GetType(), (Action<PluginManager, string>)((a, b) => {
                Settings.Strategy_SelectedRiskLevel = "high";
                SetProp("Strategy.Indicator.RiskLevel", Settings.Strategy_SelectedRiskLevel);
            }));

            pluginManager.AddAction("Strategy.SetFuel", this.GetType(), (Action<PluginManager, string>)((a, b) => {
                RadioAndTextChat.iRacingChat(StrategyBundle.Instance.NextStopChatString, false);
            }));


            pluginManager.AddAction("Strategy.btnPaceCarPressed", this.GetType(), (Action<PluginManager, string>)((a, b) => {
                if (Settings.Strategy_UnderSC) {
                    Settings.Strategy_UnderSC = false;

                    // set the rotaty menu to 1 - Race
                    this.menuRotary = 1;
                }
                else {
                    Settings.Strategy_UnderSC = true;
                    // set the rotaty menu to 3 = pacing
                    this.menuRotary = 3;
                }
                SetProp("Strategy.Indicator.UnderSC", Settings.Strategy_UnderSC);

            }));

          

            /// Race Control specific buttons
            pluginManager.AddAction("RC.SC.Deploy", this.GetType(), (Action<PluginManager, string>)((a, b) => {
                RaceControlAction("*** SAFETY CAR *** SAFETY CAR *** SAFETY CAR ***", "bruce-SafetyCar.mp3");
            }));

            pluginManager.AddAction("RC.SC.PitExitClosed", this.GetType(), (Action<PluginManager, string>)((a, b) => {
                RaceControlAction("*** PIT EXIT IS CLOSED ***", "Bruce-PitExitIsNowClosed.mp3");
            }));

            pluginManager.AddAction("RC.SC.PitExitIsOpen", this.GetType(), (Action<PluginManager, string>)((a, b) => {
                RaceControlAction("*** PIT EXIT IS OPEN ***", "Bruce-PitExitIsNowOpen.mp3");
            }));

            pluginManager.AddAction("RC.SC.WeWillBeGoingGreen", this.GetType(), (Action<PluginManager, string>)((a, b) => {
                RaceControlAction("*** GOING GREEN THIS LAP ***", "Bruce-WeWillBeGoingGreen.mp3");
            }));

            pluginManager.AddAction("RC.SC.LineUpLeft", this.GetType(), (Action<PluginManager, string>)((a, b) => {
                RaceControlAction("*** ALL CARS LINE UP ON THE LEFT ***", "Bruce-AllCarsOnTheLeft.mp3");
            }));

            pluginManager.AddAction("RC.SC.LineUpRight", this.GetType(), (Action<PluginManager, string>)((a, b) => {
                RaceControlAction("*** LAPPED CARS LINE UP ON THE RIGHT ***", "Bruce-EligibleCarsOnTheRight.mp3");
            }));

            pluginManager.AddAction("RC.SC.LappedMayPass", this.GetType(), (Action<PluginManager, string>)((a, b) => {
                RaceControlAction("*** ELIGIBLE CARS MAY NOW PASS THE SC ***", "Bruce-LappedCarsMayPass.mp3");
            }));

            pluginManager.AddAction("RC.SC.WeWillUnlapCars", this.GetType(), (Action<PluginManager, string>)((a, b) => {
                RaceControlAction("*** WE WILL UNLAP ELIGIBLE CARS ***", "Bruce-WeWillUnlap.mp3");
            }));

            pluginManager.AddAction("RC.SC.SCEnteringPitlane", this.GetType(), (Action<PluginManager, string>)((a, b) => {
                RaceControlAction("*** SAFETY CAR ENTERING PITLANE ***", "bruce-SafetyCarInTheLane.mp3");
            }));

            // Opponent chat buttons
            pluginManager.AddAction("Chat.Ahead.BlueFlags", this.GetType(), (Action<PluginManager, string>)((a, b) => {
                if (DriverAheadName != "") {
                    PrivateChatCarAheadAction("Can I pass please " + DriverAheadName + "?");
                }
            }));

            pluginManager.AddAction("Chat.Behind.PittingIn", this.GetType(), (Action<PluginManager, string>)((a, b) => {
                if (DriverBehindName != "") {
                    PrivateChatCarBehindAction("I'm pitting " + DriverBehindName + ".");
                }
            }));

            pluginManager.AddAction("Chat.Behind.Thanks", this.GetType(), (Action<PluginManager, string>)((a, b) => {
                if (DriverBehindName != "") {
                    PrivateChatCarBehindAction("Thanks, " + DriverBehindName + "!!");
                }
            }));


            pluginManager.AddAction("Strategy.btnRCModePressed", this.GetType(), (Action<PluginManager, string>)((a, b) => {
                if (Settings.Strategy_RCMode) {
                    Settings.Strategy_RCMode = false;
                }
                else {
                    Settings.Strategy_RCMode = true;
                }
                SetProp("Strategy.Indicator.RCMode", Settings.Strategy_RCMode);
            }));


            pluginManager.AddAction("Strategy.btnPaceCarPressed", this.GetType(), (Action<PluginManager, string>)((a, b) => {
                if (Settings.Strategy_UnderSC) {
                    Settings.Strategy_UnderSC = false;
                }
                else {
                    Settings.Strategy_UnderSC = true;
                }
                SetProp("Strategy.Indicator.UnderSC", Settings.Strategy_UnderSC);
            }));


            pluginManager.AddAction("Strategy.btnCPS1Pressed", this.GetType(), (Action<PluginManager, string>)((a, b) => {
                if (Settings.Strategy_CPS_Completed == 0) {
                    Settings.Strategy_CPS_Completed++;
                }
                else if (Settings.Strategy_CPS_Completed == 1 ) {
                    Settings.Strategy_CPS_Completed--;
                }
                SetCPSIndicators();
                SetCPSIndicatorsLegacy();
            }));

            pluginManager.AddAction("Strategy.btnCPS2Pressed", this.GetType(), (Action<PluginManager, string>)((a, b) => {
                if (Settings.Strategy_CPS_Completed == 1) {
                    Settings.Strategy_CPS_Completed++;
                }
                else if (Settings.Strategy_CPS_Completed == 2) {
                    Settings.Strategy_CPS_Completed--;
                }
                SetCPSIndicators();
                SetCPSIndicatorsLegacy();
            }));

        }

        private void SetCPSIndicatorsLegacy() {
            switch (Settings.Strategy_CPS_Completed) {
                case 1:
                    SetProp("Strategy.Indicator.CPS1Served", true);
                    SetProp("Strategy.Indicator.CPS2Served", false);
                    break;
                case 2:
                    SetProp("Strategy.Indicator.CPS1Served", true);
                    SetProp("Strategy.Indicator.CPS2Served", true);
                    break;
                default:
                    SetProp("Strategy.Indicator.CPS1Served", false);
                    SetProp("Strategy.Indicator.CPS2Served", false);
                    break;
            }
        }

        private void SetCPSIndicators() {
            switch (Settings.Strategy_CPS_Completed) {
                case 1:
                    SetProp("APRDashPlugin.Spectated.PitStops.CPS1Served", true);
                    SetProp("APRDashPlugin.Spectated.PitStops.CPS2Served", false);
                    break;
                case 2:
                    SetProp("APRDashPlugin.Spectated.PitStops.CPS1Served", true);
                    SetProp("APRDashPlugin.Spectated.PitStops.CPS2Served", true);
                    break;
                default:
                    SetProp("APRDashPlugin.Spectated.PitStops.CPS1Served", false);
                    SetProp("APRDashPlugin.Spectated.PitStops.CPS2Served", false);
                    break;
            }
        }

        public void InitOtherButtons(PluginManager pluginManager) {

            this.AttachDelegate("Button_FlashPressed", () => this.flashButtonPressed);
            pluginManager.AddAction("btnFlashPressed", this.GetType(), (Action<PluginManager, string>)((a, b) => this.flashButtonPressed = true));
            pluginManager.AddAction("btnFlashReleased", this.GetType(), (Action<PluginManager, string>)((a, b) => this.flashButtonPressed = false));

            this.AttachDelegate("Button_PitLimit_Pressed", () => this.pitLimitPressed);
            pluginManager.AddAction("btnPitLimitPressed", this.GetType(), (Action<PluginManager, string>)((a, b) => this.pitLimitPressed = true));
            pluginManager.AddAction("btnPitLimitReleased", this.GetType(), (Action<PluginManager, string>)((a, b) => this.pitLimitPressed = false));

            this.AttachDelegate("Button_enablePressed", () => this.enablePressed);
            pluginManager.AddAction("btnEnablePressed", this.GetType(), (Action<PluginManager, string>)((a, b) => this.enablePressed = true));
            pluginManager.AddAction("btnEnableReleased", this.GetType(), (Action<PluginManager, string>)((a, b) => this.enablePressed = false));

            this.AttachDelegate("Button_radioPressed", () => this.radioPressed);
            pluginManager.AddAction("btnRadioPressed", this.GetType(), (Action<PluginManager, string>)((a, b) => this.radioPressed = true));
            pluginManager.AddAction("btnRadioReleased", this.GetType(), (Action<PluginManager, string>)((a, b) => this.radioPressed = false));

            this.AttachDelegate("Button_volUpPressed", () => this.volUpPressed);
            pluginManager.AddAction("btnVolUpPressed", this.GetType(), (Action<PluginManager, string>)((a, b) => this.volUpPressed = true));
            pluginManager.AddAction("btnVolUpReleased", this.GetType(), (Action<PluginManager, string>)((a, b) => this.volUpPressed = false));

            this.AttachDelegate("Button_VolDownPressed", () => this.volDownPressed);
            pluginManager.AddAction("btnVolDownPressed", this.GetType(), (Action<PluginManager, string>)((a, b) => this.volDownPressed = true));
            pluginManager.AddAction("btnVolDownReleased", this.GetType(), (Action<PluginManager, string>)((a, b) => this.volDownPressed = false));

            this.AttachDelegate("Button_prevPressed", () => this.prevButtonPressed);
            pluginManager.AddAction("btnPrevPressed", this.GetType(), (Action<PluginManager, string>)((a, b) => this.prevButtonPressed = true));
            pluginManager.AddAction("btnPrevReleased", this.GetType(), (Action<PluginManager, string>)((a, b) => this.prevButtonPressed = false));

            this.AttachDelegate("Button_nextPressed", () => this.nextButtonPressed);
            pluginManager.AddAction("btnNextPressed", this.GetType(), (Action<PluginManager, string>)((a, b) => this.nextButtonPressed = true));
            pluginManager.AddAction("btnNextReleased", this.GetType(), (Action<PluginManager, string>)((a, b) => this.nextButtonPressed = false));

            this.AttachDelegate("Button_ThanksPressed", () => this.thanksButtonPressed);
            pluginManager.AddAction("btnThanksPressed", this.GetType(), (Action<PluginManager, string>)((a, b) => this.thanksButtonPressed = true));
            pluginManager.AddAction("btnThanksPressed", this.GetType(), (Action<PluginManager, string>)((a, b) => this.thanksButtonPressed = false));

            this.AttachDelegate("Button_SorryPressed", () => this.sorryButtonPressed);
            pluginManager.AddAction("btnSorryPressed", this.GetType(), (Action<PluginManager, string>)((a, b) => this.sorryButtonPressed = true));
            pluginManager.AddAction("btnSorryPressed", this.GetType(), (Action<PluginManager, string>)((a, b) => this.sorryButtonPressed = false));

            this.AttachDelegate("Button_InsultAhead", () => this.randomInsultAheadPressed);
            pluginManager.AddAction("btnInsultAheadPressed", this.GetType(), (Action<PluginManager, string>)((a, b) => this.randomInsultAheadPressed = true));
            pluginManager.AddAction("btnInsultAheadPressed", this.GetType(), (Action<PluginManager, string>)((a, b) => this.randomInsultAheadPressed = false));

            this.AttachDelegate("Button_InsultBehindPressed", () => this.randomInsultBehindPressed);
            pluginManager.AddAction("btnInsultBehindPressed", this.GetType(), (Action<PluginManager, string>)((a, b) => this.randomInsultBehindPressed = true));
            pluginManager.AddAction("btnInsultBehindPressed", this.GetType(), (Action<PluginManager, string>)((a, b) => this.randomInsultBehindPressed = false));

            this.AttachDelegate("Button_PassLeftPressed", () => this.passLeftPressed);
            pluginManager.AddAction("btnPassLeftPressed", this.GetType(), (Action<PluginManager, string>)((a, b) => this.passLeftPressed = true));
            pluginManager.AddAction("btnPassLeftPressed", this.GetType(), (Action<PluginManager, string>)((a, b) => this.passLeftPressed = false));

            this.AttachDelegate("Button_PassRightPressed", () => this.passRightPressed);
            pluginManager.AddAction("btnPassRightPressed", this.GetType(), (Action<PluginManager, string>)((a, b) => this.passRightPressed = true));
            pluginManager.AddAction("btnPassRightPressed", this.GetType(), (Action<PluginManager, string>)((a, b) => this.passRightPressed = false));

        }
    }
}