using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

using Rasa.Context.World;
using Rasa.Services.Preloader;

namespace Rasa.Migrations.MySqlWorld
{
    /// <summary>
    /// Mount Hellas Outpost and Atta Territory given their volumes
    /// (HellasAttaRegions). Data only, in plain SQL: no model of its own.
    ///
    /// Down puts the seeded row back and takes the new ones out.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    [DbContext(typeof(MySqlWorldContext))]
    [Migration("20270627000000_Hellas_atta_regions")]
    public class Hellas_atta_regions : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in HellasAttaRegions.Up())
                migrationBuilder.Sql(sql);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in HellasAttaRegions.Down())
                migrationBuilder.Sql(sql);
        }
    }
}
