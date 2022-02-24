using Common.Messages;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Text;

namespace Common.Transport
{
    /*
     * Stream extensions for sending data packets
     * between server and clients.
     */
    public static class StreamExtensions
    {
        //Serializes and sends object
        public static void Send(this Stream stream, Object obj)
        {
            string json = JsonConvert.SerializeObject(obj);
            byte[] message = Encoding.UTF8.GetBytes(json);
            stream.Write(message, 0, message.Length);
        }

        //Serializes and sends object asynchronously
        public static void SendAsync(this Stream stream, Object obj)
        {
            string json = JsonConvert.SerializeObject(obj);
            byte[] message = Encoding.UTF8.GetBytes(json);
            try
            {
                stream.WriteAsync(message, 0, message.Length);
            }
            catch { }
        }

        //Sends simple string message
        public static void Send(this Stream stream, string message)
        {
            Send(stream, new StringMessage { Message = message });
        }

        //Receives and deserializes object of given type
        public static T Receive<T>(this Stream stream)
        {
            byte[] buffer = new byte[102400];

            try
            {
                int bytesRead = stream.Read(buffer, 0, buffer.Length);
                byte[] message = new byte[bytesRead];

                Buffer.BlockCopy(buffer, 0, message, 0, bytesRead);
                string json = Encoding.UTF8.GetString(message);
                return JsonConvert.DeserializeObject<T>(json);
            }
            catch
            {
                return default(T);
            }
        }

        //Receives simple string message
        public static string Receive(this Stream stream)
        {
            var msg = stream.Receive<StringMessage>();
            return msg.Message;
        }
    }
}
