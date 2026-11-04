using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Marshes;

#nullable disable

namespace Rasa.Migrations.SqliteWorld
{
    /// <summary>
    /// Valverde Marshes set 5, the sentry tower of Far West Watch: Sergeant McDonald, Warrior Dikah, Lt. Kiang and Overseer Nelg
    /// (MarshesSentryTowerNpcsV1), with McDonald's Mortar Madness (1874, MarshesMortarMadnessV1), Dikah's Under the Radar (1875,
    /// MarshesUnderTheRadarV1) and Kiang's two versions of Under the Radar - Part II, the control point one (1876,
    /// MarshesUnderTheRadarPartTwoV1) and Overseer Nelg's (1877, MarshesUnderTheRadarNelgV1). Data only.
    /// </summary>
    public partial class Marshes_sentry_tower : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in MarshesSentryTowerNpcsV1.Up())
                migrationBuilder.Sql(MarshesSql.Guard(sql));
            MarshesMortarMadnessV1.Up(migrationBuilder);
            MarshesUnderTheRadarV1.Up(migrationBuilder);
            MarshesUnderTheRadarPartTwoV1.Up(migrationBuilder);
            MarshesUnderTheRadarNelgV1.Up(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            MarshesUnderTheRadarNelgV1.Down(migrationBuilder);
            MarshesUnderTheRadarPartTwoV1.Down(migrationBuilder);
            MarshesUnderTheRadarV1.Down(migrationBuilder);
            MarshesMortarMadnessV1.Down(migrationBuilder);
            foreach (var sql in MarshesSentryTowerNpcsV1.Down())
                migrationBuilder.Sql(MarshesSql.Guard(sql));
        }
    }
}
