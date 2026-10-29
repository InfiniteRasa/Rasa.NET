using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Plateau;

#nullable disable

namespace Rasa.Migrations.SqliteWorld
{
    /// <summary>
    /// Amee Corman's Retreads to Retreads (1751, PlateauRetreadsToRetreadsV1): Retread Vincent in Valverde Marshes. Data only.
    /// </summary>
    public partial class Plateau_retreads_to_retreads : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            PlateauRetreadsToRetreadsV1.Up(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            PlateauRetreadsToRetreadsV1.Down(migrationBuilder);
        }
    }
}
