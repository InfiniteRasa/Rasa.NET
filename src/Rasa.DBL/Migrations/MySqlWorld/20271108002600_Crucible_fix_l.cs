using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Crucible;

#nullable disable

namespace Rasa.Migrations.MySqlWorld
{
    /// <summary>
    /// Ligo Crucible fix job L (to-fix row 81): Leaky Pipes (1398) poses Dhimand's copy Standing as it comes (scene sequence
    /// 0), so it holds its post at (-40, 94.61, 386) and, invulnerable and posted, seeks no fight while it stands idle
    /// (CreatureGameplayRules.KeepsOutOfFights): it no longer joins the Sanctum hub Incurables' fight and stays at their pool;
    /// an evidence row for 1398 (CrucibleFixLV1). A delta on 20271108002000. Data only, plain SQL; Down puts back the old scene.
    /// </summary>
    public partial class Crucible_fix_l : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder) => CrucibleFixLV1.Up(migrationBuilder);

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) => CrucibleFixLV1.Down(migrationBuilder);
    }
}
