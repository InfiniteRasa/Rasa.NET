using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Pools
{
    /// <summary>
    /// Testing the Pools (native mission 561), Valverde Pools: Jayson Venma at the Retread Camp gives the player sample vials
    /// to fill at three key points of the pools, to find the source of the pollution.
    ///
    /// Native: texts 1634-1637 and 8053 (missionconversation): log "Collect tainted water samples from key points in the
    /// cascading pools", offer "taking these sample vials ... Get a sample from each key location marked on your map";
    /// objectives 1 "Collect Northern Water Sample" 1642/8054, 3 "Collect East Sample" 1643/8055, 4 "Collect West Sample"
    /// 1644/8056, 5 "Collect Water Samples" 1645/8057, no counters (missionobjective); no objective conversation. Items:
    /// Empty Vial 700 (class 7766), water samples north 701, east 702, west 703 (classes 7767-7769).
    ///
    /// Sourced: TaRapedia Testing the Pools: giver and receiver Jayson Venma, Retread Camp; requires A Cult Divided; 60,000
    /// XP, 5,250 credits; one of Titan Bio Armor Helmet level 35, Dynamo Hazmat Armor Vest 34, Phoenix Motor Assist Vest 33,
    /// Phoenix Reflective Vest 34; objectives "Collect Water Samples: Eastern / Western / Northern Water Sample 0/1";
    /// walkthrough: a sample completes its objective, but the marker stays lit. TR MMO DB: level 33.
    ///
    /// Reconstructed (ESTIMATE, taken by Claude where the user gave no choice): Venma gives three vials on accept; three
    /// personal sample spots (class 11114 UsableItemDisp_item_canister_hum_v01, a sample canister: the client has no sample
    /// class, and 7595 UsableItemDispWaterjet has the client's "Default" mesh 776, to-fix Pools #4) at the polluted water
    /// nearest each point of the compass from the camp (the user's choice: estimated places), each usable once: it takes a
    /// vial, gives its sample and completes its objective (1, 3, 4), in any order; 5 completes when all three are done
    /// (an aggregate). Markers: each spot for its objective, and all three for 5. The samples go at the turn-in. The items the lowest Uncommon templates of the lines and bands that
    /// hold those names and levels (Bio and Motor Assist V04 for TaRapedia's "4" and "6", Hazmat and Reflective V02), one to
    /// choose.
    /// </summary>
    public static class PoolsTestingThePoolsV1
    {
        public const uint MissionId = 561;
        public const uint Level = 33;
        public const uint Venma = PoolsRetreadCampNpcsV1.Venma;
        public const uint North = 1, East = 3, West = 4, Samples = 5;
        public const uint SampleScenario = 1;

        public const uint VialTemplate = 700, VialClass = 7766, Vials = 3;
        public const string VialKey = "empty-vial";
        public const uint SpotClass = 11114;            // UsableItemDisp_item_canister_hum_v01
        public const uint UseArgument = 3, ReadyState = 81, TakenState = 82;

        public const uint Experience = 60000;
        public const uint Credits = 5250;
        public const uint BioHelmet = 46020;            // Armor_T3_Bio_V04_UNC_Helmet_35_to_39
        public const uint HazmatVest = 20931;           // Armor_T2_Hazmat_V02_UNC_Vest_34_to_38
        public const uint MotorAssistVest = 29227;      // Armor_T1_MotorAssist_V04_UNC_Vest_33_to_37
        public const uint ReflectiveVest = 36168;       // Armor_T2_Reflective_V02_UNC_Vest_34_to_38

        /// <summary>
        /// The three points: objective, sample template, class, item key and the spot (scene event 1-3), at the polluted
        /// water north of the camp, east by the Thrax Barracks and west under the Hermit's butte, on the navmesh.
        /// </summary>
        public static readonly (uint Objective, uint Template, uint Class, string Key, ScenePosition At)[] Points =
        {
            (North, 701, 7767, "water-sample-north", new(-310, 872.71f, 825)),
            (East, 702, 7768, "water-sample-east", new(521.8f, 681.37f, 430.8f)),
            (West, 703, 7769, "water-sample-west", new(-421.1f, 920.78f, 186.5f)),
        };

        public static string SpotRole(int index) => $"sample-{index + 1}";

        public static void Up(MigrationBuilder migration)
        {
            var mission = new PoolsMissionDataV1(migration, MissionId, "Testing the Pools", 1634, Venma, Venma, Level);
            mission.Reward(Experience, Credits, new[] { BioHelmet, HazmatVest, MotorAssistVest, ReflectiveVest });
            mission.Prerequisite(PoolsCultDividedV1.MissionId);

            mission.Objective(North, 1642, 8054, 0, MissionObjectiveState.Incomplete);
            mission.Objective(East, 1643, 8055, 1, MissionObjectiveState.Incomplete);
            mission.Objective(West, 1644, 8056, 2, MissionObjectiveState.Incomplete);
            mission.Objective(Samples, 1645, 8057, 3, MissionObjectiveState.Incomplete);
            mission.Scenario(SampleScenario, "A water sample taken", (uint)Points.Length);

            for (var index = 0; index < Points.Length; index++)
            {
                var (objective, _, _, _, at) = Points[index];
                mission.Transition(objective);
                mission.Progress(objective, MissionProgressEventKind.ScenarioEvent, (uint)index + 1, null, counter: SampleScenario);
                if (index == 0)
                    mission.Action(objective, 1, MissionActionKind.GrantReward, reward: 1);
                mission.Indicator(objective, (uint)index + 1, at.X, at.Y, at.Z);
                mission.Indicator(Samples, (uint)Points.Length + (uint)index + 1, at.X, at.Y, at.Z);
            }

            var scene = new MissionSceneDefinition
            {
                Script = "data.sequence",
                ObjectiveAggregations = new() { [Samples] = new MissionObjectiveAggregation(new[] { North, East, West }, 3) },
                Items = new()
                {
                    new MissionItemBinding(VialKey, VialTemplate, MissionItemScope.AssignmentIssued, Vials,
                        MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove)
                },
                AcceptanceItems = new() { new IssueMissionItemIntent("issue-vials", MissionId, VialKey, VialTemplate, Vials) }
            };
            scene.Sequences[0] = new SceneSequenceDefinition();
            for (var index = 0; index < Points.Length; index++)
            {
                var (objective, template, _, key, at) = Points[index];
                var role = SpotRole(index);
                var sequence = (uint)index + 1;
                scene.Items.Add(new MissionItemBinding(key, template, MissionItemScope.AssignmentIssued, 1,
                    MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove));
                scene.Actors[role] = new SceneActorDefinition(role, SceneActorKind.Object, SpotClass, at,
                    InitialObjectState: ReadyState, UseAction: new SceneObjectAction(MissionId, objective, sequence, UseArgument));
                scene.Sequences[0].World.Add(new EnsureActorIntent($"create-{role}", role));
                scene.Sequences[sequence] = new SceneSequenceDefinition
                {
                    World =
                    {
                        new TransitionObjectStateIntent($"fill-{role}", role, TakenState),
                        new SetInteractionIntent($"close-{role}", role, false)
                    },
                    Character =
                    {
                        new ConsumeMissionItemIntent($"{role}-vial", MissionId, VialKey, 1, MissionItemScope.AssignmentIssued),
                        new IssueMissionItemIntent($"{role}-filled", MissionId, key, template, 1)
                    },
                    Signals = { new SceneMissionSignal(MissionId, SampleScenario, sequence) }
                };
            }

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/561",
                "Native1 'Collect Northern Water Sample' 1642/8054, 3 'Collect East Sample' 1643/8055, 4 'Collect West Sample' " +
                "1644/8056, 5 'Collect Water Samples' 1645/8057, no counters; no objectiveconversation. Texts 1634-1637, 8053. " +
                "Vial template700/class7766, samples 701-703/classes 7767-7769.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Testing_the_Pools",
                "Giver/receiver Jayson Venma (Retread Camp); requires A Cult Divided; 60000XP, 5250 credits; one of Titan Bio " +
                "Helmet35, Dynamo Hazmat Vest34, Phoenix Motor Assist Vest33, Phoenix Reflective Vest34; Eastern, Western, " +
                "Northern Water Sample 0/1. TR MMO DB level33.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:PoolsTestingThePoolsV1",
                "Three vials on accept; three personal spots (class11114; 7595 has the Default mesh) at polluted water north, east and west of the camp " +
                "(places estimated), any order, each a vial for its sample; 5 an aggregate of 1, 3, 4. Items 46020, 20931, 29227, " +
                "36168, one to choose.", 0.3);

            mission.Enable(scene);
        }

        public static void Down(MigrationBuilder migration) => PoolsMissionDataV1.Remove(migration, MissionId);
    }
}
