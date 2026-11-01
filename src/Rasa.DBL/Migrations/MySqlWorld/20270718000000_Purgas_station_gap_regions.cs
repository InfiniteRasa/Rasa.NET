using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

using Rasa.Context.World;
using Rasa.Services.Preloader;

namespace Rasa.Migrations.MySqlWorld
{
    /// <summary>
    /// Purgas Station's three corridor ends that showed the Level 01 entrance hall's name: two on
    /// Level 03 and the top of a ramp up to Level 01 (PurgasStationGapRegions). Data only, in
    /// plain SQL: no model of its own.
    ///
    /// Down takes the new rows out.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    [DbContext(typeof(MySqlWorldContext))]
    [Migration("20270718000000_Purgas_station_gap_regions")]
    public class Purgas_station_gap_regions : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in PurgasStationGapRegions.Up())
                migrationBuilder.Sql(sql);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in PurgasStationGapRegions.Down())
                migrationBuilder.Sql(sql);
        }
    }
}
