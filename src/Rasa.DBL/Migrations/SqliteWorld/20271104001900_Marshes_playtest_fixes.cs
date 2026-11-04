using Microsoft.EntityFrameworkCore.Migrations;

using Rasa.Services.Preloader.Missions.Marshes;

#nullable disable

namespace Rasa.Migrations.SqliteWorld
{
    /// <summary>
    /// Valverde Marshes, to-fix 13: fixes that first went into the data of migrations the live server had applied, written
    /// again as their V2 classes: Survivors (664, set 16, to-fix 5), Freedom's Just Another Word (548, set 13, to-fix 7), Slay
    /// on Target (718, set 13, to-fix 9), Head East, Young Man (920, set 12, to-fix 10's approach) and Needle in a Bane Camp
    /// and The Choices We Make (893 and 1078, set 14, one choice per player). Down writes the V1 missions again. Data only.
    /// </summary>
    public partial class Marshes_playtest_fixes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            MarshesSurvivorsV1.Down(migrationBuilder);
            MarshesSurvivorsV2.Up(migrationBuilder);
            MarshesFreedomsJustAnotherWordV1.Down(migrationBuilder);
            MarshesFreedomsJustAnotherWordV2.Up(migrationBuilder);
            MarshesSlayOnTargetV1.Down(migrationBuilder);
            MarshesSlayOnTargetV2.Up(migrationBuilder);
            MarshesHeadEastV1.Down(migrationBuilder);
            MarshesHeadEastV2.Up(migrationBuilder);
            MarshesNeedleInABaneCampV1.Down(migrationBuilder);
            MarshesNeedleInABaneCampV2.Up(migrationBuilder);
            MarshesTheChoicesWeMakeV1.Down(migrationBuilder);
            MarshesTheChoicesWeMakeV2.Up(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            MarshesTheChoicesWeMakeV2.Down(migrationBuilder);
            MarshesTheChoicesWeMakeV1.Up(migrationBuilder);
            MarshesNeedleInABaneCampV2.Down(migrationBuilder);
            MarshesNeedleInABaneCampV1.Up(migrationBuilder);
            foreach (var sql in MarshesSafeGroundV1.MarkersOf(MarshesNeedleInABaneCampV1.MissionId))
                migrationBuilder.Sql(MarshesSql.Guard(sql));   // the elder's marker as Marshes_safe_ground left it
            MarshesHeadEastV2.Down(migrationBuilder);
            MarshesHeadEastV1.Up(migrationBuilder);
            MarshesSlayOnTargetV2.Down(migrationBuilder);
            MarshesSlayOnTargetV1.Up(migrationBuilder);
            MarshesFreedomsJustAnotherWordV2.Down(migrationBuilder);
            MarshesFreedomsJustAnotherWordV1.Up(migrationBuilder);
            MarshesSurvivorsV2.Down(migrationBuilder);
            MarshesSurvivorsV1.Up(migrationBuilder);
        }
    }
}
