using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ludorill_server_core
{
    class Match
    {
        public int id;
        private Player[] players;
        private int lastJoinedIndex = 0;
        // private Player currentPlayer;

        public Match(int id)
        {
            this.id = id;
            players = new Player[4];
        }

        public void Join(Player p)
        {
            players[lastJoinedIndex++] = p;
        }

        public bool Has(Player player)
        {
            for (int i=0; i < lastJoinedIndex; i++)
            {
                if (players[i].username == player.username && players[i].socket == player.socket)
                    return true;
            }

            return false;
        }
    }

    class MatchPlayer
    {
        Player player;
        Animal selectedAnimal;
        // Movimientos / posiciones de fichas
    }
}
