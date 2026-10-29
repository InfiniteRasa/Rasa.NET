using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Pools
{
    /// <summary>
    /// Miasma Testing (native mission 1136), Retread Caves (squad instance 1694), Valverde Pools: Dr. Donius gives the
    /// player a pheromone vial to lead three docile miasmas, one at a time, to H.P. Miles for his tests.
    ///
    /// Native: texts 8064-8068 (missionconversation): log "Bring back the docile Miasmas for testing.", offer "Some of the
    /// miasma, fortunately, have developed docile qualities. I'm going to give you this vial of pheremones ... I'm hoping
    /// that they'll follow the smell and you'll be able to lead them", turn-in "Well, these tests are conclusive. There's
    /// really only one thing to do."; objectives (missionobjective) 1 "Find the Docile Miasma" 8100/8101, 2 "Return to HP
    /// Miles" 8102/8103, 3 "Speak with H.P." 8104/8105, 4 "Acquire Test Miasma" 8106/8107, 5 "Return to H.P. Miles"
    /// 8108/8109, 6 "Speak to H.P." 8110/8111, 7 "Acquire Test Miasma" 8112/8113, 8 "Return to H.P." 8114/8115, 9 "Speak
    /// to H.P." 8117/8118, 10 "Speak To H.P." 8121/8122, 11 "Speak To H.P." 8124/8125; objectiveconversation package 851:
    /// 3 text 8116 "let's see what this new concoction I've put together does to him...", 6 text 8120 "let's see if this
    /// second batch does any better...", 9 text 8119 "that didn't work. Fortunately I know where another one of these
    /// docile miasmas is. Go get it", 10 text 8123 "I have one more test to run. I think I know where one last Miasma can
    /// be found.", 11 text 8126 "This is the final test...".
    ///
    /// Sourced: TaRapedia Miasma Testing: giver Dr. Donius, reward giver H.P. Miles (Retread Caves); requires Miasma
    /// Mutations; 45,000 XP, 4,800 credits; one of Astra Bio Armor Vest lvl 30, Titan Graviton Armor Vest lvl 30, Astra
    /// Mech Armor Legs lvl 30, Phoenix Stealth Armor Vest lvl 30. Ten Ton Hammer Retread Caves guide: "Follow the mission
    /// markers as provided. H.P. Miles is next to Gun Dealer Doris (the lowest level of the Retread Camp next to the fire
    /// pit). The radio marker over H.P. Miles head doesnt appear until your escort is close to him." TR MMO DB: level 35.
    ///
    /// Reconstructed (ESTIMATE; the user's choice of 2026-10-25: a personal scripted scene as the client's steps):
    ///  - Three rounds, in the texts' order: find (1, 4, 7), return (2, 5, 8), Miles tests (3, 6, 11); after the first
    ///    and second tests Miles sends the player for the next (9, 10).
    ///  - Three personal Docile Miasmas (PoolsRetreadCavesNpcsV1) stand in the camp's part of the caves (the Mox Lair
    ///    has no path to the camp): on the branch above the camp, on the camp's west ledge, and up the branch towards the
    ///    main passage. Coming within 6 m of the round's miasma completes its find; it then follows the player. Coming
    ///    within 8 m of Miles completes the return (the server checks the player, not the miasma). Each test takes its
    ///    miasma away.
    ///  - Rewards the lowest Uncommon templates of the bands that hold those levels, one to choose. Markers in the instance too.
    /// </summary>
    public static class PoolsMiasmaTestingV1
    {
        public const uint MissionId = 1136;
        public const uint Level = 35;
        public const uint MapContextId = PoolsRetreadCavesNpcsV1.MapContextId;
        public const uint Donius = PoolsRetreadCavesNpcsV1.Donius, Miles = PoolsRetreadCavesNpcsV1.Miles;
        public const uint MilesArea = 4;
        public const double FindRadius = 6, MilesRadius = 8;

        public const uint Experience = 45000;
        public const uint Credits = 4800;
        public const uint BioVest = 19246;              // Armor_T3_Bio_V02_UNC_Vest_30_to_34
        public const uint GravitonVest = 24046;         // Armor_T3_Graviton_V02_UNC_Vest_30_to_34
        public const uint MechLegs = 33044;             // Armor_T3_Mech_V02_UNC_Legs_30_to_34
        public const uint StealthVest = 39300;          // Armor_T3_Stealth_V02_UNC_Vest_30_to_34

        /// <summary>
        /// The rounds: the find, return and test objectives (name and body texts), the objective that sends the player
        /// for the next round (0 after the last), the miasma's place.
        /// </summary>
        public static readonly (uint Find, uint FindName, uint FindBody, uint Return, uint ReturnName, uint ReturnBody,
            uint Test, uint TestName, uint TestBody, uint Next, uint NextName, uint NextBody, ScenePosition At)[] Rounds =
        {
            (1, 8100, 8101, 2, 8102, 8103, 3, 8104, 8105, 9, 8117, 8118, new(-32f, 74.28f, -144f)),
            (4, 8106, 8107, 5, 8108, 8109, 6, 8110, 8111, 10, 8121, 8122, new(-63.2f, 64.69f, -247.2f)),
            (7, 8112, 8113, 8, 8114, 8115, 11, 8124, 8125, 0, 0, 0, new(8f, 74.45f, -104f)),
        };

        public static string DocileRole(int index) => $"docile-miasma-{index + 1}";
        public static uint FollowSequence(int index) => 11 + 2 * (uint)index;
        public static uint TestSequence(int index) => 12 + 2 * (uint)index;
        public static uint FindArea(int index) => 1 + (uint)index;

        public static void Up(MigrationBuilder migration)
        {
            var mission = new PoolsMissionDataV1(migration, MissionId, "Miasma Testing", 8064, Donius, Miles, Level);
            mission.Reward(Experience, Credits, new[] { BioVest, GravitonVest, MechLegs, StealthVest });
            mission.Prerequisite(PoolsMiasmaMutationsV1.MissionId);

            var miles = PoolsRetreadCavesNpcsV1.MilesAt;
            mission.Area(MilesArea, MapContextId, miles.X, miles.Y, miles.Z, MilesRadius);
            var ordinal = 0U;
            for (var index = 0; index < Rounds.Length; index++)
            {
                var round = Rounds[index];
                mission.Objective(round.Find, round.FindName, round.FindBody, ordinal++,
                    index == 0 ? MissionObjectiveState.Incomplete : MissionObjectiveState.Inactive);
                mission.Objective(round.Return, round.ReturnName, round.ReturnBody, ordinal++, MissionObjectiveState.Inactive);
                mission.Objective(round.Test, round.TestName, round.TestBody, ordinal++, MissionObjectiveState.Inactive);
                if (round.Next != 0)
                    mission.Objective(round.Next, round.NextName, round.NextBody, ordinal++, MissionObjectiveState.Inactive);
                mission.Scenario(FollowSequence(index), $"Docile miasma {index + 1} follows", 0);
                mission.Scenario(TestSequence(index), $"Docile miasma {index + 1} tested", 0);
                mission.Area(FindArea(index), MapContextId, round.At.X, round.At.Y, round.At.Z, FindRadius);
            }
            for (var index = 0; index < Rounds.Length; index++)
            {
                var round = Rounds[index];

                // Find: the miasma follows; return: at Miles; test: Miles takes it.
                mission.Transition(round.Find);
                mission.EnterArea(round.Find, FindArea(index));
                Open(mission, round.Find, 1, round.Return);
                mission.Action(round.Find, 3, MissionActionKind.StartScenario, scenario: FollowSequence(index));
                mission.Transition(round.Return);
                mission.EnterArea(round.Return, MilesArea);
                Open(mission, round.Return, 1, round.Test);
                mission.Transition(round.Test);
                mission.Conversation(round.Test, PoolsRetreadCavesNpcsV1.MilesPackage);
                mission.Action(round.Test, 1, MissionActionKind.StartScenario, scenario: TestSequence(index));
                if (round.Next == 0)
                {
                    mission.Action(round.Test, 2, MissionActionKind.GrantReward, reward: 1);
                    continue;
                }
                Open(mission, round.Test, 2, round.Next);
                // Next: Miles sends the player for the next round's miasma.
                mission.Transition(round.Next);
                mission.Conversation(round.Next, PoolsRetreadCavesNpcsV1.MilesPackage);
                Open(mission, round.Next, 1, Rounds[index + 1].Find);
            }

            var scene = new MissionSceneDefinition { Script = "data.sequence", MapContextId = MapContextId };
            scene.Sequences[0] = new SceneSequenceDefinition();
            for (var index = 0; index < Rounds.Length; index++)
            {
                var role = DocileRole(index);
                scene.Actors[role] = new SceneActorDefinition(role, SceneActorKind.Creature, PoolsRetreadCavesNpcsV1.DocileMiasma,
                    Rounds[index].At, Orientation: index * 2.0943951024, FollowOffset: new ScenePosition(0f, 0f, -2.5f));
                scene.Sequences[0].World.Add(new EnsureActorIntent($"create-{role}", role));
                scene.Sequences[FollowSequence(index)] = new SceneSequenceDefinition
                {
                    World = { new FollowActorIntent($"{role}-follows", role, 0, Enabled: true) }
                };
                scene.Sequences[TestSequence(index)] = new SceneSequenceDefinition
                {
                    World =
                    {
                        new FollowActorIntent($"{role}-stops", role, 0, Enabled: false),
                        new RemoveActorIntent($"{role}-tested", role)
                    }
                };
            }

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1136",
                "Native1-11: find/acquire 1, 4, 7; return 2, 5, 8; speak 3, 6, 9, 10, 11 (objectiveconversation package851 texts " +
                "8116, 8120, 8119, 8123, 8126). Texts 8064-8068 (a pheromone vial; the docile miasmas follow). Name 9038 Docile Miasma.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Miasma_Testing",
                "Giver Dr. Donius, reward H.P. Miles (Retread Caves); requires Miasma Mutations; 45000XP, 4800 credits; one of Astra " +
                "Bio Armor Vest30, Titan Graviton Armor Vest30, Astra Mech Armor Legs30, Phoenix Stealth Armor Vest30. Ten Ton " +
                "Hammer: Miles at the lowest level of the camp by the fire pit; an escort. TR MMO DB level35.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:PoolsMiasmaTestingV1",
                "Personal scene (user): three rounds in the texts' order; three personal Docile Miasmas 760227 in the camp's part " +
                "of the caves follow the player from 6 m; within 8 m of Miles (the player) returns; each test takes its miasma. " +
                "Items 19246, 24046, 33044, 39300, one to choose. Places are estimates. Indicators in the instance too.", 0.3);

            // Find at the round's miasma; return, test and the next round at Miles.
            var marker = 0U;
            foreach (var round in Rounds)
            {
                mission.Indicator(round.Find, ++marker, round.At.X, round.At.Y, round.At.Z, FindRadius);
                foreach (var objective in round.Next != 0 ? new[] { round.Return, round.Test, round.Next } : new[] { round.Return, round.Test })
                    mission.Indicator(objective, ++marker, miles.X, miles.Y, miles.Z, MilesRadius);
            }

            mission.Enable(scene);
        }

        /// <summary>Reveals and opens <paramref name="target"/> when <paramref name="objective"/> completes.</summary>
        private static void Open(PoolsMissionDataV1 mission, uint objective, uint first, uint target)
        {
            mission.Action(objective, first, MissionActionKind.RevealObjective, targetObjective: target);
            mission.Action(objective, first + 1, MissionActionKind.ActivateObjective, targetObjective: target,
                state: MissionObjectiveState.Incomplete);
        }

        public static void Down(MigrationBuilder migration) => PoolsMissionDataV1.Remove(migration, MissionId);
    }
}
