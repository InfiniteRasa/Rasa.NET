using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Plateau;

#nullable disable

namespace Rasa.Migrations.SqliteWorld
{
    /// <summary>
    /// A Voice From The Past (mission 934, Plateau) (PlateauVoiceFromThePastV1). Data only.
    /// </summary>
    public partial class Plateau_voice_from_the_past : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            PlateauVoiceFromThePastV1.Up(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            PlateauVoiceFromThePastV1.Down(migrationBuilder);
        }
    }
}
