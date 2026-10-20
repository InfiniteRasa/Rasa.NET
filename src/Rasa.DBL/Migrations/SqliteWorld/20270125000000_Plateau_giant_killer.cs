using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Plateau;

#nullable disable

namespace Rasa.Migrations.SqliteWorld
{
    /// <summary>
    /// Giant Killer (mission 939, Plateau) and two to four Kael guards in Goliath's pool
    /// (PlateauGiantKillerV1). Data only.
    /// </summary>
    public partial class Plateau_giant_killer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            PlateauGiantKillerV1.Up(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            PlateauGiantKillerV1.Down(migrationBuilder);
        }
    }
}
