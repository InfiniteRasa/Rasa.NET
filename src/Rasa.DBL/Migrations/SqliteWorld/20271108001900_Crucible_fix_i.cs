using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Crucible;

#nullable disable

namespace Rasa.Migrations.SqliteWorld
{
    /// <summary>
    /// Ligo Crucible fix job I (to-fix row 69, triage items 21 and 24): the Ward door marker of Sadistic Sabotage (1445) and
    /// Rehabilitation at Gunpoint (1402) moved onto the walk from Tempa, 8.5 m outside the door's circle, its radius 3; Control
    /// the Brain (1521)'s container marker onto the container's south side; an evidence row each (CrucibleFixIV1). A delta on
    /// 20271108001600 and 20271108001700. Data only, plain SQL; Down puts back the old places.
    /// </summary>
    public partial class Crucible_fix_i : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder) => CrucibleFixIV1.Up(migrationBuilder);

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) => CrucibleFixIV1.Down(migrationBuilder);
    }
}
