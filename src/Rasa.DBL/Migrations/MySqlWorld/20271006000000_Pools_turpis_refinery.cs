using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Pools;

#nullable disable

namespace Rasa.Migrations.MySqlWorld
{
    /// <summary>
    /// Turpis Refinery (squad instance 1429): its five givers and the Toxicologist, with Corporal Hash on the Pools
    /// (PoolsTurpisRefineryNpcsV1), and its missions: Things That Blow Up (882, PoolsThingsThatBlowUpV1), Hard Core (884,
    /// PoolsHardCoreV1), Chemistry 101 (885, PoolsChemistry101V1), The Bad Doctor (886, PoolsTheBadDoctorV1), Cost of
    /// Doing Business (1079, PoolsCostOfDoingBusinessV1) and Turpis Transport (1305, PoolsTurpisTransportV1). Data only.
    /// </summary>
    public partial class Pools_turpis_refinery : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in PoolsTurpisRefineryNpcsV1.Up())
                migrationBuilder.Sql(sql);
            PoolsThingsThatBlowUpV1.Up(migrationBuilder);
            PoolsHardCoreV1.Up(migrationBuilder);
            PoolsChemistry101V1.Up(migrationBuilder);
            PoolsTheBadDoctorV1.Up(migrationBuilder);
            PoolsCostOfDoingBusinessV1.Up(migrationBuilder);
            PoolsTurpisTransportV1.Up(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            PoolsTurpisTransportV1.Down(migrationBuilder);
            PoolsCostOfDoingBusinessV1.Down(migrationBuilder);
            PoolsTheBadDoctorV1.Down(migrationBuilder);
            PoolsChemistry101V1.Down(migrationBuilder);
            PoolsHardCoreV1.Down(migrationBuilder);
            PoolsThingsThatBlowUpV1.Down(migrationBuilder);
            foreach (var sql in PoolsTurpisRefineryNpcsV1.Down())
                migrationBuilder.Sql(sql);
        }
    }
}
