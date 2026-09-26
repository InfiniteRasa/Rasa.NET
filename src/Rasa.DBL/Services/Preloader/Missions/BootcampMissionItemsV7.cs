using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;

namespace Rasa.Services.Preloader.Missions
{
    public static class BootcampMissionItemsV7
    {
        public static MissionSceneDefinition Scene(uint missionId)
        {
            var scene = missionId == 1995 ? BootcampCorpseDialogueV6.Scene() : BootcampExtractionDataV5.Scene(missionId);
            scene.Items = new()
            {
                new MissionItemBinding("bomb", 11519, MissionItemScope.AssignmentIssued, 1,
                    MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove,
                    MissionItemCleanupDisposition.Remove)
            };
            if (missionId == 2005)
                scene.AcceptanceItems = new() { new IssueMissionItemIntent("accept-bomb", 2005, "bomb", 11519, 1) };
            return scene;
        }

        public static void Up(MigrationBuilder migration)
        {
            MissionDataMigration.UpdateScene(migration, 1995, BootcampMissionDataV1.Revision, Scene(1995));
            MissionDataMigration.UpdateScene(migration, 2005, BootcampMissionDataV1.Revision, Scene(2005));
            Action(migration, 1995, 3, 4, 10,
                new IssueMissionItemIntent("issue-bomb", 1995, "bomb", 11519, 1));
            foreach (var mission in new[] { 1995U, 2005U })
                Action(migration, mission, 1, 2, 11,
                    new ConsumeMissionItemIntent("plant-bomb", mission, "bomb", 1, MissionItemScope.AssignmentIssued));
        }

        public static void Down(MigrationBuilder migration)
        {
            migration.Sql("delete from mission_action where mission_id in (1995, 2005) " +
                "and content_revision = 'deployment_11' and kind in (10, 11, 12);");
            MissionDataMigration.UpdateScene(migration, 1995, BootcampMissionDataV1.Revision, BootcampCorpseDialogueV6.Scene());
            MissionDataMigration.UpdateScene(migration, 2005, BootcampMissionDataV1.Revision, BootcampExtractionDataV5.Scene(2005));
        }

        private static void Action(MigrationBuilder migration, uint mission, uint objective, uint actionId,
            byte kind, CharacterIntent intent) =>
            migration.InsertData("mission_action",
                new[] { "mission_id", "content_revision", "objective_id", "transition_id", "action_id",
                    "requirement", "kind", "sequence", "comment", "item_intent" },
                new object[] { mission, BootcampMissionDataV1.Revision, objective, 1U, actionId, (byte)1, kind,
                    actionId, "Assignment-owned bomb lifecycle", JsonSerializer.Serialize(intent, MissionContentCodec.Options) });
    }
}
