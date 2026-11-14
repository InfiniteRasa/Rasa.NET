using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Ashen;

#nullable disable

namespace Rasa.Migrations.SqliteWorld
{
    /// <summary>
    /// Ligo Ashen Desert fix job J (AshenFixJV1), a delta on 20271110001500 (set 15, live) and 20271110001600 (set 16, live).
    /// Indra Caverns (2055): the Watch Post's east pool 790566 away from the facility's entrance tunnel, 1360's marker 223 with
    /// it (to-fix row 36); 1447's drills 4-6 out of the lava, their markers with them (to-fix row 38); 1452's 3 opens 6 with 5 (set 16
    /// re-audit 1). Data only, plain SQL; each UPDATE guarded on the sets' exact stored values; Down puts them back and deletes
    /// fix J's two actions and evidence rows.
    /// </summary>
    public partial class Ashen_fix_j : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder) => AshenFixJV1.Up(migrationBuilder);

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) => AshenFixJV1.Down(migrationBuilder);
    }
}
