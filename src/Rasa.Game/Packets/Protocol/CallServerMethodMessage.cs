using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Rasa.Packets.Protocol
{
    using Data;
    using Memory;

    public class CallServerMethodMessage : ISubtypedPacket<CallServerMethodSubtype>
    {
        public ClientMessageOpcode Type { get; set; } = ClientMessageOpcode.CallServerMethod;
        public byte RawSubtype { get; set; }
        public CallServerMethodSubtype Subtype
        {
            get { return (CallServerMethodSubtype) RawSubtype; }
            set { RawSubtype = (byte) value; }
        }

        public byte MinSubtype { get; } = 1;
        public byte MaxSubtype { get; } = 10;
        public ClientMessageSubtypeFlag SubtypeFlags { get; } = ClientMessageSubtypeFlag.HasSubtype;

        public GameOpcode MethodId { get; set; }
        public string MethodName { get; set; }
        public PythonPacket Packet { get; set; }

        private byte[] _payload { get; set; }

        public void Read(ProtocolBufferReader reader)
        {
            switch (Subtype)
            {
                case CallServerMethodSubtype.UserMethodById:
                case CallServerMethodSubtype.SysUserMethodById:
                case CallServerMethodSubtype.ActorMethodById:
                case CallServerMethodSubtype.ChatMsgById:
                case CallServerMethodSubtype.WorldMsgById:
                    MethodId = (GameOpcode) reader.ReadUInt();
                    break;

                case CallServerMethodSubtype.UserMethodByName:
                case CallServerMethodSubtype.SysUSerMethodByName:
                case CallServerMethodSubtype.ActorMethodByName:
                case CallServerMethodSubtype.ChatMsgByName:
                case CallServerMethodSubtype.WorldMsgByName:
                    MethodName = reader.ReadString();

                    Debugger.Break(); // This isn't supported yet
                    break;
            }

            _payload = reader.ReadArray();
        }

        public void Write(ProtocolBufferWriter writer)
        {
            switch (Subtype)
            {
                case CallServerMethodSubtype.UserMethodById:
                case CallServerMethodSubtype.SysUserMethodById:
                case CallServerMethodSubtype.ActorMethodById:
                case CallServerMethodSubtype.ChatMsgById:
                case CallServerMethodSubtype.WorldMsgById:
                    writer.WriteUInt((uint) MethodId);
                    break;

                case CallServerMethodSubtype.UserMethodByName:
                case CallServerMethodSubtype.SysUSerMethodByName:
                case CallServerMethodSubtype.ActorMethodByName:
                case CallServerMethodSubtype.ChatMsgByName:
                case CallServerMethodSubtype.WorldMsgByName:
                    writer.WriteString(MethodName);
                    break;
            }

            writer.WriteArray(_payload);
        }

        public bool ReadPacket()
        {
            if(Packet != null)
            {
                return true;
            }

            using var ms = new MemoryStream(_payload, false);
            using var br = new BinaryReader(ms, Encoding.UTF8, true);
            if (br.ReadByte() != 0x4F)
            {
                Logger.WriteLog(LogType.Error, $"Invalid payload formatting for: {MethodId}. Skipping packet...");
                return false;
            }

            var packetType = Rasa.Game.Client.GetPacketType(MethodId);
            if (packetType != null)
            {
                Packet = Activator.CreateInstance(packetType) as PythonPacket;
                if (Packet == null)
                {
                    Logger.WriteLog(LogType.Error, $"Unable to create packet instance for opcode: {MethodId}. Skipping packet...");
                    return false;
                }

                Packet.Read(br);
            }
            else
                Logger.WriteLog(LogType.Error, $"Unhandled game opcode: {MethodId}");

            if (br.ReadByte() != 0x66)
            {
                Logger.WriteLog(LogType.Error, $"Invalid payload formatting for: {MethodId}. Skipping packet...");
                return false;
            }

            return true;
        }
    }
}
