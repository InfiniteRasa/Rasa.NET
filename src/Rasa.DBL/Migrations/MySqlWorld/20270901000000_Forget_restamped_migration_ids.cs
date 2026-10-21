using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

using Rasa.Context.World;
using Rasa.Services.Preloader;

namespace Rasa.Migrations.MySqlWorld
{
    /// <summary>
    /// The history rows of Logos_shrine_heights' and Wilderness_urn_enigma_regions' old ids taken
    /// off (RestampedMigrationIds). Data only, in plain SQL: no model of its own.
    ///
    /// Down puts nothing back.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    [DbContext(typeof(MySqlWorldContext))]
    [Migration("20270901000000_Forget_restamped_migration_ids")]
    public class Forget_restamped_migration_ids : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in RestampedMigrationIds.Up())
                migrationBuilder.Sql(sql);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
