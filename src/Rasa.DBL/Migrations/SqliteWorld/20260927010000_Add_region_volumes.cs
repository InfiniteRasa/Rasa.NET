using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

namespace Rasa.Migrations.SqliteWorld
{
    using Services.Preloader;
    using Structures.World;

    /// <summary>
    /// 225 more region volumes, from each region's display name matched to a marker on the map
    /// screen (MapRegionMarkerPreloader). Before this 359 of the client's 1,256 regions could
    /// ever apply; with it 584 can. The places this reaches include Alia Caverns, Stone Anvil,
    /// Pinhole Falls, Nidu Dav, Thoria Das, Delta and Foxtrot Outposts and the Landing Zone
    /// control point, each now switching to its own ambience, music and sky.
    ///
    /// Down deletes the closed id range these rows occupy.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    public partial class Add_region_volumes : Migration
    {
        private const uint IdMin = 20001;
        private const uint IdMax = 20999;

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            new MapRegionMarkerPreloader().Preload(migrationBuilder);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($"delete from {MapRegionEntry.TableName} where id between {IdMin} and {IdMax};");
        }
    }
}
