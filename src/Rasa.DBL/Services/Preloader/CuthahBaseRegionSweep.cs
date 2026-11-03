using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Rasa.Services.Preloader
{
    /// <summary>
    /// The region sweep's volumes on Cuthah Base (adv_foreas_howlingmaw_cuthahbase, 2162;
    /// 20270720010000_Cuthah_base_region_sweep).
    ///
    /// Cuthah Command Base (2162) is a three-level Bane base under an outdoor courtyard. Level 1 floors
    /// are y 164-187, Level 2 floors y 140-164 and the Sublevel y 104-131. The navmesh flags all
    /// interiors as underground. The minimap rects for Level 2, Sublevel and the courtyard (region 33)
    /// had no height limit. Because of that, Level 2 (priority 10) named the whole of Level 1 above it,
    /// the courtyard's outdoor sky, music and ambient fell on the Service Sector, Rat Hole and entrance
    /// tunnels below it, and Sublevel took the east edge of the Viral Engineering room. All three rects
    /// are recut to their own height band.
    ///
    /// Recut Level 2 (row 342): x -314..280, z -451..-9, y 130..169. Recut courtyard region 33 (row
    /// 344): y &gt;= 190, so the goo-lake courtyard, tower and landing pad (y 203-214) keep it. Recut
    /// Sublevel (row 343): y &lt;= 136.
    ///
    /// Region 31 (Cuthah Command Base, interior sky + tunnel env, priority 4 above the courtyard) goes
    /// on the arch_bane_industrial_tunnel_* entrance run. That run leads from the courtyard ramp mouth
    /// at (5,212,-532) down to y 181, then east via the turn and ramp_02 to the Level 1 chunnel at x
    /// 117. Boxes are underground-only.
    ///
    /// Service Sector covers the service_* maintenance tunnels (label at 3.7,172.8,-448.4; mission
    /// 1620).
    ///
    /// AFS Rat Hole covers the y164 service rooms at x -19..29, z -543..-505. Its POI and hospital are
    /// there, and its music is the AFS safe-zone set.
    ///
    /// Barracks Sector covers the two sleep-pod chunnel halls (x 100..198, z -357..-235, y172) with the
    /// Barracks POI.
    ///
    /// Security Checkpoint covers the enclosed overlook booth (POI at y181) on the walkway between the
    /// Level 1 hall and the Interchange, and the z -288 chunnel ring around it.
    ///
    /// Interchange is room_control_24m_v07, where its POI is.
    ///
    /// Three tunnel_ramp_01_32m ramps drop from the Interchange to Level 2. The west one leads to
    /// Bioresearch, the middle one to the Command tunnel loop and the east one to Intelligence. They
    /// become the Bioresearch, Command and Intelligence Wing Stairwells, and each stairwell's priority
    /// sits right above its wing's. The ramp from the Level 2 tunnel cross down to y108 is the Sublevel
    /// Stairwell.
    ///
    /// Bioresearch Wing is the whole west part of Level 2 (room v09 with its label, the prison, the
    /// west service tunnels, the round lab). The Viral Engineering Division is the round lab with its
    /// POI. The Viral Experimentation Ward is the y150 extraction booth above the prison, with its POI.
    ///
    /// Command Wing is the centre of Level 2 (tunnel loop, tunnel south, control block, y148 walkways).
    /// Mission 15425 puts Creelig in the Command Wing. Command Control is room_control_24m_v05, the
    /// only room with command monitors. Creelig's Quarters is the east pod with six human beds and
    /// dressers. Meslam (medium) is the matching west pod with human workstations. It is the one
    /// remaining room, and its priority is right above the Quarters.
    ///
    /// Intelligence Center is the central v08 room. Mission 1623 names Central, Valverde and Concordia
    /// mainframes, matching the centre and two side rooms. The existing Intelligence Wing box covered
    /// only that room, so it is recut (row 20241) to the whole east wing at y 135..169.
    ///
    /// Thermal Energy Conversion Plant is the reactor room. Its label is there and it uses the same
    /// cave sky as the Magma Reservoir.
    ///
    /// Magma Reservoir's surface-only circle is left alone, because the reservoir navmesh is not
    /// flagged underground (checked), so the circle applies.
    ///
    /// Not placed: Concordia and Valverde Intelligence Divisions (nothing tells which v13 room is
    /// which), and region 2 (Molder.sky).
    ///
    /// Plain SQL, valid on SQLite and MySQL alike. A new row goes in only where the map is loaded and
    /// no box of that region is at that spot; a recut row changes only while it is as it was. Down puts
    /// the recut rows back and takes the new ones out.
    /// </summary>
    public static class CuthahBaseRegionSweep
    {
        public sealed record Volume(uint RegionId, double X, double Y, double Z, double HalfX, double HalfZ, double MinY, double MaxY, byte Underground, string Comment);

        /// <summary>An existing row as it was and the box it becomes.</summary>
        public sealed record Recut(uint RowId, uint RegionId, byte Shape, double X, double Y, double Z, double Radius, double HalfX, double HalfZ, double MinY, double MaxY,
            byte Underground, string Comment, Volume Box);

        public const uint MapContextId = 2162;
        public const byte Box = 2, AnyLevel = 0, UndergroundOnly = 1, SurfaceOnly = 2;

        private const string Map = "foreas_howlingmaw_cuthahbase - ";

        public static readonly Volume[] Volumes =
        {
            Of(31, -15, 70, -582, -538, 176, 200, UndergroundOnly, Map + "region 31 (entrance tunnel, west run)"),
            Of(31, -11, 21, -553, -530, 176, 214, UndergroundOnly, Map + "region 31 (entrance ramp from courtyard)"),
            Of(31, 69, 126, -550, -528, 172, 200, UndergroundOnly, Map + "region 31 (entrance tunnel, east turn)"),
            Of(31, 92, 126, -528, -474, 168, 200, UndergroundOnly, Map + "region 31 (entrance tunnel ramp to L1)"),
            Of(11, -53, 92, -520, -393, 166, 189, AnyLevel, Map + "Level 1: Service Sector (maintenance tunnels)"),
            Of(11, 85, 103, -475, -455, 166, 189, AnyLevel, Map + "Level 1: Service Sector (east service link)"),
            Of(9, -20, 30, -544, -503, 160, 176, AnyLevel, Map + "AFS Rat Hole (y164 service rooms, AFS hideout)"),
            Of(7, 100, 199, -358, -234, 170, 200, AnyLevel, Map + "Level 1: Barracks Sector (sleep-pod chunnel halls)"),
            Of(10, -89, 99, -328, -240, 170, 200, AnyLevel, Map + "Level 1: Security Checkpoint (booth and ring)"),
            Of(8, -28, 38, -240, -152, 168, 200, AnyLevel, Map + "Level 1: Cuthah Base Interchange (hub room)"),
            Of(14, -301, -91, -320, -28, 135, 175, AnyLevel, Map + "Level 2: Bioresearch Wing (west wing of level 2)"),
            Of(27, -60, -27, -215, -184, 138, 190, AnyLevel, Map + "Bioresearch Wing Stairwell (west tunnel ramp)"),
            Of(15, -204, -170, -293, -262, 146, 165, AnyLevel, Map + "Viral Experimentation Ward (extraction booth)"),
            Of(22, -254, -120, -184, -28, 135, 185, AnyLevel, Map + "Viral Engineering Division (round lab room)"),
            Of(16, -33, 43, -216, -88, 136, 160, AnyLevel, Map + "Level 2: Command Wing (tunnel loop)"),
            Of(16, -1, 11, -264, -245, 136, 160, AnyLevel, Map + "Level 2: Command Wing (tunnel to control)"),
            Of(16, -52, 62, -337, -262, 136, 169, AnyLevel, Map + "Level 2: Command Wing (control room block)"),
            Of(16, -92, 94, -282, -262, 144, 160, AnyLevel, Map + "Level 2: Command Wing (y148 walkways)"),
            Of(28, -10, 21, -154, -121, 138, 190, AnyLevel, Map + "Command Wing Stairwell (middle tunnel ramp)"),
            Of(32, -35, 45, -330, -263, 136, 169, AnyLevel, Map + "Command Control (command-console room)"),
            Of(17, 45, 62, -313, -280, 145, 165, AnyLevel, Map + "Creelig's Quarters (east pod with human beds)"),
            Of(19, -52, -35, -313, -280, 145, 165, AnyLevel, Map + "Meslam (west pod with human workstations)"),
            Of(21, 109, 189, -368, -295, 136, 169, AnyLevel, Map + "Intelligence Center (central v08 room)"),
            Of(29, 37, 70, -216, -185, 138, 190, AnyLevel, Map + "Intelligence Wing Stairwell (east tunnel ramp)"),
            Of(30, -11, 21, -90, -55, 100, 150, AnyLevel, Map + "Sublevel Stairwell (ramp from level 2 cross)"),
            Of(25, -62, 70, 0, 130, 95, 152, AnyLevel, Map + "Thermal Energy Conversion Plant (reactor room)"),
        };

        public static readonly Recut[] Recuts =
        {
            new(20241, 20, 2, 149, 139.6, -331.8, 0, 40.2, 36.4, 138.5, 169.1, 0, "foreas_howlingmaw_cuthahbase - Level 2: Intelligence Wing (room)",
                Of(20, 100, 263, -442, -218, 135, 169, AnyLevel, Map + "Level 2: Intelligence Wing (east wing of level 2)")),
            new(342, 12, 2, -17, 204.6, -230, 0, 296.5, 221, -2000, 2000, 0, "foreas_howlingmaw_cuthahbase - Cuthah Command Base: Level 2",
                Of(12, -314, 280, -451, -9, 130, 169, AnyLevel, Map + "Cuthah Command Base: Level 2 (level 2 only)")),
            new(344, 33, 2, 5, 204.6, -477, 0, 121, 118, -2000, 2000, 0, "foreas_howlingmaw_cuthahbase - Cuthah Command Base",
                Of(33, -116, 126, -595, -359, 190, 400, AnyLevel, Map + "Cuthah Command Base (outdoor courtyard)")),
            new(343, 24, 2, 5, 114, 185, 0, 154, 276, -2000, 2000, 0, "foreas_howlingmaw_cuthahbase - Cuthah Command Base: Sublevel",
                Of(24, -149, 159, -91, 461, -2000, 136, AnyLevel, Map + "Cuthah Command Base: Sublevel (sublevel only)")),
        };

        private static Volume Of(uint region, double west, double east, double south, double north, double floor, double roof, byte underground, string comment) =>
            new(region, (west + east) / 2, floor, (south + north) / 2, (east - west) / 2, (north - south) / 2, floor, roof, underground, comment);

        public static IEnumerable<string> Up()
        {
            foreach (var v in Volumes)
                yield return "INSERT INTO map_region (map_context_id, region_id, shape, pos_x, pos_y, pos_z, radius, half_x, half_z, min_y, max_y, underground, enabled, comment) " +
                    $"SELECT {MapContextId}, {v.RegionId}, {Box}, {N(v.X)}, {N(v.Y)}, {N(v.Z)}, 0, {N(v.HalfX)}, {N(v.HalfZ)}, {N(v.MinY)}, {N(v.MaxY)}, {v.Underground}, 1, {S(v.Comment)} " +
                    $"FROM map_info m WHERE m.map_context_id = {MapContextId} AND NOT EXISTS (SELECT 1 FROM map_region x WHERE {At(v, "x")});";

            foreach (var r in Recuts)
                yield return $"UPDATE map_region SET shape = {Box}, pos_x = {N(r.Box.X)}, pos_y = {N(r.Box.Y)}, pos_z = {N(r.Box.Z)}, radius = 0, " +
                    $"half_x = {N(r.Box.HalfX)}, half_z = {N(r.Box.HalfZ)}, min_y = {N(r.Box.MinY)}, max_y = {N(r.Box.MaxY)}, underground = {r.Box.Underground}, " +
                    $"comment = {S(r.Box.Comment)} " +
                    $"WHERE id = {r.RowId} AND map_context_id = {MapContextId} AND region_id = {r.RegionId} AND shape = {r.Shape} " +
                    $"AND ABS(pos_x - {N(r.X)}) < 0.01 AND ABS(pos_z - {N(r.Z)}) < 0.01 AND ABS(radius - {N(r.Radius)}) < 0.01 " +
                    $"AND ABS(half_x - {N(r.HalfX)}) < 0.01 AND ABS(half_z - {N(r.HalfZ)}) < 0.01 " +
                    $"AND ABS(min_y - {N(r.MinY)}) < 0.01 AND ABS(max_y - {N(r.MaxY)}) < 0.01 AND underground = {r.Underground};";
        }

        public static IEnumerable<string> Down()
        {
            foreach (var r in Recuts.Reverse())
                yield return $"UPDATE map_region SET shape = {r.Shape}, pos_x = {N(r.X)}, pos_y = {N(r.Y)}, pos_z = {N(r.Z)}, radius = {N(r.Radius)}, " +
                    $"half_x = {N(r.HalfX)}, half_z = {N(r.HalfZ)}, min_y = {N(r.MinY)}, max_y = {N(r.MaxY)}, underground = {r.Underground}, comment = {S(r.Comment)} " +
                    $"WHERE id = {r.RowId} AND {Is(r.Box)};";

            foreach (var v in Volumes.Reverse())
                yield return $"DELETE FROM map_region WHERE {Is(v)};";
        }

        private static string Is(Volume v) =>
            $"{At(v, "map_region")} AND ABS(half_x - {N(v.HalfX)}) < 0.01 AND ABS(half_z - {N(v.HalfZ)}) < 0.01 " +
            $"AND ABS(min_y - {N(v.MinY)}) < 0.01 AND ABS(max_y - {N(v.MaxY)}) < 0.01 AND underground = {v.Underground} AND comment = {S(v.Comment)}";

        private static string At(Volume v, string t) =>
            $"{t}.map_context_id = {MapContextId} AND {t}.region_id = {v.RegionId} AND {t}.shape = {Box} " +
            $"AND ABS({t}.pos_x - {N(v.X)}) < 0.01 AND ABS({t}.pos_z - {N(v.Z)}) < 0.01";

        private static string N(double value) => value.ToString("R", CultureInfo.InvariantCulture);

        private static string S(string value) => $"'{value.Replace("'", "''")}'";
    }
}
