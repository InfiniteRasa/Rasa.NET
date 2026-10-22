using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

using Rasa.Context.World;
using Rasa.Services.Preloader;

namespace Rasa.Migrations.MySqlWorld
{
    /// <summary>
    /// The Bane Fluxite Mines' places given their volumes
    /// (FluxiteMinesRegions). Data only, in plain SQL: no model of its own.
    ///
    /// Down takes the new rows out again and gives the circles back.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    [DbContext(typeof(MySqlWorldContext))]
    [Migration("20270612000000_Fluxite_mines_regions")]
    public class Fluxite_mines_regions : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in FluxiteMinesRegions.Up())
                migrationBuilder.Sql(sql);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in FluxiteMinesRegions.Down())
                migrationBuilder.Sql(sql);
        }
    }
}
