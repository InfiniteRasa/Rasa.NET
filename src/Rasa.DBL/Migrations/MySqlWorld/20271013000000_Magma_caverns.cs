using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

using Rasa.Context.World;
using Rasa.Services.Preloader;

namespace Rasa.Migrations.MySqlWorld
{
    /// <summary>
    /// Magma Caverns' people, both sides, Ember and missions (MagmaCavernsContent).
    /// Data only, in plain SQL: no model of its own.
    ///
    /// Down takes the rows out again and puts the Flaregashers back.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    [DbContext(typeof(MySqlWorldContext))]
    [Migration("20271013000000_Magma_caverns")]
    public class Magma_caverns : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in MagmaCavernsContent.Up())
                migrationBuilder.Sql(sql);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in MagmaCavernsContent.Down())
                migrationBuilder.Sql(sql);
        }
    }
}
