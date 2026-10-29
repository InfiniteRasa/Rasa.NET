using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Runtime;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Pools
{
    /// <summary>
    /// Spy Networking (native mission 1331), Valverde Pools: Amee Corman sends the player to the East Listening Post to
    /// steal the AFS's data on the Thrax Barracks' poison canisters from Corporal Sheen's console, once she is out of the
    /// way; Amee radios in to end it.
    ///
    /// Native: texts 10907-10911 (missionconversation): log "The command console is located inside the East Listening
    /// Post.", offer "Your AFS have set up a little spy post nearby ... I want you to go inside and steal that
    /// information", turn-in "This information better be accurate or I will personally see to your death."; objective 25
    /// "Steal Data" 11615/11616 counter 11617 "Tatical Data Stolen"; hidden native objectives 4 "Clear Flags", 5 "Choice
    /// 1", 7 "Choice 2", 11/23/24 "Choice 3 ...", 12 and 15-22 "Data ...", 14 "Counters" (missionobjective). The console's
    /// package 1530 (objectiveconversation): a root menu on 5 flag 1 (Poison / Thrax Barracks / Canisters), three menus on
    /// 7 flags 316-318, nine on 11/23/24 flags 319-321, 27 pages on 12 and 15-22 flags 322-324, each with "New Search"; on
    /// 18 flag 324 ("Poison Canisters") the choice "Download Poison Canister Tatical Data". 14 flag 315 on 1530: "Corporal
    /// Sheen is watching you too closely for you to secretly access the terminal." Sheen's package 1524 on 25 flag 315:
    /// "These consoles are for official use only." / "Send her to the first aid office." / "Goodbye.".
    ///
    /// Sourced: TaRapedia Spy Networking: giver Amee Corman, reward from Headquarters by radio; requires Retread Planning
    /// Part II; follow-up Taking The Poison (AFS) or Poison Control (Retreads); 65,000 XP, 5,400 credits; one of ChiTech
    /// Graviton Gloves [4] level 35, ChiTech Hazmat Helmet [3] 34, Wellcare Motor Assist Helmet [6] 33, Astra Reflective
    /// Helmet [3] 34; walkthrough: send Sheen to the infirmary, then on her terminal "1 - Thrax Barracks, 2 -
    /// Communication, and 3 - Poison Canisters, then select the option to download". TR MMO DB: level 32.
    ///
    /// Reconstructed (ESTIMATE; the user's choice of 2026-10-25: a personal Sheen stand-in walks away for this character,
    /// all choices and flags):
    ///  - A personal console (class 21087 UsableNPCHumWorkstationV01, on the post's workstation bench) is a menu
    ///    (SceneObjectConversation.Menu): it shows the page the character's page flag names. Each choice is a transition of
    ///    25 that only sets that flag and leaves 25 open (engine: a dialogue transition to Incomplete), and the console
    ///    shows the next page at once. "New Search" goes back to the root. The download completes 25.
    ///  - The pages' choices go to the pages of the next flag in turn (choice k of a flag-316..318 menu to flag 318+k on its
    ///    menu objective, and so on), as the client's rows are laid out; the three pages whose text sits in the choice slot
    ///    (16 flag 324, 17 flag 323, 22 flag 322) go back to the root by that slot. The client's unreached rows (12 and 15
    ///    flag 318, 16 flag 321) are left out.
    ///  - Sheen's stand-in (PoolsSpyNpcsV1) stands by the console; until she is sent away the console only says she is
    ///    watching. "Send her to the first aid office." walks her to the post's hospital and takes her out; "Goodbye." puts
    ///    the menu back at the root. Flags are the server's own (1331001 page, 1331002 Sheen gone), reset at each accept.
    ///  - Native objectives 4 and 14 (the client's own flag and counter bookkeeping) are left out: the server's flags do it.
    ///  - Rewards the lowest Uncommon templates of the lines, versions and bands TaRapedia names ([n] read as Vn, as One
    ///    Man's Trash's), one to choose. Turned in by radio (RadioCompletion). The marker on the console.
    /// </summary>
    public static class PoolsSpyNetworkingV1
    {
        public const uint MissionId = 1331;
        public const uint Level = 32;
        public const uint Amee = PoolsRetreadCampNpcsV1.Amee;
        public const uint Steal = 25, StealCounter = 11617;
        public const uint ConsolePackage = 1530, SheenPackage = PoolsSpyNpcsV1.StandInPackage;
        public const uint RootObjective = 5, RootFlag = 1, WatchingObjective = 14, SheenFlag = 315;
        public const uint PageFlag = 1331001, SheenGoneFlag = 1331002;
        public const uint Root = 1, Pages = 40;
        public const uint ToRootFirst = 41, Download = 44, SendAway = 45, Goodbye = 46;
        public const uint DownloadPage = 14 + 9 * 1 + 3 * 1 + 2;           // 18 flag 324, "Poison Canisters"
        public const uint Leaving = 2, Gone = 3, LeaveMs = 20000;

        public const uint ConsoleClass = 21087;         // UsableNPCHumWorkstationV01
        public static readonly ScenePosition ConsoleAt = new(985.5f, 656.6f, 347.1f);
        public static readonly ScenePosition SheenAt = new(984f, 656.6f, 352f);
        public static readonly ScenePosition HospitalAt = new(996f, 656.81f, 316f);      // East Listening Post Hospital 500294

        public const uint Experience = 65000;
        public const uint Credits = 5400;
        public const uint GravitonGloves = 24970;       // Armor_T3_Graviton_V04_UNC_Gloves_35_to_39
        public const uint HazmatHelmet = 21380;         // Armor_T2_Hazmat_V03_UNC_Helmet_34_to_38
        public const uint MotorAssistHelmet = 30454;    // Armor_T1_MotorAssist_V06_UNC_Helmet_33_to_37
        public const uint ReflectiveHelmet = 36618;     // Armor_T2_Reflective_V03_UNC_Helmet_34_to_38

        /// <summary>A console page: its code (the page flag's value), the native objective and flag of its text, the codes its choices go to.</summary>
        public sealed record Page(uint Code, uint DialogObjective, uint Flag, uint[] Choices);

        private static readonly uint[] Menus = { 11, 23, 24 };
        private static readonly uint[][] Leaves = { new uint[] { 12, 15, 16 }, new uint[] { 17, 18, 19 }, new uint[] { 20, 21, 22 } };

        /// <summary>The 40 pages: the root (1), three menus (2-4), nine menus (5-13), 27 leaves (14-40).</summary>
        public static IReadOnlyList<Page> AllPages()
        {
            var pages = new List<Page> { new(Root, RootObjective, RootFlag, new uint[] { 2, 3, 4 }) };
            for (uint i = 0; i < 3; i++)
                pages.Add(new(2 + i, 7, 316 + i, new[] { 5 + 3 * i, 6 + 3 * i, 7 + 3 * i }));
            for (uint i = 0; i < 3; i++)
                for (uint j = 0; j < 3; j++)
                {
                    var first = 14 + 9 * i + 3 * j;
                    pages.Add(new(5 + 3 * i + j, Menus[i], 319 + j, new[] { first, first + 1, first + 2 }));
                }
            for (uint i = 0; i < 3; i++)
                for (uint j = 0; j < 3; j++)
                    for (uint k = 0; k < 3; k++)
                    {
                        var code = 14 + 9 * i + 3 * j + k;
                        pages.Add(new(code, Leaves[i][j], 322 + k, code == DownloadPage ? new[] { Root, 0U } : new[] { Root, Root }));
                    }
            return pages;
        }

        /// <summary>The transition a choice to <paramref name="target"/> from a page of flag <paramref name="flag"/> takes (0: the download).</summary>
        public static uint TransitionTo(uint target, uint flag) =>
            target == 0 ? Download : target == Root ? ToRootFirst + (flag - 322) : target;

        public static void Up(MigrationBuilder migration)
        {
            var mission = new PoolsMissionDataV1(migration, MissionId, "Spy Networking", 10907, Amee, null, Level);
            mission.Reward(Experience, Credits, new[] { GravitonGloves, HazmatHelmet, MotorAssistHelmet, ReflectiveHelmet });
            mission.Prerequisite(PoolsRetreadPlanningPartTwoV1.MissionId);
            mission.RadioCompletion();
            mission.Scenario(Leaving, "Sheen goes to the hospital", 0);

            mission.Objective(Steal, 11615, 11616, 0, MissionObjectiveState.Incomplete, counterTextId: StealCounter);

            // The menu: a way to each page but the root, from its parent's flag; back to the root from each leaf flag.
            var pages = AllPages();
            var parentFlag = new Dictionary<uint, uint>();
            foreach (var page in pages)
                foreach (var child in page.Choices)
                    if (child > Root)
                        parentFlag[child] = page.Flag;
            for (var code = Root + 1; code <= Pages; code++)
            {
                mission.Transition(Steal, code, code, MissionObjectiveState.Incomplete);
                mission.Conversation(Steal, ConsolePackage, parentFlag[code], code);
                mission.Action(Steal, 1, MissionActionKind.SetPlayerFlag, playerFlag: PageFlag, playerFlagValue: code, transition: code);
            }
            for (var flag = 322U; flag <= 324; flag++)
            {
                var transition = ToRootFirst + (flag - 322);
                mission.Transition(Steal, transition, transition, MissionObjectiveState.Incomplete);
                mission.Conversation(Steal, ConsolePackage, flag, transition);
                mission.Action(Steal, 1, MissionActionKind.SetPlayerFlag, playerFlag: PageFlag, playerFlagValue: Root, transition: transition);
            }
            mission.Transition(Steal, Download, Download);
            mission.Conversation(Steal, ConsolePackage, 324, Download);
            mission.Action(Steal, 1, MissionActionKind.GrantReward, reward: 1, transition: Download);

            // Sheen: sent away, or goodbye.
            mission.Transition(Steal, SendAway, SendAway, MissionObjectiveState.Incomplete);
            mission.Conversation(Steal, SheenPackage, SheenFlag, SendAway);
            mission.Action(Steal, 1, MissionActionKind.SetPlayerFlag, playerFlag: SheenGoneFlag, playerFlagValue: 1, transition: SendAway);
            mission.Action(Steal, 2, MissionActionKind.StartScenario, scenario: Leaving, transition: SendAway);
            mission.Transition(Steal, Goodbye, Goodbye, MissionObjectiveState.Incomplete);
            mission.Conversation(Steal, SheenPackage, SheenFlag, Goodbye);
            mission.Action(Steal, 1, MissionActionKind.SetPlayerFlag, playerFlag: PageFlag, playerFlagValue: Root, transition: Goodbye);

            mission.Indicator(Steal, 1, ConsoleAt.X, ConsoleAt.Y, ConsoleAt.Z);

            var sheenGone = new FlagRequirement(SheenGoneFlag, 1);
            var dialogue = new List<MissionDialogueTopicDefinition>
            {
                new(Steal, SheenPackage, SheenFlag, MissionDialogueKind.Choice,
                    choices: new Dictionary<int, uint> { [1] = SendAway, [2] = Goodbye }, dialogObjectiveId: Steal,
                    requirement: new NotRequirement(sheenGone), sourceCreatureId: PoolsSpyNpcsV1.StandIn),
                new(Steal, ConsolePackage, SheenFlag, MissionDialogueKind.Ambient, dialogObjectiveId: WatchingObjective,
                    requirement: new NotRequirement(sheenGone))
            };
            foreach (var page in pages)
            {
                var choices = new Dictionary<int, uint>();
                for (var index = 0; index < page.Choices.Length; index++)
                    choices[index + 1] = TransitionTo(page.Choices[index], page.Flag);
                dialogue.Add(new(Steal, ConsolePackage, page.Flag, MissionDialogueKind.Choice, choices: choices,
                    dialogObjectiveId: page.DialogObjective,
                    requirement: new AllRequirements(new MissionRequirement[] { new FlagRequirement(PageFlag, page.Code), sheenGone })));
            }

            var scene = new MissionSceneDefinition { Script = "data.sequence", Dialogue = dialogue };
            scene.Routes["to-hospital"] = new SceneRoute("to-hospital", new[] { new SceneWaypoint(HospitalAt) }, Speed: 3f);
            scene.Actors["console"] = new SceneActorDefinition("console", SceneActorKind.Object, ConsoleClass, ConsoleAt,
                Orientation: 1.5707963268,
                Conversation: new SceneObjectConversation(MissionId, Steal, ConsolePackage, RootObjective, RootFlag,
                    MissionDialogueKind.Choice, Menu: true));
            scene.Actors["sheen"] = new SceneActorDefinition("sheen", SceneActorKind.Creature, PoolsSpyNpcsV1.StandIn, SheenAt,
                Orientation: 3.1415926536);
            scene.Sequences[0] = new SceneSequenceDefinition
            {
                World = { new EnsureActorIntent("create-console", "console"), new EnsureActorIntent("create-sheen", "sheen") },
                Character =
                {
                    new SetCharacterFlagIntent("menu-at-root", PageFlag, Root),
                    new SetCharacterFlagIntent("sheen-at-her-console", SheenGoneFlag, null)
                }
            };
            scene.Sequences[Leaving] = new SceneSequenceDefinition
            {
                World = { new RunRouteIntent("sheen-to-hospital", "sheen", "to-hospital") },
                Timers = new() { new SequenceTimer("sheen-at-hospital", LeaveMs, Gone) }
            };
            scene.Sequences[Gone] = new SceneSequenceDefinition { World = { new RemoveActorIntent("sheen-gone", "sheen") } };

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1331",
                "Native25 'Steal Data' 11615/11616 counter11617; hidden 4, 5, 7, 11-24 (choices, data pages, flags, counters). " +
                "objectiveconversation package1530: 5 flag1, 7 flags316-318, 11/23/24 flags319-321, 12/15-22 flags322-324, 18 " +
                "flag324 download, 14 flag315 'Sheen is watching'; package1524 25 flag315 (send away / goodbye). Texts 10907-10911.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Spy_Networking",
                "Giver Amee Corman, reward Headquarters (radio); requires Retread Planning Part II; follow-ups Taking The Poison / " +
                "Poison Control; 65000XP, 5400 credits; one of ChiTech Graviton Gloves[4]35, ChiTech Hazmat Helmet[3]34, Wellcare " +
                "Motor Assist Helmet[6]33, Astra Reflective Helmet[3]34; send Sheen to the infirmary, Thrax Barracks - " +
                "Communication - Poison Canisters - download. TR MMO DB level32.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:PoolsSpyNetworkingV1",
                "Personal console (21087) as a menu on server flags 1331001/1331002 (engine: menu objects, choices that keep the " +
                "objective open); personal Sheen stand-in 760212 walks to the hospital (user). Items 24970, 21380, 30454, 36618.", 0.3);

            mission.Enable(scene);
        }

        public static void Down(MigrationBuilder migration) => PoolsMissionDataV1.Remove(migration, MissionId);
    }
}
