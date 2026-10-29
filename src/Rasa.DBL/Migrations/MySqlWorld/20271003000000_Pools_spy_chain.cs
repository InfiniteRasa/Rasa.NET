using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Pools;

#nullable disable

namespace Rasa.Migrations.MySqlWorld
{
    /// <summary>
    /// Corporal Sheen's package and her stand-in at her console (PoolsSpyNpcsV1), with the spy chain: Spy Networking (1331,
    /// PoolsSpyNetworkingV1), Poison Control (1368, PoolsPoisonControlV1) and Taking the Poison (1590,
    /// PoolsTakingThePoisonV1), the last two a fork (PoolsPoisonCanistersV1). Data only.
    /// </summary>
    public partial class Pools_spy_chain : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in PoolsSpyNpcsV1.Up())
                migrationBuilder.Sql(sql);
            PoolsSpyNetworkingV1.Up(migrationBuilder);
            PoolsPoisonControlV1.Up(migrationBuilder);
            PoolsTakingThePoisonV1.Up(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            PoolsTakingThePoisonV1.Down(migrationBuilder);
            PoolsPoisonControlV1.Down(migrationBuilder);
            PoolsSpyNetworkingV1.Down(migrationBuilder);
            foreach (var sql in PoolsSpyNpcsV1.Down())
                migrationBuilder.Sql(sql);
        }
    }
}
