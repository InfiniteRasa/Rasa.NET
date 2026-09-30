using System;
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

        private byte[] Payload { get; set; }

        /// <summary>A by-name call whose name is not a method in the client's method table.</summary>
        private bool UnknownMethodName { get; set; }

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
                    // The by-name subtypes carry the method's name in place of its id. The names are
                    // generated.client.methodid's, which GameOpcode matches one for one, so the name
                    // resolves to the same opcode and the call goes on as the by-id form would.
                    // This used to stop at a Debugger.Break(): a stall with a debugger attached, and
                    // a crash on a Windows box without one, from one message any client could send.
                    MethodName = reader.ReadString();
                    UnknownMethodName = !TryResolve(MethodName, out var methodId);
                    MethodId = methodId;
                    break;
            }

            Payload = reader.ReadArray();
        }

        /// <summary>
        /// The opcode a method name stands for: an identifier that names a GameOpcode exactly.
        /// Enum.TryParse alone would also take a number ("145") or a comma list ("A, B") and
        /// hand back an opcode, or a value no method has.
        /// </summary>
        public static bool TryResolve(string name, out GameOpcode methodId)
        {
            methodId = default;

            if (string.IsNullOrEmpty(name) || name.Length > 64 || !char.IsLetter(name[0]))
                return false;

            foreach (var c in name)
                if (!char.IsLetterOrDigit(c) && c != '_')
                    return false;

            return Enum.TryParse(name, false, out methodId) && Enum.IsDefined(typeof(GameOpcode), methodId);
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

            writer.WriteArray(Payload);
        }

        public bool ReadPacket()
        {
            if (UnknownMethodName)
            {
                Logger.WriteLog(LogType.Security, $"By-name call to '{MethodName}', which is not a method in the client's table. Skipping packet...");
                return false;
            }

            using (var ms = new MemoryStream(Payload, false))
            {
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
                {
                    Logger.WriteLog(LogType.Error, $"Unhandled game opcode: {MethodId}");
                    return false;
                }

                if (br.ReadByte() != 0x66)
                {
                    Logger.WriteLog(LogType.Error, $"Invalid payload formatting for: {MethodId}. Skipping packet...");
                    return false;
                }

                if (br.BaseStream.Position != br.BaseStream.Length)
                {
                    Logger.WriteLog(LogType.Error, $"Trailing payload data for: {MethodId}. Skipping packet...");
                    return false;
                }
            }

            return true;
        }
    }
}
