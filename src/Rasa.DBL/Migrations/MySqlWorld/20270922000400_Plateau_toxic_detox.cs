using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Plateau;

#nullable disable

namespace Rasa.Migrations.MySqlWorld
{
    /// <summary>
    /// Toxic Detox (mission 1053, Plateau): five Bane Toxic Waste Pumps at the Septic Bog for Locke
    /// (PlateauToxicDetoxV1). Data only.
    /// </summary>
    public partial class Plateau_toxic_detox : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            PlateauToxicDetoxV1.Up(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            PlateauToxicDetoxV1.Down(migrationBuilder);
        }
    }
}
