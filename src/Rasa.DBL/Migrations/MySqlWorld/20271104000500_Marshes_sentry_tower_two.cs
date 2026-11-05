using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Marshes;

#nullable disable

namespace Rasa.Migrations.MySqlWorld
{
    /// <summary>
    /// Valverde Marshes set 6, the sentry tower's second part: Archer Thoung and the four soldiers she sends the player to
    /// (MarshesSentryTowerTwoNpcsV1), with Sergeant McDonald's The Core of the Matter (1878, MarshesCoreOfTheMatterV1) and Data
    /// Disaster (1870, MarshesDataDisasterV1), Lt. Kiang's This Calls For Destruction (1879, MarshesThisCallsForDestructionV1)
    /// and Thoung's Need a Little Help Here (1873, MarshesNeedALittleHelpHereV1). Data only.
    /// </summary>
    public partial class Marshes_sentry_tower_two : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in MarshesSentryTowerTwoNpcsV1.Up())
                migrationBuilder.Sql(MarshesSql.Guard(sql));
            MarshesCoreOfTheMatterV1.Up(migrationBuilder);
            MarshesThisCallsForDestructionV1.Up(migrationBuilder);
            MarshesDataDisasterV1.Up(migrationBuilder);
            MarshesNeedALittleHelpHereV1.Up(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            MarshesNeedALittleHelpHereV1.Down(migrationBuilder);
            MarshesDataDisasterV1.Down(migrationBuilder);
            MarshesThisCallsForDestructionV1.Down(migrationBuilder);
            MarshesCoreOfTheMatterV1.Down(migrationBuilder);
            foreach (var sql in MarshesSentryTowerTwoNpcsV1.Down())
                migrationBuilder.Sql(MarshesSql.Guard(sql));
        }
    }
}
