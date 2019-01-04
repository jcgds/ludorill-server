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
                    TcpClient dc = disconnectedList[i];
                    disconnectedList.RemoveAt(i);
     
                    Player player = FindLoggedPlayerBy(dc);
                    if (player != null)
                    {
                        if (matchManager.IsAlreadyInAMatch(player))
                        {
                            // Usuario loggeado en partida se desconecta ¿Que deberiamos hacer?
                            // TODO: Manejar este caso
                            // Aqui se podria avisar a los miembros de la partida que alguien se desconecto
                            Console.WriteLine("{0} se desconecto pero esta en partida. FALTA IMPLEMENTAR ESTE CASO", player.username);
                        }
                        else
                        {
                            // El cliente estaba loggeado pero no estaba en partida
                            // Se borra de la lista de clientes loggeados y se cierra el socket
                            loggedClients.Remove(player);
                            dc.Close();
                            Console.WriteLine("{0} se desconecto. Clientes conectados: {1}", player.username, AmountOfClients());
                        }
                    } else
                    {
                        unloggedClients.Remove(dc);
                        dc.Close();
                        Console.WriteLine("Cliente sin sesion iniciada desconectado. Clientes conectados: {0}", AmountOfClients());
                    }
                    
                }
            }
        }

        public void StartListening()
        {
            server.BeginAcceptTcpClient(AcceptTcpClient, server);
        }

        private void AcceptTcpClient(IAsyncResult ar)
        {
            TcpListener listener = (TcpListener)ar.AsyncState;
            TcpClient client = listener.EndAcceptTcpClient(ar);
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
            foreach (TcpClient sc in clients)
            {
                try
                {
                    StreamWriter writer = new StreamWriter(sc.GetStream());
                    writer.WriteLine(data);
                    writer.Flush();
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error sending message: " + e.Message);
                }
            }
        }

        public void Broadcast(string data, List<Player> players)
        {
            var clients = PlayerListToClientList(players);
            foreach (TcpClient sc in clients)
            {
                try
                {
                    StreamWriter writer = new StreamWriter(sc.GetStream());
                    writer.WriteLine(data);
                    writer.Flush();
                }
                catch (Exception e)
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

            try
            {
                switch (split[1])
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
                                case "CREATE":
                                    try
                                    {
                                        Match m = matchManager.CreateMatch(player, split[3]);
                                        Console.WriteLine("Successfully created match with id: " + m.id);
                                        string message = string.Format("S|MATCH|CREATED|{0}|{1}|{2}|{3}|{4}|{5}", m.id, player.username, (int)m.GetPlayerColor(player), m.name, (int)m.GetPlayerAnimal(player), (int)m.GetCurrentPlayerColor());
                                        Console.WriteLine("Server sends: " + message);
                                        Broadcast(message, loggedClients);
                                    }
                                    catch (Exception e)
                                    {
                                        if (e is PlayerAlreadyInGameException)
                                        {
                                            Console.WriteLine("Player already in match");
                                            Broadcast("S|ERROR|ALREADY_IN_MATCH", source);
                                        }
                                        else
                                        {
                                            // Cualquier otra excepcion recibida, de manera que nunca crashee el servidor al crear una partida
                                            Console.WriteLine("Unhandled error: " + e.Message);
                                            Broadcast("S|ERROR|UNKNOWN_ERROR", source);
                                        }
                                    }
                                    break;

                                case "JOIN":
                                    int matchId = Convert.ToInt16(split[3]);
                                    try
                                    {
                                        Match m = matchManager.JoinMatch(matchId, player);
                                        int playerCount = m.GetPlayers().Count;
                                        string message = string.Format("S|MATCH|JOINED|{0}|{1}|{2}|{3}|{4}|{5}|{6}",
                                            m.id, player.username, 
                                            (int)m.GetPlayerColor(player), 
                                            playerCount, 
                                            (int)m.GetPlayerAnimal(player),
                                            (int)m.GetCurrentPlayerColor(),
                                            m.FormattedListOfPlayers()
                                        );

                                        Console.WriteLine("Sent: " + message);
                                        //Broadcast(message, m.GetPlayers());
                                        Broadcast(message, loggedClients);
                                    }
                                    catch (Exception e)
                                    {
                                        if (e is ArgumentException)
                                        {
                                            Console.WriteLine("Error: Invalid match id");
                                            Broadcast("S|ERROR|INVALID_MATCH_ID", player.socket);
                                        }
                                        else if (e is PlayerAlreadyInGameException)
                                        {
                                            Console.WriteLine("Error: Player already in a match");
                                            Broadcast("S|ERROR|ALREADY_IN_MATCH", player.socket);
                                        } else if (e is MatchIsFullException)
                                        {
                                            Console.WriteLine("Error: Match is already full");
                                            Broadcast("S|ERROR|MATCH_IS_FULL", player.socket);
                                        }
                                        else
                                        {
                                            Console.WriteLine("Dafuck is this: " + e);
                                        }
                                    }
                                    break;

                                // C|MATCH|PLAY|{ROLL | SELECT_PIECE}
                                case "PLAY":
                                    try
                                    {
                                        Match match = matchManager.FindMatchBy(player);

                                        switch (split[3])
                                        {
                                            // C|MATCH|PLAY|ROLL --respuesta--> S|MATCH|PLAY|ROLLED|:matchId|:usernameJugador|:playerColor|:diceRoll|:indexDeFichasMovibles
                                            case "ROLL":
                                                // Generar numero random y mandarselo a todos los players de la partida.
                                                // En el Match, se genera el numero y se mantiene, pero no se mueve el currentPlayer
                                                // pues hay que esperar a que este le indique que ficha desea mover (con el mensaje
                                                // SELECT_PIECE)
                                                int rolled = match.RollDice(player);
                                                List<int> fichasMovibles = match.MovablePieces(player, rolled);
                                                string message = string.Format("S|MATCH|PLAY|ROLLED|{0}|{1}|{2}|{3}|{4}|{5}",
                                                    match.id, player.username, (int)match.GetPlayerColor(player), rolled, 
                                                    fichasMovibles.Count == 0 ? "NONE" : string.Join(",", fichasMovibles),
                                                    (int)match.GetCurrentPlayerColor()
                                                );
                                                Console.WriteLine("Sent: " + message);
                                                Broadcast(message, match.GetPlayers());
                                                break;

                                            case "SELECT_PIECE":
                                                // El servidor deberia hacer un Broadcast a la partida indicando el color, ficha y numero
                                                // de movimientos ejecutados.
                                                int pieceIndex = Convert.ToInt16(split[4]);                                              

                                                try
                                                {
                                                    int movimientosEjecutados = match.PlayTurn(player, pieceIndex);
                                                    
                                                    // Tratamos de conseguir la pieza que tenga colision
                                                    Queue<int[]> collision = match.GetPieceThatHasToReturn();
                                                    string collisionMsg = "NONE";
                                                    if (collision.TryDequeue(out int[] pieceToReturn))
                                                    {
                                                        collisionMsg = string.Join(',', pieceToReturn);
                                                    }

                                                    message = string.Format("S|MATCH|PLAY|MOVE|{0}|{1}|{2}|{3}|{4}|{5}",
                                                        (int)match.GetPlayerColor(player), 
                                                        pieceIndex, 
                                                        movimientosEjecutados, 
                                                        match.IsInColorRoad(player, pieceIndex),
                                                        (int)match.GetCurrentPlayerColor(),
                                                        collisionMsg
                                                    );
                                                    Console.WriteLine("Sent: " + message);                                                   
                                                    Broadcast(message, match.GetPlayers());
                                                    Player winner = match.HasWinner();
                                                    // Si hay un ganador, avisamos a los miembros de la partida y cerramos la partida
                                                    if (winner != null)
                                                    {
                                                        Console.WriteLine("La partida tiene un ganador: " + winner.username);
                                                        string winnerNotice = string.Format("S|MATCH|WINNER|{0}|{1}|{2}", match.id, winner.username, (int)match.GetPlayerColor(winner));
                                                        Broadcast(winnerNotice, match.GetPlayers());
                                                        Console.WriteLine("Sent: " + winnerNotice);
                                                        CloseMatch(match.id);
                                                    }
                                                }
                                                catch (PieceCantBeMovedException)
                                                {
                                                    Console.WriteLine("Pieza no puede ser movida");
                                                    Broadcast("S|ERROR|UNMOVABLE_PIECE", source);
                                                }
                                                break;
                                        }
                                    }
                                    catch (Exception e)
                                    {
                                        if (e is ArgumentException)
                                        {
                                            Console.WriteLine("Player not in a match");
                                            Broadcast("S|ERROR|NOT_IN_MATCH", source);
                                        }
                                        else if (e is NotYourTurnException)
                                        {
                                            Console.WriteLine("No es el turno del jugador");
                                            Broadcast("S|ERROR|NOT_YOUR_TURN", source);
                                        }
                                        else if (e is MatchNotFullException)
                                        {
                                            Console.WriteLine("No se puede jugar la partida, faltan jugadores");
                                            Broadcast("S|ERROR|MATCH_NOT_FULL", source);
                                        }
                                    }
                                    break;
                            }

                        }
                        catch (ArgumentException)
                        {
                            Console.WriteLine("Non-logged user triying to play match");
                            Broadcast("S|ERROR|NEEDS_LOGIN", source);
                        }
                        break;
                }
            }
            catch (IndexOutOfRangeException)
            {
                Console.WriteLine("Instruccion recibida invalida: " + data);
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
            }
            catch
            {
                return false;
            }
        }

        private void HandleLogin(TcpClient source, string username, string password)
        {
            foreach (Player logged in loggedClients)
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
                    string successMessage = string.Format("S|LOGIN|SUCCESS|{0}", matchManager.GetAvailableMatches());
                    Console.WriteLine(successMessage);
                    Broadcast(successMessage, source);
                    unloggedClients.Remove(source);
                    loggedClients.Add(p);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Broadcast("S|LOGIN|FAIL", source);
            }
        }

        private Player GetLoggedPlayerBy(TcpClient client)
        {
            foreach (Player p in loggedClients)
            {
                if (p.socket == client)
                    return p;
            }

            throw new ArgumentException("Client is not logged in.");
        }

        private List<TcpClient> PlayerListToClientList(List<Player> players)
        {
            List<TcpClient> tcpClients = new List<TcpClient>();
            foreach (Player p in players)
            {
                tcpClients.Add(p.socket);
            }
            return tcpClients;
        }

        private Player FindLoggedPlayerBy(TcpClient tcp)
        {
            foreach (Player p in loggedClients)
            {
                if (p.socket == tcp)
                {
                    return p;
                }
            }

            return null;
        }

        private void CloseMatch(int matchId)
        {
            matchManager.CloseMatch(matchId);
            // TODO: Se puede hacer un broadcast que indique que se cerro una partida
            // Asi se puede borrar del menu o algo
            // -- Baja prioridad --
        }
    }
}
