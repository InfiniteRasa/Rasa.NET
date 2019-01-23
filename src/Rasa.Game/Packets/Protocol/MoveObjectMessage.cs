using System;

namespace Rasa.Packets.Protocol
{
    using Data;
    using Memory;

    public class MoveObjectMessage : IClientMessage
    {
        public ClientMessageOpcode Type { get; set; } = ClientMessageOpcode.MoveObject;
        public byte RawSubtype { get; set; }

        public byte MinSubtype { get; } = 1;
        public byte MaxSubtype { get; } = 3;
        public ClientMessageSubtypeFlag SubtypeFlags { get; } = ClientMessageSubtypeFlag.HasSubtype;

        public byte UnkByte { get; set; }
        public uint EntityId { get; set; }
        public MovementData MovementData { get; set; }

        public MoveObjectMessage(byte unkByte, uint entityId, MovementData movementData)
        {
            UnkByte = unkByte;
            EntityId = entityId;
            MovementData = movementData;
        }

        public void Read(ProtocolBufferReader reader)
        {
            throw new NotImplementedException();
        }

        public void Write(ProtocolBufferWriter writer)
        {
            writer.WriteByte(UnkByte);

            writer.WriteByte(2);
            
            writer.WriteUIntBySevenBits(EntityId);

            writer.WriteMovementData(MovementData);
        }
    }
}
