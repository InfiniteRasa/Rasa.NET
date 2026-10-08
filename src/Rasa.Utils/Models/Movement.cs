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

        public Movement(MovementType type, Vector3 position, float velocity, byte flags, Vector2 viewDirection)
        {
            Type = type;
            Position = position;
            Velocity = velocity;
            Flags = flags;
            ViewDirection = viewDirection;
        }

        /// <summary>The flags a player's own client always sets: bit 3, the fast turn rate for the copy others see.</summary>
        public const byte FastTurn = 0x08;

        /// <summary>The most a velocity can be on the wire: sixteen bits of 1/1024 m/s.</summary>
        public const float MaxVelocity = ushort.MaxValue / 1024f;

        /// <summary>Thrown to where the knockback ends: the client flies the arc itself (MovementType.Knockback).</summary>
        public static Movement Knockback(Vector3 destination, Vector2 viewDirection) =>
            new Movement(MovementType.Knockback, destination, 0f, FastTurn, viewDirection);

        /// <summary>Run to where the rush ends at this speed: the client makes the run itself (MovementType.Rush).</summary>
        public static Movement Rush(Vector3 destination, float speed, Vector2 viewDirection) =>
            new Movement(MovementType.Rush, destination, speed, FastTurn, viewDirection);

        /// <summary>What kind of movement this is: the first byte of the block.</summary>
        public MovementType Type { get; }

        public Vector3 Position { get; }

        public float Velocity { get; }

        /// <summary>
        /// Bits 0 to 2: a value from 0 to 7 the client's controller keeps, by its shape the way
        /// the entity moves against the way it faces. Bit 3: turn at the fast rate. Bit 4:
        /// interpolate over 800 ms rather than 200.
        /// </summary>
        public byte Flags { get; }

        public Vector2 ViewDirection { get; }
    }
}