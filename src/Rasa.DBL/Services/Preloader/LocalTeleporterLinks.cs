using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Rasa.Services.Preloader
{
    using Structures.World;

    /// <summary>
    /// The local teleporters that are known to be the two ends of one teleporter, linked both ways
    /// (local_teleporter_link): step onto one and you are taken to the other.
    ///
    /// The client has no name for a local teleporter in its waypoint table (waypointlanguage), so
    /// they were never travel-window destinations. What it does have is a map marker of type
    /// LOCAL_TELEPORTER at each (generated.client.uimapmarker), with a label
    /// (uimapmarkertextlanguage). Where the same label is on exactly two markers on a map, they
    /// are a pair, and each marker has a teleporter row of type 1 at it:
    ///  - Torden Abyss (2028), Penumbra Sections 1-5, the rows named A and B of each.
    ///  - The Atta Colony on Torden Plains (1773): Harvest Grotto, Shining Cavern and Royal Nest,
    ///    each two pads at different depths of the colony (29 to 82 m apart in height).
    ///  - Valverde Marshes (1454): the Tower Teleporters of Research Point, one in each of two
    ///    outpost towers 155 m apart, and the Shortcut Teleporters of Research Point.
    ///  - Ashen Desert (1734): Ashoka Upper and Ashoka Lower, two markers with labels of their
    ///    own ("Ashoka Upper Teleporter", "Ashoka Lower Teleporter").
    ///
    /// The rest of the type 1 rows have no link and take nobody anywhere: Thunderhead's base pads
    /// (one label for fourteen markers; ThunderheadBaseTeleporters links ten of them later, by the
    /// signs beside them) and its pads at Larai Outpost, Outpost Aurora and the
    /// Atta Collector; Stonewall's four towers on Ashen Desert; Fithik Hatchery, Bane Detention
    /// Center and Bane Comm Tower in Devil's Den; Falcon Hold's tower teleporter on the Marshes;
    /// and the rows of type 1 that are not local teleporters (Treeback Ridge 64, the
    /// battleground team bases 491 and 492, the two graveyards 595 and 597, and the CELLAR Arena
    /// link's pads 990 and 991). Nothing in the client says where those go. Linking one is a row
    /// of local_teleporter_link each way.
    ///
    /// Ashoka Upper (613) is moved to its marker. It came from a hospital row of Omega Labs
    /// (ClientWaypointIds), and kept that map's position, which on Ashen Desert is the top of a
    /// mesa 21 m above the settlement and 12 m from the marker; the marker stands in a room of
    /// the Brann settlement, on the floor the navmesh has there. Its height is the floor's,
    /// 0.66 m under the marker, as Ashoka Lower's row (541) is under its own.
    ///
    /// Plain SQL, valid on SQLite and MySQL alike: Local_teleporter_links has no target model
    /// (no Designer). The move is guarded on the row as it was, so a row a GM has moved since
    /// is left alone, and Down puts back only what is still as Up left it.
    /// </summary>
    public static class LocalTeleporterLinks
    {
        public const uint AshokaUpper = 613;

        public static readonly (double X, double Y, double Z) AshokaUpperWas = (497.4414, 286.6914, -431.41406);
        public static readonly (double X, double Y, double Z) AshokaUpperIs = (495.26932, 264.5, -443.25302);

        /// <summary>The pairs, as (map, one end, the other end, what the client's markers call them).</summary>
        public static readonly (uint MapContextId, uint A, uint B, string Name)[] Pairs =
        {
            (2028, 551, 560, "Penumbra Section 1"),
            (2028, 554, 559, "Penumbra Section 2"),
            (2028, 552, 557, "Penumbra Section 3"),
            (2028, 553, 558, "Penumbra Section 4"),
            (2028, 555, 556, "Penumbra Section 5"),
            (1773, 561, 563, "Atta Teleporter Harvest Grotto"),
            (1773, 562, 564, "Atta Teleporter Shining Cavern"),
            (1773, 565, 566, "Atta Teleporter Royal Nest"),
            (1454, 571, 573, "Tower Teleporter: Research Point"),
            (1454, 572, 574, "Shortcut Teleporter: Research Point"),
            (1734, 541, AshokaUpper, "Ashoka Lower Teleporter, Ashoka Upper Teleporter")
        };

        /// <summary>Every link, one a way.</summary>
        public static IEnumerable<(uint From, uint To)> Links() =>
            Pairs.SelectMany(pair => new[] { (pair.A, pair.B), (pair.B, pair.A) });

        public static IEnumerable<string> Up()
        {
            yield return Move(AshokaUpperWas, AshokaUpperIs);

            foreach (var (from, to) in Links())
                yield return $"INSERT INTO {LocalTeleporterLinkEntry.TableName} (from_teleporter_id, to_teleporter_id) VALUES ({from}, {to});";
        }

        /// <summary>Ashoka Upper back where it was; the table goes with the migration's DropTable.</summary>
        public static IEnumerable<string> Down()
        {
            yield return Move(AshokaUpperIs, AshokaUpperWas);
        }

        private static string Move((double X, double Y, double Z) from, (double X, double Y, double Z) to) =>
            $"UPDATE teleporter SET pos_x = {N(to.X)}, pos_y = {N(to.Y)}, pos_z = {N(to.Z)} " +
            $"WHERE id = {AshokaUpper} AND map_context_id = 1734 AND type = 1 " +
            $"AND ABS(pos_x - {N(from.X)}) < 0.01 AND ABS(pos_y - {N(from.Y)}) < 0.01 AND ABS(pos_z - {N(from.Z)}) < 0.01;";

        private static string N(double value) => value.ToString("R", CultureInfo.InvariantCulture);
    }
}
