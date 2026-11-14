using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Ashen;

#nullable disable

namespace Rasa.Migrations.MySqlWorld
{
    /// <summary>
    /// Ligo Ashen Desert fix job A (to-fix row 1): Fithik Infestation's (1274) Stonewall Ridge pool 540067 put a share of its
    /// Fithik on the ground under a rock shelf, where a player on the shelf could not shoot them, and filled up with them.
    /// Now a point pool (radius 0) on the open east slope at (261, 308.3, -642.2), and 1274's marker on it; an evidence row for
    /// 1274 (AshenFixAV1). A delta on 20271110000000. Data only, plain SQL, guarded on the old values; Down puts upstream's
    /// pool and set 1's marker back.
    /// </summary>
    public partial class Ashen_fix_a : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder) => AshenFixAV1.Up(migrationBuilder);

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) => AshenFixAV1.Down(migrationBuilder);
    }
}
