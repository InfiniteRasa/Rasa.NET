using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Ashen;

#nullable disable

namespace Rasa.Migrations.MySqlWorld
{
    /// <summary>
    /// Ligo Ashen Desert fix job L (to-fix row 46, AshenFixLV1): a way out of Avernus Outpost's prison (1473's teleporter
    /// sets the player down in a room the real client keeps closed): the one-way local teleporter 923 in the prison to the
    /// arrival 924 beside the inbound teleporter, and 1473's evidence row 4. To-fix row 47: 1498's scene names its drop pools
    /// 531064 and 531066 as public-spawn actors (they come back on their own 120 s in a squad's copy), and 1498's evidence row
    /// 4. Sorts after set 19 (20271110001900) and fix K (20271110001910). Its one UPDATE (1498's scene) only where the row is
    /// set 19's exactly, Down the same way back; Down deletes only the rows it inserted.
    /// </summary>
    public partial class Ashen_fix_l : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder) => AshenFixLV1.Up(migrationBuilder);

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) => AshenFixLV1.Down(migrationBuilder);
    }
}
