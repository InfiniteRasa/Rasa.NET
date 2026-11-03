using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

using Rasa.Context.World;
using Rasa.Services.Preloader;

namespace Rasa.Migrations.MySqlWorld
{
    /// <summary>
    /// Guardian Prominence's caves, ruins and the drop below it given their volumes
    /// (GuardianProminenceRegions). Data only, in plain SQL: no model of its own.
    ///
    /// Down puts the ruins' circle back and takes the new rows out.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    [DbContext(typeof(MySqlWorldContext))]
    [Migration("20270704000000_Guardian_prominence_regions")]
    public class Guardian_prominence_regions : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in GuardianProminenceRegions.Up())
                migrationBuilder.Sql(sql);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in GuardianProminenceRegions.Down())
                migrationBuilder.Sql(sql);
        }
    }
}
