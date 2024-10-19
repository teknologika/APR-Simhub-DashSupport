using GameReaderCommon;
using iRacingSDK;
using SimHub.Plugins;
using SimHub.Plugins.OutputPlugins.Dash.GLCDTemplating;
using SimHub.Plugins.OutputPlugins.GraphicalDash.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Markup;
using System.Windows.Media.Animation;

namespace APR.DashSupport {

    public partial class APRDashPlugin : IPlugin, IDataPlugin, IWPFSettingsV2 {

        private List<GameReaderCommon.Opponent> opponents;
        private List<ExtendedOpponent> OpponentsExtended = new List<ExtendedOpponent>();
        private List<carClass> carClasses = new List<carClass>();
        private float trackLength;

        public class carClass {
            public int carClassID;
            public string carClassShortName;
        }

        public void CheckAndAddCarClass(int CarClassID, string CarClassShortName) {
            bool has = this.carClasses.Any(a => a.carClassID == CarClassID);

            if (has == false) {

                this.carClasses.Add(new carClass() { carClassID = CarClassID, carClassShortName = CarClassShortName });
            }
        }

        private ExtendedOpponent SpectatedCar {
            get { return this.OpponentsExtended.Find(a => a.CarIdx == irData.Telemetry.CamCarIdx); }
        }

        private List<ExtendedOpponent> OpponentsInClasss {
            get {
                // TODO : add logic so if driving use that Id instead of spectated car
                return OpponentsExtended.FindAll(a => a.CarClassID == this.SpectatedCar.CarClassID);
            }
        }
        private List<ExtendedOpponent> OpponentsInClass() {
            return this.OpponentsInClass(this.SpectatedCar.CarClassID);
        }

        private List<ExtendedOpponent> OpponentsInClass(int CarClassID) {
            return this.OpponentsExtended.FindAll(a => a.CarClassID == this.SpectatedCar.CarClassID);
        }

        private class RelativePositions {

            public Dictionary<string, List<int>> PositionCarIdx { get; private set; } = new Dictionary<string, List<int>>();
            public List<int[]> TrackPosCarIdx { get; private set; } = new List<int[]>();

            public List<ExtendedOpponent> SortInWorldOpponentsByTrackPct(List<ExtendedOpponent> opponents) {

                // Ensure the car is in world
                List<ExtendedOpponent> opponentsInWorld = opponents.FindAll(a => a.IsInWorld).ToList();

                // Sort by position around track in descending order
                List<ExtendedOpponent> SortedOpponents = opponentsInWorld.OrderByDescending(a => a.TrackPositionPercent).ToList();

                return SortedOpponents;
            }



            private List<ExtendedOpponent> _relativePositions = new List<ExtendedOpponent>();
            private RelativeTable _relativeTable = new RelativeTable();

            public void  Clear() {
                _relativeTable.Clear();
                _relativePositions.Clear(); }

            public void Add(ExtendedOpponent item) {
                _relativePositions.Add(item);
            }

            public RelativeTable Get() {
                return _relativeTable;
            }

            private string TimeToStr_ms(double time, int precision) {
                // Convert time to a formatted string
                return time.ToString($"F{precision}");
            }

            private string FormatDriverNumber(int carIdx, bool pitRoad) {
                // Format driver number string based on carIdx and pitRoad status
                return pitRoad ? $"#{carIdx} (P)" : $"#{carIdx}";
            }

            private string DetermineColor(int i, int playerCarIdx, int lap, int playerLap, bool pitRoad) {
                if (i == playerCarIdx) {
                    return "#FFB923"; // Player car color
                }
                else if (lap > playerLap) {
                    return pitRoad ? "#7F1818" : "#FE3030"; // Lapping you
                }
                else if (lap == playerLap) {
                    return pitRoad ? "#7F7F7F" : "#FFFFFF"; // Same lap as you
                }
                else {
                    return pitRoad ? "#00607F" : "#00C0FF"; // Being lapped by you
                }
            }

