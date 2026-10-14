using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.MySqlChar
{
    /// <summary>
    /// An Experience Booster's boost the character left the world with (the game server's
    /// ExperienceBoost, RelogVitals): character.xp_boost_percent, the percent more experience
    /// from kills (0 for none); xp_boost_level, the booster's level; and
    /// xp_boost_remaining_ms, the time it had left. Every character there is starts with none.
    /// </summary>
    public partial class Add_character_xp_boost : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<uint>(
                name: "xp_boost_percent",
                table: "character",
                type: "int unsigned",
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "xp_boost_level",
                table: "character",
                type: "int unsigned",
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<long>(
                name: "xp_boost_remaining_ms",
                table: "character",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "xp_boost_remaining_ms", table: "character");
            migrationBuilder.DropColumn(name: "xp_boost_level", table: "character");
            migrationBuilder.DropColumn(name: "xp_boost_percent", table: "character");
        }
    }
}
