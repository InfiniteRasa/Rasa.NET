using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

using Rasa.Context.World;
using Rasa.Services.Preloader.Missions;

namespace Rasa.Migrations.SqliteWorld
{
    /// <summary>
    /// The Proving Grounds' cave-in blows whichever way the player comes to it
    /// (BootcampCaveInAnyRouteV13): reaching the doorwell starts Capture the Flag's breach as
    /// coming off the bridge does, and a player whose "Find a way out of the cave" is done with
    /// the pile still shut starts it by coming up to the pile (a new area, 441). Data only, in
    /// plain SQL: no model of its own.
    ///
    /// Down takes the rows out again.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    [DbContext(typeof(SqliteWorldContext))]
    [Migration("20270120000000_Bootcamp_cave_in_any_route")]
    public class Bootcamp_cave_in_any_route : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in BootcampCaveInAnyRouteV13.Up())
                migrationBuilder.Sql(sql);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in BootcampCaveInAnyRouteV13.Down())
                migrationBuilder.Sql(sql);
        }
    }
}
