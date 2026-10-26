using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

using Rasa.Context.World;
using Rasa.Services.Preloader;

namespace Rasa.Migrations.SqliteWorld
{
    /// <summary>
    /// The Caves of Donn's people, Bane, egg clusters and missions (CavesOfDonnContent). Data
    /// only, in plain SQL: no model of its own.
    ///
    /// Down takes the rows out again and gives Wilderness Targets of Opportunity back Body Count.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    [DbContext(typeof(SqliteWorldContext))]
    [Migration("20270217000000_Caves_of_donn")]
    public class Caves_of_donn : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in CavesOfDonnContent.Up())
                migrationBuilder.Sql(sql);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in CavesOfDonnContent.Down())
                migrationBuilder.Sql(sql);
        }
    }
}
