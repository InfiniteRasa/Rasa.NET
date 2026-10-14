using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.SqliteChar
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
                type: "INTEGER",
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "xp_boost_level",
                table: "character",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<long>(
                name: "xp_boost_remaining_ms",
                table: "character",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);
        }

        /// <summary>
        /// In SQL of its own: EF rebuilds the table to drop a column on SQLite, from the model of
        /// the migration before, which a migration with no model of its own does not give.
        /// SQLite drops a column itself since 3.35.
        /// </summary>
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE character DROP COLUMN xp_boost_remaining_ms;");
            migrationBuilder.Sql("ALTER TABLE character DROP COLUMN xp_boost_level;");
            migrationBuilder.Sql("ALTER TABLE character DROP COLUMN xp_boost_percent;");
        }
    }
}
