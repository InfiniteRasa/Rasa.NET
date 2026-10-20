using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

using Rasa.Context.World;

namespace Rasa.Migrations.MySqlWorld
{
    /// <summary>
    /// itemtemplate_weapon.windup becomes signed. The client's tooltip prints its "Autofire"
    /// line for a weapon whose windup is -1 (client/ui/tooltipwindow.py, windupTime == -1), and
    /// an unsigned column could not hold it (ItemTemplateWeaponEntry.AutofireWindup). No row
    /// changes: every weapon keeps its windup, and which ones were autofire is not in the
    /// client; a game master sets -1 through the item editor.
    ///
    /// Down makes it unsigned again; a -1 would not fit, so autofire weapons are given a windup
    /// of 0 first.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    [DbContext(typeof(MySqlWorldContext))]
    [Migration("20270124000000_Weapon_windup_signed")]
    public partial class Weapon_windup_signed : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "windup",
                table: "itemtemplate_weapon",
                type: "int",
                nullable: false,
                oldClrType: typeof(uint),
                oldType: "int unsigned");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE itemtemplate_weapon SET windup = 0 WHERE windup < 0;");

            migrationBuilder.AlterColumn<uint>(
                name: "windup",
                table: "itemtemplate_weapon",
                type: "int unsigned",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");
        }
    }
}
