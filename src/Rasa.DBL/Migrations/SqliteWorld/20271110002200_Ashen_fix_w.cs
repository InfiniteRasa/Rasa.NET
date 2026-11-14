using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Ashen;

#nullable disable

namespace Rasa.Migrations.SqliteWorld
{
    /// <summary>
    /// Ligo Ashen Desert fix job W (AshenFixWV1), a delta on 20271110002190 (fix V2): Wendy (790376), the Atta Hive Queen (790664)
    /// and Enslaver Xahn (790666) get attacks their client skeletons animate (new creature_action 790243-790245: 470/1, 174/25,
    /// 1/241), and 1499's scenarios 6 and 7 get names that fit MySQL's 64 characters. Data only, plain SQL; the UPDATEs guarded
    /// on the exact old values; Down puts the old actions back and deletes the three rows (the short names stay).
    /// </summary>
    public partial class Ashen_fix_w : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder) => AshenFixWV1.Up(migrationBuilder);

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) => AshenFixWV1.Down(migrationBuilder);
    }
}
