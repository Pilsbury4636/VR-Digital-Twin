using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System;
using System.Threading;
using System.Threading.Tasks;

public class TwinClient : MonoBehaviour
{

    private IPEndPoint ipEndPoint;
    public const int HEADER = 64;
    public static readonly Encoding FORMAT = Encoding.UTF8;

    private async void Start()
    {
        ipEndPoint = GetEndPoint();            
        await ConnectToServer(ipEndPoint, "Hello World!");
    }

    private IPEndPoint GetEndPoint()
    {
        
        var hostName = Dns.GetHostName();
        var hostEntry = Dns.GetHostEntry(hostName);

        foreach (var addr in hostEntry.AddressList)
        {
            if (addr.AddressFamily == AddressFamily.InterNetwork)
                return new IPEndPoint(addr, 5050);
        }

        return new IPEndPoint(IPAddress.Loopback, 5050);
    }

    private async Task ConnectToServer(IPEndPoint endPoint, string msg)
    {
        using Socket client = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

        try
        {
            await client.ConnectAsync(endPoint);
            Debug.Log($"Connected to {endPoint.Address}:{endPoint.Port}");

           
            byte[] messageBytes = Encoding.UTF8.GetBytes(msg);
            int messageLength = messageBytes.Length;
            byte[] lengthBytes = FORMAT.GetBytes(messageLength.ToString());

            if (lengthBytes.Length > HEADER)
                throw new InvalidOperationException($"Message length header too long ({lengthBytes.Length} > {HEADER}).");

            byte[] headerBytes = new byte[HEADER];
            Array.Copy(lengthBytes, headerBytes, lengthBytes.Length);

            for (int i = lengthBytes.Length; i < HEADER; i++)
                headerBytes[i] = (byte)' ';

            await client.SendAsync(new ArraySegment<byte>(headerBytes), SocketFlags.None);
            await client.SendAsync(new ArraySegment<byte>(messageBytes), SocketFlags.None);
            Debug.Log($"Sent: {msg}");

       

            client.Shutdown(SocketShutdown.Both);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Socket error: {ex.Message}");
        }
    }
}
