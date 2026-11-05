using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Marshes;

#nullable disable

namespace Rasa.Migrations.SqliteWorld
{
    /// <summary>
    /// Valverde Marshes: Trapped!'s (1052) crew and waves 25 m west, clear of the depot's Thrax pool 531291, which joined
    /// the defence and came back while it ran (MarshesTrappedClearOfDepotV1); the approach area and the markers move with
    /// the stand-in. Down writes the binding, the area and the markers back as before. Data only.
    /// </summary>
    public partial class Marshes_trapped_clear_of_depot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder) => MarshesTrappedClearOfDepotV1.Up(migrationBuilder);

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) => MarshesTrappedClearOfDepotV1.Down(migrationBuilder);
    }
}
