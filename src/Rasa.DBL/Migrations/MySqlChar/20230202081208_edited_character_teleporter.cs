using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.MySqlChar
{
    public partial class edited_character_teleporter : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte>(
                name: "waypoint_type",
                table: "character_teleporter",
                type: "tinyint unsigned",
                nullable: false,
                defaultValue: (byte)0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "waypoint_type",
                table: "character_teleporter");
        }
    }
}
