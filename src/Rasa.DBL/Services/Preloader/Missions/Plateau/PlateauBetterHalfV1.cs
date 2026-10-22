using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;
using static Rasa.Services.Preloader.Missions.Plateau.PlateauCreatureSqlV1;

namespace Rasa.Services.Preloader.Missions.Plateau
{
    /// <summary>
    /// The Better Half (native mission 1975), Plateau: Science Officer Clark at the Wedge Rock sends the player
    /// into Velon Hollow after his wife Tala. A Thrax Maxxum, Quarsh, holds her: the player kills him and she
    /// dies, or lets him escape and she lives. Clark hears the end either way.
    ///
    /// Native: name/log/offer/turn-in/reminder texts 20803-20807 (missionconversation; 20805 "Please, find my
    /// wife and bring her home."); objectives 1 "Find Tala Clark" 20871/20872 ("Explore Velon Hollow and find
    /// any evidence you can of Science Officer Tala Clark."), 2 "Decision: Let Maxxum Quarsh escape or sacrifice
    /// Tala Clark?" 20873/20874 ("Your actions in the next few seconds will decide who walks out of here alive."),
    /// 3 "Inform Officer Clark of Tala's Death" 20875/20876, 4 "Inform Officer Clark that Tala is alive"
    /// 20877/20878, 5 "Required, Completed when either End objective finished" 20879/20880, no counters
    /// (missionobjective). Clark's talks on package 407 (objectiveconversation): 3 20881 ("You had to choose
    /// between letting my sweet Tala live and executing a Thrax Maxxum?"), 4 20882 ("My wife arrived shortly
    /// before you did."). Names 10194 "Maxxum Quarsh", 10528 "Tala Clark".
    ///
    /// Sourced: TaRapedia The Better Half (Velon Hollow mission list): giver and receiver Science Officer Clark,
    /// requirement Village Of The Damned (1981), 90,000 XP, 6,400 credits, Purifier Helmet.
    ///
    /// Reconstructed (ESTIMATE, user decisions): level 32, as the other Velon Hollow missions. In the upper
    /// control room of the squad's copy (a bound scene, PlateauMountainSpringToxinsV1) Quarsh holds Tala and a
    /// clock of <see cref="EscapeMs"/> runs: Quarsh killed before it ends, Tala dies (3); the clock out, Quarsh
    /// escapes and Tala goes home (4). The same reward on both ends: the four Purifier Helmets.
    /// </summary>
    public static class PlateauBetterHalfV1
    {
        public const uint MissionId = 1975;
        public const uint Level = 32;
        public const uint Clark = PlateauPerduNpcsV1.Clark, ClarkPackage = PlateauPerduNpcsV1.ClarkPackage;
        public const uint Find = 1, Decide = 2, Dead = 3, Alive = 4, Ended = 5;
        public const uint Room = 1;

        /// <summary>Scenario and sequence 1: Quarsh and Tala, the clock on. Scenario 2: its two ends, as steps.</summary>
        public const uint Hostage = 1, Outcome = 2;
        public const uint Sacrificed = 1, Escaped = 2;
        public const uint QuarshDown = 11, Escape = 12;
        public const string EscapeTimer = "quarsh-escapes";
        public const uint EscapeMs = 20000;
        public const string QuarshRole = "quarsh", TalaRole = "tala";

        public const uint Quarsh = 650080, Tala = 650081;
        public const uint QuarshName = 10194, TalaName = 10528;
        public const uint QuarshHitPoints = 3177, QuarshArmor = 150;   // a level 32 Thrax, one third of the officer's
        public const uint FemaleClass = 3848;            // NPC_Human_Swapset_Female
        public const uint TalaOutfitOf = 510030;         // Lieutenant Sherer - Outpost Intrepid
        public const uint TalaLevel = 30;

        public const uint Experience = 90000;
        public const uint Credits = 6400;
        public const uint GravitonHelmet = 122258, StealthHelmet = 122260, MechHelmet = 122262, BioHelmet = 122264;

