using Rasa.Missions.Scenes;

namespace Rasa.Services.Preloader.Missions.Marshes
{
    /// <summary>
    /// The seven gas harvester places of Reaping the Harvest (881) and Hassling the Harvesters (890) on dry ground (to-fix row 20,
    /// 2026-11-03). Why: the places of MarshesHarvestersNpcsV1 stood in the water of the Northern Mud Pit: the client map's swamp
    /// water is the WaterForeasPond128mSwamp tiles (class 6408) at y 214.68 round the pit, and the terrain there was 213.3-214.0,
    /// so a player swam to them and could not fire (pm 998), and 881's small body and tap drew under the water.
    ///
    /// Source: the client map adv_foreas_valverde_marshes.map has no harvester, tap or pump static in the area (mapfile.py,
    /// 2026-11-03: 160 m round the field, and no class named harvest, gas, pump, tap, siphon or extract on the whole map), so the
    /// places are ours (ESTIMATE): each the nearest point to its V1 place (1-11 m) where the client terrain heightmap is at
    /// least 214.75 (above the water) on a 3 m square round it, the navmesh lies on that terrain (within 1 m), the tap's spot
    /// 3 m east is the same, it is at least 30 m from the others, at least ReachClearance outside every hostile pool's circle
    /// (33.8 m and more), and the walk from the Falcon Hold waypoint is complete (navmesh probe 2026-11-03). Heights the
    /// navmesh's. The harvester tower's mesh (prop_bane_gasharvester.geo, 7906) has its origin at its base's top, with a
    /// 3 m foundation below it (collision y -2.97..5.44, the same 2.7 m square at -3 and at 0), so its origin goes on the
    /// ground, as before.
    /// </summary>
    public static class MarshesHarvesterPlacesV2
    {
        public static readonly ScenePosition[] Harvesters =
        {
            new(-27f, 217.14f, 607f), new(36f, 217.08f, 574f), new(99f, 217.04f, 601f), new(-9f, 216.99f, 634f),
            new(51f, 217.06f, 685f), new(-39f, 217.03f, 700f), new(39f, 216.99f, 721f)
        };

        /// <summary>Each harvester's sample tap, 3 m east of its body (navmesh heights).</summary>
        public static readonly ScenePosition[] Taps =
        {
            new(-24f, 216.9f, 607f), new(39f, 216.57f, 574f), new(102f, 217.08f, 601f), new(-6f, 216.53f, 634f),
            new(54f, 216.46f, 685f), new(-36f, 216.53f, 700f), new(42f, 217f, 721f)
        };

        /// <summary>The water surface of the pit (the WaterForeasPond128mSwamp tiles' height).</summary>
        public const float WaterLevel = 214.68f;
    }
}
