using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Services.Preloader.Missions.Wilderness;

#nullable disable

namespace Rasa.Migrations.SqliteWorld
{
    /// <inheritdoc />
    public partial class WildernessHubWorld : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            WildernessHubContactsV1.Up(migrationBuilder);
            WildernessWaveAPopulationV1.Up(migrationBuilder);
            WildernessLateHubContactsV1.Up(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            WildernessLateHubContactsV1.Down(migrationBuilder);
            WildernessWaveAPopulationV1.Down(migrationBuilder);
            WildernessHubContactsV1.Down(migrationBuilder);
        }
    }
}
