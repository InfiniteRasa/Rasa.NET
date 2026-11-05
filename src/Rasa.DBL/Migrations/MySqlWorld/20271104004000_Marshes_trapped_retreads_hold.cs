using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Marshes;

#nullable disable

namespace Rasa.Migrations.MySqlWorld
{
    /// <summary>
    /// Valverde Marshes: Trapped!'s (1052) Retreads 8000 hit points, not the escort's 4000, so each holds a Thrax while one
    /// player kills a wave (MarshesTrappedRetreadsHoldV1). Down sets the escort's back. Data only.
    /// </summary>
    public partial class Marshes_trapped_retreads_hold : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder) => MarshesTrappedRetreadsHoldV1.Up(migrationBuilder);

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) => MarshesTrappedRetreadsHoldV1.Down(migrationBuilder);
    }
}
