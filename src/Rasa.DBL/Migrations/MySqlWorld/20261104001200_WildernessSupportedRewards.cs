using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Wilderness;

#nullable disable

namespace Rasa.Migrations.MySqlWorld
{
    /// <inheritdoc />
    public partial class WildernessSupportedRewards : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            WildernessSupportedRewardsV1.Up(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            WildernessSupportedRewardsV1.Down(migrationBuilder);
        }
    }
}
