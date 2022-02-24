using System.Collections.Generic;


namespace Common.Messages
{
    /*
     * Packet consisting players, score and food info
     * sent to clients.
     */
    public class DataPacket
    {
        // Player name -> status, score, points
        public Dictionary<string, (byte, int, List<Point>)> Snakes { get; set; }
        // List of points
        public List<Point> Food { get; set; }

        public DataPacket(Dictionary<string, (byte, int, List<Point>)> snakes, List<Point> food)
        {
            Snakes = snakes;
            Food = food;
        }
    }
}
