using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Plateau;

#nullable disable

namespace Rasa.Migrations.MySqlWorld
{
    /// <summary>
    /// Murder Most Foul (mission 895, Plateau) (PlateauMurderMostFoulV1). Data only.
    /// </summary>
    public partial class Plateau_murder_most_foul : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            PlateauMurderMostFoulV1.Up(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            PlateauMurderMostFoulV1.Down(migrationBuilder);
        }
    }
}
