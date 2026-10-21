using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

using Rasa.Context.World;
using Rasa.Services.Preloader;

namespace Rasa.Migrations.MySqlWorld
{
    /// <summary>
    /// Staal's buildings, the Eloh places and the Chaukus Robotics Facility on Burning Steps
    /// given their volumes (BurningStepsRegions). Data only, in plain SQL: no model of its own.
    ///
    /// Down takes the new rows out and gives the two changed circles back.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    [DbContext(typeof(MySqlWorldContext))]
    [Migration("20270220000000_Burning_steps_regions")]
    public class Burning_steps_regions : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in BurningStepsRegions.Up())
                migrationBuilder.Sql(sql);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in BurningStepsRegions.Down())
                migrationBuilder.Sql(sql);
        }
    }
}
