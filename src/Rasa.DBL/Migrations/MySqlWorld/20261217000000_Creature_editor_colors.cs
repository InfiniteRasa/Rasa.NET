using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Context.World;

namespace Rasa.Migrations.MySqlWorld
{
    [DbContext(typeof(MySqlWorldContext))]
    [Migration("20261217000000_Creature_editor_colors")]
    public sealed class Creature_editor_colors : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<uint>(name: "body_hue", table: "creature", type: "int unsigned", nullable: true);
            migrationBuilder.AddColumn<uint>(name: "body_hue2", table: "creature", type: "int unsigned", nullable: true);
            migrationBuilder.AddColumn<uint>(name: "hue2", table: "creature_appearance", type: "int unsigned", nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "hue2", table: "creature_appearance");
            migrationBuilder.DropColumn(name: "body_hue2", table: "creature");
            migrationBuilder.DropColumn(name: "body_hue", table: "creature");
        }
    }
}
