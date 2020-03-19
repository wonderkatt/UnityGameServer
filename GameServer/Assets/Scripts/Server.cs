using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

public class Server 
{
    public static int MaxPlayers { get; private set; }
    public static int Port { get; set; }
    public delegate void PacketHandler(int fromClient, Packet packet);
    public static Dictionary<int, PacketHandler> packetHandlers;

    private static TcpListener TcpListener;
    private static UdpClient UdpListener;

    public static Dictionary<int, Client> Clients = new Dictionary<int, Client>();
    public static void Start(int maxPlayers, int port)
    {
        MaxPlayers = maxPlayers;
        Port = port;

        Debug.Log("starting server");
        InitializeServerData();
        TcpListener = new TcpListener(IPAddress.Any, port);
        TcpListener.Start();
        TcpListener.BeginAcceptTcpClient(new AsyncCallback(TCPConnectCallback), null);

        UdpListener = new UdpClient(Port);
        UdpListener.BeginReceive(UDPRecieveCallback, null);
        Debug.Log("Server started on port: " + port);
    }

    private static void TCPConnectCallback(IAsyncResult result)
    {
        TcpClient client = TcpListener.EndAcceptTcpClient(result);
        TcpListener.BeginAcceptTcpClient(TCPConnectCallback, null);

        Debug.Log($"incomming connection from {client.Client.RemoteEndPoint}");

        for (int i = 1; i <= MaxPlayers; i++)
        {
            if (Clients[i].tcp.Socket == null)
            {
                Clients[i].tcp.Connect(client);
                return;
            }
        }

        Debug.Log($"{client.Client.RemoteEndPoint} failed to connect, Server is full");
    }

    private static void UDPRecieveCallback(IAsyncResult result)
    {
        try
        {
            IPEndPoint clientEndPoint = new IPEndPoint(IPAddress.Any, 0);
            byte[] data = UdpListener.EndReceive(result, ref clientEndPoint);
            UdpListener.BeginReceive(UDPRecieveCallback, null);

            if (data.Length < 4)
            {
                return;
            }

            using (Packet packet = new Packet(data))
            {
                int clientId = packet.ReadInt();

                if (clientId == 0)
                {
                    return;
                }

                if (Clients[clientId].udp.EndPoint == null)
                {
                    Clients[clientId].udp.Connect(clientEndPoint);
                    return;
                }

                if (Clients[clientId].udp.EndPoint.ToString() == clientEndPoint.ToString())
                {
                    Clients[clientId].udp.HandleData(packet);
                }
            }
        }
        catch (Exception ex)
        {
            Debug.Log($"Error recievin UDP data: {ex}");

        }
    }

    public static void SendUDPData(IPEndPoint clientEndPoint, Packet packet)
    {
        try
        {
            if (clientEndPoint != null)
            {
                UdpListener.BeginSend(packet.ToArray(), packet.Length(), clientEndPoint, null, null);
            }
        }
        catch (Exception)
        {
            Debug.Log($"Error sending data to {clientEndPoint}");

        }
    }

    private static void InitializeServerData()
    {
        for (int i = 1; i <= MaxPlayers; i++)
        {
            Clients.Add(i, new Client(i));
        }

        packetHandlers = new Dictionary<int, PacketHandler>()
            {
                {(int)ClientPackets.welcomeReceived, ServerHandle.WelcomeRecieved },
                {(int)ClientPackets.playerMovement, ServerHandle.PlayerMovement },
            };

        Debug.Log("Initialized Packets");
    }
}

