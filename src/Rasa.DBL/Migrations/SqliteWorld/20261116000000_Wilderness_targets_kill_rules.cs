using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

namespace Rasa.Migrations.SqliteWorld
{
    using Context.World;
    using Services.Preloader;

    /// <summary>
    /// Wilderness Targets of Opportunity (1449) runs as the Wilderness missions wrote it, with
    /// the kill rules of the other battlefields' Targets of Opportunity on its four kill
    /// objectives: species, maps, squad credit and titles (WildernessTargetsKillRules, which
    /// says what goes and why). No table changes.
    ///
    /// Down puts the mission's kill triggers and scene binding back as they were written. The
    /// character database has the other half (its own Wilderness_targets_kill_rules).
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    [DbContext(typeof(SqliteWorldContext))]
    [Migration("20261116000000_Wilderness_targets_kill_rules")]
    public partial class Wilderness_targets_kill_rules : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var statement in WildernessTargetsKillRules.WorldUp)
                migrationBuilder.Sql(statement);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var statement in WildernessTargetsKillRules.WorldDown)
                migrationBuilder.Sql(statement);
        }
    }
}
