﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Trestle.Attributes;
using Trestle.Enums;
using Trestle.Utils;

namespace Trestle.Networking
{
    public class Listener
    {
        private TcpListener _listener = new(IPAddress.Any, Config.Port);

        private bool _isListening = false;

        private readonly Dictionary<byte, Type> _handshakingPackets = new();
        private readonly Dictionary<byte, Type> _statusPackets = new();
        private readonly Dictionary<byte, Type> _loginPackets = new();
        private readonly Dictionary<byte, Type> _playPackets = new();

        public List<Client> Clients { get; private set; } = new();
        
        public void Start()
        {
            LoadHandlers();
            
            _listener.Start();
            _isListening = true;
            
            Logger.Info($"Accepting connections on port {Config.Port}");
            
            while (_isListening)
            {
                var client = _listener.AcceptTcpClient();
                new Task(() => HandleConnection(client)).Start();
            }
        }

        private void HandleConnection(TcpClient tcpClient)
        {
            var stream = tcpClient.GetStream();
            var client = new Client(tcpClient);
            Clients.Add(client);
            
            while (tcpClient.Connected)
            {
                try
                {
                    while (!stream.DataAvailable)
                    {
                        if (client.Kicked)
                            break;
                        
                        Thread.Sleep(5);
                    }
                    
                    if (client.Kicked)
                        break;

                    // TODO: add support for compressed packets & some other logic
                    HandleUncompressedPacket(client, stream);
                }
                catch (Exception ex)
                {
                }
            }

            // Client lost connection, remove.
            Logger.Info(client.Player.Username + " lost connection");
            Globals.BroadcastChat($"{ChatColor.Yellow}{client.Username} left the game");
            Clients.Remove(client);
        }

        private void HandleUncompressedPacket(Client client, NetworkStream stream)
        {
            int length = ReadVarInt(stream);
            byte[] buffer = new byte[length];
            int receivedData = stream.Read(buffer, 0, buffer.Length);

            if (receivedData > 0)
            {
                var dbuffer = new MinecraftStream(client);
                if (client.Decrypter != null)
                {
                    byte[] data = new byte[4096];
                    client.Decrypter.TransformBlock(buffer, 0, buffer.Length, data, 0);
                    dbuffer.BufferedData = data;
                }

                dbuffer.BufferedData = buffer;
                dbuffer.Size = length;
                
                byte packetId = (byte)dbuffer.ReadVarInt();
                
                HandlePacket(client, dbuffer, packetId);
                
                dbuffer.Dispose();
            }
        }

        private void HandlePacket(Client client, MinecraftStream buffer, byte packetId)
        {
            var type = client.State switch
            {
                ClientState.Handshaking => _handshakingPackets.GetValue(packetId),
                ClientState.Status => _statusPackets.GetValue(packetId),
                ClientState.Login => _loginPackets.GetValue(packetId),
                ClientState.Play => _playPackets.GetValue(packetId),
            };

            if (type == null)
            {
                Logger.Warn($"Unknown packet '0x{packetId:X2}' for state '{client.State}'");
                return;
            }
            
            try
            {
                var packet = (Packet)Activator.CreateInstance(type);
                if (packet == null)
                    throw new Exception($"Unable to create instance of packet handler {type}");
                
                packet.Client = client;

                packet.DeserializePacket(buffer);
                packet.HandlePacket();
            }
            catch (Exception e)
            {
                if(type.GetCustomAttribute<IgnoreExceptionsAttribute>() == null)
                    client.Player?.Kick(new MessageComponent($"{ChatColor.Red}An exception occurred while handling packet.\n\n{ChatColor.Reset}{e.Message}\n{ChatColor.DarkGray}{e.StackTrace}"));
            }
        }
        
        private void LoadHandlers()
        {
            foreach(Type type in Assembly.GetExecutingAssembly().GetTypes())
            {
                var attribute = (ServerBoundAttribute)Attribute.GetCustomAttribute(type, typeof(ServerBoundAttribute));
                if (attribute == null) 
                    continue;
                
                if (attribute.State == ClientState.Handshaking)
                    _handshakingPackets.Add(attribute.Id, type);
                else if (attribute.State == ClientState.Status)
                    _statusPackets.Add(attribute.Id, type);
                else if (attribute.State == ClientState.Login)
                    _loginPackets.Add(attribute.Id, type);
                else if (attribute.State == ClientState.Play)
                    _playPackets.Add(attribute.Id, type);
            }
        }
        
        private int ReadVarInt(NetworkStream stream)
        {
            var value = 0;
            var size = 0;
            int b;

            while (((b = stream.ReadByte()) & 0x80) == 0x80)
            {
                value |= (b & 0x7F) << (size++ * 7);
                
                if (size > 5)
                    throw new IOException("VarInt too long!");
            }

            return value | ((b & 0x7F) << (size * 7));
        }
    }
}