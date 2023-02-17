using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

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