            public RelativeTable Update(List<ExtendedOpponent> opponents, ExtendedOpponent spectator) {
                this.Clear();
                List<ExtendedOpponent> sortedOponents = SortInWorldOpponentsByTrackPct(opponents);

                int spectatorIdx = spectator.CarIdx;
                double spectatorTime = spectatorIdx >= 0 ? spectator.CarEstTime : 0;
                int spectatorLap = spectatorIdx >= 0 ? spectator.CurrentLap : 0;

                for (int i = 0; i < sortedOponents.Count; i++) {

                    int carIdx = sortedOponents[i].CarIdx;
                    int lap = sortedOponents[i].Lap;
                    bool pitRoad = sortedOponents[i].IsCarInPitLane;
                    int racePos = sortedOponents[i].Position;
                    double remoteTime = sortedOponents[i].CarEstTime;
                    string timeStr = TimeToStr_ms(remoteTime - spectatorTime,1);
                    string numStr = FormatDriverNumber(carIdx, pitRoad);
                    string nameStr = sortedOponents[i].DriverName;
                    string color = DetermineColor(i, spectatorIdx, lap, spectatorLap, pitRoad);

                    _relativeTable.Add(racePos, numStr, nameStr, timeStr, color); 
                }

                return _relativeTable;
            }

        }

        private void UpdateRelativeTable() {

        }


        private List<ExtendedOpponent> OpponentsBehind{
            get {
                var tmp = OpponentsExtended.FindAll(a => (a.LapDistPctSpectatedCar < 0  && a.IsConnected && a.IsInWorld)).OrderBy(a => a.LapDistPctSpectatedCar).ToList();
                return tmp;
            }
        }

        private List<ExtendedOpponent> OpponentsAhead {
            get {
                var tmp = OpponentsExtended.FindAll(a => (a.LapDistPctSpectatedCar > 0  && a.IsConnected && a.IsInWorld)).OrderBy(a => a.LapDistPctSpectatedCar).ToList();

                return tmp; 
            }
        }

        private double GetReferenceClassLaptime() {
            return GetReferenceClassLaptime(this.SpectatedCar.CarClassID);
        }

        private double GetReferenceClassLaptime(int CarClassID) {
            double averageLapTime = 0;
            int count = 0;
            foreach (var item in this.OpponentsInClass(CarClassID)) {

                // use the  last lap time
                if (item.LastLapTimeSeconds > 0 &&
                        (item.LastLapTimeSeconds < (item.LastLapTimeSeconds * 1.05)) &&
                        (item.LastLapTimeSeconds > (item.LastLapTimeSeconds * 0.95))) {
                    averageLapTime += item.LastLapTimeSeconds;
                    count++;
                }
                // if the last lap time is empty, try and use the best
                else if (item.BestLapTimeSeconds > 0 &&
                        (item.BestLapTimeSeconds < (item.BestLapTimeSeconds * 1.05)) &&
                        (item.BestLapTimeSeconds > (item.BestLapTimeSeconds * 0.95))) {
                    averageLapTime += item.BestLapTimeSeconds;
                    count++;

                }
            }
            if (count > 0) {
                averageLapTime = averageLapTime / count;
            }
            // if no time, just use 2 mins
            if (averageLapTime == 0) {
                return 120.0;
            }
            return averageLapTime;
        }

        private double ReferenceClassLaptime {
            get {
                return GetReferenceClassLaptime();
            }
        }

        private double GetGapAsTimeForClass(int CarClassID, double DistanceToTarget) {
            return (GetReferenceClassLaptime(CarClassID) / trackLength) * DistanceToTarget;

        }

        public double RelativeGapToSpectatedCar(int CarIdx) {
            return this.GetGapAsTimeForClass(this.SpectatedCar.CarClassID, this.OpponentsExtended[CarIdx].LapDistSpectatedCar);
        }


