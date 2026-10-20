using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

namespace Rasa.Migrations.SqliteChar
{
    using Context.Char;

    /// <summary>
    /// A unique index on the clan's name, case-blind: on SQLite with NOCASE, on MySQL by the
    /// column's own collation. Before it a name was checked and then written, with nothing in the
    /// database to hold it; and SQLite compared case, so "Wolves" and "wolves" could both be made
    /// there, where MySQL took them for one.
    ///
    /// A clan whose name another, older clan already has in some case is renamed to its name
    /// with " (id)" after it, so the index can be made; its leader can rename it again.
    ///
    /// Down drops the index. The names stay as they are.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    [DbContext(typeof(SqliteCharContext))]
    [Migration("20261214000000_Clan_name_unique")]
    public partial class Clan_name_unique : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // lower() folds A-Z only, as NOCASE does.
            migrationBuilder.Sql(
                "UPDATE clan SET name = name || ' (' || id || ')' " +
                "WHERE EXISTS (SELECT 1 FROM clan AS earlier WHERE earlier.id < clan.id AND lower(earlier.name) = lower(clan.name));");
            migrationBuilder.Sql("CREATE UNIQUE INDEX clan_index_name ON clan (name COLLATE NOCASE);");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX IF EXISTS clan_index_name;");
        }
    }
}
