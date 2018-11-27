using System;
using System.Net;
using System.Net.Sockets;

namespace ludorill_server_core
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("1. Correr servidor");
            Console.WriteLine("2. Correr cliente de prueba");
            int c = Console.Read();
            if (c - '0' == 1)
            {
                Console.WriteLine("Iniciando servidor...");
                // TODO: Manejar error puerto en uso
                Server s = new Server();
                s.StartListening();
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
                    switch(seleccion2)
                    {

                    }
                }
            } else
            {
                try
                {
                    TcpClient testClient = new TcpClient("127.0.0.1", 6969);
                    NetworkStream st = testClient.GetStream();
                    Byte[] data = System.Text.Encoding.ASCII.GetBytes("Hola soy un cliente");
                    st.Write(data, 0, data.Length);
                    Console.WriteLine("Mensaje enviado");
                    Console.ReadKey();
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
