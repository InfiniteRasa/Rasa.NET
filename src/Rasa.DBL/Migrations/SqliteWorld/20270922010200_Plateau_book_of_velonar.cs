using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Plateau;

#nullable disable

namespace Rasa.Migrations.SqliteWorld
{
    /// <summary>
    /// The Book Of Velonar (mission 930, Plateau): the book's satchel in the Velon Das ruins is a scene
    /// object of the mission (PlateauBookOfVelonarV1). Data only.
    /// </summary>
    public partial class Plateau_book_of_velonar : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            PlateauBookOfVelonarV1.Up(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            PlateauBookOfVelonarV1.Down(migrationBuilder);
        }
    }
}
