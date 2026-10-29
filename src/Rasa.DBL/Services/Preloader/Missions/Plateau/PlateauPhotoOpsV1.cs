using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Plateau
{
    /// <summary>
    /// Photo Ops (native mission 929), Plateau: Outpost Cmdr. Russ at Wedge Rock Outpost sends the player to
    /// destroy Desolation Quarry's five surveillance sentinels and take their data to Science Officer Clark.
    ///
    /// Native: name/log/offer/turn-in/reminder texts 4978-4981 and 6427 (missionconversation); objectives 5-9
    /// "Get surveillance data #1" to "#5" (4993/6428, 5729/6429, 5730/6430, 5731/6431, 5732/6432), "Destroy
    /// the first [...fifth] Bane Surveillance Sentinel and obtain its surveillance data", no counters
    /// (missionobjective); no objective conversation. Disks: Surveillance Data - Disk One to Five, classes
    /// 21999-22003 (templates 44924-44928). Russ's offer (4980): "Once you've got all five, take them to
    /// Science Officer Clark for analysis. You'll find him in the research tent next door."
    ///
    /// Sourced: TaRapedia Photo Ops: giver Outpost Cmdr. Russ, reward from Science Officer Clark, level "32
    /// (possibly less)", requirement Mind Bender, follow-up The Results Are In; 45,000 XP, 4,800 credits,
    /// "Equipment"; disk N 0/1 per objective; "Each bot is numbered according to the Data Disk they drop".
    ///
    /// Reconstructed (ESTIMATE): level 32; sentinel N (PlateauSurveillanceSentinelsV1) drops disk N, a
    /// personal drop at every kill while its objective is open, picked up from the corpse; Clark keeps the
    /// five. "Equipment" read as the four Uncommon V04 boots of the 30-34 band, one of each armor type.
    /// </summary>
    public static class PlateauPhotoOpsV1
    {
        public const uint MissionId = 929;
        public const uint Level = 32;
        public const uint Russ = PlateauPerduNpcsV1.Russ, Clark = PlateauPerduNpcsV1.Clark;
        public const int DiskChance = 100;

        public const uint Experience = 45000;
        public const uint Credits = 4800;
        public const uint BioBoots = 46003, GravitonBoots = 24821, MechBoots = 33947, StealthBoots = 40074;

        /// <summary>Per sentinel One to Five: objective and texts, disk template and class.</summary>
        public static readonly (uint Objective, uint Name, uint Body, uint Disk, uint DiskClass)[] Disks =
        {
            (5, 4993, 6428, 44924, 21999),
            (6, 5729, 6429, 44925, 22000),
            (7, 5730, 6430, 44926, 22001),
            (8, 5731, 6431, 44927, 22002),
            (9, 5732, 6432, 44928, 22003),
        };

        public static string DiskKey(int index) => $"surveillance-disk-{index + 1}";

        public static void Up(MigrationBuilder migration)
        {
            var mission = new PlateauMissionDataV1(migration, MissionId, "Photo Ops", 4978, Russ, Clark, Level);
            mission.Reward(Experience, Credits, new[] { BioBoots, GravitonBoots, MechBoots, StealthBoots });
            mission.Prerequisite(PlateauMindBenderV1.MissionId);

            var items = new List<MissionItemBinding>();
            for (var index = 0; index < Disks.Length; index++)
            {
                var disk = Disks[index];
                var sentinel = PlateauSurveillanceSentinelsV1.Sentinels[index];
                mission.Objective(disk.Objective, disk.Name, disk.Body, (uint)index, MissionObjectiveState.Incomplete);
                mission.Transition(disk.Objective);
                mission.Progress(disk.Objective, MissionProgressEventKind.ItemAcquired, disk.DiskClass, 1);
                mission.Indicator(disk.Objective, (uint)index + 1, sentinel.At.X, sentinel.At.Y, sentinel.At.Z, 15);
                items.Add(new MissionItemBinding(DiskKey(index), disk.Disk, MissionItemScope.CharacterOwned, 1,
                    MissionItemCleanupDisposition.Retain, MissionItemCleanupDisposition.Retain,
                    MissionItemCleanupDisposition.Retain, TurnInQuantity: 1,
                    Drop: new MissionItemDropDefinition(new[] { sentinel.Id }, disk.Objective,
                        PlateauMissionDataV1.MapContextId, DiskChance, 1)));
            }
            mission.Action(Disks[0].Objective, 1, MissionActionKind.GrantReward, reward: 1);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/929",
                "Native5-9 'Get surveillance data #1-#5' (4993/6428, 5729-5732/6429-6432), no counters, no objective " +
                "conversation. Texts 4978-4981, 6427. Disks classes21999-22003/templates44924-44928. Sentinels' names 8909-8913; " +
                "Russ name4677; Clark 'in the research tent next door' (4980).");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Photo_Ops",
                "Giver Outpost Cmdr. Russ, reward from Science Officer Clark; level '32 (possibly less)'; requirement Mind " +
                "Bender; follow-up The Results Are In; 45000XP, 4800 credits, 'Equipment'; the sentinels patrol the quarry's " +
                "levels, each numbered by the disk it drops; elite; they call Thrax.", 0.5);
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:PlateauPhotoOpsV1",
                "Level32. Sentinel N650037-750041 (PlateauSurveillanceSentinelsV1: level, strength, places estimated; no call " +
                "for help) drops disk N, 100% personal while its objective is open; Clark keeps the five. Equipment read as " +
                "Uncommon V04 boots 30-34: 46003, 24821, 33947, 40074. Russ added, Clark moved to the research tent.", 0.3);

            mission.Enable(new MissionSceneDefinition
            {
                ExistingFactObjectiveIds = Disks.Select(disk => disk.Objective).ToList(),
                Items = items
            });
        }

        public static void Down(MigrationBuilder migration) => PlateauMissionDataV1.Remove(migration, MissionId);
    }
}
