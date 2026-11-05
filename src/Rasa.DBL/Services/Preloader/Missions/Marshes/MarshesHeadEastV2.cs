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
    /// Head East, Young Man (native mission 920), Valverde Marshes: Commander Figgins at Falcon Hold sends the player to
    /// Lieutenant Shaw's eastern guard station to hold it against an inbound Bane attack; after it Ranger Meeko, fled from his
    /// overrun village, comes to the player.
    ///
    /// Native: texts 4911-4914 and 7311 (missionconversation): log "Report to Lieutenant Shaw at the eastern guard station and
    /// defend against the inbound Bane attack force.", offer "Get out there to the far eastern guard station and give those
    /// boys a hand", turn-in (Meeko) "I barely escaped from my village in the northeast"; objectives 1 "Report to Lieutenant
    /// Shaw at the Eastern Lookout" 5041/7312, 2 "Defend the Lookout Point against the Bane Attack" 5043/7313, 3 "Speak to
    /// Ranger Meeko" 5044/7314 (missionobjective); objective conversations 920,1 package 1012 5042 (Shaw: "This is the key
    /// attack location, so stick around here and help us take 'em out. I hear their ships approaching now!") and 920,3
    /// package 1013 5045 (Meeko: "I've been trying to get through these swamps to safety for hours now."). Names 8623
    /// Commander Figgins, 8625 Lieutenant Shaw, 8627 Ranger Meeko. Indicator name 340 "Guard Station".
    ///
    /// Sourced: TaRapedia Head East, Young Man: giver Commander Figgins (Falcon Hold), receiver Ranger Meeko ("The Northern
    /// Mud Pit"; before 2008-03 "Eastern Lookout"); 70,000 XP, 5,550 credits; one of Teleract Bio Armor Helmet lvl 35, Dynamo
    /// Hazmat Armor Legs lvl 34, Dynamo Motor Assist Armor Legs lvl 38, Olympia Reflective Armor Legs lvl 34. Ten Ton Hammer:
    /// Shaw "at his guard station at 90,216,535"; "Watch for the dropships to start coming in, and expect to see a Stalker or
    /// two. After a few minutes the mission will update, and the Forean Ranger Meeko will tap you on the shoulder". TR MMO
    /// DB: level 33. The rewards the client's item block 120069-120072 (Bio Helmet 35, Hazmat Legs 34, Motor Assist Legs 38,
    /// Reflective Legs 34: TaRapedia's four in order). World DB: the Marshes Thrax Soldier 531045 and Grenadier 531046.
    ///
    /// Reconstructed (ESTIMATE, MarshesDefenceScene as Trapped! 1052):
    ///  - Shaw's stand-in (package 1012) and two AFS soldiers stand at the guard station once the player comes within 40 m of
    ///    Shaw (server-only hidden 4, its area, the scene's "approach"; to-fix 10: as Trapped!, nothing at the station can kill
    ///    them while no player is near), the public Shaw hidden from the player meanwhile; Shaw's talk (1) starts the defence.
    ///  - Two waves of the Marshes Thrax, 10 s and 90 s after the talk, from the swamp east of the station: 2 soldiers and 2
    ///    grenadiers, then 2 and 2 with a Stalker (MarshesEastAndNorthNpcsV1's own row). They come on foot: no Bane dropship
    ///    brings them (the Marshes dropships are on their own branch, and no scene spawns by dropship).
    ///  - The hold 180 s (the triage's 3 minutes for "a few minutes"); then 2 completes, the station's men and what is left of
    ///    the Thrax go (the public Shaw is back) and Meeko comes, 6 m from Shaw: his talk (3), then the turn-in at him.
    ///    Shaw's death fails 1 before the talk, 2 during the defence.
    ///  - Meeko is invulnerable (ActorGameplayPolicy.Invulnerable): the receiver cannot be killed (a killed scene actor stays
    ///    dead for its run).
    ///  - Level TR MMO DB's. Markers: the client's "Guard Station" on Shaw (1), the station (2), Meeko's place (3).
    ///
    /// V2 (2026-11-03, to-fix 13): the mission as MarshesHeadEastV1 wrote it, with to-fix 10 (the station's men stand only once the player comes within 40 m: server-only hidden 4 and the scene's approach). MarshesHeadEastV1 is the mission as the live
    /// server applied it; migration 20271104001900_Marshes_playtest_fixes removes it and writes this one.
    /// </summary>
    public static class MarshesHeadEastV2
    {
        public const uint MissionId = 920;
        public const uint Level = 33;
        public const uint Figgins = MarshesRetreadCityNpcsV1.Figgins;
        public const uint Meeko = MarshesEastAndNorthNpcsV1.Meeko;
        public const uint Report = 1, Defend = 2, Speak = 3, Near = 4;
        public const uint DefenceStarts = 1, Held = 2, Approach = 3;   // scenarios: the start (its sequence), the outcome (its step 1), the approach
        public const uint ApproachArea = 1;
        public const double ApproachRadius = 40;
        public const uint ShawPackage = MarshesEastAndNorthNpcsV1.ShawPackage, MeekoPackage = MarshesEastAndNorthNpcsV1.MeekoPackage;
        public const uint PublicShawPool = MarshesEastAndNorthNpcsV1.Shaw;
        public const uint Soldier = 531045, Grenadier = 531046, Stalker = MarshesEastAndNorthNpcsV1.Stalker;
        public const uint HoldMilliseconds = 180000;
        public const uint GuardStation = 340;           // missionobjectiveindicatorlanguage
        public const double StationRadius = 40;

        public static readonly ScenePosition ShawAt = new(90.4f, 216.88f, 535f);
        public static readonly ScenePosition[] SoldiersAt = { new(86f, 216.88f, 539f), new(95f, 216.88f, 531f) };
        public static readonly ScenePosition MeekoAt = new(95f, 216.88f, 540f);
        public const double MeekoYaw = 2.3;             // facing Shaw

        /// <summary>The waves: when (ms after the talk) and who comes from where.</summary>
        public static readonly (uint After, (uint Creature, ScenePosition At)[] Members)[] Waves =
        {
            (10000, new[] { (Soldier, new ScenePosition(120f, 217.07f, 560f)), (Soldier, new ScenePosition(123f, 217.07f, 562f)),
                (Grenadier, new ScenePosition(118f, 217.07f, 563f)), (Grenadier, new ScenePosition(124f, 217.08f, 557f)) }),
            (90000, new[] { (Soldier, new ScenePosition(130f, 214.08f, 520f)), (Soldier, new ScenePosition(132f, 214.08f, 523f)),
                (Grenadier, new ScenePosition(110f, 214.01f, 500f)), (Grenadier, new ScenePosition(113f, 214.01f, 502f)),
                (Stalker, new ScenePosition(125f, 217.08f, 545f)) })
        };

        public const uint Experience = 70000;
        public const uint Credits = 5550;
        public static readonly uint[] Rewards = { 120069, 120070, 120071, 120072 };

        public static void Up(MigrationBuilder migration)
        {
            var mission = new MarshesMissionDataV1(migration, MissionId, "Head East, Young Man", 4911, Figgins, Meeko, Level);
            mission.Reward(Experience, Credits, Rewards);
            mission.Scenario(DefenceStarts, "Shaw reported to: the Bane attack", 0);
            mission.Scenario(Held, "The guard station held", 1);
            mission.Scenario(Approach, "The player near the station: Shaw's men stand", 0);
            mission.Area(ApproachArea, MarshesMissionDataV1.MapContextId, ShawAt.X, ShawAt.Y, ShawAt.Z, ApproachRadius, 20);

            mission.Objective(Report, 5041, 7312, 0, MissionObjectiveState.Incomplete);
            mission.Objective(Defend, 5043, 7313, 1, MissionObjectiveState.Inactive);
            mission.Objective(Speak, 5044, 7314, 2, MissionObjectiveState.Inactive);
            mission.Objective(Near, 5041, 7312, 3, MissionObjectiveState.Incomplete, required: false);   // hidden, server only
            mission.Transition(Near);
            mission.EnterArea(Near, ApproachArea);
            mission.Action(Near, 1, MissionActionKind.StartScenario, scenario: Approach);

            mission.Transition(Report);
            mission.Conversation(Report, ShawPackage);
            mission.Action(Report, 1, MissionActionKind.RevealObjective, targetObjective: Defend);
            mission.Action(Report, 2, MissionActionKind.ActivateObjective, targetObjective: Defend, state: MissionObjectiveState.Incomplete);
            mission.Action(Report, 3, MissionActionKind.StartScenario, scenario: DefenceStarts);
            mission.Transition(Defend);
            mission.Progress(Defend, MissionProgressEventKind.ScenarioEvent, 1, null, counter: Held);
            mission.Action(Defend, 1, MissionActionKind.RevealObjective, targetObjective: Speak);
            mission.Action(Defend, 2, MissionActionKind.ActivateObjective, targetObjective: Speak, state: MissionObjectiveState.Incomplete);
            mission.Transition(Speak);
            mission.Conversation(Speak, MeekoPackage);
            mission.Action(Speak, 1, MissionActionKind.GrantReward, reward: 1);

            mission.NamedIndicator(Report, GuardStation, ShawAt.X, ShawAt.Y, ShawAt.Z);
            mission.Indicator(Defend, 1, ShawAt.X, ShawAt.Y, ShawAt.Z, StationRadius);
            mission.Indicator(Speak, 2, MeekoAt.X, MeekoAt.Y, MeekoAt.Z);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/920",
                "Native1 'Report to Lieutenant Shaw at the Eastern Lookout' 5041/7312, 2 'Defend the Lookout Point against the Bane " +
                "Attack' 5043/7313, 3 'Speak to Ranger Meeko' 5044/7314; objectiveconversation 920,1 pkg1012 5042 (Shaw), 920,3 " +
                "pkg1013 5045 (Meeko). Texts 4911-4914, 7311. Names 8623, 8625, 8627. Indicator name340 'Guard Station'. Item block " +
                "120069-120072.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Head_East%2C_Young_Man",
                "Giver Commander Figgins (Falcon Hold), receiver Ranger Meeko (The Northern Mud Pit); 70000XP, 5550 credits; Bio " +
                "Helmet35, Hazmat Legs34, Motor Assist Legs38, Reflective Legs34. Ten Ton Hammer: Shaw at 90,216,535; dropships, a " +
                "Stalker or two; after a few minutes Meeko taps you on the shoulder. TR MMO DB level33.", 0.5);
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:MarshesHeadEastV2",
                "Personal Shaw stand-in 760358 (pkg1012) + 2 AFS soldiers 760359 at the station; the public Shaw 760357 hidden. " +
                "The talk starts 2 waves (4 Thrax 531045/531046 at 10 s, 4 + Stalker 760360 at 90 s), on foot, no dropship; after " +
                "180 s 2 completes and Meeko 760361 (pkg1013, the receiver) comes. Shaw's death fails 1 or 2.", 0.3);

            mission.Enable(Scene());
        }

        /// <summary>The defence (MarshesDefenceScene): Shaw's stand-in and the soldiers, the two waves, Meeko after the hold.</summary>
        public static MissionSceneDefinition Scene()
        {
            var scene = new MissionSceneDefinition
            {
                Script = "marshes.defence",
                MapContextId = MarshesMissionDataV1.MapContextId,
                DefeatSequences = new(),
                Names = new()
                {
                    [MarshesDefenceScene.Mission] = MissionId,
                    [MarshesDefenceScene.Locate] = Report,
                    [MarshesDefenceScene.Defend] = Defend,
                    [MarshesDefenceScene.Start] = DefenceStarts,
                    [MarshesDefenceScene.Waves] = (uint)Waves.Length,
                    [MarshesDefenceScene.Hold] = HoldMilliseconds,
                    [MarshesDefenceScene.Outcome] = Held,
                    [MarshesDefenceScene.Step] = 1,
                    [MarshesDefenceScene.Hide] = PublicShawPool,
                    [MarshesDefenceScene.Approach] = Approach
                },
                HiddenObjectiveIds = new() { Near }
            };
            scene.Sequences[Approach] = new SceneSequenceDefinition();
            scene.Sequences[0] = new SceneSequenceDefinition();
            scene.Sequences[DefenceStarts] = new SceneSequenceDefinition();
            scene.Sequences[MarshesDefenceScene.HoldTimerSequence] = new SceneSequenceDefinition();
            var defeat = 100U;
            void Add(string role, uint creature, ScenePosition at, bool interactable, double yaw = 0, bool fallen = true,
                ActorGameplayPolicy policy = null)
            {
                scene.Actors[role] = new SceneActorDefinition(role, SceneActorKind.Creature, creature, at, Orientation: yaw,
                    InitiallyInteractable: interactable, GameplayPolicy: policy);
                if (!fallen)
                    return;
                scene.DefeatSequences[role] = ++defeat;
                scene.Sequences[defeat] = new SceneSequenceDefinition();
            }
            Add(MarshesDefenceScene.Leader, MarshesEastAndNorthNpcsV1.ShawStandIn, ShawAt, true);
            for (var member = 0; member < SoldiersAt.Length; member++)
                Add(MarshesDefenceScene.Ward(member + 2), MarshesEastAndNorthNpcsV1.StationSoldier, SoldiersAt[member], false);
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
            // Meeko invulnerable (as PlateauBetterHalfV1's immune actors): the receiver cannot be killed. A killed actor stays dead for
            // its run (the scene's actor state), so he could not come back without a relog.
            Add(MarshesDefenceScene.AfterRole(1), Meeko, MeekoAt, true, MeekoYaw, fallen: false,
                policy: new ActorGameplayPolicy { Invulnerable = true });
            return scene;
        }

        public static void Down(MigrationBuilder migration) => MarshesMissionDataV1.Remove(migration, MissionId);
    }
}