        /// <summary>The upper control room (ArchBaneIndustrialRoomControl24mV07): its floor is a navmesh island of its own.</summary>
        public static readonly (double X, double Y, double Z) RoomAt = (364, 64.3, 126);
        public const double RoomRadius = 12, RoomHeight = 8;
        public static readonly ScenePosition TalaAt = new(364, 64.3f, 126);
        public static readonly ScenePosition QuarshAt = new(366.7f, 64.3f, 127.7f);

        public static IEnumerable<string> Creatures()
        {
            yield return Creature(Quarsh, "Maxxum Quarsh - Velon Hollow", PlateauVelonHollowBaneV1.ThraxClass, Level, QuarshHitPoints,
                QuarshName, PlateauVelonHollowBaneV1.ThraxKick33, PlateauVelonHollowBaneV1.ThraxWeapon33);
            yield return Stat(Quarsh, QuarshHitPoints, QuarshArmor);
            yield return "INSERT INTO creature (id, comment, class_id, faction, level, max_hp, name_id, run_speed, walk_speed, " +
                         "action1, action2, action3, action4, action5, action6, action7, action8) " +
                         $"VALUES ({Tala}, 'Science Officer Tala Clark - Velon Hollow', {FemaleClass}, 1, {TalaLevel}, 1000, {TalaName}, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);";
            yield return "INSERT INTO creature_appearance (id, slot_id, Class_id, color) " +
                         $"SELECT {Tala}, slot_id, Class_id, color FROM creature_appearance WHERE id = {TalaOutfitOf};";
        }

        public static IEnumerable<string> RemoveCreatures()
        {
            yield return $"DELETE FROM creature_stat WHERE id = {Quarsh};";
            yield return $"DELETE FROM creature_appearance WHERE id = {Tala};";
            yield return $"DELETE FROM creature WHERE id IN ({Quarsh}, {Tala});";
        }

