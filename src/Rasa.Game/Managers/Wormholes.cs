using System.Collections.Generic;
using System.Linq;

namespace Rasa.Managers
{
    using Structures;
    using Structures.World;

    /// <summary>
    /// The world wormholes as one network: the teleporter rows of type 3, one in each of
    /// Concordia Divide (Foreas Base), Torden Plains (Irendas Penal Colony), Valverde Plateau
    /// (Fort Defiance), Ligo Crucible (Outpost Intrepid), Torden Abyss (Icarus Outpost), Valverde
    /// Descent (Fort Virgil), Howling Maw (Gangus Outpost) and Guardian Prominence. A wormhole's
    /// window lists the others, map by map, and picking one takes the player there, onto
    /// another map or another planet - "Fort Defiance is a major transportation hub, not just for
    /// Foreas, but for Areiki as well. There are continents on Areiki that are only accessible
    /// from the Defiance wormhole." (npcgreeting 1449). The client lays such a window out with
    /// a row for every map (waypointwindow.py ShowWaypoints, WORMHOLE). They used to list only
    /// the wormholes on the player's own map, which is to say none, and refused any trip.
    ///
    /// A wormhole need not have been gained to be gone to; walking into one gains it as before.
    /// What closes a trip is a row of wormhole_lock (<see cref="WormholeLockEntry"/>): the trip
    /// is open when every row that matches it is met. The one there is keeps the Concordia
    /// Divide wormhole's way to Arieki shut until the player has taken mission 1038, "Arieki"
    /// ("Go through the wormhole to Arieki."). A trip that is shut is not listed, and refused if
    /// asked for.
    ///
    /// Kind 2, a required level, is read and not enforced yet: its rows are logged at start
    /// and leave the trip open.
    /// </summary>
    public sealed class Wormholes
    {
        private List<WormholeLockEntry> _locks = new List<WormholeLockEntry>();

        /// <summary>The rows in force.</summary>
        public IReadOnlyList<WormholeLockEntry> Locks => _locks;

        /// <summary>
        /// Takes the rows of wormhole_lock. One of a kind there is no such thing as is kept, and
        /// shuts what it matches: a lock nobody understands is safer shut than open.
        /// </summary>
        public void Load(IEnumerable<WormholeLockEntry> rows)
        {
            _locks = (rows ?? Enumerable.Empty<WormholeLockEntry>()).ToList();

            foreach (var row in _locks)
            {
                if (row.Kind == WormholeLockEntry.KindRequiredLevel)
                    Logger.WriteLog(LogType.Initialize,
                        $"wormhole_lock {row.Id} ({row.FromTeleporterId} -> {row.ToTeleporterId}) asks for level {row.Value}: required levels are not enforced yet, so it is open.");
                else if (row.Kind != WormholeLockEntry.KindMissionAccepted)
                    Logger.WriteLog(LogType.Error,
                        $"wormhole_lock {row.Id} ({row.FromTeleporterId} -> {row.ToTeleporterId}) is of kind {row.Kind}, which nothing knows: it is shut.");
            }
        }

        /// <summary>Whether this player may go from the wormhole <paramref name="fromId"/> to <paramref name="toId"/>.</summary>
        public bool IsOpen(Manifestation player, uint fromId, uint toId)
        {
            if (player == null)
                return false;

            foreach (var row in _locks)
            {
                if (row.FromTeleporterId != 0 && row.FromTeleporterId != fromId)
                    continue;

                if (row.ToTeleporterId != 0 && row.ToTeleporterId != toId)
                    continue;

                if (!IsMet(player, row))
                    return false;
            }

            return true;
        }

        private static bool IsMet(Manifestation player, WormholeLockEntry row)
        {
            switch (row.Kind)
            {
                case WormholeLockEntry.KindMissionAccepted:
                    return HasTaken(player, row.Value);

                // Not enforced yet (Load says so): player.Level >= row.Value when it is.
                case WormholeLockEntry.KindRequiredLevel:
                    return true;

                default:
                    return false;
            }
        }

        /// <summary>
        /// Whether the player has ever taken the mission: it is in their log, whatever its state,
        /// or in their history - completed, failed or abandoned, each of which is archived there
        /// (MissionApplication, CharacterMissions.Runtime.Archive).
        /// </summary>
        public static bool HasTaken(Manifestation player, uint missionId) =>
            player.Missions.ContainsKey(missionId) || player.MissionHistory.ContainsKey(missionId);
    }
}
