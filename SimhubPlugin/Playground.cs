using SimHub.Plugins;
using IRacingReader;
using iRacingSDK;
using System;
using System.Globalization;
using System.Diagnostics;
using System.Collections;

namespace APR.DashSupport {
    public partial class APRDashPlugin : IPlugin, IDataPlugin, IWPFSettingsV2 {

        public void TestStuff() {

            foreach (var iRacingdata in iRacing
             .GetDataFeed()
             .WithCorrectedPercentages()) {
                Telemetry telemetry = iRacingdata.Telemetry;
                CarArray cars = new CarArray(telemetry);
                SessionData._DriverInfo._Drivers[] competingDrivers = telemetry.SessionData.DriverInfo.CompetingDrivers;
                var carstwo = new Car[competingDrivers.Length];
                for (int i = 0; i < competingDrivers.Length; i++) {
                    carstwo[i] = new Car(telemetry, i);
                    
                }
                
            }
        }

        public static void trythis() {

            var iracing = new iRacingConnection();
            foreach (var data in iracing.GetDataFeed() ){
                data.Telemetry.TryGetValue("CarIdxF2Time", out object CarIdxF2Time);
                IList list = CarIdxF2Time as IList;
                double[] gaps = new double[list.Count];
                for (int i = 0; i < gaps.Length; i++) {
                    gaps[i] = Convert.ToDouble(list[i]);
                    if (gaps[i] > 0) {
                        DebugMessage("Position: " + i + " Gap:" + gaps[i]);
                    }  
                }
            }

        }





        public static void Sample() {
            var iracing = new iRacingConnection();

            foreach (var data in iracing.GetDataFeed()
                .WithCorrectedPercentages()
                .WithCorrectedDistances()
                .WithPitStopCounts()) {

                var tele = data.Telemetry;
                float[] times = tele.CarIdxF2Time;
                for (int i = 0; i < times.Length; i++) {
                    if (times[i] > 0) {
                        Trace.WriteLine("Position - " + i + "Time: " + times[i]);
                    }

                }


                Trace.WriteLine(times.ToString());

                //Trace.WriteLine(data.SessionData.Raw);

                //System.Diagnostics.Debugger.Break();

            }
        }

    }

}
