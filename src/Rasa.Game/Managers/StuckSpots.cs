using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Rasa.Managers
{
    using Data;
    using Game;
    using Models;
    using Packets.MapChannel.Server;
    using Packets.Protocol;
    using Structures;

    /// <summary>
    /// Places where the client's collision holds a player the navmesh lets through: a corner
    /// they can walk into and not out of. The server cannot change what the client collides
    /// with, so a living player who stays in one for <see cref="HoldMs"/> is put down at its way
    /// out, on open floor a few metres off. Every copy of the map has them.
    ///
    /// Each is a box around the corner, in world coordinates, and where to put them; the way
    /// out is put on the navmesh under it.
    /// </summary>
    public static class StuckSpots
    {
        /// <summary>How long a player has to stay in one to be taken out.</summary>
        public const long HoldMs = 4_000;

        /// <summary>A corner: its map, what it is, its box, and the way out.</summary>
        public sealed record Spot(uint Map, string Name, Vector3 Min, Vector3 Max, Vector3 Out)
        {
            public bool Holds(Vector3 position) =>
                position.X >= Min.X && position.X <= Max.X && position.Y >= Min.Y && position.Y <= Max.Y &&
                position.Z >= Min.Z && position.Z <= Max.Z;
        }

        public static readonly Spot[] Spots =
        {
            // The cave under Pinhole Falls (Wilderness): a sandbag pile pushed into the rock, which
            // leans out over the corner between them at head height. Out onto the cave floor to
            // the south east.
            new(1220, "Pinhole Falls cave, where the sandbags meet the rock",
                new Vector3(335.4f, 228.0f, 475.4f), new Vector3(337.0f, 230.4f, 477.2f), new Vector3(338.5f, 228.9f, 474.0f))
        };

        /// <summary>Who is in which spot of a copy of the map, and since when.</summary>
        private static readonly ConditionalWeakTable<MapChannel, Dictionary<uint, (Spot Spot, long Since)>> States = new();

        /// <summary>The clock; a test's to replace.</summary>
        public static Func<long> Now { get; set; } = () => Environment.TickCount64;

        /// <summary>The spot a position is in on a map, or null.</summary>
        public static Spot At(uint map, Vector3 position) => Spots.FirstOrDefault(spot => spot.Map == map && spot.Holds(position));

        /// <summary>From the map channel worker.</summary>
        public static void Worker(MapChannel map)
        {
            var mapId = map?.MapInfo?.MapContextId ?? 0;

            if (mapId == 0 || !Spots.Any(spot => spot.Map == mapId))
                return;

            var held = States.GetValue(map, _ => new Dictionary<uint, (Spot, long)>());
            var now = Now();
            var present = new HashSet<uint>();

            foreach (var client in map.ClientList?.ToList() ?? new List<Client>())
            {
                var player = client?.Player;

                if (player == null || client.State != ClientState.Ingame || player.MapChannel != map || player.State == CharacterState.Dead)
                    continue;

                var spot = At(mapId, player.Position);

                if (spot == null)
                    continue;

                present.Add(player.Id);

                if (!held.TryGetValue(player.Id, out var entry) || entry.Spot != spot)
                {
                    held[player.Id] = (spot, now);
                    continue;
                }

                if (now - entry.Since < HoldMs)
                    continue;

                held.Remove(player.Id);
                TakeOut(map, client, spot);
            }

            foreach (var id in held.Keys.Where(id => !present.Contains(id)).ToList())
                held.Remove(id);
        }

        private static void TakeOut(MapChannel map, Client client, Spot spot)
        {
            var player = client.Player;
            var from = player.Position;
            var to = NavMeshManager.SnapToGround(map, spot.Out);

            player.PlaceAt(to);
            client.CellMoveObject(client, new MoveObjectMessage(player.EntityId,
                new Movement(to, client.Movement?.ViewDirection ?? new Vector2(0f, 0f))), false);

            Logger.WriteLog(LogType.Debug, $"Character {player.Id} taken out of a stuck spot ({spot.Name}) at {from}, to {to}.");
        }

        /// <summary>Forgets every copy; for tests.</summary>
        public static void Reset() => States.Clear();
    }
}
