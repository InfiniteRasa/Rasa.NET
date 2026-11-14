using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Ashen;

#nullable disable

namespace Rasa.Migrations.SqliteWorld
{
    /// <summary>
    /// Ligo Ashen Desert fix job V (AshenFixVV1, to-fix row 64), a delta on 20271110002170 (fix T), Bane Conscription Facility
    /// (1988): 1501's Life Support System (26423) spawns in 10000001 (idle), goes to 10000002 when 3 is done and to 10000003 (the
    /// end of the client's chain, no use point) when 4 is done. Data only, plain SQL; the UPDATE guarded on 1501's exact scene
    /// JSON; Down puts it back and deletes fix V's evidence row.
    /// </summary>
    public partial class Ashen_fix_v : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder) => AshenFixVV1.Up(migrationBuilder);

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) => AshenFixVV1.Down(migrationBuilder);
    }
}
