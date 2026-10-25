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
    /// into Velon Hollow after his wife Tala. A Thrax Maxxum, Quarsh, holds her: the player strikes him and she
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
    /// before you did."). Names 10194 "Maxxum Quarsh", 10528 "Tala Clark". Barks (barklanguage): 848 "You try
    /// and stop me, and the girl dies. Your choice.", 405 "No, please! I don't want to die! Somebody help me!",
    /// 784 "Thank you so much for saving me."
    ///
    /// Sourced: TaRapedia The Better Half (Velon Hollow mission list): giver and receiver Science Officer Clark,
    /// requirement Village Of The Damned (1981), 90,000 XP, 6,400 credits, Purifier Helmet.
    ///
    /// Reconstructed (user decisions, playtest 2026-10-25): level 32, as the other Velon Hollow missions. A bound
    /// scene in the squad's copy (PlateauMountainSpringToxinsV1): when the player comes into Velon Hollow, Tala
    /// works at a terminal of the upper control room and Quarsh stands behind her, passive (neutral). Into the
    /// room: Quarsh threatens (848), Tala begs (405), and a clock of <see cref="EscapeMs"/> runs. The first hit on
    /// Quarsh decides, in the room or before it (then it opens 2 itself): he shoots Tala dead and fights
    /// to the end, with no clock; his death opens 3. No hit by the clock: Quarsh runs out into the hallway and is
    /// gone, Tala thanks the player (784), walks out of the room and is gone; 4. The same reward on both ends:
    /// the four Purifier Helmets.
    /// </summary>
    public static class PlateauBetterHalfV1
    {
        public const uint MissionId = 1975;
        public const uint Level = 32;
        public const uint Clark = PlateauPerduNpcsV1.Clark, ClarkPackage = PlateauPerduNpcsV1.ClarkPackage;
        public const uint Find = 1, Decide = 2, Dead = 3, Alive = 4, Ended = 5;
        public const uint Room = 1;

        /// <summary>
        /// Scenario and sequence 1: the standoff, the clock on. Scenario 2: its two ends, as steps. Sequence 0 runs
        /// when the scene starts, which a scene bound to a map does when the owner first comes onto it.
        /// </summary>
        public const uint Arrival = 0, Hostage = 1, Outcome = 2;
        /// <summary>Scenario 2's events: Sacrificed and Escaped end 2; Confronted (Quarsh struck or killed) ends 1 if the room did not.</summary>
        public const uint Sacrificed = 1, Escaped = 2, Confronted = 3;
        public const uint QuarshDown = 11, Escape = 12, Begs = 13, Struck = 14, QuarshGone = 15, TalaLeaves = 16, TalaGone = 17, Sacrifice = 18, TalaDies = 19;
        public const string EscapeTimer = "quarsh-escapes", BegsTimer = "tala-begs", QuarshGoneTimer = "quarsh-gone",
            TalaLeavesTimer = "tala-leaves", TalaGoneTimer = "tala-gone", SacrificeTimer = "quarsh-dead";
        /// <summary>
        /// Quarsh's death ends 2 a step after it opens 1's way out (Confronted): the signals of one step are all read
        /// against the objectives as they were before it, so Sacrificed in the same step as Confronted was lost when
        /// he died at one blow before the room.
        /// </summary>
        public const uint SacrificeMs = 100;
        public const uint EscapeMs = 20000;              // user, 2026-10-25 (30 s, then back to 20 s)
        public const uint BegsMs = 3000;                 // ESTIMATE: after Quarsh's bark
        public const uint QuarshGoneMs = 8000;           // ESTIMATE: his run out, about 42 m at 6.5 m/s
        public const uint TalaLeavesMs = 3000;           // ESTIMATE: after her thanks
        public const uint TalaGoneMs = 27000;            // ESTIMATE: her walk out, about 72 m at 3 m/s
        public const float TalaWalkSpeed = 3f;           // ESTIMATE, under her walk speed: shown walking
        public const uint TalaRowWalkSpeed = 4;          // ESTIMATE (creature.walk_speed is whole); run speed 0 keeps her from strolling (BehaviorManager)

        /// <summary>
        /// Quarsh is two roles of one creature: holding Tala from the player's arrival to the clock (the one a first
        /// hit and a death count on), and fleeing after it (immune, so a hit on his way out does not take back the
        /// end the clock gave). Playtest 2026-10-25: a third, waiting role swapped at the room looked like a spawn.
        /// </summary>
        public const string TalaRole = "tala", QuarshRole = "quarsh", QuarshFleesRole = "quarsh-flees";
        public const string QuarshRunsOut = "quarsh-runs-out", TalaWalksOut = "tala-walks-out";

        /// <summary>Client barks (barklanguage).</summary>
        public const uint QuarshThreatBark = 848, TalaBegsBark = 405, TalaThanksBark = 784;

        /// <summary>NpcPose (Rasa.Data, server): None, Standing (keeps its post and facing), AtConsole (female skeleton).</summary>
        public const uint NoPose = 0, StandingPose = 1, AtConsolePose = 8;

        public const uint Quarsh = 650080, Tala = 650081;
        public const uint QuarshName = 10194, TalaName = 10528;
        public const uint QuarshHitPoints = 3177, QuarshArmor = 150;   // a level 32 Thrax, one third of the officer's
        /// <summary>Neutral (TargetCategory 3): seeks nobody and is sought by nobody, may be attacked and fights back whoever does.</summary>
        public const uint QuarshFaction = 3;
        public const uint FemaleClass = 3848;            // NPC_Human_Swapset_Female
        public const uint TalaOutfitOf = 510030;         // Lieutenant Sherer - Outpost Intrepid
        public const uint TalaLevel = 30;
        /// <summary>
        /// ESTIMATE: under the least of Quarsh's blows (kick 76-100, weapon 147-220, creature_action 53068/53069), no armour, so
        /// one kills her. Set on her scene actor: a scene creature otherwise has at least 100 health and 100 armour.
        /// </summary>
        public const uint TalaHitPoints = 70;

        public const uint Experience = 90000;
        public const uint Credits = 6400;
        public const uint GravitonHelmet = 122258, StealthHelmet = 122260, MechHelmet = 122262, BioHelmet = 122264;

        /// <summary>The upper control room (ArchBaneIndustrialRoomControl24mV07).</summary>
        public static readonly (double X, double Y, double Z) RoomAt = (364, 64.3, 126);
        public const double RoomRadius = 12, RoomHeight = 8;
        // Playtest 2026-10-25, the user's .where: Tala at a terminal deeper in the room, Quarsh behind her; his exit
        // into the hallway, hers out of the room the player came in by. All four on the navmesh, with paths.
        // Second pass (same playtest): Tala nearer Quarsh, both facing from him towards her (the first 0.74 faced away).
        public static readonly ScenePosition TalaAt = new(353.07f, 66.55f, 113.99f);
        public const double TalaFacing = 3.87;
        public static readonly ScenePosition QuarshAt = new(349.98f, 66.58f, 110.32f);
        public const double QuarshFacing = 3.86;
        public static readonly ScenePosition QuarshExit = new(339.27f, 74.18f, 101.32f);
        public const double QuarshExitFacing = 5.43;
        public static readonly ScenePosition TalaExit = new(405.31f, 66.21f, 153.15f);
        public const double TalaExitFacing = 5.53;

        public static IEnumerable<string> Creatures()
        {
            yield return Creature(Quarsh, "Maxxum Quarsh - Velon Hollow", PlateauVelonHollowBaneV1.ThraxClass, Level, QuarshHitPoints,
                QuarshName, PlateauVelonHollowBaneV1.ThraxKick33, PlateauVelonHollowBaneV1.ThraxWeapon33);
            yield return $"UPDATE creature SET faction = {QuarshFaction} WHERE id = {Quarsh};";
            yield return Stat(Quarsh, QuarshHitPoints, QuarshArmor);
            yield return "INSERT INTO creature (id, comment, class_id, faction, level, max_hp, name_id, run_speed, walk_speed, " +
                         "action1, action2, action3, action4, action5, action6, action7, action8) " +
                         $"VALUES ({Tala}, 'Science Officer Tala Clark - Velon Hollow', {FemaleClass}, 1, {TalaLevel}, 1000, {TalaName}, 0, {TalaRowWalkSpeed}, 0, 0, 0, 0, 0, 0, 0, 0);";
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
            mission.Scenario(Outcome, "Quarsh killed or escaped", 3);
            mission.Area(Room, PlateauVelonHollowBaneV1.MapContextId, RoomAt.X, RoomAt.Y, RoomAt.Z, RoomRadius, RoomHeight);

            mission.Transition(Find);
            mission.EnterArea(Find, Room);
            PlateauSilentButDeadlyV1.Open(mission, Find, 1, Decide);
            mission.Action(Find, 3, MissionActionKind.StartScenario, scenario: Hostage);
            // Or Quarsh struck (or killed) before the room: 2 opens, with no standoff, so that his death still ends it.
            mission.Transition(Find, 2, 2);
            mission.Progress(Find, 2, MissionProgressEventKind.ScenarioEvent, Confronted, null, counter: Outcome);
            Open(mission, Find, Decide, 2);

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

            var immune = new ActorGameplayPolicy { Invulnerable = true };
            var scene = new MissionSceneDefinition
            {
                Script = "data.sequence",
                MapContextId = PlateauVelonHollowBaneV1.MapContextId,   // accepted at Clark on the Plateau, played in Velon Hollow
                Actors = new()
                {
                    // Not immune: Quarsh shoots her at the first hit. Friendly, so the player cannot hurt her (TargetCategories),
                    // and no kill of hers is anyone's (a creature's kill credits no player).
                    [TalaRole] = new SceneActorDefinition(TalaRole, SceneActorKind.Creature, Tala, TalaAt, Orientation: TalaFacing,
                        InitiallyInteractable: false, HitPoints: TalaHitPoints, Armor: 0),
                    [QuarshRole] = new SceneActorDefinition(QuarshRole, SceneActorKind.Creature, Quarsh, QuarshAt, Orientation: QuarshFacing,
                        GameplayPolicy: PlateauSilentButDeadlyV1.Hostile()),
                    [QuarshFleesRole] = new SceneActorDefinition(QuarshFleesRole, SceneActorKind.Creature, Quarsh, QuarshAt,
                        Orientation: QuarshFacing, GameplayPolicy: immune)
                },
                Routes = new()
                {
                    [QuarshRunsOut] = new SceneRoute(QuarshRunsOut, new[] { new SceneWaypoint(QuarshExit, QuarshExitFacing) }),
                    [TalaWalksOut] = new SceneRoute(TalaWalksOut, new[] { new SceneWaypoint(TalaExit, TalaExitFacing) }, TalaWalkSpeed)
                },
                DefeatSequences = new() { [QuarshRole] = QuarshDown, [TalaRole] = TalaDies },
                DamagedSequences = new() { [QuarshRole] = Struck },
                // 5, hidden, is done once Clark hears either end, as Looking For Clues' 11 (PlateauLookingForCluesV1).
                HiddenObjectiveIds = new() { Ended },
                ObjectiveAggregations = new() { [Ended] = new MissionObjectiveAggregation(new[] { Dead, Alive }, 1) }
            };
            // Into Velon Hollow: both in their places before the player can see the room.
            scene.Sequences[Arrival] = new SceneSequenceDefinition
            {
                World =
                {
                    new EnsureActorIntent("tala-works", TalaRole),
                    new PoseActorIntent("tala-at-terminal", TalaRole, AtConsolePose),
                    new EnsureActorIntent("quarsh-holds", QuarshRole),
                    new PoseActorIntent("quarsh-stands-behind", QuarshRole, StandingPose)
                }
            };
            // Into the room: the standoff.
            scene.Sequences[Hostage] = new SceneSequenceDefinition
            {
                World =
                {
                    new PoseActorIntent("tala-held", TalaRole, NoPose),
                    new BarkActorIntent("quarsh-threatens", QuarshRole, QuarshThreatBark)
                },
                Timers =
                {
                    new SequenceTimer(EscapeTimer, EscapeMs, Escape),
                    new SequenceTimer(BegsTimer, BegsMs, Begs)
                }
            };
            scene.Sequences[Begs] = new SceneSequenceDefinition
            {
                World = { new BarkActorIntent("tala-begs", TalaRole, TalaBegsBark) }
            };
            // The first hit: he shoots her (one blow kills her), then fights the player; no clock.
            scene.Sequences[Struck] = new SceneSequenceDefinition
            {
                World = { new AttackActorIntent("quarsh-shoots-tala", QuarshRole, TalaRole, HoldTarget: true) },
                Signals = { new SceneMissionSignal(MissionId, Outcome, Confronted) },
                Timers =
                {
                    new SequenceTimer(EscapeTimer, 0, Escape, Cancel: true),
                    new SequenceTimer(BegsTimer, 0, Begs, Cancel: true)
                }
            };
            scene.Sequences[TalaDies] = new SceneSequenceDefinition
            {
                World = { new AttackActorIntent("quarsh-fights", QuarshRole) }
            };
            // Her body is left where she fell.
            scene.Sequences[QuarshDown] = new SceneSequenceDefinition
            {
                // Killed before the room with no hit counted first (one blow): Confronted opens 2, Sacrifice ends it.
                Signals = { new SceneMissionSignal(MissionId, Outcome, Confronted) },
                Timers =
                {
                    new SequenceTimer(EscapeTimer, 0, Escape, Cancel: true),
                    new SequenceTimer(BegsTimer, 0, Begs, Cancel: true),
                    new SequenceTimer(SacrificeTimer, SacrificeMs, Sacrifice)
                }
            };
            scene.Sequences[Sacrifice] = new SceneSequenceDefinition
            {
                Signals = { new SceneMissionSignal(MissionId, Outcome, Sacrificed) }
            };
            // The clock out: he runs into the hallway; she thanks the player and walks out of the room.
            scene.Sequences[Escape] = new SceneSequenceDefinition
            {
                World =
                {
                    new RemoveActorIntent("quarsh-breaks-off", QuarshRole),
                    new EnsureActorIntent("quarsh-flees", QuarshFleesRole),
                    new RunRouteIntent("quarsh-runs-out", QuarshFleesRole, QuarshRunsOut)
                },
                Signals = { new SceneMissionSignal(MissionId, Outcome, Escaped) },
                Timers =
                {
                    new SequenceTimer(BegsTimer, 0, Begs, Cancel: true),
                    new SequenceTimer(QuarshGoneTimer, QuarshGoneMs, QuarshGone)
                }
            };
            scene.Sequences[QuarshGone] = new SceneSequenceDefinition
            {
                World =
                {
                    new RemoveActorIntent("quarsh-gone", QuarshFleesRole),
                    new BarkActorIntent("tala-thanks", TalaRole, TalaThanksBark)
                },
                Timers = { new SequenceTimer(TalaLeavesTimer, TalaLeavesMs, TalaLeaves) }
            };
            scene.Sequences[TalaLeaves] = new SceneSequenceDefinition
            {
                World = { new RunRouteIntent("tala-walks-out", TalaRole, TalaWalksOut) },
                Timers = { new SequenceTimer(TalaGoneTimer, TalaGoneMs, TalaGone) }
            };
            scene.Sequences[TalaGone] = new SceneSequenceDefinition
            {
                World = { new RemoveActorIntent("tala-goes-home", TalaRole) }
            };

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1975",
                "Native1 find Tala 20871/20872, 2 decision 20873/20874 'in the next few seconds', 3 Tala dead 20875/20876, 4 Tala " +
                "alive 20877/20878, 5 hidden 20879/20880, no counters; objectiveconversation 1975: 3/407 20881, 4/407 20882. Texts " +
                "20803-20807. Names 10194 Maxxum Quarsh, 10528 Tala Clark. Barks 848, 405, 784 (barklanguage).");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/The_Better_Half",
                "Giver/receiver Science Officer Clark; requirement Village Of The Damned; 90000XP, 6400 credits, Purifier Helmet " +
                "(Velon Hollow mission list).");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:PlateauBetterHalfV1",
                "Level32. Bound scene in the squad's Velon Hollow: on arrival Tala650081 works at a terminal (353.07, 66.55, 113.99) " +
                "and Quarsh650080, neutral, stands behind her (349.98, 66.58, 110.32); 1 on entering the upper control room (364, " +
                "64.3, 126): barks 848 and 405, a 20s clock. First hit on Quarsh (in the room or before it, then it opens 2): he shoots Tala (70 HP, one blow), then fights the player, no clock; his death 3. " +
                "Clock out: Quarsh runs to (339.27, 74.18, 101.32) and is removed, Tala barks 784, walks to (405.31, 66.21, " +
                "153.15) and is removed; 4. Hidden 5 by either. Helmets 122258/122260/122262/122264 on both ends. Places and " +
                "the clock from the user's playtest; bark timing, Quarsh's strength and neutrality are estimates. No indicator " +
                "(instance).", 0.2);

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
