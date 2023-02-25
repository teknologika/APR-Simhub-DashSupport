using SimHub.Plugins.OutputPlugins.GraphicalDash.Behaviors.DoubleText;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace APR.DashSupport {
    internal class Standings {
        public int CurrentlyObservedDriver { get; set; } = 0;

        public string BattleBoxDisplayString { get; set; } = string.Empty;
        public double BattleBoxGap { get; set; }
        public int BattleBoxDriver1Position { get; set; } = 0;
        public int BattleBoxDriver2Position { get; set; } = 0;
        public string BattleBoxDriver1Name { get; set; } = string.Empty;
        public string BattleBoxDriver2Name { get; set; } = string.Empty;
        public int EstimatedOvertakeLaps { get; set; } = 0;
        public double EstimatedOvertakePercentage { get; set; } = 0.0;

    }

    internal class Championship {
        // Leadercar
        // Leader Photo
        // Leader team
        // Championship name
        // Next event
        // Current round number
        // Championship Sponsor

        // Championship Standings
    }

    internal class CarClass {
        public int CarClassID { get; set; } = 0;
        public string CarClassName { get; set; } = string.Empty;
        public string CarClassColour { get; set; } = string.Empty;
        public string CarClassDisplayName { get; set; } = string.Empty;
    }

    internal class RaceCar {

        public string CarClass { get; set; } = string.Empty;
        public Driver Driver { get; set; } = null;
        public Team Team { get; set; } = null;

        // Lap timing info
        public double BestLap { get; set; } = 0;
        public double BestLapSector1 { get; set; } = 0;
        public double BestLapSector2 { get; set; } = 0;
        public double BestLapSector3 { get; set; } = 0;
        public double CurrentSector1Time { get; set; } = 0;
        public double CurrentSector2Time { get; set; } = 0;
        public double CurrentSector3Time { get; set; } = 0;
        public int CurrentSectorNumber { get; set; } = 0;
        public int EstimatedLapTime { get; set; } = 0;
        public double IntervalGap { get; set; } = 0;
        public double IntervalGapDelayed { get; set; } = 0;
        public double LapDistancePercent { get; set; } = 0;
        public int LapsBehindLeader { get; set; } = 0;
        public int LapsBehindNext { get; set; } = 0;
        public int Position { get; set; } = 0;
        public int PositionsGainedLost { get; set; } = 0;
        public int SpeedCurrent { get; set; } = 0;
        public int SpeedMax { get; set; } = 0;
        public double TimeBehindLeader { get; set; } = 0;
        public double TimeBehindNext { get; set; } = 0;
        public int TotalLaps { get; set; } = 0;
        public int LapsDown { get; set; } = 0;


        // Pit info
        public bool PitInPitBox { get; set; } = false;
        public int PitInPitLane { get; set; } = 0;
        public int PitLastLapPitted { get; set; } = 0;
        public int PitLastStopDuration { get; set; } = 0;

        public int PitCount { get; set; } = 0;


        // Flags
        public bool HasFinished { get; set; } = false;
        public bool HasRetired { get; set; } = false;
        public bool HasBlueFlag { get; set; } = false;
        public bool HasOfftrack { get; set; } = false;
        public int IncidentCount { get; set; } = 0;
    }

    // Holds driver information
    internal class Driver {
        public int DriverID { get; set; } = 0;
        public string DriverFullName { get; set; } = string.Empty;
        public string DriverFirstName { get; set; } = string.Empty;
        public string DriverFirstNameInitial { get; set; } = string.Empty;
        public string DriverLastName { get; set; } = string.Empty;
        public string DriverLastNameInitial { get; set; } = string.Empty;
        public string DriverLastNameShort { get; set; } = string.Empty;
        public int DriverIRating { get; set; } = 0;
        public int DriverSafetyRating { get; set; } = 0;
        public string Nationality { get; set; } = string.Empty;

    }

    // Used for teams races
    internal class Team {}

}
