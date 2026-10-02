using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Rasa.Managers
{
    using Data;
    using Game;
    using Packets.MapChannel.Server;
    using Structures;
    using Structures.Char;

    /// <summary>
    /// Hospitals - the client's graveyards - where a dead player goes back to life
    /// (PlayerDeath). They are the teleporter table's type 5 rows.
    ///
    /// A hospital is gained by walking up to it (within <see cref="DiscoveryRadius"/>), as a
    /// waypoint is: kept with the character's waypoints (character_teleporter, type 5), and
    /// announced with GraveyardGained, "You just gained ... Hospital." The client has no trigger
    /// of its own for it; the server watches the players on each map.
    ///
    /// A player who dies is offered the hospitals they have gained on that map, and the free ones:
    /// a hospital the map screen marks as a safe zone (Map_SafeZone, isSafe), and a base's
    /// hospital ("Hospital: Foreas Base") - so nobody is stranded on a map they have only just
    /// arrived on. Should neither give one, the hospital nearest to where they fell is offered.
    ///
    /// A control point's hospital belongs to whoever holds the point (ControlPoints; "Losing a
    /// Control Point to the Bane means that the hospital ... [is] lost"): while the Bane do it is
    /// not offered and is not gained by walking up to it. A player who had gained it keeps it, and
    /// has it back when the point is retaken. Nobody is stranded: with every hospital on the map
    /// lost, the nearest is offered all the same.
    /// </summary>
    public static class Hospitals
    {
        /// <summary>How close a player comes to a hospital to gain it. Ours: a hospital is a building, not a pad.</summary>
        public const float DiscoveryRadius = 15f;

        public sealed class Hospital
        {
            public uint TeleporterId { get; set; }
            public uint GraveyardId { get; set; }
            public uint MapContextId { get; set; }
            public Vector3 Position { get; set; }
            public string Name { get; set; }

            /// <summary>The map screen marks it a safe zone: shown so in the hospital window, and free.</summary>
            public bool IsSafe { get; set; }

            /// <summary>Offered to everyone who dies on its map, gained or not.</summary>
            public bool IsFree { get; set; }
        }

        private static readonly object Lock = new object();
        private static Dictionary<uint, List<Hospital>> _byMap;

        /// <summary>Where the hospitals come from: the teleporter table, as DynamicObjectManager loaded it. Replaceable for tests.</summary>
        public static Func<IEnumerable<(uint Id, uint MapContextId, Vector3 Position, string Name)>> Source { get; set; } = FromTeleporters;

        /// <summary>Whether the map screen marks a teleporter a safe zone. Replaceable for tests.</summary>
        public static Func<uint, bool> IsSafeZone { get; set; } = id => MapMarkerManager.Instance.IsSafeZone(id);

        /// <summary>Whether a hospital is the AFS's to use: not while the Bane hold its control point. Replaceable for tests.</summary>
        public static Func<uint, bool> IsOpen { get; set; } = id => ControlPoints.Instance.IsOpen(id);

        /// <summary>Writes a gained hospital to the character. Replaceable for tests.</summary>
        public static Action<Client, CharacterTeleporterEntry> Persist { get; set; } =
            (client, entry) => CharacterManager.Instance.UpdateCharacter(client, CharacterUpdate.Teleporter, entry);

        /// <summary>Forget what was read, so the next look reads the source again.</summary>
        public static void Reset()
        {
            lock (Lock)
                _byMap = null;
        }

        private static IEnumerable<(uint, uint, Vector3, string)> FromTeleporters()
        {
            foreach (var teleporter in DynamicObjectManager.Instance.Teleporters.Values)
                if (teleporter.ObjectData is WaypointInfo info && info.WaypointType == WaypointType.Hospital && teleporter.MapContextId != 0
                    && teleporter.Position != Vector3.Zero)
                    yield return (info.WaypointId, teleporter.MapContextId, teleporter.Position, teleporter.Comment ?? "");
        }

        /// <summary>The hospitals on a map.</summary>
        public static IReadOnlyList<Hospital> OnMap(uint mapContextId)
        {
            lock (Lock)
            {
                // Read once there is something to read: the teleporters load after the maps.
                if (_byMap == null || _byMap.Count == 0)
                    _byMap = Build(Source());

                return _byMap.TryGetValue(mapContextId, out var list) ? list : new List<Hospital>();
            }
        }

        /// <summary>
        /// The hospitals by map, each with its graveyard id - its own (HospitalGraveyards), or a
        /// generic one nothing else on the map uses - and whether it is safe and free.
        /// </summary>
        internal static Dictionary<uint, List<Hospital>> Build(IEnumerable<(uint Id, uint MapContextId, Vector3 Position, string Name)> rows)
        {
            var byMap = new Dictionary<uint, List<Hospital>>();

            foreach (var group in rows.GroupBy(r => r.MapContextId))
            {
                var list = new List<Hospital>();
                var used = new HashSet<uint>();

                foreach (var row in group.OrderBy(r => r.Id))
                    if (HospitalGraveyards.ByTeleporter.TryGetValue(row.Id, out var named) && used.Add(named))
                        list.Add(Make(row, named));

                foreach (var row in group.OrderBy(r => r.Id))
                {
                    if (list.Any(h => h.TeleporterId == row.Id))
                        continue;

                    var generic = HospitalGraveyards.GenericIds.FirstOrDefault(id => !used.Contains(id));

                    if (generic == 0)
                        continue;

                    used.Add(generic);
                    list.Add(Make(row, generic));
                }

                byMap[group.Key] = list.OrderBy(h => h.TeleporterId).ToList();
            }

            return byMap;
        }

        private static Hospital Make((uint Id, uint MapContextId, Vector3 Position, string Name) row, uint graveyardId)
        {
            var safe = IsSafeZone(row.Id);

            return new Hospital
            {
                TeleporterId = row.Id,
                GraveyardId = graveyardId,
                MapContextId = row.MapContextId,
                Position = row.Position,
                Name = row.Name,
                IsSafe = safe,
                IsFree = safe || IsBase(row.Name)
            };
        }

        /// <summary>A base's own hospital - "Hospital: Foreas Base", "Thunderhead Base Hospital" - which is not a control point's.</summary>
        public static bool IsBase(string name)
        {
            return !string.IsNullOrEmpty(name) && name.Contains(" Base", StringComparison.Ordinal)
                   && !name.Contains("Control Point", StringComparison.OrdinalIgnoreCase) && !name.Contains("(CP)", StringComparison.Ordinal);
        }

        /// <summary>Whether the player has gained this hospital.</summary>
        public static bool Knows(Manifestation player, uint teleporterId)
        {
            lock (player.GainedWaypoints)
                return player.GainedWaypoints.Any(w => w.WaypointId == teleporterId && w.WaypointType == (byte)WaypointType.Hospital);
        }

        /// <summary>
        /// The hospitals a player who died on this map may go back to: those they have gained and
        /// the free ones, or else the one nearest to them; none on a map with no hospital.
        /// </summary>
        public static List<Hospital> AvailableTo(Manifestation player, uint mapContextId)
        {
            var all = OnMap(mapContextId);
            var open = all.Where(h => IsOpen(h.TeleporterId)).ToList();
            var available = open.Where(h => h.IsFree || Knows(player, h.TeleporterId)).ToList();

            // The nearest of those the AFS hold - or of them all, should the Bane hold every one.
            if (available.Count == 0 && all.Count > 0)
                available.Add(Nearest(open.Count > 0 ? open : all, player.Position));

            return available;
        }

        public static Hospital Nearest(IEnumerable<Hospital> hospitals, Vector3 position)
        {
            return hospitals.OrderBy(h => Vector3.DistanceSquared(h.Position, position)).FirstOrDefault();
        }

        /// <summary>Gains every hospital the living players on this map are standing at.</summary>
        internal static void Worker(MapChannel mapChannel)
        {
            var hospitals = OnMap(mapChannel.MapInfo.MapContextId);

            if (hospitals.Count == 0)
                return;

            foreach (var client in mapChannel.ClientList.ToArray())
            {
                var player = client?.Player;

                if (player == null || client.State != ClientState.Ingame || player.Disconected || player.State == CharacterState.Dead)
                    continue;

                foreach (var hospital in hospitals)
                    if (Vector3.Distance(player.Position, hospital.Position) <= DiscoveryRadius && IsOpen(hospital.TeleporterId))
                        Gain(client, hospital);
            }
        }

        /// <summary>Gains a hospital for the player: kept, announced, and marked found on the map. False when they had it.</summary>
        public static bool Gain(Client client, Hospital hospital)
        {
            var player = client?.Player;

            if (player == null || hospital == null)
                return false;

            CharacterTeleporterEntry entry;

            lock (client.SyncRoot)
            {
                if (Knows(player, hospital.TeleporterId))
                    return false;

                entry = new CharacterTeleporterEntry(player.Id, hospital.TeleporterId, (byte)WaypointType.Hospital);

                lock (player.GainedWaypoints)
                    player.GainedWaypoints.Add(entry);
            }

            try
            {
                Persist?.Invoke(client, entry);
            }
            catch (Exception e)
            {
                Logger.WriteLog(LogType.Error, $"Hospital {hospital.TeleporterId} for {player.FamilyName} was not saved: {e.Message}");
            }

            client.CallMethod(player.EntityId, new GraveyardGainedPacket(hospital.TeleporterId));
            MapMarkerManager.Instance.WaypointDiscovered(client, hospital.TeleporterId);

            return true;
        }
    }
}
