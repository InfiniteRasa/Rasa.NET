using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Ashen;

#nullable disable

namespace Rasa.Migrations.MySqlWorld
{
    /// <summary>
    /// Ligo Ashen Desert fix job F (AshenFixFV1), a delta on 20271110001000 (set 10, live); it sorts after set 13 (20271110001300):
    /// set 10's four boss pools (790361, 790366, 790372, 790377) respawn after 300 s, not 50 s (to-fix row 18), and Designer
    /// Jewelry Gone Wrong (1287) gets one Ashen Atta Soldier pool of its own (790378) out of the Mauna Barrens camp's call for help,
    /// with a marker on it (to-fix row 22). Data only, plain SQL; each UPDATE guarded on the row's exact stored values; Down puts
    /// them back and takes the new rows out.
    /// </summary>
    public partial class Ashen_fix_f : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder) => AshenFixFV1.Up(migrationBuilder);

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) => AshenFixFV1.Down(migrationBuilder);
    }
}
