using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Content.Marshes;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Marshes
{
    /// <summary>
    /// Trapped! (native mission 1052), Valverde Marshes: Retread Vincent sends the player to Retread Samuel's crew, cornered
    /// at the Bane Refueling Station; the player finds Samuel and holds off the Bane until the crew can slip away.
    ///
    /// Native: texts 5912-5915, 7013 (missionconversation); objectives 1 "Locate Samuel in the Bane Depot Exterior"
    /// 6082/7014, 2 "Defend the Retreads from the incoming Bane attack" 6083/7015 (missionobjective); objective conversation
    /// 1052,1 package 1120 6084 (Samuel: "We got turned around and lost").
    ///
    /// Sourced: TaRapedia Trapped!: giver Retread Vincent; level 38; 75,000 XP, 5,800 credits; AccuMax RPG Launcher 35,
    /// ChiTech Virulent Leech Gun 39, Teleract Cryogenic Net Gun 36, Vitalius Pulse Rifle 36; "a crew out in the Bane
    /// Refueling Station ... cornered"; "babysit him for about 4 minutes ... the mission will fail if Samuel dies" (a hidden
    /// timer). World DB: Retread Samuel 510167 at the Bane Refueling Station (196.4, 219.4, -669.1, pool 510167), the
    /// Marshes Thrax Soldier 531045 and Grenadier 531046. The rewards the client's item block 120097-120100
    /// (GrenadeLauncher Physical 35-39, LeechGun Virulent 39-43, NetGun Ice 36-40, Rifle EMP 36-40).
    ///
    /// Reconstructed (ESTIMATE):
    ///  - A personal Samuel stand-in (package 1120, MarshesRetreadCityNpcsV1) and 3 Retreads at the DB Samuel's spot (TaR's
    ///    "near (15, -535)" left: the triage); the public Samuel hidden from the player while the stand-in stands. Talking to
    ///    him (1) starts the defence (MarshesDefenceScene).
    ///  - 3 waves of the Marshes Thrax (3, 4, 5) 5 s, 75 s and 150 s after the talk, from the station's west, south-east and
    ///    south; the hold 240 s (TaR's "about 4 minutes"); then 2 completes and the crew goes. Samuel's death fails 1 before
    ///    the talk, 2 during the defence; the Retreads' deaths change nothing. TaR's "leave the vicinity" fail left out.
    ///  - The waves go for the Retreads first and for Samuel only when no Retread lives (the scene's spare, to-fix 29: with
    ///    the waves' members sent at the crew in turn, Samuel first, wave 3 and the station's Thrax killed him 10-17 s after
    ///    its spawn while one player fought them, in both real-client runs of 2026-11-03).
    /// </summary>
    public static class MarshesTrappedV1
    {
        public const uint MissionId = 1052;
        public const uint Level = 38;
        public const uint Vincent = MarshesStalkerWoodsNpcsV1.Vincent;
        public const uint Locate = 1, Defend = 2;
        public const uint DefenceStarts = 1, Freed = 2;          // scenarios: the start (its sequence), the outcome (its step 1)
        public const uint SamuelPackage = MarshesRetreadCityNpcsV1.SamuelPackage;
        public const uint PublicSamuelPool = MarshesRetreadCityNpcsV1.Samuel;
        public const uint Soldier = 531045, Grenadier = 531046;
        public const uint HoldMilliseconds = 240000;

        public static readonly ScenePosition SamuelAt = new(196.4f, 219.65f, -669.1f);
        public static readonly ScenePosition[] RetreadsAt = { new(190f, 219.68f, -665f), new(202f, 219.61f, -672f), new(192f, 219.57f, -674f) };

        /// <summary>The waves: when (ms after the talk) and who comes from where.</summary>
        public static readonly (uint After, (uint Creature, ScenePosition At)[] Members)[] Waves =
        {
            (5000, new[] { (Soldier, new ScenePosition(170f, 218.48f, -650f)), (Soldier, new ScenePosition(172f, 218.48f, -652f)),
                (Grenadier, new ScenePosition(168f, 218.48f, -648f)) }),
            (75000, new[] { (Soldier, new ScenePosition(225f, 219.40f, -690f)), (Soldier, new ScenePosition(227f, 219.40f, -688f)),
                (Grenadier, new ScenePosition(223f, 219.40f, -692f)), (Grenadier, new ScenePosition(225f, 219.40f, -693f)) }),
            (150000, new[] { (Soldier, new ScenePosition(170f, 218.97f, -690f)), (Soldier, new ScenePosition(172f, 218.97f, -692f)),
                (Grenadier, new ScenePosition(196f, 220.66f, -700f)), (Soldier, new ScenePosition(198f, 220.66f, -702f)),
                (Grenadier, new ScenePosition(194f, 220.66f, -702f)) })
        };

        public const uint Experience = 75000;
        public const uint Credits = 5800;
        public static readonly uint[] Rewards = { 120097, 120098, 120099, 120100 };

        public static void Up(MigrationBuilder migration) => Write(migration, SamuelAt, RetreadsAt, approach: false);

        /// <summary>
        /// The mission's rows; with <paramref name="approach"/> (MarshesTrappedV2, to-fix 10) the crew stands only once the
        /// player comes near.
        /// </summary>
        internal static void Write(MigrationBuilder migration, ScenePosition samuelAt, ScenePosition[] retreadsAt, bool approach)
        {
            var mission = new MarshesMissionDataV1(migration, MissionId, "Trapped!", 5912, Vincent, Vincent, Level);
            mission.Reward(Experience, Credits, Rewards);
            mission.Scenario(DefenceStarts, "Samuel found: the Bane attack", 0);
            mission.Scenario(Freed, "The Retreads held out", 1);

            mission.Objective(Locate, 6082, 7014, 0, MissionObjectiveState.Incomplete);
            mission.Objective(Defend, 6083, 7015, 1, MissionObjectiveState.Inactive);
            if (approach)
            {
                mission.Scenario(MarshesTrappedV2.Approach, "The player near the station: the crew stands", 0);
                mission.Area(MarshesTrappedV2.ApproachArea, MarshesMissionDataV1.MapContextId, samuelAt.X, samuelAt.Y, samuelAt.Z,
                    MarshesTrappedV2.ApproachRadius, 20);
                mission.Objective(MarshesTrappedV2.Near, 6082, 7014, 2, MissionObjectiveState.Incomplete, required: false);   // hidden, server only
                mission.Transition(MarshesTrappedV2.Near);
                mission.EnterArea(MarshesTrappedV2.Near, MarshesTrappedV2.ApproachArea);
                mission.Action(MarshesTrappedV2.Near, 1, MissionActionKind.StartScenario, scenario: MarshesTrappedV2.Approach);
            }

            mission.Transition(Locate);
            mission.Conversation(Locate, SamuelPackage);
            mission.Action(Locate, 1, MissionActionKind.RevealObjective, targetObjective: Defend);
            mission.Action(Locate, 2, MissionActionKind.ActivateObjective, targetObjective: Defend, state: MissionObjectiveState.Incomplete);
            mission.Action(Locate, 3, MissionActionKind.StartScenario, scenario: DefenceStarts);
            mission.Transition(Defend);
            mission.Progress(Defend, MissionProgressEventKind.ScenarioEvent, 1, null, counter: Freed);
            mission.Action(Defend, 1, MissionActionKind.GrantReward, reward: 1);

            mission.Indicator(Locate, 1, samuelAt.X, samuelAt.Y, samuelAt.Z);
            mission.Indicator(Defend, 2, samuelAt.X, samuelAt.Y, samuelAt.Z, 30);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1052",
                "Native1 'Locate Samuel in the Bane Depot Exterior' 6082/7014, 2 'Defend the Retreads from the incoming Bane " +
                "attack' 6083/7015; objectiveconversation 1052,1 pkg1120 6084. Texts 5912-5915, 7013. Item block 120097-120100.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Trapped!",
                "Giver Retread Vincent; level38; 75000XP, 5800 credits; crew cornered at the Bane Refueling Station; hidden " +
                "timer: babysit Samuel about 4 minutes; fails if Samuel dies.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:MarshesTrappedV1",
                "Personal Samuel stand-in 760342 (pkg1120) + 3 Retreads 760343 at the DB Samuel's spot; the public Samuel hidden. " +
                "The talk starts 3 waves (3/4/5 Thrax 531045/531046 at 5/75/150 s); after 240 s 2 completes. Samuel's death " +
                "fails 1 or 2. The leave-the-vicinity fail left out.", 0.3);

            var scene = new MissionSceneDefinition
            {
                Script = "marshes.defence",
                MapContextId = MarshesMissionDataV1.MapContextId,
                DefeatSequences = new(),
                Names = new()
                {
                    [MarshesDefenceScene.Mission] = MissionId,
                    [MarshesDefenceScene.Locate] = Locate,
                    [MarshesDefenceScene.Defend] = Defend,
                    [MarshesDefenceScene.Start] = DefenceStarts,
                    [MarshesDefenceScene.Waves] = (uint)Waves.Length,
                    [MarshesDefenceScene.Hold] = HoldMilliseconds,
                    [MarshesDefenceScene.Outcome] = Freed,
                    [MarshesDefenceScene.Step] = 1,
                    [MarshesDefenceScene.Hide] = PublicSamuelPool,
                    [MarshesDefenceScene.Spare] = 1
                }
            };
            scene.Sequences[0] = new SceneSequenceDefinition();
            scene.Sequences[DefenceStarts] = new SceneSequenceDefinition();
            scene.Sequences[MarshesDefenceScene.HoldTimerSequence] = new SceneSequenceDefinition();
            var defeat = 100U;
            void Add(string role, uint creature, ScenePosition at, bool interactable)
            {
                scene.Actors[role] = new SceneActorDefinition(role, SceneActorKind.Creature, creature, at, Orientation: 0,
                    InitiallyInteractable: interactable);
                scene.DefeatSequences[role] = ++defeat;
                scene.Sequences[defeat] = new SceneSequenceDefinition();
            }
            Add(MarshesDefenceScene.Leader, MarshesRetreadCityNpcsV1.SamuelStandIn, samuelAt, true);
            for (var member = 0; member < retreadsAt.Length; member++)
                Add(MarshesDefenceScene.Ward(member + 2), MarshesRetreadCityNpcsV1.Retread, retreadsAt[member], false);
            if (approach)
            {
                scene.Names[MarshesDefenceScene.Approach] = MarshesTrappedV2.Approach;
                scene.Sequences[MarshesTrappedV2.Approach] = new SceneSequenceDefinition();
                scene.HiddenObjectiveIds = new() { MarshesTrappedV2.Near };
            }
            for (var wave = 1; wave <= Waves.Length; wave++)
            {
                scene.Names[MarshesDefenceScene.After(wave)] = Waves[wave - 1].After;
                scene.Sequences[MarshesDefenceScene.WaveTimerSequence(wave)] = new SceneSequenceDefinition();
                for (var member = 1; member <= Waves[wave - 1].Members.Length; member++)
                {
                    var (creature, at) = Waves[wave - 1].Members[member - 1];
                    Add(MarshesDefenceScene.WaveRole(wave, member), creature, at, false);
                }
            }

            mission.Enable(scene);
        }

        public static void Down(MigrationBuilder migration) => MarshesMissionDataV1.Remove(migration, MissionId);
    }
}