        private void UpdateRelatives(GameData data) {

            if (Settings.EnableRelatives) {


                this.opponents = data.NewData.Opponents;

                SessionData._DriverInfo._Drivers[] competitors = irData.SessionData.DriverInfo.CompetingDrivers;
                this.opponents = data.NewData.Opponents;
                this.OpponentsExtended = new List<ExtendedOpponent>();


                // Get the Spectated car info
                int spectatedCarIdx = irData.Telemetry.CamCarIdx;
                float spectatedCarLapDistPct = irData.Telemetry.CarIdxLapDistPct[spectatedCarIdx];
                int spectatedCarCurrentLap = irData.Telemetry.CarIdxLap[spectatedCarIdx];
                

                for (int i = 0; i < competitors.Length; ++i) {
                    for (int j = 0; j < opponents.Count; ++j) {
                        // Add the aligned Opponents and Competitor data to our ExtendedOpponent list
                        if (string.Equals(competitors[i].CarNumber, opponents[j].CarNumber)) {

                            // Add to the Extended Opponents class
                            OpponentsExtended.Add(new ExtendedOpponent() {
                                _sessionType = SessionType,
                                _opponent = opponents[j],
                                _competitor = competitors[i],
                                _carEstTime = irData.Telemetry.CarIdxEstTime[competitors[i].CarIdx],
                                _trackSurface = (int)irData.Telemetry.CarIdxTrackSurface[competitors[i].CarIdx],
                                _trackLength = trackLength,
                                _spectatedCarIdx = spectatedCarIdx,
                                _spectatedCarCurrentLap = spectatedCarCurrentLap,
                                _specatedCarLapDistPct = spectatedCarLapDistPct,
                                LicenseColor = LicenseColor(opponents[j].LicenceString)
                            });

                            // Update the car class info
                            CheckAndAddCarClass((int)competitors[i].CarClassID, competitors[i].CarClassShortName);

                        }
                    }
                }

                // Create the spectator
                ExtendedOpponent spectator = OpponentsExtended.Find(a => a.CarIdx == spectatedCarIdx);

                // update car reference lap time
                foreach (var item in OpponentsExtended) {
                    item.CarClassReferenceLapTime = GetReferenceClassLaptime(item.CarClassID);
                }

                if (IsRaceSession) {
                    // update iRating gain / loss
                    foreach (var item in OpponentsExtended) {
                        item.iRatingChange = CalculateMultiClassIREstimation(item);
                    }
                }

                
                RelativePositions relpos = new RelativePositions();
                relpos.Update(OpponentsExtended, spectator);



#if DEBUG
                var ben = relpos.Get();

                // this is for debugging only
                var bob = this.OpponentsInClass();
                var tim = this.GetReferenceClassLaptime();
                var fred = this.RelativeGapToSpectatedCar(0);
                var ahead = this.OpponentsAhead;
                var behind = this.OpponentsBehind;
#endif
                UpdateRelativeProperties();

            }
        }

        public string GetPositionforDriverAhead(int carAhead, List<ExtendedOpponent> opponentsAhead) {
            return "";
        }

        public string GetGapforDriverAhead(int index, List<ExtendedOpponent> opponentsAhead) {
            index = index - 1;
            if (index + 1 > opponentsAhead.Count || index < 0) {
                return "";
            }

            var carAhead = opponentsAhead[index];

            return RelativeGapToSpectatedCar(carAhead.CarIdx).ToString("0.00");

        }

