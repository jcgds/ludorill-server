using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using ludorill_server_core.Exceptions;

namespace ludorill_server_core
{
    class Match
    {
        public int id;
        public List<Player> players;
        public List<Animal> selectedAnimals;
        // private Player currentPlayer;

        public Match(int id)
        {
            this.id = id;
            players = new List<Player>();
            selectedAnimals = new List<Animal>();
        }

        public void Join(Player p, Animal selection)
        {
            if (players.Count == 4)
                throw new MatchIsFullException();

            if (IsAlreadySelected(selection))
                throw new AnimalAlreadySelectedException();

            players.Add(p);
            selectedAnimals.Add(selection);
        }

        public bool Has(Player player)
        {
            for (int i=0; i < players.Count; i++)
            {
                if (players[i].username == player.username && players[i].socket == player.socket)
                    return true;
            }

            return false;
        }

        public bool IsAlreadySelected(Animal animal)
        {
            for (int i = 0; i < selectedAnimals.Count; i++)
            {
                if (selectedAnimals[i] == animal)
                    return true;
            }

            return false;
        }
    }
}
