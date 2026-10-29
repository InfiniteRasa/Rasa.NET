using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Plateau;

#nullable disable

namespace Rasa.Migrations.SqliteWorld
{
    /// <summary>
    /// Trinity Bridge Logos: Past (mission 1044, Plateau) (PlateauTrinityLogosPastV1). Data only.
    /// </summary>
    public partial class Plateau_trinity_logos_past : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            PlateauTrinityLogosPastV1.Up(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            PlateauTrinityLogosPastV1.Down(migrationBuilder);
        }
    }
}
