using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;

namespace Rasa.AuthPackets.Server
{
    using AuthData;
    using AuthStructures;
    using Packets;

    public class SendServerListExtPacket : IOpcodedPacket<AuthServerOpcode>
    {
        public byte LastServerId { get; set; }
        public List<ServerInfo> ServerList { get; set; }

        public AuthServerOpcode Opcode { get; } = AuthServerOpcode.SendServerListExt;

        public SendServerListExtPacket(List<ServerInfo> servers, byte lastServerId = 0)
        {
            ServerList = servers;
            LastServerId = lastServerId;
        }

        public void Read(BinaryReader reader)
        {
            ServerList = new List<ServerInfo>();

            var count = reader.ReadByte();
            LastServerId = reader.ReadByte();

            for (var i = 0; i < count; ++i)
            {
                ServerList.Add(new ServerInfo
                {
                    ServerId = reader.ReadByte(),
                    Ip = new IPAddress(reader.ReadBytes(4)),
                    QueuePort = reader.ReadUInt32(),
                    AgeLimit = reader.ReadByte(),
                    PKFlag = reader.ReadByte(),
                    CurrentPlayers = reader.ReadUInt16(),
                    MaxPlayers = reader.ReadUInt16(),
                    Status = reader.ReadByte()
                });
            }
        }

        public void Write(BinaryWriter writer)
        {
            if (ServerList == null)
                throw new InvalidOperationException("You must specify a list of ServerInfo before you can serialize it!");

            var count = ServerList.Count();
            if (count >= 16)
                count = 16;

            writer.Write((byte) Opcode);
            writer.Write((byte)count);
            writer.Write(LastServerId);

            var c = 0U;

            foreach (var s in ServerList)
            {
                writer.Write(s.ServerId);
                writer.Write(s.Ip.GetAddressBytes());
                writer.Write(s.QueuePort);
                writer.Write(s.AgeLimit);
                writer.Write(s.PKFlag);
                writer.Write(s.CurrentPlayers);
                writer.Write(s.MaxPlayers);
                writer.Write(s.Status);

                if (++c == count)
                    break;
            }
        }

        public override string ToString()
        {
            return $"SendServerListExtPacket(Count: {ServerList?.Count ?? -1})";
        }
    }
}
