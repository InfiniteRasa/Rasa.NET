using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Plateau;

#nullable disable

namespace Rasa.Migrations.SqliteWorld
{
    /// <summary>
    /// Bomb Voyage (mission 931, Plateau) (PlateauBombVoyageV1). Data only.
    /// </summary>
    public partial class Plateau_bomb_voyage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            PlateauBombVoyageV1.Up(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            PlateauBombVoyageV1.Down(migrationBuilder);
        }
    }
}
