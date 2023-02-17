using SimHub.Plugins;
using System;

namespace APR.DashSupport {
    public partial class DashPlugin : IPlugin, IDataPlugin, IWPFSettingsV2 {
        public string MainMenuValue() {
            string selectedMenu;
            switch (this.menuRotary) {
                case 1:
                    selectedMenu = "home";
                    break;
                case 2:
                    selectedMenu = "strat";
                    break;

                case 3:
                    selectedMenu = "pace";
                    break;

                case 4:
                    selectedMenu = "settings";
                    break;

                case 5:
                    selectedMenu = "launch";
                    break;
                default:
                    selectedMenu = "none";
                    break;
            }
            return selectedMenu;
        }
    }
}