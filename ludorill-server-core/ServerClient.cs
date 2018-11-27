using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;

namespace ludorill_server_core
{
    class ServerClient
    {
        public string username;
        public string password;
        public TcpClient tcp;

        public ServerClient(TcpClient tcp)
        {
            this.tcp = tcp;
        }
    }
}
