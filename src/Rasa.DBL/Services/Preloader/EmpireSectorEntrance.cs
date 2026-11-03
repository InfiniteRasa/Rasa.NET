using System.Collections.Generic;
using System.Globalization;

namespace Rasa.Services.Preloader
{
    /// <summary>The Manhattan entry map marker was on the surface above its lower AFS gate.</summary>
    public static class EmpireSectorEntrance
    {
        // Client gate platform 21057 at (-282.7010,176.99998,-678.64594), local floor Y=8.
        public const double X = -282.7010, Y = 185.0, Z = -678.6459;
        public const double OldX = -285.7884, OldY = 208.9081, OldZ = -706.0602;
        public const uint EntryLink = 16, ExitLink = 132;
        public static IEnumerable<string> Up()
        {
            yield return "CREATE TABLE IF NOT EXISTS empire_sector_entrance_repair (map_link_id INTEGER NOT NULL PRIMARY KEY);";
            foreach (var id in new[] { EntryLink, ExitLink })
            {
                yield return $"INSERT INTO empire_sector_entrance_repair (map_link_id) SELECT id FROM map_link WHERE {Guard(id, false)} " +
                    "AND NOT EXISTS (SELECT 1 FROM empire_sector_entrance_repair r WHERE r.map_link_id=map_link.id);";
                yield return Move(id, true);
            }
        }
        public static IEnumerable<string> Down()
        {
            foreach (var id in new[] { EntryLink, ExitLink }) yield return Move(id, false);
            yield return "DROP TABLE empire_sector_entrance_repair;";
        }
        private static string Move(uint id, bool up)
        {
            var prefix = id == EntryLink ? "dest_pos_" : "pos_";
            return $"UPDATE map_link SET {prefix}x={F(up ? X : OldX)}, {prefix}y={F(up ? Y : OldY)}, {prefix}z={F(up ? Z : OldZ)} " +
                $"WHERE {Guard(id, !up)} AND EXISTS (SELECT 1 FROM empire_sector_entrance_repair r WHERE r.map_link_id=map_link.id);";
        }
        private static string Guard(uint id, bool corrected)
        {
            var prefix = id == EntryLink ? "dest_pos_" : "pos_";
            var map = id == EntryLink ? 1220 : 2327;
            var dest = id == EntryLink ? 2327 : 1220;
            return $"id={id} AND map_context_id={map} AND dest_map_context_id={dest} AND kind=1 " +
                $"AND ABS({prefix}x-({F(corrected ? X : OldX)}))<0.001 " +
                $"AND ABS({prefix}y-({F(corrected ? Y : OldY)}))<0.001 " +
                $"AND ABS({prefix}z-({F(corrected ? Z : OldZ)}))<0.001";
        }
        private static string F(double value) => value.ToString("0.####", CultureInfo.InvariantCulture);
    }
}
