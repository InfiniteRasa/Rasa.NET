using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

using Rasa.Context.World;
using Rasa.Structures.World;

namespace Rasa.Migrations.MySqlWorld
{
    /// <summary>
    /// spawnpool_patrol_group.pace: the pace a pool's group walks its beat at, metres a second;
    /// null for the default stroll (SpawnPoolPatrolGroupEntry.Pace). Every row is left null.
    ///
    /// Down drops the column.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    [DbContext(typeof(MySqlWorldContext))]
    [Migration("20270124100000_Add_patrol_group_pace")]
    public class Add_patrol_group_pace : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "pace",
                table: SpawnPoolPatrolGroupEntry.TableName,
                type: "double",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "pace", table: SpawnPoolPatrolGroupEntry.TableName);
        }
    }
}
