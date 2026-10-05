using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

namespace Rasa.Migrations.MySqlWorld
{
    using Context.World;
    using Services.Preloader;

    /// <summary>
    /// What the Edmund Range match stands on (EdmundRangeSeed): the four Simulated Bane, their
    /// six spawn pools, the control points Whiskey, Charlie and Echo with their links, and the
    /// four map links of the team teleporters and the ways back to the staging area. No table
    /// changes.
    ///
    /// Down deletes the rows by their id ranges.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    [DbContext(typeof(MySqlWorldContext))]
    [Migration("20261107000000_Add_edmund_range_match")]
    public partial class Add_edmund_range_match : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var insert in EdmundRangeSeed.InsertStatements)
                migrationBuilder.Sql(insert);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var delete in EdmundRangeSeed.DeleteStatements)
                migrationBuilder.Sql(delete);
        }
    }
}
