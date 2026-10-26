using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

using Rasa.Context.World;
using Rasa.Services.Preloader;

namespace Rasa.Migrations.MySqlWorld
{
    /// <summary>
    /// Quasso Station's Dropship Zone yard and its Prison given their volumes
    /// (QuassoStationRegions). Data only, in plain SQL: no model of its own.
    ///
    /// Down takes the rows out again.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    [DbContext(typeof(MySqlWorldContext))]
    [Migration("20270214000000_Quasso_station_regions")]
    public class Quasso_station_regions : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in QuassoStationRegions.Up())
                migrationBuilder.Sql(sql);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in QuassoStationRegions.Down())
                migrationBuilder.Sql(sql);
        }
    }
}