        public void UpdateRelativeProperties() {

            if (Settings.EnableRelatives) {
                ClearRelativeProperties();

                int count = 1;
                foreach (var opponent in OpponentsAhead) {
                    
                    if (Settings.RelativeShowCarsInPits || (! Settings.RelativeShowCarsInPits && !opponent._opponent.IsCarInPitLane)){
                        SetProp("Relative.Ahead." + count + ".Position", opponent.PositionString);
                        SetProp("Relative.Ahead." + count + ".Name", opponent.DriverName);
                        SetProp("Relative.Ahead." + count + ".Show", opponent.DriverName != "");
                        SetProp("Relative.Ahead." + count + ".CarNumber", opponent.CarNumber);
                        SetProp("Relative.Ahead." + count + ".TrackPct", Math.Abs(opponent.LapDistPctSpectatedCar).ToString("0.0"));
                        SetProp("Relative.Ahead." + count + ".Distance", Math.Abs(opponent.LapDistSpectatedCar).ToString("0.0"));
                        SetProp("Relative.Ahead." + count + ".Gap", Math.Abs(opponent.GapSpectatedCar).ToString("0.0"));
                        SetProp("Relative.Ahead." + count + ".AheadBehind", opponent.AheadBehind.ToString());
                        SetProp("Relative.Ahead." + count + ".DriverNameColor", opponent.DriverNameColour);
                        SetProp("Relative.Ahead." + count + ".CarClassColor", opponent.CarClassColor);
                        SetProp("Relative.Ahead." + count + ".CarClassTextColor", opponent.CarClassTextColor);
                        SetProp("Relative.Ahead." + count + ".LicenseColor", opponent.LicenseColor);

                        SetProp("Relative.Ahead." + count + ".SR", opponent.SafetyRating);
                        SetProp("Relative.Ahead." + count + ".IR", opponent.iRatingString);
                        SetProp("Relative.Ahead." + count + ".SRSimple", opponent.SafetyRatingSimple);
                        SetProp("Relative.Ahead." + count + ".IRChange", opponent.iRatingChange);
                        SetProp("Relative.Ahead." + count + ".PitInfo", opponent.PitInfo);
                        count++;
                    }
                }

                count = 1;
                foreach (var opponent in OpponentsBehind) {
                    if (Settings.RelativeShowCarsInPits || (!Settings.RelativeShowCarsInPits && !opponent._opponent.IsCarInPitLane)) {
                        SetProp("Relative.Behind." + count + ".Position", opponent.PositionString);
                        SetProp("Relative.Behind." + count + ".Name", opponent.DriverName);
                        SetProp("Relative.Behind." + count + ".Show", opponent.DriverName != "");
                        SetProp("Relative.Behind." + count + ".CarNumber", opponent.CarNumber);
                        SetProp("Relative.Behind." + count + ".TrackPct", Math.Abs(opponent.LapDistPctSpectatedCar).ToString("0.0"));

                        SetProp("Relative.Behind." + count + ".Distance", Math.Abs(opponent.LapDistSpectatedCar).ToString("0.0"));
                        SetProp("Relative.Behind." + count + ".Gap", Math.Abs(opponent.GapSpectatedCar).ToString("0.0"));
                        SetProp("Relative.Behind." + count + ".AheadBehind", opponent.AheadBehind.ToString());

                        SetProp("Relative.Behind." + count + ".DriverNameColor", opponent.DriverNameColour);
                        SetProp("Relative.Behind." + count + ".CarClassColor", opponent.CarClassColor);
                        SetProp("Relative.Behind." + count + ".CarClassTextColor", opponent.CarClassTextColor);
                        SetProp("Relative.Behind." + count + ".LicenseColor", opponent.LicenseColor);

                        SetProp("Relative.Behind." + count + ".SR", opponent.SafetyRating);
                        SetProp("Relative.Behind." + count + ".SRSimple", opponent.SafetyRatingSimple);
                        SetProp("Relative.Behind." + count + ".IR", opponent.iRatingString);
                        SetProp("Relative.Behind." + count + ".IRChange", opponent.iRatingChange);
                        SetProp("Relative.Behind." + count + ".PitInfo", opponent.PitInfo);
                        count++;
                    }
                }

                SetProp("Relative.Spectated.Position", SpectatedCar.PositionString);
                SetProp("Relative.Spectated.Name", SpectatedCar.DriverName);
                SetProp("Relative.Behind.Spectated.Show", SpectatedCar.DriverName != "");
                SetProp("Relative.Spectated.CarNumber", SpectatedCar.CarNumber);
                SetProp("Relative.Spectated.PitInfo", SpectatedCar.PitInfo);
                SetProp("Relative.Spectated.Gap", 0.0);
                SetProp("Relative.Spectated.AheadBehind", "0");

                SetProp("Relative.Spectated.SR", SpectatedCar.SafetyRating);
                SetProp("Relative.Spectated.SRSimple", SpectatedCar.SafetyRatingSimple);
                SetProp("Relative.Spectated.IR", SpectatedCar.iRatingString);
                SetProp("Relative.Spectated.IRChange", SpectatedCar.iRatingChange);

                SetProp("Relative.Spectated.DriverNameColor", SpectatedCar.DriverNameColour);
                SetProp("Relative.Spectated.CarClassColor", SpectatedCar.CarClassColor);
                SetProp("Relative.Spectated.CarClassTextColor", SpectatedCar.CarClassTextColor);
                SetProp("Relative.Spectated.LicenseColor", SpectatedCar.LicenseColor);
            }
        }

