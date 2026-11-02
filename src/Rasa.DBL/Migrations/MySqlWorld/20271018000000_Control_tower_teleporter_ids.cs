using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

using Rasa.Context.World;
using Rasa.Services.Preloader;

namespace Rasa.Migrations.MySqlWorld
{
    /// <summary>
    /// The control tower pads moved from 1100-1156 to 800-856, under the wormholes' ids
    /// (ControlTowerTeleporterIds). Data only, in plain SQL: no model of its own.
    ///
    /// Down moves them back.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    [DbContext(typeof(MySqlWorldContext))]
    [Migration("20271018000000_Control_tower_teleporter_ids")]
    public class Control_tower_teleporter_ids : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in ControlTowerTeleporterIds.Up())
                migrationBuilder.Sql(sql);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in ControlTowerTeleporterIds.Down())
                migrationBuilder.Sql(sql);
        }
    }
}
