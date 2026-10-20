using System.Collections.Generic;
using System.Linq;

namespace Rasa.Services.Preloader.Missions.Wilderness
{
    using Structures.World;

    /// <summary>
    /// The Wilderness gear rewards are given with the module their source names them by
    /// (Wilderness_reward_modules).
    ///
    /// The sources name a reward by its modifier: "Pulsar Boots", "AccuMax Rifle"
    /// (docs/wilderness-missions.md, Historical source reward item names). The client names an item
    /// by its highest-priority module (gameuiutil.GetItemName): modulenamelanguage gives each
    /// module a name such as "Pulsar %(ClassName)s". So each prefix is turned back into the modules
    /// of that name, of the class set the reward item's class is in (modifiable_class): armor 212,
    /// weapons 1212. Each prefix here names one kind of module there; its five levels share the
    /// name, and level [1] is taken - the name does not say which, and the earlier reconstruction
    /// chose the [1] modules for 1069 and 479 (Dynamo 100008, Prodigy 100068, Luminar 100028).
    /// The module goes in the item's first slot, the rest stay empty. A choice written without a
    /// prefix after one with it ("Pathogex Boots / Gloves") has the same one.
    ///
    /// Not here, and given as before with no module:
    ///  - a prefix that names more than one kind of module for its item: ChiTech weapons (Power
    ///    Regen Bonus or Steal Power; 421, 430, 433), Vitalius weapons (Health Regen Bonus or Steal
    ///    Health; 429, 431), the Teleract Helmet (Armor Absorption or Armor Recharge; 700, 820),
    ///    Vextronics weapons (eight resistance debuffs; 682, 696, 860);
    ///  - a tool whose prefix has no tool module: Astra Healing Disc (429, 451), Prodigy Repair
    ///    Tool (428);
    ///  - 508's "Reflective Legs", named without a modifier;
    ///  - rewards with no source name: 1992, 2010, 2011.
    ///
    /// Each row changes only while it holds its template and no module; a row a GM changed by hand
    /// is left alone. An evidence row per mission (102) records it; Down empties the slot
    /// again, only where it still holds this module, and takes the evidence out. No item a
    /// character already holds is changed.
    /// </summary>
    public static class WildernessRewardModules
    {
        public const string Revision = WildernessMissionDataV1.Revision;
        public const uint RewardId = 1;
        public const uint EvidenceId = 102;

        /// <summary>Olympia: Armor Module: Resist Physical [1].</summary>
        public const uint ResistPhysical = 100020;

        /// <summary>Pulsar: Armor Module: Resist EMP [1].</summary>
        public const uint ResistEmp = 100048;

        /// <summary>Luminar: Armor Module: Resist Photonic [1].</summary>
        public const uint ResistPhotonic = 100028;

        /// <summary>Pathogex: Armor Module: Resist Virulent [1].</summary>
        public const uint ResistVirulent = 100044;

        /// <summary>Phoenix: Armor Module: Regen Bonus [1].</summary>
        public const uint RegenBonus = 100016;

        /// <summary>Dynamo: Armor Module: Power Bonus [1].</summary>
        public const uint PowerBonus = 100008;

        /// <summary>Prodigy: Armor Module: Mind Bonus [1].</summary>
        public const uint MindBonus = 100068;

        /// <summary>AccuMax: Weapon Module: Crit Hit Bonus [1].</summary>
        public const uint CritHitBonus = 100056;

        /// <summary>Eclipse: Weapon Module: Threat Reduction [1].</summary>
        public const uint ThreatReduction = 100052;

        public sealed record Row(uint Mission, uint Item, uint Template, uint Module, string Name);

