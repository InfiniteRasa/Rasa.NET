using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Ashen;

#nullable disable

namespace Rasa.Migrations.MySqlWorld
{
    /// <summary>
    /// Ligo Ashen Desert fix job B (to-fix row 5): no real-client walk reached Thrax Overseer Exing (Chemical Oversight, 1244) at
    /// set 2's spot on the mesa's slope (-289, 332.98, -264): the navmesh's routes end on flanks the client does not climb. His
    /// pool 520007 moves to (-260, 308.9, -310) on the flat at the mesa's south foot, and 1244's marker with it; an evidence row
    /// for 1244 (AshenFixBV1). A delta on 20271110000100. Data only, plain SQL, guarded on set 2's values; Down puts set 2's
    /// pool and marker back.
    /// </summary>
    public partial class Ashen_fix_b : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder) => AshenFixBV1.Up(migrationBuilder);

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) => AshenFixBV1.Down(migrationBuilder);
    }
}
