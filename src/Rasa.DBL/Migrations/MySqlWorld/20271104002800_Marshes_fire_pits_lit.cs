using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Marshes;

#nullable disable

namespace Rasa.Migrations.MySqlWorld
{
    /// <summary>
    /// Valverde Marshes, to-fix row 23: Light the Fires' pits start cold (state 56) and a use lights them (55, the state the
    /// client shows the class's fire in) (MarshesLightTheFiresV2 in place of MarshesLightTheFiresV1). Down puts back V1.
    /// Data only.
    /// </summary>
    public partial class Marshes_fire_pits_lit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            MarshesLightTheFiresV1.Down(migrationBuilder);
            MarshesLightTheFiresV2.Up(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            MarshesLightTheFiresV2.Down(migrationBuilder);
            MarshesLightTheFiresV1.Up(migrationBuilder);
        }
    }
}
