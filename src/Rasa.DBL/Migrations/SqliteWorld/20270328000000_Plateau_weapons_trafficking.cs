using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Plateau;

#nullable disable

namespace Rasa.Migrations.SqliteWorld
{
    /// <summary>
    /// Valverde Pools' Weapons Trafficking (904, PlateauWeaponsTraffickingV1): Amee Corman's five prototypes from the
    /// Lamnas Armory. Data only.
    /// </summary>
    public partial class Plateau_weapons_trafficking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            PlateauWeaponsTraffickingV1.Up(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            PlateauWeaponsTraffickingV1.Down(migrationBuilder);
        }
    }
}
