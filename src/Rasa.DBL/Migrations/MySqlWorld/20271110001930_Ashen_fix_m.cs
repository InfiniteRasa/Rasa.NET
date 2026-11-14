using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Ashen;

#nullable disable

namespace Rasa.Migrations.MySqlWorld
{
    /// <summary>
    /// Ligo Ashen Desert fix job M (AshenFixMV1, to-fix row 48), a delta on 20271110001700 (set 17, live) and 20271110001800
    /// (set 18, live), Avernus Outpost (2110): Captain Petrovich 790642, 1533's area, 1535's scene copy and squad, and pad 916
    /// off upstream pool 531050's spawn ground; pad 915 to flat floor, walked onto by its ramp; Colonel Vandeweghe (1537's scene, area 2, markers 2-3) and pad 918 off 531058's;
    /// Petrovich, Drazen and Petrovich at the top named public-spawn actors (1533's and 1535's scenes). Data only, plain SQL;
    /// each UPDATE guarded on the sets' exact stored values; Down puts them back and deletes fix M's evidence rows.
    /// </summary>
    public partial class Ashen_fix_m : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder) => AshenFixMV1.Up(migrationBuilder);

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) => AshenFixMV1.Down(migrationBuilder);
    }
}
