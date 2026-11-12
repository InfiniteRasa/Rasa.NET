using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Marshes;

#nullable disable

namespace Rasa.Migrations.SqliteWorld
{
    /// <summary>
    /// Valverde Marshes: Head East, Young Man's (920) guard station held by one player: the two soldiers stand still, Shaw's
    /// stand-in and the soldiers have more hit points, the waves spare Shaw while a soldier stands, and wave 1 has three
    /// Thrax (MarshesGuardStationHoldsV1). Down puts back the speeds, the escort's hit points and the binding as it was.
    /// Data only.
    /// </summary>
    public partial class Marshes_guard_station_holds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder) => MarshesGuardStationHoldsV1.Up(migrationBuilder);

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) => MarshesGuardStationHoldsV1.Down(migrationBuilder);
    }
}
