using System;

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
        

        public uint MethodId { get; set; }
        public string MethodName { get; set; }
        public byte[] Payload { get; set; }

        public void Read(ProtocolBufferReader reader)
        {
            switch (Subtype)
            {
                case CallServerMethodSubtype.UserMethodById:
                case CallServerMethodSubtype.SysUserMethodById:
                case CallServerMethodSubtype.ActorMethodById:
                case CallServerMethodSubtype.ChatMsgById:
                case CallServerMethodSubtype.WorldMsgById:
                    MethodId = reader.ReadUInt();
                    break;

                case CallServerMethodSubtype.UserMethodByName:
                case CallServerMethodSubtype.SysUSerMethodByName:
                case CallServerMethodSubtype.ActorMethodByName:
                case CallServerMethodSubtype.ChatMsgByName:
                case CallServerMethodSubtype.WorldMsgByName:
                    MethodName = reader.ReadString();
                    break;
            }

            Payload = reader.ReadArray();
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
                    writer.WriteUInt(MethodId);
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
    }
}
