using System;
using System.Numerics;

namespace Rasa.Packets.Protocol
{
    using Data;
    using Memory;

    public class MoveMessage : IClientMessage
    {
        public ClientMessageOpcode Type { get; set; } = ClientMessageOpcode.Move;

        public byte RawSubtype
        {
            get { return 0; }
            set { }
        } 

        public byte MinSubtype { get; } = 0;
        public byte MaxSubtype { get; } = 0;
        
        public ClientMessageSubtypeFlag SubtypeFlags { get; } = ClientMessageSubtypeFlag.None;

        public byte UnkByte { get; set; }
        public byte UnkByte2 { get; set; }
        public Vector3 Position { get; set; }
        public float Velocity { get; set; }
        public byte Flag { get; set; }
        public float ViewX { get; set; }
        public float ViewY { get; set; }
        public uint EntityId { get; set; }

        public MoveMessage(uint entityId)
        {
            EntityId = entityId;
        }

        public MoveMessage()
        {
        }

        public void Read(ProtocolBufferReader reader)
        {
            UnkByte = reader.ReadByte();
            /*var movementData = reader.ReadMovement();*/

            reader.ReadDebugByte(41);

            reader.ReadDebugByte(3);
            UnkByte2 = reader.ReadByte();

            var x = reader.ReadPackedFloat(); // TODO
            var y = reader.ReadPackedFloat();
            var z = reader.ReadPackedFloat();

            Position = new Vector3(x, y, z);
            Velocity = reader.ReadPackedVelocity();
            Flag = reader.ReadByte();

            reader.ReadPackedViewCoords(out var viewX, out var viewY);

            ViewX = viewX;
            ViewY = viewY;

            reader.ReadDebugByte(42);
        }

        public void Write(ProtocolBufferWriter writer)
        {
            writer.WriteByte(UnkByte);
            //writer.WriteMovementData();

            writer.WriteUIntBySevenBits(EntityId);

            writer.WriteDebugByte(41);

            writer.WriteDebugByte(3);
            writer.WriteByte(UnkByte2);

            // pos
            writer.WritePackedFloat(Position.X);
            writer.WritePackedFloat(Position.Y);
            writer.WritePackedFloat(Position.Z);

            writer.WritePackedVelocity(Velocity);
            writer.WriteByte(Flag);

            writer.WriteViewCoords(ViewX, ViewY);

            writer.WriteDebugByte(42);
        }
    }
}