        public void AddRelativeProperties() {
            if (Settings.EnableRelatives) {


                for (int i = 1; i < Settings.RelativeNumberOfCarsAheadToShow + 1; i++) {
                    AddProp("Relative.Ahead." + i + ".Position", "");
                    AddProp("Relative.Ahead." + i + ".Name", "");
                    AddProp("Relative.Ahead." + i + ".CarNumber", "");
                    AddProp("Relative.Ahead." + i + ".Distance", "");
                    AddProp("Relative.Ahead." + i + ".Gap", "");
                    AddProp("Relative.Ahead." + i + ".AheadBehind", "");
                    AddProp("Relative.Ahead." + i + ".CarClassTextColor", "");
                    AddProp("Relative.Ahead." + i + ".DriverNameColor", "");
                    AddProp("Relative.Ahead." + i + ".CarClassColor", "");
                    AddProp("Relative.Ahead." + i + ".LicenseColor", "");
                    AddProp("Relative.Ahead." + i + ".Show", "");
                    AddProp("Relative.Ahead." + i + ".SR", "");
                    AddProp("Relative.Ahead." + i + ".SRSimple", "");
                    AddProp("Relative.Ahead." + i + ".IR", "");
                    AddProp("Relative.Ahead." + i + ".IRChange", "");
                    AddProp("Relative.Ahead." + i + ".PitInfo", "");
                }

                for (int i = 1; i < Settings.RelativeNumberOfCarsBehindToShow + 1; i++) {
                    AddProp("Relative.Behind." + i + ".Position", "");
                    AddProp("Relative.Behind." + i + ".Name", "");
                    AddProp("Relative.Behind." + i + ".CarNumber", "");
                    AddProp("Relative.Behind." + i + ".Distance", "");
                    AddProp("Relative.Behind." + i + ".Gap", "");
                    AddProp("Relative.Behind." + i + ".AheadBehind", "");
                    AddProp("Relative.Behind." + i + ".CarClassTextColor", "");
                    AddProp("Relative.Behind." + i + ".DriverNameColor", "");
                    AddProp("Relative.Behind." + i + ".CarClassColor", "");
                    AddProp("Relative.Behind." + i + ".LicenseColor", "");
                    AddProp("Relative.Behind." + i + ".Show", "False");
                    AddProp("Relative.Behind." + i + ".SR", "");
                    AddProp("Relative.Behind." + i + ".SRSimple", "");
                    AddProp("Relative.Behind." + i + ".IR", "");
                    AddProp("Relative.Behind." + i + ".IRChange", "");
                    AddProp("Relative.Behind." + i + ".PitInfo", "");
                }

                AddProp("Relative.Spectated.Position", "");
                AddProp("Relative.Spectated.Name", "");
                AddProp("Relative.Spectated.CarNumber", "");
                AddProp("Relative.Spectated.PitInfo", "");
                AddProp("Relative.Spectated.Gap", "0.0");
                AddProp("Relative.Spectated.AheadBehind", "");
                AddProp("Relative.Spectated.CarClassTextColor", "");
                AddProp("Relative.Spectated.Distance", "0.0");
                AddProp("Relative.Spectated.DriverNameColor", "#FFFFFF");
                AddProp("Relative.Spectated.CarClassColor", "#000000");
                AddProp("Relative.Spectated.LicenseColor", "#FFFFFF");

                AddProp("Relative.Spectated.Position", "");
                AddProp("Relative.Spectated.Show", "False");
                AddProp("Relative.Spectated.SR", "");
                AddProp("Relative.Spectated.SRSimple", "");
                AddProp("Relative.Spectated.IR", "");
                AddProp("Relative.Spectated.IRChange", "");
                AddProp("Relative.Spectated.PitInfo", "");

                AddProp("Relative.Layout.FontSize", Settings.RelativeFontSize);
                AddProp("Relative.Layout.RowHeight", Settings.RelativeRowHeight);
                AddProp("Relative.Layout.RowOffset", Settings.RelativeRowOffset);

                AddProp("Relative.Layout.NumberOfCarsAhead", Settings.RelativeNumberOfCarsAheadToShow);
                AddProp("Relative.Layout.NumbersOfCarsBehind", Settings.RelativeNumberOfCarsBehindToShow);

                int totalRowHeight = (Settings.RelativeRowHeight + Settings.RelativeRowOffset);
                int headerHeight = 50;
                int headerTop = 0;
                int aheadTop = (totalRowHeight * Settings.RelativeNumberOfCarsAheadToShow);
                int spectatorTop = aheadTop + totalRowHeight;
                int behindTop = spectatorTop + totalRowHeight;
                int footerHeight = 50;
                int footerTop = behindTop + (totalRowHeight * Settings.RelativeNumberOfCarsAheadToShow);

                int windowHeight = footerTop + footerHeight;

                AddProp("Relative.Layout.HeaderTop", headerTop);
                AddProp("Relative.Layout.HeaderHeight", headerHeight);
                AddProp("Relative.Layout.AheadTop", aheadTop);
                AddProp("Relative.Layout.SpectatorTop", spectatorTop);
                AddProp("Relative.Layout.BehindTop", behindTop);
                AddProp("Relative.Layout.FooterTop", footerTop);
                AddProp("Relative.Layout.FooterHeight", footerHeight);
                AddProp("Relative.Layout.WindowHeight", windowHeight);

            }
        }

