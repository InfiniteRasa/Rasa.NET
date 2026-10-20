using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

namespace Rasa.Migrations.MySqlChar
{
    using Context.Char;

    /// <summary>
    /// A unique index on the account's family name, case-blind, leaving out the empty name of an
    /// account with no characters yet: on SQLite a partial index with NOCASE, on MySQL a
    /// functional index on NULLIF(family_name, '') by the column's own collation, since MySQL has
    /// no partial index and takes any number of NULLs in a unique one. Before it a name was
    /// checked and then written, with nothing in the database to hold it.
    ///
    /// An account whose family name another, older account already has in some case is given
    /// its name with its id after it, so the index can be made; a GM can rename it again.
    ///
    /// Down drops the index. The names stay as they are.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    [DbContext(typeof(MySqlCharContext))]
    [Migration("20261215000000_Family_name_unique")]
    public partial class Family_name_unique : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // The name comparison is the column's collation, as the index's is. DISTINCT keeps the
            // derived table from being merged into the UPDATE, which may not read the table it
            // writes.
            migrationBuilder.Sql(
                "UPDATE `account` JOIN (SELECT DISTINCT later.id FROM `account` AS later " +
                "JOIN `account` AS earlier ON earlier.id < later.id AND earlier.family_name = later.family_name " +
                "WHERE later.family_name <> '') AS duplicate ON duplicate.id = `account`.id " +
                "SET `account`.family_name = CONCAT(`account`.family_name, `account`.id);");
            migrationBuilder.Sql("CREATE UNIQUE INDEX account_index_family_name ON `account` ((NULLIF(family_name, '')));");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX account_index_family_name ON `account`;");
        }
    }
}
