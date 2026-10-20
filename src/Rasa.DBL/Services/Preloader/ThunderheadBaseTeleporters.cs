using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Rasa.Services.Preloader
{
    using Structures.World;

    /// <summary>
    /// Thunderhead Base's local teleporters, linked as the base's own signs say
    /// (Thunderhead_base_teleporters).
    ///
    /// adv_arieki_ligo_thunderhead.map places ten human teleporter pads (UsableTwoStateHumTeleporter)
    /// in the base, and the client marks each with a LOCAL_TELEPORTER marker, all with the one
    /// label "Local Teleporter: Thunderhead Base", so the markers do not say which goes where
    /// (LocalTeleporterLinks left them unlinked). Six had teleporter rows (542 to 545, 547, 548);
    /// the four on the floor at 383 m had none.
    ///
    /// What does say is the holosign the map stands by each pad, 3 to 5 m from it, naming where it
    /// goes. The base has three places with two pads each, and each place's two signs name the
    /// other two places:
    ///  - by the dropship (542, 543), Hospital and HQ;
    ///  - by the hospital (547, 548), Frontlines and HQ;
    ///  - on the HQ's floor at 383 m (two new rows), Hospital and Frontlines.
    /// So the dropship's place is the Frontlines, and the three places are linked in a ring:
    /// Frontlines-Hospital (542, 547), Frontlines-HQ (543 and the HQ pad signed Frontlines) and
    /// Hospital-HQ (548 and the HQ pad signed Hospital). The other two pads at 383 m have no
    /// sign; each stands 4 m across from one of the two pads on the ground under it (544, 545),
    /// both signed HQ: the lifts up to the HQ and back down.
    ///
    /// The four new rows (980 to 983) are on their pads, at the height over the pad the others
    /// are (0.1836 m), of the class the others are (ArchHumTeleporterV02), type 1 and facing 0 as
    /// every local teleporter row is. Thunderhead's other base marker rows (546 and three
    /// markers with neither pad nor row) stand on wall tops with no pad in the map and no sign,
    /// and are left as they are.
    ///
    /// Plain SQL, valid on SQLite and MySQL alike. A row goes in only where its id is free, a link
    /// only where its pad has none and both its ends are there; Down takes out the links and rows
    /// only as Up put them.
    /// </summary>
    public static class ThunderheadBaseTeleporters
    {
        public const uint MapContextId = 1911;
        public const uint TeleporterClass = 29648;          // ArchHumTeleporterV02, as 542 to 548
        public const uint LocalTeleporterType = 1;
        public const double PadHeight = 0.1836;
        public const string Description = "Local Teleporter: Thunderhead Base";

        /// <summary>The HQ's floor pads, the two lifts' and the two signed ones.</summary>
        public const uint HqLiftWest = 980, HqLiftEast = 981, HqToFrontlines = 982, HqToHospital = 983;

        public sealed record Pad(uint Id, double X, double Y, double Z);

        /// <summary>The four pads at 383 m that had no row: the map's pad position, and the pad's height plus <see cref="PadHeight"/>.</summary>
        public static readonly Pad[] Pads =
        {
            new(HqLiftWest, 640.3850708007812, 382.9999694824219 + PadHeight, -509.5959777832031),
            new(HqLiftEast, 653.4601440429688, 382.9999694824219 + PadHeight, -496.35986328125),
            new(HqToFrontlines, 659.4149169921875, 382.9999694824219 + PadHeight, -490.4725036621094),
            new(HqToHospital, 634.4654541015625, 382.9999694824219 + PadHeight, -515.3790893554688),
        };

        /// <summary>The pairs, as (one end, the other end, what they are).</summary>
        public static readonly (uint A, uint B, string Name)[] Pairs =
        {
            (542, 547, "Frontlines - Hospital"),
            (543, HqToFrontlines, "Frontlines - HQ"),
            (548, HqToHospital, "Hospital - HQ"),
            (544, HqLiftWest, "lift up to the HQ, west"),
            (545, HqLiftEast, "lift up to the HQ, east"),
        };

        /// <summary>Every link, one a way.</summary>
        public static IEnumerable<(uint From, uint To)> Links() =>
            Pairs.SelectMany(pair => new[] { (pair.A, pair.B), (pair.B, pair.A) });

        public static IEnumerable<string> Up()
        {
            foreach (var p in Pads)
                yield return "INSERT INTO teleporter (id, class_id, type, description, pos_x, pos_y, pos_z, rotation, map_context_id) " +
                    $"SELECT {p.Id}, {TeleporterClass}, {LocalTeleporterType}, '{Description}', {N(p.X)}, {N(p.Y)}, {N(p.Z)}, 0, {MapContextId} " +
                    $"FROM entityclass e WHERE e.id = {TeleporterClass} AND NOT EXISTS (SELECT 1 FROM teleporter x WHERE x.id = {p.Id});";

            foreach (var (from, to) in Links())
                yield return $"INSERT INTO {LocalTeleporterLinkEntry.TableName} (from_teleporter_id, to_teleporter_id) " +
                    $"SELECT {from}, {to} FROM teleporter a, teleporter b " +
                    $"WHERE a.id = {from} AND a.type = {LocalTeleporterType} AND a.map_context_id = {MapContextId} " +
                    $"AND b.id = {to} AND b.type = {LocalTeleporterType} AND b.map_context_id = {MapContextId} " +
                    $"AND NOT EXISTS (SELECT 1 FROM {LocalTeleporterLinkEntry.TableName} x WHERE x.from_teleporter_id = {from});";
        }

        public static IEnumerable<string> Down()
        {
            foreach (var (from, to) in Links())
                yield return $"DELETE FROM {LocalTeleporterLinkEntry.TableName} WHERE from_teleporter_id = {from} AND to_teleporter_id = {to};";

            foreach (var p in Pads)
                yield return $"DELETE FROM teleporter WHERE id = {p.Id} AND class_id = {TeleporterClass} AND type = {LocalTeleporterType} " +
                    $"AND map_context_id = {MapContextId} AND ABS(pos_x - {N(p.X)}) < 0.01 AND ABS(pos_z - {N(p.Z)}) < 0.01;";
        }

        private static string N(double value) => value.ToString("R", CultureInfo.InvariantCulture);
    }
}
