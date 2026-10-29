using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Plateau
{
    /// <summary>
    /// Looking For Clues (native mission 928), Plateau: Detective Crais's murder investigation. Private Remick
    /// lost the murder weapon in a floating craps game; Luminary Sampei names the Forean suspect, Pendrik; at
    /// the craps game Pendrik confesses, and the player lets him go with a warning or tries to arrest him,
    /// and he fights to the death. Crais hears the end either way.
    ///
    /// Native: name/log/offer/turn-in/reminder texts 4974-4977 and 6383 (missionconversation); objectives 1
    /// "Question Private Remick." 5070/6386, 4 "Question Private Remick." 5075/6387, 6 "Tell Crais about the
    /// suspect." 5081/6384, 5 "Question Luminary Sampei." 5083/6385, 2 "Search for Pendrik." 5071/6388, 8 "Kill
    /// Pendrick!" 5090/6389, 9 "Return to Detective Crais." 5091/6390, 10 "Return to Detective Crais."
    /// 5092/6391, 11 no text (5100/6392), no counters (missionobjective). The talks (objectiveconversation): 1 and 4
    /// Remick 1015 (5079, 5080: the craps game "in one of the cement bunkers at Fort Defiance. More than likely,
    /// the one closest to New Velon"), 6 Crais 963 (5082), 5 Sampei 994 (5084), 2 Pendrik 1017 (5085, then the
    /// choices 5086 "Let Pendrik off with a warning." and 5087 "Attempt to arrest Pendrik."), 9 Crais 963
    /// (5098, Pendrik dead), 10 Crais 963 (5099, Pendrik let go).
    ///
    /// Sourced: TaRapedia Looking For Clues: giver and receiver CID Detective Crais, requirement Murder Most
    /// Foul, 40,000 XP, four choices (names not given); the two options at Pendrik, "When you choose second
    /// option, you will need to kill him. He is very easy". TR MMO DB quest list: level 30.
    ///
    /// Reconstructed (ESTIMATE): level 30; the objectives one after another 1, 4, 6, 5, then 2; the warning ends
    /// at 10, the arrest goes through 8 to 9 (8, 9, 10 not required); 11, hidden, is done by 9 or 10. Pendrik and two Craps Players
    /// are the player's own (user decision), from Sampei's talk; the warning sends all three away, the arrest
    /// puts a hostile copy in Pendrik's place (PlateauCidNpcsV1) that attacks, and the players go. Credits
    /// 4650 as Murder Most Foul; the four choices the Rare set after Murder Most Foul's, 116451-116454.
    /// </summary>
    public static class PlateauLookingForCluesV1
    {
        public const uint MissionId = 928;
        public const uint Level = 30;
        public const uint Remick = 1, Search = 2, Weapon = 4, Sampei = 5, Tell = 6, Kill = 8, Killed = 9, LetGo = 10, Ended = 11;
        public const uint Warning = 1, Arrest = 2;
        public const uint Gathering = 1, Leaving = 2, Resisting = 3, PendrikDead = 4;
        public const uint CraisNpc = PlateauCidNpcsV1.Crais;
        public const uint RemickNpc = PlateauWedgeRockNpcsV1.Remick, RemickPackage = 1015;
        public const uint SampeiNpc = 510186, SampeiPackage = 994;

        public const uint Experience = 40000;
        public const uint Credits = 4650;
        public const uint StealthHelmet = 116451, HazmatBoots = 116452, ReflectiveVest = 116453, MotorAssistBoots = 116454;

        public static readonly ScenePosition PendrikAt = new(20, 399.37f, 920);
        public static readonly ScenePosition[] PlayersAt = { new(24, 399.37f, 923), new(17, 399.37f, 917) };

        public static void Up(MigrationBuilder migration)
        {
            var mission = new PlateauMissionDataV1(migration, MissionId, "Looking For Clues", 4974, CraisNpc, CraisNpc, Level);
            mission.Reward(Experience, Credits, new[] { StealthHelmet, HazmatBoots, ReflectiveVest, MotorAssistBoots });
            mission.Prerequisite(PlateauMurderMostFoulV1.MissionId);
            mission.Scenario(Gathering, "The craps game gathers", 0);
            mission.Scenario(Leaving, "Pendrik and the players go", 0);
            mission.Scenario(Resisting, "Pendrik resists arrest", 0);
            mission.Scenario(PendrikDead, "Pendrik is dead", 1);

            var remick = PlateauWedgeRockNpcsV1.Moves[3].At;
            var crais = PlateauCidNpcsV1.Moves[1].At;
            (double X, double Y, double Z) sampei = (334.8, 365.0, 643.2);
            var pendrik = (PendrikAt.X, PendrikAt.Y, PendrikAt.Z);

            mission.Objective(Remick, 5070, 6386, 0, MissionObjectiveState.Incomplete);
            mission.Objective(Weapon, 5075, 6387, 1, MissionObjectiveState.Inactive);
            mission.Objective(Tell, 5081, 6384, 2, MissionObjectiveState.Inactive);
            mission.Objective(Sampei, 5083, 6385, 3, MissionObjectiveState.Inactive);
            mission.Objective(Search, 5071, 6388, 4, MissionObjectiveState.Inactive);
            mission.Objective(Kill, 5090, 6389, 5, MissionObjectiveState.Inactive, required: false);
            mission.Objective(Killed, 5091, 6390, 6, MissionObjectiveState.Inactive, required: false);
            mission.Objective(LetGo, 5092, 6391, 7, MissionObjectiveState.Inactive, required: false);
            mission.Objective(Ended, 5100, 6392, 8, MissionObjectiveState.Incomplete);

            Talk(mission, Remick, RemickPackage, Weapon);
            Talk(mission, Weapon, RemickPackage, Tell);
            Talk(mission, Tell, PlateauCidNpcsV1.CraisPackage, Sampei);
            Talk(mission, Sampei, SampeiPackage, Search);
            mission.Action(Sampei, 3, MissionActionKind.StartScenario, scenario: Gathering);

            // 2: Pendrik's two choices.
            mission.Transition(Search);
            mission.Conversation(Search, PlateauCidNpcsV1.PendrikPackage);
            Open(mission, Search, LetGo, Warning);
            mission.Action(Search, 3, MissionActionKind.StartScenario, scenario: Leaving);
            mission.Transition(Search, Arrest, Arrest);
            mission.Conversation(Search, PlateauCidNpcsV1.PendrikPackage, 1, Arrest);
            Open(mission, Search, Kill, Arrest);
            mission.Action(Search, 3, MissionActionKind.StartScenario, scenario: Resisting, transition: Arrest);

            mission.Transition(Kill);
            mission.Progress(Kill, MissionProgressEventKind.ScenarioEvent, 1, null, counter: PendrikDead);
            Open(mission, Kill, Killed, 1);

            foreach (var end in new[] { Killed, LetGo })
            {
                mission.Transition(end);
                mission.Conversation(end, PlateauCidNpcsV1.CraisPackage);
                mission.Action(end, 1, MissionActionKind.GrantReward, reward: 1);
            }

            mission.Indicator(Remick, 1, remick.X, remick.Y, remick.Z);
            mission.Indicator(Weapon, 2, remick.X, remick.Y, remick.Z);
            mission.Indicator(Tell, 3, crais.X, crais.Y, crais.Z);
            mission.Indicator(Sampei, 4, sampei.X, sampei.Y, sampei.Z);
            mission.Indicator(Search, 5, pendrik.X, pendrik.Y, pendrik.Z, 15);
            mission.Indicator(Kill, 6, pendrik.X, pendrik.Y, pendrik.Z, 15);
            mission.Indicator(Killed, 7, crais.X, crais.Y, crais.Z);
            mission.Indicator(LetGo, 8, crais.X, crais.Y, crais.Z);

            var scene = new MissionSceneDefinition
            {
                Script = "data.sequence",
                Actors = new()
                {
                    ["pendrik"] = new SceneActorDefinition("pendrik", SceneActorKind.Creature, PlateauCidNpcsV1.Pendrik, PendrikAt),
                    ["player-1"] = new SceneActorDefinition("player-1", SceneActorKind.Creature, PlateauCidNpcsV1.CrapsPlayer, PlayersAt[0],
                        Orientation: 3.14),
                    ["player-2"] = new SceneActorDefinition("player-2", SceneActorKind.Creature, PlateauCidNpcsV1.CrapsPlayer, PlayersAt[1]),
                    ["resisting"] = new SceneActorDefinition("resisting", SceneActorKind.Creature, PlateauCidNpcsV1.HostilePendrik, PendrikAt,
                        GameplayPolicy: new ActorGameplayPolicy
                        {
                            RewardScenarioKills = true, TrackParticipation = true,
                            // The run's own creature on a public map: no ordinary loot table can bind it, so none.
                            Loot = new AuthoredLootProfile(System.Array.Empty<LootDrop>())
                        })
                },
                Dialogue = new List<MissionDialogueTopicDefinition>
                {
                    new(Search, PlateauCidNpcsV1.PendrikPackage, 1, MissionDialogueKind.Choice,
                        choices: new Dictionary<int, uint> { [1] = Warning, [2] = Arrest })
                },
                DefeatSequences = new() { ["resisting"] = PendrikDead },
                // 11, with no text, is done once Crais hears either end, as Conscientious Objector's 12 (WildernessAliaBranchesV1).
                HiddenObjectiveIds = new() { Ended },
                ObjectiveAggregations = new() { [Ended] = new MissionObjectiveAggregation(new[] { Killed, LetGo }, 1) }
            };
            scene.Sequences[0] = new SceneSequenceDefinition();
            scene.Sequences[Gathering] = new SceneSequenceDefinition
            {
                World =
                {
                    new EnsureActorIntent("pendrik-plays", "pendrik"),
                    new EnsureActorIntent("player-1-plays", "player-1"),
                    new EnsureActorIntent("player-2-plays", "player-2")
                }
            };
            scene.Sequences[Leaving] = new SceneSequenceDefinition
            {
                World =
                {
                    new RemoveActorIntent("pendrik-goes", "pendrik"),
                    new RemoveActorIntent("player-1-goes", "player-1"),
                    new RemoveActorIntent("player-2-goes", "player-2")
                }
            };
            scene.Sequences[Resisting] = new SceneSequenceDefinition
            {
                World =
                {
                    new RemoveActorIntent("pendrik-draws", "pendrik"),
                    new RemoveActorIntent("player-1-runs", "player-1"),
                    new RemoveActorIntent("player-2-runs", "player-2"),
                    new EnsureActorIntent("pendrik-resists", "resisting"),
                    QuestSceneIntents.Attack("pendrik-attacks", "resisting")
                }
            };
            scene.Sequences[PendrikDead] = new SceneSequenceDefinition { Signals = { new SceneMissionSignal(MissionId, PendrikDead, 1) } };

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/928",
                "Native 1/4 Remick 5070/6386, 5075/6387; 6 Crais 5081/6384; 5 Sampei 5083/6385; 2 Pendrik 5071/6388; 8 Kill " +
                "5090/6389; 9/10 Crais 5091/6390, 5092/6391; 11 no text 5100/6392; no counters. objectiveconversation 928: 1,4/1015 5079 " +
                "5080; 6/963 5082; 5/994 5084; 2/1017 5085, choices 5086 warning, 5087 arrest; 9/963 5098; 10/963 5099. " +
                "Texts 4974-4977, 6383. Pendrik name8633, Craps Player 8634.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Looking_For_Clues",
                "Giver/receiver CID Detective Crais; requirement Murder Most Foul; 40000XP; four choices, names not given; " +
                "warning or arrest, the arrest means killing him, 'very easy'. TR MMO DB level30.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:PlateauLookingForCluesV1",
                "Level30; order 1,4,6,5,2; warning ends at 10, arrest 8 then 9, hidden 11 done by either. Personal Pendrik and two players in the fort's " +
                "east room (20, 399.37, 920) from Sampei's talk; the arrest swaps in hostile650046. Credits 4650 and the Rare " +
                "set 116451-116454 estimated.", 0.2);

            mission.Enable(scene);
        }

        public static void Down(MigrationBuilder migration) => PlateauMissionDataV1.Remove(migration, MissionId);

        /// <summary>A talk that completes <paramref name="objective"/> and opens <paramref name="next"/>.</summary>
        private static void Talk(PlateauMissionDataV1 mission, uint objective, uint package, uint next)
        {
            mission.Transition(objective);
            mission.Conversation(objective, package);
            Open(mission, objective, next, 1);
        }

        private static void Open(PlateauMissionDataV1 mission, uint objective, uint next, uint transition)
        {
            mission.Action(objective, 1, MissionActionKind.RevealObjective, targetObjective: next, transition: transition);
            mission.Action(objective, 2, MissionActionKind.ActivateObjective, targetObjective: next,
                state: MissionObjectiveState.Incomplete, transition: transition);
        }
    }
}
