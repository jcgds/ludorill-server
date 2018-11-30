using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using ludorill_server_core.Exceptions;

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
        private IPlayerDao playerDao;

        public Server(IPlayerDao playerDao)
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

            if (split.Length < 1)
                return;

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
                            // C|MATCH|CREATE|:animalSelection --respuesta--> S|MATCH|CREATED|:id|:playerColor
                            case "CREATE":
                                try
                                {
                                    // TODO: Esta conversion puede dar error, manejarlo
                                    Animal selection = (Animal)Convert.ToInt16(split[3]);
                                    Console.WriteLine("Animal selection: " + selection);
                                    Match m = matchManager.CreateMatch(player, selection);
                                    Console.WriteLine("Successfully created match with id: " + m.id);
                                    // TODO: Tal vez se deberia avisar que se creo una partida a todos los clientes conectados
                                    string message = string.Format("S|MATCH|CREATED|{0}|{1}", m.id, m.GetPlayerColor(player));
                                    Console.WriteLine("Server sends: " + message);
                                    Broadcast(message, player.socket);
                                }
                                catch (PlayerAlreadyInGameException)
                                {
                                    Console.WriteLine("Player already in match");
                                    Broadcast("S|ERROR|ALREADY_IN_MATCH", source);
                                }
                                break;

                            // C|MATCH|JOIN|:id|:animalSelection --respuesta--> S|MATCH|JOINED|:matchId|:username|:playerColor|:nPlayers|
                            case "JOIN":
                                int matchId = Convert.ToInt16(split[3]);
                                Animal animal = (Animal)Convert.ToInt16(split[4]);
                                try
                                {
                                    Match m = matchManager.JoinMatch(matchId, player, animal);
                                    string message = string.Format("S|MATCH|JOINED|{0}|{1}|{2}|{3}", 
                                        m.id, player.username, m.GetPlayerColor(player), m.GetPlayers().Count);

                                    Console.WriteLine("Sent: " + message);
                                    // Cada vez que un jugador se una, hay que avisar a todos los demas miembros de la partida
                                    Broadcast(message, PlayerListToClientList(m.GetPlayers()));
                                }
                                catch (Exception e)
                                {
                                    if (e is AnimalAlreadySelectedException)
                                    {
                                        Console.WriteLine("Error: Animal already selected");
                                        Broadcast("S|ERROR|ANIMAL_ALREADY_SELECTED", player.socket);
                                    } else if (e is ArgumentException)
                                    {
                                        Console.WriteLine("Error: Invalid match id");
                                        Broadcast("S|ERROR|INVALID_MATCH_ID", player.socket);
                                    }
                                    else if (e is PlayerAlreadyInGameException)
                                    {
                                        Console.WriteLine("Error: Player already in a match");
                                        Broadcast("S|ERROR|ALREADY_IN_MATCH", player.socket);
                                    }
                                }
                                break;

                            // C|MATCH|PLAY|:matchId
                            case "PLAY":
                                matchId = Convert.ToInt16(split[3]);
                                // Ya tenemos el player
                                try
                                {
                                    Match match = matchManager.FindMatchBy(matchId);

                                } catch (Exception e)
                                {

                                }
                                // Si no es su turno, no hacer nada o lanzar excepcion
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
                // Si ya se tiene el mismo socket, significa que es el mismo cliente
                // Si tiene el mismo username significa que ya esta logeado en un cliente 
                // y esta tratando de hacer login desde otro cliente
                if (logged.socket == source || logged.username == username)
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

        private List<TcpClient> PlayerListToClientList(List<Player> players)
        {
            List<TcpClient> tcpClients = new List<TcpClient>();
            foreach(Player p in players)
            {
                tcpClients.Add(p.socket);
            }
            return tcpClients;
        }
    }
}
