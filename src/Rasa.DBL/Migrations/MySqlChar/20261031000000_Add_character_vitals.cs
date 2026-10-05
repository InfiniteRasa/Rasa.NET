using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.MySqlChar
{
    /// <summary>
    /// What a character left the world with, so that logging out and back in is no longer a free
    /// heal or a way out of a death's penalty: current health, armour and power (-1 when none was
    /// saved, which loads on full - every existing character, and one that has not left the world
    /// since), and the Rez Trauma and no-healing a revive leaves - Rez Trauma's stack count and
    /// when each wears off, in Unix milliseconds (0 for none).
    /// </summary>
    public partial class Add_character_vitals : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "current_health",
                table: "character",
                type: "int",
                nullable: false,
                defaultValue: -1);

            migrationBuilder.AddColumn<int>(
                name: "current_armor",
                table: "character",
                type: "int",
                nullable: false,
                defaultValue: -1);

            migrationBuilder.AddColumn<int>(
                name: "current_power",
                table: "character",
                type: "int",
                nullable: false,
                defaultValue: -1);

            migrationBuilder.AddColumn<uint>(
                name: "rez_trauma_stacks",
                table: "character",
                type: "int unsigned",
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<long>(
                name: "rez_trauma_ends_at",
                table: "character",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "no_heal_ends_at",
                table: "character",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "no_heal_ends_at", table: "character");
            migrationBuilder.DropColumn(name: "rez_trauma_ends_at", table: "character");
            migrationBuilder.DropColumn(name: "rez_trauma_stacks", table: "character");
            migrationBuilder.DropColumn(name: "current_power", table: "character");
            migrationBuilder.DropColumn(name: "current_armor", table: "character");
            migrationBuilder.DropColumn(name: "current_health", table: "character");
        }
    }
}
