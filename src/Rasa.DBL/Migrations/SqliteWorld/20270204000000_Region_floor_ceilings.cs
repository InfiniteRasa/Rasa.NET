using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

using Rasa.Context.World;
using Rasa.Services.Preloader;

namespace Rasa.Migrations.SqliteWorld
{
    /// <summary>
    /// Three interior minimap rectangles given the floor and ceiling of their rooms, so their sky
    /// stays below the base on top of them: Fort Virgil's cargo bay and Foreas Base's two lower
    /// levels (RegionFloorCeilings). Data only, in plain SQL: no model of its own.
    ///
    /// Down gives them their full height again.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    [DbContext(typeof(SqliteWorldContext))]
    [Migration("20270204000000_Region_floor_ceilings")]
    public class Region_floor_ceilings : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in RegionFloorCeilings.Up())
                migrationBuilder.Sql(sql);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in RegionFloorCeilings.Down())
                migrationBuilder.Sql(sql);
        }
    }
}
