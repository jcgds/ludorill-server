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
        private Player[] players;
        private Player currentPlayer;

        public bool IsHosting(TcpClient client)
        {
            throw new NotImplementedException();
        }
    }

    class MatchPlayer
    {
        Player player;
        Animal selectedAnimal;
        // Movimientos / posiciones de fichas
    }
}