        public static readonly Row[] Rows =
        {
            new(421, 2, 26996, ResistEmp, "Pulsar Boots"),
            new(425, 1, 12827, ResistVirulent, "Pathogex Boots"),
            new(425, 2, 12855, ResistVirulent, "Pathogex Gloves"),
            new(427, 1, 20697, RegenBonus, "Phoenix Legs"),
            new(430, 2, 13739, ResistPhysical, "Olympia Boots"),
            new(431, 1, 20548, ResistPhysical, "Olympia Helmet"),
            new(433, 1, 20399, ResistPhysical, "Olympia Gloves"),
            new(442, 1, 3315, CritHitBonus, "AccuMax Rifle"),
            new(442, 2, 3603, ThreatReduction, "Eclipse Shotgun"),
            new(444, 1, 20399, ResistEmp, "Pulsar Gloves"),
            new(444, 2, 20697, ResistEmp, "Pulsar Legs"),
            new(479, 1, 20846, ResistPhotonic, "Luminar Vest"),
            new(506, 1, 35784, ResistEmp, "Pulsar Helmet"),
            new(506, 2, 20250, ResistPhysical, "Olympia Boots"),
            new(508, 1, 20697, ResistPhotonic, "Luminar Hazmat Legs"),
            new(570, 1, 4019, CritHitBonus, "AccuMax Chaingun"),
            new(570, 2, 20846, ResistEmp, "Pulsar Vest"),
            new(574, 1, 20548, ResistEmp, "Pulsar Helmet"),
            new(574, 2, 36083, ResistPhysical, "Olympia Vest"),
            new(665, 1, 13744, ResistPhysical, "Olympia Legs"),
            new(665, 2, 28692, ResistPhysical, "Olympia Gloves"),
            new(666, 1, 13744, ResistPhysical, "Olympia Legs"),
            new(666, 2, 28692, ResistEmp, "Pulsar Gloves"),
            new(682, 1, 12831, ResistPhotonic, "Luminar Boots"),
            new(695, 1, 12887, ResistPhysical, "Olympia Helmet"),
            new(695, 2, 12943, ResistPhysical, "Olympia Vest"),
            new(696, 1, 12887, ResistPhysical, "Olympia Helmet"),
            new(697, 1, 11567, ResistPhotonic, "Luminar Boots"),
            new(697, 2, 3061, CritHitBonus, "AccuMax Pistol"),
            new(698, 1, 12831, ResistPhysical, "Olympia Boots"),
            new(698, 2, 12943, ResistPhotonic, "Luminar Vest"),
            new(700, 2, 12915, ResistPhysical, "Olympia Legs"),
            new(820, 2, 12915, ResistPhysical, "Olympia Legs"),
            new(860, 2, 11568, ResistPhotonic, "Luminar Legs"),
            new(1069, 1, 20399, PowerBonus, "Dynamo Gloves"),
            new(1069, 2, 35486, MindBonus, "Prodigy Boots"),
            new(1392, 1, 20250, PowerBonus, "Dynamo Hazmat Boots"),
            new(1392, 2, 35486, MindBonus, "Prodigy Reflective Boots"),
            new(1393, 1, 20250, PowerBonus, "Dynamo Hazmat Boots"),
            new(1393, 2, 35486, MindBonus, "Prodigy Reflective Boots"),
        };

        /// <summary>What each module is, for the evidence notes.</summary>
        public static readonly IReadOnlyDictionary<uint, string> ModuleNames = new Dictionary<uint, string>
        {
            [ResistPhysical] = "Armor Module: Resist Physical [1]",
            [ResistEmp] = "Armor Module: Resist EMP [1]",
            [ResistPhotonic] = "Armor Module: Resist Photonic [1]",
            [ResistVirulent] = "Armor Module: Resist Virulent [1]",
            [RegenBonus] = "Armor Module: Regen Bonus [1]",
            [PowerBonus] = "Armor Module: Power Bonus [1]",
            [MindBonus] = "Armor Module: Mind Bonus [1]",
            [CritHitBonus] = "Weapon Module: Crit Hit Bonus [1]",
            [ThreatReduction] = "Weapon Module: Threat Reduction [1]",
        };

        public static IEnumerable<uint> Missions => Rows.Select(r => r.Mission).Distinct();

        /// <summary>The statements, one each, as MigrationBuilder.Sql takes them.</summary>
        public static IEnumerable<string> Up()
        {
            foreach (var r in Rows)
                yield return $"UPDATE mission_reward_item SET module_1 = {r.Module} " +
                    $"{Key(r)} AND module_1 = 0 AND module_2 = 0 AND module_3 = 0 AND module_4 = 0;";

            foreach (var mission in Rows.GroupBy(r => r.Mission))
                yield return "INSERT INTO mission_evidence (mission_id, content_revision, evidence_id, confidence, local_client_path, owner_id, owner_kind, " +
                    "reconstruction_note, source_kind, source_uri) " +
                    $"SELECT {mission.Key}, {S(Revision)}, {EvidenceId}, 0.5, NULL, {RewardId}, {(byte)MissionEvidenceOwnerKind.Reward}, " +
                    $"{S(Note(mission))}, {(byte)MissionEvidenceSourceKind.Reconstruction}, {S("repository:WildernessRewardModules")} " +
                    $"FROM mission_content_definition d WHERE d.mission_id = {mission.Key} AND d.content_revision = {S(Revision)} " +
                    $"AND NOT EXISTS (SELECT 1 FROM mission_evidence x WHERE x.mission_id = {mission.Key} AND x.content_revision = {S(Revision)} AND x.evidence_id = {EvidenceId});";
        }

        public static IEnumerable<string> Down()
        {
            foreach (var mission in Missions)
                yield return $"DELETE FROM mission_evidence WHERE mission_id = {mission} AND content_revision = {S(Revision)} AND evidence_id = {EvidenceId};";

            foreach (var r in Rows)
                yield return $"UPDATE mission_reward_item SET module_1 = 0 " +
                    $"{Key(r)} AND module_1 = {r.Module} AND module_2 = 0 AND module_3 = 0 AND module_4 = 0;";
        }

        private static string Key(Row r) =>
            $"WHERE mission_id = {r.Mission} AND content_revision = {S(Revision)} AND reward_id = {RewardId} AND item_id = {r.Item} " +
            $"AND item_template_id = {r.Template}";

        private static string Note(IEnumerable<Row> rows) =>
            $"R from the source names: {string.Join(", ", rows.Select(r => $"item {r.Item} {r.Name} = {ModuleNames[r.Module]} ({r.Module})"))}. " +
            "The prefix is the client's module name (modulenamelanguage) of the item's class set; level [1] is chosen, the name does not give it. " +
            "XP, credits, templates, selection and quantities unchanged.";

        private static string S(string value) => "'" + value.Replace("'", "''") + "'";
    }
}
