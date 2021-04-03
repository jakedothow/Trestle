﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using fNbt;
using LibNoise.Modifier;
using Trestle.Worlds.Biomes;
using Trestle.Blocks;
using Trestle.Enums;
using Trestle.Utils;

namespace Trestle.Worlds
{
    public class ChunkColumn : IDisposable
    {
        public IBiome Biome;
		public int[] BiomeColor = ArrayOf<int>.Create(256, 1);
		public byte[] BiomeId = ArrayOf<byte>.Create(256, 1);
		public NibbleArray Blocklight = new(16*16*256);
		public ushort[] Blocks = new ushort[16*16*256];
		public bool IsDirty = false;
		public ushort[] Metadata = new ushort[16*16*256];
		public NibbleArray Skylight = new(16*16*256);
		public IDictionary<Vector3, NbtCompound> TileEntities = new Dictionary<Vector3, NbtCompound>();

		public ChunkColumn()
		{
			for (var i = 0; i < Skylight.Length; i ++)
				Skylight[i] = 0xff;
			for (var i = 0; i < BiomeColor.Length; i++)
				BiomeColor[i] = 8761930;
			for (var i = 0; i < Metadata.Length; i++)
				Metadata[i] = 0;
		}

		public World World { get; set; }
		public int X { get; set; }
		public int Z { get; set; }

		public Material GetBlock(int x, int y, int z)
		{
			var index = x + 16 * z + 16 * 16 * y;
			
			if (index >= 0 && index < Blocks.Length)
				return (Material)Blocks[index];
			
			return Material.Air;
		}

		public byte GetMetadata(int x, int y, int z)
		{
			var index = x + 16*z + 16*16*y;
			if (index >= 0 && index < Metadata.Length)
			{
				return (byte) (Metadata[index]);
			}
			return 0x0;
		}

		public void SetMetadata(int x, int y, int z, byte metadata)
		{
			var index = x + 16*z + 16*16*y;
			if (index >= 0 && index < Metadata.Length)
			{
				Metadata[index] = metadata;
			}
		}

		public void SetBlock(int x, int y, int z, Material block)
		{
			var index = x + 16*z + 16*16*y;
			if (index >= 0 && index < Blocks.Length)
			{
				Blocks[index] = (ushort)block;
				Metadata[index] = 0;
			}
		}
		
		public void SetBlock(int x, int y, int z, Block block)
		{
			var index = x + 16*z + 16*16*y;
			if (index >= 0 && index < Blocks.Length)
			{
				Blocks[index] = block.Id;
				Metadata[index] = block.Metadata;
			}
		}

		public void SetBlocklight(int x, int y, int z, byte data)
		{
			Blocklight[(x*2048) + (z*256) + y] = data;
		}

		public byte GetBlocklight(int x, int y, int z)
		{
			return Blocklight[(x*2048) + (z*256) + y];
		}

		public byte GetSkylight(int x, int y, int z)
		{
			return Skylight[(x*2048) + (z*256) + y];
		}

		public void SetSkylight(int x, int y, int z, byte data)
		{
			Skylight[(x*2048) + (z*256) + y] = data;
		}

		public NbtCompound GetBlockEntity(Vector3 coordinates)
		{
			NbtCompound nbt;
			TileEntities.TryGetValue(coordinates, out nbt);
			return nbt;
		}

		public void SetBlockEntity(Vector3 coordinates, NbtCompound nbt)
		{
			IsDirty = true;
			TileEntities[coordinates] = nbt;
		}

		public void RemoveBlockEntity(Vector3 coordinates)
		{
			IsDirty = true;
			TileEntities.Remove(coordinates);
		}

		public byte[] GetMeta()
		{
			using (var stream = new MemoryStream())
			{
				using (var writer = new NbtBinaryWriter(stream, true))
				{
					writer.Write(IPAddress.HostToNetworkOrder(X));
					writer.Write(IPAddress.HostToNetworkOrder(Z));
					writer.Write((ushort) 0xffff); // bitmap

					writer.Flush();
					writer.Close();
				}
				return stream.ToArray();
			}
		}

		public byte[] GetChunkData()
		{
			using (var stream = new MemoryStream())
			{
				using (var writer = new NbtBinaryWriter(stream, true))
				{
					writer.WriteVarInt((Blocks.Length*2) + Skylight.Data.Length + Blocklight.Data.Length + BiomeId.Length);

					for (var i = 0; i < Blocks.Length; i++)
						writer.Write((ushort) ((Blocks[i] << 4) | Metadata[i]));

					writer.Write(Blocklight.Data);
					writer.Write(Skylight.Data);

					writer.Write(BiomeId);

					writer.Flush();
					writer.Close();
				}
				return stream.ToArray();
			}
		}

		public byte[] GetBytes(bool unloader = false)
		{
			var writer = new MinecraftStream(new byte[0]);
			if (!unloader)
			{
				writer.WriteVarInt((Blocks.Length*2) + Skylight.Data.Length + Blocklight.Data.Length + BiomeId.Length);

				for (var i = 0; i < Blocks.Length; i++)
				{
					writer.WriteUShort((ushort) ((Blocks[i] << 4) | Metadata[i]));
				}

				writer.Write(Blocklight.Data);
				writer.Write(Skylight.Data);

				writer.Write(BiomeId);
			}
			else
			{
				writer.WriteUShort(0);
				writer.WriteVarInt(0);
			}
			return writer.Data;
		}

		public byte[] Export()
		{
			var buffer = new MinecraftStream(new byte[0]);

			buffer.WriteInt(Blocks.Length);

			for (var i = 0; i < Blocks.Length; i++)
				buffer.WriteUShort(Blocks[i]);

			buffer.WriteInt(Blocks.Length);
			for (var i = 0; i < Blocks.Length; i++)
				buffer.WriteUShort((ushort) Metadata[i]);

			buffer.WriteInt(Blocklight.Data.Length);
			buffer.Write(Blocklight.Data);

			buffer.WriteInt(Skylight.Data.Length);
			buffer.Write(Skylight.Data);

			buffer.WriteInt(BiomeId.Length);
			buffer.Write(BiomeId);

			return buffer.Data;
		}

		private bool _disposed = false;
		protected virtual void Dispose(bool disposing)
		{
			if (!_disposed)
			{
				if (disposing)
				{
					Blocks = null;
					Metadata = null;
					Blocklight.Data = null;
					Blocklight = null;
					BiomeId = null;
					BiomeColor = null;
					Skylight.Data = null;
					Skylight = null;
				}

				_disposed = true;
			}
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}
	}
}