using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

using Rasa.Context.World;
using Rasa.Services.Preloader;

namespace Rasa.Migrations.MySqlWorld
{
    /// <summary>
    /// Palisades' temple passages, caves, interiors, ruins, fields and land bridge given their volumes
    /// (PalisadesRegions). Data only, in plain SQL: no model of its own.
    ///
    /// Down takes the rows out and gives the circle back.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    [DbContext(typeof(MySqlWorldContext))]
    [Migration("20270625000000_Palisades_regions")]
    public class Palisades_regions : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in PalisadesRegions.Up())
                migrationBuilder.Sql(sql);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in PalisadesRegions.Down())
                migrationBuilder.Sql(sql);
        }
    }
}
