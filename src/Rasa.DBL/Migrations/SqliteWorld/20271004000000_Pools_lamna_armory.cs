using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Pools;

#nullable disable

namespace Rasa.Migrations.SqliteWorld
{
    /// <summary>
    /// The Lamna Armory (squad instance 1465): its givers, key bosses and Overlord (PoolsLamnaArmoryNpcsV1), with its
    /// three missions: Bane Battle Plans (896, PoolsBaneBattlePlansV1), Operation Overlord (898, PoolsOperationOverlordV1)
    /// and Blow Them All Up (903, PoolsBlowThemAllUpV1). Data only.
    /// </summary>
    public partial class Pools_lamna_armory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in PoolsLamnaArmoryNpcsV1.Up())
                migrationBuilder.Sql(sql);
            PoolsBaneBattlePlansV1.Up(migrationBuilder);
            PoolsOperationOverlordV1.Up(migrationBuilder);
            PoolsBlowThemAllUpV1.Up(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            PoolsBlowThemAllUpV1.Down(migrationBuilder);
            PoolsOperationOverlordV1.Down(migrationBuilder);
            PoolsBaneBattlePlansV1.Down(migrationBuilder);
            foreach (var sql in PoolsLamnaArmoryNpcsV1.Down())
                migrationBuilder.Sql(sql);
        }
    }
}
