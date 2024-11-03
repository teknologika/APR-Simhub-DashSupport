using GameReaderCommon.Replays;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.Eventing.Reader;
using System.Drawing;
using System.Drawing.Printing;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Windows.Controls;
using System.Windows.Forms;
using Color = System.Drawing.Color;

namespace APR.DashSupport
{


    /// <summary>
    /// Settings class, make sure it can be correctly serialized using JSON.net
    /// </summary>
    public class DashPluginSettings : INotifyPropertyChanged
    {
        public int MaxCars = 64;

        // General colours and layout settings
        public string Color_VeryDarkGrey = "#FF292929";
        public string Color_DarkGrey = "#FF898989";
        public string Color_LightGrey = "#FF808080";
        public string Color_Purple = "#Ff990099";
        public string Color_Green = "#FF009933";
        public string Color_Blue = "#FF0000ff";
        public string Color_White = "#FFFFFFFF";
        public string Color_Black = "#FF000000";
        public string Color_Yellow = "#FFFFFF00";
        public string Color_Red = "#FFFF0000";
        public string Color_RedLineFlash = "#FF800000"; // Maroon
        public string Color_Transparent = "#00000000";
        public string Color_DarkBackground = "#FF2D2D2D";
        public string Color_LightBlue = "DeepSkyBlue";


        // Relative Settings
        public int RelativeNumberOfCarsAheadToShow { get; set; } = 5;
        public int RelativeNumberOfCarsBehindToShow { get; set; } = 5;
        public int RelativeFontSize { get; set; } = 14;
        public int RelativeRowHeight { get; set; } = 25;
        public int RelativeRowOffset { get; set; } = 4;

        public bool EnableRPMBar { get; set; } = true;
        public bool EnableBrakeAndThrottleBars { get; set; } = true;
        public double BrakeTargetPercentage { get; set; } = 85;
        public double BrakeMaxPercentage { get; set; } = 99.9;
        public double BrakeTrailStartPercentage { get; set; } = 15;
        public double BrakeTrailEndPercentage { get; set; } = 20;
        public double PreferredBrakeBiasPercentage { get; set; } = 52.5;
        public double SetupBrakeBiasPercentage { get; set; } = 0.0;

        public bool EnablePitWindowPopup { get; set; } = true;
        public double PitWindowPopupPercentage { get; set; } = 33;
        public bool EnableFuelPopup { get; set; } = true;
        public double FuelPopupPercentage { get; set; } = 66;
        public bool PreferFullThrottleStarts { get; set; } = false;
        public bool AdjustBiteRecommendationForTrackTemp { get; set; } = false;
        public bool LaunchUsingDualClutchPaddles { get; set; } = false;

        public string Strategy_SelectedStrategy { get; set; } = "A"; // can be A, B, C or D
        public string Strategy_SelectedRiskLevel { get; set; } = "med"; // can be Low, Med, Higi
        public int Strategy_CPS_Completed { get; set; } = 0;
        public bool Strategy_UnderSC { get; set; } = false;
        public bool Strategy_RCMode { get; set; } = false;

        public string AudioSamplesOutputDevice { get; set; } = "Sample (TC-HELICON GoXLR Mini)";
        public string AudioSamplesFolder { get; set; } = "./RCSamples/";

        public uint ControlMapperVoyInstance { get; set; } = 3;
        public uint ControlMapperVoyPushToTalkButtonId { get; set; } = 1;

        // Settings to support standings


        // Enable Standings Properties and calculations
        public bool EnableStandings { get; set; } = true;

        

        public bool Standinds_Options_ShowTeamName { get; set; } = true;
        public int Standings_Layout_FontSize { get; set; } = 14;
     
        

        // Enable Multi-class in standings
        public bool Multiclass { get; set; } = false;
        public int NumberOfMulticlassDrivers { get; set; } = 3;
        public bool ShowCarClassName { get; set; } = false;
        public bool UseGapBasedPositions { get; set; } = true;

        public string StandingsBackgroundColour { get; set; } = "#00000";
        public string StandingsBackgroundRowAlternateColour { get; set; } = "#00000";
        public string ReferenceDriverBackgroundColour { get; set; } = "#044BFC";
        public double BackgroundTransparency { get; set; } = 0.2;
        public double BackgroundAlternateTransparency { get; set; } = 0.4;
       
