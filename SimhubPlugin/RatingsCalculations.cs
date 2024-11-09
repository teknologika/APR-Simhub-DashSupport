using APR.DashSupport.Themes;
using SimHub.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace APR.DashSupport {
    public partial class APRDashPlugin : IPlugin, IDataPlugin, IWPFSettingsV2 {

        public string CalculateMultiClassIREstimation(ExtendedOpponent driver) {

            List<ExtendedOpponent> myOpponents = OpponentsInClass(driver.CarIdx);
           
            int myClassID = driver.CarClassID;
            int opponentsCount = this.opponents.Count;
            int classOpponentsCount = myOpponents.Count;
            int driverIRating = (int)driver._opponent.IRacing_IRating.GetValueOrDefault();
            int classPosition = driver.PositionInClass;
   
            // if no opponents return
            if (classOpponentsCount <= 1) { return ""; }

            double weight = 1600 / Math.Log(2);
            double fudge = (classOpponentsCount / 2.0 - classPosition) / 100.0;

            List<int> iRatings = new List<int>();
            foreach (var classOpponent in myOpponents) {
                if (classOpponent.iRating != 0) {
                    iRatings.Add(classOpponent.iRating);
                }
            }

            double tmpexpScore = 0;
    
            //FIXME - This breaks with a lot of classes
            try {
                for (int i = 0; i < classOpponentsCount; i++) {
                    tmpexpScore +=
                        (1 - Math.Exp(-driverIRating / weight)) *
                        Math.Exp(-iRatings[i] / weight) /
                        ((1 - Math.Exp(-iRatings[i] / weight)) * Math.Exp(-driverIRating / weight) +
                         (1 - Math.Exp(-driverIRating / weight)) * Math.Exp(-iRatings[i] / weight));
                }
            }
            catch { }


            double expScore = tmpexpScore - 0.5;
            int irChange = (int)Math.Round((classOpponentsCount - classPosition - expScore - fudge) * 200 / classOpponentsCount);

            return (irChange <= 0 ? "" : "+") + irChange;
        }

        private string LicenseColor(string licenseString) {
            if (licenseString.Contains("A")) // Blue
                return IRacing.Colors.BlueTransparent;
            if (licenseString.Contains("B")) // Green
                return IRacing.Colors.GreenTransparent;
            if (licenseString.Contains("C")) // Yellow
                return IRacing.Colors.YellowTransparent;
            if (licenseString.Contains("D")) // Orange
                return IRacing.Colors.OrangeTransparent;
            return !licenseString.Contains("R") ? "Black" : IRacing.Colors.RedTransparent; // red
        }

        private string LicenseTextColor(string licenseString) {
            if (licenseString.Contains("A")) // Blue
                return IRacing.Colors.BlueText;
            if (licenseString.Contains("B")) // Green
                return IRacing.Colors.GreenText;
            if (licenseString.Contains("C")) // Yellow
                return IRacing.Colors.YellowText;
            if (licenseString.Contains("D")) // Orange
                return IRacing.Colors.OrangeText;
            return !licenseString.Contains("R") ? "Black" : IRacing.Colors.RedText; // White
        }

        private string LicenseBorderColor(string licenseString) {
            if (licenseString.Contains("A")) // Blue
                return IRacing.Colors.Blue;
            if (licenseString.Contains("B")) // Green
                return IRacing.Colors.Green;
            if (licenseString.Contains("C")) // Yellow
                return IRacing.Colors.Yellow;
            if (licenseString.Contains("D")) // Orange
                return IRacing.Colors.Orange;
            return !licenseString.Contains("R") ? "Black" : IRacing.Colors.Red; // White
        }
    }

}
