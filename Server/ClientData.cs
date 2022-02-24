using Common;
using System.Collections.Generic;
using System.Net;

namespace Server
{
    /*
     * Stores all information of a given client
     * on the server side.
     */
    public class ClientData
    {
        public string Name { get; set; }
        public IPEndPoint Ip { get; set; }
        public List<Point> Snake { get; set; }
        public int Score { get; set; }
        public byte Status { get; set; }
        public bool ToAdd { get; set; }

        public ClientData(string name, IPEndPoint ip, int score, byte status)
        {
            Name = name;
            Ip = ip;
            Snake = new List<Point>();
            Score = score;
            Status = status;
            ToAdd = false;
        }
    }
}
