using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

using Rasa.Context.World;
using Rasa.Services.Preloader;

namespace Rasa.Migrations.SqliteWorld
{
    /// <summary>
    /// Tahrendra Base's rooms, prison blocks and levels given their volumes
    /// (TahrendraBaseRegions). Data only, in plain SQL: no model of its own.
    ///
    /// Down puts the seeded rows back and takes the new ones out.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    [DbContext(typeof(SqliteWorldContext))]
    [Migration("20270622000000_Tahrendra_base_regions")]
    public class Tahrendra_base_regions : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in TahrendraBaseRegions.Up())
                migrationBuilder.Sql(sql);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in TahrendraBaseRegions.Down())
                migrationBuilder.Sql(sql);
        }
    }
}
