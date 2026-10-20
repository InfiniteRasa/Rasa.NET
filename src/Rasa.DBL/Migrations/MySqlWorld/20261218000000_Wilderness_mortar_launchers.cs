using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

using Rasa.Context.World;
using Rasa.Services.Preloader;

namespace Rasa.Migrations.MySqlWorld
{
    /// <summary>
    /// The four Wilderness Bane mortars (630076..630079) given Weapon_Creature_Bane_Mortar_Launcher
    /// in their weapon slot, which their WEAPON_GROUNDTARGET attack fires from
    /// (WildernessMortarLaunchers). Data only, in plain SQL: no model of its own.
    ///
    /// It was 20261217000000, the timestamp Fithik_bites has. A World that ran it under that id
    /// runs it again under this one, which the guarded insert leaves as it is, and its history
    /// row for the old id is removed.
    ///
    /// Down takes the launcher away.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    [DbContext(typeof(MySqlWorldContext))]
    [Migration("20261218000000_Wilderness_mortar_launchers")]
    public class Wilderness_mortar_launchers : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(WildernessMortarLaunchers.Up);
            migrationBuilder.Sql(WildernessMortarLaunchers.ForgetOldId);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(WildernessMortarLaunchers.Down);
        }
    }
}
