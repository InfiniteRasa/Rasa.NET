using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

using Rasa.Context.World;
using Rasa.Services.Preloader;

namespace Rasa.Migrations.MySqlWorld
{
    /// <summary>
    /// Daghda's Urn Village, Enigma Falls and Enigma Cavern in the Wilderness given volumes
    /// (WildernessUrnEnigmaRegions). Data only, in plain SQL: no model of its own.
    ///
    /// Down takes the new rows out and gives region 48 its circle back.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    [DbContext(typeof(MySqlWorldContext))]
    [Migration("20270306000000_Wilderness_urn_enigma_regions")]
    public class Wilderness_urn_enigma_regions : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in WildernessUrnEnigmaRegions.Up())
                migrationBuilder.Sql(sql);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in WildernessUrnEnigmaRegions.Down())
                migrationBuilder.Sql(sql);
        }
    }
}
