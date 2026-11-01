using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

using Rasa.Context.World;
using Rasa.Services.Preloader;

namespace Rasa.Migrations.MySqlWorld
{
    /// <summary>
    /// The region sweep on the Logos Research Facility: new volumes for its unplaced regions and the
    /// broken rows recut (LogosResearchFacilityRegionSweep). Data only, in plain SQL: no model of its
    /// own.
    ///
    /// Down puts the recut rows back and takes the new ones out.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    [DbContext(typeof(MySqlWorldContext))]
    [Migration("20270719020000_Logos_research_facility_region_sweep")]
    public class Logos_research_facility_region_sweep : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in LogosResearchFacilityRegionSweep.Up())
                migrationBuilder.Sql(sql);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in LogosResearchFacilityRegionSweep.Down())
                migrationBuilder.Sql(sql);
        }
    }
}
