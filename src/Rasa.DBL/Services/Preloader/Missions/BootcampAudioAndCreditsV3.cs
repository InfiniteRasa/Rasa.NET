using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Missions.Content;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions
{
    public static class BootcampAudioAndCreditsV3
    {
        private const string Revision = BootcampMissionDataV1.Revision;

        public static void Up(MigrationBuilder migration)
        {
            var initiation = BootcampMissionDataV1.Mission1990();
            initiation.Audio = new MissionAudioDefinition
            {
                OfferAudioSetId = 2773,
                Events = new() { [MissionAudioEvent.Completed] = 2777 },
                Announcements = new() { [1634] = 2788, [1636] = 2789 }
            };
            var gearing = BootcampMissionDataV1.Mission1992();
            gearing.Audio = new MissionAudioDefinition { OfferAudioSetId = 2774 };
            var capture = BootcampMissionDataV1.Mission1994();
            capture.Audio = new MissionAudioDefinition { OfferAudioSetId = 2775 };
            var finale = BootcampFinaleDataV2.Reinforcements();
            finale.Audio = new MissionAudioDefinition { OfferAudioSetId = 2776 };
            MissionDataMigration.UpdateScene(migration, 1990, Revision, initiation);
            MissionDataMigration.UpdateScene(migration, 1992, Revision, gearing);
            MissionDataMigration.UpdateScene(migration, 1994, Revision, capture);
            MissionDataMigration.UpdateScene(migration, 1995, Revision, finale);

            foreach (var entry in new[] { (Mission: 1990U, Credits: 100U), (Mission: 1992U, Credits: 200U), (Mission: 1994U, Credits: 200U) })
                migration.UpdateData("mission_reward_definition",
                    new[] { "mission_id", "content_revision", "reward_id" },
                    new object[] { entry.Mission, Revision, 1U }, "credits", entry.Credits);
            foreach (var mission in new uint[] { 1995, 2005 })
            {
                var requirement = mission == 1995 ? MissionContentRequirement.Required : MissionContentRequirement.Optional;
                migration.InsertData("mission_reward_definition",
                    new[] { "mission_id", "content_revision", "reward_id", "requirement", "experience",
                        "credits", "prestige", "selection_count", "comment" },
                    new object[] { mission, Revision, 1U, (byte)requirement, 0U, 200U, 0U, (byte)0, "Bootcamp finale credit reward" });
                migration.InsertData("mission_action",
                    new[] { "mission_id", "content_revision", "objective_id", "transition_id", "action_id",
                        "requirement", "kind", "sequence", "reward_id", "comment" },
                    new object[] { mission, Revision, 4U, 1U, 3U, (byte)requirement,
                        (byte)MissionActionKind.GrantReward, 3U, 1U, "Reference finale turn-in reward" });
            }
        }

        public static void Down(MigrationBuilder migration)
        {
            foreach (var mission in new uint[] { 1995, 2005 })
            {
                migration.DeleteData("mission_action",
                    new[] { "mission_id", "content_revision", "objective_id", "transition_id", "action_id" },
                    new object[] { mission, Revision, 4U, 1U, 3U });
                migration.DeleteData("mission_reward_definition",
                    new[] { "mission_id", "content_revision", "reward_id" }, new object[] { mission, Revision, 1U });
            }
            foreach (var mission in new uint[] { 1990, 1994 })
                migration.UpdateData("mission_reward_definition",
                    new[] { "mission_id", "content_revision", "reward_id" },
                    new object[] { mission, Revision, 1U }, "credits", 0U);
            MissionDataMigration.UpdateScene(migration, 1990, Revision, BootcampMissionDataV1.Mission1990());
            MissionDataMigration.UpdateScene(migration, 1992, Revision, BootcampMissionDataV1.Mission1992());
            MissionDataMigration.UpdateScene(migration, 1994, Revision, BootcampMissionDataV1.Mission1994());
            MissionDataMigration.UpdateScene(migration, 1995, Revision, BootcampFinaleDataV2.Reinforcements());
        }
    }
}
