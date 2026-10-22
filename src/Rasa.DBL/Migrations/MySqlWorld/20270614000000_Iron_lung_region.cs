using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

using Rasa.Context.World;
using Rasa.Services.Preloader;

namespace Rasa.Migrations.MySqlWorld
{
    /// <summary>
    /// The Iron Lung, Shadow's Edge Post's tavern, given its volume
    /// (IronLungRegion). Data only, in plain SQL: no model of its own.
    ///
    /// Down takes the row out again.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    [DbContext(typeof(MySqlWorldContext))]
    [Migration("20270614000000_Iron_lung_region")]
    public class Iron_lung_region : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in IronLungRegion.Up())
                migrationBuilder.Sql(sql);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in IronLungRegion.Down())
                migrationBuilder.Sql(sql);
        }
    }
}
