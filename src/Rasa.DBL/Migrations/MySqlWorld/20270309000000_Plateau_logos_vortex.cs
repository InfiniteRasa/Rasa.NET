using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Plateau;

#nullable disable

namespace Rasa.Migrations.MySqlWorld
{
    /// <summary>
    /// Logos: Vortex (mission 1659) (PlateauLogosVortexV1). Data only.
    /// </summary>
    public partial class Plateau_logos_vortex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            PlateauLogosVortexV1.Up(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            PlateauLogosVortexV1.Down(migrationBuilder);
        }
    }
}
