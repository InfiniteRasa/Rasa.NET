using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Ashen;
using Rasa.Services.Preloader.Missions.Crucible;

#nullable disable

namespace Rasa.Migrations.SqliteWorld
{
    /// <summary>
    /// Ligo Ashen Desert set 14, round Ashoka: Lieutenant Daniel Chase, Researchers Yorish and Jackson and Overseer Jirex new
    /// (AshenAshokaNpcsV1), with Echoes in the Sand (1736, AshenEchoesInTheSandV1), Supply Logistics (1358,
    /// AshenSupplyLogisticsV1) and Precious Tools (1286, AshenPreciousToolsV1). Sorts after fix G (20271110001320).
    /// </summary>
    public partial class Ashen_ashoka : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in AshenAshokaNpcsV1.Up())
                migrationBuilder.Sql(CrucibleSql.Guard(sql));
            AshenEchoesInTheSandV1.Up(migrationBuilder);
            AshenSupplyLogisticsV1.Up(migrationBuilder);
            AshenPreciousToolsV1.Up(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            AshenPreciousToolsV1.Down(migrationBuilder);
            AshenSupplyLogisticsV1.Down(migrationBuilder);
            AshenEchoesInTheSandV1.Down(migrationBuilder);
            foreach (var sql in AshenAshokaNpcsV1.Down())
                migrationBuilder.Sql(CrucibleSql.Guard(sql));
        }
    }
}
