using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

using Rasa.Context.World;
using Rasa.Services.Preloader;

namespace Rasa.Migrations.SqliteWorld
{
    /// <summary>
    /// The Devil's Den's people, creatures, usable objects, Bane Communications Tower region and
    /// missions (DevilsDenContent). Data only, in plain SQL: no model of its own.
    ///
    /// Down takes the rows out again and gives the Den's pools back the Palisades' rows.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    [DbContext(typeof(SqliteWorldContext))]
    [Migration("20270305000000_Devils_den")]
    public class Devils_den : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in DevilsDenContent.Up())
                migrationBuilder.Sql(sql);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in DevilsDenContent.Down())
                migrationBuilder.Sql(sql);
        }
    }
}