        public void ClearRelativeProperties() {

            if (Settings.EnableRelatives) {
                for (int i = 1; i < Settings.RelativeNumberOfCarsAheadToShow + 1; i++) {
                    SetProp("Relative.Ahead." + i + ".Position", "");
                    SetProp("Relative.Ahead." + i + ".Name", "");
                    SetProp("Relative.Ahead." + i + ".CarNumber", "");
                    SetProp("Relative.Ahead." + i + ".Distance", "");
                    SetProp("Relative.Ahead." + i + ".Gap", "");
                    SetProp("Relative.Ahead." + i + ".AheadBehind", "");
                    SetProp("Relative.Ahead." + i + ".CarClassTextColor", "");
                    SetProp("Relative.Ahead." + i + ".DriverNameColor", "");
                    SetProp("Relative.Ahead." + i + ".CarClassColor", "");
                    SetProp("Relative.Ahead." + i + ".LicenseColor", "");
                    SetProp("Relative.Ahead." + i + ".Show", false);
                    SetProp("Relative.Ahead." + i + ".SR", "");
                    SetProp("Relative.Ahead." + i + ".SRSimple", "");
                    SetProp("Relative.Ahead." + i + ".IR", "");
                    SetProp("Relative.Ahead." + i + ".IRChange", "");
                    SetProp("Relative.Ahead." + i + ".PitInfo", "");
                }

                for (int i = 1; i < Settings.RelativeNumberOfCarsBehindToShow + 1; i++) {
                    SetProp("Relative.Behind." + i + ".Position", "");
                    SetProp("Relative.Behind." + i + ".Name", "");
                    SetProp("Relative.Behind." + i + ".CarNumber", "");
                    SetProp("Relative.Behind." + i + ".Distance", "");
                    SetProp("Relative.Behind." + i + ".Gap", "");
                    SetProp("Relative.Behind." + i + ".AheadBehind", "");
                    SetProp("Relative.Behind." + i + ".CarClassTextColor", "");
                    SetProp("Relative.Behind." + i + ".DriverNameColor", "");
                    SetProp("Relative.Behind." + i + ".CarClassColor", "");
                    SetProp("Relative.Behind." + i + ".LicenseColor", "");
                    SetProp("Relative.Behind." + i + ".Show", false);
                    SetProp("Relative.Behind." + i + ".SR", "");
                    SetProp("Relative.Behind." + i + ".SRSimple", "");
                    SetProp("Relative.Behind." + i + ".IR", "");
                    SetProp("Relative.Behind." + i + ".IRChange", "");
                    SetProp("Relative.Behind." + i + ".PitInfo", "");
                }

                SetProp("Relative.Spectated.Position", "");
                SetProp("Relative.Spectated.Name", "");
                SetProp("Relative.Spectated.CarNumber", "");
                SetProp("Relative.Spectated.PitInfo", "");
                SetProp("Relative.Spectated.Gap", "0.0");
                SetProp("Relative.Spectated.AheadBehind", "");
                SetProp("Relative.Spectated.CarClassTextColor", "");
                SetProp("Relative.Spectated.Distance", "0.0");
                SetProp("Relative.Spectated.DriverNameColor", "#FFFFFF");
                SetProp("Relative.Spectated.CarClassColor", "#000000");
                SetProp("Relative.Spectated.LicenseColor", "#FFFFFF");

                SetProp("Relative.Spectated.Position", "");
                SetProp("Relative.Spectated.Show", false);
                SetProp("Relative.Spectated.SR", "");
                SetProp("Relative.Spectated.SRSimple", "");
                SetProp("Relative.Spectated.IR", "");
                SetProp("Relative.Spectated.IRChange", "");
                SetProp("Relative.Spectated.PitInfo", "");

                SetProp("Relative.Layout.NumberOfCarsAhead", Settings.RelativeNumberOfCarsAheadToShow);
                SetProp("Relative.Layout.NumbersOfCarsBehind", Settings.RelativeNumberOfCarsBehindToShow);
            }
        }

