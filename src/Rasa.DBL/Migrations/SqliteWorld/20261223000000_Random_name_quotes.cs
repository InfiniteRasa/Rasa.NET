using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

using Rasa.Context.World;
using Rasa.Services.Preloader;

namespace Rasa.Migrations.SqliteWorld
{
    /// <summary>
    /// The four seeded family names with a double quote in them (O"Brien and the rest), which
    /// character creation refused, spelt as the client sends them without it (RandomNameQuotes).
    /// Data only, in plain SQL: no model of its own.
    ///
    /// Down puts the quoted names back.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    [DbContext(typeof(SqliteWorldContext))]
    [Migration("20261223000000_Random_name_quotes")]
    public class Random_name_quotes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in RandomNameQuotes.Up())
                migrationBuilder.Sql(sql);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in RandomNameQuotes.Down())
                migrationBuilder.Sql(sql);
        }
    }
}
