using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Content.Marshes;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Ashen
{
    /// <summary>
    /// King of the Hill (native mission 1278), Ligo Ashen Desert: the Bane answer the radar work (1277) with an assault on
    /// Stonewall Ridge; Captain Marcus Emmons sends the player up to the four Anti-Air Batteries to hold them until the assault
    /// is beaten off.
    ///
    /// Native: texts 10286-10290 (missionconversation); objectives (missionobjective) 1 "Hurry to the top of Stonewall Ridge"
    /// 10291/10292, 2 "Defend Stonewall Ridge" 10293/10294 counter 10295 "Towers Lost", 3 "Wave 1" 10296/10297, 4-6 "Spawn 2".."4"
    /// 10298/10299 .. 10302/10303 (helpers of the client's waves); no objective conversation. The client's body of 2: all four
    /// batteries destroyed, or the player leaving Stonewall Ridge, fails it. World DB: the AFS Turret 550001 (class 4064, level
    /// 44) at the four turret platforms, pools 550001-550004.
    ///
    /// Sourced: TaRapedia King of the Hill: giver and reward giver Captain Marcus Emmons; requires On The Same Frequency (1277);
    /// 190,000 XP, 8,400 credits; Bio Helmet, Hazmat Gloves, Reflective Gloves, Motor Assist Boots (version numbers V03, V02,
    /// V05, V09); "Time remaining: 00:03:00" under the first objective; "a few stalkers and some Bane".
    ///
    /// Reconstructed (ESTIMATE):
    ///  - Level 42 (triage D5: the zone band).
    ///  - 1 completes when the player comes to the batteries (an area over the four platforms) within TaRapedia's three
    ///    minutes, else it fails (its deadline, the client's countdown). Its completion opens 2 and starts the defence
    ///    (MarshesDefenceScene, the Trapped! pattern).
    ///  - The wards: four personal copies of the AFS Turret (550001) at the public turrets' spots, each hiding its public
    ///    turret from the player while it stands (hide, hide.2-4); ward 1 the turret of pool 550001. They stand from the
    ///    mission's start. Each loss, before the defence and during it, counts on a hidden objective 7 (server only, as
    ///    1263's tally; the scene's lost scenario); the fourth fails the mission (1 before the top, 2 after it: the client's
    ///    body fails it only with all four destroyed). A counter rule can only complete
    ///    an objective, so the count is not on 2: the client's "Towers Lost" counter (10295) is not written (deviation).
    ///  - Four waves (the helpers 3-6, not written): 0 s three Thrax Soldiers (531008) between batteries 2 and 3; 45 s two Desert
    ///    Stalkers (790121, TaRapedia's stalkers) and a Thrax Ravager (531009) by battery 1; 90 s two Soldiers and two
    ///    Ravagers below battery 3; 135 s two Stalkers and two Soldiers below battery 4. Each wave's members attack the
    ///    turrets in turn; a turret's loss sends them at the ones left. The hold 180 s from the start; then 2 completes and
    ///    the turrets and what is left of the waves go (the public turrets come back).
    ///  - Not built: the fail on leaving Stonewall Ridge (no area-leave event: triage M2, D17).
    ///  - Markers: 1 and 2 over the four batteries (on the ground by batteries 2 and 3, r70).
    ///  - The items: the lowest Uncommon template of each kind in the band of level 42 with TaRapedia's version numbers (client
    ///    entityclass names): Bio V03 Helmet 19673 (40-44), Hazmat V02 Gloves 20504 (39-43), Reflective V05 RAR Gloves 37985
    ///    (39-43), Motor Assist V09 RAR Boots 45843 (38-42); one to choose.
    /// </summary>
    public static class AshenKingOfTheHillV1
    {
        public const uint MissionId = 1278;
        public const uint Level = 42;
        public const uint Emmons = AshenLeadInNpcsV1.Emmons;
        public const uint Hurry = 1, Defend = 2, LostTally = 7;      // 7: server only, hidden (the client has 1-6)
        public const uint DefenceStarts = 1, Held = 2, TowersLost = 3;     // scenarios: the start, the outcome (step 1), the losses (step M)
        public const uint HurrySeconds = 180;
        public const uint HoldMilliseconds = 180000;
        public const uint BatteriesArea = 1;

        public const uint Turret = 550001;              // AFS Turret - Ashen Desert (class 4064)
        public const uint Soldier = 531008, Ravager = 531009, Stalker = AshenBoneyardNpcsV1.Stalker;

        /// <summary>The four batteries: the public pool and its turret's place and facing (World DB spawnpool rows).</summary>
        public static readonly (uint Pool, ScenePosition At, double Facing)[] Batteries =
        {
            (550001, new(64.65f, 350.02f, -486.31f), -0.7854),
            (550002, new(83.19f, 348.32f, -516.37f), 0),
            (550003, new(109.70f, 347.95f, -543.36f), 0),
            (550004, new(136.37f, 348.03f, -570.53f), -1.5708)
        };

        /// <summary>The area of 1: the middle of the four platforms, a radius that takes them in.</summary>
        public static readonly (double X, double Y, double Z) RidgeAt = (100.5, 344.0, -528.4);
        public const double RidgeRadius = 60, RidgeHalfHeight = 20;

        /// <summary>
        /// The markers of 1 and 2: the area's middle has no client floor under it (a gap between the platforms), so they stand
        /// on the ground by batteries 2 and 3 (the navmesh and the client's floor, the probe of 2026-11-10), with a radius that
        /// takes in the four batteries.
        /// </summary>
        public static readonly (double X, double Y, double Z) MarkerAt = (108, 340.05, -510);
        public const double MarkerRadius = 70;

        /// <summary>The waves: when (ms after the start) and who comes from where (the navmesh ground).</summary>
        public static readonly (uint After, (uint Creature, ScenePosition At)[] Members)[] Waves =
        {
            (0, new[] { (Soldier, new ScenePosition(106f, 340.05f, -510f)), (Soldier, new ScenePosition(108f, 340.95f, -512f)),
                (Soldier, new ScenePosition(110f, 339.36f, -510f)) }),
            (45000, new[] { (Stalker, new ScenePosition(40f, 345.92f, -490f)), (Stalker, new ScenePosition(42f, 346.22f, -492f)),
                (Ravager, new ScenePosition(38f, 346.03f, -488f)) }),
            (90000, new[] { (Soldier, new ScenePosition(128f, 341.65f, -540f)), (Soldier, new ScenePosition(130f, 341.27f, -542f)),
                (Ravager, new ScenePosition(132f, 340.22f, -540f)), (Ravager, new ScenePosition(130f, 340.49f, -538f)) }),
            (135000, new[] { (Stalker, new ScenePosition(147f, 336.46f, -559f)), (Stalker, new ScenePosition(149f, 334.70f, -557f)),
                (Soldier, new ScenePosition(145f, 338.48f, -561f)), (Soldier, new ScenePosition(149f, 335.48f, -561f)) })
        };

        public const uint Experience = 190000;
        public const uint Credits = 8400;
        public const uint BioHelmet = 19673;            // Armor_Avatar_Bio_V03_Helmet_UNC_40_to_44
        public const uint HazmatGloves = 20504;         // Armor_Avatar_Hazmat_V02_Gloves_UNC_39_to_43
        public const uint ReflectiveGloves = 37985;     // Armor_Avatar_Reflective_V05_RAR_Gloves_UNC_39_to_43
        public const uint MotorAssistBoots = 45843;     // Armor_Avatar_MotorAssist_V09_RAR_Boots_UNC_38_to_42

        public static void Up(MigrationBuilder migration)
        {
            var mission = new AshenMissionDataV1(migration, MissionId, "King of the Hill", 10286, Emmons, Emmons, Level);
            mission.Reward(Experience, Credits, new[] { BioHelmet, HazmatGloves, ReflectiveGloves, MotorAssistBoots });
            mission.Prerequisite(AshenOnTheSameFrequencyV1.MissionId);
            mission.Scenario(DefenceStarts, "On top of Stonewall Ridge: the Bane assault", 0);
            mission.Scenario(Held, "Stonewall Ridge held", 1);
            mission.Scenario(TowersLost, "An Anti-Air Battery lost", (uint)Batteries.Length);

            mission.Objective(Hurry, 10291, 10292, 0, MissionObjectiveState.Incomplete);
            mission.Objective(Defend, 10293, 10294, 1, MissionObjectiveState.Inactive);
            mission.Objective(LostTally, 10293, 10294, 2, MissionObjectiveState.Incomplete, required: false);    // hidden

            mission.Area(BatteriesArea, AshenMissionDataV1.MapContextId, RidgeAt.X, RidgeAt.Y, RidgeAt.Z, RidgeRadius, RidgeHalfHeight);
            mission.Transition(Hurry);
            mission.EnterArea(Hurry, BatteriesArea);
            mission.Action(Hurry, 1, MissionActionKind.RevealObjective, targetObjective: Defend);
            mission.Action(Hurry, 2, MissionActionKind.ActivateObjective, targetObjective: Defend, state: MissionObjectiveState.Incomplete);
            mission.Action(Hurry, 3, MissionActionKind.StartScenario, scenario: DefenceStarts);
            mission.FailOnDeadline(Hurry, HurrySeconds, transition: 2);

            mission.Transition(Defend);
            mission.Progress(Defend, MissionProgressEventKind.ScenarioEvent, 1, null, counter: Held);
            mission.Action(Defend, 1, MissionActionKind.GrantReward, reward: 1);
            // The losses: hidden 7 counts the four batteries' (distinct, scene results 1-4); its completion, the fourth, fails 2.
            // A counter rule can only complete its objective (MissionRuntime.Evaluate), so the count is not on 2: the client's
            // "Towers Lost" counter (10295) is not shown.
            mission.Transition(LostTally);
            for (var battery = 1U; battery <= Batteries.Length; battery++)
                mission.Progress(LostTally, MissionProgressEventKind.ScenarioEvent, battery, (uint)Batteries.Length,
                    counter: TowersLost, trigger: battery);
            mission.Transition(Defend, 2, 2, MissionObjectiveState.Failed);
            mission.ObjectiveState(Defend, 2, LostTally, MissionObjectiveState.Completed);
            // The four lost before the top (the client's body: the mission fails only with all four destroyed) fail 1.
            mission.Transition(Hurry, 3, 3, MissionObjectiveState.Failed);
            mission.ObjectiveState(Hurry, 3, LostTally, MissionObjectiveState.Completed);

            mission.Indicator(Hurry, 1, MarkerAt.X, MarkerAt.Y, MarkerAt.Z, MarkerRadius);
            mission.Indicator(Defend, 2, MarkerAt.X, MarkerAt.Y, MarkerAt.Z, MarkerRadius);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1278",
                "Native1 'Hurry to the top of Stonewall Ridge' 10291/10292, 2 'Defend Stonewall Ridge' 10293/10294 counter 10295 " +
                "'Towers Lost', 3 'Wave 1', 4-6 'Spawn 2'..'4' 10296-10303 (helpers); no objectiveconversation. Texts 10286-10290. " +
                "Body of 2: fails when all four batteries are destroyed or the player leaves Stonewall Ridge. AFS Turret 550001 " +
                "(class4064, L44) pools 550001-550004.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/King_of_the_Hill",
                "Giver/reward giver Captain Marcus Emmons; requires On The Same Frequency; 190000XP, 8400 credits; Bio Helmet V03, " +
                "Hazmat Gloves V02, Reflective Gloves V05, Motor Assist Boots V09; 'Time remaining: 00:03:00' under 1; a few " +
                "stalkers and some Bane.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:AshenKingOfTheHillV1",
                "Level42 estimated. 1: the battery area within 180 s, else it fails. Four personal AFS Turret 550001 copies at the " +
                "public pools (each hidden for the owner while its copy stands); each loss counts on hidden 7, the fourth " +
                "fails 1 (before the top) or 2 (no Towers Lost counter shown). Waves 0/45/90/135 s (Thrax 531008/531009, Desert Stalkers 790121), " +
                "hold 180 s, then 2 completes. Leave-area fail not built (M2). Items 19673, 20504, 37985, 45843.", 0.3);

            mission.Enable(Scene());
        }

        /// <summary>The defence: the four turrets the wards, the waves on them, the hold, the losses counted.</summary>
        public static MissionSceneDefinition Scene()
        {
            var scene = new MissionSceneDefinition
            {
                Script = "marshes.defence",
                MapContextId = AshenMissionDataV1.MapContextId,
                HiddenObjectiveIds = new() { LostTally },
                DefeatSequences = new(),
                Names = new()
                {
                    [MarshesDefenceScene.Mission] = MissionId,
                    [MarshesDefenceScene.Locate] = Hurry,
                    [MarshesDefenceScene.Defend] = Defend,
                    [MarshesDefenceScene.Start] = DefenceStarts,
                    [MarshesDefenceScene.Waves] = (uint)Waves.Length,
                    [MarshesDefenceScene.Hold] = HoldMilliseconds,
                    [MarshesDefenceScene.Outcome] = Held,
                    [MarshesDefenceScene.Step] = 1,
                    [MarshesDefenceScene.Hide] = Batteries[0].Pool,
                    [MarshesDefenceScene.Lost] = TowersLost
                }
            };
            for (var battery = 2; battery <= Batteries.Length; battery++)
                scene.Names[MarshesDefenceScene.MoreHidden(battery)] = Batteries[battery - 1].Pool;
            scene.Sequences[0] = new SceneSequenceDefinition();
            scene.Sequences[DefenceStarts] = new SceneSequenceDefinition();
            scene.Sequences[MarshesDefenceScene.HoldTimerSequence] = new SceneSequenceDefinition();
            var defeat = 100U;
            void Add(string role, uint creature, ScenePosition at, double facing)
            {
                scene.Actors[role] = new SceneActorDefinition(role, SceneActorKind.Creature, creature, at, Orientation: facing,
                    InitiallyInteractable: false);
                scene.DefeatSequences[role] = ++defeat;
                scene.Sequences[defeat] = new SceneSequenceDefinition();
            }
            for (var battery = 1; battery <= Batteries.Length; battery++)
                Add(MarshesDefenceScene.Ward(battery), Turret, Batteries[battery - 1].At, Batteries[battery - 1].Facing);
            for (var wave = 1; wave <= Waves.Length; wave++)
            {
                scene.Names[MarshesDefenceScene.After(wave)] = Waves[wave - 1].After;
                scene.Sequences[MarshesDefenceScene.WaveTimerSequence(wave)] = new SceneSequenceDefinition();
                for (var member = 1; member <= Waves[wave - 1].Members.Length; member++)
                {
                    var (creature, at) = Waves[wave - 1].Members[member - 1];
                    Add(MarshesDefenceScene.WaveRole(wave, member), creature, at, 0);
                }
            }
            return scene;
        }

        public static void Down(MigrationBuilder migration) => AshenMissionDataV1.Remove(migration, MissionId);
    }
}
