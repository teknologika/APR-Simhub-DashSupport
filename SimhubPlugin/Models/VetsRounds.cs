using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace APR.DashSupport {
    public class VetsRounds {
        public List<Round> Rounds;

        public VetsRounds() {
            Rounds = new List<Round>();

            Rounds.Add(new Round(21, 8, "twinring east", 45, 150, 1, 0, 2.8));
            Rounds.Add(new Round(21, 9, "bathurst", 49, 300, 1, 0, 4));
            Rounds.Add(new Round(21, 10,"bathurst",49, 300, 1, 0, 4));

            Rounds.Add(new Round(22, 1, "watkins glen clasic boot", 47, 250, 2, 0,4.2));
            Rounds.Add(new Round(22, 2, "phillip island", 46, 200, 1, 0,2.8));
            Rounds.Add(new Round(22, 3, "brands hatch", 55, 200, 1, 0,3.2));
            Rounds.Add(new Round(22, 4, "magny-cours", 46, 200, 1, 0,3.2));
            Rounds.Add(new Round(22, 5, "nurb rgem24", 10, 200, 1, 0,11));
            Rounds.Add(new Round(22, 6, "belle isle", 40, 150, 3, 0,3.1));
            Rounds.Add(new Round(22, 7, "okayma full course", 54, 200, 1,0, 2.9));
            Rounds.Add(new Round(22, 8, "snetterton 300", 32, 150, 1, 0,3.8));
            Rounds.Add(new Round(22, 9, "red bull ring grand prix", 47, 200, 1, 0,3.5));
            Rounds.Add(new Round(22, 10,"sandown", 97, 300, 3, 0,2.5));
        }

        public Round GetRound(int season, int roundNumber) {
            return Rounds.Find(x => x.Season == season && x.RoundNumber == roundNumber) ?? new Round();
        }
    }
}
