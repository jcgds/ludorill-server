using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ludorill_server_core.Exceptions;

namespace ludorill_server_core
{
    class PlayerDaoTextFile : IPlayerDao
    {
        private const string PATH = "playerdb.txt";

        public PlayerDaoTextFile()
        {
            if (!File.Exists(PATH))          
                File.Create(PATH);           
        }

        public void DeletePlayer(Player p)
        {
            throw new NotImplementedException();
        }

        public List<Player> GetAllPlayers()
        {
            throw new NotImplementedException();
        }

        public Player GetPlayer(string username)
        {
            using (StreamReader sr = File.OpenText(PATH))
            {
                string s = "";
                while ((s = sr.ReadLine()) != null)
                {
                    Player p = LineToPlayer(s);
                    if (p.username == username)
                        return p;
                }
            }

            throw new ArgumentException("No player for given username");
        }

        public void SavePlayer(Player p)
        {
            try
            {
                GetPlayer(p.username);
                throw new UsernameAlreadyUsedException();
            }
            catch (ArgumentException)
            {
                StreamWriter writer = File.AppendText(PATH);
                writer.WriteLine(string.Format("{0}:{1}", p.username, p.password));
                writer.Flush();
                writer.Close();
            }           
        }

        private Player LineToPlayer(string fileLine)
        {
            string[] split = fileLine.Split(':');
            // TODO: Validar linea valida
            return new Player(split[0], split[1]);
        }
    }
}
