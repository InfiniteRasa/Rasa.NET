using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

using Rasa.Context.World;
using Rasa.Services.Preloader;

namespace Rasa.Migrations.MySqlWorld
{
    /// <summary>
    /// The Fault Lever's West, East and North Sectors over their wings, the Main Generators
    /// label circle recut to the generator hall, and the Operations room and Transfer Station
    /// (FaultLeverSectorRegions). Data only, in plain SQL: no model of its own.
    ///
    /// Down puts the circle back and takes the new rows out.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    [DbContext(typeof(MySqlWorldContext))]
    [Migration("20270717000000_Fault_lever_sector_regions")]
    public class Fault_lever_sector_regions : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in FaultLeverSectorRegions.Up())
                migrationBuilder.Sql(sql);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in FaultLeverSectorRegions.Down())
                migrationBuilder.Sql(sql);
        }
    }
}
