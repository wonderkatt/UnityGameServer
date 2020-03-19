using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

public class Client
{
    public static int DataBufferSize = 4096;
    public int id;
    public TCP tcp;
    public UDP udp;
    public Player player;
    public Client(int clientId)
    {
        id = clientId;
        tcp = new TCP(id);
        udp = new UDP(id);

    }

    public class TCP
    {
        public TcpClient Socket;
        private readonly int id;
        private NetworkStream stream;
        private byte[] recieveBuffer;
        private Packet recievedData;


        public TCP(int _id)
        {
            id = _id;

        }

        public void Connect(TcpClient socket)
        {
            Socket = socket;
            Socket.ReceiveBufferSize = DataBufferSize;
            Socket.SendBufferSize = DataBufferSize;

            stream = Socket.GetStream();

            recievedData = new Packet();

            recieveBuffer = new byte[DataBufferSize];

            stream.BeginRead(recieveBuffer, 0, DataBufferSize, RecieveCallback, null);

            ServerSend.Welcome(id, "Welcome to the server");
        }

        public void SendData(Packet packet)
        {
            try
            {
                if (Socket != null)
                {
                    stream.BeginWrite(packet.ToArray(), 0, packet.Length(), null, null);
                }
            }
            catch (Exception ex)
            {
                Debug.Log($"Error sending data to player {id} via TCP: {ex}");

            }
        }
        private void RecieveCallback(IAsyncResult result)
        {
            try
            {
                int byteLength = stream.EndRead(result);
                if (byteLength <= 0)
                {
                    Server.Clients[id].Disconnect();
                    return;
                }
                byte[] data = new byte[byteLength];
                Array.Copy(recieveBuffer, data, byteLength);

                recievedData.Reset(HandleData(data));
                stream.BeginRead(recieveBuffer, 0, DataBufferSize, RecieveCallback, null);
            }
            catch (Exception ex)
            {
                Debug.Log("Error recieving TCP data" + ex);
                Server.Clients[id].Disconnect();
            }
        }

        private bool HandleData(byte[] data)
        {
            int packetLength = 0;
            recievedData.SetBytes(data);

            if (recievedData.UnreadLength() >= 4)
            {
                packetLength = recievedData.ReadInt();
                if (packetLength <= 0)
                {
                    return true;
                }
            }

            while (packetLength > 0 && packetLength <= recievedData.UnreadLength())
            {
                byte[] packetBytes = recievedData.ReadBytes(packetLength);
                ThreadManager.ExecuteOnMainThread(() =>
                {
                    using (Packet packet = new Packet(packetBytes))
                    {
                        int packetId = packet.ReadInt();
                        Server.packetHandlers[packetId](id, packet);
                    }
                });

                packetLength = 0;
                if (recievedData.UnreadLength() >= 4)
                {
                    packetLength = recievedData.ReadInt();
                    if (packetLength <= 0)
                    {
                        return true;
                    }
                }
            }

            if (packetLength <= 1)
            {
                return true;
            }

            return false;
        }

        public void Disconnect()
        {
            Socket.Close();
            stream = null;
            recieveBuffer = null;
            recievedData = null;
            Socket = null;
        }
    }

    public class UDP
    {
        public int Id;
        public IPEndPoint EndPoint;

        public UDP(int id)
        {
            Id = id;
        }

        public void Connect(IPEndPoint endPoint)
        {
            EndPoint = endPoint;
        }

        public void SendData(Packet packet)
        {
            Server.SendUDPData(EndPoint, packet);
        }

        public void HandleData(Packet packetData)
        {
            int packetLength = packetData.ReadInt();
            byte[] packetBytes = packetData.ReadBytes(packetLength);

            ThreadManager.ExecuteOnMainThread(() =>
            {
                using (Packet packet = new Packet(packetBytes))
                {
                    int packetId = packet.ReadInt();
                    Server.packetHandlers[packetId](Id, packet);
                }
            });
        }

        public void Disconnect()
        {
            EndPoint = null;
        }

    }

    public void SendIntoGame(string playerName)
    {
        player = NetworkManager.Instance.InstantiatePlayer();
        player.Initialize(id, playerName);

        foreach (var client in Server.Clients.Values)
        {
            if (client.player != null)
            {
                if (client.id != id)
                {
                    ServerSend.SpawnPlayer(id, client.player);
                }
            }
        }

        foreach (var client in Server.Clients.Values)
        {
            if (client.player != null)
            {
                ServerSend.SpawnPlayer(client.id, player);
            }
        }
    }

    public void Disconnect()
    {
        Debug.Log($"{tcp.Socket.Client.RemoteEndPoint} has disconnected");
        player = null;

        tcp.Disconnect();
        udp.Disconnect();
    }
}


