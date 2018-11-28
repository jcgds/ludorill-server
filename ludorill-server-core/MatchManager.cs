using System;
using System.Collections.Generic;
using System.Text;

namespace ludorill_server_core
{
    class MatchManager
    {
        private List<Match> matches = new List<Match>();
        private int matchIdSequence = 0;

        public Match CreateMatch(Player creator, Animal selection)
        {
            if (IsAlreadyInAMatch(creator))
            {
                Console.WriteLine("Cant create new match, user is already in a game.");
                throw new PlayerAlreadyInGameException();
            } else
            {
                Match m = new Match(matchIdSequence++);
                m.Join(creator);
                matches.Add(m);
                return m;
            }
        }

        /*
         * Indica si un jugador ya pertenece a alguna partida.
         */
        private bool IsAlreadyInAMatch(Player p)
        {
            foreach(Match m in matches)
            {
                if (m.Has(p))
                    return true;
            }

            return false;
        }

    }
}
