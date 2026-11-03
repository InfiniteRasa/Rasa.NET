using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

using Rasa.Context.World;
using Rasa.Services.Preloader;

namespace Rasa.Migrations.SqliteWorld
{
    /// <summary>
    /// Mining for Information's and Kill the Light part II's scenes bound to their instance
    /// (BoundSceneMaps). Data only, in plain SQL: no model of its own.
    ///
    /// Down takes the binding off again.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    [DbContext(typeof(SqliteWorldContext))]
    [Migration("20271019000000_Bound_scene_maps")]
    public class Bound_scene_maps : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in BoundSceneMaps.Up(mySql: false))
                migrationBuilder.Sql(sql);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in BoundSceneMaps.Down())
                migrationBuilder.Sql(sql);
        }
    }
}
