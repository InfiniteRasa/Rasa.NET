using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Pools
{
    /// <summary>
    /// Deadly Moxins, er-Toxins (native mission 1134), Retread Caves (squad instance 1694), Valverde Pools: Dr. Donius
    /// gives the player toxic flares to set in the Mox cave; the last Mox come out and die.
    ///
    /// Native: texts 8033-8037 (missionconversation): log "Dr. Donius wants you to place the flares filled with toxins to
    /// kill the Mox in the Mox cave. When the flares are placed kill all the Mox you can find in the cave. When you're
    /// done report to Amee Corman.", offer "I've developed some toxins that should only affect them", turn-in "Well, that
    /// seems to have pretty much done the trick."; objectives 1 "Place Flares" 8038/8039 counter 8040 "Flares Placed", 2
    /// "Kill the Remaining Mox" 8041/8042 (missionobjective); no objective conversation. Item: 49705
    /// MisRetreadToxicMoxFlare (class 24056).
    ///
    /// Sourced: TaRapedia Deadly Moxins, er-Toxins: giver Dr. Donius, reward giver Ellen Lenee (Retread Caves); requires
    /// A Mox On Both Our Houses; 45,000 XP, 4,800 credits; one of Prodigy Bio Armor Boots lvl 30, Teleract Graviton Armor
    /// Boots lvl 30, Phoenix Mech Armor Boots lvl 30, Teleract Stealth Armor Boots lvl 30. Ten Ton Hammer Retread Caves
    /// guide: "place three flares where the flashing markers are in the Western Grotto. Then you need to kill the
    /// remaining mox (you only need to kill three more)"; "It states that you should turn in the mission to Amee Corman.
    /// You actually turn in the mission to Ellen Lenee." TR MMO DB: level 35, series Mox Clearing.
    ///
    /// Reconstructed (ESTIMATE; the user's choice of 2026-10-25: the receiver Amee Corman, as the client's log):
    ///  - Three flares at the accept; three personal item spots (class 7847 UsableItemAccHumGpsBeacon, the item acceptor of
    ///    Mapping the Pools; the client has no flare spot) in the Western Grotto's main room, any order, each takes one.
    ///  - With the third, 2 opens and three personal Mox (the caves' Mox, PoolsRetreadCavesNpcsV1) come up from the tunnel
    ///    at the bottom of the room at the player; a server-only hidden objective 3 counts their deaths (2 has no client
    ///    counter), and 2 completes with it and pays. Amee Corman in the Pools' Retread Camp takes the turn-in.
    ///  - Rewards the lowest Uncommon templates of the bands that hold those levels, one to choose. Markers in the instance too.
    /// </summary>
    public static class PoolsDeadlyMoxinsV1
    {
        public const uint MissionId = 1134;
        public const uint Level = 35;
        public const uint MapContextId = PoolsRetreadCavesNpcsV1.MapContextId;
        public const uint Donius = PoolsRetreadCavesNpcsV1.Donius, Amee = PoolsRetreadCampNpcsV1.Amee;
        public const uint PlaceFlares = 1, KillMox = 2, Tally = 3;
        public const uint FlaresPlaced = 5, MoxKilled = 6, MoxCome = 10;

        public const uint FlareTemplate = 49705, Flares = 3;   // MisRetreadToxicMoxFlare
        public const double ObjectRadius = 8, MoxRadius = 15;
        public const string FlareKey = "toxic-mox-flare";
        public const uint SpotClass = PoolsMappingThePoolsV1.SpotClass;   // UsableItemAccHumGpsBeacon
        public const uint SpotReadyState = PoolsMappingThePoolsV1.SpotReadyState, SpotPlantedState = PoolsMappingThePoolsV1.SpotPlantedState;
        public const uint UseArgument = PoolsMappingThePoolsV1.UseArgument;

        public const uint Experience = 45000;
        public const uint Credits = 4800;
        public const uint BioBoots = 18730;             // Armor_T3_Bio_V02_UNC_Boots_30_to_34
        public const uint GravitonBoots = 23529;        // Armor_T3_Graviton_V02_UNC_Boots_30_to_34
        public const uint MechBoots = 32657;            // Armor_T3_Mech_V02_UNC_Boots_30_to_34
        public const uint StealthBoots = 38783;         // Armor_T3_Stealth_V02_UNC_Boots_30_to_34

        /// <summary>The flare spots in the Western Grotto's main room.</summary>
        public static readonly ScenePosition[] Spots =
        {
            new(120f, 8.32f, -40f),
            new(168f, 14.25f, -48f),
            new(192f, 9.46f, -32f),
        };

        /// <summary>The last Mox, at the foot of the room's tunnel.</summary>
        public static readonly ScenePosition[] MoxAt =
        {
            new(207.2f, 0.85f, 26f),
            new(208f, 1.03f, 32f),
            new(200.8f, 3.25f, 24f),
        };

        public static string SpotRole(int index) => $"flare-spot-{index + 1}";
        public static string MoxRole(int index) => $"last-mox-{index + 1}";
        public static uint SpotSequence(int index) => 1 + (uint)index;
        public static uint MoxDown(int index) => 21 + (uint)index;

        public static void Up(MigrationBuilder migration)
        {
            var mission = new PoolsMissionDataV1(migration, MissionId, "Deadly Moxins, er-Toxins", 8033, Donius, Amee, Level);
            mission.Reward(Experience, Credits, new[] { BioBoots, GravitonBoots, MechBoots, StealthBoots });
            mission.Prerequisite(PoolsMoxOnBothOurHousesV1.MissionId);

            mission.Objective(PlaceFlares, 8038, 8039, 0, MissionObjectiveState.Incomplete, counterTextId: 8040);
            mission.Objective(KillMox, 8041, 8042, 1, MissionObjectiveState.Inactive);
            mission.Objective(Tally, 8041, 8042, 2, MissionObjectiveState.Incomplete);
            mission.Scenario(FlaresPlaced, "Toxic flares placed", Flares);
            mission.Scenario(MoxKilled, "The last Mox killed", (uint)MoxAt.Length);
            mission.Scenario(MoxCome, "The last Mox come out", 0);

            // 1: the three flares; 2 opens and the last Mox come.
            mission.Transition(PlaceFlares);
            for (var id = 1U; id <= Flares; id++)
                mission.Progress(PlaceFlares, MissionProgressEventKind.ScenarioEvent, id, Flares, counter: FlaresPlaced, trigger: id);
            mission.Action(PlaceFlares, 1, MissionActionKind.RevealObjective, targetObjective: KillMox);
            mission.Action(PlaceFlares, 2, MissionActionKind.ActivateObjective, targetObjective: KillMox, state: MissionObjectiveState.Incomplete);
            mission.Action(PlaceFlares, 3, MissionActionKind.StartScenario, scenario: MoxCome);

            // 2: their deaths, counted by the hidden tally.
            var mox = (uint)MoxAt.Length;
            mission.Transition(Tally);
            for (var id = 1U; id <= mox; id++)
                mission.Progress(Tally, MissionProgressEventKind.ScenarioEvent, id, mox, counter: MoxKilled, trigger: id);
            mission.Transition(KillMox);
            mission.ObjectiveState(KillMox, Tally, MissionObjectiveState.Completed);
            mission.Action(KillMox, 1, MissionActionKind.GrantReward, reward: 1);

            var scene = new MissionSceneDefinition
            {
                Script = "data.sequence",
                MapContextId = MapContextId,
                DefeatSequences = new(),
                HiddenObjectiveIds = new() { Tally },
                Items = new()
                {
                    new MissionItemBinding(FlareKey, FlareTemplate, MissionItemScope.AssignmentIssued, Flares,
                        MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove)
                },
                AcceptanceItems = new() { new IssueMissionItemIntent("issue-flares", MissionId, FlareKey, FlareTemplate, Flares) }
            };
            scene.Sequences[0] = new SceneSequenceDefinition();
            for (var index = 0; index < Spots.Length; index++)
            {
                var role = SpotRole(index);
                var sequence = SpotSequence(index);
                scene.Actors[role] = new SceneActorDefinition(role, SceneActorKind.Object, SpotClass, Spots[index],
                    InitialObjectState: SpotReadyState, UseAction: new SceneObjectAction(MissionId, PlaceFlares, sequence, UseArgument));
                scene.Sequences[0].World.Add(new EnsureActorIntent($"create-{role}", role));
                scene.Sequences[sequence] = new SceneSequenceDefinition
                {
                    World =
                    {
                        new TransitionObjectStateIntent($"plant-{role}", role, SpotPlantedState),
                        new SetInteractionIntent($"close-{role}", role, false)
                    },
                    Character = { new ConsumeMissionItemIntent($"{role}-flare", MissionId, FlareKey, 1, MissionItemScope.AssignmentIssued) },
                    Signals = { new SceneMissionSignal(MissionId, FlaresPlaced, (uint)index + 1) }
                };
            }
            var come = new SceneSequenceDefinition();
            for (var index = 0; index < MoxAt.Length; index++)
            {
                var role = MoxRole(index);
                scene.Actors[role] = new SceneActorDefinition(role, SceneActorKind.Creature, PoolsRetreadCavesNpcsV1.Mox, MoxAt[index],
                    Orientation: 3.1416, GameplayPolicy: PoolsSabotageTheTowerV1.Hostile());
                come.World.Add(new EnsureActorIntent($"{role}-comes", role));
                come.World.Add(QuestSceneIntents.Attack($"{role}-attacks", role));
                scene.DefeatSequences[role] = MoxDown(index);
                scene.Sequences[MoxDown(index)] = new SceneSequenceDefinition
                {
                    Signals = { new SceneMissionSignal(MissionId, MoxKilled, (uint)index + 1) }
                };
            }
            scene.Sequences[MoxCome] = come;

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1134",
                "Native1 'Place Flares' 8038/8039 counter8040 'Flares Placed', 2 'Kill the Remaining Mox' 8041/8042; no " +
                "objectiveconversation. Texts 8033-8037 (report to Amee Corman). Item 49705 MisRetreadToxicMoxFlare.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Deadly_Moxins,_er-Toxins",
                "Giver Dr. Donius, reward Ellen Lenee (Retread Caves); requires A Mox On Both Our Houses; 45000XP, 4800 credits; one " +
                "of Prodigy Bio Armor Boots30, Teleract Graviton Armor Boots30, Phoenix Mech Armor Boots30, Teleract Stealth Armor " +
                "Boots30. Ten Ton Hammer: three flares at markers in the Western Grotto, then three more Mox; the log's Amee a typo " +
                "for Ellen. TR MMO DB level35.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:PoolsDeadlyMoxinsV1",
                "Receiver Amee Corman as the client's log (user). Three flares at accept, three personal spots 7847 in the Grotto, " +
                "then three personal Mox 760230 from the tunnel; hidden 3 counts their deaths, 2 with it pays. Items 18730, " +
                "23529, 32657, 38783, one to choose. Places are estimates. Indicators in the instance too.", 0.3);

            for (var index = 0; index < Spots.Length; index++)
                mission.Indicator(PlaceFlares, 1 + (uint)index, Spots[index].X, Spots[index].Y, Spots[index].Z, ObjectRadius);
            mission.Indicator(KillMox, 1 + (uint)Spots.Length, MoxAt[0].X, MoxAt[0].Y, MoxAt[0].Z, MoxRadius);

            mission.Enable(scene);
        }

        public static void Down(MigrationBuilder migration) => PoolsMissionDataV1.Remove(migration, MissionId);
    }
}
