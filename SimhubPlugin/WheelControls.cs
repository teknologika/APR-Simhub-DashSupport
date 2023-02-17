using SimHub.Plugins;
using System;

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


        public void InitRotaries(PluginManager pluginManager) {

            // this.AttachDelegate("Rotary_MenuValue", () => this.menuRotary);
            pluginManager.AddAction("MenuRotaryIncremented", this.GetType(), (Action<PluginManager, string>)((a, b) => {
                ++this.menuRotary;
                if (this.menuRotary > 5)
                    this.menuRotary = 1;
            }));

            pluginManager.AddAction("MenuRotaryDecremented", this.GetType(), (Action<PluginManager, string>)((a, b) => {
                --this.menuRotary;
                if (this.menuRotary < 1)
                    this.menuRotary = 5;
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
        }
    }
}