using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Ashen;

#nullable disable

namespace Rasa.Migrations.SqliteWorld
{
    /// <summary>
    /// Ligo Ashen Desert fix job T (AshenFixTV1, to-fix row 60), a delta on 20271110002160 (fix S2), Bane Conscription Facility
    /// (1988): 1498's drop pools 531064 (radius 30 -> 12) and 531066 (21.3 -> 14) drew creatures onto the ramps round the biolab;
    /// 1498's markers 1 and 2 round the new circles (35 -> 17, 26.3 -> 19). Data only, plain SQL; each UPDATE guarded on the
    /// exact value before; Down puts them back and deletes fix T's evidence row.
    /// </summary>
    public partial class Ashen_fix_t : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder) => AshenFixTV1.Up(migrationBuilder);

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) => AshenFixTV1.Down(migrationBuilder);
    }
}
