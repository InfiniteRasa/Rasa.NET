using System;

namespace Rasa.Packets.Protocol
{
    using Data;
    using Memory;

    public class CallMethodMessage : ISubtypedPacket<CallMethodMessageSubtype>
    {
        public ClientMessageOpcode Type { get; set; } = ClientMessageOpcode.CallMethod;
        public byte RawSubtype { get; set; }

        public CallMethodMessageSubtype Subtype
        {
            get { return (CallMethodMessageSubtype) RawSubtype; }
            set { RawSubtype = (byte) value; }
        }

        public byte MinSubtype { get; } = 1;
        public byte MaxSubtype { get; } = 4;
        public ClientMessageSubtypeFlag SubtypeFlags { get; } = ClientMessageSubtypeFlag.HasSubtype;
        public uint EntityId { get; private set; }
        public PythonPacket Packet { get; private set; }

        public CallMethodMessage(uint entityId, PythonPacket packet)
        {
            EntityId = entityId;
            Packet = packet;
        }

        public void Read(ProtocolBufferReader reader)
        {
            // read entity id
            switch (Subtype)
            {
                case CallMethodMessageSubtype.MethodId:
                    // readuint
                    break;

                case CallMethodMessageSubtype.MethodName:
                    // readstring
                    break;

                case CallMethodMessageSubtype.UnkPlusMethodId:
                    // readuint
                    break;

                case CallMethodMessageSubtype.UnkPlusMethodName:
                    // readstring
                    break;

                default:
                    throw new Exception($"Invalid subtype ({Subtype}) for CallMethodMessage!");
            }
            throw new NotImplementedException();
        }

        public void Write(ProtocolBufferWriter writer)
        {
            switch (Subtype)
            {
                case CallMethodMessageSubtype.MethodId:
                    break;

                case CallMethodMessageSubtype.MethodName:
                    break;

                case CallMethodMessageSubtype.UnkPlusMethodId:
                    break;

                case CallMethodMessageSubtype.UnkPlusMethodName:
                    break;

                default:
                    throw new Exception($"Invalid subtype ({Subtype}) for CallMethodMessage!");
            }

            throw new NotImplementedException();
        }
    }
}
