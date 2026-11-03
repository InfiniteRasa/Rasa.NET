using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

using Rasa.Context.World;
using Rasa.Services.Preloader;

namespace Rasa.Migrations.SqliteWorld
{
    /// <summary>
    /// Eloh Space's two boxes widened to the whole of its shrines
    /// (ElohSpaceShrines). Data only, in plain SQL: no model of its own.
    ///
    /// Down puts the two boxes back as they were.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    [DbContext(typeof(SqliteWorldContext))]
    [Migration("20270630000000_Eloh_space_shrines")]
    public class Eloh_space_shrines : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in ElohSpaceShrines.Up())
                migrationBuilder.Sql(sql);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in ElohSpaceShrines.Down())
                migrationBuilder.Sql(sql);
        }
    }
}
