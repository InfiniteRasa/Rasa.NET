using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

using Rasa.Context.World;
using Rasa.Services.Preloader;

namespace Rasa.Migrations.MySqlWorld
{
    /// <summary>
    /// Indra Caverns' facility and plant rooms given their volumes
    /// (IndraCavernsRegions). Data only, in plain SQL: no model of its own.
    ///
    /// Down takes the new rows out again and gives the changed rows back.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    [DbContext(typeof(MySqlWorldContext))]
    [Migration("20270605000000_Indra_caverns_regions")]
    public class Indra_caverns_regions : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in IndraCavernsRegions.Up())
                migrationBuilder.Sql(sql);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in IndraCavernsRegions.Down())
                migrationBuilder.Sql(sql);
        }
    }
}
