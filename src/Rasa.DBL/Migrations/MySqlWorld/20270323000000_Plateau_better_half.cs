using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Plateau;

#nullable disable

namespace Rasa.Migrations.MySqlWorld
{
    /// <summary>
    /// The Better Half (1975, PlateauBetterHalfV1): Maxxum Quarsh and Tala Clark, and the mission. Data only.
    /// </summary>
    public partial class Plateau_better_half : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder) => PlateauBetterHalfV1.Up(migrationBuilder);

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) => PlateauBetterHalfV1.Down(migrationBuilder);
    }
}
