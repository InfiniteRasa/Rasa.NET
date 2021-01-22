using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.MySqlChar
{
    public partial class AddCharacterEntryRunState : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte>(
                name: "run_state",
                table: "character",
                type: "tinyint(3) unsigned",
                nullable: false,
                defaultValue: (byte)1);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "run_state",
                table: "character");
        }
    }
}
