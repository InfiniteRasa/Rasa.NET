using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Plateau;

#nullable disable

namespace Rasa.Migrations.MySqlWorld
{
    /// <summary>
    /// Scavenger Hunt (mission 1015, Plateau): Thrax trophies for the Scrounger's carton of cigarettes
    /// (PlateauScavengerHuntV1). Data only.
    /// </summary>
    public partial class Plateau_scavenger_hunt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            PlateauScavengerHuntV1.Up(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            PlateauScavengerHuntV1.Down(migrationBuilder);
        }
    }
}
