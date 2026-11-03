using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

using Rasa.Context.World;
using Rasa.Services.Preloader;

namespace Rasa.Migrations.MySqlWorld
{
    /// <summary>
    /// The Caves of Donn's places given the epic copy's boxes, and the epic copy's caverns widened
    /// (CavesOfDonnRegions). Data only, in plain SQL: no model of its own.
    ///
    /// Down puts the circles and the epic boxes back and takes the new rows out.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    [DbContext(typeof(MySqlWorldContext))]
    [Migration("20270703000000_Caves_of_donn_regions")]
    public class Caves_of_donn_regions : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in CavesOfDonnRegions.Up())
                migrationBuilder.Sql(sql);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in CavesOfDonnRegions.Down())
                migrationBuilder.Sql(sql);
        }
    }
}
