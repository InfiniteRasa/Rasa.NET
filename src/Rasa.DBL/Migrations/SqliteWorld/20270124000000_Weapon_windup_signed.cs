using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

using Rasa.Context.World;

namespace Rasa.Migrations.SqliteWorld
{
    /// <summary>
    /// itemtemplate_weapon.windup becomes signed. The client's tooltip prints its "Autofire"
    /// line for a weapon whose windup is -1 (client/ui/tooltipwindow.py, windupTime == -1), and
    /// an unsigned column could not hold it (ItemTemplateWeaponEntry.AutofireWindup). No row
    /// changes: every weapon keeps its windup, and which ones were autofire is not in the
    /// client; a game master sets -1 through the item editor.
    ///
    /// On SQLite the column is INTEGER either way and already holds -1, so there is nothing to
    /// change; this is here so both Worlds have the same history. Down gives autofire weapons a
    /// windup of 0, as on MySQL.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    [DbContext(typeof(SqliteWorldContext))]
    [Migration("20270124000000_Weapon_windup_signed")]
    public partial class Weapon_windup_signed : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE itemtemplate_weapon SET windup = 0 WHERE windup < 0;");
        }
    }
}
