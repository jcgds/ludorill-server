using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace ludorill_server_core
{
    class MatchManager
    {
        List<Match> matches;

        public Match Locate(TcpClient client)
        {
            foreach (Match m in matches)
            {
                if (m.IsHosting(client))
                    return m;
            }

            throw new ArgumentException("Client is not in any match");
        }

        public void CreateMatch()
        {

        }
    }
}
