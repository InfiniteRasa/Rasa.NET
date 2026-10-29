using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Plateau;

#nullable disable

namespace Rasa.Migrations.SqliteWorld
{
    /// <summary>
    /// Looking For Clues (mission 928, Plateau) (PlateauLookingForCluesV1). Data only.
    /// </summary>
    public partial class Plateau_looking_for_clues : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            PlateauLookingForCluesV1.Up(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            PlateauLookingForCluesV1.Down(migrationBuilder);
        }
    }
}
