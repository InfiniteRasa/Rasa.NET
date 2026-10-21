using System.Collections.Generic;
using System.Globalization;

namespace Rasa.Services.Preloader
{
    /// <summary>
    /// Interior minimap rectangles given the floor and ceiling of the rooms they are drawn for
    /// (Region_floor_ceilings).
    ///
    /// MapRegionPreloader put a rectangle round each cavern or building minimap with no height
    /// to it (-2000 to 2000) and for players under the ground or over it alike. Where the
    /// interior is a level of rooms under a base, the rectangle put the interior's sky over the
    /// base on the ground above: the open plaza and landing pad of Fort Virgil under the cargo
    /// bay's dark interior sky, and Foreas Base under the sky of the two underground levels
    /// beneath it.
    ///
    /// Each rectangle below keeps its x and z and gets the height of the architecture inside it
    /// that is below the base, in the .map: the lowest floor to the highest ceiling of those
    /// pieces, a metre either side. The pieces of the base above start well over it (noted).
    ///
    /// Plain SQL, valid on SQLite and MySQL alike; each row changes only while it is the
    /// rectangle as seeded, and Down gives it its full height again.
    /// </summary>
    public static class RegionFloorCeilings
    {
        public sealed record Bound(uint MapContextId, uint RegionId, double X, double Z, double HalfX, double HalfZ,
            double MinY, double MaxY, string Place, string Why);

        public const double SeededMinY = -2000;
        public const double SeededMaxY = 2000;

        public static readonly Bound[] Bounds =
        {
            new(2047, 23, -350.0, -649.0, 36.5, 47.5, 708.0, 735.5, "Valverde Descent: Fort Virgil's cargo bay",
                "REGION_HUMAN_CARGO_BAY, sky_foreas_general_human_interior; the bay's rooms 709 to 734.5, the fort's plaza and landing pad at 737.2"),
            new(1148, 34, -63.0, 485.0, 108.0, 119.5, 46.5, 73.5, "Concordia Divide: Foreas Base's lower level",
                "sky_foreas_general_human_underground; its rooms 47.5 to 72.5, the base above from 113"),
            new(1148, 35, 112.0, 444.0, 66.0, 40.0, 53.5, 80.5, "Concordia Divide: Foreas Base's east lower level",
                "sky_foreas_general_human_underground; its rooms 54.5 to 79.5, the base above from 123.7"),
        };

        public static IEnumerable<string> Up()
        {
            foreach (var b in Bounds)
                yield return $"UPDATE map_region SET min_y = {N(b.MinY)}, max_y = {N(b.MaxY)} WHERE {Seeded(b)} " +
                    $"AND min_y = {N(SeededMinY)} AND max_y = {N(SeededMaxY)};";
        }

        public static IEnumerable<string> Down()
        {
            foreach (var b in Bounds)
                yield return $"UPDATE map_region SET min_y = {N(SeededMinY)}, max_y = {N(SeededMaxY)} WHERE {Seeded(b)} " +
                    $"AND ABS(min_y - {N(b.MinY)}) < 0.01 AND ABS(max_y - {N(b.MaxY)}) < 0.01;";
        }

        private static string Seeded(Bound b) =>
            $"map_context_id = {b.MapContextId} AND region_id = {b.RegionId} AND shape = 2 " +
            $"AND ABS(pos_x - {N(b.X)}) < 0.01 AND ABS(pos_z - {N(b.Z)}) < 0.01 " +
            $"AND ABS(half_x - {N(b.HalfX)}) < 0.01 AND ABS(half_z - {N(b.HalfZ)}) < 0.01";

        private static string N(double value) => value.ToString("R", CultureInfo.InvariantCulture);
    }
}