        public string StandingsBackgroundRowColourWithTransparency {
            get {
                Color colour = ColorTranslator.FromHtml(StandingsBackgroundColour);
                int opacity = Convert.ToInt16(255 * (1 - BackgroundTransparency));
                Color colourWithAplha = Color.FromArgb(opacity, colour);
                return string.Format("#{0:X2}{1:X2}{2:X2}{3:X2}", colourWithAplha.A, colourWithAplha.R, colourWithAplha.G, colourWithAplha.B);

            }
        }
        public string StandingsBackgroundRowAlternateColourWithTransparency {
            get {
                Color colour = ColorTranslator.FromHtml(StandingsBackgroundRowAlternateColour);
                int opacity = Convert.ToInt16(255 * (1 - BackgroundAlternateTransparency));
                Color colourWithAplha = Color.FromArgb(opacity, colour);
                return string.Format("#{0:X2}{1:X2}{2:X2}{3:X2}", colourWithAplha.A, colourWithAplha.R, colourWithAplha.G, colourWithAplha.B);

            }
        }
        public string StandingsBackgroundDriverReferenceRowColourWithTransparency {
            get {
                Color colour = ColorTranslator.FromHtml(ReferenceDriverBackgroundColour);
                int opacity = Convert.ToInt16(255 * (1 - BackgroundTransparency));
                Color colourWithAplha = Color.FromArgb(opacity, colour);
                return string.Format("#{0:X2}{1:X2}{2:X2}{3:X2}", colourWithAplha.A, colourWithAplha.R, colourWithAplha.G, colourWithAplha.B);

            }
        }

        // Relatives Settings
        public bool EnableRelatives { get; set; } = true;
        public bool RelativeShowCarsInPits { get; set; } = false;


        // Pit Strategy Calculator Settings
        public bool EnableStrategyCalculation { get; set; } = false;

        // If we want to do the calcs ourselves, we can override values in the UI
        public double Strategy_OverrideStartingFuelPercentage { get; set; } = 100;
        public double Strategy_OverrideAvailableTankSizePercentage { get; set; } = 100;
        public double Strategy_OverrideFuelPerLap { get; set; } = 0;
        public double Strategy_OverrideNumberOfRaceLaps { get; set; } = 0;
        public double Strategy_OverrideAvailableTankSize{ get; set; } = 0;


        public int SlideOutDelay { get; set; } = 20;

        public bool SettingsUpdated { get; set; } = false;

        public int DriverNameWidth {
            get {
                // Update the driver's display name
                switch (DriverNameStyle) {
                    case 0: // Firstname Middle Lastname
                    case 7: // Best team name on the planet
                        return 200;

                    case 1: // Firstname Lastname
                    case 2: // Lastname, Firstname
                        return 150;

                    case 3: // F. Lastname
                    case 4: // Firstname L.
                    case 5: // Lastname, F.
                        return 100;

                    case 6: // LAS
                        return 50;


                    default: //   Firstname Middle Lastname
                        return 150;
                }
            }
        }


        // The driver name style dropdown
        public int DriverNameStyle {
            get {
                if (DriverNameStyle_0) {
                    return 0;
                }
                else if (DriverNameStyle_1) {
                    return 1;
                }
                else if (DriverNameStyle_2) {
                    return 2;
                }
                else if (DriverNameStyle_3) {
                    return 3;
                }
                else if (DriverNameStyle_4) {
                    return 4;
                }
                else if (DriverNameStyle_5) {
                    return 5;
                }
                else if (DriverNameStyle_6) {
                    return 6;
                }
                else if (DriverNameStyle_7) {
                    return 7;
                }
                else {
                    return 0;
                }
            }
        }
         
        public bool DriverNameStyle_0 { get; set; } = true;
        public bool DriverNameStyle_1 { get; set; } = false;
        public bool DriverNameStyle_2 { get; set; } = false;
        public bool DriverNameStyle_3 { get; set; } = false;
        public bool DriverNameStyle_4 { get; set; } = false;
        public bool DriverNameStyle_5 { get; set; } = false;
        public bool DriverNameStyle_6 { get; set; } = false;
        public bool DriverNameStyle_7 { get; set; } = false;


        // The data so show dropdown 

        // The driver name style dropdown
        public int Standings_MiscDataToShow {
            get {
                if (Standings_MiscDataToShow_0) {
                    return 0;
                }
                else if (Standings_MiscDataToShow_1) {
                    return 1;
                }
                else if (Standings_MiscDataToShow_2) {
                    return 2;
                }
                else if (Standings_MiscDataToShow_3) {
                    return 3;
                }
                else if (Standings_MiscDataToShow_4) {
                    return 4;
                }
                else if (Standings_MiscDataToShow_5) {
                    return 5;
                }
                else if (Standings_MiscDataToShow_6) {
                    return 6;
                }
                else {
                    return 0;
                }
            }
        }

        public string Standings_MiscDataToShowString {
            get {
                if (Standings_MiscDataToShow_0) {
                    return "None";
                }
                else if (Standings_MiscDataToShow_1) {
                    return "GainLoss";
                }
                else if (Standings_MiscDataToShow_2) {
                    return "NumberOfStops";
                }
                else if (Standings_MiscDataToShow_3) {
                    return "NumberOfCPS";
                }
                else if (Standings_MiscDataToShow_4) {
                    return "CPSIndicators";
                }
                else if (Standings_MiscDataToShow_5) {
                    return "";
                }
                else if (Standings_MiscDataToShow_6) {
                    return "";
                }
                else {
                    return "";
                }
            }
        }

