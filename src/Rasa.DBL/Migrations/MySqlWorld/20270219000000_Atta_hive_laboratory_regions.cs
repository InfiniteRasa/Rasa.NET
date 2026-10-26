using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

using Rasa.Context.World;
using Rasa.Services.Preloader;

namespace Rasa.Migrations.MySqlWorld
{
    /// <summary>
    /// Avernus Outpost's Atta Hives and the Conscription Facility's laboratory, maintenance and
    /// processing areas given volumes (AttaHiveLaboratoryRegions). Data only, in plain SQL: no
    /// model of its own.
    ///
    /// Down takes the new rows out and gives the recut circles back.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    [DbContext(typeof(MySqlWorldContext))]
    [Migration("20270219000000_Atta_hive_laboratory_regions")]
    public class Atta_hive_laboratory_regions : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in AttaHiveLaboratoryRegions.Up())
                migrationBuilder.Sql(sql);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in AttaHiveLaboratoryRegions.Down())
                migrationBuilder.Sql(sql);
        }
    }
}
