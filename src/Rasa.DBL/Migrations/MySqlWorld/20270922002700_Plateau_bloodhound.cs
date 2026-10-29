using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Plateau;

#nullable disable

namespace Rasa.Migrations.MySqlWorld
{
    /// <summary>
    /// The Stainless Steel Bloodhound (mission 867, Plateau) (PlateauBloodhoundV1). Data only.
    /// </summary>
    public partial class Plateau_bloodhound : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            PlateauBloodhoundV1.Up(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            PlateauBloodhoundV1.Down(migrationBuilder);
        }
    }
}
