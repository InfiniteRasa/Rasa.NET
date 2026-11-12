using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Marshes;

#nullable disable

namespace Rasa.Migrations.MySqlWorld
{
    /// <summary>
    /// Valverde Marshes: Head East, Young Man's (920) waves 54-62 m out east of the guard station, not 36-43 m, so one player
    /// meets them before they reach it (MarshesGuardStationWavesOutV1). Down writes the binding back as before. Data only.
    /// </summary>
    public partial class Marshes_guard_station_waves_out : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder) => MarshesGuardStationWavesOutV1.Up(migrationBuilder);

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) => MarshesGuardStationWavesOutV1.Down(migrationBuilder);
    }
}
