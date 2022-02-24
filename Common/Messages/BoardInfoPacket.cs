namespace Common.Messages
{
    /*
     * Packet consisting game board info
     * sent to clients.
     */
    public class BoardInfoPacket
    {
        public byte ID { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
    }
}
