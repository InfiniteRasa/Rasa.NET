using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

using Rasa.Context.World;
using Rasa.Services.Preloader;

namespace Rasa.Migrations.MySqlWorld
{
    /// <summary>
    /// The maps of the mission markers the server could not place - Crater Lake's, Timora Mines'
    /// Report to Field Ranger Kearney and Magma Caverns' Executioner and Final Chance
    /// (MarkerMapScopes). Data only, in plain SQL: no model of its own.
    ///
    /// Down takes them off again.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    [DbContext(typeof(MySqlWorldContext))]
    [Migration("20271020000000_Marker_map_scopes")]
    public class Marker_map_scopes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in MarkerMapScopes.Up(mySql: true))
                migrationBuilder.Sql(sql);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in MarkerMapScopes.Down())
                migrationBuilder.Sql(sql);
        }
    }
}
