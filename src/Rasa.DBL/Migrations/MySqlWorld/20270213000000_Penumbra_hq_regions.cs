using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

using Rasa.Context.World;
using Rasa.Services.Preloader;

namespace Rasa.Migrations.MySqlWorld
{
    /// <summary>
    /// Penumbra Headquarters and its five Sections in Tantalus Base given their volumes, and Eloh Space's
    /// minimap rectangle cut to its two shrines (PenumbraHqRegions). Data only, in plain SQL.
    ///
    /// Down takes the new rows out and puts Eloh Space's rectangle back.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    [DbContext(typeof(MySqlWorldContext))]
    [Migration("20270213000000_Penumbra_hq_regions")]
    public class Penumbra_hq_regions : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in PenumbraHqRegions.Up())
                migrationBuilder.Sql(sql);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in PenumbraHqRegions.Down())
                migrationBuilder.Sql(sql);
        }
    }
}
