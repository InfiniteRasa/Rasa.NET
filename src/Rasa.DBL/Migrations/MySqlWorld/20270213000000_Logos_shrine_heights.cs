using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

using Rasa.Context.World;
using Rasa.Services.Preloader;

namespace Rasa.Migrations.MySqlWorld
{
    /// <summary>
    /// Eight logos shrines put back over their pedestals as the rest stand (LogosShrineHeights).
    /// Data only, in plain SQL: no model of its own.
    ///
    /// Down undoes only what is still as Up left it.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    [DbContext(typeof(MySqlWorldContext))]
    [Migration("20270213000000_Logos_shrine_heights")]
    public class Logos_shrine_heights : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in LogosShrineHeights.Up())
                migrationBuilder.Sql(sql);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in LogosShrineHeights.Down())
                migrationBuilder.Sql(sql);
        }
    }
}
