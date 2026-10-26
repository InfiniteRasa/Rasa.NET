using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

using Rasa.Context.World;
using Rasa.Services.Preloader;

namespace Rasa.Migrations.MySqlWorld
{
    /// <summary>
    /// The reconstructed Wilderness hostile pools' respawn put in tenths of a second: they were
    /// written 20, two seconds (WildernessRespawns). Data only, in plain SQL: no model of its own.
    ///
    /// Down puts back 20 where this put its value.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    [DbContext(typeof(MySqlWorldContext))]
    [Migration("20270129000000_Wilderness_respawn_in_tenths")]
    public class Wilderness_respawn_in_tenths : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in WildernessRespawns.Up())
                migrationBuilder.Sql(sql);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in WildernessRespawns.Down())
                migrationBuilder.Sql(sql);
        }
    }
}
