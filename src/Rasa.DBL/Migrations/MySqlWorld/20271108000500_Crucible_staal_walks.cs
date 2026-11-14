using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Crucible;

#nullable disable

namespace Rasa.Migrations.MySqlWorld
{
    /// <summary>
    /// Ligo Crucible fix (to-fix rows 32, 34, 35): Top Khana Grupa Munit moved off the holed Top Khana room onto the lower
    /// corridor, Merchant Vargo moved out of the Staal Centrum crafting station, their markers on them, and Merchant Yeg's
    /// marker kept off the second floor hole (CrucibleStaalWalksV1). A delta on 20271108000000 and 20271108000100, which the
    /// live World DB has applied. Data only, plain SQL; Down puts back their values.
    /// </summary>
    public partial class Crucible_staal_walks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder) => CrucibleStaalWalksV1.Up(migrationBuilder);

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) => CrucibleStaalWalksV1.Down(migrationBuilder);
    }
}
