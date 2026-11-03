using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

using Rasa.Context.World;
using Rasa.Services.Preloader;

namespace Rasa.Migrations.MySqlWorld
{
    /// <summary>
    /// Minos Caverns' warnet nests hung from the cave roof instead of standing under the floor
    /// (MinosWarnetNestsHung). Data only, in plain SQL: no model of its own.
    ///
    /// Down undoes only what is still as Up left it.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    [DbContext(typeof(MySqlWorldContext))]
    [Migration("20270928000000_Minos_warnet_nests_hung")]
    public class Minos_warnet_nests_hung : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in MinosWarnetNestsHung.Up())
                migrationBuilder.Sql(sql);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in MinosWarnetNestsHung.Down())
                migrationBuilder.Sql(sql);
        }
    }
}
