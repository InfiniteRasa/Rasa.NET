using System.Numerics;

namespace Rasa.Structures
{
    using Data;
    using World;

    /// <summary>
    /// The live form of a <see cref="MapEmitterEntry"/>: an FX package played at a spot on a map,
    /// and the FXPackageEmitter entity that plays it. See EmitterManager.
    /// </summary>
    public class MapEmitter
    {
        public uint Id { get; set; }
        public uint MapContextId { get; set; }
        public Vector3 Position { get; set; }
        public double Rotation { get; set; }
        public uint PackageId { get; set; }

        /// <summary>The row's state: whether it plays when the map comes up.</summary>
        public bool OnByDefault { get; set; }

        /// <summary>Whether it is playing now. Starts as OnByDefault; turned on and off without touching the row by whatever scripts it.</summary>
        public bool IsOn { get; set; }

        public string Comment { get; set; }

        /// <summary>The FXPackageEmitter in the world; null on a map that is not loaded.</summary>
        public DynamicObject Object { get; set; }

        public static MapEmitter FromEntry(MapEmitterEntry entry)
        {
            return new MapEmitter
            {
                Id = entry.Id,
                MapContextId = entry.MapContextId,
                Position = entry.Position,
                Rotation = entry.Rotation,
                PackageId = entry.PackageId,
                OnByDefault = entry.IsOn != 0,
                IsOn = entry.IsOn != 0,
                Comment = entry.Comment ?? ""
            };
        }

        public MapEmitterEntry ToEntry()
        {
            return new MapEmitterEntry
            {
                Id = Id,
                MapContextId = MapContextId,
                PosX = Position.X,
                PosY = Position.Y,
                PosZ = Position.Z,
                Rotation = Rotation,
                PackageId = PackageId,
                IsOn = (byte)(OnByDefault ? 1 : 0),
                Comment = Comment ?? ""
            };
        }

        public string Describe()
        {
            var state = IsOn ? "on" : "off";
            var start = IsOn == OnByDefault ? "" : $" (starts {(OnByDefault ? "on" : "off")})";

            return $"#{Id} {FxPackages.NameOf(PackageId)} [{PackageId}] {state}{start} at ({Position.X:0.#}, {Position.Y:0.#}, {Position.Z:0.#}) - {Comment}";
        }
    }
}
