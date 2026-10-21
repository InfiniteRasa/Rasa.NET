using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

using Rasa.Context.World;
using Rasa.Services.Preloader;

namespace Rasa.Migrations.SqliteWorld
{
    /// <summary>
    /// Minos Caverns populated (MinosCavernsContent): its warnets, miasmas and nests, the Bane
    /// base's Thrax, Kael, cameras, databanks, alarm lights, siren, charged doors and crates, its
    /// NPCs and the two mission givers on the Divide. Data only, in plain SQL: no model of its own.
    ///
    /// Down takes the rows out again and gives PR105's Thrax pools back what they had.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    [DbContext(typeof(SqliteWorldContext))]
    [Migration("20270309000000_Minos_caverns_content")]
    public class Minos_caverns_content : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in MinosCavernsContent.Up())
                migrationBuilder.Sql(sql);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in MinosCavernsContent.Down())
                migrationBuilder.Sql(sql);
        }
    }
}
