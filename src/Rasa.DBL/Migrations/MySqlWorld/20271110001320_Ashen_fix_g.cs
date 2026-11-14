using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Ashen;

#nullable disable

namespace Rasa.Migrations.MySqlWorld
{
    /// <summary>
    /// Ligo Ashen Desert fix job G (AshenFixGV1), a delta on 20271110001300 (set 13, live) and on the Atta pools of set 2 and
    /// fix F (20271110001310); it sorts after fix F: Surveyor Malya Odesha, Researcher Endala Berish and four of God of Storms'
    /// resistors move off the Indra Pass waypoint's floor, which a new player cannot walk to (to-fix row 24), into the valley
    /// by its south entry; their markers move with them; the seven Atta pools put down one creature, not three (real client
    /// 2026-11-10 22:46). Data only, plain SQL; each UPDATE guarded on the row's exact stored values; Down puts them back.
    /// </summary>
    public partial class Ashen_fix_g : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder) => AshenFixGV1.Up(migrationBuilder);

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) => AshenFixGV1.Down(migrationBuilder);
    }
}
