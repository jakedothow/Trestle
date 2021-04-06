﻿namespace Trestle.Enums.Packets.Server
{
    public enum PlayPacket : byte
    {
        TabComplete = 0x01,
        KeepAlive = 0x0B,
        ChatMessage = 0x02,
        Player = 0x0C,
        PlayerPosition = 0x0D,
        PlayerLook = 0x0F,
        PlayerPositionAndLook = 0x0E,
        PlayerDigging = 0x14,
        PlayerBlockPlacement = 0x1F,
        ClientSettings = 0x04,
        Animation = 0x1D
    }
}