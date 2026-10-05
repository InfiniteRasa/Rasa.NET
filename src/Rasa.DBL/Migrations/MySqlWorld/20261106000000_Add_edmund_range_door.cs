using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

namespace Rasa.Migrations.MySqlWorld
{
    using Context.World;
    using Services.Preloader;

    /// <summary>
    /// The door from the CELLAR Arena into Edmund Range and the way back (EdmundRangeDoor): two
    /// map_link rows, 9001 and 9002.
    ///
    /// Down deletes the two rows.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    [DbContext(typeof(MySqlWorldContext))]
    [Migration("20261106000000_Add_edmund_range_door")]
    public partial class Add_edmund_range_door : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var insert in EdmundRangeDoor.InsertStatements)
                migrationBuilder.Sql(insert);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(EdmundRangeDoor.DeleteStatement);
        }
    }
}
