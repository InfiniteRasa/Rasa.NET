namespace Rasa.Packets.Protocol
{
    using Data;
    using Memory;

    public class MoveObjectMessage : ISubtypedPacket<MoveObjectSubtype>
    {
        public ClientMessageOpcode Type { get; set; } = ClientMessageOpcode.MoveObject;
        public byte RawSubtype { get; set; }

        public MoveObjectSubtype Subtype
        {
            get { return (MoveObjectSubtype) RawSubtype; }
            set { RawSubtype = (byte) value; }
        }

        public byte MinSubtype { get; } = 1;
        public byte MaxSubtype { get; } = 3;
        public ClientMessageSubtypeFlag SubtypeFlags { get; } = ClientMessageSubtypeFlag.HasSubtype;

        public byte UnkByte { get; set; }
        public ulong EntityId { get; set; }
        public MovementData MovementData { get; set; }

        public MoveObjectMessage(MovementData movementData)
        {
            Subtype = MoveObjectSubtype.Subtype1;

            MovementData = movementData;
        }

        public MoveObjectMessage(ulong entityId, MovementData movementData)
        {
            Subtype = MoveObjectSubtype.Subtype2;

            EntityId = entityId;
            MovementData = movementData;
        }

        public MoveObjectMessage(byte unkByte, MovementData movementData)
        {
            Subtype = MoveObjectSubtype.Subtype3;

            UnkByte = unkByte;
            MovementData = movementData;
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

            MovementData = reader.ReadMovement();
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

            writer.WriteMovementData(MovementData);
        }
    }
}
