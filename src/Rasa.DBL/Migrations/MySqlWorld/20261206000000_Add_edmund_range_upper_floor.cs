using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

namespace Rasa.Migrations.MySqlWorld
{
    using Context.World;
    using Services.Preloader;

    /// <summary>
    /// The staging area's upper floor in Edmund Range (EdmundRangeSeed.UpperFloorMapLinks): its
    /// Red and Blue teleporters, 9007 and 9008, and where the winning team of a match is put,
    /// 9009. No table changes.
    ///
    /// Down deletes the three rows.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    [DbContext(typeof(MySqlWorldContext))]
    [Migration("20261206000000_Add_edmund_range_upper_floor")]
    public partial class Add_edmund_range_upper_floor : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var insert in EdmundRangeSeed.UpperFloorInsertStatements)
                migrationBuilder.Sql(insert);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var delete in EdmundRangeSeed.UpperFloorDeleteStatements)
                migrationBuilder.Sql(delete);
        }
    }
}
