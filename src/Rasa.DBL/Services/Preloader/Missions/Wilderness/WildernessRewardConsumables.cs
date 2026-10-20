using System.Collections.Generic;
using System.Linq;

namespace Rasa.Services.Preloader.Missions.Wilderness
{
    using Structures.World;

    /// <summary>
    /// The Wilderness rewards get back the bombs, grenades, Res Trauma Kits and Adrenaline Booster
    /// their sources list, where med packs stood in for them (Wilderness_reward_consumables).
    ///
    /// When the missions were authored none of those had an effect on the server: using one was
    /// refused as not implemented and the item kept. WildernessSupportedRewardsV1 (missions 422,
    /// 432, 434, 436, 441, 549, 1390) and the first Twin Pillars and Ranja Gorge migrations (758,
    /// 769, 787, 791) gave a med pack of the same quantity in their place. Each now resolves:
    /// grenades (CONSUMABLE_GRENADE 420: a level 1-10 one physical, 11-20 EMP, so the Class I EMP
    /// Grenade, level 11, is EMP), EMP Bombs (CONSUMABLE_BOMB 10000004), Res Trauma Kits
    /// (CONSUMABLE_RES_TRAUMA_KIT 427) and the Adrenaline Booster. So each reward row holds the
    /// source's item again, its quantity, kind (fixed or chosen) and the rest of the reward as
    /// they were.
    ///
    /// Not here: 623's Fire Resistance modification (module item 123023), which is a module rather
    /// than a consumable, keeps its Advanced Med Pack.
    ///
    /// Each row changes only while it holds the med pack it was given; a row a GM changed by hand
    /// is left alone. An evidence row per mission (101) records it; Down puts the med packs back,
    /// again only where the source's item still stands, and takes the evidence out. No item a
    /// character already holds is changed.
    /// </summary>
    public static class WildernessRewardConsumables
    {
        public const string Revision = WildernessMissionDataV1.Revision;
        public const uint RewardId = 1;
        public const uint EvidenceId = 101;

        public sealed record Swap(uint Mission, uint Item, uint MedPack, uint Source, uint Quantity, string What);

        public static readonly Swap[] Swaps =
        {
            new(422, 1, 44918, 111247, 1, "Class I EMP Bomb"),
            new(432, 2, 44918, 111022, 1, "Class I EMP Grenade"),
            new(434, 2, 44918, 111247, 1, "Class I EMP Bomb"),
            new(436, 2, 44918, 111022, 1, "Class I EMP Grenade"),
            new(441, 1, 44918, 45125, 1, "Class I Fragmentation Grenade"),
            new(549, 1, 44917, 47593, 1, "Class I Res Trauma Kit"),
            new(549, 2, 44918, 45125, 1, "Class I Fragmentation Grenade"),
            new(758, 1, 44920, 111227, 2, "Class II EMP Bomb"),
            new(769, 2, 44920, 111227, 2, "Class II EMP Bomb"),
            new(787, 1, 44919, 47594, 2, "Class II Res Trauma Kit"),
            new(787, 2, 44920, 45441, 6, "Class II Fragmentation Grenade"),
            new(791, 1, 44918, 111247, 1, "Class I EMP Bomb"),
            new(791, 2, 44917, 111136, 1, "Class I Adrenaline Booster"),
            new(1390, 1, 44918, 111247, 1, "Class I EMP Bomb"),
        };

        public static IEnumerable<uint> Missions => Swaps.Select(s => s.Mission).Distinct();

        /// <summary>The statements, one each, as MigrationBuilder.Sql takes them.</summary>
        public static IEnumerable<string> Up()
        {
            foreach (var s in Swaps)
                yield return Change(s, s.MedPack, s.Source);

            foreach (var mission in Swaps.GroupBy(s => s.Mission))
                yield return "INSERT INTO mission_evidence (mission_id, content_revision, evidence_id, confidence, local_client_path, owner_id, owner_kind, " +
                    "reconstruction_note, source_kind, source_uri) " +
                    $"SELECT {mission.Key}, {S(Revision)}, {EvidenceId}, 0.5, NULL, {RewardId}, {(byte)MissionEvidenceOwnerKind.Reward}, " +
                    $"{S(Note(mission))}, {(byte)MissionEvidenceSourceKind.Reconstruction}, {S("repository:WildernessRewardConsumables")} " +
                    $"FROM mission_content_definition d WHERE d.mission_id = {mission.Key} AND d.content_revision = {S(Revision)} " +
                    $"AND NOT EXISTS (SELECT 1 FROM mission_evidence x WHERE x.mission_id = {mission.Key} AND x.content_revision = {S(Revision)} AND x.evidence_id = {EvidenceId});";
        }

        public static IEnumerable<string> Down()
        {
            foreach (var mission in Missions)
                yield return $"DELETE FROM mission_evidence WHERE mission_id = {mission} AND content_revision = {S(Revision)} AND evidence_id = {EvidenceId};";

            foreach (var s in Swaps)
                yield return Change(s, s.Source, s.MedPack);
        }

        private static string Note(IEnumerable<Swap> swaps) =>
            $"S restored: {string.Join(", ", swaps.Select(s => $"{s.MedPack}->{s.Source} {s.What} x{s.Quantity}"))}. " +
            "The source's consumable now resolves on the server, so the med pack that stood in for it goes; " +
            "XP, credits, selection and each quantity unchanged. Supersedes WildernessSupportedRewardsV1's and the provider substitutes.";

        private static string Change(Swap s, uint from, uint to) =>
            $"UPDATE mission_reward_item SET item_template_id = {to} " +
            $"WHERE mission_id = {s.Mission} AND content_revision = {S(Revision)} AND reward_id = {RewardId} AND item_id = {s.Item} " +
            $"AND item_template_id = {from} AND quantity = {s.Quantity};";

        private static string S(string value) => "'" + value.Replace("'", "''") + "'";
    }
}
