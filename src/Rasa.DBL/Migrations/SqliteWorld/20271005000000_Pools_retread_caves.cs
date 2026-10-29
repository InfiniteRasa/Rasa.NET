using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Pools;

#nullable disable

namespace Rasa.Migrations.SqliteWorld
{
    /// <summary>
    /// The Retread Caves (squad instance 1694): their Retreads, mission creatures and Mox and mutated miasma pools
    /// (PoolsRetreadCavesNpcsV1), with Stick Your Neck Out (1329, PoolsStickYourNeckOutV1) and the Mox Clearing chain:
    /// Mox Clearing (1132, PoolsMoxClearingV1), Mox Shouldn't Smoke (1135, PoolsMoxShouldntSmokeV1), A Mox On Both Our
    /// Houses (1133, PoolsMoxOnBothOurHousesV1), Deadly Moxins, er-Toxins (1134, PoolsDeadlyMoxinsV1), Miasma Mutations
    /// (1131, PoolsMiasmaMutationsV1), Miasma Testing (1136, PoolsMiasmaTestingV1), Total Slaughter (1138,
    /// PoolsTotalSlaughterV1) and The Mad Bomber (856, PoolsMadBomberV1). Data only.
    /// </summary>
    public partial class Pools_retread_caves : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in PoolsRetreadCavesNpcsV1.Up())
                migrationBuilder.Sql(sql);
            PoolsStickYourNeckOutV1.Up(migrationBuilder);
            PoolsMoxClearingV1.Up(migrationBuilder);
            PoolsMoxShouldntSmokeV1.Up(migrationBuilder);
            PoolsMoxOnBothOurHousesV1.Up(migrationBuilder);
            PoolsDeadlyMoxinsV1.Up(migrationBuilder);
            PoolsMiasmaMutationsV1.Up(migrationBuilder);
            PoolsMiasmaTestingV1.Up(migrationBuilder);
            PoolsTotalSlaughterV1.Up(migrationBuilder);
            PoolsMadBomberV1.Up(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            PoolsMadBomberV1.Down(migrationBuilder);
            PoolsTotalSlaughterV1.Down(migrationBuilder);
            PoolsMiasmaTestingV1.Down(migrationBuilder);
            PoolsMiasmaMutationsV1.Down(migrationBuilder);
            PoolsDeadlyMoxinsV1.Down(migrationBuilder);
            PoolsMoxOnBothOurHousesV1.Down(migrationBuilder);
            PoolsMoxShouldntSmokeV1.Down(migrationBuilder);
            PoolsMoxClearingV1.Down(migrationBuilder);
            PoolsStickYourNeckOutV1.Down(migrationBuilder);
            foreach (var sql in PoolsRetreadCavesNpcsV1.Down())
                migrationBuilder.Sql(sql);
        }
    }
}
