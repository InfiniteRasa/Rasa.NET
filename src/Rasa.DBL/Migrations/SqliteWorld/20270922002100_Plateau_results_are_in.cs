using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Plateau;

#nullable disable

namespace Rasa.Migrations.SqliteWorld
{
    /// <summary>
    /// The Results Are In (mission 936, Plateau) (PlateauResultsAreInV1). Data only.
    /// </summary>
    public partial class Plateau_results_are_in : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            PlateauResultsAreInV1.Up(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            PlateauResultsAreInV1.Down(migrationBuilder);
        }
    }
}
