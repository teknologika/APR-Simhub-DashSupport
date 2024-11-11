using FMOD;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace APR.DashSupport {

    public enum StrategyType {
        EvenStops,      // Strat A
        FillEarly,      // Strat B
        FillLate,       // Strat C
        EarlyStopRecovery   // Strat D
    }

}