        public class ExtendedOpponent {
            public GameReaderCommon.Opponent _opponent;
            public string _sessionType;
            public SessionData._DriverInfo._Drivers _competitor;
            public float _trackLength;
            public int _trackSurface;
            public double _carEstTime;
            public double CarEstTime { get {return _carEstTime;} } 

            public bool IsOnTrack { get { return (_trackSurface == 3); } }
            public bool IsOffTrack { get { return (_trackSurface == 0); } }
            public bool IsInWorld { get { return (_trackSurface > -1); } }
            public bool IsSlow { get { return (IsOnTrack && (Speed > 30.0)); } }

            public double Speed { get { return _opponent.Speed.GetValueOrDefault(); } }
            
     
            public string TrackLocation {
                get {
                    
                    switch (_trackSurface) {
                        case 0:
                            return "Off Track";  
                            
                        case 1:
                            return "In Pit Stall";

                        case 2:
                            return "Approaching Pits";

                        case 3:
                            return "On Track";

                        default:
                            return "Not In World";
                    } 
                }
            }
            public long _spectatedCarIdx;
            public float _specatedCarLapDistPct;
            public int _spectatedCarCurrentLap;
            public int CarIdx { get { return Convert.ToInt32(_competitor.CarIdx); } }
            public string DriverName { get { return _opponent.Name; } }
            public string TeamName { get { return _opponent.TeamName; } }
            public string CarClass { get { return _opponent.CarClass; } }
            public int CarClassID { get { return (int)_competitor.CarClassID; } }
            public double CarClassReferenceLapTime { get; set; }
            public string CarClassColor {
                get {
                    return _competitor.CarClassColor.ToLower().Replace("0x", "#ff"); ;
                }
            }
            public string CarClassTextColor {
                get {
                    if (CarClassColor == "#ff000000") {
                        return "#ffffffff";
                    }
                    else {
                        return "#ff000000";
                    }
                }
            }
            public int Position { get { return _opponent.Position; } }
            public string PositionString {
                get {
                    if (_opponent.Position < 1) {
                        return "";
                    }
                    return _opponent.Position.ToString();
                }
            }

            public bool IsConnected { get {  return _opponent.IsConnected; } }
            public bool IsCarInPit { get { return _opponent.IsCarInPit; } }
            public bool IsCarInPitLane { get { return _opponent.IsCarInPitLane; } }
            public bool IsCarInGarage { get { return _opponent.IsCarInGarage.GetValueOrDefault(); } }
            public bool IsOutlap { get { return _opponent.IsOutLap; } }
            public bool IsPlayer { get { return _opponent.IsPlayer; } }
            public bool IsSpectator { get { return _spectatedCarIdx == _competitor.CarIdx; } }


            public string CarNumber { get { return _opponent.CarNumber; } }

            public int PositionInClass { get { return _opponent.PositionInClass; } }
            public string PositionInClassString {
                get {
                    if (_opponent.PositionInClass < 1) {
                        return "";
                    }
                    return _opponent.PositionInClass.ToString();
                }
            }
            public int CurrentLap { get { return _opponent.CurrentLap ?? -1; } }
            public int Lap { get { return  CurrentLap; } }
            public int LapsToLeader { get { return _opponent.LapsToLeader ?? -1; } }
            public double TrackPositionPercent { get { return _opponent.TrackPositionPercent ?? 0.0; } }
            public string TrackPositionPercentString { get { return TrackPositionPercent.ToString("0.000"); } }
            public double LapDist { get { return TrackPositionPercent * _trackLength; } }

            public int CappedLapToSpectatedCar(ExtendedOpponent opponent) {

                var lapDifference = Convert.ToInt32(_spectatedCarCurrentLap - opponent.CurrentLap);
                var cappedLapDifference = Math.Max(Math.Min(lapDifference, 1), -1);

                // -1 is behind, 0 same, +1 ahead  
                return cappedLapDifference;
            }

