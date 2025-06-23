using System.Net;
using System.Net.Sockets;

public partial class Issue554_ImplicitArrayType
{
    public static void Main()
    {
        byte[] msg = new byte[] { 2 };
        var ep = new IPEndPoint(IPAddress.Loopback, 1434);
        var l_socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        int i_Test;
        int[] i_Tab;
        int[,] bearb;
        l_socket.SendTo(msg, ep);
    }
}