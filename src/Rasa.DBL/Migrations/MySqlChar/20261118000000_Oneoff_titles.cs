using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

namespace Rasa.Migrations.MySqlChar
{
    using Context.Char;
    using Services.Preloader;

    /// <summary>
    /// The objectives Oneoff_titles adds to three Targets of Opportunity missions in the world
    /// database (1585, 1752, 1809), for every character holding one of them: a mission held
    /// with fewer objectives than it has is cleared on login. Their other counts stay. And the
    /// Wilderness Spelunker for a character who has visited every cave already
    /// (TargetsOfOpportunitySeed.OneOffs). No table changes.
    ///
    /// Down takes the objective rows out again. Titles stay.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    [DbContext(typeof(MySqlCharContext))]
    [Migration("20261118000000_Oneoff_titles")]
    public partial class Oneoff_titles : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var statement in TargetsOfOpportunitySeed.OneOffs.CharUp)
                migrationBuilder.Sql(statement);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var statement in TargetsOfOpportunitySeed.OneOffs.CharDown)
                migrationBuilder.Sql(statement);
        }
    }
}
