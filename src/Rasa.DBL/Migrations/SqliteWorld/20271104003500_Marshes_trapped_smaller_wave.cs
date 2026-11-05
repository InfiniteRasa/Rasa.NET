using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Marshes;

#nullable disable

namespace Rasa.Migrations.SqliteWorld
{
    /// <summary>
    /// Valverde Marshes: Trapped!'s (1052) third wave three Thrax, not five, so one player can hold it
    /// (MarshesTrappedSmallerWaveV1). Down writes the binding back as MarshesTrappedV3 wrote it. Data only.
    /// </summary>
    public partial class Marshes_trapped_smaller_wave : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder) => MarshesTrappedSmallerWaveV1.Up(migrationBuilder);

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) => MarshesTrappedSmallerWaveV1.Down(migrationBuilder);
    }
}
