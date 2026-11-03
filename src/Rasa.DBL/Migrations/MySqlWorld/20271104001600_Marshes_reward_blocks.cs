using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Marshes;

#nullable disable

namespace Rasa.Migrations.MySqlWorld
{
    /// <summary>
    /// Valverde Marshes sets 1-4: the rewards of seven missions moved to the client's per-mission template blocks
    /// (MarshesRewardBlocksV1). Data only.
    /// </summary>
    public partial class Marshes_reward_blocks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in MarshesRewardBlocksV1.Up())
                migrationBuilder.Sql(sql);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in MarshesRewardBlocksV1.Down())
                migrationBuilder.Sql(sql);
        }
    }
}
