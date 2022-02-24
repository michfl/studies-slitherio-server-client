using System;
using System.Net.Sockets;

namespace Server
{
    /*
     * Class for storing last message from the client.
     */
    public class ClientInfo
    {
        public TcpClient Client { get; set; }
        public string Name { get; set; }
        public double Direction { get; set; }
        public DateTime LastUpdate { get; set; }

        public ClientInfo(TcpClient client, string name)
        {
            Client = client;
            Name = name;
            Direction = 0;
            LastUpdate = DateTime.Now;
        }
    }
}
