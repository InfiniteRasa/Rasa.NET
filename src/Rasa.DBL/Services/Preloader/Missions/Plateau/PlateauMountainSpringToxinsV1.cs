using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Plateau
{
    /// <summary>
    /// Mountain Spring Toxins (native mission 1979), Plateau: Outpost Cmdr. Russ at Wedge Rock Outpost sends the
    /// player into Velon Hollow (squad instance 2136) to find where the Bane put the genetic virus into the
    /// water, blow its four chemical towers and overload the water conversion machine inside the Bane outpost.
    ///
    /// Native: texts 20823-20827 (missionconversation): "We've procured some tactical explosives for you";
    /// objectives (missionobjective) 1 "Find the source of the toxin" 20906/20907, 2-5 "Sabotage Chemical Tower
    /// 1-4" 20908/20909, 20910/20911, 20912/20913, 20914/20915, 6 "Overload the Water Conversion Machine"
    /// 20916/20917 ("Explore the inside of the Bane Outpost and find the primary controls for the water
    /// conversion machine. Use the controls to overload and destroy the toxin mixer for good."); no counters,
    /// no objective conversation. Items and objects (the client's, none on its map): Hollow_Mission_Water_
    /// Converter_Bomb (class 29316, template 122243); UsableThreeStateBaneWaterConverterTowerV01-V04 (29148,
    /// 29282, 29286, 29287, augmentation 71: state 10000003 running, 10000001 the destroyed mesh, the
    /// 10000003-10000001 change with its blast); UsableFiveStateBaneWaterConverterEngineV01 (29146,
    /// augmentation 75: 10000007 running, 10000008 the destroyed mesh, the change with its blast);
    /// UsableThreeStateBaneWaterConverterMixerV01 (29091, 10000003 running, 10000001 stopped) (usabledata).
    /// Client map: the outpost's front with its infected ponds (WaterForeasPond64mInfected at 282.8/346.8,
    /// 32.7, 41.9 and 378.8, 32.7, -22.1), its control room ArchBaneIndustrialRoomControl24mV14 (455, 32.6, 103).
    ///
    /// Sourced: TaRapedia Mountain Spring Toxins: giver and receiver Outpost Cmdr. Russ, Wedge Rock Outpost;
    /// requirement Village Of The Damned; 90,000 XP, 6,400 credits, Purifier Vest (Velon Hollow mission list:
    /// the Purifier set). The client's Graviton, Stealth, Mech and Bio Vest (templates 122253-122256, the only
    /// named vests, beside the Velon Hollow items) are read as the set's vests.
    ///
    /// Reconstructed (ESTIMATE, the user's choices of 2026-10-22):
    ///  - Level 32, as Deadly Silence.
    ///  - Russ gives four bombs on accept. The scene is bound to Velon Hollow (MissionSceneDefinition.MapContextId):
    ///    its towers, engine and mixer come in the squad's copy when the player first arrives there, not on the
    ///    Plateau where Russ is. 1: done on walking up to the ponds before the outpost.
    ///  - 2-5: each tower (personal, by the ponds, among pool 531225's Bane) is used: it takes a bomb and blows.
    ///  - 6: open once the four are down (a hidden tally, objective <see cref="Tally"/>: 6's order in the
    ///    client is after them); the engine in the control room is used, it blows and the mixer beside it stops.
    ///  - The vests without the set module: module 900513 "Set: Purifier" has no class set on the server, so it
    ///    goes into no armor. No indicators: the goals are in an instance.
    /// </summary>
    public static class PlateauMountainSpringToxinsV1
    {
        public const uint MissionId = 1979;
        public const uint Level = 32;
        public const uint Russ = PlateauPerduNpcsV1.Russ;
        public const uint Find = 1, Overload = 6, Tally = 7;
        public const uint Ponds = 1;
        public const uint Sabotaged = 1, Overloaded = 2, TowersDown = 3;

        public const uint Bomb = 122243, BombClass = 29316, Bombs = 4;
        public const string BombKey = "water-converter-bomb";
        public const uint TowerRunningState = 10000003, TowerDestroyedState = 10000001;
        public const uint EngineClass = 29146, EngineRunningState = 10000007, EngineDestroyedState = 10000008;
        public const uint MixerClass = 29091, MixerRunningState = 10000003, MixerStoppedState = 10000001;
        public const uint UseArgument = 1;
        public const string EngineRole = "converter-engine", MixerRole = "toxin-mixer";

        public const uint Experience = 90000;
        public const uint Credits = 6400;
        public const uint GravitonVest = 122253, StealthVest = 122254, MechVest = 122255, BioVest = 122256;

        /// <summary>The ponds before the outpost: objective 1's area, round the towers.</summary>
        public static readonly (double X, double Y, double Z) PondsAt = (310, 34, 20);
        public const double PondsRadius = 55, PondsHeight = 30;

        /// <summary>Towers 1-4: (objective, class, name text, body text, place on the navmesh's ground).</summary>
        public static readonly (uint Objective, uint Class, uint Name, uint Body, ScenePosition At)[] Towers =
        {
            (2, 29148, 20908, 20909, new(300, 35.26f, 45)),
            (3, 29282, 20910, 20911, new(340, 35.55f, 45)),
            (4, 29286, 20912, 20913, new(260, 31.9f, 20)),
            (5, 29287, 20914, 20915, new(330, 31.7f, -22)),
        };

        public static readonly ScenePosition EngineAt = new(448, 32.3f, 98);
        public static readonly ScenePosition MixerAt = new(465, 32.41f, 96);

        public static string TowerRole(int index) => $"chemical-tower-{index + 1}";

        public static void Up(MigrationBuilder migration)
        {
            var mission = new PlateauMissionDataV1(migration, MissionId, "Mountain Spring Toxins", 20823, Russ, Russ, Level);
            mission.Reward(Experience, Credits, new[] { GravitonVest, StealthVest, MechVest, BioVest });
            mission.Prerequisite(PlateauVillageOfTheDamnedV1.MissionId);

            mission.Objective(Find, 20906, 20907, 0, MissionObjectiveState.Incomplete);
            foreach (var tower in Towers)
                mission.Objective(tower.Objective, tower.Name, tower.Body, tower.Objective - 1, MissionObjectiveState.Inactive);
            mission.Objective(Overload, 20916, 20917, 5, MissionObjectiveState.Inactive);
            mission.Objective(Tally, 20908, 20909, 6, MissionObjectiveState.Incomplete);   // counts the four towers
            mission.Scenario(Sabotaged, "Chemical towers blown", (uint)Towers.Length);
            mission.Scenario(Overloaded, "Water conversion machine overloaded", 1);
            mission.Scenario(TowersDown, "Towers counted for the machine", (uint)Towers.Length);
            mission.Area(Ponds, PlateauVelonHollowBaneV1.MapContextId, PondsAt.X, PondsAt.Y, PondsAt.Z, PondsRadius, PondsHeight);

            mission.Transition(Find);
            mission.EnterArea(Find, Ponds);
            for (var index = 0; index < Towers.Length; index++)
                PlateauSilentButDeadlyV1.Open(mission, Find, 1 + 2 * (uint)index, Towers[index].Objective);

            for (var index = 0; index < Towers.Length; index++)
            {
                mission.Transition(Towers[index].Objective);
                mission.Progress(Towers[index].Objective, MissionProgressEventKind.ScenarioEvent, (uint)index + 1, null, counter: Sabotaged);
            }

            // 6: open once the four towers are down, counted by the hidden tally.
            mission.Transition(Tally);
            for (var tower = 1U; tower <= Towers.Length; tower++)
                mission.Progress(Tally, MissionProgressEventKind.ScenarioEvent, tower, (uint)Towers.Length, counter: TowersDown, trigger: tower);
            PlateauSilentButDeadlyV1.Open(mission, Tally, 1, Overload);

            mission.Transition(Overload);
            mission.Progress(Overload, MissionProgressEventKind.ScenarioEvent, 1, null, counter: Overloaded);
            mission.Action(Overload, 1, MissionActionKind.GrantReward, reward: 1);

            var scene = new MissionSceneDefinition
            {
                Script = "data.sequence",
                MapContextId = PlateauVelonHollowBaneV1.MapContextId,   // accepted at Russ on the Plateau, played in Velon Hollow
                HiddenObjectiveIds = new() { Tally },
                Items = new()
                {
                    new MissionItemBinding(BombKey, Bomb, MissionItemScope.AssignmentIssued, Bombs,
                        MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove,
                        MissionItemCleanupDisposition.Remove)
                },
                AcceptanceItems = new()
                {
                    new IssueMissionItemIntent("issue-bombs", MissionId, BombKey, Bomb, Bombs)
                }
            };
            scene.Sequences[0] = new SceneSequenceDefinition();
            for (var index = 0; index < Towers.Length; index++)
            {
                var role = TowerRole(index);
                var sequence = (uint)index + 1;
                scene.Actors[role] = new SceneActorDefinition(role, SceneActorKind.Object, Towers[index].Class, Towers[index].At,
                    InitialObjectState: TowerRunningState,
                    UseAction: new SceneObjectAction(MissionId, Towers[index].Objective, sequence, UseArgument));
                scene.Sequences[0].World.Add(new EnsureActorIntent($"create-{role}", role));
                scene.Sequences[sequence] = new SceneSequenceDefinition
                {
                    World =
                    {
                        new TransitionObjectStateIntent($"blow-{role}", role, TowerDestroyedState),
                        new SetInteractionIntent($"done-{role}", role, false)
                    },
                    Character = { new ConsumeMissionItemIntent($"bomb-on-{role}", MissionId, BombKey, 1, MissionItemScope.AssignmentIssued) },
                    Signals =
                    {
                        new SceneMissionSignal(MissionId, Sabotaged, sequence),
                        new SceneMissionSignal(MissionId, TowersDown, sequence)
                    }
                };
            }

            const uint overload = 11;
            scene.Actors[EngineRole] = new SceneActorDefinition(EngineRole, SceneActorKind.Object, EngineClass, EngineAt,
                InitialObjectState: EngineRunningState, UseAction: new SceneObjectAction(MissionId, Overload, overload, UseArgument));
            scene.Actors[MixerRole] = new SceneActorDefinition(MixerRole, SceneActorKind.Object, MixerClass, MixerAt,
                InitialObjectState: MixerRunningState);
            scene.Sequences[0].World.Add(new EnsureActorIntent("create-engine", EngineRole));
            scene.Sequences[0].World.Add(new EnsureActorIntent("create-mixer", MixerRole));
            scene.Sequences[overload] = new SceneSequenceDefinition
            {
                World =
                {
                    new TransitionObjectStateIntent("overload-engine", EngineRole, EngineDestroyedState),
                    new SetInteractionIntent("engine-done", EngineRole, false),
                    new TransitionObjectStateIntent("stop-mixer", MixerRole, MixerStoppedState)
                },
                Signals = { new SceneMissionSignal(MissionId, Overloaded, 1) }
            };

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1979",
                "Native1 source 20906/20907, 2-5 towers 20908-20915, 6 machine 20916/20917, no counters, no objective " +
                "conversation. Texts 20823-20827. Bomb class29316/template122243; towers 29148/29282/29286/29287 (states " +
                "10000003, 10000001), engine 29146 (10000007, 10000008), mixer 29091 (usabledata). Outpost ponds and control room.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Mountain_Spring_Toxins",
                "Giver/receiver Outpost Cmdr. Russ, Wedge Rock Outpost; requirement Village Of The Damned; 90000XP, 6400 credits, " +
                "Purifier Vest (Velon Hollow mission list).");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:PlateauMountainSpringToxinsV1",
                "Level32. Four bombs on accept; 1 by the ponds before the outpost (area); 2-5 a personal tower each, used, takes " +
                "a bomb, blows; 6 after the four (hidden tally 7): the engine in the control room used, it blows, the mixer stops. " +
                "Vests 122253-122256 without set module 900513 (no class set). Places are estimates. No indicator (instance).", 0.3);

            mission.Enable(scene);
        }

        public static void Down(MigrationBuilder migration) => PlateauMissionDataV1.Remove(migration, MissionId);
    }
}
