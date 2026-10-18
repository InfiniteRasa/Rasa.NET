using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

using Rasa.Context.World;
using Rasa.Services.Preloader;

namespace Rasa.Migrations.SqliteWorld
{
    /// <summary>
    /// The Proving Grounds' outdoors under the Caldera's daylight sky: region 4's volume covers the
    /// map west of the cavern, and region 5's, a building interior's sky, is disabled
    /// (BootcampCalderaSky). Data only, in plain SQL: no model of its own.
    ///
    /// Down puts the seeded circle back and enables region 5's again.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    [DbContext(typeof(SqliteWorldContext))]
    [Migration("20270115000000_Bootcamp_caldera_sky")]
    public class Bootcamp_caldera_sky : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in BootcampCalderaSky.Up())
                migrationBuilder.Sql(sql);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in BootcampCalderaSky.Down())
                migrationBuilder.Sql(sql);
        }
    }
}
