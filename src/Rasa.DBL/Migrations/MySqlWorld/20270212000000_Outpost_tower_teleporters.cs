using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

using Rasa.Context.World;
using Rasa.Services.Preloader;

namespace Rasa.Migrations.MySqlWorld
{
    /// <summary>
    /// A local teleporter on each floor of the fourteen human outpost towers outside the
    /// Wilderness, linked both ways (OutpostTowerTeleporters). Data only, in plain SQL: no model
    /// of its own.
    ///
    /// Down undoes only what is still as Up left it.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    [DbContext(typeof(MySqlWorldContext))]
    [Migration("20270212000000_Outpost_tower_teleporters")]
    public class Outpost_tower_teleporters : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in OutpostTowerTeleporters.Up())
                migrationBuilder.Sql(sql);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in OutpostTowerTeleporters.Down())
                migrationBuilder.Sql(sql);
        }
    }
}
