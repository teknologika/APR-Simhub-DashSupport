using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace APR.DashSupport {
    public class Round {
        public int Season;
        public int RoundNumber;
        public string TrackID;
        public int NumberOfLaps;
        public int RaceLength;
        public int MinStops = 2;

        public double StartingFuel {
            get {
                if (RaceLength == 150) return 50.0;
                else return 80.0;
            }
        }
        public int ChanceOfSC; // expected 0 none 1 low 2 med - 3 high (3 or more)
        public double PitLaneTransitTime;
        public double Gen2FuelBurn;


        public Round() { }

        public Round(
            int season,
            int roundNumber,
            string trackID,
            int numberOfLaps,
            int raceLength,
            int chanceOfSC,
            double pitLaneTransitTime,
            double gen2FuelBurn) {

            Season = season;
            RoundNumber = roundNumber;
            TrackID = trackID;
            NumberOfLaps = numberOfLaps;
            RaceLength = raceLength;
            ChanceOfSC = chanceOfSC;
            PitLaneTransitTime = pitLaneTransitTime;
            Gen2FuelBurn = gen2FuelBurn;
        }
    }
}
