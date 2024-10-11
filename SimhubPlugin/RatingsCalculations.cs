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
   

            double weight = 1600 / Math.Log(2);
            double fudge = (classOpponentsCount / 2.0 - classPosition) / 100.0;

            List<int> iRatings = new List<int>();
            foreach (var classOpponent in myOpponents) {
                if (classOpponent.iRating != 0) {
                    iRatings.Add(classOpponent.iRating);
                }
            }

            double tmpexpScore = 0;
            for (int i = 0; i < classOpponentsCount; i++) {
                tmpexpScore +=
                    (1 - Math.Exp(-driverIRating / weight)) *
                    Math.Exp(-iRatings[i] / weight) /
                    ((1 - Math.Exp(-iRatings[i] / weight)) * Math.Exp(-driverIRating / weight) +
                     (1 - Math.Exp(-driverIRating / weight)) * Math.Exp(-iRatings[i] / weight));
            }

            double expScore = tmpexpScore - 0.5;
            int irChange = (int)Math.Round((classOpponentsCount - classPosition - expScore - fudge) * 200 / classOpponentsCount);

            return (irChange <= 0 ? "" : "+") + irChange;
        }

        private string LicenseColor(string licenseString) {
            if (licenseString.Contains("A")) // Blue
                return "#FF0153db";
            if (licenseString.Contains("B")) // Green
                return "#FF00c702";
            if (licenseString.Contains("C")) // Yellow
                return "#FFfeec04";
            if (licenseString.Contains("D")) // Orange
                return "#FFFC8A27";
            return !licenseString.Contains("R") ? "Black" : "#FFB40800"; // red
        }

        private string LicenseTextColor(string licenseString) {
            if (licenseString.Contains("A")) // White
                return "#FFFFFFFF";
            if (licenseString.Contains("B")) // white
                return "#FFFFFFFF";
            if (licenseString.Contains("C")) // Black
                return "#FF000000";
            if (licenseString.Contains("D")) // Black
                return "#FF000000";
            return !licenseString.Contains("R") ? "Black" : "#FF000000"; // White
        }
    }

}
