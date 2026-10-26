using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

using Rasa.Context.World;
using Rasa.Services.Preloader;

namespace Rasa.Migrations.MySqlWorld
{
    /// <summary>
    /// Convict Pass's lava tube and the ravine at its north mouth given their volumes
    /// (ConvictPassRegions). Data only, in plain SQL: no model of its own.
    ///
    /// Down takes the new rows out and gives the minimap rectangle back.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    [DbContext(typeof(MySqlWorldContext))]
    [Migration("20270221000000_Convict_pass_regions")]
    public class Convict_pass_regions : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in ConvictPassRegions.Up())
                migrationBuilder.Sql(sql);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in ConvictPassRegions.Down())
                migrationBuilder.Sql(sql);
        }
    }
}
