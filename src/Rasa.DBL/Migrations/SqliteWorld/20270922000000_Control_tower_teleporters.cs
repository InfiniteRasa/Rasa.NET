using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

using Rasa.Context.World;
using Rasa.Services.Preloader;

namespace Rasa.Migrations.SqliteWorld
{
    /// <summary>
    /// A local teleporter on each floor of the thirty-three ArchHumOutpostTowerV02 towers - the
    /// control points' and outposts' - linked both ways, and Research Point's two ground pads
    /// linked up their own towers (ControlTowerTeleporters). Data only, in plain SQL: no model of
    /// its own.
    ///
    /// Down undoes only what is still as Up left it.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    [DbContext(typeof(SqliteWorldContext))]
    [Migration("20270922000000_Control_tower_teleporters")]
    public class Control_tower_teleporters : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in ControlTowerTeleporters.Up())
                migrationBuilder.Sql(sql);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in ControlTowerTeleporters.Down())
                migrationBuilder.Sql(sql);
        }
    }
}
