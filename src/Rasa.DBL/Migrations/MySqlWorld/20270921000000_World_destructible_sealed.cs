using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

using Rasa.Context.World;
using Rasa.Structures.World;

namespace Rasa.Migrations.MySqlWorld
{
    /// <summary>
    /// A force field players cannot shoot down (world_destructible.sealed, PlacedDestructibles):
    /// a mission takes it down (MissionActionKind.TakeDownField). 0, as they were, for the rows
    /// there are.
    ///
    /// Down drops the column.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    [DbContext(typeof(MySqlWorldContext))]
    [Migration("20270921000000_World_destructible_sealed")]
    public class World_destructible_sealed : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "sealed",
                table: WorldDestructibleEntry.TableName,
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "sealed", table: WorldDestructibleEntry.TableName);
        }
    }
}
