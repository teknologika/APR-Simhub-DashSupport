using SimHub.Plugins;
using System;

namespace APR.DashSupport {
    public partial class APRDashPlugin : IPlugin, IDataPlugin, IWPFSettingsV2 {
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

        public void UpdatePitWindowMessage() {
            // This method is a lot of messy nested if statements. This is deliberate so the higher priority messages appear first

            int ddTrackType = Convert.ToInt16(GetProp("DahlDesign.TrackType"));
            double ddFuelPitStops = Convert.ToDouble(GetProp("DahlDesign.FuelPitStops"));
            double ddFuelDelta = Convert.ToDouble(GetProp("DahlDesign.FuelDelta"));
            bool ddFuelAlert = Convert.ToBoolean(GetProp("DahlDesign.FuelAlert"));
            int ddFuelPitWindowFirst = Convert.ToInt16(GetProp("DahlDesign.FuelPitWindowFirst"));
            int ddFuelPitWindowLast = Convert.ToInt16(GetProp("DahlDesign.FuelPitWindowLast"));
            int currentLap = Convert.ToInt16(GetProp("DataCorePlugin.GameData.CurrentLap"));
            double ddFuelConserveToSaveAStop = Convert.ToDouble(GetProp("DahlDesign.FuelConserveToSaveAStop"));

            // If we need to pit this lap .. shout it out
            if (ddFuelAlert) {
                SetProp("PitWindowMessage", "BOX");
                SetProp("PitWindowTextColour", Settings.Color_White);
                SetProp("pitWindowBackGroundColour", "Red");
            }
            else {
                // if we need to save to make a stop
                // [DahlDesign.FuelConserveToSaveAStop] > 0 && [DahlDesign.FuelConserveToSaveAStop] != 0 && [DahlDesign.FuelConserveToSaveAStop] < 0.15
                if (ddFuelConserveToSaveAStop > 0 && ddFuelConserveToSaveAStop < 0.15) {
                    SetProp("PitWindowMessage", "SAVE");
                    SetProp("PitWindowTextColour", "Black");
                    SetProp("pitWindowBackGroundColour", "Yellow");
                }
                else {
                    // if the pit window is open
                    //[DahlDesign.FuelPitWindowFirst] <= [CurrentLap] && [DahlDesign.FuelDelta] < 0
                    if (ddFuelPitWindowFirst <= currentLap && ddFuelDelta < 0) {
                        SetProp("PitWindowMessage", "OPEN");
                        SetProp("PitWindowTextColour", "Black");
                        SetProp("pitWindowBackGroundColour", "lawngreen");
                    }
                    else {

                        // This only works for Road and Oval courses
                        // 0 = road, 1-3 = rallycross, 4 = dirt road w/o joker laps, 5-7 = short to long ovals, 8 = dirt oval
                        if (ddTrackType > 0 && ddTrackType < 5) {
                            SetProp("PitWindowMessage", "");
                            SetProp("PitWindowTextColour", "Transparent");
                            SetProp("pitWindowBackGroundColour", "Transparent"); 
                        }
                        else {
                            // if we have more fuel than needed, no stops
                            if (ddFuelPitStops > 0 && ddFuelDelta >= 0) {
                                SetProp("PitWindowMessage","NO PIT");
                                SetProp("PitWindowTextColour", "Black");
                                SetProp("pitWindowBackGroundColour","Grey");
                            }

                        }
                    }
                }
            }
        }

    }
}