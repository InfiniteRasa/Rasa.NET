using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Plateau;

#nullable disable

namespace Rasa.Migrations.SqliteWorld
{
    /// <summary>
    /// Coup (878) names Squad Leader Altera's pool as a public-spawn actor (PlateauCoupAlteraRespawnV1, to-fix #55), so
    /// that in a squad's copy of Maligo Base she comes back on her own respawn time after a death, not after the copy's
    /// 45 minutes. Her pool and her two markers move off the ramp's side, where the client has no floor, to the room's
    /// floor (-136.5, -95.7, 139). The mission's scene binding is written again; Down writes it without the actor and puts
    /// her back. Data only.
    /// </summary>
    public partial class Plateau_coup_altera_respawn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder) => PlateauCoupAlteraRespawnV1.Up(migrationBuilder);

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) => PlateauCoupAlteraRespawnV1.Down(migrationBuilder);
    }
}
