using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ServerSend
{
    private static void SendTcpData(int toClient, Packet packet)
    {
        packet.WriteLength();
        Server.Clients[toClient].tcp.SendData(packet);
    }

    private static void SendUdpData(int toClient, Packet packet)
    {
        packet.WriteLength();
        Server.Clients[toClient].udp.SendData(packet);
    }

    private static void SendTPCDataToAll(Packet packet)
    {
        for (int i = 1; i <= Server.MaxPlayers; i++)
        {
            Server.Clients[i].tcp.SendData(packet);
        }
    }

    private static void SendTPCDataToAll(int exceptClient, Packet packet)
    {
        for (int i = 1; i <= Server.MaxPlayers; i++)
        {
            if (i != exceptClient)
            {
                Server.Clients[i].tcp.SendData(packet);
            }
        }
    }

    private static void SendUdpDataToAll(Packet packet)
    {
        packet.WriteLength();
        for (int i = 1; i <= Server.MaxPlayers; i++)
        {
            Server.Clients[i].udp.SendData(packet);
        }
    }

    private static void SendUdpDataToAll(int exceptClient, Packet packet)
    {
        packet.WriteLength();
        for (int i = 1; i <= Server.MaxPlayers; i++)
        {
            if (i != exceptClient)
            {
                Server.Clients[i].udp.SendData(packet);
            }
        }
    }



    public static void Welcome(int _toClient, string _msg)
    {
        using (Packet _packet = new Packet((int)ServerPackets.welcome))
        {
            _packet.Write(_msg);
            _packet.Write(_toClient);

            SendTcpData(_toClient, _packet);
        }
    }

    public static void SpawnPlayer(int _toClient, Player _player)
    {
        using (Packet _packet = new Packet((int)ServerPackets.spawnPlayer))
        {
            _packet.Write(_player.Id);
            _packet.Write(_player.Username);
            _packet.Write(_player.transform.position);
            _packet.Write(_player.transform.rotation);

            SendTcpData(_toClient, _packet);
        }
    }

    public static void PlayerPosition(Player _player)
    {
        using (Packet _packet = new Packet((int)ServerPackets.playerPosition))
        {
            _packet.Write(_player.Id);
            _packet.Write(_player.transform.position);

            SendUdpDataToAll(_packet);
        }
    }

    /// <summary>Sends a player's updated rotation to all clients except to himself (to avoid overwriting the local player's rotation).</summary>
    /// <param name="_player">The player whose rotation to update.</param>
    public static void PlayerRotation(Player _player)
    {
        using (Packet _packet = new Packet((int)ServerPackets.playerRotation))
        {
            _packet.Write(_player.Id);
            _packet.Write(_player.transform.rotation);

            SendUdpDataToAll(_player.Id, _packet);
        }
    }
}
