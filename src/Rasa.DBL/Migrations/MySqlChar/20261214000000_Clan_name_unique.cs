using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

namespace Rasa.Migrations.MySqlChar
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
    [DbContext(typeof(MySqlCharContext))]
    [Migration("20261214000000_Clan_name_unique")]
    public partial class Clan_name_unique : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // The name comparison is the column's collation, as the index's is. DISTINCT keeps the
            // derived table from being merged into the UPDATE, which may not read the table it
            // writes.
            migrationBuilder.Sql(
                "UPDATE clan JOIN (SELECT DISTINCT later.id FROM clan AS later " +
                "JOIN clan AS earlier ON earlier.id < later.id AND earlier.name = later.name) AS duplicate ON duplicate.id = clan.id " +
                "SET clan.name = CONCAT(clan.name, ' (', clan.id, ')');");
            migrationBuilder.CreateIndex(
                name: "clan_index_name",
                table: "clan",
                column: "name",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(name: "clan_index_name", table: "clan");
        }
    }
}
