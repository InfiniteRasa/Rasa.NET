using System.Linq;
using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Wilderness
{
    public static class WildernessSupportedRewardsV1
    {
        private const uint EvidenceId = 100;
        private static readonly (uint Mission, uint Item, uint Before, uint After)[] Rows =
        {
            (1390, 1, 111247, 44918),
            (422, 1, 111247, 44918),
            (432, 2, 111022, 44918),
            (434, 2, 111247, 44918),
            (436, 2, 111022, 44918),
            (549, 1, 47593, 44917),
            (549, 2, 45125, 44918),
            (441, 1, 45125, 44918)
        };

        public static void Up(MigrationBuilder migration)
        {
            foreach (var row in Rows)
                Update(migration, row.Mission, row.Item, row.After);
            foreach (var mission in Rows.GroupBy(row => row.Mission))
                migration.InsertData("mission_evidence",
                    new[]
                    {
                        "mission_id", "content_revision", "evidence_id", "owner_kind", "owner_id",
                        "source_kind", "source_uri", "local_client_path", "confidence", "reconstruction_note"
                    },
                    new object[]
                    {
                        mission.Key, WildernessMissionDataV1.Revision, EvidenceId,
                        (byte)MissionEvidenceOwnerKind.Reward, 1U,
                        (byte)MissionEvidenceSourceKind.Reconstruction,
                        "repository:WildernessSupportedRewardsV1", null, 0.5,
                        $"R: {string.Join(", ", mission.Select(row => $"{row.Before}->{row.After}"))}; " +
                        "native action419 healing replaces unsupported effects. XP, credits, selection and each quantity " +
                        "stay unchanged. Supersedes earlier reward-effect claims; no original-effect parity."
                    });
        }

        public static void Down(MigrationBuilder migration)
        {
            foreach (var row in Rows)
                Update(migration, row.Mission, row.Item, row.Before);
            foreach (var mission in Rows.Select(row => row.Mission).Distinct())
                migration.DeleteData("mission_evidence",
                    new[] { "mission_id", "content_revision", "evidence_id" },
                    new object[] { mission, WildernessMissionDataV1.Revision, EvidenceId });
        }

        private static void Update(MigrationBuilder migration, uint mission, uint item, uint template) =>
            migration.UpdateData("mission_reward_item",
                new[] { "mission_id", "content_revision", "reward_id", "item_id" },
                new object[] { mission, WildernessMissionDataV1.Revision, 1U, item },
                "item_template_id", template);
    }
}
