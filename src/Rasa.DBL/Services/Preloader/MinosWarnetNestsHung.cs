using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Rasa.Services.Preloader
{
    /// <summary>
    /// Minos Caverns' nine warnet nests hung from the cave roof (Minos_warnet_nests_hung).
    ///
    /// A nest (UsableInertDestMinoswarnetnestDELETEV01) is arch_forean_warnethive_v01.geo, a hive
    /// that hangs from its origin: a stalk at the top, a pod widest 4.25 m below it (5.4 m across)
    /// and its tip 8.9 m down. MinosCavernsContent stood each one's origin on the floor, so the
    /// whole hive was drawn under the floor and a player could only target it, and shoot it,
    /// through the ground.
    ///
    /// Each nest now hangs from the roof over its spot - its origin where the cover mesh first
    /// finds the cave roof above the walkable floor - with the pod low enough to shoot from the
    /// floor. Where the roof over the old spot was too low for the hive, the nest moves along the
    /// tunnel to where it is not: the upper tunnel's west end 7.2 m (the roof there is 6.1 m up),
    /// the main cavern's nest by the offset tunnel 7.2 m, the others 0 to 4.7 m. The warnets'
    /// spawn pools stay on the floor where they were.
    ///
    /// Plain SQL, valid on SQLite and MySQL alike: a row moves only while it is where
    /// MinosCavernsContent put it, and Down puts it back.
    /// </summary>
    public static class MinosWarnetNestsHung
    {
        public const uint Map = MinosCavernsContent.Map;

        private static readonly (double X, double Y, double Z)[] Hang =
        {
            (-15.0, 44.53, 8.5),      // upper tunnel, west end: roof 8.1 m over the floor
            (29.5, 39.41, 9.0),       // upper tunnel, east: 9.1 m
            (33.5, 22.30, 27.0),      // main cavern, by the offset tunnel: 8.9 m
            (69.5, 22.87, 41.5),      // main cavern, the dead end with the fallen soldier: 9.7 m
            (85.0, 23.56, 20.0),      // main cavern, the south passage: 8.7 m
            (90.2, 24.56, 73.2),      // main cavern, the east turn: 9.7 m
            (60.5, 24.54, 62.0),      // main cavern, the north turn: 9.3 m
            (43.0, 6.90, -14.2),      // the vault, the upper ledge: 10.0 m
            (62.0, 3.61, -62.0),      // the vault, by the crystal torches: the overhang 19 m up
        };

        /// <summary>Each nest's row, where it stood, and where it hangs.</summary>
        public static readonly (uint Id, (double X, double Y, double Z) Was, (double X, double Y, double Z) Hangs)[] Nests =
            MinosCavernsContent.Nests.Select((n, i) => (MinosCavernsContent.FirstNest + (uint)i, (n.X, n.Y, n.Z), Hang[i])).ToArray();

        public static IEnumerable<string> Up() =>
            Nests.Select(n => Move(n.Id, n.Was, n.Hangs));

        public static IEnumerable<string> Down() =>
            Nests.Select(n => Move(n.Id, n.Hangs, n.Was));

        private static string Move(uint id, (double X, double Y, double Z) from, (double X, double Y, double Z) to) =>
            $"UPDATE world_destructible SET pos_x = {N(to.X)}, pos_y = {N(to.Y)}, pos_z = {N(to.Z)} " +
            $"WHERE id = {id} AND map_context_id = {Map} AND class_id = {MinosCavernsContent.WarnetNestClass} " +
            $"AND ABS(pos_x - {N(from.X)}) < 0.01 AND ABS(pos_y - {N(from.Y)}) < 0.01 AND ABS(pos_z - {N(from.Z)}) < 0.01;";

        private static string N(double value) => value.ToString("R", CultureInfo.InvariantCulture);
    }
}
