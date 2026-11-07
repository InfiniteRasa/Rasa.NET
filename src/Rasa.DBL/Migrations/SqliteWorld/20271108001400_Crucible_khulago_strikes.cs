using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Crucible;

#nullable disable

namespace Rasa.Migrations.SqliteWorld
{
    /// <summary>
    /// Ligo Crucible set 9, Khulago Base from Prometheus Outpost: Captain Tiersky moved out of the control point model
    /// (CrucibleKhulagoStrikesNpcsV1), with Pump Up The Jam (1224, CruciblePumpUpTheJamV1) and Strike Zone (1214,
    /// CrucibleStrikeZoneV1). Data only, plain SQL.
    /// </summary>
    public partial class Crucible_khulago_strikes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in CrucibleKhulagoStrikesNpcsV1.Up())
                migrationBuilder.Sql(CrucibleSql.Guard(sql));
            CruciblePumpUpTheJamV1.Up(migrationBuilder);
            CrucibleStrikeZoneV1.Up(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            CrucibleStrikeZoneV1.Down(migrationBuilder);
            CruciblePumpUpTheJamV1.Down(migrationBuilder);
            foreach (var sql in CrucibleKhulagoStrikesNpcsV1.Down())
                migrationBuilder.Sql(CrucibleSql.Guard(sql));
        }
    }
}
