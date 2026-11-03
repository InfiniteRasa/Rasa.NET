using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

using Rasa.Context.World;
using Rasa.Services.Preloader;

namespace Rasa.Migrations.SqliteWorld
{
    /// <summary>
    /// Daghda's Urn Village's hospital, housing and research HQ under the Wilderness's human
    /// interior regions 26, 27 and 30 instead of its cave region 38 (DaghdasUrnBuildingRegions).
    /// Data only, in plain SQL: no model of its own.
    ///
    /// Down undoes only what is still as Up left it.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    [DbContext(typeof(SqliteWorldContext))]
    [Migration("20270923000000_Daghdas_urn_building_regions")]
    public class Daghdas_urn_building_regions : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in DaghdasUrnBuildingRegions.Up())
                migrationBuilder.Sql(sql);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in DaghdasUrnBuildingRegions.Down())
                migrationBuilder.Sql(sql);
        }
    }
}
