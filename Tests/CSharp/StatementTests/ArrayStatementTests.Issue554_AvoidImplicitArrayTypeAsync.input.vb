Imports System.Net
Imports System.Net.Sockets

Public Class Issue554_ImplicitArrayType
    Public Shared Sub Main()
        Dim msg() As Byte = {2}
        Dim ep As IPEndPoint = New IPEndPoint(IPAddress.Loopback, 1434)
        Dim l_socket As Socket = New Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp)
        Dim i_Test, i_Tab(), bearb(,) As Integer
        l_socket.SendTo(msg, ep)
    End Sub
End Class