        public bool Standings_MiscDataToShow_0 { get; set; } = true;   // None
        public bool Standings_MiscDataToShow_1 { get; set; } = false;  // Gain / loss 
        public bool Standings_MiscDataToShow_2 { get; set; } = false;  // CPS Indicators
        public bool Standings_MiscDataToShow_3 { get; set; } = false;  // Number of CPS
        public bool Standings_MiscDataToShow_4 { get; set; } = false;  // Number of Stops
        public bool Standings_MiscDataToShow_5 { get; set; } = false;  // not used
        public bool Standings_MiscDataToShow_6 { get; set; } = false;  // not used
 


        public bool ShowGapToLeader { get; set; } = true;
        public bool ShowGapToCarInFront { get; set; } = true;
        public bool ShowFastestLap { get; set; } = true;
        public bool ShowLastlap { get; set; } = true;
        
        
        public bool SlideOutLastLapTimes { get; set; } = true;
        public bool SlideOutPitStatus { get; set; } = true;

        public bool ShowCarNumber { get; set; } = true;


        public bool ColumnShowPosition { get; set; } = true;
        public bool ColumnShowCarNumber { get; set; } = true;
        public bool ColumnShowDriverName { get; set; } = true;
        public bool ColumnShowGapToLeader { get; set; } = true;
        public bool ColumnShowGapToCarInFront { get; set; } = true;
        public bool ColumnShowMiscData { get; set; } = true;
        public bool ColumnShowFastestLap { get; set; } = true;
        public bool ColumnShowLastLap { get; set; } = true;

       
        public int ColumnWidthPosition { get; set; } = 30;
        public int ColumnWidthCarNumber { get; set; } = 30;
        public int ColumnWidthDriverName { get; set; } = 200;
        public int ColumnWidthGapToLeader { get; set; } = 60;
        public int ColumnWidthGapToCarInFront { get; set; } = 60;
        public int ColumnWidthMiscData { get; set; } = 60;
        public int ColumnWidthFastestLap { get; set; } = 80;
        public int ColumnWidthLastLap { get; set; } = 80;

   
        public int ColumnStartPosition {
            get {
                return 0;
            }
        }

        public int ColumnStartCarNumber {
            get {
                if (ColumnShowPosition) {
                    return ColumnStartPosition + ColumnWidthPosition;

                }
                else {
                    return ColumnStartPosition;
                }
            }
        }

        public int ColumnStartDriverName {
            get {
                if (ColumnShowCarNumber) {
                    return ColumnStartCarNumber + ColumnWidthCarNumber;
                }
                else {
                    return ColumnStartCarNumber;
                }
            }

        }

        public int ColumnStartGapToLeader {
            get {
                if (ColumnShowDriverName) {
                    return ColumnStartDriverName + ColumnWidthDriverName;
                }
                else {
                    return ColumnStartDriverName;
                }
            }
        }

        public int ColumnStartGapToCarInFront {
            get {
                if (ColumnShowGapToLeader) {
                    return ColumnStartGapToLeader + ColumnWidthGapToLeader;
                }
                else {
                    return ColumnStartGapToLeader;
                }
            }
        }

        public int ColumnStartMiscData {
            get {
                if (ColumnShowGapToCarInFront) {
                    return ColumnStartGapToCarInFront + ColumnWidthGapToCarInFront;
                }
                else {
                    return ColumnStartGapToCarInFront;
                }
            }
        }

        public int ColumnStartLastLap {
            get {
                if (ColumnShowMiscData) {
                    return ColumnStartMiscData + ColumnWidthMiscData;
                }
                else {
                    return ColumnStartMiscData;
                }
            }
        }

        public int ColumnStartFastestLap {
            get {
                if (ColumnShowLastLap) {
                    return ColumnStartLastLap + ColumnWidthLastLap;
                }
                else {
                    return ColumnStartLastLap;
                }
            }
        }

        public int ColumnStartFastestLapSlider {
            get {
                if (ColumnShowLastLap) {
                    return ColumnStartLastLap + ColumnWidthLastLap;
                }
                else {
                    return ColumnStartLastLap;
                }
            }
        }

        public int ColumnSlideOutDuration { get; set; } = 10;
        public int ColumnCycleDuration { get; set; } = 30;
        public bool ColumnSlideOutFastestLap { get; set; } = true;
        public bool ColumnCycleDisplay { get; set; } = false;

        public int RowWidth { get; set; }
        public int RowHeight { get; set; } = 25;
        public int RowGapBetweenRows { get; set; } = 2;



        #region Property supporting UI refresh from code
        /*
        private string _FilePath;
        public string FilePath
        {
            get => _FilePath;
            set => SetField(ref _FilePath, value);
        }
        */
        #endregion

        #region Utilities methods to refresh the UI see https://docs.microsoft.com/en-us/dotnet/api/system.componentmodel.inotifypropertychanged?view=netframework-4.7.2

        protected void OnPropertyChanged(string propertyName)
          => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null) {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        #endregion

    }

}