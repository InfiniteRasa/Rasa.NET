using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

namespace Rasa.Migrations.SqliteChar
{
    using Context.Char;
    using Services.Preloader;

    /// <summary>
    /// The items made in the old default colour, and the character_appearance rows that show
    /// them, in the new one: white (ItemDefaultColor). No table changes.
    ///
    /// Down puts each white row back to the old grey.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    [DbContext(typeof(SqliteCharContext))]
    [Migration("20270130000000_Item_default_color")]
    public partial class Item_default_color : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var statement in ItemDefaultColor.CharUp)
                migrationBuilder.Sql(statement);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var statement in ItemDefaultColor.CharDown)
                migrationBuilder.Sql(statement);
        }
    }
}
