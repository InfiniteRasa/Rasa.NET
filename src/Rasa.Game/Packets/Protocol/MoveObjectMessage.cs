using System;

namespace Rasa.Packets.Protocol
{
    using Data;
    using Memory;
    using Models;

    public class MoveObjectMessage : IClientMessage
    {
        public ClientMessageOpcode Type { get; set; } = ClientMessageOpcode.MoveObject;
        public byte RawSubtype { get; set; }
        public byte MinSubtype { get; } = 1;
        public byte MaxSubtype { get; } = 3;
        public ClientMessageSubtypeFlag SubtypeFlags { get; } = ClientMessageSubtypeFlag.HasSubtype;

        public MoveObjectSubtype Subtype
        {
            get { return (MoveObjectSubtype)RawSubtype; }
            set { RawSubtype = (byte)value; }
        }

        public byte UnkByte { get; set; }
        public ulong EntityId { get; set; }
        public Movement Movement { get; set; }

        public MoveObjectMessage(Movement movementData)
        {
            Subtype = MoveObjectSubtype.Subtype1;

            Movement = movementData;
        }

        public MoveObjectMessage(ulong entityId, Movement movement)
        {
            Subtype = MoveObjectSubtype.Subtype2;

            EntityId = entityId;
            Movement = movement;
        }

        public MoveObjectMessage(byte unkByte, Movement movement)
        {
            Subtype = MoveObjectSubtype.Subtype3;

            UnkByte = unkByte;
            Movement = movement;
        }

        public void Read(ProtocolBufferReader reader)
        {
            switch (Subtype)
            {
                case MoveObjectSubtype.Subtype2:
                    EntityId = reader.ReadULong();
                    break;

                case MoveObjectSubtype.Subtype3:
                    UnkByte = reader.ReadByte();
                    break;
            }

            Movement = reader.ReadMovement();
        }

        public void Write(ProtocolBufferWriter writer)
        {
            switch (Subtype)
            {
                case MoveObjectSubtype.Subtype2:
                    writer.WriteULong(EntityId);
                    break;

                case MoveObjectSubtype.Subtype3:
                    writer.WriteByte(UnkByte);
                    break;
            }

            writer.WriteMovementData(Movement);
        }
    }
}
