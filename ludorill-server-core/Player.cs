using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;


namespace ludorill_server_core
{
    class Player
    {
        private string username;
        public Animal animalType;
        private TcpClient tcp;
        private int[] movementsPerCharacter = { 0, 0, 0, 0 };

        public Player(string username, Animal animalType)
        {
            this.username = username;
            this.animalType = animalType;
        }
    }
}
