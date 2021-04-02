﻿using System;
using System.Linq;
using Trestle.Enums;
using Trestle.Utils;
using Trestle.Entity;
using System.Threading;
using System.Diagnostics;
using System.Collections.Generic;
using System.Collections.Concurrent;
using Trestle.Networking.Packets.Play.Client;
using Trestle.Worlds;

namespace Trestle.Worlds
{
    public class World : IDisposable
    {
        public ConcurrentDictionary<int, Player> Players { get; private set; }
        public ConcurrentDictionary<int, Entity.Entity> Entities { get; private set; }

        public long Ticks;
        public int PlayerCount => Players.Count;

        public readonly IWorldGenerator WorldGenerator;
        public readonly string Name;
        public Location SpawnPoint;
        public GameMode DefaultGameMode;
        public EntityManager EntityManager;

        private Timer _tickTimer;
        private bool _disposed = false;
        
        public World(string name, IWorldGenerator worldGenerator)
        {
            Name = name;
            WorldGenerator = worldGenerator;

            Players = new ConcurrentDictionary<int, Player>();
            Entities = new ConcurrentDictionary<int, Entity.Entity>();
            EntityManager = new EntityManager();
            
            SpawnPoint = worldGenerator.GetSpawnPoint();
            
            // TODO: Fix this
            //LoadSpawnChunks();
        }

        private void OnTick(object state)
        {
            foreach (var player in Players.Values.ToArray())
                player.OnTick();
        }

        public void LoadSpawnChunks()
        {
            var chunkLoading = Stopwatch.StartNew();

            GenerateChunks(null, new ChunkLocation(SpawnPoint), 8);
                
            chunkLoading.Stop();
            Logger.Info($"Loaded spawn chunks in {chunkLoading.ElapsedMilliseconds}ms");
            
            _tickTimer = new Timer(OnTick, null, 50, 50);
        }

        public void AddEntity(Entity.Entity entity)
            => new NotImplementedException();

        public void RemoveEntity(Entity.Entity entity)
            => new NotImplementedException();

        public virtual void AddPlayer(Player player, bool spawn = true)
        {
            EntityManager.AddEntity(player);

            if (Players.TryAdd(player.EntityId, player))
            {
                // TODO: Be arsed
            }

            player.IsSpawned = spawn;
            player.GameMode = DefaultGameMode;
        }
        
        public virtual void RemovePlayer(Player player, bool despawn = true)
        {
            if (Players.TryRemove(player.EntityId, out _))
            {
                // TODO: Be arsed
                EntityManager.RemoveEntity(null, player);
            }

            player.IsSpawned = !despawn;
        }

        public void SpawnForAll(Player newPlayer)
        {
            var players = Players.Values.ToArray();
            newPlayer.SpawnForPlayers(players);

            foreach (var player in players)
                player.SpawnForPlayers(new Player[] { player });
        }
        
        public IEnumerable<ChunkColumn> GenerateChunks(Player player, ChunkLocation chunkLocation, double radius)
        {
            lock (player.ChunksUsed)
            {
                Dictionary<Tuple<int, int>, double> newOrders = new Dictionary<Tuple<int, int>, double>();

                int centerX = chunkLocation.X;
                int centerZ = chunkLocation.Z;

                for (double x = -radius; x <= radius; ++x)
                {
                    for (double z = -radius; z <= radius; ++z)
                    {
                        double distance = (x*x) + (z*z);
                        int chunkX = (int) (x + centerX);
                        int chunkZ = (int) (z + centerZ);
                        
                        var index = new Tuple<int, int>(chunkX, chunkZ);
                        newOrders[index] = distance;
                    }
                }

                foreach (var chunkKey in player.ChunksUsed.Keys.ToArray())
                {
                    if (!newOrders.ContainsKey(chunkKey))
                    {
                        player?.UnloadChunk(chunkKey.Item1, chunkKey.Item2);
                        player.ChunksUsed.Remove(chunkKey);
                    }
                }

                foreach (var pair in newOrders.OrderBy(pair => pair.Value))
                {
                    if (player.ChunksUsed.ContainsKey(pair.Key) || WorldGenerator == null) 
                        continue;

                    ChunkColumn chunk = WorldGenerator.GenerateChunk(new ChunkLocation(pair.Key.Item1, pair.Key.Item2));

                    player.ChunksUsed.Add(pair.Key, chunk);
                    yield return chunk;
                }
            }
        }

        public void BroadcastChat(MessageComponent message, ChatMessageType chattype, Player sender)
        {
            foreach (var player in Players)
            {
                player.Value.Client.SendPacket(new ChatMessage(message));
            }
        }
        
        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_disposed) 
                    return;
                
                _disposed = true;
                _tickTimer.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~World()
        {
            Dispose(false);
        }
    }
}