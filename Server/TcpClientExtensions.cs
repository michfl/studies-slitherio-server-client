using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace Server
{
    /*
     * TCP client extenion for checking connection state.
     */
    public static class TcpClientExtensions
    {
        //Gets current connection state
        public static TcpState GetState(this TcpClient tcpClient)
        {
            var foo = IPGlobalProperties.GetIPGlobalProperties()
              .GetActiveTcpConnections()
              .SingleOrDefault(x => x.LocalEndPoint.Equals(tcpClient.Client.LocalEndPoint)
                                 && x.RemoteEndPoint.Equals(tcpClient.Client.RemoteEndPoint)
              );

            return foo != null ? foo.State : TcpState.Unknown;
        }
    }
}
