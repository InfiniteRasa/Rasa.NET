using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Crucible
{
    /// <summary>
    /// Who's In Charge Here? (native mission 1215), Ligo Crucible: Larai Zupa Madias gives the player a Viddia hacking program
    /// to hack a Bane computer at Khulago Base and find the name of the base's leader.
    ///
    /// Native: texts 9574-9578 (missionconversation): log "Enter the Bane structure at the heart of Khalago Base and access the
    /// mainframe within to identify the leader of the base.", offer "... That building, with the glow of green? I am thinking
    /// perhaps there are computers inside ...", turn-in "So, you have found the name of the leader of the crusty devils? ...",
    /// reminder "Who is leading the enemy? A name, my friend. I must have a name!"; objective 1 "Hack the Bane Computer."
    /// 9797/9798, objective 2 "Use the Hacked Computer." 9800/9801, no counters (missionobjective); objectiveconversation
    /// (1215,1) package 1453 text 9799 "Viddia hacking program uploading... Access granted.", (1215,2) package 1453 text 9802
    /// "... Kuhlago Base Commander: Maxxum Ugoretz." Item class 25313 "Viddia Syndicate Hacking Program" (template 96429).
    /// The client map's console static ArchBaneObjConsoleV03V03 at (360.7, 175.6, -236.2), in a Bane industrial tunnel piece
    /// at Khulago Base.
    ///
    /// Sourced: TaRapedia Who's In Charge Here?: giver and reward giver Larai Zupa Madias; requires Iron Giants; follow-up The
    /// Big Kahuna; 85,000 XP, 6,000 credits; one of Vextronics Electric Injector Gun, Vextronics Laser Rifle, Titan Cipher
    /// Tool, Teleract RPG Launcher; the terminal "Bane CommLink Terminal". TR MMO DB: level 35; the objectives and "Return to
    /// Larai Zupa Madias".
    ///
    /// Reconstructed (ESTIMATE):
    ///  - The program issued at the accept. The computer a personal talking Bane CommLink Terminal, class 10519 as the
    ///    Bane consoles of Poison Control (1368, PoolsPoisonControlV1), Bane Investigation (PoolsBaneInvestigationV1) and Data
    ///    Disaster (MarshesDataDisasterV1); the real client showed it on the ground as "Bane CommLink Terminal" and opened its
    ///    talk (1368, 2026-10-28, NOTES-playtest.md), with package 1453 at the mouth of the tunnel piece that holds the client's console, (390, 174.28, -236): the tunnel's floor is a
    ///    navmesh island 1.4 m above the ground (probe 2026-11-07), so the server cannot walk a player to the console.
    ///  - One object binding serves one objective (MissionObjectConversations), so the terminal is two actors in one place:
    ///    the first's talk (objective 1) takes the program and opens objective 2, and its scenario puts the hacked terminal in
    ///    its place; the hacked one's talk (objective 2) completes the mission. The turn-in at Madias.
    ///  - A marker (r10) at the terminal for each objective.
    ///  - The items: the lowest template id of each kind whose Uncommon band holds level 35 (InjectionGun_Electric 15582,
    ///    Rifle_Laser 14691, RocketLauncher_Physical 49136), and Tool_Avatar_Cipher_35 3853 (quality 2: the client has no
    ///    Uncommon cipher tool); the RPG's name gives no damage kind: physical. One to choose.
    /// </summary>
    public static class CrucibleWhosInChargeHereV1
    {
        public const uint MissionId = 1215;
        public const uint Level = 35;
        public const uint Madias = CrucibleKardashAndPrometheusNpcsV1.Madias;
        public const uint Hack = 1, Use = 2;
        public const uint Hacked = 1;                   // the scenario (and scene sequence) that puts the hacked terminal in place

        public const uint ProgramTemplate = 96429, ProgramClass = 25313;
        public const string ProgramKey = "viddia-hacking-program";
        public const uint TerminalClass = 10519;        // UsableNPCBaneKeyboard, "Bane CommLink Terminal"
        public const uint TerminalPackage = 1453;       // objectiveconversation 1215,1 and 1215,2
        public const string TerminalRole = "khulago-bane-terminal";
        public const string HackedRole = "khulago-bane-terminal-hacked";
        public static readonly ScenePosition TerminalAt = new(390f, 174.28f, -236f);
        public const double TerminalOrientation = 0;
        public const double MarkerRadius = 10;

        public const uint Experience = 85000;
        public const uint Credits = 6000;
        public const uint ElectricInjectorGun = 15582;  // InjectionGun_Electric
        public const uint LaserRifle = 14691;           // Rifle_Laser
        public const uint CipherTool = 3853;            // Tool_Avatar_Cipher_35
        public const uint RpgLauncher = 49136;          // RocketLauncher_Physical

        public static void Up(MigrationBuilder migration)
        {
            var mission = new CrucibleMissionDataV1(migration, MissionId, "Who's In Charge Here?", 9574, Madias, Madias, Level);
            mission.Reward(Experience, Credits, new[] { ElectricInjectorGun, LaserRifle, CipherTool, RpgLauncher });
            mission.Prerequisite(CrucibleIronGiantsV1.MissionId);
            mission.Scenario(Hacked, "The Bane terminal hacked: the hacked terminal in its place", 0);

            mission.Objective(Hack, 9797, 9798, 0, MissionObjectiveState.Incomplete);
            mission.Objective(Use, 9800, 9801, 1, MissionObjectiveState.Inactive);

            mission.Transition(Hack);
            mission.Conversation(Hack, TerminalPackage);
            mission.Action(Hack, 1, MissionActionKind.ConsumeMissionItem,
                itemIntent: new ConsumeMissionItemIntent("viddia-program-uploaded", MissionId, ProgramKey, 1, MissionItemScope.AssignmentIssued));
            CrucibleCalendarGirlV1.Open(mission, Hack, 1, 2, Use);
            mission.Action(Hack, 4, MissionActionKind.StartScenario, scenario: Hacked);

            mission.Transition(Use);
            mission.Conversation(Use, TerminalPackage);
            mission.Action(Use, 1, MissionActionKind.GrantReward, reward: 1);

            mission.Indicator(Hack, 1, TerminalAt.X, TerminalAt.Y, TerminalAt.Z, MarkerRadius);
            mission.Indicator(Use, 1, TerminalAt.X, TerminalAt.Y, TerminalAt.Z, MarkerRadius);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1215",
                "Native1 'Hack the Bane Computer.' 9797/9798, 2 'Use the Hacked Computer.' 9800/9801, no counters; " +
                "objectiveconversation 1215,1 and 1215,2 package1453 (9799 upload, 9802 'Kuhlago Base Commander: Maxxum Ugoretz'). " +
                "Texts 9574-9578. Item class25313 Viddia Syndicate Hacking Program (template96429). Console static " +
                "ArchBaneObjConsoleV03V03 (360.7,175.6,-236.2).");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Who%27s_In_Charge_Here%3F",
                "Giver/reward giver Larai Zupa Madias; requires Iron Giants; follow-up The Big Kahuna; 85000XP, 6000 credits; one of " +
                "Vextronics Electric Injector Gun, Vextronics Laser Rifle, Titan Cipher Tool, Teleract RPG Launcher; Bane CommLink " +
                "Terminal. TR MMO DB level35, 'Return to Larai Zupa Madias'.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:CrucibleWhosInChargeHereV1",
                "The program issued at the accept (a mission item: no destroy, sale or trade); a personal talking terminal 10519 " +
                "(the Bane console of 1368, PoolsPoisonControlV1, seen in the real client 2026-10-28) (package1453) at the mouth of the console's " +
                "tunnel, (390,174.28,-236) (its floor a navmesh island); its talk takes the program and opens 2, and a hacked " +
                "terminal (2's binding) takes its place; that talk completes. Markers r10. Items 15582, 14691, 3853 (quality 2), " +
                "49136, one to choose.", 0.3);

            mission.Enable(Scene());
        }

        /// <summary>The player's own Bane terminal at Khulago Base, the hacked one after the upload, and the program issued at the accept.</summary>
        public static MissionSceneDefinition Scene()
        {
            var scene = new MissionSceneDefinition
            {
                Script = "data.sequence",
                MapContextId = CrucibleMissionDataV1.MapContextId,
                Dialogue = new List<MissionDialogueTopicDefinition>
                {
                    new(Hack, TerminalPackage, 1, transitionId: 1),
                    new(Use, TerminalPackage, 1, transitionId: 1)
                },
                Items = new()
                {
                    new MissionItemBinding(ProgramKey, ProgramTemplate, MissionItemScope.AssignmentIssued, 1,
                        MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove)
                },
                AcceptanceItems = new() { new IssueMissionItemIntent("issue-viddia-program", MissionId, ProgramKey, ProgramTemplate, 1) }
            };
            scene.Actors[TerminalRole] = new SceneActorDefinition(TerminalRole, SceneActorKind.Object, TerminalClass, TerminalAt,
                Orientation: TerminalOrientation, Conversation: new SceneObjectConversation(MissionId, Hack, TerminalPackage, Hack));
            scene.Actors[HackedRole] = new SceneActorDefinition(HackedRole, SceneActorKind.Object, TerminalClass, TerminalAt,
                Orientation: TerminalOrientation, Conversation: new SceneObjectConversation(MissionId, Use, TerminalPackage, Use));
            scene.Sequences[0] = new SceneSequenceDefinition { World = { new EnsureActorIntent($"create-{TerminalRole}", TerminalRole) } };
            scene.Sequences[Hacked] = new SceneSequenceDefinition
            {
                World =
                {
                    new RemoveActorIntent($"{TerminalRole}-hacked", TerminalRole),
                    new EnsureActorIntent($"create-{HackedRole}", HackedRole)
                }
            };
            return scene;
        }

        public static void Down(MigrationBuilder migration) => CrucibleMissionDataV1.Remove(migration, MissionId);
    }
}
