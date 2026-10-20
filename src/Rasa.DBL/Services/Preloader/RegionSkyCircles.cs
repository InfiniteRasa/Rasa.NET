using System.Collections.Generic;
using System.Globalization;

namespace Rasa.Services.Preloader
{
    /// <summary>
    /// Outdoor places that were under an interior or cave sky (Region_sky_circles).
    ///
    /// The client draws the sky of the highest-priority region it is told the player is in that
    /// has one (regionmanager.py OnUpdateRegions; a map's regions, their priorities and skies are
    /// in its .map). The region volumes seeded in map_region put a 200 m circle round each of the
    /// client's region labels with every region of that name in it, and a rectangle round each
    /// cavern or building minimap. Where a place's regions are an outdoor one and an interior or
    /// cave one, or the interior one has a rectangle of its own as well, the circle put the
    /// interior's sky over the open ground round the label: a building's dark interior over a
    /// whole fort, a cave over a village.
    ///
    /// Each circle below is disabled. Where the region has its own rectangle (noted), that keeps
    /// its sky where it belongs; where it has none, the place has the outdoor sky until a game
    /// master puts the interior where it is (.region). Disabled, not deleted, so .regions still
    /// lists it.
    ///
    /// Plain SQL, valid on SQLite and MySQL alike; each row changes only while it is the circle
    /// as seeded, and Down enables it again.
    /// </summary>
    public static class RegionSkyCircles
    {
        public sealed record Circle(uint MapContextId, uint RegionId, double X, double Z, double Radius, string Place, string Why);

        public static readonly Circle[] Circles =
        {
            new(1759, 4, 612.2, 403.8, 200, "Torden Mires: Fort Haroun", "sky_arieki_general_human_interior, the fort's only region"),
            new(1759, 18, -542.7, 278.0, 200, "Torden Mires: Bane Occupied Mires", "sky_bane_interior_default, beside a region of the same name with no sky"),
            new(1764, 6, 208.0, -115.8, 200, "Torden Plains: Irendas Penal Colony", "sky_arieki_brann_interior_default, over region 5's outdoor sky_arieki_torden_plains"),
            new(1911, 39, 624.5, -479.5, 137, "Ligo Thunderhead: Thunderhead Base", "sky_arieki_torden_abyss_cave, over region 9's outdoor sky_arieki_ligo_thunderhead_bright"),
            new(1911, 25, 624.5, -479.5, 137, "Ligo Thunderhead: Thunderhead Base", "sky_arieki_general_human_interior; its own 227 m rectangle stays"),
            new(1911, 27, 953.0, 510.0, 200, "Ligo Thunderhead: AFS Substation E-104", "sky_arieki_general_human_interior; its own rectangle stays"),
            new(1911, 26, -777.0, 5.0, 200, "Ligo Thunderhead: Rivasa Atta Colony", "sky_arieki_atta_interior_corridors; its own underground rectangle stays"),
            new(2047, 26, 540.2, 401.5, 200, "Valverde Descent: Mal Dys", "sky_bane_interior_default; its own rectangle stays"),
            new(2047, 28, 540.2, 401.5, 200, "Valverde Descent: Mal Dys", "sky_bane_interior_default, over regions 17 and 27's outdoor tiers; its own rectangle stays"),
            new(2047, 21, 101.2, 542.5, 200, "Valverde Descent: Antaeus Hollow", "sky_foreas_cave_short; its own underground rectangle stays"),
            new(1244, 30, 737.3, -267.6, 155, "Palisades: Staging Point", "sky_foreas_cave_hall; its own rectangle stays"),
            new(1220, 38, -662.9, 799.5, 200, "Wilderness: Daghda's Urn Village", "sky_foreas_cave_short, beside two regions of the same name with no sky"),
            new(2374, 16, -64.0, -365.8, 200, "Wargame Edmund Range: Staging Area", "sky_foreas_general_human_interior"),
            new(2361, 16, 40.0, -20.0, 200, "Wargame Proving Grounds: Staging Area", "sky_foreas_general_human_interior"),
        };

        public static IEnumerable<string> Up()
        {
            foreach (var c in Circles)
                yield return $"UPDATE map_region SET enabled = 0 WHERE {Seeded(c)} AND enabled = 1;";
        }

        public static IEnumerable<string> Down()
        {
            foreach (var c in Circles)
                yield return $"UPDATE map_region SET enabled = 1 WHERE {Seeded(c)} AND enabled = 0;";
        }

        private static string Seeded(Circle c) =>
            $"map_context_id = {c.MapContextId} AND region_id = {c.RegionId} AND shape = 1 " +
            $"AND ABS(pos_x - {N(c.X)}) < 0.01 AND ABS(pos_z - {N(c.Z)}) < 0.01 AND ABS(radius - {N(c.Radius)}) < 0.01";

        private static string N(double value) => value.ToString("R", CultureInfo.InvariantCulture);
    }
}
