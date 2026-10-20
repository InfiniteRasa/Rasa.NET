using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

namespace Rasa.Migrations.SqliteChar
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
    [DbContext(typeof(SqliteCharContext))]
    [Migration("20261215000000_Family_name_unique")]
    public partial class Family_name_unique : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // lower() folds A-Z only, as NOCASE does.
            migrationBuilder.Sql(
                "UPDATE account SET family_name = family_name || id " +
                "WHERE family_name <> '' AND EXISTS (SELECT 1 FROM account AS earlier " +
                "WHERE earlier.id < account.id AND lower(earlier.family_name) = lower(account.family_name));");
            migrationBuilder.Sql("CREATE UNIQUE INDEX account_index_family_name ON account (family_name COLLATE NOCASE) WHERE family_name <> '';");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX IF EXISTS account_index_family_name;");
        }
    }
}
