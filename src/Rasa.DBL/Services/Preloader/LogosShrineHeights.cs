using System.Collections.Generic;
using System.Globalization;

namespace Rasa.Services.Preloader
{
    using Structures.World;

    /// <summary>
    /// Eight logos shrines put back over their pedestals as the rest stand (Logos_shrine_heights).
    ///
    /// A shrine (UsableItemDispElohLogos*V01) hangs over the pedestal the .map places for it
    /// (ArchElohLogosDispenserBase). Every shrine mesh is the same 1.34 m tablet centred on its
    /// origin, and the pedestal's top is 0.92 m over its own. Of the 171 rows, 141 stand 2.65 to
    /// 2.85 m over their pedestal (2.74 the middle) and within a few centimetres of its centre,
    /// the tablet's bottom 1.15 m over the top, and 18 more at 2.0. These eight did not:
    ///  - Those, in Minos Caverns, 1.26 m under its pedestal: under the floor, out of sight;
    ///  - Future, in Guardian Promontory, and Speed, on Concordia Divide, 0.7 m over theirs:
    ///    half sunk in the pedestal;
    ///  - Star (Concordia Divide), Near (Torden Incline) and Crush (Torden Plains), 0.9 to 1.1 m
    ///    low, sat on the pedestal's top;
    ///  - Ancestor (Valverde Marshes) and Death (Torden Incline), 1.0 and 2.0 m high.
    /// Each is moved over its pedestal's centre, 2.74 m over its origin.
    ///
    /// Plain SQL, valid on SQLite and MySQL alike. A row moves only from where it was, and Down
    /// moves it back only from where Up put it, so a shrine a GM has moved is left where they put it.
    /// </summary>
    public static class LogosShrineHeights
    {
        /// <summary>How high over its pedestal's origin a shrine hangs, as the rows from retail have it.</summary>
        public const double OverPedestal = 2.74;

        public sealed record Shrine(uint Id, uint MapContextId, string Name,
            double WasX, double WasY, double WasZ, double X, double Y, double Z);

        public static readonly Shrine[] Shrines =
        {
            new(321, 1347, "Those", 26.132812, -34.48828, -51.117188, 26.2426, -30.4884, -51.1465),
            new(58, 1416, "Future", 351.67188, 333.73047, 255.91016, 351.7446, 335.74, 256.0084),
            new(54, 1148, "Speed", 140.52734, 141.76172, 411.8164, 140.2109, 143.8191, 411.9413),
            new(68, 1148, "Star", 3.53126, 117.21875, 771.33594, 3.3453, 118.0742, 771.6075),
            new(373, 1761, "Near", -577.7344, 243.9414, -471.58203, -577.8339, 244.9433, -471.6412),
            new(122, 1764, "Crush", -111.35547, 375.92188, -655.3047, -111.5, 377.0239, -655.75),
            new(72, 1454, "Ancestor", 557.5625, 219.98828, -190.35838, 557.5862, 218.9788, -190.3156),
            new(125, 1761, "Death", 1179.9727, 300.75, 384.0, 1180.0, 298.74, 384.0),
        };

        public static IEnumerable<string> Up()
        {
            foreach (var s in Shrines)
                yield return Move(s, s.WasX, s.WasY, s.WasZ, s.X, s.Y, s.Z);
        }

        public static IEnumerable<string> Down()
        {
            foreach (var s in Shrines)
                yield return Move(s, s.X, s.Y, s.Z, s.WasX, s.WasY, s.WasZ);
        }

        private static string Move(Shrine s, double fromX, double fromY, double fromZ, double toX, double toY, double toZ) =>
            $"UPDATE {LogosEntry.TableName} SET pos_x = {N(toX)}, pos_y = {N(toY)}, pos_z = {N(toZ)} " +
            $"WHERE id = {s.Id} AND map_context_id = {s.MapContextId} " +
            $"AND ABS(pos_x - {N(fromX)}) < 0.01 AND ABS(pos_y - {N(fromY)}) < 0.01 AND ABS(pos_z - {N(fromZ)}) < 0.01;";

        private static string N(double value) => value.ToString("R", CultureInfo.InvariantCulture);
    }
}
