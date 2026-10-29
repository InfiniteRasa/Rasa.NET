using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Pools
{
    /// <summary>
    /// Chemistry 101 (native mission 885), Turpis Refinery (squad instance 1429), Valverde Pools: Field Chemist Larouche
    /// wants five refined and five unrefined toxin samples from the refinery's chemical engineers.
    ///
    /// Native: texts 4633-4636, 7315 (missionconversation): log "Bring Field Chemist Larouche 5 unrefined samples of toxin
    /// and 5 refined samples of toxin from the Chemical Engineers.", offer "The chemical engineers here have plenty of
    /// samples that they ferry back and forth from testing to production", turn-in "I shall begin processing these
    /// immediately."; objectives 1 "Refined Samples" 5910/7316, 2 "Unrefined Samples" 5911/7317 (missionobjective); no
    /// objective conversation. Items: 45025 MisTurpisFactoryBaneRefinedChemicalSample (class 22115, "Refined Chemical
    /// Sample"), 45026 MisTurpisFactoryBaneUnrefinedChemicalSample (class 22116, "Unrefined Sample").
    ///
    /// Sourced: TaRapedia Chemistry 101: giver and receiver Field Chemist Larouche (Turpis Refinery); no requirement;
    /// 50,000 XP, 4,950 credits; one of Titan Graviton Armor Gloves lvl 30, Prodigy Hazmat Armor Vest lvl 34, Titan Motor
    /// Assist Armor Vest lvl 30, Astra Reflective Armor Vest lvl 34. Ten Ton Hammer Turpis Refinery guide: Larouche
    /// "Inside the tent with the medic". TR MMO DB: level 33. No source names a "Chemical Engineer" creature; the client
    /// has none.
    ///
    /// Reconstructed (ESTIMATE; the user's choice of 2026-10-25: the refinery's creatures closest to "Chemical
    /// Engineers"):
    ///  - The refinery's Thrax Technicians (Thrax Technician - Marshes, 531047, one or two in seven of its ten pools) carry
    ///    the samples: each kill in Turpis Refinery drops one refined and one unrefined sample for the killer while their
    ///    objectives are open (100% each, as Miasma Mutations' samples); samples already held count at the accept.
    ///    Larouche takes the five of each at the turn-in.
    ///  - Rewards the lowest Uncommon templates of the bands that hold those levels, one to choose. Markers in the instance too.
    /// </summary>
    public static class PoolsChemistry101V1
    {
        public const uint MissionId = 885;
        public const uint Level = 33;
        public const uint MapContextId = PoolsTurpisRefineryNpcsV1.MapContextId;
        public const uint Larouche = PoolsTurpisRefineryNpcsV1.Larouche;
        public const uint Refined = 1, Unrefined = 2;
        public const uint Technician = 531047;          // Thrax Technician - Marshes
        public const uint Samples = 5;
        public const double TechnicianRadius = 20;

        /// <summary>The seven spawn pools that hold Thrax Technicians (531370-531376, WorldSpawnpoolPreloader), the markers.</summary>
        public static readonly ScenePosition[] TechniciansAt =
        {
            new(-209.1f, 108.8f, -90.8f), new(-181.9f, 112.5f, 10.6f), new(-67.9f, 104.4f, 176.3f), new(-19f, 114.3f, -17f),
            new(-113.3f, 109.6f, 79.6f), new(-35.8f, 116.9f, -127.2f), new(-79.4f, 104.2f, -25.2f)
        };

        public const uint RefinedTemplate = 45025, RefinedClass = 22115;
        public const uint UnrefinedTemplate = 45026, UnrefinedClass = 22116;
        public const string RefinedKey = "turpis-refined-sample", UnrefinedKey = "turpis-unrefined-sample";

        public const uint Experience = 50000;
        public const uint Credits = 4950;
        public const uint GravitonGloves = 23658;       // Armor_T3_Graviton_V02_UNC_Gloves_30_to_34
        public const uint HazmatVest = 20931;           // Armor_T2_Hazmat_V02_UNC_Vest_34_to_38
        public const uint MotorAssistVest = 29207;      // Armor_T1_MotorAssist_V04_UNC_Vest_28_to_32
        public const uint ReflectiveVest = 36168;       // Armor_T2_Reflective_V02_UNC_Vest_34_to_38

        public static void Up(MigrationBuilder migration)
        {
            var mission = new PoolsMissionDataV1(migration, MissionId, "Chemistry 101", 4633, Larouche, Larouche, Level);
            mission.Reward(Experience, Credits, new[] { GravitonGloves, HazmatVest, MotorAssistVest, ReflectiveVest });

            mission.Objective(Refined, 5910, 7316, 0, MissionObjectiveState.Incomplete);
            mission.Objective(Unrefined, 5911, 7317, 1, MissionObjectiveState.Incomplete);
            mission.Transition(Refined);
            mission.Progress(Refined, MissionProgressEventKind.ItemAcquired, RefinedClass, Samples);
            mission.Action(Refined, 1, MissionActionKind.GrantReward, reward: 1);
            mission.Transition(Unrefined);
            mission.Progress(Unrefined, MissionProgressEventKind.ItemAcquired, UnrefinedClass, Samples);

            var scene = new MissionSceneDefinition
            {
                Script = "data.sequence",
                MapContextId = MapContextId,
                ExistingFactObjectiveIds = new() { Refined, Unrefined },
                Items = new()
                {
                    Sample(RefinedKey, RefinedTemplate, Refined),
                    Sample(UnrefinedKey, UnrefinedTemplate, Unrefined)
                }
            };
            scene.Sequences[0] = new SceneSequenceDefinition();

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/885",
                "Native1 'Refined Samples' 5910/7316, 2 'Unrefined Samples' 5911/7317; no objectiveconversation. Texts 4633-4636, " +
                "7315 (from the Chemical Engineers). Items 45025 class22115, 45026 class22116.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Chemistry_101",
                "Giver/receiver Field Chemist Larouche (Turpis Refinery); no requirement; 50000XP, 4950 credits; one of Titan " +
                "Graviton Armor Gloves30, Prodigy Hazmat Armor Vest34, Titan Motor Assist Armor Vest30, Astra Reflective Armor " +
                "Vest34. Ten Ton Hammer: Larouche in the tent with the medic. TR MMO DB level33.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:PoolsChemistry101V1",
                "No Chemical Engineer creature (user: the closest): each kill of a Thrax Technician 531047 in the refinery drops " +
                "one sample of each (100%, killer only, while open). Items 23658, 20931, 29207, 36168, one to choose. Indicators in the instance too.", 0.3);

            for (var index = 0; index < TechniciansAt.Length; index++)
            {
                var at = TechniciansAt[index];
                mission.Indicator(Refined, 1 + (uint)index, at.X, at.Y, at.Z, TechnicianRadius);
                mission.Indicator(Unrefined, 1 + (uint)(TechniciansAt.Length + index), at.X, at.Y, at.Z, TechnicianRadius);
            }

            mission.Enable(scene);
        }

        public static void Down(MigrationBuilder migration) => PoolsMissionDataV1.Remove(migration, MissionId);

        private static MissionItemBinding Sample(string key, uint template, uint objective) =>
            new(key, template, MissionItemScope.CharacterOwned, Samples,
                MissionItemCleanupDisposition.Retain, MissionItemCleanupDisposition.Retain, MissionItemCleanupDisposition.Retain,
                TurnInQuantity: Samples,
                Drop: new MissionItemDropDefinition(new[] { Technician }, objective, MapContextId, 100, 1));
    }
}
