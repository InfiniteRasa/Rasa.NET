using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Ashen;

#nullable disable

namespace Rasa.Migrations.SqliteWorld
{
    /// <summary>
    /// Ligo Ashen Desert fix job S (AshenFixSV1), a delta on 20271110002140 (fix R, live), Bane Conscription Facility (1988):
    /// 1497's terminal (scene role in its mission_scene_binding) and its marker 251 moved from the HQ "hall" (-4, 135.94, -372),
    /// terrain the real client has no floor on, onto the HQ tunnel's end deck. Data only, plain SQL; each UPDATE guarded on
    /// the exact values before; Down puts them back and deletes fix S's evidence row.
    /// </summary>
    public partial class Ashen_fix_s : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder) => AshenFixSV1.Up(migrationBuilder);

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) => AshenFixSV1.Down(migrationBuilder);
    }
}
