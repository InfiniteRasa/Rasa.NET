using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

using Rasa.Context.World;
using Rasa.Structures.World;

namespace Rasa.Migrations.SqliteWorld
{
    /// <summary>
    /// spawnpool_patrol_group.spacing: how far to each side of its leader a pool's group walks,
    /// metres, the ranks behind scaled with it; null for the default 3 m
    /// (SpawnPoolPatrolGroupEntry.Spacing). Every row is left null.
    ///
    /// Down drops the column.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    [DbContext(typeof(SqliteWorldContext))]
    [Migration("20270124200000_Add_patrol_group_spacing")]
    public class Add_patrol_group_spacing : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "spacing",
                table: SpawnPoolPatrolGroupEntry.TableName,
                type: "REAL",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Plain SQL: SQLite drops a column only by a table rebuild, which needs a target model.
            migrationBuilder.Sql($"ALTER TABLE {SpawnPoolPatrolGroupEntry.TableName} DROP COLUMN spacing;");
        }
    }
}
