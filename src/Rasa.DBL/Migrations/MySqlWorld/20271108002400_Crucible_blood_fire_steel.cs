using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Crucible;

#nullable disable

namespace Rasa.Migrations.MySqlWorld
{
    /// <summary>
    /// Ligo Crucible set 15, the Incurables Ward: Atali, Umank and the Headsman with their guards, Incurable pool 770048 made
    /// a point (CrucibleBloodFireSteelV1.NpcUp), and Blood, Fire, and Steel (1401, CrucibleBloodFireSteelV1). Data only, plain
    /// SQL.
    /// </summary>
    public partial class Crucible_blood_fire_steel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in CrucibleBloodFireSteelV1.NpcUp())
                migrationBuilder.Sql(CrucibleSql.Guard(sql));
            CrucibleBloodFireSteelV1.Up(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            CrucibleBloodFireSteelV1.Down(migrationBuilder);
            foreach (var sql in CrucibleBloodFireSteelV1.NpcDown())
                migrationBuilder.Sql(CrucibleSql.Guard(sql));
        }
    }
}
