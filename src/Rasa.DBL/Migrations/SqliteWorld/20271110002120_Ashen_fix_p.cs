using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Ashen;

#nullable disable

namespace Rasa.Migrations.SqliteWorld
{
    /// <summary>
    /// Ligo Ashen Desert fix job P (AshenFixPV1), a delta on 20271110002110 (fix O, live), Avernus Outpost (2110): fix O's two
    /// pools (790700, 790701, 1535's objective 1) sized for one player; places and radii kept. Data only, plain SQL; each UPDATE
    /// guarded on fix O's exact counts; Down puts them back and deletes fix P's evidence row.
    /// </summary>
    public partial class Ashen_fix_p : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder) => AshenFixPV1.Up(migrationBuilder);

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) => AshenFixPV1.Down(migrationBuilder);
    }
}
