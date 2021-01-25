using System.Numerics;

namespace Rasa.Models
{
    using Memory;

    public class Movement
    {
        public Movement(Vector3 position, float velocity, byte flags, Vector2 viewDirection)
        {
            Position = position;
            Velocity = velocity;
            Flags = flags;
            ViewDirection = viewDirection;
        }

        public Movement(ProtocolBufferReader reader)
        {
            reader.ReadDebugByte(41);
            reader.ReadDebugByte(3);
            reader.ReadByte();

            var x = reader.ReadPackedFloat(); // TODO
            var y = reader.ReadPackedFloat();
            var z = reader.ReadPackedFloat();
            Position = new Vector3(x, y, z);

            Velocity = reader.ReadPackedVelocity();
            Flags = reader.ReadByte();

            var (viewX, viewY) = reader.ReadPackedViewCoords();
            ViewDirection = new Vector2(viewX, viewY);

            reader.ReadDebugByte(42);
        }

        public Vector3 Position { get; }

        public float Velocity { get; }

        public byte Flags { get; }

        public Vector2 ViewDirection { get; }
    }
}