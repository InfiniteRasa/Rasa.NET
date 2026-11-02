using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

using Rasa.Context.World;
using Rasa.Services.Preloader;

namespace Rasa.Migrations.SqliteWorld
{
    /// <summary>
    /// Warnet Caverns' people, creatures, objects, way in from the Palisades and eight missions
    /// (WarnetCavernsContent, WarnetCavernsMissions). Data only, in plain SQL: no model of its own.
    ///
    /// Down takes the missions out, then the rows, and puts back the turrets' pools it changed.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    [DbContext(typeof(SqliteWorldContext))]
    [Migration("20271014000000_Warnet_caverns")]
    public class Warnet_caverns : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in WarnetCavernsContent.Up())
                migrationBuilder.Sql(sql);

            foreach (var sql in WarnetCavernsMissions.Up())
                migrationBuilder.Sql(sql);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in WarnetCavernsMissions.Down())
                migrationBuilder.Sql(sql);

            foreach (var sql in WarnetCavernsContent.Down())
                migrationBuilder.Sql(sql);
        }
    }
}
