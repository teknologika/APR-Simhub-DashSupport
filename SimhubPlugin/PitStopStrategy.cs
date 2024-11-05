using SimHub.Plugins;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace APR.DashSupport {

    public partial class APRDashPlugin : IPlugin, IDataPlugin, IWPFSettingsV2 {

        public class PitStopStrategy {

            private double MaxTankSize { get { return StrategyBundle.Instance.MaxTankSize; } }
                    
            public double RestrictedFuelPercent { get { return StrategyBundle.Instance.RestrictedFuelPercent; } }
            public double StartingFuel { get { return StrategyBundle.Instance.StartingFuel; } }
            public double CoughAllowance { get { return StrategyBundle.Instance.CoughAllowance; } }

            public double TankSize { get { return StrategyBundle.Instance.TankSize; }  }
            public double AvailableSpaceInTankAtStart { get { return StrategyBundle.Instance.AvailableSpaceInTankAtStart; } }

            public int TotalLapsForStrategyCalc;
            public int CompulsoryStopsRemaining { get; set; } = 0;
            public double FuelPerLap { get; set; }
            public double FuelFillRateLitresPerSecond { get { return StrategyBundle.Instance.FuelFillRateLitresPerSecond; } }
            public double FourTyresPitstopTime { get { return StrategyBundle.Instance.FuelFillRateLitresPerSecond; } }

            public void CalculateStrategyA(Round rnd) {

                // Hard Coded for testing to be read from a season file matching track
                //totalLaps = 50;
                

                List<string> stopLap = new List<string>();
                List<string> stopLapPct = new List<string>();
                List<string> stopDuration = new List<string>();
                List<string> fuelToAdd = new List<string>();

                var strategy = new PitStopStrategy();

                strategy.TotalLapsForStrategyCalc = rnd.NumberOfLaps;
                strategy.CompulsoryStopsRemaining = rnd.MinStops;
                strategy.FuelPerLap = rnd.Gen2FuelBurn;

                var pitstops = strategy.CalculateEvenStops();
                foreach (var item in pitstops) {
                    stopLap.Add(item.Lap.ToString("0"));
                    var pct = (item.Lap / (double)strategy.TotalLapsForStrategyCalc) * 100;

                    stopLapPct.Add(pct.ToString("0"));
                    stopDuration.Add(item.StopDuration.ToString("0.0"));
                    fuelToAdd.Add(item.FuelToAdd.ToString("0.0"));

                }
                StrategyBundle.Instance.StratA_Stops = string.Join(",", stopLap);
                StrategyBundle.Instance.StratA_FuelToAdd = string.Join(",", stopDuration);
                StrategyBundle.Instance.StratA_StopDuration = string.Join(",", fuelToAdd);
                StrategyBundle.Instance.StratA_StopsPct = string.Join(",", stopLapPct);
            }


            public double RoundUpToPointFive(double d) {
                return Math.Ceiling(d * 2) / 2;
            }

            public double RoundDownToPointFive(double d) {
                return Math.Floor(d * 2) / 2;
            }

            public int RoundDownToInt(double d) {
                return (int)Math.Floor(d);
            }

            public enum Strategy {
                EvenStops,      // Strat A
                FillEarly,      // Strat B
                FillLate,       // Strat C
                CrashOnLapOne   // Strat D
            }

            public class PredictedPitStop {

                public StrategyBundle StrategyObserver = StrategyBundle.Instance;

                private double CoughAllowance;
                private double FuelPerLap;
                private int TotalLaps;
                public double FuelFillRateLitresPerSecond;
                public double FourTyresPitstopTime;


                public PredictedPitStop() {
                    StrategyObserver = StrategyBundle.Instance;
                    this.Update();
                }

                public void Update() {
                    CoughAllowance = StrategyObserver.CoughAllowance;
                    FuelPerLap = StrategyObserver.FuelLitersPerLap;
                    TotalLaps = StrategyObserver.TotaLaps;
                    FuelFillRateLitresPerSecond = StrategyObserver.FuelFillRateLitresPerSecond;
                    FourTyresPitstopTime = StrategyObserver.FuelFillRateLitresPerSecond;

                }

                public int Lap { get; set; }
                private double _FuelToAdd;
                public double FuelToAdd {
                    get { return Math.Round(_FuelToAdd, 1); }
                    set { _FuelToAdd = value; }
                }
                private bool _changeTyres;
                public bool ChangeTyres { get { return _changeTyres; } }
                public double StopDuration {
                    get {
                        double duration = FuelToAdd / this.FuelFillRateLitresPerSecond;
                        if (duration > FourTyresPitstopTime) {
                            _changeTyres = true;
                        }
                        else {
                            _changeTyres = false;
                        }
                        return duration;
                    }
                }
            }


            private bool OneMoreLap(double fuelInTank) {
                bool canGoOneMoreLap = false;
                if (fuelInTank >=  +  CoughAllowance) {
                    canGoOneMoreLap = true;
                }
                return canGoOneMoreLap;
            }

            private double SpaceLeftInTankAfterLapCompleted(double fuelInTank) {
                return Math.Round(fuelInTank -  FuelPerLap, 1);
            }

            private int CalculateMaximimRange(double fuelInTank) {
                double _fuelInTank = fuelInTank;
                int lapsUntilStop = 0;
                while (OneMoreLap(_fuelInTank)) {
                    lapsUntilStop++;
                    _fuelInTank = _fuelInTank - FuelPerLap;
                }
                return lapsUntilStop;
            }

            private double CalculateSpaceInTank(double fuelInTank) {
                return TankSize - fuelInTank;
            }

            private double CalculateSpaceInTankInNLaps(double fuelInTank, int numberOfLaps) {
                return CalculateSpaceInTank(fuelInTank - CalculateFuelNeededForLaps(numberOfLaps));
            }

            private double CalculateFuelNeededForLaps(int laps) {
                return laps * FuelPerLap;
            }

            private double CalculateEmptySpaceForLaps(int laps) {
                return TankSize - CalculateFuelNeededForLaps(laps);
            }

            // Calculate the number of laps given and amount of fuel
            private double CalculateLapsForAmountOfFuelPrecise(double fuelAmount) {
                double laps = (fuelAmount - CoughAllowance) / FuelPerLap;
                return laps;
            }

            private int CalculateLapsForAmountOfFuel(double fuelAmount) {
                return RoundDownToInt(CalculateLapsForAmountOfFuelPrecise(fuelAmount));
            }

            private double CalculateTotalFuelNeededForRace() {
                return RoundUpToPointFive(CalculateFuelNeededForLaps(TotalLapsForStrategyCalc));
            }

            private double CalculateTotalFuelNeededForRace(int numberOfRaceLaps) {
                return RoundUpToPointFive(CalculateFuelNeededForLaps(numberOfRaceLaps));
            }

            private double CalculateTotalFuelNeededToAddForRace() {
                return RoundUpToPointFive(CalculateFuelNeededForLaps(TotalLapsForStrategyCalc) - StartingFuel + CoughAllowance);
            }

            private double CalculateTotalFuelNeededToAddForRace(double fuelInTank, int numberOfRaceLaps) {
                return RoundUpToPointFive(CalculateFuelNeededForLaps(numberOfRaceLaps) - StartingFuel + CoughAllowance);
            }

            //TODO: Tests
            private double CalculateMinimumStopsPrecise(double fuelInTank, int numberOfRaceLaps) {
                double stopsRemaining = Math.Round(CalculateTotalFuelNeededToAddForRace(fuelInTank, numberOfRaceLaps) / TankSize, 1);
                if (stopsRemaining < CompulsoryStopsRemaining) {
                    return (double)CompulsoryStopsRemaining;
                }
                else {
                    return stopsRemaining;
                }
            }

            private double CalculateMinimumStopsPrecise() {
                return CalculateMinimumStopsPrecise(StartingFuel, TotalLapsForStrategyCalc);
            }

            private int CalculateMinimumStops() {
                return (int)Math.Ceiling(CalculateMinimumStopsPrecise(StartingFuel, TotalLapsForStrategyCalc));
            }

            private int CalculateMinimumStops(double fuelInTank, int laps) {
                return (int)Math.Ceiling(CalculateMinimumStopsPrecise(fuelInTank, laps));
            }

            private double CalculateMinimumFuelPerStop(double fuelInTank, int laps) {
                return RoundUpToPointFive(CalculateTotalFuelNeededToAddForRace(fuelInTank, laps) / CalculateMinimumStops(fuelInTank, laps));
            }

            private double CalculateEvenStopsFuel() {
                return CalculateTotalFuelNeededToAddForRace(StartingFuel, TotalLapsForStrategyCalc) / CalculateMinimumStops(StartingFuel, TotalLapsForStrategyCalc);
            }

            private double CalculateMinimumFuelPerStopAfterFirstPrecise() {
                PredictedPitStop firstStop = CalculateMaxFillFirstStop();
                double fuelInTank = StartingFuel - CoughAllowance - CalculateFuelNeededForLaps(firstStop.Lap) + firstStop.FuelToAdd;

                return CalculateTotalFuelNeededToAddForRace(fuelInTank, TotalLapsForStrategyCalc - firstStop.Lap) / CalculateMinimumStops(fuelInTank, TotalLapsForStrategyCalc - firstStop.Lap);
            }

            private PredictedPitStop CalculateMaxFillFirstStop() {
                int lapsOfRange = CalculateLapsForAmountOfFuel(StartingFuel);
                double FuelToAdd = Math.Round(CalculateSpaceInTankInNLaps(StartingFuel, lapsOfRange), 1);
                return new PredictedPitStop() { Lap = lapsOfRange, FuelToAdd = FuelToAdd };
            }

            private int CriticalLapFirstStop() {
                // =(F11-(MaxTankSize-MaxStartingFuel)+CoughAllowance)/F7
                return RoundDownToInt((CalculateEvenStopsFuel() - (TankSize - StartingFuel) + CoughAllowance) / FuelPerLap);
            }

            private List<PredictedPitStop> CalculateEvenStops() {
                var stops = new List<PredictedPitStop>();
                bool needOneLongerStint = false;
                double minStops = CalculateMinimumStopsPrecise();
                int numberOfStints = CalculateMinimumStops() + 1;
                double fuelPerStop = CalculateEvenStopsFuel();
                int stintLength = RoundDownToInt(TotalLapsForStrategyCalc / numberOfStints);

                // work out if we have an odd sequence so we can go one more lap on the second stop.
                if (TotalLapsForStrategyCalc % CalculateMinimumStops() != 0) {
                    needOneLongerStint = true;
                }
                for (int i = 1; i < minStops + 1; i++) {
                    PredictedPitStop stop = new PredictedPitStop() { Lap = stintLength * i, FuelToAdd = fuelPerStop };
                    stops.Add(stop);

                    // if it is the second stop, and we have an odd number of laps, go one lap longer.
                    if (i == 2 && needOneLongerStint) {
                        stops[1].Lap = stops[1].Lap + 1;
                    }
                }
                return stops;
            }

            private List<PredictedPitStop> CalculateMaxFillEarlyStops() {
                var stops = new List<PredictedPitStop>();
                double minStops = CalculateMinimumStopsPrecise();
                double totalFuelNeeded = CalculateTotalFuelNeededToAddForRace();
                double remainingFuelNeeded = totalFuelNeeded;
                double fuelInTank = StartingFuel - CoughAllowance;
                int lastStopOnLap = 0;
                int nextStintLength = 0;


                for (int i = 1; i < minStops + 1; i++) {
                    if (i == 1) {
                        PredictedPitStop firstStop = CalculateMaxFillFirstStop();
                        if (firstStop.FuelToAdd > remainingFuelNeeded) {
                            firstStop.FuelToAdd = RoundUpToPointFive(remainingFuelNeeded);
                        }
                        stops.Add(firstStop);

                        // update the amount of fuel we need
                        fuelInTank -= CalculateFuelNeededForLaps(firstStop.Lap);
                        remainingFuelNeeded -= (fuelInTank + firstStop.FuelToAdd);

                        // fill the tank and loop again
                        fuelInTank += firstStop.FuelToAdd;

                        // work out the next stint length
                        nextStintLength = CalculateMaximimRange(fuelInTank);
                        lastStopOnLap = firstStop.Lap;

                    }
                    else {
                        if (remainingFuelNeeded < CalculateFuelNeededForLaps(nextStintLength)) {
                            PredictedPitStop stop = new PredictedPitStop() { Lap = lastStopOnLap + nextStintLength, FuelToAdd = RoundUpToPointFive(remainingFuelNeeded) };
                            stops.Add(stop);

                            // update the amount of fuel we need
                            fuelInTank -= CalculateFuelNeededForLaps(nextStintLength);
                            remainingFuelNeeded -= (fuelInTank + stop.FuelToAdd);

                            // fill the tank and loop again
                            fuelInTank += stop.FuelToAdd;

                            // work out the next stint length
                            nextStintLength = CalculateMaximimRange(fuelInTank);
                            lastStopOnLap = lastStopOnLap + nextStintLength;
                        }
                        else {
                            PredictedPitStop stop = new PredictedPitStop() { Lap = lastStopOnLap + nextStintLength, FuelToAdd = RoundUpToPointFive(CalculateFuelNeededForLaps(nextStintLength)) };
                            stops.Add(stop);

                            // update the amount of fuel we need
                            fuelInTank -= CalculateFuelNeededForLaps(nextStintLength);
                            remainingFuelNeeded -= (fuelInTank + stop.FuelToAdd);

                            // fill the tank and loop again
                            fuelInTank += stop.FuelToAdd;

                            // work out the next stint length
                            nextStintLength = CalculateMaximimRange(fuelInTank);
                            lastStopOnLap = lastStopOnLap + nextStintLength;
                        }
                    }
                }
                return stops;
            }


            private List<PredictedPitStop> CalculateMaxFillLateStops() {

                var EarlyMaxStops = CalculateMaxFillEarlyStops();
                var stops = new List<PredictedPitStop>();
                double minStops = CalculateMinimumStopsPrecise();
                double totalFuelNeeded = CalculateTotalFuelNeededToAddForRace();
                double remainingFuelNeeded = totalFuelNeeded;
                double fuelInTank = StartingFuel - CoughAllowance;
                int lastStopOnLap = 0;
                int nextStintLength = 0;


                for (int i = 1; i < minStops + 1; i++) {
                    if (i == 1) {
                        PredictedPitStop firstStop = CalculateMaxFillFirstStop();

                        firstStop.FuelToAdd = EarlyMaxStops[EarlyMaxStops.Count - 1].FuelToAdd;

                        if (firstStop.FuelToAdd > remainingFuelNeeded) {
                            firstStop.FuelToAdd = RoundUpToPointFive(remainingFuelNeeded);
                        }
                        stops.Add(firstStop);

                        // update the amount of fuel we need
                        fuelInTank = fuelInTank - CalculateFuelNeededForLaps(firstStop.Lap);
                        remainingFuelNeeded = remainingFuelNeeded - fuelInTank - firstStop.FuelToAdd;

                        fuelInTank = fuelInTank + firstStop.FuelToAdd;
                        //double FuelToAdd = Math.Round(CalculateSpaceInTankInNLaps(StartingFuel,lapsOfRange),1);
                        // work out the next stint length
                        nextStintLength = CalculateMaximimRange(fuelInTank);
                        lastStopOnLap = firstStop.Lap;

                    }
                    else {
                        if (remainingFuelNeeded > CalculateFuelNeededForLaps(nextStintLength)) {
                            PredictedPitStop stop = new PredictedPitStop() { Lap = lastStopOnLap + nextStintLength, FuelToAdd = RoundUpToPointFive(remainingFuelNeeded) };
                            stops.Add(stop);
                            lastStopOnLap = lastStopOnLap + nextStintLength;


                            fuelInTank = fuelInTank - CalculateFuelNeededForLaps(nextStintLength) + stop.FuelToAdd;
                            remainingFuelNeeded = remainingFuelNeeded - fuelInTank - stop.FuelToAdd;
                        }
                    }
                }
                return stops;
            }
        }
    }
}

