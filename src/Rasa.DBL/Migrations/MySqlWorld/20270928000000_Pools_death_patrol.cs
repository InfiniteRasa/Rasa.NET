using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Pools;

#nullable disable

namespace Rasa.Migrations.MySqlWorld
{
    /// <summary>
    /// Riggins's Death Patrol (1315, PoolsDeathPatrolV1), its repeat More Clips and Tags (1321, PoolsMoreClipsAndTagsV1)
    /// and The Thirsty Hermit (1328, PoolsThirstyHermitV1); Colonel Snake Washington to his sourced place
    /// (PoolsWashingtonPlaceV1). Data only.
    /// </summary>
    public partial class Pools_death_patrol : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in PoolsWashingtonPlaceV1.Up())
                migrationBuilder.Sql(sql);
            PoolsDeathPatrolV1.Up(migrationBuilder);
            PoolsMoreClipsAndTagsV1.Up(migrationBuilder);
            PoolsThirstyHermitV1.Up(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            PoolsThirstyHermitV1.Down(migrationBuilder);
            PoolsMoreClipsAndTagsV1.Down(migrationBuilder);
            PoolsDeathPatrolV1.Down(migrationBuilder);
            foreach (var sql in PoolsWashingtonPlaceV1.Down())
                migrationBuilder.Sql(sql);
        }
    }
}
