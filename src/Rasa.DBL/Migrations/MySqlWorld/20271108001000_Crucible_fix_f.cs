using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Crucible;

#nullable disable

namespace Rasa.Migrations.MySqlWorld
{
    /// <summary>
    /// Ligo Crucible fix job F (the real client pass 2026-11-07; to-fix rows 44, 50, 59, 60, Minesweeper 1232): twelve pools
    /// made points (radius 0), six of them (the Lasher pools, 540141, 770011, 770024, Molitor's) moved off lava with their
    /// markers, and nine evidence rows (CrucibleFixFV1). A delta on 20271108000200-20271108000800, which the live World DB
    /// has applied. Data only, plain SQL; Down puts back their values.
    /// </summary>
    public partial class Crucible_fix_f : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder) => CrucibleFixFV1.Up(migrationBuilder);

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) => CrucibleFixFV1.Down(migrationBuilder);
    }
}
