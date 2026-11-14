using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Ashen;

#nullable disable

namespace Rasa.Migrations.SqliteWorld
{
    /// <summary>
    /// Ligo Ashen Desert fix job H (AshenFixHV1), a delta on 20271110001500 (set 15, live); it sorts after set 15 and before
    /// set 16 (20271110001600): a pair of local teleporters (913 in the Indra Pass valley by Surveyor Odesha, 914 on the
    /// island of the Indra Caverns door, linked both ways), since no player walks onto that island (to-fix row 33, upstream
    /// N23/N26), and a marker for 1559's objective 1 on the valley pad. Data only, plain SQL; each INSERT guarded; Down takes
    /// out exactly these rows.
    /// </summary>
    public partial class Ashen_fix_h : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder) => AshenFixHV1.Up(migrationBuilder);

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) => AshenFixHV1.Down(migrationBuilder);
    }
}
