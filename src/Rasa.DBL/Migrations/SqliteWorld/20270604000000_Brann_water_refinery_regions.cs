using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

using Rasa.Context.World;
using Rasa.Services.Preloader;

namespace Rasa.Migrations.SqliteWorld
{
    /// <summary>
    /// The Brann Water Refinery's rooms given their volumes
    /// (BrannWaterRefineryRegions). Data only, in plain SQL: no model of its own.
    ///
    /// Down takes the new rows out again and gives the label circles back.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    [DbContext(typeof(SqliteWorldContext))]
    [Migration("20270604000000_Brann_water_refinery_regions")]
    public class Brann_water_refinery_regions : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in BrannWaterRefineryRegions.Up())
                migrationBuilder.Sql(sql);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in BrannWaterRefineryRegions.Down())
                migrationBuilder.Sql(sql);

            // The rows' ids given back, so a reapply brings them back with the same ones (MapRegionSequence).
            migrationBuilder.Sql(MapRegionSequence.Sqlite);
        }
    }
}
