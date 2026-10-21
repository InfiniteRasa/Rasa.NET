using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Plateau;

#nullable disable

namespace Rasa.Migrations.MySqlWorld
{
    /// <summary>
    /// Blue Flu (mission 1014, Plateau) (PlateauBlueFluV1). Data only.
    /// </summary>
    public partial class Plateau_blue_flu : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            PlateauBlueFluV1.Up(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            PlateauBlueFluV1.Down(migrationBuilder);
        }
    }
}
