using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.IO;

namespace ludorill_server_core
{
    class Player
    {
        public string username;
        public string password;
        public TcpClient socket;

        public Player(string username, string password)
        {
            this.username = username;
            this.password = password;
        }

        public bool TryPassword(string password)
        {
            return this.password == password;
        }
    }
}
