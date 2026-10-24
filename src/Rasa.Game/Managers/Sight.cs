using System;
using System.Numerics;

namespace Rasa.Managers
{
    using Config;
    using Navigation;
    using Structures;

    /// <summary>
    /// What a line of sight is tested against under the ground. The cover mesh (Navigation.CoverMesh) has a map's
    /// collision triangles and its terrain as a height grid, and the grid is the whole heightmap: it knows nothing of
    /// a cave or a tunnel that runs under it. Two actors standing in one are, to the grid, both deep in the ground,
    /// and every line between them longer than a grid cell is blocked by terrain: a ranged hit does the least share
    /// of its damage there (Cover.Modifier), a creature never uses a ranged attack (BehaviorManager.Sees), a selected
    /// target is wholly behind cover (WeaponChecks). The Pinhole Falls Caverns under the Wilderness, where "Mama
    /// Miasma!" sends players after three egg-layers, are such a place: the grid is fifty metres and more over them.
    ///
    /// With appsettings.json's Sight.UnderTerrain on, a line that has an end underground is tested against the
    /// collision alone - the cave's own walls, floor and roof, which the cover file has (Rasa.NavMesh builds it from
    /// every entity's collision, the caves' shells among them) - as the client's own sight test does for such a
    /// line. Underground is the map's word and not the actor's (<see cref="IsUnderground"/>): the feet are under the
    /// height grid by <see cref="Depth"/> or more; the navmesh by them is surface that was built under the terrain
    /// (NavMeshFlags.Underground; <see cref="FloorAcross"/>, <see cref="FloorBelow"/>, <see cref="FloorAbove"/>);
    /// and nothing of the collision lies between the body and the air over that floor. A place inside a hill, which
    /// a client can say it stands at, is under the grid and by no such floor; a place in the rock right beside a cave
    /// or over its roof has the cave's own shell between it and the floor; a place on the hill over a cave is not
    /// under the grid. None of them gains anything.
    ///
    /// There is no roof condition: the places under the grid with a floor and no roof in the collision are real
    /// sunken places where the client has a floor and leaves its own ground out too; a roof asked for took most of
    /// the fix back when it was tried on five maps (22-38 % of cave lines parted from the client's again, against
    /// 0.1-1.4 % without).
    ///
    /// Off, which is the default without the section, nothing here is asked and every answer is the cover mesh's own.
    /// </summary>
    public static class Sight
    {
        /// <summary>How far under the height grid the feet have to be: what the navmesh's builder takes for under the terrain.</summary>
        public const float Depth = NavMeshFlags.UndergroundDepth;

        /// <summary>
        /// How far from the feet the underground floor may be: to the side (the navmesh keeps a body's width off every
        /// wall, and has none under a rock or a crate that a body can stand on), under them (a jump, a ledge stepped
        /// off, the top of that rock) and over them (a client's floor and the navmesh's are not the same surface to
        /// the centimetre).
        /// </summary>
        public const float FloorAcross = 2f;
        public const float FloorBelow = 3f;
        public const float FloorAbove = 1f;

        public static SightConfig Config { get; set; } = new SightConfig();

        /// <summary>
        /// The underground floor a map has right under a place, or null: the navmesh's answer
        /// (NavMeshQuery.UndergroundFloor), kept as a call so that a test can stand in for a navmesh it has not got.
        /// </summary>
        public static Func<MapChannel, Vector3, Vector3?> UndergroundFloor { get; set; } = NavMeshFloor;

        /// <summary>The navmesh's answer; null on a map that has none.</summary>
        public static Vector3? NavMeshFloor(MapChannel map, Vector3 feet)
        {
            return map?.NavMesh?.UndergroundFloor(feet, FloorAcross, FloorBelow, FloorAbove);
        }

        /// <summary>
        /// Whether an actor standing at <paramref name="feet"/> is underground: under the height grid of
        /// <paramref name="cover"/> by <see cref="Depth"/>, by floor the map has under the terrain, and in the air over
        /// that floor - from chest height over the floor straight up or down to the height of its own chest, and from
        /// there across to it, nothing of the collision is in the way. So it stands in the cave, on its floor or on
        /// something in it, and not in the rock beside the cave or over its roof within reach of the floor. The grid
        /// is asked first, so on the surface - nearly everywhere - neither the navmesh nor the collision is looked at.
        /// </summary>
        public static bool IsUnderground(MapChannel map, CoverMesh cover, Vector3 feet)
        {
            if (map == null || cover == null)
                return false;

            var ground = cover.TerrainHeight(feet.X, feet.Z);

            if (!ground.HasValue || !(feet.Y < ground.Value - Depth))
                return false;

            var floor = UndergroundFloor(map, feet);

            if (!floor.HasValue)
                return false;

            var chest = feet + new Vector3(0f, MovementChecks.ChestHeight, 0f);
            var overFloor = floor.Value + new Vector3(0f, MovementChecks.ChestHeight, 0f);
            var level = new Vector3(overFloor.X, chest.Y, overFloor.Z);

            return !cover.GeometryBlocks(overFloor, level) && !cover.GeometryBlocks(level, chest);
        }

        /// <summary>
        /// Whether the height grid is left out of the line between two actors, standing at <paramref name="a"/> and
        /// <paramref name="b"/>: the section is on and either of them is underground.
        /// </summary>
        public static bool WithoutTerrain(MapChannel map, CoverMesh cover, Vector3 a, Vector3 b)
        {
            return Config.UnderTerrain && (IsUnderground(map, cover, a) || IsUnderground(map, cover, b));
        }

        /// <summary>The same for a line with one actor to it: to an object, which has no feet to stand anywhere.</summary>
        public static bool WithoutTerrain(MapChannel map, CoverMesh cover, Vector3 a)
        {
            return Config.UnderTerrain && IsUnderground(map, cover, a);
        }

        /// <summary>Whether anything stands on the segment: the cover mesh's own answer, or that of its collision alone.</summary>
        public static bool Blocked(CoverMesh cover, Vector3 from, Vector3 to, bool withoutTerrain)
        {
            return withoutTerrain ? cover.GeometryBlocks(from, to) : cover.Blocked(from, to);
        }
    }
}
