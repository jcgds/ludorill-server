using System;
using System.Collections.Generic;
using System.IO;
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
        private TcpListener server;
        private List<TcpClient> unloggedClients;
        private List<TcpClient> disconnectedList;
        // private MatchManager matchManager;
        private PlayerDao playerDao;
        private bool listening = false;

        public Server(PlayerDao playerDao)
        {
            this.playerDao = playerDao;
            unloggedClients = new List<TcpClient>();
            disconnectedList = new List<TcpClient>();
            server = new TcpListener(IPAddress.Any, port);
            server.Start();
        }

        public void Listen()
        {
            StartListening();
            Console.WriteLine("Escuchando puerto {0}" + Environment.NewLine, port);
            while (listening)
            {
                for (int i = 0; i < unloggedClients.Count; i++)
                {
                    TcpClient sc = unloggedClients[i];
                    if (!IsConnected(sc))
                    {
                        sc.Close();
                        disconnectedList.Add(sc);
                    }
                    else
                    {
                        NetworkStream st = sc.GetStream();
                        if (st.DataAvailable)
                        {
                            StreamReader reader = new StreamReader(st, true);
                            string data = reader.ReadLine();
                            if (data != null)
                            {
                                OnIncomingData(sc, data);
                            }
                        }
                    }
                }

                for (int i = 0; i < disconnectedList.Count; i++)
                {
                    unloggedClients.Remove(disconnectedList[i]);
                    // Creo que esto no aplica para nuestro caso, tal vez permitir el reconnect
                    // disconnectedList.RemoveAt(i);

                    // TODO: Avisar a todos que alguien se desconecto
                }
            }           
        }

        public void StartListening()
        {
            if (listening)
                return;
       
            server.BeginAcceptTcpClient(AcceptTcpClient, server);
            listening = true;
        }

        private void AcceptTcpClient(IAsyncResult ar)
        {
            TcpListener listener = (TcpListener) ar.AsyncState;
            TcpClient client =listener.EndAcceptTcpClient(ar);
            unloggedClients.Add(client);
            
            Console.WriteLine("Conexion establecida con cliente. Clientes conectados: {0}", unloggedClients.Count - disconnectedList.Count);
            StartListening();
        }

        public void Broadcast(string data, List<TcpClient> unloggedClients)
        {
            foreach(TcpClient sc in unloggedClients)
            {
                try
                {
                    StreamWriter writer = new StreamWriter(sc.GetStream());
                    writer.WriteLine(data);
                    writer.Flush();
                } catch (Exception e)
                {
                    Console.WriteLine("Error sending message: " + e.Message);
                }
            }
        }

        public void Broadcast(string data, TcpClient client)
        {
            var clientTempList = new List<TcpClient>();
            clientTempList.Add(client);
            Broadcast(data, clientTempList);
        }

        public void OnIncomingData(TcpClient source, string data)
        {
            Console.WriteLine("Mensaje recibido: {0}", data);
            string[] split = data.Split('|');
            // TODO: Validar length de split

            switch(split[1])
            {
                case "LOGIN":
                    if (split.Length < 4)
                    {
                        Console.WriteLine("Intento de login fallido <");
                        Broadcast("S|LOGIN|FAIL", source);
                    }

                    if (!ValidCredentials(split[1], split[2]))
                    {
                        Console.WriteLine("Intento de login fallido");
                        Broadcast("S|LOGIN|FAIL", source);
                    } else
                    {
                        Console.WriteLine("Login exitoso");
                        Broadcast("S|LOGIN|SUCCESS", source);
                    }
                    break;

                case "REGISTER":
                    try
                    {
                        playerDao.SavePlayer(new Player(split[2], split[3]));
                        Broadcast("S|REGISTER|SUCCESS", source);
                        Console.WriteLine("Successfully registered..");
                    }
                    catch (UsernameAlreadyUsedException)
                    {
                        Broadcast("S|REGISTER|FAIL", source);
                        Console.WriteLine("Failed register");
                    }              
                    break;
            }
        }

        private bool IsConnected(TcpClient client)
        {
            try
            {
                if (client != null && client.Client != null && client.Client.Connected)
                {
                    if (client.Client.Poll(0, SelectMode.SelectRead))
                    {
                        int check = client.Client.Receive(new byte[1], SocketFlags.Peek);
                        return !(check == 0);
                    }
                    return true;
                }
                else
                    return false;
            } catch
            {
                return false;
            }
        }

        private bool ValidCredentials(string username, string password) {
            try
            {
                Player p = playerDao.GetPlayer(username);
                return p.TryPassword(password);
            }
            catch
            {
                return false;
            }
        }
    }
}
