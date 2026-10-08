using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

namespace Rasa.Migrations.SqliteWorld
{
    using Context.World;
    using Services.Preloader;

    /// <summary>
    /// The creatures that can go nowhere, put on the floor under them: 185 spawn pools given
    /// their floor's height and 15 moved out of the furniture they were entered in
    /// (StandingHeights, which says how each was measured and what is left alone). No table
    /// changes.
    ///
    /// Down puts each row back.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    [DbContext(typeof(SqliteWorldContext))]
    [Migration("20261124000000_Stand_npcs_on_their_floors")]
    public partial class Stand_npcs_on_their_floors : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var statement in StandingHeights.WorldUp)
                migrationBuilder.Sql(statement);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var statement in StandingHeights.WorldDown)
                migrationBuilder.Sql(statement);
        }
    }
}
