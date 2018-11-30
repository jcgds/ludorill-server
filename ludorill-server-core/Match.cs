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
        private Dictionary<Player, Color> playersToColors;
        private Dictionary<Player, Animal> animalSelections;
        private Board board;
        private Player currentPlayer;
        private Color lastSelectedColor = Color.BLUE;

        public Match(int id)
        {
            this.id = id;
            playersToColors = new Dictionary<Player, Color>();
            animalSelections = new Dictionary<Player, Animal>();
            board = new Board();
        }

        public void Start()
        {
            // Validar que la partida este llena
            if (playersToColors.Count < 4)
            {
                Console.WriteLine("No se puede comenzar la partida, faltan jugadores");
                // TODO: Lanzar excepcion
                return;
            }
        }

        public void PlayTurn(Player p, int ficha, int diceRoll)
        {
            if (currentPlayer != p)
                throw new NotYourTurnException();

            // board.Move(playersToColors.TryGet(p), );
        }

        public void Join(Player p, Animal selection)
        {
            if (playersToColors.Count == 4)
                throw new MatchIsFullException();

            if (IsAlreadySelected(selection))
                throw new AnimalAlreadySelectedException();

            playersToColors.Add(p, lastSelectedColor++);
            animalSelections.Add(p, selection);
        }

        public bool Has(Player player)
        {
            return playersToColors.TryGetValue(player, out Color c);
        }

        public bool IsAlreadySelected(Animal animal)
        {
            foreach(KeyValuePair<Player, Animal> keyPair in animalSelections)
            {
                if (keyPair.Value == animal)
                    return true;
            }
 
            return false;
        }

        public List<Player> GetPlayers()
        {
            List<Player> res = new List<Player>();
            foreach (KeyValuePair<Player, Color> keyValue in playersToColors)
                res.Add(keyValue.Key);

            return res;
        }

        public Color GetPlayerColor(Player p)
        {
            bool found = playersToColors.TryGetValue(p, out Color c);
            if (found)
            {
                return c;
            } else
            {
                // TODO: Throw Exception?
                return Color.EMPTY;
            }           
        }
    }
}
