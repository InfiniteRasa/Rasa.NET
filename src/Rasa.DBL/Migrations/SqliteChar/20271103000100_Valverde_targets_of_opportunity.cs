using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

namespace Rasa.Migrations.SqliteChar
{
    using Context.Char;
    using Services.Preloader;

    /// <summary>
    /// The objectives Valverde_targets_of_opportunity adds to Plateau's and Valverde Pools' Targets of
    /// Opportunity (1586, 1594) in the world database, for every character holding one of them: a
    /// mission held with fewer objectives than it has is cleared on login. Their other counts stay
    /// (TargetsOfOpportunitySeed.Valverde). No table changes.
    ///
    /// Down takes the objective rows out again.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    [DbContext(typeof(SqliteCharContext))]
    [Migration("20271103000100_Valverde_targets_of_opportunity")]
    public partial class Valverde_targets_of_opportunity : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var statement in TargetsOfOpportunitySeed.Valverde.CharUp)
                migrationBuilder.Sql(statement);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var statement in TargetsOfOpportunitySeed.Valverde.CharDown)
                migrationBuilder.Sql(statement);
        }
    }
}
