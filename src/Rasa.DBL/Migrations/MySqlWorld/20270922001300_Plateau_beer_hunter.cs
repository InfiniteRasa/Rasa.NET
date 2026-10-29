using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Plateau;

#nullable disable

namespace Rasa.Migrations.MySqlWorld
{
    /// <summary>
    /// The Beer Hunter (mission 855, Plateau) with its Saccharo Plants (PlateauBeerHunterV1). Data only.
    /// </summary>
    public partial class Plateau_beer_hunter : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            PlateauBeerHunterV1.Up(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            PlateauBeerHunterV1.Down(migrationBuilder);
        }
    }
}
