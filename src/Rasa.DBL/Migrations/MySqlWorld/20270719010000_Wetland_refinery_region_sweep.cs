using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

using Rasa.Context.World;
using Rasa.Services.Preloader;

namespace Rasa.Migrations.MySqlWorld
{
    /// <summary>
    /// The region sweep on the Wetland Refinery: new volumes for its unplaced regions and the broken
    /// rows recut (WetlandRefineryRegionSweep). Data only, in plain SQL: no model of its own.
    ///
    /// Down puts the recut rows back and takes the new ones out.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    [DbContext(typeof(MySqlWorldContext))]
    [Migration("20270719010000_Wetland_refinery_region_sweep")]
    public class Wetland_refinery_region_sweep : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in WetlandRefineryRegionSweep.Up())
                migrationBuilder.Sql(sql);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in WetlandRefineryRegionSweep.Down())
                migrationBuilder.Sql(sql);
        }
    }
}
