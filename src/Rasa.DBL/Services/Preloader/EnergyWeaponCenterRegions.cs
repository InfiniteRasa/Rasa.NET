using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Rasa.Services.Preloader
{
    /// <summary>
    /// The Energy Weapon Center's rooms (adv_arieki_torden_mires_energyweaponcenter, 2107;
    /// Energy_weapon_center_regions).
    ///
    /// The map, "Casso Weapons Facility" (its region 0, under the Bane interior sky), has no
    /// labels, so none of its regions had a volume. From the way in at the east end (map link at
    /// (346.8, 97.8, 0), the hospital beside it) a tunnel runs west along z 0 past three Bane
    /// control rooms, two circuit tubes and a round hall to the central room, which looks out
    /// through the base's exterior window onto a yard under the open sky.
    ///
    /// - Reactor Tubes (3): the two parallel circuit tubes (arch_bane_industrial_circuit_*,
    ///   x 126 to 258, along z -24 and z -72, a reactor machine in each) and the service rooms
    ///   between them.
    /// - Warehouse (5): the round hall north of the tunnel (arch_bane_industrial_chunnel_room_turn
    ///   pieces round (192, 96, 88), x 128 to 256, z 22 to 154): 27 Bane crate stacks, two landing
    ///   pads and catwalks.
    /// - Tesla Control Room (4): the control room south of the tunnel cross
    ///   (arch_bane_industrial_room_control_24m_v10 at (296, 96, -48)): consoles, a command
    ///   console monitor and command chairs on its catwalks over a pool of Bane goo; nothing of a
    ///   lab but one bio table.
    /// - Research Lab (6): the central room (arch_bane_industrial_room_control_24m_v08 at
    ///   (88, 96, 0)): three research bio tables and extraction sensors round two chairs on its
    ///   floor, a fourth with its sensors in the galleries over it, and its exterior window to
    ///   the west. The galleries have consoles too, but the bio tables are the map's lab.
    /// - Region 2 (unnamed, sky_arieki_default only): the yard outside that window, the one part
    ///   of the map under the open sky, which the map's own region put under the Bane interior
    ///   sky: Bane trenches, sandbags, floodlights, catwalks and sulfur ponds in front of the
    ///   prison facade, among the rock vistas; surface only, so the base behind stays the map's.
    ///
    /// The third control room, north of the tunnel at (296, 96, 88), is empty and has no region.
    ///
    /// Plain SQL, valid on SQLite and MySQL alike. Each row goes in only where the map is loaded
    /// and no box of that region is at that spot; Down takes out the rows as they went in.
    /// </summary>
    public static class EnergyWeaponCenterRegions
    {
        public sealed record Volume(uint RegionId, double X, double Y, double Z, double HalfX, double HalfZ, double MinY, double MaxY, byte Underground, string Comment);

        public const uint MapContextId = 2107;
        public const uint YardSkyRegion = 2, ReactorTubesRegion = 3, TeslaControlRoomRegion = 4, WarehouseRegion = 5, ResearchLabRegion = 6;
        public const byte Box = 2, AnyLevel = 0, SurfaceOnly = 2;

        private const string Map = "arieki_torden_mires_energyweaponcenter - ";

        public static readonly Volume[] Volumes =
        {
            Of(ReactorTubesRegion, 125, 259, -86, -10, 94, 120, AnyLevel, Map + "Reactor Tubes (circuit tubes)"),
            Of(WarehouseRegion, 127, 257, 21, 155, 94, 140, AnyLevel, Map + "Warehouse (round hall)"),
            Of(TeslaControlRoomRegion, 263, 329, -81, -15, 94, 126, AnyLevel, Map + "Tesla Control Room (south control room)"),
            Of(ResearchLabRegion, 23, 122, -41, 41, 92, 126, AnyLevel, Map + "Research Lab (central room)"),
            Of(ResearchLabRegion, 72, 128, 40, 74, 102, 114, AnyLevel, Map + "Research Lab (north gallery)"),
            Of(ResearchLabRegion, 72, 105, -57, -40, 102, 114, AnyLevel, Map + "Research Lab (south gallery)"),
            Of(YardSkyRegion, -200, 30, -150, 190, 85, 220, SurfaceOnly, Map + "open sky over the yard"),
        };

        private static Volume Of(uint region, double west, double east, double south, double north, double floor, double roof, byte underground, string comment) =>
            new(region, (west + east) / 2, floor, (south + north) / 2, (east - west) / 2, (north - south) / 2, floor, roof, underground, comment);

        public static IEnumerable<string> Up()
        {
            foreach (var v in Volumes)
                yield return "INSERT INTO map_region (map_context_id, region_id, shape, pos_x, pos_y, pos_z, radius, half_x, half_z, min_y, max_y, underground, enabled, comment) " +
                    $"SELECT {MapContextId}, {v.RegionId}, {Box}, {N(v.X)}, {N(v.Y)}, {N(v.Z)}, 0, {N(v.HalfX)}, {N(v.HalfZ)}, {N(v.MinY)}, {N(v.MaxY)}, {v.Underground}, 1, {S(v.Comment)} " +
                    $"FROM map_info m WHERE m.map_context_id = {MapContextId} AND NOT EXISTS (SELECT 1 FROM map_region x WHERE {At(v, "x")});";
        }

        public static IEnumerable<string> Down()
        {
            foreach (var v in Volumes.Reverse())
                yield return $"DELETE FROM map_region WHERE {At(v, "map_region")} AND ABS(half_x - {N(v.HalfX)}) < 0.01 AND ABS(half_z - {N(v.HalfZ)}) < 0.01 AND comment = {S(v.Comment)};";
        }

        private static string At(Volume v, string t) =>
            $"{t}.map_context_id = {MapContextId} AND {t}.region_id = {v.RegionId} AND {t}.shape = {Box} " +
            $"AND ABS({t}.pos_x - {N(v.X)}) < 0.01 AND ABS({t}.pos_z - {N(v.Z)}) < 0.01";

        private static string N(double value) => value.ToString("R", CultureInfo.InvariantCulture);

        private static string S(string value) => $"'{value.Replace("'", "''")}'";
    }
}
