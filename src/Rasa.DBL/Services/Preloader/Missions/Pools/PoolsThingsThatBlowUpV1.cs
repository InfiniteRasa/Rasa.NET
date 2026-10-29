using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Pools
{
    /// <summary>
    /// Things That Blow Up (native mission 882), Turpis Refinery (squad instance 1429), Valverde Pools: Demolitionist
    /// Niebolo gives the player explosives to blow a way through the cave's wall into the refinery and to plant five more
    /// bombs inside.
    ///
    /// Native: texts 4620-4623, 7320 (missionconversation): log "Use the explosives to blast your way in and to destroy
    /// everything on your way out, then go back and see Demolitionist Niebolo.", offer "start with the wall just on the
    /// other side of this mountain. It leads directly into Turpis ... Make sure you blow it all up.", turn-in "I've never
    /// seen an explosion quite like that one."; objectives 1 "Make Your Way In" 4624/7321 counter 6205 "Door Blown", 3-7
    /// "Plant the First/Second/Third/Fourth/Fifth Bomb" 12581-12590 (missionobjective); no objective 2; no objective
    /// conversation. Item: 42427 MisTurpisBombsToPlant (class 21074, "Explosives"). Bomb acceptor: 7334
    /// UsableItemAccHumBombV01 (augmentation 43, as the GPS beacon; use argument 1, usabledata).
    ///
    /// Sourced: TaRapedia Things That Blow Up: giver and receiver Demolitionist Niebolo (Turpis Refinery); no requirement;
    /// level 33; 50,000 XP, 4,950 credits; one of Astra Bio Armor Gloves lvl 30, Astra Graviton Armor Helmet lvl 30,
    /// Prodigy Mech Armor Gloves lvl 30, Phoenix Stealth Armor Gloves lvl 30. Ten Ton Hammer Turpis Refinery guide: "talk
    /// to Demolitionist Niebolo ... plant several bombs. The first bomb will blow a hole in the wall separating the cave
    /// from the facility"; "There are also bombs to plant according to the mission markers". TR MMO DB: level 33.
    ///
    /// Reconstructed (ESTIMATE):
    ///  - Six explosives at the accept (one stack). A personal bomb acceptor at the wall, where the landing zone's navmesh
    ///    ends 8 m short of the facility's (navmesh probe 2026-10-25): its bomb blows the way in, 1 completes and 3-7
    ///    open. The client's wall is not on the server (no such entity on the client's map; the player walks on the
    ///    client's ground), so the way is open before too.
    ///  - Five personal bomb acceptors inside, along the horseshoe (the entrance hall, the west arm, the barracks, the
    ///    experimentation area at the top, the east arm); any order, each takes one bomb and completes its objective. The
    ///    turn-in at Niebolo pays. No explosion object.
    ///  - Rewards the lowest Uncommon templates of the bands that hold level 30, one to choose. Markers in the instance too.
    /// </summary>
    public static class PoolsThingsThatBlowUpV1
    {
        public const uint MissionId = 882;
        public const uint Level = 33;
        public const uint MapContextId = PoolsTurpisRefineryNpcsV1.MapContextId;
        public const uint Niebolo = PoolsTurpisRefineryNpcsV1.Niebolo;
        public const uint WayIn = 1, FirstBomb = 3, Bombs = 5;
        public const uint DoorBlown = 1, BombsPlanted = 2;

        public const uint ExplosivesTemplate = 42427, Explosives = 6;   // MisTurpisBombsToPlant
        public const string ExplosivesKey = "turpis-explosives";
        public const uint SpotClass = 7334;             // UsableItemAccHumBombV01
        public const uint SpotReadyState = PoolsMappingThePoolsV1.SpotReadyState, SpotPlantedState = PoolsMappingThePoolsV1.SpotPlantedState;
        public const uint UseArgument = 1;
        public const string WallRole = "wall-bomb";

        public const uint Experience = 50000;
        public const uint Credits = 4950;
        public const uint BioGloves = 18859;            // Armor_T3_Bio_V02_UNC_Gloves_30_to_34
        public const uint GravitonHelmet = 23788;       // Armor_T3_Graviton_V02_UNC_Helmet_30_to_34
        public const uint MechGloves = 32786;           // Armor_T3_Mech_V02_UNC_Gloves_30_to_34
        public const uint StealthGloves = 38912;        // Armor_T3_Stealth_V02_UNC_Gloves_30_to_34

        public static readonly ScenePosition WallAt = new(-280f, 125.27f, -200f);
        public const double ObjectRadius = 8;

        /// <summary>The five bombs inside, objectives 3-7 in order.</summary>
        public static readonly ScenePosition[] BombsAt =
        {
            new(-200.2f, 108.97f, -120.9f),     // the entrance hall
            new(-184f, 108.71f, 8f),            // the west arm
            new(-104f, 109.75f, 80f),           // the barracks
            new(-40f, 99.8f, 224f),             // the experimentation area
            new(48f, 106.27f, 40f),             // the east arm
        };

        public static uint BombObjective(int index) => FirstBomb + (uint)index;
        public static string BombRole(int index) => $"bomb-{index + 1}";
        public static uint BombSequence(int index) => 2 + (uint)index;

        public static void Up(MigrationBuilder migration)
        {
            var mission = new PoolsMissionDataV1(migration, MissionId, "Things That Blow Up", 4620, Niebolo, Niebolo, Level);
            mission.Reward(Experience, Credits, new[] { BioGloves, GravitonHelmet, MechGloves, StealthGloves });

            mission.Objective(WayIn, 4624, 7321, 0, MissionObjectiveState.Incomplete, counterTextId: 6205);
            for (var index = 0; index < BombsAt.Length; index++)
                mission.Objective(BombObjective(index), 12581 + 2 * (uint)index, 12582 + 2 * (uint)index, 1 + (uint)index,
                    MissionObjectiveState.Inactive);
            mission.Scenario(DoorBlown, "The wall blown", 1);
            mission.Scenario(BombsPlanted, "A bomb planted inside", Bombs);

            // 1: the wall; 3-7 open.
            mission.Transition(WayIn);
            mission.Progress(WayIn, MissionProgressEventKind.ScenarioEvent, 1, 1, counter: DoorBlown);
            for (var index = 0; index < BombsAt.Length; index++)
            {
                mission.Action(WayIn, 1 + 2 * (uint)index, MissionActionKind.RevealObjective, targetObjective: BombObjective(index));
                mission.Action(WayIn, 2 + 2 * (uint)index, MissionActionKind.ActivateObjective, targetObjective: BombObjective(index),
                    state: MissionObjectiveState.Incomplete);
            }

            // 3-7: a bomb each.
            for (var index = 0; index < BombsAt.Length; index++)
            {
                mission.Transition(BombObjective(index));
                mission.Progress(BombObjective(index), MissionProgressEventKind.ScenarioEvent, (uint)index + 1, null, counter: BombsPlanted);
            }
            mission.Action(BombObjective(BombsAt.Length - 1), 1, MissionActionKind.GrantReward, reward: 1);

            var scene = new MissionSceneDefinition
            {
                Script = "data.sequence",
                MapContextId = MapContextId,
                Items = new()
                {
                    new MissionItemBinding(ExplosivesKey, ExplosivesTemplate, MissionItemScope.AssignmentIssued, Explosives,
                        MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove)
                },
                AcceptanceItems = new() { new IssueMissionItemIntent("issue-explosives", MissionId, ExplosivesKey, ExplosivesTemplate, Explosives) }
            };
            scene.Sequences[0] = new SceneSequenceDefinition();
            scene.Actors[WallRole] = new SceneActorDefinition(WallRole, SceneActorKind.Object, SpotClass, WallAt,
                InitialObjectState: SpotReadyState, UseAction: new SceneObjectAction(MissionId, WayIn, 1, UseArgument));
            scene.Sequences[0].World.Add(new EnsureActorIntent($"create-{WallRole}", WallRole));
            scene.Sequences[1] = Plant(WallRole, DoorBlown, 1);
            for (var index = 0; index < BombsAt.Length; index++)
            {
                var role = BombRole(index);
                scene.Actors[role] = new SceneActorDefinition(role, SceneActorKind.Object, SpotClass, BombsAt[index],
                    InitialObjectState: SpotReadyState,
                    UseAction: new SceneObjectAction(MissionId, BombObjective(index), BombSequence(index), UseArgument));
                scene.Sequences[0].World.Add(new EnsureActorIntent($"create-{role}", role));
                scene.Sequences[BombSequence(index)] = Plant(role, BombsPlanted, (uint)index + 1);
            }

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/882",
                "Native1 'Make Your Way In' 4624/7321 counter6205 'Door Blown', 3-7 'Plant the First..Fifth Bomb' 12581-12590; no " +
                "objective 2; no objectiveconversation. Texts 4620-4623, 7320. Item 42427 MisTurpisBombsToPlant; acceptor 7334.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Things_That_Blow_Up",
                "Giver/receiver Demolitionist Niebolo (Turpis Refinery); no requirement; level33; 50000XP, 4950 credits; one of Astra " +
                "Bio Armor Gloves30, Astra Graviton Armor Helmet30, Prodigy Mech Armor Gloves30, Phoenix Stealth Armor Gloves30. Ten " +
                "Ton Hammer: the first bomb blows the wall between the cave and the facility, more bombs at the markers.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:PoolsThingsThatBlowUpV1",
                "Six explosives at accept; a personal acceptor 7334 at the wall (where the navmesh breaks), then five inside along " +
                "the horseshoe, any order. No wall entity, no explosion object. Items 18859, 23788, 32786, 38912, one to choose. " +
                "Places are estimates. Indicators in the instance too.", 0.3);

            mission.Indicator(WayIn, 1, WallAt.X, WallAt.Y, WallAt.Z, ObjectRadius);
            for (var index = 0; index < BombsAt.Length; index++)
                mission.Indicator(BombObjective(index), 2 + (uint)index, BombsAt[index].X, BombsAt[index].Y, BombsAt[index].Z, ObjectRadius);

            mission.Enable(scene);
        }

        public static void Down(MigrationBuilder migration) => PoolsMissionDataV1.Remove(migration, MissionId);

        /// <summary>A bomb planted at <paramref name="role"/>: it takes one, and passes on the scene event.</summary>
        private static SceneSequenceDefinition Plant(string role, uint scenario, uint eventId) => new()
        {
            World =
            {
                new TransitionObjectStateIntent($"plant-{role}", role, SpotPlantedState),
                new SetInteractionIntent($"close-{role}", role, false)
            },
            Character = { new ConsumeMissionItemIntent($"{role}-explosive", MissionId, ExplosivesKey, 1, MissionItemScope.AssignmentIssued) },
            Signals = { new SceneMissionSignal(MissionId, scenario, eventId) }
        };
    }
}
