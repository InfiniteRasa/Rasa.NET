using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Plateau;

#nullable disable

namespace Rasa.Migrations.MySqlWorld
{
    /// <summary>
    /// Soyuz Salvage: Plateau (mission 2007): the debris pieces are scene objects of the mission
    /// (PlateauSoyuzSalvageV1). Data only.
    /// </summary>
    public partial class Plateau_soyuz_salvage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            PlateauSoyuzSalvageV1.Up(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            PlateauSoyuzSalvageV1.Down(migrationBuilder);
        }
    }
}
