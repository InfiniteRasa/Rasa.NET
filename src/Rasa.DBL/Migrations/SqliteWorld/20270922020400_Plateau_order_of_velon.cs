using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Plateau;

#nullable disable

namespace Rasa.Migrations.SqliteWorld
{
    /// <summary>
    /// The Order Of Velon (mission 1982, Plateau) (PlateauOrderOfVelonV1). Data only.
    /// </summary>
    public partial class Plateau_order_of_velon : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            PlateauOrderOfVelonV1.Up(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            PlateauOrderOfVelonV1.Down(migrationBuilder);
        }
    }
}
