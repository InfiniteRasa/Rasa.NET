using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

using Rasa.Context.World;
using Rasa.Services.Preloader;

namespace Rasa.Migrations.SqliteWorld
{
    /// <summary>
    /// Dybukkar Garrison's rooms given their volumes
    /// (DybukkarGarrisonRegions). Data only, in plain SQL: no model of its own.
    ///
    /// Down takes the new rows out again and gives the label circles back.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    [DbContext(typeof(SqliteWorldContext))]
    [Migration("20270606000000_Dybukkar_garrison_regions")]
    public class Dybukkar_garrison_regions : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in DybukkarGarrisonRegions.Up())
                migrationBuilder.Sql(sql);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in DybukkarGarrisonRegions.Down())
                migrationBuilder.Sql(sql);
        }
    }
}
