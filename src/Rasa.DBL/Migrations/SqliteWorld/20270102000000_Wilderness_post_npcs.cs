using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

using Rasa.Context.World;
using Rasa.Services.Preloader;

namespace Rasa.Migrations.SqliteWorld
{
    /// <summary>
    /// The second batch of Wilderness NPCs and objects placed from gameplay video, at the Landing
    /// Zone, Lower Eloh Creek, Alia Das and Sgt. Pierre's cache: new NPCs, moves, recasts, looks
    /// and poses, the unnamed vendor at the Landing Zone taken away, terminals, footlockers, a
    /// turret, Bane barrels and the Landing Zone gate's force field (WildernessPostNpcs). Data
    /// only, in plain SQL: no model of its own.
    ///
    /// Down undoes what is still as Up left it.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    [DbContext(typeof(SqliteWorldContext))]
    [Migration("20270102000000_Wilderness_post_npcs")]
    public class Wilderness_post_npcs : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in WildernessPostNpcs.Up())
                migrationBuilder.Sql(sql);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in WildernessPostNpcs.Down())
                migrationBuilder.Sql(sql);
        }
    }
}
