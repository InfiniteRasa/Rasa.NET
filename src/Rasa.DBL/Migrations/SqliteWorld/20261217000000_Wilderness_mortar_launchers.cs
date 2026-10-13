using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

using Rasa.Context.World;
using Rasa.Services.Preloader;

namespace Rasa.Migrations.SqliteWorld
{
    /// <summary>
    /// The four Wilderness Bane mortars (630076..630079) given Weapon_Creature_Bane_Mortar_Launcher
    /// in their weapon slot, which their WEAPON_GROUNDTARGET attack fires from
    /// (WildernessMortarLaunchers). Data only, in plain SQL: no model of its own.
    ///
    /// Down takes the launcher away.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    [DbContext(typeof(SqliteWorldContext))]
    [Migration("20261217000000_Wilderness_mortar_launchers")]
    public class Wilderness_mortar_launchers : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(WildernessMortarLaunchers.Up);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(WildernessMortarLaunchers.Down);
        }
    }
}
