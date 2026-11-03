using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Marshes;

#nullable disable

namespace Rasa.Migrations.MySqlWorld
{
    /// <summary>
    /// Valverde Marshes, to-fix row 10: Trapped! (1052, set 10) written again with the crew standing only once the player
    /// comes near (MarshesTrappedV2). Down writes the first mission (MarshesTrappedV1) again. Data only.
    /// </summary>
    public partial class Marshes_trapped_approach : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            MarshesTrappedV1.Down(migrationBuilder);
            MarshesTrappedV2.Up(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            MarshesTrappedV2.Down(migrationBuilder);
            MarshesTrappedV1.Up(migrationBuilder);
        }
    }
}
