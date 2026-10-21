using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Plateau;

#nullable disable

namespace Rasa.Migrations.SqliteWorld
{
    /// <summary>
    /// Alpha Mox (mission 1027, Plateau) (PlateauAlphaMoxV1). Data only.
    /// </summary>
    public partial class Plateau_alpha_mox : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            PlateauAlphaMoxV1.Up(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            PlateauAlphaMoxV1.Down(migrationBuilder);
        }
    }
}
