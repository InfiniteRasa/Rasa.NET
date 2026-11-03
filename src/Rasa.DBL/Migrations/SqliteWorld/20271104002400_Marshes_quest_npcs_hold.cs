using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Marshes;

#nullable disable

namespace Rasa.Migrations.SqliteWorld
{
    /// <summary>
    /// Valverde Marshes, to-fix row 17: the Marshes quest NPCs get a stat row (4500 hit points, armour 300; they had the
    /// server's default 100), and Elder Kirii stands out of 891's waves (MarshesQuestNpcsHoldV1). Down takes the stat rows
    /// out and puts Kirii and his marker back at the Marshes_npcs_stand_still place. Data only.
    /// </summary>
    public partial class Marshes_quest_npcs_hold : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in MarshesQuestNpcsHoldV1.Up())
                migrationBuilder.Sql(sql);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in MarshesQuestNpcsHoldV1.Down())
                migrationBuilder.Sql(sql);
        }
    }
}