            public int AheadBehind {
                get {
                    return CappedLapToSpectatedCar(this);
                }
            }

            public string DriverNameColour {
                get {
                    if (IsCarInPit || IsCarInPitLane || IsCarInGarage || !IsConnected) {
                        return "#FF808080";
                    }

                    if (_sessionType == "Race") {

                        // driver is behind so LightSkyBlue
                        if (AheadBehind > 0) {
                            return "#FF87CEFA";
                        }
                        // driver is ahead so Salmon
                        else if (AheadBehind < 0) {
                            return "#FFFA8072";
                        }
                    }
                    return "#ffffffff";
                }
            }

            public string SafetyRating {
                get {
                    return _opponent.LicenceString.Remove(5, 1).Replace(" ", "");
                }
            }

            public string SafetyRatingSimple {
                get {
                    return _opponent.LicenceString.Substring(0, 1);
                }
            }

            public string LicenseColor { get; set; }

            public int iRating {
                get {
                    return (int)_competitor.IRating;
                }
            }

            public string iRatingString {
                get {
                    if (iRating < 1) {
                        return "";
                    }
                    return (iRating / 1000d).ToString("0.0") + "k";
                }
            }

            public string iRatingChange { get; set; }

            public double LapDistPctSpectatedCar {
                get {
                    Console.WriteLine(this.DriverName);
                    // Do we need to add or subtract a lap
                    var lapDifference = Convert.ToInt32(_spectatedCarCurrentLap - CurrentLap);
                    var cappedLapDifference = Math.Max(Math.Min(lapDifference, 1), -1);
                    double percentAdjustment = 0;

                    //if ((_opponent.TrackPositionPercent.Value - _specatedCarLapDistPct) < -0.50 ) {
                    //    percentAdjustment = 1;
                    // }

                    if (_specatedCarLapDistPct < 0 && _specatedCarLapDistPct > -50) {
                        return 1d - _specatedCarLapDistPct;
                    }

                    return _opponent.TrackPositionPercent.Value - _specatedCarLapDistPct;
                }
            }

            public double LapDistSpectatedCar {
                get {

                    // Do we need to add or subtract a lap
                    var lapDifference = Convert.ToInt32(_spectatedCarCurrentLap - CurrentLap);
                    var cappedLapDifference = Math.Max(Math.Min(lapDifference, 1), -1);
                    double distanceAdjustment = 0;

                    if (cappedLapDifference == 1) {
                        //distanceAdjustment = +_trackLength;
                    }
                    else if (cappedLapDifference == -1) {
                        //distanceAdjustment = -_trackLength;
                    }

                    if (_specatedCarLapDistPct < 0 && _specatedCarLapDistPct > -50) {
                        return 1d - _specatedCarLapDistPct;
                    }

                    return ((_specatedCarLapDistPct * _trackLength) - LapDist) + distanceAdjustment;
                }
            }

            public double GapSpectatedCar {
                get {
                    return CarClassReferenceLapTime / _trackLength * LapDistSpectatedCar;
                }
            }

            public int LapSpectatedCar {
                get {
                    return _spectatedCarCurrentLap;
                }
            }

            public TimeSpan LastLapTime { get { return _opponent.LastLapTime; } }
            public double LastLapTimeSeconds { get { return LastLapTime.TotalSeconds; } }
            public TimeSpan BestLapTime { get { return _opponent.BestLapTime; } }
            public double BestLapTimeSeconds { get { return BestLapTime.TotalSeconds; } }

            public TimeSpan? CurrentLapTime { get { return _opponent.CurrentLapTime; } }
            public double CurrentLapTimeSeonds { get { return CurrentLapTime.GetValueOrDefault().TotalSeconds; } }

            public string PitInfo {
                get {
                    if (_opponent.IsCarInPit) {
                        return "BOX";
                    }

                    if (_opponent.IsCarInPitLane) {
                        return "LANE";
                    }

                    if (_opponent.IsOutLap) {
                        return "OUT";
                    }

                    return "";
                }
            }

            public override string ToString() {
                return "Idx: " + CarIdx + " P:" + Position + " " + DriverName + " " + LapDistSpectatedCar.ToString("0.00") + " " + GapSpectatedCar.ToString("0.00");
            }

        }
    }
}
