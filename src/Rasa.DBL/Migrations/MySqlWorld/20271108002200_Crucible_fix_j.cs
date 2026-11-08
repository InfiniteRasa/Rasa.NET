using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Crucible;

#nullable disable

namespace Rasa.Migrations.MySqlWorld
{
    /// <summary>
    /// Ligo Crucible fix job J (to-fix row 75): the Smart Mine (770014) of Minesweeper (1232) no longer self-destructs (its
    /// action 2, the necromite_self_destruct row 770016, taken off) and its attack, row 770014, is the Thrax's Virulent decay
    /// 450/1 (a small damage over time) instead of the Necromite's Physical weapon 1/109; an evidence row for 1232
    /// (CrucibleFixJV1). A delta on 20271108000400. Data only, plain SQL; Down puts back the old rows.
    /// </summary>
    public partial class Crucible_fix_j : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder) => CrucibleFixJV1.Up(migrationBuilder);

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) => CrucibleFixJV1.Down(migrationBuilder);
    }
}
