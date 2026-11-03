using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

using Rasa.Context.World;
using Rasa.Services.Preloader;

namespace Rasa.Migrations.SqliteWorld
{
    /// <summary>
    /// The Kardash Atta Colony's research outpost waypoints, Captain Reyko's Underground Mysteries
    /// and Atta Control offered at Into the Hive's turn-in (KardashWaypointsAndMysteries). Data
    /// only, in plain SQL: no model of its own.
    ///
    /// Down takes the mission out, puts back Atta Control's sources and the waypoints' placeholders.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    [DbContext(typeof(SqliteWorldContext))]
    [Migration("20271017000000_Kardash_waypoints_mysteries")]
    public class Kardash_waypoints_mysteries : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in KardashWaypointsAndMysteries.Up())
                migrationBuilder.Sql(sql);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in KardashWaypointsAndMysteries.Down())
                migrationBuilder.Sql(sql);
        }
    }
}
