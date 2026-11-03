using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

using Rasa.Context.World;
using Rasa.Services.Preloader;

namespace Rasa.Migrations.MySqlWorld
{
    /// <summary>
    /// Burning Steps' Outpost Intrepid and Khulago Base label circles recut to their underground
    /// interiors, the rest of both put under their regions, and the east end of the cave behind
    /// the Staal Overlook (BurningStepsInteriorRegions). Data only, in plain SQL: no model of
    /// its own.
    ///
    /// Down puts the circles back and takes the new rows out.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    [DbContext(typeof(MySqlWorldContext))]
    [Migration("20270716000000_Burning_steps_interior_regions")]
    public class Burning_steps_interior_regions : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in BurningStepsInteriorRegions.Up())
                migrationBuilder.Sql(sql);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in BurningStepsInteriorRegions.Down())
                migrationBuilder.Sql(sql);
        }
    }
}
