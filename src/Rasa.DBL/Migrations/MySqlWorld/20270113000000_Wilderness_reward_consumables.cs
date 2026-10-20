using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

using Rasa.Context.World;
using Rasa.Services.Preloader.Missions.Wilderness;

namespace Rasa.Migrations.MySqlWorld
{
    /// <summary>
    /// The Wilderness rewards get back the EMP Bombs, grenades, Res Trauma Kits and Adrenaline
    /// Booster their sources list, in place of the med packs that stood in for them while those had
    /// no effect on the server (WildernessRewardConsumables). Data only, in plain SQL: no model of
    /// its own.
    ///
    /// Down puts the med packs back.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    [DbContext(typeof(MySqlWorldContext))]
    [Migration("20270113000000_Wilderness_reward_consumables")]
    public class Wilderness_reward_consumables : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in WildernessRewardConsumables.Up())
                migrationBuilder.Sql(sql);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in WildernessRewardConsumables.Down())
                migrationBuilder.Sql(sql);
        }
    }
}
