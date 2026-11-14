using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Ashen;

#nullable disable

namespace Rasa.Migrations.MySqlWorld
{
    /// <summary>
    /// Ligo Ashen Desert fix job E (AshenFixEV1), a delta on 20271110000900 (set 9, live) and 20271110000200 (set 3); it sorts after set 10 (20271110001000): On The
    /// Same Frequency's (1277) consoles 5-6 m off their towers' upper teleporters and off the walk from the lift's exit, used only
    /// from their own floor, and its ambushes on the top floors (to-fix rows 12-14); the Sheitan Highway's two Thrax Ravager pools
    /// (790085, 790086) out of the camp pool's reach, two Ravagers each, with the markers of 1247, 1266 and 1269 on them (the
    /// encounter tuning). Data only, plain SQL; each UPDATE guarded on the row's exact stored values; Down puts them back.
    /// </summary>
    public partial class Ashen_fix_e : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder) => AshenFixEV1.Up(migrationBuilder);

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) => AshenFixEV1.Down(migrationBuilder);
    }
}
