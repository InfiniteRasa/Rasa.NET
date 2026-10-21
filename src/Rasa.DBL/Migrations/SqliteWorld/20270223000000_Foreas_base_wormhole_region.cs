using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

using Rasa.Context.World;
using Rasa.Services.Preloader;

namespace Rasa.Migrations.SqliteWorld
{
    /// <summary>
    /// Foreas Base's wormhole clearing on Concordia Divide given its volume
    /// (ForeasBaseWormholeRegion). Data only, in plain SQL: no model of its own.
    ///
    /// Down takes the row out again.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    [DbContext(typeof(SqliteWorldContext))]
    [Migration("20270223000000_Foreas_base_wormhole_region")]
    public class Foreas_base_wormhole_region : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in ForeasBaseWormholeRegion.Up())
                migrationBuilder.Sql(sql);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in ForeasBaseWormholeRegion.Down())
                migrationBuilder.Sql(sql);
        }
    }
}
