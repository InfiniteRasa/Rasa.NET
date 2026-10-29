using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Pools
{
    /// <summary>
    /// Bane Jamming (native mission 938), Valverde Pools: Lieutenant Feldman sends the player back to the Bane fort in
    /// the west to destroy the equipment attached to the Eloh obelisk and to plant six jammers round it.
    ///
    /// Native: texts 5066-5069 and 7977 (missionconversation): offer "take out any of their equipment that's directly
    /// attached to the tower ... Take this satchel of electronic jammers and sink them into the ground surrounding the
    /// Obelisk"; objectives 1 "Destroy the Bane Equipment attached to the Eloh Obelisk" 5093/7978, counter 5097
    /// "Equipment Destroyed"; 2 and 8-12 "Place Jammer: Alpha" .. "Zeta" 5096, 5263-5267 / 7979-7984 (missionobjective);
    /// no objective conversation. Jammer item 713 (class 7791 MisPoolsItemJammer "Distortion Frequency Jammer"); the
    /// jammer spot class 7837 UsableItemAccHumJammer (augmentation 43, use argument 3, the planted model in state 121);
    /// Bane Machinery 7874 (augmentation 41, a model and a destroyed model).
    ///
    /// Sourced: TaRapedia Bane Jamming: giver and receiver Lieutenant Feldman; requires Sabotage the Tower; 120,000 XP,
    /// 7,000 credits; one of Teleract Graviton Gloves level 35, Wellcare Hazmat Legs 34, Wellcare Motor Assist Legs 33,
    /// Teleract Reflective Legs 34; walkthrough: three jammers outside the base, "jump onto the wall of the base to
    /// reach the other three". TR MMO DB: level 35.
    ///
    /// Reconstructed (ESTIMATE): Feldman gives six jammers on accept (the "satchel"); six personal spots, Alpha-Gamma
    /// outside the fort's west wall, Delta-Zeta inside round the obelisk (PoolsObeliskV1), any order, each takes one and
    /// completes its own objective; the equipment four personal Bane Machinery round the obelisk's foot (user,
    /// 2026-10-25), 100 hit points; all objectives open from the accept. The items the lowest Uncommon templates of the
    /// lines and bands that hold those names and levels (Graviton T3 V04, Hazmat and Reflective T2 V02, Motor Assist T1
    /// V04), one to choose.
    /// </summary>
    public static class PoolsBaneJammingV1
    {
        public const uint MissionId = 938;
        public const uint Level = 35;
        public const uint Feldman = PoolsSnakePitContactsV1.Feldman;
        public const uint Destroy = 1;
        public const uint MachineryScenario = 1, JammerScenario = 2;

        public const uint MachineryClass = 7874;        // UsableInertDestBaneMachineryV01DELETEDUPE "Bane Machinery"
        public const uint MachineryHitPoints = 100;
        public const uint IntactState = 110, DestroyedState = 2;

        public const uint JammerTemplate = 713, JammerClass = 7791, Jammers = 6;
        public const string JammerKey = "jammer";
        public const uint SpotClass = 7837;             // UsableItemAccHumJammer
        public const uint SpotReadyState = 119, SpotPlantedState = 121;
        public const uint UseArgument = 3;

        public const uint Experience = 120000;
        public const uint Credits = 7000;
        public const uint GravitonGloves = 24970;       // Armor_T3_Graviton_V04_UNC_Gloves_35_to_39
        public const uint HazmatLegs = 20782;           // Armor_T2_Hazmat_V02_UNC_Legs_34_to_38
        public const uint MotorAssistLegs = 29073;      // Armor_T1_MotorAssist_V04_UNC_Legs_33_to_37
        public const uint ReflectiveLegs = 36019;       // Armor_T2_Reflective_V02_UNC_Legs_34_to_38

        /// <summary>At the obelisk's foot, on the navmesh.</summary>
        public static readonly ScenePosition[] Machinery =
        {
            new(-844, 791.45f, -20),
            new(-852, 791.4f, -20),
            new(-848, 791.4f, -16),
            new(-848, 791.4f, -24),
        };

        /// <summary>Jammers Alpha-Gamma outside the west wall, Delta-Zeta inside, on the navmesh.</summary>
        public static readonly ScenePosition[] Spots =
        {
            new(-888, 791.40f, -20),
            new(-882, 791.34f, -46),
            new(-886, 791.4f, -60),
            new(-864, 791.58f, -20),
            new(-848, 791.4f, -36),
            new(-858, 791.27f, -8),
        };

        /// <summary>Objective of jammer <paramref name="number"/> (1-6, Alpha-Zeta): 2, then 8-12.</summary>
        public static uint Place(int number) => number == 1 ? 2U : 6 + (uint)number;

        /// <summary>Name text of jammer <paramref name="number"/>: 5096, then 5263-5267.</summary>
        private static uint PlaceName(int number) => number == 1 ? 5096U : 5261 + (uint)number;

        public static string MachineryRole(int index) => $"machinery-{index + 1}";
        public static string SpotRole(int number) => $"jammer-{number}";

        public static void Up(MigrationBuilder migration)
        {
            var mission = new PoolsMissionDataV1(migration, MissionId, "Bane Jamming", 5066, Feldman, Feldman, Level);
            mission.Reward(Experience, Credits, new[] { GravitonGloves, HazmatLegs, MotorAssistLegs, ReflectiveLegs });
            mission.Prerequisite(PoolsSabotageTheTowerV1.MissionId);

            mission.Objective(Destroy, 5093, 7978, 0, MissionObjectiveState.Incomplete, counterTextId: 5097);
            for (var number = 1; number <= Jammers; number++)
                mission.Objective(Place(number), PlaceName(number), 7978 + (uint)number, (uint)number, MissionObjectiveState.Incomplete);
            mission.Scenario(MachineryScenario, "Destroy the Bane Machinery on the obelisk", (uint)Machinery.Length);
            mission.Scenario(JammerScenario, "A jammer planted", Jammers);

            // 1: the four machines.
            mission.Transition(Destroy);
            for (var id = 1U; id <= Machinery.Length; id++)
                mission.Progress(Destroy, MissionProgressEventKind.ScenarioEvent, id, (uint)Machinery.Length, counter: MachineryScenario, trigger: id);
            mission.Action(Destroy, 1, MissionActionKind.GrantReward, reward: 1);

            // 2, 8-12: each jammer at its own spot, any order.
            for (var number = 1; number <= Jammers; number++)
            {
                mission.Transition(Place(number));
                mission.Progress(Place(number), MissionProgressEventKind.ScenarioEvent, (uint)number, null, counter: JammerScenario);
                mission.Action(Place(number), 1, MissionActionKind.GrantReward, reward: 1);
            }

            var obelisk = PoolsObeliskV1.At;
            mission.Indicator(Destroy, 1, obelisk.X, obelisk.Y, obelisk.Z, 10);
            for (var number = 1; number <= Jammers; number++)
            {
                var spot = Spots[number - 1];
                mission.Indicator(Place(number), 1 + (uint)number, spot.X, spot.Y, spot.Z);
            }

            var scene = new MissionSceneDefinition
            {
                Script = "data.sequence",
                Items = new()
                {
                    new MissionItemBinding(JammerKey, JammerTemplate, MissionItemScope.AssignmentIssued, Jammers,
                        MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove)
                },
                AcceptanceItems = new()
                {
                    new IssueMissionItemIntent("issue-jammers", MissionId, JammerKey, JammerTemplate, Jammers)
                }
            };
            scene.Sequences[0] = new SceneSequenceDefinition();
            for (var index = 0; index < Machinery.Length; index++)
            {
                var id = (uint)index + 1;
                var role = MachineryRole(index);
                scene.Actors[role] = new SceneActorDefinition(role, SceneActorKind.Object, MachineryClass, Machinery[index],
                    Orientation: index * 1.5707963268, InitialObjectState: IntactState,
                    Destruction: new SceneObjectDestruction(MissionId, Destroy, id, MachineryHitPoints, DestroyedState));
                scene.Sequences[0].World.Add(new EnsureActorIntent($"create-{role}", role));
                scene.Sequences[id] = new SceneSequenceDefinition
                {
                    World =
                    {
                        new TransitionObjectStateIntent($"destroy-{role}", role, DestroyedState),
                        new SetInteractionIntent($"disable-{role}", role, false)
                    },
                    Signals = { new SceneMissionSignal(MissionId, MachineryScenario, id) }
                };
            }
            for (var number = 1; number <= Jammers; number++)
            {
                var role = SpotRole(number);
                var sequence = (uint)Machinery.Length + (uint)number;
                scene.Actors[role] = new SceneActorDefinition(role, SceneActorKind.Object, SpotClass, Spots[number - 1],
                    InitialObjectState: SpotReadyState, UseAction: new SceneObjectAction(MissionId, Place(number), sequence, UseArgument));
                scene.Sequences[0].World.Add(new EnsureActorIntent($"create-{role}", role));
                scene.Sequences[sequence] = new SceneSequenceDefinition
                {
                    World =
                    {
                        new TransitionObjectStateIntent($"plant-{role}", role, SpotPlantedState),
                        new SetInteractionIntent($"close-{role}", role, false)
                    },
                    Character = { new ConsumeMissionItemIntent($"{role}-planted", MissionId, JammerKey, 1, MissionItemScope.AssignmentIssued) },
                    Signals = { new SceneMissionSignal(MissionId, JammerScenario, (uint)number) }
                };
            }

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/938",
                "Native1 'Destroy the Bane Equipment attached to the Eloh Obelisk' 5093/7978 counter5097; 2, 8-12 'Place Jammer: " +
                "Alpha..Zeta' 5096, 5263-5267 / 7979-7984; no objectiveconversation. Texts 5066-5069, 7977. Jammer item713/class7791, " +
                "spot class7837 (augmentation43, use3, planted in state121), Bane Machinery 7874 (augmentation41).");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Bane_Jamming",
                "Giver/receiver Lieutenant Feldman; requires Sabotage the Tower; 120000XP, 7000 credits; one of Teleract Graviton " +
                "Gloves35, Wellcare Hazmat Legs34, Wellcare Motor Assist Legs33, Teleract Reflective Legs34; three jammers outside " +
                "the base, three inside from the wall. TR MMO DB level35.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:PoolsBaneJammingV1",
                "Six jammers on accept; six personal spots 7837, Alpha-Gamma outside, Delta-Zeta inside, any order; four personal " +
                "Bane Machinery 7874 (user) at the obelisk 760017, 100HP; all open at accept. Items 24970, 20782, 29073, 36019, one " +
                "to choose.", 0.3);

            mission.Enable(scene);
        }

        public static void Down(MigrationBuilder migration) => PoolsMissionDataV1.Remove(migration, MissionId);
    }
}
