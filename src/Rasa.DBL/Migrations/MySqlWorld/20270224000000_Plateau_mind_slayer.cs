using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Plateau;

#nullable disable

namespace Rasa.Migrations.MySqlWorld
{
    /// <summary>
    /// Mind Slayer (mission 719, Plateau) (PlateauMindSlayerV1). Data only.
    /// </summary>
    public partial class Plateau_mind_slayer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            PlateauMindSlayerV1.Up(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            PlateauMindSlayerV1.Down(migrationBuilder);
        }
    }
}
