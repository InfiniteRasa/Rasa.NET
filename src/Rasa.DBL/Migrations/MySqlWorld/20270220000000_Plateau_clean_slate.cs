using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Plateau;

#nullable disable

namespace Rasa.Migrations.MySqlWorld
{
    /// <summary>
    /// Clean Slate (mission 1983, Plateau) (PlateauCleanSlateV1). Data only.
    /// </summary>
    public partial class Plateau_clean_slate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            PlateauCleanSlateV1.Up(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            PlateauCleanSlateV1.Down(migrationBuilder);
        }
    }
}
