using System.Numerics;

namespace Rasa.Models
{
    public class Movement
    {
        public Movement(Vector3 position, float velocity, byte flags, Vector2 viewDirection)
        {
            Position = position;
            Velocity = velocity;
            Flags = flags;
            ViewDirection = viewDirection;
        }

        public Movement(Vector3 position, Vector2 viewDirection)
        {
            Position = position;
            ViewDirection = viewDirection;
        }

        public Movement(byte unknownByte, Vector3 position, float velocity, byte flags, Vector2 viewDirection)
        {
            UnknownByte = unknownByte;
            Position = position;
            Velocity = velocity;
            Flags = flags;
            ViewDirection = viewDirection;
        }

        public byte UnknownByte { get; set; }

        public Vector3 Position { get; }

        public float Velocity { get; }

        public byte Flags { get; }

        public Vector2 ViewDirection { get; }
    }
}