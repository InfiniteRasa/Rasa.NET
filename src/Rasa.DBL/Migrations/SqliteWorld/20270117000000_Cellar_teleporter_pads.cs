using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

using Rasa.Context.World;
using Rasa.Services.Preloader;

namespace Rasa.Migrations.SqliteWorld
{
    /// <summary>
    /// A human teleporter pad on each of the CELLAR Arena's five links to the battlefields,
    /// teleporters 992 to 996 (CellarTeleporterPads). Data only, in plain SQL: no model of its
    /// own.
    ///
    /// Down takes them out.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    [DbContext(typeof(SqliteWorldContext))]
    [Migration("20270117000000_Cellar_teleporter_pads")]
    public class Cellar_teleporter_pads : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in CellarTeleporterPads.Up())
                migrationBuilder.Sql(sql);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in CellarTeleporterPads.Down())
                migrationBuilder.Sql(sql);
        }
    }
}
