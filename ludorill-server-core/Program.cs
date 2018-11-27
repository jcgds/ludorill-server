using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace ludorill_server_core
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("1. Correr servidor");
            Console.WriteLine("2. Correr cliente de prueba");
            string c = Console.ReadLine();
            if (c == "1")
            {
                Console.WriteLine("Iniciando servidor...");
                PlayerDao playerDao = new PlayerDaoTextFile();
                // TODO: Manejar error puerto en uso
                Server s = new Server(playerDao);
                s.Listen();
                /*
                Thread t = new Thread(new ThreadStart(s.Listen));
                int seleccion2 = -1;
                while (seleccion2 != 0)
                {
                    Console.Clear();
                    Console.WriteLine("Escuchando puerto {0}" + Environment.NewLine, s.port);
                    Console.WriteLine("1. Registrar Usuarios");
                    Console.WriteLine("2. Listar usuarios");
                    Console.WriteLine("3. Partidas");
                    Console.WriteLine("0. Salir");
                    seleccion2 = Console.Read() - '0';

                    switch (seleccion2)
                    {
                        case 0:
                            break;
                    }
                }*/
            } else
            {
                try
                {
                    TcpClient testClient = new TcpClient("127.0.0.1", 6969);
                    NetworkStream st = testClient.GetStream();
                    StreamWriter writer = new StreamWriter(st);
                    while (true)
                    {
                        Console.WriteLine("Username:");
                        string username = Console.ReadLine();
                        if (username == "chao")
                            break;
                        Console.WriteLine("Password:");
                        string password = Console.ReadLine();
                        writer.WriteLine(string.Format("C|REGISTER|{0}|{1}", username, password));
                        writer.Flush();
                        Console.WriteLine("Mensaje enviado");
                    }
                    Console.WriteLine("Cerrando");
                    st.Close();
                    testClient.Close();
                }
                catch (SocketException e)
                {
                    Console.WriteLine("Error conectando a servidor");
                    Console.ReadKey();
                }
            }
        }
    }
}
