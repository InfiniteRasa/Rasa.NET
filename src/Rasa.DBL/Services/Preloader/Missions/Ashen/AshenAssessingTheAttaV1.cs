using System.Linq;
using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Ashen
{
    /// <summary>
    /// Assessing The Atta (native mission 1536), Avernus Outpost (squad instance 2110): Captain Petrovich sends the player with
    /// Sgt. Frogga's men down the northern tunnel to find Captain Tananda at the Cave In, and back to him.
    ///
    /// Native: texts 13804-13808 (missionconversation): title "Assessing The Atta", log "Follow Frogga's men down the tunnel and find
    /// Captain Tananda.", offer "Frogga says Captain Tananda went off on her own down this tunnel, looking for something ... Go with
    /// his men, find out if she's ok. When you get back, let me know", turn-in "That's interesting. Ok, I have an idea what to do
    /// next.", 13808 "none". Objectives (missionobjective) 1 "Finding Tananda" 13809/13810, 2 "Tananda's Story" 13811/13812, 3
    /// "Return to Petrovich" 13814/13815; no counters, no marker names. Objective conversations 1536,2 package 1914 (Tananda)
    /// 13813 ("This is definitely where the Atta came in ... There's a teleporter on the opposite side of the main cavern that
    /// leads to the Queen's chamber ..."), 1536,3 package 1803 (Petrovich) 13816 ("The queen, huh? Frogga's men found the
    /// teleporter. There's another one in here, but it's not functional. At least not yet..."). Names 9728 Captain Tananda, 9731
    /// Sgt. Frogga, 9672 Captain Petrovich.
    ///
    /// Sourced: TaRapedia Assessing The Atta: giver and reward giver Captain Petrovich (Avernus Outpost); requires Petrovich's Run;
    /// follow-up God Save the Queen; 90,000 XP, 8,200 credits; one of Lv 40 Phoenix Mech Armor Legs (icon 6), Lv 44 Pulsar Hazmat
    /// Armor Legs (5), Lv 44 Teleract Reflective Armor Vest (4), Lv 38 Pathogex Motor Assist Armor Vest (4), "(Experimental)";
    /// "Go down the northern 'Cave In' tunnel and meet up with Captain Tananda ... return to Captain Petrovich in the Food Storage
    /// chamber" (0.5).
    ///
    /// Reconstructed (ESTIMATE, confidence 0.3):
    ///  - Level 43 (triage D5, the Avernus Outpost band 41-44, after 1535's 42). Requires 1535 (TaRapedia). Given and taken in at
    ///    Petrovich's second row at the Food Storage (AshenAvernusOutpostNpcsV1 790643, 1535's receiver).
    ///  - Frogga's men: two Avernus Outpost Soldiers (set 17's row 790645, the player's own: OwnerOnly, unhurt by anything: the
    ///    client has no fail text) stand by Frogga from the accept and, a moment after it (a scene timer: a scene's start inside the
    ///    accept is refused), lead down the northern tunnel to the Cave In on the navmesh (QuestSceneIntents.Escort, as 1535's
    ///    Petrovich); they stay there till the mission ends. Nothing waits for them: 1 is the player's own
    ///    walk.
    ///  - Tananda: her public pool (AshenAvernusHiveNpcsV1 790650) named by the scene as a public-spawn actor (TanandaRole, as set
    ///    14's Kuutk and fix #55's Altera): she stands 20 m from the warcry pool 531051 and can be killed there; named, her pool
    ///    comes back on its own time (2 s) in the squad's copy, not after the copy's 45 min, so 2 is never held up.
    ///  - 1 the player at Tananda (AreaEntered, TanandaRadius) opens 2; 2 the talk with Tananda (13813) opens 3; 3 the talk with
    ///    Petrovich (13816) gives the reward; the turn-in at him.
    ///  - The items: the template of each named kind, version (the icon number) and slot whose level band starts at TaRapedia's
    ///    level, the lowest template id of its class (client entityclass names): Mech V06 Legs 46149 (Armor_T3_Mech_V06_RAR_Legs_40_to_44),
    ///    Hazmat V05 Legs 23062 (Armor_T2_Hazmat_V05_RAR_Legs_44_to_48), Reflective V04 Vest 37705
    ///    (Armor_T2_Reflective_V04_RAR_Vest_44_to_48), Motor Assist V04 Vest 29247 (Armor_T1_MotorAssist_V04_UNC_Vest_38_to_42); one
    ///    to choose. "Experimental" is not a client quality name: the version's only quality is taken.
    ///  - Markers (unnamed): 1 and 2 at Tananda, 3 at Petrovich.
    /// </summary>
    public static class AshenAssessingTheAttaV1
    {
        public const uint MissionId = 1536;
        public const uint Level = 43;
        public const uint Outpost = AshenAvernusHiveNpcsV1.Outpost;
        public const uint Petrovich = AshenAvernusOutpostNpcsV1.PetrovichTop, PetrovichPackage = AshenAvernusOutpostNpcsV1.PetrovichPackage;
        public const uint TanandaPackage = AshenAvernusHiveNpcsV1.TanandaPackage, Variant = 1;
        public const uint FindingTananda = 1, TanandasStory = 2, ReturnToPetrovich = 3;
        public const uint TanandaArea = 1;
        public const double TanandaRadius = 10, NpcMarkerRadius = 8;

        /// <summary>Scenarios: the men go (the timer's sequence).</summary>
        public const uint MenGo = 30;
        public const uint MenDelayMilliseconds = 2000;
        public const float RouteSpeed = 4f;
        public const string LeadRoute = "frogga-men-down-the-tunnel", SecondRoute = "frogga-men-down-the-tunnel-2";
        public static string ManRole(int index) => $"frogga-man-{index + 1}";

        /// <summary>Tananda's public pool, named so that it comes back on its own time in the squad's copy.</summary>
        public const string TanandaRole = "tananda";

        /// <summary>Frogga's men by him at the accept.</summary>
        public static readonly ScenePosition[] MenAt = { new(650f, 353.48f, -240f), new(654f, 352.85f, -240f) };

        /// <summary>Their way down the northern tunnel to the Cave In (the client-floor walk's corners on the navmesh's ground, ESTIMATE).</summary>
        public static readonly ScenePosition[] TunnelWay =
        {
            new(588f, 352.9f, -246f), new(560f, 355.27f, -214f), new(560f, 352.88f, -165f), new(559f, 352.28f, -160f),
            new(558f, 361.37f, -129f), new(553f, 362.88f, -126f), new(516f, 371.36f, -127f), new(513f, 372.79f, -131f),
            new(513f, 374.76f, -150f), new(491f, 379.63f, -166f), new(460f, 384.22f, -165f), new(459f, 385.2f, -135f),
            new(463f, 385.18f, -134f), new(463f, 385.23f, -92f), new(458f, 387.7f, -72f), new(457f, 390.85f, -57f),
            new(454f, 392.86f, -44f)
        };

        public const uint Experience = 90000;
        public const uint Credits = 8200;
        public const uint MechLegs = 46149;             // Armor_T3_Mech_V06_RAR_Legs_40_to_44 (class 23513)
        public const uint HazmatLegs = 23062;           // Armor_T2_Hazmat_V05_RAR_Legs_44_to_48 (class 14486)
        public const uint ReflectiveVest = 37705;       // Armor_T2_Reflective_V04_RAR_Vest_44_to_48 (class 19096)
        public const uint MotorAssistVest = 29247;      // Armor_T1_MotorAssist_V04_UNC_Vest_38_to_42 (class 16431)

        public static void Up(MigrationBuilder migration)
        {
            var mission = new AshenMissionDataV1(migration, MissionId, "Assessing The Atta", 13804, Petrovich, Petrovich, Level);
            mission.Prerequisite(AshenPetrovichsRunV1.MissionId);
            mission.Reward(Experience, Credits, new[] { MechLegs, HazmatLegs, ReflectiveVest, MotorAssistVest });

            mission.Objective(FindingTananda, 13809, 13810, 0, MissionObjectiveState.Incomplete);
            mission.Objective(TanandasStory, 13811, 13812, 1, MissionObjectiveState.Inactive);
            mission.Objective(ReturnToPetrovich, 13814, 13815, 2, MissionObjectiveState.Inactive);

            var at = AshenAvernusHiveNpcsV1.TanandaAt;
            mission.Area(TanandaArea, Outpost, at.X, at.Y, at.Z, TanandaRadius);
            mission.Transition(FindingTananda);
            mission.EnterArea(FindingTananda, TanandaArea);
            Open(mission, FindingTananda, 1, TanandasStory);

            mission.Transition(TanandasStory);
            mission.Conversation(TanandasStory, TanandaPackage, Variant);
            Open(mission, TanandasStory, 1, ReturnToPetrovich);

            mission.Transition(ReturnToPetrovich);
            mission.Conversation(ReturnToPetrovich, PetrovichPackage, Variant);
            mission.Action(ReturnToPetrovich, 1, MissionActionKind.GrantReward, reward: 1);

            var top = AshenAvernusOutpostNpcsV1.PetrovichTopAt;
            mission.Indicator(FindingTananda, 1, at.X, at.Y, at.Z, TanandaRadius, Outpost);
            mission.Indicator(TanandasStory, 2, at.X, at.Y, at.Z, NpcMarkerRadius, Outpost);
            mission.Indicator(ReturnToPetrovich, 3, top.X, top.Y, top.Z, NpcMarkerRadius, Outpost);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1536",
                "Native1 'Finding Tananda' 13809/13810, 2 'Tananda's Story' 13811/13812, 3 'Return to Petrovich' 13814/13815; no " +
                "counters, no indicator names. objectiveconversation 1536,2 pkg1914 13813, 1536,3 pkg1803 13816. Texts 13804-13808. " +
                "Names 9728 Tananda, 9731 Frogga.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Assessing_The_Atta",
                "Giver/reward giver Captain Petrovich (Avernus Outpost); requires Petrovich's Run; follow-up God Save the Queen; " +
                "90000XP, 8200 credits; Lv40 Phoenix Mech Armor Legs 6, Lv44 Pulsar Hazmat Armor Legs 5, Lv44 Teleract Reflective " +
                "Armor Vest 4, Lv38 Pathogex Motor Assist Armor Vest 4 (Experimental); the northern Cave In tunnel, Tananda, back to " +
                "Petrovich in the Food Storage chamber.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:AshenAssessingTheAttaV1",
                "Level43; requires 1535; Petrovich's second row 790643 gives and rewards. Frogga's men (two Avernus Outpost Soldiers " +
                "790645, the player's own, unhurt) lead a navmesh route down the northern tunnel 2 s after the accept and stay at " +
                "the Cave In; Tananda's pool a named public spawn (back on its own time); 1 the 10 m area at Tananda (449,391.46,-45); 2 her talk (pkg1914); 3 Petrovich's talk (pkg1803) and the " +
                "reward. Items 46149, 23062, 37705, 29247, one to choose.", 0.3);

            mission.Enable(Scene());
        }

        /// <summary>Frogga's men and their way to the Cave In, in the squad's copy.</summary>
        public static MissionSceneDefinition Scene()
        {
            var scene = new MissionSceneDefinition
            {
                Script = "data.sequence",
                BoundMapContextId = Outpost,    // taken at Petrovich in the squad's copy
                MapContextId = Outpost,
                Routes = new()
            };
            var unhurt = new ActorGameplayPolicy { Invulnerable = true };
            for (var index = 0; index < MenAt.Length; index++)
                scene.Actors[ManRole(index)] = new SceneActorDefinition(ManRole(index), SceneActorKind.Creature, AshenAvernusOutpostNpcsV1.Soldier,
                    MenAt[index], GameplayPolicy: unhurt, OwnerOnly: true);
            scene.Actors[TanandaRole] = new SceneActorDefinition(TanandaRole, SceneActorKind.PublicSpawn, AshenAvernusHiveNpcsV1.Tananda);
            scene.Routes[LeadRoute] = new SceneRoute(LeadRoute, TunnelWay.Select(point => new SceneWaypoint(point)).ToArray(), Speed: RouteSpeed);
            // The second man stops a corner short of the first, behind him.
            scene.Routes[SecondRoute] = new SceneRoute(SecondRoute, TunnelWay.Take(TunnelWay.Length - 1).Select(point => new SceneWaypoint(point)).ToArray(), Speed: RouteSpeed);

            scene.Sequences[0] = new SceneSequenceDefinition { Timers = { new SequenceTimer("frogga-men-go", MenDelayMilliseconds, MenGo) } };
            for (var index = 0; index < MenAt.Length; index++)
                scene.Sequences[0].World.Add(new EnsureActorIntent($"{ManRole(index)}-comes", ManRole(index)));
            scene.Sequences[MenGo] = new SceneSequenceDefinition
            {
                World =
                {
                    QuestSceneIntents.Escort("frogga-man-1-leads", ManRole(0), LeadRoute),
                    QuestSceneIntents.Escort("frogga-man-2-leads", ManRole(1), SecondRoute)
                }
            };
            return scene;
        }

        public static void Down(MigrationBuilder migration) => AshenMissionDataV1.Remove(migration, MissionId);

        /// <summary>The objective's action <paramref name="action"/> reveals and opens <paramref name="next"/>.</summary>
        private static void Open(AshenMissionDataV1 mission, uint objective, uint action, uint next)
        {
            mission.Action(objective, action, MissionActionKind.RevealObjective, targetObjective: next);
            mission.Action(objective, action + 1, MissionActionKind.ActivateObjective, targetObjective: next, state: MissionObjectiveState.Incomplete);
        }
    }
}
