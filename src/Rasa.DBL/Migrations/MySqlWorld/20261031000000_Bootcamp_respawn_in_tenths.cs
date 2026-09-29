using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

namespace Rasa.Migrations.MySqlWorld
{
    using Context.World;
    using Structures.World;

    /// <summary>
    /// spawnpool.respown_time is in tenths of a second (SpawnPoolManager: 900 is a minute and a
    /// half). The Bootcamp pools SeedWorldContent added were written in whole seconds - 20 for a
    /// scene's actors, 120 for the route's Thrax - so they are put in tenths here: 200 and 1200.
    /// Only the Bootcamp rows, 510203-510271 on map 1985.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    [DbContext(typeof(MySqlWorldContext))]
    [Migration("20261031000000_Bootcamp_respawn_in_tenths")]
    public partial class Bootcamp_respawn_in_tenths : Migration
    {
        private const string Rows = "map_context_id = 1985 and id between 510203 and 510271";

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($"update {SpawnPoolEntry.TableName} set respown_time = respown_time * 10 where {Rows};");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($"update {SpawnPoolEntry.TableName} set respown_time = respown_time / 10 where {Rows};");
        }
    }
}
