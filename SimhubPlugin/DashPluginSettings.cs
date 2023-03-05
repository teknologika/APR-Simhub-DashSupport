using GameReaderCommon.Replays;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace APR.DashSupport
{


    /// <summary>
    /// Settings class, make sure it can be correctly serialized using JSON.net
    /// </summary>
    public class DashPluginSettings :INotifyPropertyChanged
    {

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

        
        // Settings to support standings
        
        public bool EnableStandings { get; set; } = false;
        public int SlideOutDelay { get; set; } = 20;
        public bool Multiclass { get; set; } = false;
        public int NumberOfMulticlassDrivers { get; set; } = 3;
        public bool ShowCarClassName { get; set; } = false;

        public int DriverNameStyle { get; set; } = 0;
        public bool SettingsUpdated { get; set; } = false;
        public bool DriverNameStyle_0 { get; set; } = true;
        public bool DriverNameStyle_1 { get; set; } = false;
        public bool DriverNameStyle_2 { get; set; } = false;
        public bool DriverNameStyle_3 { get; set; } = false;
        public bool DriverNameStyle_4 { get; set; } = false;
        public bool DriverNameStyle_5 { get; set; } = false;
        public bool DriverNameStyle_6 { get; set; } = false;

        public bool ShowGapToLeader { get; set; } = true;
        public bool ShowGapToCarInFront { get; set; } = true;
        public bool ShowFastestLap { get; set; } = true;
        public bool ShowLastlap { get; set; } = true;
        
        public bool SlideOutLastLapTimes { get; set; } = true;
        public bool SlideOutPitStatus { get; set; } = true;

        public bool ShowCarNumber { get; set; } = true;

        public int ColumnWidthPosition { get; set; } = 25;
        public int ColumnWidthCarNumber { get; set; } = 40;
        public int ColumnWidthDriverName { get; set; } = 170;
        public int RowWidth { get; set; }
        public int ColumnWidthGapToLeader { get; set; } = 40;
        public int ColumnWidthGapToCarInFront { get; set; } = 40;
        public int ColumnWidthFastestLap { get; set; } = 40;
        public int ColumnWIdthLastLap { get; set; } = 40;



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