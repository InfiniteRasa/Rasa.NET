using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Marshes;

#nullable disable

namespace Rasa.Migrations.MySqlWorld
{
    /// <summary>
    /// Valverde Marshes: Trapped!'s (1052) first wave two Thrax, its second three and 100 s after the talk, so one player can
    /// hold them (MarshesTrappedLighterWavesV1). Down writes the binding back as MarshesTrappedSmallerWaveV1 wrote it. Data
    /// only.
    /// </summary>
    public partial class Marshes_trapped_lighter_waves : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder) => MarshesTrappedLighterWavesV1.Up(migrationBuilder);

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) => MarshesTrappedLighterWavesV1.Down(migrationBuilder);
    }
}
