using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

using Rasa.Context.World;
using Rasa.Services.Preloader;

namespace Rasa.Migrations.MySqlWorld
{
    /// <summary>
    /// The region sweep on the Valverde Descent: new volumes for its unplaced regions and the broken
    /// rows recut (ValverdeDescentRegionSweep). Data only, in plain SQL: no model of its own.
    ///
    /// Down puts the recut rows back and takes the new ones out.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    [DbContext(typeof(MySqlWorldContext))]
    [Migration("20270721000000_Valverde_descent_region_sweep")]
    public class Valverde_descent_region_sweep : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in ValverdeDescentRegionSweep.Up())
                migrationBuilder.Sql(sql);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in ValverdeDescentRegionSweep.Down())
                migrationBuilder.Sql(sql);
        }
    }
}
