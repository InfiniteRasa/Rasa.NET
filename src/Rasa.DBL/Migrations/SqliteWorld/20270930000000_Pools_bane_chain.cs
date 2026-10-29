using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Pools;

#nullable disable

namespace Rasa.Migrations.SqliteWorld
{
    /// <summary>
    /// The Eloh obelisk in the western Bane fort (PoolsObeliskV1) and a Linker for the Pools (PoolsLinkerV1), with
    /// Sabotage the Tower (937, PoolsSabotageTheTowerV1), Bane Jamming (938, PoolsBaneJammingV1) and Elimination (941,
    /// PoolsEliminationV1). Data only.
    /// </summary>
    public partial class Pools_bane_chain : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in PoolsObeliskV1.Up())
                migrationBuilder.Sql(sql);
            foreach (var sql in PoolsLinkerV1.Up())
                migrationBuilder.Sql(sql);
            PoolsSabotageTheTowerV1.Up(migrationBuilder);
            PoolsBaneJammingV1.Up(migrationBuilder);
            PoolsEliminationV1.Up(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            PoolsEliminationV1.Down(migrationBuilder);
            PoolsBaneJammingV1.Down(migrationBuilder);
            PoolsSabotageTheTowerV1.Down(migrationBuilder);
            foreach (var sql in PoolsLinkerV1.Down())
                migrationBuilder.Sql(sql);
            foreach (var sql in PoolsObeliskV1.Down())
                migrationBuilder.Sql(sql);
        }
    }
}
