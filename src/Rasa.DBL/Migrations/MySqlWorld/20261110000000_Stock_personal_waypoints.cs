using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

namespace Rasa.Migrations.MySqlWorld
{
    using Context.World;
    using Services.Preloader;

    /// <summary>
    /// Every medical vendor sells the three Personal Waypoints (PersonalWaypointStock, which
    /// says which vendors those are and why). No table changes.
    ///
    /// Down takes the three off the medical vendors' lists; none had them before.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    [DbContext(typeof(MySqlWorldContext))]
    [Migration("20261110000000_Stock_personal_waypoints")]
    public partial class Stock_personal_waypoints : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var statement in PersonalWaypointStock.Up)
                migrationBuilder.Sql(statement);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var statement in PersonalWaypointStock.Down)
                migrationBuilder.Sql(statement);
        }
    }
}
