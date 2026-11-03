using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

using Rasa.Context.World;
using Rasa.Services.Preloader;

namespace Rasa.Migrations.MySqlWorld
{
    /// <summary>
    /// Ligo Thunderhead's Eloh chambers, fault chasm, Brimstone passage and Brann camps given their
    /// volumes (ThunderheadRegions). Data only, in plain SQL: no model of its own.
    ///
    /// Down takes the new rows out.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    [DbContext(typeof(MySqlWorldContext))]
    [Migration("20270709000000_Thunderhead_regions")]
    public class Thunderhead_regions : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in ThunderheadRegions.Up())
                migrationBuilder.Sql(sql);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in ThunderheadRegions.Down())
                migrationBuilder.Sql(sql);
        }
    }
}
