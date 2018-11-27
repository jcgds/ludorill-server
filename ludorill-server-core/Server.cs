using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ludorill_server_core
{
    class Server
    {
        public int port = 6969;
        private List<ServerClient> clients;
        private List<ServerClient> disconnectedList;
        private TcpListener server;
        private MatchManager matchManager;
        public IAsyncResult test;


        public Server()
        {
            clients = new List<ServerClient>();
            disconnectedList = new List<ServerClient>();
            server = new TcpListener(IPAddress.Any, port);
            server.Start();
        }

        public void StartListening()
        {
            test = server.BeginAcceptTcpClient(AcceptTcpClient, server);
        }

        private void AcceptTcpClient(IAsyncResult ar)
        {
            TcpListener listener = (TcpListener) ar.AsyncState;
            ServerClient client = new ServerClient(listener.EndAcceptTcpClient(ar));
            clients.Add(client);
            
            Console.WriteLine("Conexion establecida con cliente");
            StartListening();
        }

        public void Broadcast(List<TcpClient> clients)
        {
            throw new NotImplementedException();
        }

        public void OnIncomingData(TcpClient source, string data)
        {
            throw new NotImplementedException();
        }
    }
}
