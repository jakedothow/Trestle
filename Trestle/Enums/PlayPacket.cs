﻿using Trestle.Attributes;
using Trestle.Networking;

namespace Trestle.Enums
{
    public enum PlayPacket : byte
    {
        // Clientbound
        Client_PlayerPositionAndLook = 0x08,
        Client_ChatMessage = 0x02,
        Client_KeepAlive = 0x00,
        Client_SpawnPosition = 0x05,
        Client_JoinGame = 0x01,
        Client_ChunkData = 0x21,
        Client_ChangeGameState = 0x2B,
        Client_Disconnect = 0x40,
        Client_Animation = 0x0B,
        Client_SpawnObject = 0x0E,
        Client_EntityMetadata = 0x1C,
        Client_DestroyEntities = 0x13,
        Client_CollectItem = 0x0D,
        Client_SoundEffect = 0x29,
        
        // Serverbound
        Server_PlayerPositionAndLook = 0x06,
        Server_ChatMessage = 0x01,
        Server_KeepAlive = 0x00,
        Server_Player = 0x03,
        Server_PlayerPosition = 0x04,
        Server_PlayerLook = 0x05,
        Server_PlayerDigging = 0x07,
        Server_ClientSettings = 0x15,
    }
}