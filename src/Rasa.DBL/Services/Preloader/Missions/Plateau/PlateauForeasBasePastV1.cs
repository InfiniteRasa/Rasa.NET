using System;
using System.Linq;
using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Content.Plateau;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Plateau
{
    /// <summary>
    /// Foreas Base Past (native mission 1178), Temporal Chamber (squad instance 2029): Veryin's second trial. Through
    /// the teleporter on Veryin's left to Foreas Base in its first days, under a Bane attack; Colonel Fonseca has the
    /// player take the Forean diplomat Villaran to the drop-ship, then help to beat Taskmaster Xykryl's strike force.
    ///
    /// Native: texts 8846-8850 (missionconversation): offer "The Bane ... launched an attack on Foreas Base with the
    /// intent of killing the Forean diplomat, Villaran ... You will arrive during the Bane assault", turn-in "Now I will
    /// instruct you on opening the Communications Chamber."; objectives (missionobjective), no objective 2: 1 "Speak
    /// with Colonel Fonseca." 8856/8857, 3 "Escort Diplomat Villaran to the AFS Drop-Ship." 8915/8916 ("the dropship
    /// northwest of AFS HQ"), 4 "Speak with Colonel Fonseca" 8947/8948, 5 "Kill the Bane Taskmaster." 8950/8951 with
    /// counters 8952 "Bane Taskmaster Killed" and 8953 "Taskmaster Strike Force Eliminated", 6 "Speak to Colonel
    /// Fonseca." 8954/8955, 7 "Return to the Temporal Chamber" 8957/8958 ("Go to the teleporter north of HQ"). The
    /// talks (objectiveconversation): 1, 4, 6 Fonseca 1238 (8858 "take him to the drop-ship outside", 8949 "a Bane
    /// Taskmaster is headed in with his personal strike force", 8956), 7 Veryin 1199 (8959).
    ///
    /// Sourced: TaRapedia Foreas Base Past: giver the Eloh Hologram (Temporal Chamber), requires The Final Assault,
    /// follow-up Send a Message; 140,000 XP, 9,250 credits, no items. Walkthrough: "Kill the Striders immediately,
    /// doing this will make sure the friendly NPCs can handle the assault. Chances are the taskmaster will be killed
    /// before you even get the objective to kill him". TR MMO DB: level 30.
    ///
    /// Reconstructed (ESTIMATE, the user's choices of 2026-10-24; see PlateauForeasBasePastNpcsV1 for the NPCs):
    ///  - The escort as upstream's Wilderness escorts: Villaran walks a fixed route (the navmesh's path from HQ through
    ///    the gate in the north wall to the drop-ship), stops to fight, and his death fails 3 (PlateauForeasBasePastScene).
    ///  - Two waves: Fonseca's first talk brings four Thrax Recon Soldiers and two Thrax Cavalrymen (Striders) at
    ///    Villaran; his second brings Xykryl and four Thrax Special Forces at the player. 5 counts the five of them,
    ///    whoever kills them: one counter, as the server has one count per objective (8952 is not shown).
    ///  - A personal drop-ship (ArchHumGenVehicleDropship) on a landing pad in the barricaded place north-west of HQ;
    ///    Villaran is gone when he reaches it. Four personal AFS 1st Infantry in the yard north of HQ fight the Bane.
    ///  - 7 is Veryin's talk (the pad north of HQ takes the player back). Level 30. No indicators: instance.
    /// </summary>
    public static class PlateauForeasBasePastV1
    {
        public const uint MissionId = PlateauForeasBasePastScene.MissionId;
        public const uint Level = 30;
        public const uint Veryin = PlateauTemporalChamberNpcsV1.Veryin, VeryinPackage = PlateauTemporalChamberNpcsV1.VeryinPackage;
        public const uint FonsecaPackage = PlateauForeasBasePastNpcsV1.FonsecaPackage;

        public const uint FirstTalk = 1, Escort = PlateauForeasBasePastScene.Escort, SecondTalk = PlateauForeasBasePastScene.SecondTalk,
            Kill = PlateauForeasBasePastScene.Kill, ThirdTalk = 6, Return = 7;
        public const uint EscortStarts = PlateauForeasBasePastScene.EscortStarts, AssaultStarts = PlateauForeasBasePastScene.AssaultStarts,
            Kills = PlateauForeasBasePastScene.Kills;

        public const uint Experience = 140000;
        public const uint Credits = 9250;
        public const float EscortSpeed = 2.5f;

        public const uint DropshipClass = 7331;         // ArchHumGenVehicleDropship
        public const uint LandingPadClass = 9488;       // ArchHumDropshipLandingpadV01

        public static readonly ScenePosition VillaranAt = new(-470, 230.93f, 415.2f);

        /// <summary>The navmesh's path from beside Fonseca out of HQ's north door, through the wall's gate, to the drop-ship (NavMeshQuery.FindPath, 113 m).</summary>
        public static readonly ScenePosition[] Route =
        {
            VillaranAt,
            new(-468, 230.45f, 413.2f),
            new(-465.2f, 229.85f, 412.4f),
            new(-465.2f, 228.85f, 416.8f),
            new(-466.4f, 228.85f, 420),
            new(-472.8f, 227.85f, 439.2f),
            new(-488, 227.25f, 488),
            new(-488, 226.25f, 518)
        };

        public static readonly ScenePosition DropshipAt = new(-488, 226.25f, 530);
        public const double DropshipYaw = 3.1416;

        public static readonly ScenePosition[] DefendersAt =
            { new(-482, 226.25f, 470), new(-466, 226.34f, 470), new(-480, 226.33f, 482), new(-468, 226.98f, 482) };

        /// <summary>The first wave, in the field north of the wall: the four Recon Soldiers, then the two Cavalrymen.</summary>
        public static readonly ScenePosition[] FirstWaveAt =
        {
            new(-480, 226.12f, 545), new(-470, 226.03f, 552), new(-500, 226.25f, 515), new(-458, 225.84f, 556),
            new(-503.6f, 228.25f, 544.8f), new(-440, 226.09f, 470)
        };

        /// <summary>The strike force, in the same field: Xykryl, then the four Special Forces.</summary>
        public static readonly ScenePosition[] SecondWaveAt =
        {
            new(-470, 226.03f, 552), new(-480, 226.12f, 545), new(-458, 225.84f, 556), new(-500, 226.25f, 515),
            new(-503.6f, 228.25f, 544.8f)
        };

        public static void Up(MigrationBuilder migration)
        {
            var mission = new PlateauMissionDataV1(migration, MissionId, "Foreas Base Past", 8846, Veryin, Veryin, Level);
            mission.Reward(Experience, Credits, Array.Empty<uint>());
            mission.Prerequisite(PlateauFinalAssaultV1.MissionId);

            mission.Objective(FirstTalk, 8856, 8857, 0, MissionObjectiveState.Incomplete);
            mission.Objective(Escort, 8915, 8916, 1, MissionObjectiveState.Inactive);
            mission.Objective(SecondTalk, 8947, 8948, 2, MissionObjectiveState.Inactive);
            mission.Objective(Kill, 8950, 8951, 3, MissionObjectiveState.Inactive, counterTextId: 8953);
            mission.Objective(ThirdTalk, 8954, 8955, 4, MissionObjectiveState.Inactive);
            mission.Objective(Return, 8957, 8958, 5, MissionObjectiveState.Inactive);
            mission.Scenario(EscortStarts, "Villaran walks to the drop-ship; the first wave", 0);
            mission.Scenario(AssaultStarts, "Taskmaster Xykryl's strike force", 0);
            mission.Scenario(Kills, "Xykryl and his strike force killed", 1 + PlateauForeasBasePastScene.StrikeForce);

            mission.Transition(FirstTalk);
            mission.Conversation(FirstTalk, FonsecaPackage);
            PlateauSilentButDeadlyV1.Open(mission, FirstTalk, 1, Escort);
            mission.Action(FirstTalk, 3, MissionActionKind.StartScenario, scenario: EscortStarts);

            // 3 is the scene's (PlateauForeasBasePastScene): it completes 3 and opens 4, or fails 3.
            mission.Transition(SecondTalk);
            mission.Conversation(SecondTalk, FonsecaPackage);
            PlateauSilentButDeadlyV1.Open(mission, SecondTalk, 1, Kill);
            mission.Action(SecondTalk, 3, MissionActionKind.StartScenario, scenario: AssaultStarts);

            mission.Transition(Kill);
            var count = 1 + (uint)PlateauForeasBasePastScene.StrikeForce;
            for (var step = 1U; step <= count; step++)
                mission.Progress(Kill, MissionProgressEventKind.ScenarioEvent, step, count, counter: Kills, trigger: step);
            PlateauSilentButDeadlyV1.Open(mission, Kill, 1, ThirdTalk);

            mission.Transition(ThirdTalk);
            mission.Conversation(ThirdTalk, FonsecaPackage);
            PlateauSilentButDeadlyV1.Open(mission, ThirdTalk, 1, Return);

            mission.Transition(Return);
            mission.Conversation(Return, VeryinPackage);
            mission.Action(Return, 1, MissionActionKind.GrantReward, reward: 1);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1178",
                "Native1 Fonseca 8856/8857, 3 escort Villaran 8915/8916, 4 Fonseca 8947/8948, 5 kill the Taskmaster 8950/8951 " +
                "counters 8952/8953, 6 Fonseca 8954/8955, 7 return 8957/8958; no objective 2. objectiveconversation 1/4/6 on 1238 " +
                "(8858, 8949, 8956), 7 on 1199 (8959). Texts 8846-8850. Names 9081 Fonseca, 9085 Villaran, 9108 Xykryl.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Foreas_Base_Past",
                "Giver the Eloh Hologram (Temporal Chamber); requires The Final Assault; follow-up Send a Message; 140000XP, 9250 " +
                "credits. Walkthrough: kill the Striders, the friendly NPCs handle the assault, the taskmaster is often killed " +
                "before his objective. TR MMO DB level30.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:PlateauForeasBasePastV1",
                "Level30. Personal scene: Villaran650097 walks the navmesh path from HQ to the drop-ship (113m), his death fails 3; " +
                "wave 1 (4 Recon650099, 2 Cavalryman650100) at Villaran on Fonseca's first talk; Xykryl650101 and 4 Special " +
                "Forces650102 at the player on the second, 5 counts all five; 4 AFS 1st Infantry650098, a drop-ship7331 on a " +
                "pad9488 NW of HQ. Places are estimates. No indicator (instance).", 0.3);

            mission.Enable(Scene());
        }

        public static void Down(MigrationBuilder migration) => PlateauMissionDataV1.Remove(migration, MissionId);

        /// <summary>The mission's personal NPCs and objects on the squad's copy of the Temporal Chamber.</summary>
        public static MissionSceneDefinition Scene()
        {
            var hostile = new ActorGameplayPolicy
            {
                RewardScenarioKills = true, TrackParticipation = true,
                // The run's own creature: no ordinary loot table can bind it, so none (as Clean Slate's).
                Loot = new AuthoredLootProfile(Array.Empty<LootDrop>())
            };
            var scene = new MissionSceneDefinition
            {
                Script = "plateau.foreas-base-past",
                MapContextId = PlateauForeasBasePastNpcsV1.MapContextId,
                Routes = new()
                {
                    [PlateauForeasBasePastScene.Route] = new SceneRoute(PlateauForeasBasePastScene.Route,
                        Array.ConvertAll(Route, point => new SceneWaypoint(point)), Speed: EscortSpeed)
                },
                DefeatSequences = new(),
                Sequences = new()
                {
                    [0] = new SceneSequenceDefinition(),
                    [EscortStarts] = new SceneSequenceDefinition(),
                    [AssaultStarts] = new SceneSequenceDefinition()
                }
            };

            scene.Actors[PlateauForeasBasePastScene.Villaran] = new SceneActorDefinition(PlateauForeasBasePastScene.Villaran,
                SceneActorKind.Creature, PlateauForeasBasePastNpcsV1.Villaran, VillaranAt, Orientation: 0, InitiallyInteractable: false);
            for (var index = 0; index < DefendersAt.Length; index++)
                scene.Actors[PlateauForeasBasePastScene.Statics[index]] = new SceneActorDefinition(PlateauForeasBasePastScene.Statics[index],
                    SceneActorKind.Creature, PlateauForeasBasePastNpcsV1.Defender, DefendersAt[index], Orientation: 0, InitiallyInteractable: false);
            scene.Actors["landing-pad"] = new SceneActorDefinition("landing-pad", SceneActorKind.Object, LandingPadClass, DropshipAt,
                Orientation: DropshipYaw, InitiallyInteractable: false);
            scene.Actors["dropship"] = new SceneActorDefinition("dropship", SceneActorKind.Object, DropshipClass, DropshipAt,
                Orientation: DropshipYaw, InitiallyInteractable: false, ViewRadius: 120);

            for (var index = 0; index < PlateauForeasBasePastScene.FirstWave.Length; index++)
            {
                var role = PlateauForeasBasePastScene.FirstWave[index];
                var template = index < 4 ? PlateauForeasBasePastNpcsV1.Recon : PlateauForeasBasePastNpcsV1.Cavalryman;
                scene.Actors[role] = new SceneActorDefinition(role, SceneActorKind.Creature, template, FirstWaveAt[index],
                    Orientation: Math.PI, GameplayPolicy: hostile);
            }

            var second = PlateauForeasBasePastScene.SecondWave().ToArray();
            for (var index = 0; index < second.Length; index++)
            {
                var role = second[index];
                var template = index == 0 ? PlateauForeasBasePastNpcsV1.Xykryl : PlateauForeasBasePastNpcsV1.SpecialForces;
                scene.Actors[role] = new SceneActorDefinition(role, SceneActorKind.Creature, template, SecondWaveAt[index],
                    Orientation: Math.PI, GameplayPolicy: hostile);
                var sequence = PlateauForeasBasePastScene.DefeatedFrom + 1 + (uint)index;
                scene.DefeatSequences[role] = sequence;
                scene.Sequences[sequence] = new SceneSequenceDefinition();
            }
            return scene;
        }
    }
}
