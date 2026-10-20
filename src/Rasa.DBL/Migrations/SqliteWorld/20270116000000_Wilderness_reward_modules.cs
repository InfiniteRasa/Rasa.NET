using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

using Rasa.Context.World;
using Rasa.Services.Preloader.Missions.Wilderness;

namespace Rasa.Migrations.SqliteWorld
{
    /// <summary>
    /// The Wilderness gear rewards are given with the module their source names them by: a Pulsar
    /// Boots is Hazmat boots with Resist EMP [1] (WildernessRewardModules). Data only, in plain
    /// SQL: no model of its own.
    ///
    /// Down empties the slots again.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    [DbContext(typeof(SqliteWorldContext))]
    [Migration("20270116000000_Wilderness_reward_modules")]
    public class Wilderness_reward_modules : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in WildernessRewardModules.Up())
                migrationBuilder.Sql(sql);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in WildernessRewardModules.Down())
                migrationBuilder.Sql(sql);
        }
    }
}
