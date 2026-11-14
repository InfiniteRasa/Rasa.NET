using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Rasa.Missions.Scenes;
using static Rasa.Services.Preloader.Missions.Plateau.PlateauCreatureSqlV1;

namespace Rasa.Services.Preloader.Missions.Ashen
{
    /// <summary>
    /// The Ashen Desert set 12 pools, Ligo Ashen Desert (map 1734), for Neurotoxins (1300): two pools of set 10's Lasher
    /// (790362, AshenDesignerHidesNpcsV1) on the ridge west of White Oasis Post. No creature row is new; Water Purification
    /// (1299) uses set 10's Coheri Pools Granitour and Poisoning the Wellwater (1302) its own scene panels.
    ///
    /// Ids (the Ashen block 790000-790999, AshenLeadInNpcsV1; set 12 790440-790479): 790440 and 790441 the two pools.
    ///
    /// Sourced: the client's 1300 log (missionconversation 10501, 1.0): "Lashers can be found throughout the Ashen Desert,
    /// including the ridge west of White Oasis."
    ///
    /// Reconstructed (ESTIMATE, confidence 0.3):
    ///  - The ridge: the slope that rises west of the post from its floor (about 282) to the high ground at x -700 (about 385,
    ///    a navmesh floor joined to nothing); the pools on its joined part, by the probe of 2026-11-10 (the client files and the
    ///    server navmesh): each point on the navmesh ground, round it (AshenFixAV1.ClearRadius, 17 m) one navmesh floor joined to
    ///    the White Oasis Post waypoint, a client floor under open sky; walked to on the client's floor from the waypoint and
    ///    back (AshenFixBTests.ClientFloorLine with a player's half width), the walk 30 m or more outside every other hostile
    ///    pool's circle; 60 m or more from every pool of another hostile kind and 85 m or more from every friendly NPC (as set
    ///    10's pools).
    ///  - Three a pool, radius 0, 120 s respawn: set 10's Lasher pools'.
    ///
    /// Plain SQL, valid on SQLite and MySQL; Down takes out only these two rows.
    /// </summary>
    public static class AshenMurkyWatersNpcsV1
    {
        public const uint MapContextId = AshenMissionDataV1.MapContextId;
        public const uint Lasher = AshenDesignerHidesNpcsV1.Lasher;
        public const uint PerPool = AshenDesignerHidesNpcsV1.PerPool, RespawnTenths = AshenDesignerHidesNpcsV1.RespawnTenths, PoolRadius = AshenDesignerHidesNpcsV1.PoolRadius;
        /// <summary>The gaps (2D) a ridge pool keeps: to every pool of another hostile kind, to every friendly NPC's pool.</summary>
        public const float HostileGap = 60, NpcGap = 85;

        /// <summary>The Lasher pools on the ridge west of White Oasis Post: pool, point.</summary>
        public static readonly (uint Pool, (double X, double Y, double Z) At)[] RidgePools =
        {
            (790440, (-648, 336.06, 314)),   // ESTIMATE: the ridge's joined slope, 12 m from the navmesh's nearest edge
            (790441, (-655, 345.48, 360))    // ESTIMATE: 47 m north of 790440 (set 10's Lasher pools stand 50 m apart), 20 m from the edge
        };

        public const uint NextFreeId = 790442;

        /// <summary>The corners of the walk from the White Oasis Post waypoint to a ridge pool (back: the same, the other way round).</summary>
        public static ScenePosition[] WayTo(uint pool) => Array.Empty<ScenePosition>();

        public static IEnumerable<string> Up() =>
            RidgePools.Select(row => Pool(row.Pool, row.At.X, row.At.Y, row.At.Z, MapContextId, Lasher, PerPool, PerPool, RespawnTenths, PoolRadius));

        public static IEnumerable<string> Down()
        {
            yield return $"DELETE FROM spawnpool WHERE id IN ({string.Join(", ", RidgePools.Select(row => row.Pool.ToString(CultureInfo.InvariantCulture)))});";
        }
    }
}
