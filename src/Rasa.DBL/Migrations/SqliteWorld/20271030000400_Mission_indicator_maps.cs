using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

using Rasa.Context.World;
using Rasa.Services.Preloader.Missions;
using Rasa.Structures.World;

namespace Rasa.Migrations.SqliteWorld
{
    /// <summary>
    /// The map each mission indicator is on (mission_indicator.map_context_id, 0 = every map), so that a player is
    /// sent only the markers of the map it is on (MissionIndicatorMapsV1).
    ///
    /// Down drops the column.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    [DbContext(typeof(SqliteWorldContext))]
    [Migration("20271030000400_Mission_indicator_maps")]
    public class Mission_indicator_maps : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<uint>(
                name: "map_context_id",
                table: MissionIndicatorEntry.TableName,
                type: "int(11)",
                nullable: false,
                defaultValue: 0u);
            foreach (var sql in MissionIndicatorMapsV1.Up())
                migrationBuilder.Sql(sql);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // ALTER TABLE ... DROP COLUMN (SQLite 3.35): EF's DropColumn rebuilds the table from a
            // target model, which these migrations have not got.
            migrationBuilder.Sql($"ALTER TABLE {MissionIndicatorEntry.TableName} DROP COLUMN map_context_id;");
        }
    }
}
