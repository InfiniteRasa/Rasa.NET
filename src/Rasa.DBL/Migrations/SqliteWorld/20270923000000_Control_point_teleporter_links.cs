using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

using Rasa.Context.World;
using Rasa.Services.Preloader;

namespace Rasa.Migrations.SqliteWorld
{
    /// <summary>
    /// The three hospitals and waypoints Add_control_points left off their control points - the
    /// Landing Zone's waypoint among them, which stayed open while the Bane held the point
    /// (ControlPointTeleporterLinks). Data only, in plain SQL: no model of its own.
    ///
    /// Down takes out these links alone.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    [DbContext(typeof(SqliteWorldContext))]
    [Migration("20270923000000_Control_point_teleporter_links")]
    public class Control_point_teleporter_links : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in ControlPointTeleporterLinks.Up())
                migrationBuilder.Sql(sql);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in ControlPointTeleporterLinks.Down())
                migrationBuilder.Sql(sql);
        }
    }
}
