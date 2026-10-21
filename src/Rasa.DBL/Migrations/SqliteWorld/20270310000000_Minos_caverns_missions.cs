using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

using Rasa.Context.World;
using Rasa.Services.Preloader;

namespace Rasa.Migrations.SqliteWorld
{
    /// <summary>
    /// The four Minos Caverns missions (MinosCavernsMissions): Retrieval for Recon, Data Thieves,
    /// Cave Extraction and Destroying the Warnet Barrier, on the people Minos_caverns_content
    /// placed. Data only, in plain SQL: no model of its own.
    ///
    /// Down takes the rows out again.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    [DbContext(typeof(SqliteWorldContext))]
    [Migration("20270310000000_Minos_caverns_missions")]
    public class Minos_caverns_missions : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in MinosCavernsMissions.Up())
                migrationBuilder.Sql(sql);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in MinosCavernsMissions.Down())
                migrationBuilder.Sql(sql);
        }
    }
}
