﻿using System;
using System.Reflection;
using Trestle.Attributes;
using Trestle.Networking;
using Trestle.Utils;

namespace Trestle.Entity
{
    public class Metadata
    {
        [Field]
        [Index(0)]
        public byte Status { get; set; } = 0x00;

        [Field]
        [Index(1)]
        public int Air { get; set; } = 300;

        [Field]
        [Index(2)]
        public string CustonName { get; set; } = "";

        [Field]
        [Index(3)]
        public bool IsCustomNameVisible { get; set; } = false;

        [Field]
        [Index(4)]
        public bool IsSilent { get; set; } = false;

        [Field]
        [Index(5)]
        public bool HasNoGravity { get; set; } = false;
        
        public byte[] ToArray()
        {
            var buffer = new MinecraftStream();

            foreach (var property in GetType().GetProperties())
            {
                // Checks if the field is meant to be serialized
                var field = (FieldAttribute)property.GetCustomAttribute<FieldAttribute>(false);
                if (field == null)
                    continue;
                
                var index = property.GetCustomAttribute<IndexAttribute>(false);
                if (index == null)
                    continue;

                buffer.WriteByte((byte)index.Index);
                switch (property.GetValue(this))
                {
                    case byte data:
                        buffer.WriteVarInt(0);
                        buffer.WriteByte(data);
                        break;
                    case int data:
                        buffer.WriteVarInt(1);
                        buffer.WriteVarInt(data);
                        break;
                    case float data:
                        buffer.WriteVarInt(2);
                        buffer.WriteFloat(data);
                        break;
                    case string data:
                        buffer.WriteVarInt(3);
                        buffer.WriteString(data);
                        break;
                    case bool data:
                        buffer.WriteVarInt(6);
                        buffer.WriteBool(data);
                        break;
                    default:
                        var message = $"Unable to serialize field '{property.Name}' of type '{property.PropertyType}'";
                        //Client.Player?.Kick(new MessageComponent($"{ChatColor.Red}An error occured while serializing.\n\n{ChatColor.Reset}{message}"));
                        throw new Exception(message);
                        break;
                }
            }
            buffer.WriteByte(0xff);

            return buffer.Data;
        }
    }
}