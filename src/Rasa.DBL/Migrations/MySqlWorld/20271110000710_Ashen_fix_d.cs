using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Ashen;

#nullable disable

namespace Rasa.Migrations.MySqlWorld
{
    /// <summary>
    /// Ligo Ashen Desert fix job D (AshenFixDV1): Robotic Salvage Recovery's (1264) Motor Assist Armor Legs at TaRapedia's
    /// level 43, 29093 -> 29113, with an evidence row for 1264 (a delta on 20271110000700, live; set 7 review); and the Shadow's
    /// Edge Post hospital (teleporter 208) from y 269.3, under the floor, to 296.5 (to-fix row 8). Data only, plain SQL, each
    /// UPDATE guarded on the row's exact stored values; Down puts 29093 and 269.3 back.
    /// </summary>
    public partial class Ashen_fix_d : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder) => AshenFixDV1.Up(migrationBuilder);

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) => AshenFixDV1.Down(migrationBuilder);
    }
}
