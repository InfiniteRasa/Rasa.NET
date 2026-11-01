using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

using Rasa.Context.World;
using Rasa.Services.Preloader;

namespace Rasa.Migrations.SqliteWorld
{
    /// <summary>
    /// The region sweep on Pre'o Das (the village ruins): new volumes for its unplaced regions and the
    /// broken rows recut (VillageRuinsRegionSweep). Data only, in plain SQL: no model of its own.
    ///
    /// Down puts the recut rows back and takes the new ones out.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    [DbContext(typeof(SqliteWorldContext))]
    [Migration("20270719030000_Village_ruins_region_sweep")]
    public class Village_ruins_region_sweep : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in VillageRuinsRegionSweep.Up())
                migrationBuilder.Sql(sql);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in VillageRuinsRegionSweep.Down())
                migrationBuilder.Sql(sql);

            // The rows' ids given back, so a reapply brings them back with the same ones (MapRegionSequence).
            migrationBuilder.Sql(MapRegionSequence.Sqlite);
        }
    }
}
