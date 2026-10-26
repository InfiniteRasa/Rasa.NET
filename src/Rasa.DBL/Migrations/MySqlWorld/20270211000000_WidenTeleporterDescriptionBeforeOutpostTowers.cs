using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Context.World;

namespace Rasa.Migrations.MySqlWorld
{
    /// <summary>
    /// Outpost_tower_teleporters inserts human-readable locations longer than the historical
    /// teleporter.description varchar(64). Widen it before those rows are inserted on MySQL.
    /// A rollback must not shrink it and risk truncating existing descriptions.
    /// </summary>
    [DbContext(typeof(MySqlWorldContext))]
    [Migration("20270211000000_WidenTeleporterDescriptionBeforeOutpostTowers")]
    public class WidenTeleporterDescriptionBeforeOutpostTowers : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "description",
                table: "teleporter",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(64)")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Keep TEXT: shrinking could truncate persisted teleporter descriptions.
        }
    }
}
