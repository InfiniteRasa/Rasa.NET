using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

using Rasa.Context.World;
using Rasa.Services.Preloader;

namespace Rasa.Migrations.SqliteWorld
{
    /// <summary>
    /// The four human gate force fields moved into the slots their archways have for them
    /// (GateFieldNooks). Data only, in plain SQL: no model of its own.
    ///
    /// Down moves back only the rows that still stand where Up put them.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    [DbContext(typeof(SqliteWorldContext))]
    [Migration("20270925000000_Gate_field_nooks")]
    public class Gate_field_nooks : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in GateFieldNooks.Up())
                migrationBuilder.Sql(sql);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in GateFieldNooks.Down())
                migrationBuilder.Sql(sql);
        }
    }
}
