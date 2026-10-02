using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

namespace Rasa.Migrations.SqliteWorld
{
    using Context.World;
    using Services.Preloader;

    /// <summary>
    /// The Targets of Opportunity missions of the fifteen battlefields with their kill
    /// objectives (TargetsOfOpportunitySeed): 15 missions, their 63 kill objectives each with
    /// its counter, its title, the maps it counts on and squad credit, and the radio offer on
    /// arriving on the battlefield. No table changes.
    ///
    /// Down deletes the rows by mission and revision. A character who holds one of the missions
    /// keeps the assignment, which then names no mission the server has.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    [DbContext(typeof(SqliteWorldContext))]
    [Migration("20261108000000_Add_targets_of_opportunity")]
    public partial class Add_targets_of_opportunity : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var insert in TargetsOfOpportunitySeed.InsertStatements)
                migrationBuilder.Sql(insert);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var delete in TargetsOfOpportunitySeed.DeleteStatements)
                migrationBuilder.Sql(delete);
        }
    }
}
