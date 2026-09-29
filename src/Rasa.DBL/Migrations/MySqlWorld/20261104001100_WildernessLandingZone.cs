using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Services.Preloader.Missions.Wilderness;

#nullable disable

namespace Rasa.Migrations.MySqlWorld
{
    /// <inheritdoc />
    public partial class WildernessLandingZone : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            WildernessLandingZoneV1.Up(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            WildernessLandingZoneV1.Down(migrationBuilder);
        }
    }
}
