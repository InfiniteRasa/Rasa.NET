using System;
using System.Globalization;
using System.Numerics;

namespace Rasa.Structures
{
    using Data;

    /// <summary>
    /// One volume of ground that hurts (Managers.MissionHazards): a circle or a turned box on a map, between two heights,
    /// that puts its effect on a player standing in it and hurts them on the effect's tick.
    /// </summary>
    public sealed class HazardVolume
    {
        public enum HazardKind
        {
            Radiation = 1,
            ToxicGas = 2
        }

        public enum HazardShape
        {
            Circle = 0,
            Box = 1
        }

        public uint Id { get; init; }
        public uint MapContextId { get; init; }
        public HazardKind Kind { get; init; }
        public HazardShape Shape { get; init; }

        /// <summary>The middle of the circle or the box, on the ground; Y is only for telling where it is.</summary>
        public Vector3 Position { get; init; }

        /// <summary>A circle's radius.</summary>
        public float Radius { get; init; }

        /// <summary>A box's half sizes along its own X and Z.</summary>
        public float HalfX { get; init; }
        public float HalfZ { get; init; }

        /// <summary>A box's turn about Y, in radians.</summary>
        public float Rotation { get; init; }

        /// <summary>The heights it holds, both included: the floor under it to the air over a jump.</summary>
        public float MinY { get; init; }
        public float MaxY { get; init; }

        /// <summary>The client's effect (gameeffectdata) and the level it is drawn at.</summary>
        public int EffectTypeId { get; init; }
        public uint EffectLevel { get; init; }

        public DamageType DamageType { get; init; }

        /// <summary>How often it hurts.</summary>
        public int TickMs { get; init; }

        /// <summary>Each hurt: a roll between the two (both included; 0 and 0 for none) plus a share of the victim's own maximum health.</summary>
        public int DamageMin { get; init; }
        public int DamageMax { get; init; }
        public double PercentMaxHealth { get; init; }

        /// <summary>How long the effect stays on after the player was last found in it.</summary>
        public int LingerMs { get; init; }

        /// <summary>The mission a player must hold (taken, and not yet turned in, failed or abandoned) for it to touch them; null: everyone.</summary>
        public uint? MissionId { get; init; }

        public string Comment { get; init; } = "";

        /// <summary>Placed by a GM (.hazard place), held until the next restart.</summary>
        public bool Placed { get; init; }

        /// <summary>Switched on or off by a GM (.hazard id on|off), until the next restart.</summary>
        public bool Enabled { get; set; } = true;

        /// <summary>How far from <see cref="Position"/> on the ground any point of it is: the cheap test before <see cref="Contains"/>.</summary>
        public float Reach => Shape == HazardShape.Circle ? Radius : MathF.Sqrt(HalfX * HalfX + HalfZ * HalfZ);

        /// <summary>Whether the point is inside: within the heights, and within the circle or the turned box.</summary>
        public bool Contains(Vector3 point)
        {
            if (point.Y < MinY || point.Y > MaxY)
                return false;

            var dx = point.X - Position.X;
            var dz = point.Z - Position.Z;

            if (Shape == HazardShape.Circle)
                return dx * dx + dz * dz <= Radius * Radius;

            // Into the box's own frame: turned back by its rotation.
            var cos = MathF.Cos(-Rotation);
            var sin = MathF.Sin(-Rotation);
            var localX = dx * cos - dz * sin;
            var localZ = dx * sin + dz * cos;

            return MathF.Abs(localX) <= HalfX && MathF.Abs(localZ) <= HalfZ;
        }

        /// <summary>How far the point is from it on the ground: 0 inside (heights aside), the distance to its edge outside.</summary>
        public float Distance(Vector3 point)
        {
            var dx = point.X - Position.X;
            var dz = point.Z - Position.Z;

            if (Shape == HazardShape.Circle)
                return MathF.Max(0, MathF.Sqrt(dx * dx + dz * dz) - Radius);

            var cos = MathF.Cos(-Rotation);
            var sin = MathF.Sin(-Rotation);
            var outX = MathF.Max(0, MathF.Abs(dx * cos - dz * sin) - HalfX);
            var outZ = MathF.Max(0, MathF.Abs(dx * sin + dz * cos) - HalfZ);

            return MathF.Sqrt(outX * outX + outZ * outZ);
        }

        /// <summary>One line for a GM.</summary>
        public string Describe()
        {
            var shape = Shape == HazardShape.Circle
                ? FormattableString.Invariant($"circle r {Radius:0.#}")
                : FormattableString.Invariant($"box {HalfX * 2:0.#} x {HalfZ * 2:0.#}");
            var damage = DamageMax > 0 ? $"{DamageMin}-{DamageMax}" : "";

            if (PercentMaxHealth > 0)
                damage += FormattableString.Invariant($"{(damage.Length > 0 ? " + " : "")}{PercentMaxHealth:0.#}% max health");

            var where = FormattableString.Invariant(
                $"#{Id} {Kind} ({shape}, y {MinY:0.#}..{MaxY:0.#}) at {Position.X:0.#}, {Position.Y:0.#}, {Position.Z:0.#}");
            var what = FormattableString.Invariant($"effect {EffectTypeId} L{EffectLevel}, {DamageType} {damage} every {TickMs / 1000.0:0.#} s");

            return $"{where}: {what}"
                + (MissionId.HasValue ? $", mission {MissionId.Value} only" : ", everyone")
                + (Placed ? ", GM-placed" : "") + (Enabled ? "" : ", OFF")
                + (Comment.Length > 0 ? $" - {Comment}" : "");
        }
    }
}
