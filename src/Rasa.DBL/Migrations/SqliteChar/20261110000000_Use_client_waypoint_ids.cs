using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

namespace Rasa.Migrations.SqliteChar
{
    using Context.Char;
    using Services.Preloader;

    /// <summary>
    /// What each character has gained (character_teleporter) under the ids the world database's
    /// Use_client_waypoint_ids gives the waypoints and hospitals (ClientWaypointIds). No table
    /// changes.
    ///
    /// Down gives the old ids back, less what Up merged or took out.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    [DbContext(typeof(SqliteCharContext))]
    [Migration("20261110000000_Use_client_waypoint_ids")]
    public partial class Use_client_waypoint_ids : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var statement in ClientWaypointIds.CharUp)
                migrationBuilder.Sql(statement);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var statement in ClientWaypointIds.CharDown)
                migrationBuilder.Sql(statement);
        }
    }
}