        public static void Up(MigrationBuilder migration)
        {
            foreach (var sql in Creatures())
                migration.Sql(sql);

            var mission = new PlateauMissionDataV1(migration, MissionId, "The Better Half", 20803, Clark, Clark, Level);
            mission.Reward(Experience, Credits, new[] { GravitonHelmet, StealthHelmet, MechHelmet, BioHelmet });
            mission.Prerequisite(PlateauVillageOfTheDamnedV1.MissionId);

            mission.Objective(Find, 20871, 20872, 0, MissionObjectiveState.Incomplete);
            mission.Objective(Decide, 20873, 20874, 1, MissionObjectiveState.Inactive);
            mission.Objective(Dead, 20875, 20876, 2, MissionObjectiveState.Inactive, required: false);
            mission.Objective(Alive, 20877, 20878, 3, MissionObjectiveState.Inactive, required: false);
            mission.Objective(Ended, 20879, 20880, 4, MissionObjectiveState.Incomplete);
            mission.Scenario(Hostage, "Quarsh holds Tala, the clock runs", 0);
            mission.Scenario(Outcome, "Quarsh killed or escaped", 2);
            mission.Area(Room, PlateauVelonHollowBaneV1.MapContextId, RoomAt.X, RoomAt.Y, RoomAt.Z, RoomRadius, RoomHeight);

            mission.Transition(Find);
            mission.EnterArea(Find, Room);
            PlateauSilentButDeadlyV1.Open(mission, Find, 1, Decide);
            mission.Action(Find, 3, MissionActionKind.StartScenario, scenario: Hostage);

            // 2: Quarsh killed opens 3, the clock out opens 4.
            mission.Transition(Decide);
            mission.Progress(Decide, MissionProgressEventKind.ScenarioEvent, Sacrificed, null, counter: Outcome);
            Open(mission, Decide, Dead, 1);
            mission.Transition(Decide, 2, 2);
            mission.Progress(Decide, 2, MissionProgressEventKind.ScenarioEvent, Escaped, null, counter: Outcome);
            Open(mission, Decide, Alive, 2);

            foreach (var end in new[] { Dead, Alive })
            {
                mission.Transition(end);
                mission.Conversation(end, ClarkPackage);
                mission.Action(end, 1, MissionActionKind.GrantReward, reward: 1);
            }

            var scene = new MissionSceneDefinition
            {
                Script = "data.sequence",
                MapContextId = PlateauVelonHollowBaneV1.MapContextId,   // accepted at Clark on the Plateau, played in Velon Hollow
                Actors = new()
                {
                    [TalaRole] = new SceneActorDefinition(TalaRole, SceneActorKind.Creature, Tala, TalaAt, Orientation: 0.55,
                        InitiallyInteractable: false),
                    [QuarshRole] = new SceneActorDefinition(QuarshRole, SceneActorKind.Creature, Quarsh, QuarshAt, Orientation: 3.7,
                        GameplayPolicy: PlateauSilentButDeadlyV1.Hostile())
                },
                DefeatSequences = new() { [QuarshRole] = QuarshDown },
                // 5, hidden, is done once Clark hears either end, as Looking For Clues' 11 (PlateauLookingForCluesV1).
                HiddenObjectiveIds = new() { Ended },
                ObjectiveAggregations = new() { [Ended] = new MissionObjectiveAggregation(new[] { Dead, Alive }, 1) }
            };
            scene.Sequences[0] = new SceneSequenceDefinition();
            scene.Sequences[Hostage] = new SceneSequenceDefinition
            {
                World =
                {
                    new EnsureActorIntent("tala-held", TalaRole),
                    new EnsureActorIntent("quarsh-holds", QuarshRole)
                },
                Timers = { new SequenceTimer(EscapeTimer, EscapeMs, Escape) }
            };
            scene.Sequences[QuarshDown] = new SceneSequenceDefinition
            {
                World = { new RemoveActorIntent("tala-dies", TalaRole) },
                Signals = { new SceneMissionSignal(MissionId, Outcome, Sacrificed) },
                Timers = { new SequenceTimer(EscapeTimer, 0, Escape, Cancel: true) }
            };
            scene.Sequences[Escape] = new SceneSequenceDefinition
            {
                World =
                {
                    new RemoveActorIntent("quarsh-escapes", QuarshRole),
                    new RemoveActorIntent("tala-goes-home", TalaRole)
                },
                Signals = { new SceneMissionSignal(MissionId, Outcome, Escaped) }
            };

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1975",
                "Native1 find Tala 20871/20872, 2 decision 20873/20874 'in the next few seconds', 3 Tala dead 20875/20876, 4 Tala " +
                "alive 20877/20878, 5 hidden 20879/20880, no counters; objectiveconversation 1975: 3/407 20881, 4/407 20882. Texts " +
                "20803-20807. Names 10194 Maxxum Quarsh, 10528 Tala Clark.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/The_Better_Half",
                "Giver/receiver Science Officer Clark; requirement Village Of The Damned; 90000XP, 6400 credits, Purifier Helmet " +
                "(Velon Hollow mission list).");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:PlateauBetterHalfV1",
                "Level32. Bound scene in the squad's Velon Hollow: 1 on entering the upper control room (364, 64.3, 126); Quarsh650080 " +
                "and Tala650081 appear, a 20s clock runs; Quarsh killed: Tala removed, 3; clock out: both removed, 4; hidden 5 by " +
                "either. Helmets 122258/122260/122262/122264 on both ends. Places, clock and Quarsh's strength are estimates. No " +
                "indicator (instance).", 0.2);

            mission.Enable(scene);
        }

        public static void Down(MigrationBuilder migration)
        {
            PlateauMissionDataV1.Remove(migration, MissionId);
            foreach (var sql in RemoveCreatures())
                migration.Sql(sql);
        }

        /// <summary>Reveals and activates <paramref name="next"/> by <paramref name="transition"/>, as Looking For Clues' Open.</summary>
        private static void Open(PlateauMissionDataV1 mission, uint objective, uint next, uint transition)
        {
            mission.Action(objective, 1, MissionActionKind.RevealObjective, targetObjective: next, transition: transition);
            mission.Action(objective, 2, MissionActionKind.ActivateObjective, targetObjective: next,
                state: MissionObjectiveState.Incomplete, transition: transition);
        }
    }
}
