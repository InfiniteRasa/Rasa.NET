using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

using Rasa.Context.World;
using Rasa.Services.Preloader;

namespace Rasa.Migrations.MySqlWorld
{
    /// <summary>
    /// Eloh Vale's caves, gate, pyramid, waterfalls and canyon given their volumes
    /// (ElohValeRegions). Data only, in plain SQL: no model of its own.
    ///
    /// Down takes the rows out again.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    [DbContext(typeof(MySqlWorldContext))]
    [Migration("20270626000000_Eloh_vale_regions")]
    public class Eloh_vale_regions : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in ElohValeRegions.Up())
                migrationBuilder.Sql(sql);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in ElohValeRegions.Down())
                migrationBuilder.Sql(sql);
        }
    }
}
