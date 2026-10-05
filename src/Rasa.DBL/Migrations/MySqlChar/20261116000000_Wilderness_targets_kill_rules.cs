using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

namespace Rasa.Migrations.MySqlChar
{
    using Context.Char;
    using Services.Preloader;

    /// <summary>
    /// Wilderness Targets of Opportunity (1449) no longer runs under the revision targets_1
    /// (the world database's Wilderness_targets_kill_rules). A character holding it under that
    /// revision, or offered it, has the assignment or the offer deleted, and takes the mission
    /// again from Lt Col Cimoch: the kill counts start over. Titles earned stay. No table
    /// changes.
    ///
    /// Down does nothing: the counts are gone, and that revision is not there to hold them.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    [DbContext(typeof(MySqlCharContext))]
    [Migration("20261116000000_Wilderness_targets_kill_rules")]
    public partial class Wilderness_targets_kill_rules : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var statement in WildernessTargetsKillRules.CharUp)
                migrationBuilder.Sql(statement);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
