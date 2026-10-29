using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Pools
{
    /// <summary>
    /// Building Materials (native mission 1314), Valverde Pools: Supply Chief Riggins at the Snake Pit wants Thrax
    /// Carapaces, Hunter Thigh Bones and Howler Sinew "harvested" from the Bane.
    ///
    /// Native: texts 10666-10670 (missionconversation): offer "I need some Thrax Carapaces from their soldiers to shore
    /// up our defenses... saw off some thigh bones from a few of those Thrax Hunters", turn-in "You didn't even clean
    /// this stuff off before you brought it to me?"; objectives 1 "Collect Thrax Carapaces" 10671/10672 ("from the dead
    /// bodies of Thrax soldiers"), 2 "Collect Hunter Thigh Bones" 10673/10674 ("of Bane Hunters"), 3 "Collect Howler
    /// Sinew" 10675/10676 ("of the Howlers"), no counters (missionobjective). Items: Thrax Carapace class 25551
    /// (template 97505), Hunter Thigh Bone 25550 (97504), Howler Sinew 25548 (97502).
    ///
    /// Sourced: TaRapedia Building Materials: giver and receiver Supply Chief Riggins (The Snakepit); 8 carapaces, 5
    /// thigh bones, 15 sinews; 55,000 XP, 5,100 credits; one of Phoenix Bio Armor Vest, Astra Graviton Armor Vest,
    /// Teleract Mech Armor Legs, Prodigy Stealth Armor Vest, level 35, tier 4; no requirement. TR MMO DB: level 33.
    /// World DB, map 1304: Thrax Soldier Mark IV 531089 (22 pools), Hunter Tracker 531095 (3 pools), Fiend Pup Howler
    /// 540009 (12 pools).
    ///
    /// Reconstructed (ESTIMATE): "Thrax soldiers" read as the Thrax Soldier Mark IV only, "Bane Hunters" / "Thrax
    /// Hunters" as the Hunter Tracker, "Howlers" as the Fiend Pup Howler (the only creatures of those names on the
    /// Pools); personal drops, 50% a kill (no source gives a rate), one item each, picked up from the corpse; Riggins
    /// keeps them at the turn-in. The four armor pieces as the Uncommon V04 templates of the 35-39 band, one of each
    /// (the manufacturer names are not in the client tables). The creatures stand all over the Pools: markers on their pools nearest the Snake Pit.
    /// </summary>
    public static class PoolsBuildingMaterialsV1
    {
        public const uint MissionId = 1314;
        public const uint Level = 33;
        public const uint Riggins = PoolsSnakePitNpcsV1.Riggins;

        public const uint CarapaceTemplate = 97505, CarapaceClass = 25551, Carapaces = 8;
        public const uint BoneTemplate = 97504, BoneClass = 25550, Bones = 5;
        public const uint SinewTemplate = 97502, SinewClass = 25548, Sinews = 15;
        public const string CarapaceKey = "thrax-carapace", BoneKey = "hunter-thigh-bone", SinewKey = "howler-sinew";
        public const int DropChance = 50;

        public const uint ThraxSoldier = 531089, HunterTracker = 531095, FiendPupHowler = 540009;

        /// <summary>
        /// The markers (ESTIMATE: the pools of each creature nearest the Snake Pit, World DB map 1304): Thrax Soldier
        /// Mark IV pools 531670 and 531678, Hunter Tracker pool 531668, Fiend Pup Howler pools 540026-540028.
        /// </summary>
        public static readonly (uint Objective, ScenePosition At, double Radius)[] Markers =
        {
            (1, new(-811.6f, 788.5f, -7.7f), 40),
            (2, new(-337.5f, 681.9f, -254.7f), 30),
            (3, new(-747.4f, 844.1f, 205.2f), 70)
        };

        public const uint Experience = 55000;
        public const uint Credits = 5100;
        public const uint BioVest = 46036;              // Armor_T3_Bio_V04_UNC_Vest_35_to_39
        public const uint GravitonVest = 25357;         // Armor_T3_Graviton_V04_UNC_Vest_35_to_39
        public const uint MechLegs = 34354;             // Armor_T3_Mech_V04_UNC_Legs_35_to_39
        public const uint StealthVest = 40611;          // Armor_T3_Stealth_V04_UNC_Vest_35_to_39

        public static void Up(MigrationBuilder migration)
        {
            var mission = new PoolsMissionDataV1(migration, MissionId, "Building Materials", 10666, Riggins, Riggins, Level);
            mission.Reward(Experience, Credits, new[] { BioVest, GravitonVest, MechLegs, StealthVest });

            mission.Objective(1, 10671, 10672, 0, MissionObjectiveState.Incomplete);
            mission.Objective(2, 10673, 10674, 1, MissionObjectiveState.Incomplete);
            mission.Objective(3, 10675, 10676, 2, MissionObjectiveState.Incomplete);
            mission.Transition(1);
            mission.Progress(1, MissionProgressEventKind.ItemAcquired, CarapaceClass, Carapaces);
            mission.Action(1, 1, MissionActionKind.GrantReward, reward: 1);
            mission.Transition(2);
            mission.Progress(2, MissionProgressEventKind.ItemAcquired, BoneClass, Bones);
            mission.Transition(3);
            mission.Progress(3, MissionProgressEventKind.ItemAcquired, SinewClass, Sinews);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1314",
                "Native1 'Collect Thrax Carapaces' 10671/10672, 2 'Collect Hunter Thigh Bones' 10673/10674, 3 'Collect Howler Sinew' " +
                "10675/10676, no counters. Texts 10666-10670. Carapace class25551/template97505, thigh bone 25550/97504, sinew " +
                "25548/97502. Riggins name9385.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Building_Materials",
                "Giver/receiver Supply Chief Riggins (The Snakepit); 8 carapaces, 5 thigh bones, 15 sinews; 55000XP, 5100 credits; one " +
                "of Phoenix Bio vest, Astra Graviton vest, Teleract Mech legs, Prodigy Stealth vest, lvl35 tier4; no requirement. TR " +
                "MMO DB level33.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:PoolsBuildingMaterialsV1",
                "Level33. Personal drops on map1304: carapace from Thrax Soldier Mark IV 531089, bone from Hunter Tracker 531095, " +
                "sinew from Fiend Pup Howler 540009, 50% each (rate estimated). Riggins (760200, new) keeps them. Items 46036, 25357, " +
                "34354, 40611 (Uncommon V04, 35-39). Markers on the pools nearest the Snake Pit.", 0.3);

            for (var index = 0; index < Markers.Length; index++)
                mission.Indicator(Markers[index].Objective, 1 + (uint)index, Markers[index].At.X, Markers[index].At.Y, Markers[index].At.Z,
                    Markers[index].Radius);

            mission.Enable(new MissionSceneDefinition
            {
                ExistingFactObjectiveIds = new() { 1, 2, 3 },
                Items = new()
                {
                    Drop(CarapaceKey, CarapaceTemplate, Carapaces, ThraxSoldier, 1),
                    Drop(BoneKey, BoneTemplate, Bones, HunterTracker, 2),
                    Drop(SinewKey, SinewTemplate, Sinews, FiendPupHowler, 3)
                }
            });
        }

        private static MissionItemBinding Drop(string key, uint template, uint count, uint creature, uint objective) =>
            new(key, template, MissionItemScope.CharacterOwned, count,
                MissionItemCleanupDisposition.Retain, MissionItemCleanupDisposition.Retain,
                MissionItemCleanupDisposition.Retain, TurnInQuantity: count,
                Drop: new MissionItemDropDefinition(new[] { creature }, objective, PoolsMissionDataV1.MapContextId, DropChance, 1));

        public static void Down(MigrationBuilder migration) => PoolsMissionDataV1.Remove(migration, MissionId);
    }
}
