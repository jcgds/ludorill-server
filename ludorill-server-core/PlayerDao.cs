using System;
using System.Collections.Generic;
using System.Text;

namespace ludorill_server_core
{
    interface PlayerDao
    {
        List<Player> GetAllPlayers();
        void SavePlayer(Player p);
        void DeletePlayer(Player p);
        Player GetPlayer(string username);

        // Falta el Update pero no lo vamos a implementar para este proyecto
    }
}
