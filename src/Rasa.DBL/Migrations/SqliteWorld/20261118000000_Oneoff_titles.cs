using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

namespace Rasa.Migrations.SqliteWorld
{
    using Context.World;
    using Services.Preloader;

    /// <summary>
    /// Four titles of a kind of their own, each on an objective of a Targets of Opportunity
    /// mission that is here already (TargetsOfOpportunitySeed.OneOffs, which says what and why):
    /// Wilderness Spelunker on 1449's caves, Mires Explorer on 1585's operations, the Undertaker on
    /// 1752 and Palisades Stalker Killer on 1809. Objectives, areas and scene bindings; no table
    /// changes.
    ///
    /// Down deletes the rows and puts the four bindings back. The character database has the
    /// other half (its own Oneoff_titles).
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    [DbContext(typeof(SqliteWorldContext))]
    [Migration("20261118000000_Oneoff_titles")]
    public partial class Oneoff_titles : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var statement in TargetsOfOpportunitySeed.OneOffs.WorldUp)
                migrationBuilder.Sql(statement);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var statement in TargetsOfOpportunitySeed.OneOffs.WorldDown)
                migrationBuilder.Sql(statement);
        }
    }
}
