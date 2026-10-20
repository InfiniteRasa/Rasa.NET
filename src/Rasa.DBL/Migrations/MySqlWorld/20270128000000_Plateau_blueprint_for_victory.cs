using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Plateau;

#nullable disable

namespace Rasa.Migrations.MySqlWorld
{
    /// <summary>
    /// Blueprint For Victory (mission 1046, Plateau): the Maligo Base blueprints that a Maligo Elite
    /// Guardmaster can drop, from Petrie to Colonel Bosley (PlateauBlueprintForVictoryV1). Data only.
    /// </summary>
    public partial class Plateau_blueprint_for_victory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            PlateauBlueprintForVictoryV1.Up(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            PlateauBlueprintForVictoryV1.Down(migrationBuilder);
        }
    }
}
