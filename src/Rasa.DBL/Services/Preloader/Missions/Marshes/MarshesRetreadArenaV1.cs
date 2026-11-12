using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Marshes
{
    /// <summary>
    /// The Retread Arena (native missions 1085 Beat Street, 1087 Smackdown, 1088 Legend or Fall), Valverde Marshes: Retread
    /// Karl in Retread City runs the fights; Retread Foster at the Thunderdome's door lets the player in; the player beats
    /// the round's fighters (1088: or takes a fall against the champion, Toro) and is paid by Karl.
    ///
    /// Native (missionconversation, missionobjective, objectiveconversation): 1085 texts 6152-6155, 7194; objectives 2
    /// "Speak to Retread Foster to enter the Arena" 6163/7195, 1 "Take out the Retread Lightweight Fighters in the Arena"
    /// 6156/7196 counters 6157 "Retread Withers Defeated:", 6158 "Retread Ingram Defeated:", 6169 (empty); oc 1085,2 package
    /// 1161 6164 ("let's get you in the arena then"). 1087 texts 6170-6173, 7271; 2 6174/7272, 1 "Take out the Retread
    /// Middleweight Fighters in the Arena" 6176/7273 counters 6179 "Retread King Defeated:", 6178 "Retread Baker Defeated:",
    /// 6177 (empty); oc 1087,2 package 1161 6175. 1088 texts 6188-6191, 7173; 2 6192/7174, 1 "Take out the Retread
    /// Heavyweight Fighter... or don't." 6198/7175 counters 6199 "Retread Toro Defeated:", 6201 "Retread Baker Defeated:",
    /// 6200 (empty), 3 the same title 7274/7275; oc 1088,2 package 1161: 6193, the choice 6194 ("If you take a fall, I'll
    /// make it worth your while"), 6195 "I'll take a fall, but it better be worth it.", 6196 "A winner never quits...".
    ///
    /// Sourced: TaRapedia: giver and receiver Retread Karl, 1085 then 1087 then 1088; 35,000 XP and 3,700 credits each; one
    /// of 1085 2 EMP Bomb VII, 2 Power Charger VII, 4 EMP Grenade VII, 4 Concussion Grenade VII; 1087 2 EMP Bomb, 2
    /// Adrenaline Booster, 4 Concussion, 4 Frag; 1088 2 EMP Bomb, 2 Armor Charger, 2 Cryo, 2 Incendiary; 3:00 to speak to
    /// Foster, 5:00 for the fight (history: "Complete the mission in 5 Minutes"). TR MMO DB: 1088 level 35. Ten Ton Hammer:
    /// "timed at 3:00 mins each", the fighters' levels (MarshesRetreadArenaNpcsV1).
    ///
    /// Reconstructed (ESTIMATE):
    ///  - 2 (3 minutes from the accept) is Foster's talk; it opens 1 and sends the player into the dome (a scene transfer:
    ///    the dome's floor is a navmesh island) with the round's fighters, the player's own, hostile and sent at the player.
    ///    The fight is the scene's 5-minute timer: run out, 1 fails (the mission with it) and the player is set down outside.
    ///    The fighters beaten, 1 completes (scene counter) and the player is set down outside at once.
    ///  - 1085's and 1087's 1: the two fighters a scene count (counter 0 and 1 the client's texts; the count shows on 0).
    ///  - 1088 (the triage's Q20): Foster's choice 1 (a fall) sets flag 1088001 = 1 and opens 1 and 3: Toro fights, and when
    ///    the 5 minutes run out with Toro alive, 1 and 3 complete (the fall served); Toro beaten first, 1 completes (3 stays
    ///    open, optional). Choice 2 (fight) sets 1088001 = 2 and opens 1 only: Toro beaten in 5 minutes, or 1 fails. The
    ///    client's counter 6201 ("Baker" on Toro's objective) is left out. The same reward either way.
    ///  - Levels 35; the stacks the lowest Class VII templates (Concussion the Sonic grenade, the chargers the 35-39 kits).
    ///    Markers at Foster (2) and round the arena (1, 3).
    /// </summary>
    public static class MarshesRetreadArenaV1
    {
        public const uint BeatStreet = 1085, Smackdown = 1087, LegendOrFall = 1088;
        public const uint Level = 35;
        public const uint Karl = MarshesRetreadArenaNpcsV1.Karl, FosterPackage = MarshesRetreadArenaNpcsV1.FosterPackage;
        public const uint Fight = 1, Enter = 2, Fall = 3;                   // objectives
        public const uint Defeated = 1, FallServed = 2;                     // scenarios with events
        public const uint EnterFight = 11, Won = 12, EnterFall = 13;        // scenarios that run scene sequences
        public const uint FightOver = 21, FallOver = 22;                    // timer sequences
        public const uint FallChoice = 1, FightChoice = 2;                  // 1088's choices (and transitions of 2)
        public const uint ChoiceFlag = 1088001;
        public const uint EnterSeconds = 180;
        public const uint FightMilliseconds = 300000;
        public const string FightTimer = "arena-fight";
        public const double FosterRadius = 8, ArenaRadius = 25;

        public const uint Experience = 35000;
        public const uint Credits = 3700;
        public const uint EmpBomb = 111232;             // Consumable_Bomb_EMP_07_35
        public const uint PowerCharger = 118907;        // Consumable_Restore_Power_Kit_35_to_39
        public const uint ArmorCharger = 118898;        // Consumable_Restore_Armor_Kit_35_to_39
        public const uint AdrenalineBooster = 118814;   // Consumable_Restore_Adrenaline_Kit_35_to_39
        public const uint EmpGrenade = 111028;          // Consumable_Grenade_EMP_07_35
        public const uint ConcussionGrenade = 111018;   // Consumable_Grenade_Sonic_07_35
        public const uint FragGrenade = 45446;          // Consumable_Grenade_Physical_07_35
        public const uint CryoGrenade = 111048;         // Consumable_Grenade_Ice_07_35
        public const uint IncendiaryGrenade = 111038;   // Consumable_Grenade_Fire_07_35

        /// <summary>A round: the mission, its texts, its fighters (with their stand), its counters, its stacks.</summary>
        public sealed record Round(uint MissionId, string Name, uint TextId, uint EnterName, uint EnterBody, uint FightName,
            uint FightBody, uint Counter0, uint? Counter1, (uint Creature, ScenePosition At)[] Fighters,
            (uint Template, uint Quantity)[] Stacks, uint? After);

        public static readonly Round[] Rounds =
        {
            new(BeatStreet, "Retread Arena: Beat Street", 6152, 6163, 7195, 6156, 7196, 6157, 6158,
                new[] { (MarshesRetreadArenaNpcsV1.Withers, new ScenePosition(-224.9f, 213.88f, -774.1f)),
                    (MarshesRetreadArenaNpcsV1.Ingram, new ScenePosition(-214.9f, 213.88f, -774.1f)) },
                new[] { (EmpBomb, 2U), (PowerCharger, 2U), (EmpGrenade, 4U), (ConcussionGrenade, 4U) }, null),
            new(Smackdown, "Retread Arena: Smackdown", 6170, 6174, 7272, 6176, 7273, 6179, 6178,
                new[] { (MarshesRetreadArenaNpcsV1.King, new ScenePosition(-224.9f, 213.88f, -774.1f)),
                    (MarshesRetreadArenaNpcsV1.Baker, new ScenePosition(-214.9f, 213.88f, -774.1f)) },
                new[] { (EmpBomb, 2U), (AdrenalineBooster, 2U), (ConcussionGrenade, 4U), (FragGrenade, 4U) }, BeatStreet),
            new(LegendOrFall, "Retread Arena: Legend or Fall", 6188, 6192, 7174, 6198, 7175, 6199, null,
                new[] { (MarshesRetreadArenaNpcsV1.Toro, new ScenePosition(-219.9f, 213.88f, -776.1f)) },
                new[] { (EmpBomb, 2U), (ArmorCharger, 2U), (CryoGrenade, 2U), (IncendiaryGrenade, 2U) }, Smackdown)
        };

        public static string FighterRole(int index) => $"fighter-{index + 1}";
        public static uint DefeatSequence(int index) => 101 + (uint)index;

        public static void Up(MigrationBuilder migration)
        {
            foreach (var round in Rounds)
                Build(migration, round);
        }

        private static void Build(MigrationBuilder migration, Round round)
        {
            var id = round.MissionId;
            var choice = id == LegendOrFall;
            var mission = new MarshesMissionDataV1(migration, id, round.Name, round.TextId, Karl, Karl, Level);
            mission.Reward(Experience, Credits, round.Stacks, new (uint, uint)[0]);
            if (round.After is { } after)
                mission.Prerequisite(after);
            mission.Scenario(Defeated, "An arena fighter beaten", (uint)round.Fighters.Length);
            mission.Scenario(EnterFight, "Into the arena", 0);
            mission.Scenario(Won, "The round won: out of the arena", 0);
            if (choice)
            {
                mission.Scenario(FallServed, "The fall taken: the 5 minutes out with Toro standing", 1);
                mission.Scenario(EnterFall, "Into the arena to take a fall", 0);
            }

            mission.Objective(Enter, round.EnterName, round.EnterBody, 0, MissionObjectiveState.Incomplete);
            mission.Objective(Fight, round.FightName, round.FightBody, 1, MissionObjectiveState.Inactive,
                counterTextId: round.Counter0, counter1TextId: round.Counter1);
            if (choice)
                mission.Objective(Fall, 7274, 7275, 2, MissionObjectiveState.Inactive, required: false);

            // 2: Foster's talk (1088: his choice), within 3 minutes of the accept.
            void Open(uint transition, uint first, uint scenario, uint? flagValue)
            {
                var action = first;
                if (flagValue is { } value)
                    mission.Action(Enter, action++, MissionActionKind.SetPlayerFlag, playerFlag: ChoiceFlag, playerFlagValue: value,
                        transition: transition);
                mission.Action(Enter, action++, MissionActionKind.RevealObjective, targetObjective: Fight, transition: transition);
                mission.Action(Enter, action++, MissionActionKind.ActivateObjective, targetObjective: Fight,
                    state: MissionObjectiveState.Incomplete, transition: transition);
                if (scenario == EnterFall)
                {
                    mission.Action(Enter, action++, MissionActionKind.RevealObjective, targetObjective: Fall, transition: transition);
                    mission.Action(Enter, action++, MissionActionKind.ActivateObjective, targetObjective: Fall,
                        state: MissionObjectiveState.Incomplete, transition: transition);
                }
                mission.Action(Enter, action, MissionActionKind.StartScenario, scenario: scenario, transition: transition);
            }
            if (choice)
            {
                mission.Transition(Enter, FallChoice, FallChoice);
                mission.Conversation(Enter, FosterPackage, 1, FallChoice);
                Open(FallChoice, 1, EnterFall, MarshesRetreadArenaV1.FallChoice);
                mission.Transition(Enter, FightChoice, FightChoice);
                mission.Conversation(Enter, FosterPackage, 1, FightChoice);
                Open(FightChoice, 11, EnterFight, MarshesRetreadArenaV1.FightChoice);
                mission.FailOnDeadline(Enter, EnterSeconds, transition: 3);
            }
            else
            {
                mission.Transition(Enter);
                mission.Conversation(Enter, FosterPackage);
                Open(1, 1, EnterFight, null);
                mission.FailOnDeadline(Enter, EnterSeconds, transition: 2);
            }

            // 1: the fighters beaten (a scene count); 1088 also by the fall served.
            mission.Transition(Fight);
            for (var fighter = 1U; fighter <= round.Fighters.Length; fighter++)
                mission.Progress(Fight, MissionProgressEventKind.ScenarioEvent, fighter, (uint)round.Fighters.Length, counter: Defeated,
                    trigger: fighter);
            mission.Action(Fight, 1, MissionActionKind.StartScenario, scenario: Won);
            mission.Action(Fight, 2, MissionActionKind.GrantReward, reward: 1);
            if (choice)
            {
                mission.Transition(Fight, 2, 2);
                mission.Progress(Fight, 2, MissionProgressEventKind.ScenarioEvent, 1, null, counter: FallServed);
                mission.Action(Fight, 3, MissionActionKind.GrantReward, reward: 1, transition: 2);
                mission.Transition(Fall);
                mission.Progress(Fall, MissionProgressEventKind.ScenarioEvent, 1, null, counter: FallServed);
            }

            var foster = MarshesRetreadArenaNpcsV1.FosterAt;
            var arena = MarshesRetreadArenaNpcsV1.ArenaCentre;
            mission.Indicator(Enter, 1, foster.X, foster.Y, foster.Z, FosterRadius);
            mission.Indicator(Fight, 2, arena.X, arena.Y, arena.Z, ArenaRadius);
            if (choice)
                mission.Indicator(Fall, 3, arena.X, arena.Y, arena.Z, ArenaRadius);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, $"native:missionobjective/{id}",
                $"Native2 'Speak to Retread Foster to enter the Arena' {round.EnterName}/{round.EnterBody}, 1 {round.FightName}/" +
                $"{round.FightBody} counters {round.Counter0}{(round.Counter1 is { } c1 ? $"/{c1}" : "")}" +
                (choice ? ", 3 7274/7275; oc 1088,2 pkg1161 6193, choice 6194: 6195 fall, 6196 fight" : $"; oc {id},2 pkg1161") +
                $". Texts {round.TextId}-{round.TextId + 3}. Names 8636 Karl, 8918 Foster.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Retread_Arena",
                "Giver/receiver Retread Karl; 1085 -> 1087 -> 1088; 35000XP, 3700 credits; four stacks to choose; 3:00 to speak " +
                "to Foster, 5:00 for the fight.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:MarshesRetreadArenaV1",
                "Foster's talk opens 1 and transfers the player onto the dome's floor (a navmesh island) with the round's own " +
                "hostile fighters; the scene's 5-minute timer fails 1 (or, after 1088's fall choice, completes 1 and 3); " +
                "beaten, the player is set down outside. 2 fails 3 minutes after the accept. Flag1088001 1 fall, 2 fight. " +
                "Level35; lowest Class VII stacks.", 0.3);

            var scene = new MissionSceneDefinition
            {
                Script = "data.sequence",
                MapContextId = MarshesMissionDataV1.MapContextId,
                DefeatSequences = new(),
                Dialogue = choice
                    ? new List<MissionDialogueTopicDefinition>
                    {
                        new(Enter, FosterPackage, 1, MissionDialogueKind.Choice,
                            choices: new Dictionary<int, uint> { [1] = FallChoice, [2] = FightChoice })
                    }
                    : null
            };
            scene.Sequences[0] = new SceneSequenceDefinition();
            List<WorldIntent> In(string prefix)
            {
                var world = new List<WorldIntent>
                {
                    new TransferIntent($"{prefix}-in", MarshesMissionDataV1.MapContextId, MarshesRetreadArenaNpcsV1.ArenaArrival,
                        MarshesRetreadArenaNpcsV1.ArenaArrivalYaw)
                };
                for (var index = 0; index < round.Fighters.Length; index++)
                {
                    world.Add(new EnsureActorIntent($"{prefix}-{FighterRole(index)}-comes", FighterRole(index)));
                    world.Add(QuestSceneIntents.Attack($"{prefix}-{FighterRole(index)}-fights", FighterRole(index)));
                }
                return world;
            }
            List<WorldIntent> Out(string prefix)
            {
                var world = new List<WorldIntent>();
                for (var index = 0; index < round.Fighters.Length; index++)
                    world.Add(new RemoveActorIntent($"{prefix}-{FighterRole(index)}-goes", FighterRole(index)));
                world.Add(new TransferIntent($"{prefix}-out", MarshesMissionDataV1.MapContextId, MarshesRetreadArenaNpcsV1.ArenaExit,
                    MarshesRetreadArenaNpcsV1.ArenaExitYaw));
                return world;
            }
            scene.Sequences[EnterFight] = new SceneSequenceDefinition
            {
                World = In("fight"),
                Timers = { new SequenceTimer(FightTimer, FightMilliseconds, FightOver) }
            };
            scene.Sequences[FightOver] = new SceneSequenceDefinition
            {
                World = Out("lost"),
                Character = { new ObjectiveIntent("time-up", id, Fight, MissionObjectiveState.Failed) }
            };
            scene.Sequences[Won] = new SceneSequenceDefinition
            {
                World = Out("won"),
                Timers = { new SequenceTimer(FightTimer, 0, choice ? FallOver : FightOver, Cancel: true) }
            };
            if (choice)
            {
                scene.Sequences[EnterFall] = new SceneSequenceDefinition
                {
                    World = In("fall"),
                    Timers = { new SequenceTimer(FightTimer, FightMilliseconds, FallOver) }
                };
                scene.Sequences[FallOver] = new SceneSequenceDefinition
                {
                    World = Out("fallen"),
                    Signals = { new SceneMissionSignal(id, FallServed, 1) }
                };
            }
            for (var index = 0; index < round.Fighters.Length; index++)
            {
                var role = FighterRole(index);
                scene.Actors[role] = new SceneActorDefinition(role, SceneActorKind.Creature, round.Fighters[index].Creature,
                    round.Fighters[index].At, Orientation: 0, InitiallyInteractable: false);
                scene.DefeatSequences[role] = DefeatSequence(index);
                scene.Sequences[DefeatSequence(index)] = new SceneSequenceDefinition
                {
                    Signals = { new SceneMissionSignal(id, Defeated, (uint)index + 1) }
                };
            }

            mission.Enable(scene);
        }

        public static void Down(MigrationBuilder migration)
        {
            for (var index = Rounds.Length - 1; index >= 0; index--)
                MarshesMissionDataV1.Remove(migration, Rounds[index].MissionId);
        }
    }
}
