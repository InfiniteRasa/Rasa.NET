using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Ashen;

#nullable disable

namespace Rasa.Migrations.SqliteWorld
{
    /// <summary>
    /// Ligo Ashen Desert fix job V2 (AshenFixV2V1, to-fix row 64, second try), a delta on 20271110002180 (fix V), Bane Conscription
    /// Facility (1988): 1501's hanging Life Support System (26423) is out of the client's 6 m use reach; a Bane console (7468, name
    /// 153) on the floor takes its role and use (objective 4), the device stays as a visual role (10000002, 10000003 after 4), and
    /// objective 4's marker moves to the console. Data only, plain SQL; the UPDATEs guarded on fix V's exact scene JSON and the
    /// marker's place; Down puts them back and deletes fix V2's evidence row.
    /// </summary>
    public partial class Ashen_fix_v2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder) => AshenFixV2V1.Up(migrationBuilder);

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) => AshenFixV2V1.Down(migrationBuilder);
    }
}
