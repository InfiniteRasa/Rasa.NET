using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Runtime;
using Rasa.Missions.Scenes;
using Rasa.Services.Preloader.Missions.Plateau;
using Rasa.Services.Preloader.Missions.Wilderness;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Crucible
{
    /// <summary>
    /// Genome Sweet Genome (native mission 1899), Ligo Crucible, the end of the Thrax hybrid arc: Zupa Eugin gives the player
    /// a Penumbra intrusion program for the Bane laboratory under Khulago Base. There Overseer Skeev fights, or, to one who
    /// gave him the vaccine in the Wilderness, offers the data; the Bane terminal by him gives the Map of the Thrax Genome,
    /// which goes to Eugin and then to Special Agent Capriulo. Completing it unlocks the Thrax hybrid (HybridUnlocks).
    ///
    /// Native: texts 19491-19495 (missionconversation; 19494 the finishing text, Capriulo's); objectives 7 "Get a map of the
    /// Thrax Genome." 19643/19644, 4 "Kill Skeev!" 19514/19515, 5 "Download the Genome Map." 19516/19517, 6 "hidden failure
    /// conditions objective" 19521/19522, 2 "Return to Zupa Eugin." 19498/19499, 3 "Return to Special Agent Capriulo."
    /// 19500/19501, no counters (missionobjective); objective conversations (1899,7) package 2371 (Skeev): variant 1 body 19651,
    /// choice 1 19652 "Prepare to do battle"; variant 27 body 19647, choice 1 19648 (refuse, battle), choice 2 19649 (accept,
    /// download); (1899,5) package 2372 (the terminal) 19518; (1899,2) 2337 (Eugin) 19519; (1899,3) 2335 (Capriulo) 19520. Items
    /// class 28662 Penumbra Intrusion Program (template 121151), 28660 Map of the Thrax Genome (121150).
    ///
    /// Sourced: TaRapedia Genome Sweet Genome: Eugin gives, Special Agent Capriulo rewards; requires Designer Genes; a clone
    /// credit, unlocks the Thrax hybrid; XP "???"; teleporter at (114, 140, -366), Skeev at (-136, 200, -288); the vaccine
    /// branch of Orders From High Command: helped, "Refuse Skeev's offer and prepare to do battle" or "Accept Skeev's offer to
    /// download the data you need"; ignored or refused, "Prepare to do battle with Skeev"; either way "the Bane Terminal nearby
    /// will unlock"; the terminal "Bane CommLink Terminal" (cache, 0.5). TTH: the laboratory past the doorway at
    /// (276, 160, -370), a circular room with a teleporter, Skeev's room at (-124, 200, -288) with Bane in sleeping pods; return
    /// to Eugin, then to Capriulo in the nightclub (0.5). TR MMO DB level 35, 0 XP, 0 credits.
    ///
    /// Reconstructed (ESTIMATE):
    ///  - The program at the accept. The laboratory reached by the tunnel pad (CrucibleHybridArcNpcsV1).
    ///  - Skeev the player's own, at TaRapedia's spot (21 m outside the reach of the laboratory's Thrax pools): the neutral
    ///    talker (770061) first. Variant 27 for a player whose Daghda's Urn outcome (WildernessDaghdasUrnV1.OutcomeFlagId) is 26
    ///    (he surrendered the vaccine to Skeev; upstream 701), variant 1 for everyone else. Variant 1's one choice is a completion
    ///    topic (the server's choice topics need two; the client's completion text 19646 of variant 1 is its body's text). Battle
    ///    (variant 1's talk, or choice 1 of 27): 4 opens,
    ///    the talker goes and the hostile Skeev (770062) attacks the player; his death completes 4 and opens 5. Accept (choice 2
    ///    of 27): 5 opens. 7 completes at the talk; 4 is optional.
    ///  - 5: the player's own talking terminal (class 10519, as Who's In Charge Here?) 5 m from Skeev; its talk takes the
    ///    program, gives the map and opens 2. 2: Eugin takes the map, 3 opens. 3: Capriulo's talk completes it.
    ///  - 6 not written (the client gives it no condition). No clone credit: the server has none.
    ///  - Markers: the tunnel pad and Skeev (7), Skeev (4), the terminal (5), Eugin (2), Capriulo (3).
    /// </summary>
    public static class CrucibleGenomeSweetGenomeV1
    {
        public const uint MissionId = 1899;
        public const uint Level = 35;
        public const uint Eugin = CrucibleHybridArcNpcsV1.Eugin, EuginPackage = CrucibleHybridArcNpcsV1.EuginPackage;
        public const uint Capriulo = CrucibleHybridArcNpcsV1.Capriulo, CapriuloPackage = CrucibleHybridArcNpcsV1.CapriuloPackage;
        public const uint GetMap = 7, KillSkeev = 4, Download = 5, ReturnToEugin = 2, ReturnToCapriulo = 3;

        /// <summary>Skeev's talk: the variants, and the transitions of 7 (battle from 1, battle from 27, accept from 27).</summary>
        public const uint SkeevPackage = CrucibleHybridArcNpcsV1.SkeevPackage;
        public const uint IgnoredVariant = 1, HelpedVariant = 27;
        public const uint Battle = 1, HelpedBattle = 2, Accept = 3;
        public const uint VaccineFlag = WildernessDaghdasUrnV1.OutcomeFlagId, Surrendered = 26;

        /// <summary>Scenarios (and their scene sequences): Skeev fights, Skeev dead.</summary>
        public const uint SkeevFights = 1, SkeevDead = 2;
        public const string TalkerRole = "skeev-talks", FighterRole = "skeev-fights", TerminalRole = "khulago-lab-terminal";
        public static readonly ScenePosition SkeevAt = new(-136f, 199.74f, -288f);
        public const double SkeevOrientation = 1.5708;  // facing east, to the way in
        public const uint TerminalClass = CrucibleWhosInChargeHereV1.TerminalClass;  // UsableNPCBaneKeyboard, "Bane CommLink Terminal"
        public const uint TerminalPackage = 2372;
        public static readonly ScenePosition TerminalAt = new(-131f, 199.86f, -284f);

        public const uint ProgramTemplate = 121151, ProgramClass = 28662;
        public const uint MapTemplate = 121150, MapClass = 28660;
        public const string ProgramKey = "penumbra-intrusion-program", MapKey = "map-of-the-thrax-genome";

        public const double PadRadius = 4, SkeevRadius = 8, FightRadius = 15, TerminalRadius = 6, NpcRadius = 8;

        public static void Up(MigrationBuilder migration)
        {
            var mission = new CrucibleMissionDataV1(migration, MissionId, "Genome Sweet Genome", 19491, Eugin, Capriulo, Level);
            mission.Reward(0, 0, new uint[0]);
            mission.Prerequisite(CrucibleDesignerGenesV1.MissionId);
            mission.Scenario(SkeevFights, "Skeev fights: the talker goes, the hostile Skeev comes", 0);
            mission.Scenario(SkeevDead, "Skeev is dead", 1);

            mission.Objective(GetMap, 19643, 19644, 0, MissionObjectiveState.Incomplete);
            mission.Objective(KillSkeev, 19514, 19515, 1, MissionObjectiveState.Inactive, required: false);
            mission.Objective(Download, 19516, 19517, 2, MissionObjectiveState.Inactive);
            mission.Objective(ReturnToEugin, 19498, 19499, 3, MissionObjectiveState.Inactive);
            mission.Objective(ReturnToCapriulo, 19500, 19501, 4, MissionObjectiveState.Inactive);

            // 7: Skeev's talk: battle (from either text) or the data (from 27).
            foreach (var (transition, variant) in new[] { (Battle, IgnoredVariant), (HelpedBattle, HelpedVariant), (Accept, HelpedVariant) })
            {
                mission.Transition(GetMap, transition, transition);
                mission.Conversation(GetMap, SkeevPackage, variant, transition);
                if (transition == Accept)
                    CrucibleCalendarGirlV1.Open(mission, GetMap, transition, 1, Download);
                else
                {
                    CrucibleCalendarGirlV1.Open(mission, GetMap, transition, 1, KillSkeev);
                    mission.Action(GetMap, 3, MissionActionKind.StartScenario, scenario: SkeevFights, transition: transition);
                }
            }

            // 4: Skeev dead opens 5.
            mission.Transition(KillSkeev);
            mission.Progress(KillSkeev, MissionProgressEventKind.ScenarioEvent, 1, null, counter: SkeevDead);
            CrucibleCalendarGirlV1.Open(mission, KillSkeev, 1, 1, Download);

            // 5: the terminal takes the program and gives the map.
            mission.Transition(Download);
            mission.Conversation(Download, TerminalPackage);
            mission.Action(Download, 1, MissionActionKind.ConsumeMissionItem,
                itemIntent: new ConsumeMissionItemIntent("penumbra-program-run", MissionId, ProgramKey, 1, MissionItemScope.AssignmentIssued));
            mission.Action(Download, 2, MissionActionKind.IssueMissionItem,
                itemIntent: new IssueMissionItemIntent("thrax-genome-map-downloaded", MissionId, MapKey, MapTemplate, 1));
            CrucibleCalendarGirlV1.Open(mission, Download, 1, 3, ReturnToEugin);

            // 2: Eugin takes the map.
            mission.Transition(ReturnToEugin);
            mission.Conversation(ReturnToEugin, EuginPackage);
            mission.Action(ReturnToEugin, 1, MissionActionKind.ConsumeMissionItem,
                itemIntent: new ConsumeMissionItemIntent("eugin-takes-the-genome-map", MissionId, MapKey, 1, MissionItemScope.AssignmentIssued));
            CrucibleCalendarGirlV1.Open(mission, ReturnToEugin, 1, 2, ReturnToCapriulo);

            // 3: Capriulo ends it.
            mission.Transition(ReturnToCapriulo);
            mission.Conversation(ReturnToCapriulo, CapriuloPackage);
            mission.Action(ReturnToCapriulo, 1, MissionActionKind.GrantReward, reward: 1);

            var pad = CrucibleHybridArcNpcsV1.TunnelPadAt;
            var eugin = CrucibleHybridArcNpcsV1.EuginAt;
            var capriulo = CrucibleHybridArcNpcsV1.CapriuloAt;
            mission.Indicator(GetMap, 1, pad.X, pad.Y, pad.Z, PadRadius);
            mission.Indicator(GetMap, 2, SkeevAt.X, SkeevAt.Y, SkeevAt.Z, SkeevRadius);
            mission.Indicator(KillSkeev, 3, SkeevAt.X, SkeevAt.Y, SkeevAt.Z, FightRadius);
            mission.Indicator(Download, 4, TerminalAt.X, TerminalAt.Y, TerminalAt.Z, TerminalRadius);
            mission.Indicator(ReturnToEugin, 5, eugin.X, eugin.Y, eugin.Z, NpcRadius);
            mission.Indicator(ReturnToCapriulo, 6, capriulo.X, capriulo.Y, capriulo.Z, NpcRadius);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1899",
                "Native7 'Get a map of the Thrax Genome.' 19643/19644, 4 'Kill Skeev!' 19514/19515, 5 'Download the Genome Map.' " +
                "19516/19517, 6 'hidden failure conditions objective' 19521/19522, 2 'Return to Zupa Eugin.' 19498/19499, 3 'Return " +
                "to Special Agent Capriulo.' 19500/19501, no counters; objectiveconversation 1899,7 package2371 variant1 body19651 " +
                "choice19652, variant27 body19647 choices 19648 battle, 19649 download; 1899,5 package2372 19518; 1899,2 2337 19519; " +
                "1899,3 2335 19520. Texts 19491-19495. Items class28662 (template121151), 28660 (121150). Name 6734 Skeev.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Genome_Sweet_Genome",
                "Eugin gives, Special Agent Capriulo rewards; requires Designer Genes; clone credit, Thrax hybrid; teleporter " +
                "114,140,-366, Skeev -136,200,-288; helped: refuse and battle or accept the download, else battle; the Bane terminal " +
                "nearby unlocks. TTH: the doorway 276,160,-370, the teleporter room, Skeev's room -124,200,-288. TR MMO DB level35, " +
                "0XP, 0 credits.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:CrucibleGenomeSweetGenomeV1",
                "The program at the accept; the tunnel pad 911 to the lab pad 912. A personal neutral Skeev 770061 (OwnerOnly) talks: " +
                "variant 27 when flag530003 is 26, else 1. Battle opens 4: the hostile Skeev 770062 attacks; dead, 5 opens. Accept " +
                "opens 5. The personal terminal 10519 (package2372) takes the program, gives the map, opens 2; Eugin takes the map, " +
                "3 opens; Capriulo's talk completes (HybridUnlocks: Thrax). 6 not written. Markers: pad, Skeev, terminal, Eugin, " +
                "Capriulo.", 0.3);

            mission.Enable(Scene());
        }

        /// <summary>The program at the accept; the player's own Skeev and terminal in the laboratory.</summary>
        public static MissionSceneDefinition Scene()
        {
            var helped = new FlagRequirement(VaccineFlag, Surrendered);
            var scene = new MissionSceneDefinition
            {
                Script = "data.sequence",
                MapContextId = CrucibleMissionDataV1.MapContextId,
                Items = new()
                {
                    new MissionItemBinding(ProgramKey, ProgramTemplate, MissionItemScope.AssignmentIssued, 1,
                        MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove),
                    new MissionItemBinding(MapKey, MapTemplate, MissionItemScope.AssignmentIssued, 1,
                        MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove)
                },
                AcceptanceItems = new() { new IssueMissionItemIntent("issue-penumbra-program", MissionId, ProgramKey, ProgramTemplate, 1) },
                Dialogue = new List<MissionDialogueTopicDefinition>
                {
                    // Variant 1 has one choice ("Prepare to do battle with Skeev."); a choice topic needs two, so it is a completion
                    // topic, its text 19646 (the client's own completion text of variant 1).
                    new(GetMap, SkeevPackage, IgnoredVariant, MissionDialogueKind.Completion, transitionId: Battle,
                        requirement: new NotRequirement(helped)),
                    new(GetMap, SkeevPackage, HelpedVariant, MissionDialogueKind.Choice,
                        choices: new Dictionary<int, uint> { [1] = HelpedBattle, [2] = Accept }, requirement: helped),
                    new(Download, TerminalPackage, 1, transitionId: 1)
                },
                DefeatSequences = new() { [FighterRole] = SkeevDead }
            };
            scene.Actors[TalkerRole] = new SceneActorDefinition(TalkerRole, SceneActorKind.Creature, CrucibleHybridArcNpcsV1.SkeevTalker,
                SkeevAt, Orientation: SkeevOrientation, GameplayPolicy: new ActorGameplayPolicy { Invulnerable = true }, OwnerOnly: true);
            scene.Actors[FighterRole] = new SceneActorDefinition(FighterRole, SceneActorKind.Creature, CrucibleHybridArcNpcsV1.SkeevFighter,
                SkeevAt, Orientation: SkeevOrientation, GameplayPolicy: PlateauSilentButDeadlyV1.Hostile(), OwnerOnly: true);
            scene.Actors[TerminalRole] = new SceneActorDefinition(TerminalRole, SceneActorKind.Object, TerminalClass, TerminalAt,
                Conversation: new SceneObjectConversation(MissionId, Download, TerminalPackage, Download));
            scene.Sequences[0] = new SceneSequenceDefinition
            {
                World =
                {
                    new EnsureActorIntent($"create-{TalkerRole}", TalkerRole),
                    new EnsureActorIntent($"create-{TerminalRole}", TerminalRole)
                }
            };
            scene.Sequences[SkeevFights] = new SceneSequenceDefinition
            {
                World =
                {
                    new RemoveActorIntent($"{TalkerRole}-goes", TalkerRole),
                    new EnsureActorIntent($"{FighterRole}-comes", FighterRole),
                    QuestSceneIntents.Attack($"{FighterRole}-attacks", FighterRole)
                }
            };
            scene.Sequences[SkeevDead] = new SceneSequenceDefinition { Signals = { new SceneMissionSignal(MissionId, SkeevDead, 1) } };
            return scene;
        }

        public static void Down(MigrationBuilder migration) => CrucibleMissionDataV1.Remove(migration, MissionId);
    }
}
