using GameReaderCommon;
using Newtonsoft.Json;
using SimHub.Plugins;
using SimHub.Plugins.OutputPlugins.Dash.GLCDTemplating;
using System;
using System.Collections.Generic;
using System.IO.Packaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Ink;

namespace APR.DashSupport {
    public partial class APRDashPlugin : IPlugin, IDataPlugin, IWPFSettingsV2 {

        // iRacing F3 implementaiton

        // Teams event information
        public bool isDriverChangeEvent = false;
        public string DriverChangeRuleSet;
        public bool SetDriverFairShareStatusVisibility = false;
        public string DriverFairShareStatusColor = "";



        public RelativeTable RelativePositionsTable = new RelativeTable();

        public class RelativeTable {
            private static List<RelativePosition> _relativePositions = new List<RelativePosition>();

            public void Clear() {
                _relativePositions.Clear();
            }

            public List<RelativePosition> Get() {
                return _relativePositions;
            }


            public void Add(int carIdx, int racePos, string carNumberString, string nameStr, double simpleRelativeGapToSpectator, int aheadBehind) {
               
                
                _relativePositions.Add(new RelativePosition() {
                    carIdx = carIdx,
                    racePosition = racePos,
                    carNumberString = carNumberString,
                    nameStr = nameStr,
                    simpleRelativeGapToSpectator = simpleRelativeGapToSpectator,
                    aheadBehind = aheadBehind
                });
            }
        }

        public class RelativePosition {
            public int carIdx;
            public int racePosition;
            public string carNumberString;
            public double sortingRelativeGapToSpectator;
            public double simpleRelativeGapToSpectator;
            public string simpleRelativeGapToSpectatorString {
                get {
                    return simpleRelativeGapToSpectator.ToString($"0.0");
                }
            }
              
            public string nameStr;
            public string relGapStr;
            public string color;
            public int aheadBehind; // 1 = ahead, 0 = ignore, -1 = behind
        }

        public void HandleF3Update(ref GameData data) {

            // Handle driver fair share
            if (isDriverChangeEvent && DriverChangeRuleSet != "None" && SessionType == "Race") {
                SetDriverFairShareStatusVisibility = true;
                int dcLapStatus = irData.Telemetry.DCLapStatus;

                // Apply color based on driver fair share
                switch (dcLapStatus) {
                    case 1:
                        DriverFairShareStatusColor = "yellow"; // MeetsRequirementsAsOfNow
                        break;
                    case 2:
                        DriverFairShareStatusColor = "green"; // MeetsEstimatedRequirementsForSession
                        break;
                    default:
                        DriverFairShareStatusColor = "red"; // DoesNotMeetRequirements
                        break;
                }
            }
            else {
                SetDriverFairShareStatusVisibility = false;
            }

            // Update relative position table
            UpdateRelativePositionTable();

            // Update current lap information
            UpdateLapOrTimeString(data);


        }

        private void UpdateRelativePositionTable() {
            RelativePositionsTable.Clear();

            int playerCarIdx = int.Parse(irData.SessionData.DriverInfo.DriverCarIdx.ToString());
            double playerTime = playerCarIdx >= 0 ? irData.Telemetry.CarIdxEstTime[playerCarIdx] : 0;
            int playerLap = playerCarIdx >= 0 ? irData.Telemetry.CarIdxLap[playerCarIdx] : -1;

            
           /*
            for (int i = 0; i < g_driverDataValues. TrackPosCarIdx.Count; i++) {
                int carIdx = g_driverDataValues.TrackPosCarIdx[i][0];
                int lap = g_driverDataValues.CarIdxLap[carIdx];
                bool pitRoad = g_driverDataValues.CarIdxOnPitRoad[carIdx];
                int racePos = int.Parse(g_driverDataValues.CarIdxClassPosition[carIdx].ToString());
                double remoteTime = g_driverDataValues.CarIdxEstTime[carIdx];
                string timeStr = TimeToStr_ms(remoteTime - playerTime, 1);

                string numStr = FormatDriverNumber(carIdx, pitRoad);
                string nameStr = GetDriverTeamName(carIdx);

                string color = DetermineColor(i, playerCarIdx, lap, playerLap, pitRoad);
                AddToRelativePositionTable($"{racePos} {numStr} {nameStr} {timeStr}", color);
              }
            */
            

        }




    }
}
