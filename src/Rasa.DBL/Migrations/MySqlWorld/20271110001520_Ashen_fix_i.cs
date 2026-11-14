using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Ashen;

#nullable disable

namespace Rasa.Migrations.MySqlWorld
{
    /// <summary>
    /// Ligo Ashen Desert fix job I (AshenFixIV1), a delta on 20271110001500 (set 15, live) after fix H (20271110001510, live);
    /// it sorts before set 16 (20271110001600). Indra Caverns (2055), to-fix row 34: Guard Bendal's body into the pillbox
    /// (its marker with it), and 1360's facility door to the Security Check Point entrance's door place, as the client's
    /// entrance door (class 23160). Data only, plain SQL; each UPDATE guarded on set 15's exact stored values; Down puts them
    /// back and deletes fix I's evidence row.
    /// </summary>
    public partial class Ashen_fix_i : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder) => AshenFixIV1.Up(migrationBuilder);

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) => AshenFixIV1.Down(migrationBuilder);
    }
}
