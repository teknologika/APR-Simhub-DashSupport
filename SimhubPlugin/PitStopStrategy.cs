using APR.DashSupport.Models;
using FMOD.Studio;
using GameReaderCommon;
using SimHub.Plugins;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing.Text;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static iRacingSDK.MemoryMappedViewAccessorExtensions;

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
            public double FourTyresPitstopTime { get { return StrategyBundle.Instance.FourTyresPitstopTime; } }

            public void CheckAndCreateDefaultStrategyIniFile() {
                var strategyFile = new IniFile(".\\PluginsData\\Common\\APRVetsFuelStrat.ini");
                if (!strategyFile.KeyExists("TrackName", "Season 21 Round 10")) {
                    strategyFile.Write("TrackName", "bathurst", "Season 21 Round 10");
                }

                if (!strategyFile.KeyExists("StratAStops", "Season 21 Round 10")) {
                    strategyFile.Write("StratAStops", "15,30", "Season 21 Round 10");
                }

                if (!strategyFile.KeyExists("StratAFuel", "Season 21 Round 10")) {
                    strategyFile.Write("StratAFuel", "70.5,70.5", "Season 21 Round 10");
                }
            }

            public void CalculateStrategy(Round rnd) {

                string roundNameKey = "Default";

                var strategyFile = new IniFile(".\\PluginsData\\Common\\APRVetsFuelStrat.ini");

                TotalLapsForStrategyCalc = Convert.ToInt16(strategyFile.Read("NumberOfLaps", roundNameKey));
                StrategyBundle.Instance.StratA_Stops = strategyFile.Read("StratAStops", roundNameKey);
                StrategyBundle.Instance.StratA_FuelToAdd = strategyFile.Read("StratAFuel", roundNameKey);
                StrategyBundle.Instance.StratA_FuelMode = strategyFile.Read("StratARefuelMode", roundNameKey);

                StrategyBundle.Instance.StratB_Stops = strategyFile.Read("StratBStops", roundNameKey);
                StrategyBundle.Instance.StratB_FuelToAdd = strategyFile.Read("StratBFuel", roundNameKey);
                StrategyBundle.Instance.StratB_FuelMode = strategyFile.Read("StratBRefuelMode", roundNameKey);

                StrategyBundle.Instance.StratC_Stops = strategyFile.Read("StratCStops", roundNameKey);
                StrategyBundle.Instance.StratC_FuelToAdd = strategyFile.Read("StratCFuel", roundNameKey);
                StrategyBundle.Instance.StratC_FuelMode = strategyFile.Read("StratCRefuelMode", roundNameKey);

                StrategyBundle.Instance.StratD_Stops = strategyFile.Read("StratDStops", roundNameKey);
                StrategyBundle.Instance.StratD_FuelToAdd = strategyFile.Read("StratDFuel", roundNameKey);
                StrategyBundle.Instance.StratD_FuelMode = strategyFile.Read("StratDRefuelMode", roundNameKey);

                string stop1Fuel, stop2Fuel, stop1Lap, stop2Lap, stop1Mode, stop2Mode; 
                switch (StrategyBundle.Instance.StratMode) {

                    case "A":
                    default:
                        stop1Fuel = StrategyBundle.Instance.StratA_FuelToAdd.Split(',')[0];
                        stop2Fuel = StrategyBundle.Instance.StratA_FuelToAdd.Split(',')[1];
                        stop1Lap = StrategyBundle.Instance.StratA_Stops.Split(',')[0];
                        stop2Lap = StrategyBundle.Instance.StratA_Stops.Split(',')[1];
                        stop1Mode = StrategyBundle.Instance.StratA_FuelMode.Split(',')[0];
                        stop2Mode = StrategyBundle.Instance.StratA_FuelMode.Split(',')[1];

                        break;

                    case "B":
                        stop1Fuel = StrategyBundle.Instance.StratB_FuelToAdd.Split(',')[0];
                        stop2Fuel = StrategyBundle.Instance.StratB_FuelToAdd.Split(',')[1];
                        stop1Lap = StrategyBundle.Instance.StratB_Stops.Split(',')[0];
                        stop2Lap = StrategyBundle.Instance.StratB_Stops.Split(',')[1];
                        stop1Mode = StrategyBundle.Instance.StratB_FuelMode.Split(',')[0];
                        stop2Mode = StrategyBundle.Instance.StratB_FuelMode.Split(',')[1];

                        break;

                    case "C":
                        stop1Fuel = StrategyBundle.Instance.StratC_FuelToAdd.Split(',')[0];
                        stop2Fuel = StrategyBundle.Instance.StratC_FuelToAdd.Split(',')[1];
                        stop1Lap = StrategyBundle.Instance.StratC_Stops.Split(',')[0];
                        stop2Lap = StrategyBundle.Instance.StratC_Stops.Split(',')[1];
                        stop1Mode = StrategyBundle.Instance.StratC_FuelMode.Split(',')[0];
                        stop2Mode = StrategyBundle.Instance.StratC_FuelMode.Split(',')[1];

                        break;

                    case "D":
                        stop1Fuel = StrategyBundle.Instance.StratD_FuelToAdd.Split(',')[0];
                        stop2Fuel = StrategyBundle.Instance.StratD_FuelToAdd.Split(',')[1];
                        stop1Lap = StrategyBundle.Instance.StratD_Stops.Split(',')[0];
                        stop2Lap = StrategyBundle.Instance.StratD_Stops.Split(',')[1];
                        stop1Mode = StrategyBundle.Instance.StratD_FuelMode.Split(',')[0];
                        stop2Mode = StrategyBundle.Instance.StratD_FuelMode.Split(',')[1];

                        break;

                }

                StrategyBundle.Instance.FirstStopFuel = stop1Fuel;
                StrategyBundle.Instance.FirstStopLap = stop1Lap;
                StrategyBundle.Instance.FirstStopMode = stop1Mode;

                StrategyBundle.Instance.SecondStopFuel = stop2Fuel;
                StrategyBundle.Instance.SecondStopLap = stop2Lap;
                StrategyBundle.Instance.SecondStopMode = stop2Mode;

            }

        }

    }
}

