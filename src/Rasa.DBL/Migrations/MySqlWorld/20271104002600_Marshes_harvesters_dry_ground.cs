using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Marshes;

#nullable disable

namespace Rasa.Migrations.MySqlWorld
{
    /// <summary>
    /// Valverde Marshes, to-fix row 20: Reaping the Harvest's and Hassling the Harvesters' seven harvester places move out of
    /// the Northern Mud Pit's water onto dry ground (MarshesHarvesterPlacesV2; MarshesReapingTheHarvestV2 and
    /// MarshesHasslingTheHarvestersV2 in place of their V1). Down puts back V1. Data only.
    /// </summary>
    public partial class Marshes_harvesters_dry_ground : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            MarshesHasslingTheHarvestersV1.Down(migrationBuilder);
            MarshesReapingTheHarvestV1.Down(migrationBuilder);
            MarshesReapingTheHarvestV2.Up(migrationBuilder);
            MarshesHasslingTheHarvestersV2.Up(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            MarshesHasslingTheHarvestersV2.Down(migrationBuilder);
            MarshesReapingTheHarvestV2.Down(migrationBuilder);
            MarshesReapingTheHarvestV1.Up(migrationBuilder);
            MarshesHasslingTheHarvestersV1.Up(migrationBuilder);
        }
    }
}
