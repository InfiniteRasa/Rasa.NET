using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

using Rasa.Context.World;
using Rasa.Services.Preloader;

namespace Rasa.Migrations.SqliteWorld
{
    /// <summary>
    /// Twenty-eight region label circles given the floor and ceiling of the room their label
    /// stands in, so a room's region stays on its own floor (RegionLabelRooms). Data only, in
    /// plain SQL: no model of its own.
    ///
    /// Down gives them their full height again.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    [DbContext(typeof(SqliteWorldContext))]
    [Migration("20270206000000_Region_label_rooms")]
    public class Region_label_rooms : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in RegionLabelRooms.Up())
                migrationBuilder.Sql(sql);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in RegionLabelRooms.Down())
                migrationBuilder.Sql(sql);
        }
    }
}
