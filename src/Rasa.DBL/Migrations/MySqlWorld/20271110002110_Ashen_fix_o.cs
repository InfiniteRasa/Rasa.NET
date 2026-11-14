using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Ashen;

#nullable disable

namespace Rasa.Migrations.MySqlWorld
{
    /// <summary>
    /// Ligo Ashen Desert fix job O (AshenFixOV1), a delta on 20271110001700 (set 17, live) and 20271110001930 (fix M, live),
    /// Avernus Outpost (2110): Petrovich's Run's first objective counts two pools of ours (790700, 790701) on the main floor
    /// instead of upstream 531049 and 531060, whose creatures stand on navmesh islands inside the client's rock; markers 1 and 3
    /// with them. Upstream rows untouched. Data only, plain SQL; the inserts guarded (CrucibleSql.Guard), each UPDATE guarded on
    /// the stored values; Down puts them back, deletes the two pools and fix O's evidence row.
    /// </summary>
    public partial class Ashen_fix_o : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder) => AshenFixOV1.Up(migrationBuilder);

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) => AshenFixOV1.Down(migrationBuilder);
    }
}
