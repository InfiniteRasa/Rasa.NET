using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

using Rasa.Context.World;
using Rasa.Services.Preloader;

namespace Rasa.Migrations.MySqlWorld
{
    /// <summary>
    /// The epic Caves of Donn's places given their volumes
    /// (CavesOfDonnEpicRegions). Data only, in plain SQL: no model of its own.
    ///
    /// Down takes them out again.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    [DbContext(typeof(MySqlWorldContext))]
    [Migration("20270702000000_Caves_of_donn_epic_regions")]
    public class Caves_of_donn_epic_regions : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in CavesOfDonnEpicRegions.Up())
                migrationBuilder.Sql(sql);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in CavesOfDonnEpicRegions.Down())
                migrationBuilder.Sql(sql);
        }
    }
}
