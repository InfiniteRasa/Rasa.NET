using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

namespace Rasa.Migrations.MySqlWorld
{
    using Context.World;
    using Services.Preloader;

    /// <summary>
    /// A waypoint and two hospitals the client's map marks, given the place their teleporter rows
    /// lacked, and tied to their markers and control points (MarkedTeleporters, which says what
    /// each is and why). No table changes.
    ///
    /// Down leaves each row as it was.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    [DbContext(typeof(MySqlWorldContext))]
    [Migration("20261119000000_Place_marked_teleporters")]
    public partial class Place_marked_teleporters : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var statement in MarkedTeleporters.WorldUp)
                migrationBuilder.Sql(statement);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var statement in MarkedTeleporters.WorldDown)
                migrationBuilder.Sql(statement);
        }
    }
}
