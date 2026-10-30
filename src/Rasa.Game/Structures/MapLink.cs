using System.Numerics;

namespace Rasa.Structures
{
    using Data;
    using Interfaces;
    using World;

    /// <summary>
    /// The live form of a <see cref="MapLinkEntry"/>: a trigger radius on one map that sends
    /// whoever walks into it to a position on another map. Registered in the source map's cell
    /// like a <see cref="MapTrigger"/>, so the per-second worker only has to look at the links
    /// in the player's own cell matrix (CellManager.ViewRange).
    /// </summary>
    public class MapLink : IHasPosition
    {
        public uint Id { get; set; }
        public uint MapContextId { get; set; }
        public Vector3 Position { get; set; }
        public double Rotation => 0;
        public float Radius { get; set; }
        public uint DestMapContextId { get; set; }
        public Vector3 DestPosition { get; set; }
        public float DestRotation { get; set; }
        public MapLinkKind Kind { get; set; }
        public bool Enabled { get; set; }
        public string Comment { get; set; }

        public static MapLink FromEntry(MapLinkEntry entry)
        {
            return new MapLink
            {
                Id = entry.Id,
                MapContextId = entry.MapContextId,
                Position = entry.Position,
                Radius = (float)entry.Radius,
                DestMapContextId = entry.DestMapContextId,
                DestPosition = entry.DestPosition,
                DestRotation = (float)entry.DestRotation,
                Kind = (MapLinkKind)entry.Kind,
                Enabled = entry.Enabled != 0,
                Comment = entry.Comment ?? string.Empty
            };
        }

        public MapLinkEntry ToEntry()
        {
            return new MapLinkEntry
            {
                Id = Id,
                MapContextId = MapContextId,
                PosX = Position.X,
                PosY = Position.Y,
                PosZ = Position.Z,
                Radius = Radius,
                DestMapContextId = DestMapContextId,
                DestPosX = DestPosition.X,
                DestPosY = DestPosition.Y,
                DestPosZ = DestPosition.Z,
                DestRotation = DestRotation,
                Kind = (byte)Kind,
                Enabled = Enabled ? (byte)1 : (byte)0,
                Comment = Comment ?? string.Empty
            };
        }

        public override string ToString()
        {
            return $"#{Id} {Kind} {MapContextId} ({Position.X:0.#}, {Position.Y:0.#}, {Position.Z:0.#}) r={Radius:0.#} -> {DestMapContextId} ({DestPosition.X:0.#}, {DestPosition.Y:0.#}, {DestPosition.Z:0.#})"
                   + (Enabled ? "" : " [disabled]") + (string.IsNullOrEmpty(Comment) ? "" : $" {Comment}");
        }
    }
}
