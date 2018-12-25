using System;
using System.Collections.Generic;
using System.Text;
using ludorill_server_core.Exceptions;

namespace ludorill_server_core
{
    class MatchManager
    {
        private List<Match> matches = new List<Match>();
        private int matchIdSequence = 0;

        public Match CreateMatch(Player creator, Animal selection)
        {
            if (IsAlreadyInAMatch(creator))
                throw new PlayerAlreadyInGameException();
            
            if (selection >= Animal.AMOUNT_OF_CHOICES || selection < 0) 
                throw new InvalidAnimalSelectionException();

            Match m = new Match(matchIdSequence++);
            m.Join(creator, selection);
            matches.Add(m);
            return m;
        }

        public Match JoinMatch(int matchId, Player p, Animal selection)
        {
            if (IsAlreadyInAMatch(p))
                throw new PlayerAlreadyInGameException();
            
            if (selection >= Animal.AMOUNT_OF_CHOICES || selection < 0) 
                throw new InvalidAnimalSelectionException();
            
            // Esta llamada puede causar un ArgumentException si no consigue la partida
            Match m = FindMatchBy(matchId);
            // Esta llamada puede causar un AnimalAlreadySelectedException
            m.Join(p, selection);
            return m;
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

        public Match FindMatchBy(int matchId)
        {
            foreach (Match m in matches)
            {
                if (m.id == matchId)
                    return m;
            }

            throw new ArgumentException("Invalid match id");
        }

        public Match FindMatchBy(Player p)
        {
            foreach (Match m in matches)
            {
                if (m.Has(p))
                    return m;
            }

            throw new ArgumentException("Player is not in a match");
        }
    }
}
