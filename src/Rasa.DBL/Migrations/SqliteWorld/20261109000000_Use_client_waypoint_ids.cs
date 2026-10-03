using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

namespace Rasa.Migrations.SqliteWorld
{
    using Context.World;
    using Services.Preloader;

    /// <summary>
    /// Ten waypoints and two hospitals moved onto the ids the client has names for, two local
    /// teleporters and one that was a hospital typed as what they are, and the map markers that
    /// stand for them (ClientWaypointIds, which says what each is and why). No table changes.
    ///
    /// Down puts every teleporter row back as it was. The character database has the same
    /// change to what each character has gained (its own Use_client_waypoint_ids).
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    [DbContext(typeof(SqliteWorldContext))]
    [Migration("20261109000000_Use_client_waypoint_ids")]
    public partial class Use_client_waypoint_ids : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var statement in ClientWaypointIds.WorldUp)
                migrationBuilder.Sql(statement);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var statement in ClientWaypointIds.WorldDown)
                migrationBuilder.Sql(statement);
        }
    }
}
