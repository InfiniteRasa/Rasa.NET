using System.Collections.Generic;

namespace Rasa.Services.Preloader
{
    /// <summary>
    /// Four seeded family names (RandomNamePreloader) carry a double quote where the apostrophe of
    /// O'Brien was: O"Brien, O"Donnell, O"Leary and O"Rourke. The character creation screen handed
    /// them out and creation refused them as a name of letters only (Random_name_quotes).
    ///
    /// Each becomes what the client sends for it once a player accepts it - it lowers the name and
    /// capitalises its first letter - without the quote: Obrien, Odonnell, Oleary, Orourke.
    ///
    /// Guarded: a family-name row changes only from the name it is to change from; none of the
    /// four new names is in the seed. Plain SQL, valid on SQLite and MySQL alike.
    /// </summary>
    public static class RandomNameQuotes
    {
        public static readonly (string Was, string Name)[] Names =
        {
            ("O\"Brien", "Obrien"),
            ("O\"Donnell", "Odonnell"),
            ("O\"Leary", "Oleary"),
            ("O\"Rourke", "Orourke"),
        };

        /// <summary>The statements, one each, as MigrationBuilder.Sql takes them.</summary>
        public static IEnumerable<string> Up()
        {
            foreach (var (was, name) in Names)
                yield return Rename(was, name);
        }

        public static IEnumerable<string> Down()
        {
            foreach (var (was, name) in Names)
                yield return Rename(name, was);
        }

        private static string Rename(string from, string to) =>
            $"UPDATE player_random_name SET name = '{to}' WHERE name = '{from}' AND type = 1;";
    }
}
