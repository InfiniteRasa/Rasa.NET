using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

using Rasa.Context.World;
using Rasa.Services.Preloader;

namespace Rasa.Migrations.MySqlWorld
{
    /// <summary>
    /// Fourteen seeded label circles that put an interior or cave sky over open ground are
    /// disabled: Fort Haroun, Bane Occupied Mires, Irendas Penal Colony, Thunderhead Base, AFS
    /// Substation E-104, Rivasa Atta Colony, Mal Dys, Antaeus Hollow, Staging Point, Daghda's Urn
    /// Village and the two wargame Staging Areas (RegionSkyCircles). Data only, in plain SQL: no
    /// model of its own.
    ///
    /// Down enables them again.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    [DbContext(typeof(MySqlWorldContext))]
    [Migration("20270116000000_Region_sky_circles")]
    public class Region_sky_circles : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in RegionSkyCircles.Up())
                migrationBuilder.Sql(sql);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var sql in RegionSkyCircles.Down())
                migrationBuilder.Sql(sql);
        }
    }
}
