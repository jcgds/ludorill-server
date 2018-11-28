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
        private List<Player> loggedClients;
        private List<TcpClient> disconnectedList;
        private MatchManager matchManager;
        private PlayerDao playerDao;

        public Server(PlayerDao playerDao)
        {
            this.playerDao = playerDao;
            unloggedClients = new List<TcpClient>();
            loggedClients = new List<Player>();
            disconnectedList = new List<TcpClient>();
            matchManager = new MatchManager();
            server = new TcpListener(IPAddress.Any, port);
            server.Start();
        }

        /**
         * Lee los datos disponibles enviados por un cliente y delega su 
         * procesamiento a la funcion OnIncomingData.
         * 
         * Normalmente se colocaria directo en el Listen() pero como tenemos
         * varias listas de usuarios conectados (logged y unlogged) entonces
         * podemos reutilizar este codigo.
         */
        private void ProcessClient(TcpClient sc)
        {
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

        public void Listen()
        {
            StartListening();
            Console.WriteLine("Escuchando puerto {0}" + Environment.NewLine, port);
            while (true)
            {
                for (int i = 0; i < loggedClients.Count; i++)
                {
                    TcpClient sc = loggedClients[i].socket;
                    ProcessClient(sc);
                }

                for (int i = 0; i < unloggedClients.Count; i++)
                {
                    TcpClient sc = unloggedClients[i];
                    ProcessClient(sc);
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
            server.BeginAcceptTcpClient(AcceptTcpClient, server);
        }

        private void AcceptTcpClient(IAsyncResult ar)
        {
            TcpListener listener = (TcpListener) ar.AsyncState;
            TcpClient client =listener.EndAcceptTcpClient(ar);
            unloggedClients.Add(client);
            
            Console.WriteLine("Conexion establecida con cliente. Clientes conectados: {0}", AmountOfClients());
            StartListening();
        }

        private int AmountOfClients()
        {
            return unloggedClients.Count + loggedClients.Count - disconnectedList.Count;
        }

        /*
         * Metodo para enviar un mensaje a una lista de clientes.
         */
        public void Broadcast(string data, List<TcpClient> clients)
        {
            foreach(TcpClient sc in clients)
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

        /*
         * Metodo para enviar un mensaje a un solo cliente.
         */
        public void Broadcast(string data, TcpClient client)
        {
            var clientTempList = new List<TcpClient>();
            clientTempList.Add(client);
            Broadcast(data, clientTempList);
        }

        /*
         * Encargado de procesar las instrucciones recibidas
         */
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
                        Console.WriteLine("Mensaje incompleto");
                        Broadcast("S|LOGIN|FAIL", source);
                    }
                    HandleLogin(source, split[2], split[3]);
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

                case "MATCH":
                    try
                    {
                        Player player = GetLoggedPlayerBy(source);
                        switch (split[2])
                        {
                            // C|MATCH|CREATE|:animalSelection --respuesta--> S|MATCH|CREATED|:id (|:nPlayers?)
                            case "CREATE":
                                try
                                {
                                    Animal selection = (Animal) Convert.ToInt16(split[3]);
                                    Console.WriteLine("Animal selection: " + selection);
                                    Match m = matchManager.CreateMatch(player, selection);
                                    Console.WriteLine("Successfully created match with id: " + m.id);
                                    Broadcast(string.Format("S|MATCH|CREATED|{0}", m.id), player.socket);
                                }
                                catch (PlayerAlreadyInGameException)
                                {
                                    Console.WriteLine("Player already in match");
                                    Broadcast("S|ERROR|ALREADY_IN_MATCH", source);
                                }
                                break;

                            // C|MATCH|JOIN|:id --respuesta--> S|MATCH|JOINED|:id (|:nPlayers?)
                            case "JOIN":
                                // Cada vez que un jugador se una, hay que avisar a todos los demas miembros de la partida
                                break;
                        }

                    } catch (ArgumentException)
                    {
                        Console.WriteLine("Non-logged user triying to join match");
                        Broadcast("S|ERROR|NEEDS_LOGIN", source);
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

        private void HandleLogin(TcpClient source, string username, string password)
        {
            foreach(Player logged in loggedClients)
            {
                if (logged.socket == source)
                {
                    Console.WriteLine("Client already logged in");
                    return;
                }
            }

            try
            {
                Player p = playerDao.GetPlayer(username);
                if (!p.TryPassword(password))
                {
                    Console.WriteLine("Intento de login fallido");
                    Broadcast("S|LOGIN|FAIL", source);
                }
                else
                {
                    p.socket = source;
                    Console.WriteLine("Login exitoso");
                    Broadcast("S|LOGIN|SUCCESS", source);
                    Console.WriteLine("Unlogged before rm: " + unloggedClients.Count);
                    unloggedClients.Remove(source);
                    Console.WriteLine("Unlogged after rm: " + unloggedClients.Count);

                    loggedClients.Add(p);
                    Console.WriteLine("logged: " + loggedClients.Count);
                }
            }
            catch
            {
                Broadcast("S|LOGIN|FAIL", source);
            }
        }

        private Player GetLoggedPlayerBy(TcpClient client)
        {
            foreach(Player p in loggedClients)
            {
                if (p.socket == client)
                    return p;
            }

            throw new ArgumentException("Client is not logged in.");
        }
    }
}
