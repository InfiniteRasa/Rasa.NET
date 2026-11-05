using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Marshes;

#nullable disable

namespace Rasa.Migrations.MySqlWorld
{
    /// <summary>
    /// Valverde Marshes, to-fix rows 24 and 25: Kaav, Qunle and Rakla (and Kaav's and Rakla's markers) onto a client collision
    /// floor (MarshesOnTheClientFloorV1), The Core of the Matter's third crate onto the depot floor (MarshesCoreOfTheMatterV3
    /// in place of V2), Overseer Nelg from his unreachable post onto the deck (MarshesUnderTheRadarNelgV2 in place of
    /// V1), and 1871's fourth Maw flower from over a cliff onto its top (MarshesMawFlowersV2 in place of V1). Down puts back the old places and versions. Data only.
    /// </summary>
    public partial class Marshes_on_the_client_floor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in MarshesOnTheClientFloorV1.Up())
                migrationBuilder.Sql(MarshesSql.Guard(sql));
            MarshesCoreOfTheMatterV2.Down(migrationBuilder);
            MarshesCoreOfTheMatterV3.Up(migrationBuilder);
            MarshesUnderTheRadarNelgV1.Down(migrationBuilder);
            MarshesUnderTheRadarNelgV2.Up(migrationBuilder);
            MarshesMawFlowersV1.Down(migrationBuilder);
            MarshesMawFlowersV2.Up(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            MarshesMawFlowersV2.Down(migrationBuilder);
            MarshesMawFlowersV1.Up(migrationBuilder);
            MarshesUnderTheRadarNelgV2.Down(migrationBuilder);
            MarshesUnderTheRadarNelgV1.Up(migrationBuilder);
            MarshesCoreOfTheMatterV3.Down(migrationBuilder);
            MarshesCoreOfTheMatterV2.Up(migrationBuilder);
            foreach (var sql in MarshesOnTheClientFloorV1.Down())
                migrationBuilder.Sql(MarshesSql.Guard(sql));
        }
    }
}
