using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

using Rasa.Context.World;
using Rasa.Services.Preloader;

namespace Rasa.Migrations.MySqlWorld
{
    /// <summary>
    /// The maps of the mission markers the server put on the wrong map - Devil's Den, One Way Trip,
    /// Weapons Trafficking, Looking For Logos?, The Better Half, Ancient Mysteries, Turpis Transport and
    /// Deadly Silence - and a marker inside Turpis Refinery for Looking For Logos? (MissionMarkerMaps).
    /// Data only, in plain SQL: no model of its own.
    ///
    /// Down takes them off again.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    [DbContext(typeof(MySqlWorldContext))]
    [Migration("20271103000000_Mission_marker_maps")]
    public class Mission_marker_maps : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in MissionMarkerMaps.Up(mySql: true))
                migrationBuilder.Sql(sql);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in MissionMarkerMaps.Down())
                migrationBuilder.Sql(sql);
        }
    }
}
