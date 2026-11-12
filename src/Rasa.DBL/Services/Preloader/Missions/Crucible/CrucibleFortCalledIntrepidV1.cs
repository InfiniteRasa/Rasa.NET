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
    /// A Fort Called Intrepid (native mission 1150), Ligo Crucible: Captain DeLisi sends the player into the ruins of Fort
    /// Intrepid to upload the "Black Ice" hacking program at a working data terminal, so the fort's database goes to the
    /// outpost and is wiped before the Bane read it.
    ///
    /// Native: texts 8414-8417, 8418 (missionconversation): log "Find an intact data terminal connection inside the ruins of
    /// Fort Intrepid and upload the "Black Ice" hacking program ... return to Captain DeLisi at Outpost Intrepid.", offer
    /// "This datablade contains a Penumbra ...", turn-in "Worked like a charm!"; objective 1 "Upload the Hacking Program."
    /// 8574/8575, no counter (missionobjective); objectiveconversation (1150,1) package 1245 text 8576 "Uploading:
    /// PENUMBRA_BLACK_ICE.EXE ... WIPE COMPLETE" (a terminal's screen). Item class 24688 "Black Ice" Hacking Program
    /// (template 50297).
    ///
    /// Sourced: TaRapedia A Fort Called Intrepid: giver and reward giver Captain DeLisi (Outpost Intrepid); requires The Last
    /// Man Standing; follow-ups The Wire and Funeral Derge; 75,000 XP, 5,700 credits; one of Pathogex Stealth Armor Vest
    /// (icon 3), Olympia Graviton Armor Helmet (4), Luminar Hazmat Armor Boots (3), CryoGen Motor Assist Armor Vest (7);
    /// walkthrough "Take the right path that leads north and then just head into the ruins ... There you'll find the Data
    /// Terminal, press 'T' to activate".
    ///
    /// Reconstructed (ESTIMATE):
    ///  - Level 40 (no source: the level of 1142 and 1143, TR MMO DB).
    ///  - The terminal: a personal talking AFS monitor (11143 UsableNPCHumMonitorV01, physicalentityclassname "AFS Data
    ///    Terminal") with package 1245, on the floor by the ruined headquarters (client map ArchHumFortHeadqtrsV01Destroyed
    ///    at 790, 174, 664): (809.74, 175.07, 676.74), 13 m outside the Fithik pool 531113's circle (probe of 2026-11-07).
    ///    Its talk completes 1 and takes the program.
    ///  - The items: the lowest Uncommon template of each kind and icon number in the 40-44 band (the mission's level; the
    ///    client has no reward block of these four): Stealth V03 Vest 39985, Graviton V04 Helmet 25119, Hazmat V03 Boots
    ///    21100 (39-43), Motor Assist V07 Vest 31547 (38-42); one to choose. A marker at the terminal.
    /// </summary>
    public static class CrucibleFortCalledIntrepidV1
    {
        public const uint MissionId = 1150;
        public const uint Level = 40;
        public const uint DeLisi = CrucibleOutpostIntrepidNpcsV1.DeLisi;
        public const uint Upload = 1;

        public const uint ProgramTemplate = 50297, ProgramClass = 24688;
        public const string ProgramKey = "black-ice-program";
        public const uint TerminalClass = 11143;        // UsableNPCHumMonitorV01, "AFS Data Terminal"
        public const uint TerminalPackage = 1245;       // objectiveconversation 1150,1
        public const string TerminalRole = "fort-intrepid-data-terminal";
        public static readonly ScenePosition TerminalAt = new(809.74f, 175.07f, 676.74f);
        public const double TerminalOrientation = 0;
        public const double MarkerRadius = 10;

        public const uint Experience = 75000;
        public const uint Credits = 5700;
        public const uint StealthVest = 39985;          // Armor_T3_Stealth_V03_UNC_Vest_40_to_44
        public const uint GravitonHelmet = 25119;       // Armor_T3_Graviton_V04_UNC_Helmet_40_to_44
        public const uint HazmatBoots = 21100;          // Armor_T2_Hazmat_V03_UNC_Boots_39_to_43
        public const uint MotorAssistVest = 31547;      // Armor_T1_MotorAssist_V07_UNC_Vest_38_to_42

        public static void Up(MigrationBuilder migration)
        {
            var mission = new CrucibleMissionDataV1(migration, MissionId, "A Fort Called Intrepid", 8414, DeLisi, DeLisi, Level);
            mission.Reward(Experience, Credits, new[] { StealthVest, GravitonHelmet, HazmatBoots, MotorAssistVest });
            mission.Prerequisite(CrucibleLastMenStandingV1.MissionId);

            mission.Objective(Upload, 8574, 8575, 0, MissionObjectiveState.Incomplete);
            mission.Transition(Upload);
            mission.Conversation(Upload, TerminalPackage);
            mission.Action(Upload, 1, MissionActionKind.ConsumeMissionItem,
                itemIntent: new ConsumeMissionItemIntent("black-ice-uploaded", MissionId, ProgramKey, 1, MissionItemScope.AssignmentIssued));
            mission.Action(Upload, 2, MissionActionKind.GrantReward, reward: 1);
            mission.Indicator(Upload, 1, TerminalAt.X, TerminalAt.Y, TerminalAt.Z, MarkerRadius);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1150",
                "Native1 'Upload the Hacking Program.' 8574/8575, no counter; objectiveconversation 1150,1 package1245 text8576 " +
                "(the terminal's upload screen). Texts 8414-8418. Item class24688 'Black Ice' Hacking Program (template50297). " +
                "Class 11143 UsableNPCHumMonitorV01 'AFS Data Terminal'.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/A_Fort_Called_Intrepid",
                "Giver/reward giver Captain DeLisi (Outpost Intrepid); requires The Last Man Standing; follow-ups The Wire, Funeral " +
                "Derge; 75000XP, 5700 credits; one of Pathogex Stealth Armor Vest 3, Olympia Graviton Armor Helmet 4, Luminar Hazmat " +
                "Armor Boots 3, CryoGen Motor Assist Armor Vest 7; the Data Terminal in the ruins, 'T' to activate.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:CrucibleFortCalledIntrepidV1",
                "Level40 estimated. The program issued at the accept; a personal talking monitor 11143 (package1245) by the ruined " +
                "headquarters at (809.74,175.07,676.74); its talk completes 1 and takes the program. Items 39985, 25119, 21100, " +
                "31547 (lowest Uncommon of each kind and icon, band of level 40), one to choose. A marker at the terminal.", 0.3);

            mission.Enable(Scene());
        }

        /// <summary>The player's own data terminal in the ruins, and the program issued at the accept.</summary>
        public static MissionSceneDefinition Scene()
        {
            var scene = new MissionSceneDefinition
            {
                Script = "data.sequence",
                MapContextId = CrucibleMissionDataV1.MapContextId,
                Dialogue = new List<MissionDialogueTopicDefinition> { new(Upload, TerminalPackage, 1, transitionId: 1) },
                Items = new()
                {
                    new MissionItemBinding(ProgramKey, ProgramTemplate, MissionItemScope.AssignmentIssued, 1,
                        MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove)
                },
                AcceptanceItems = new() { new IssueMissionItemIntent("issue-black-ice", MissionId, ProgramKey, ProgramTemplate, 1) }
            };
            scene.Actors[TerminalRole] = new SceneActorDefinition(TerminalRole, SceneActorKind.Object, TerminalClass, TerminalAt,
                Orientation: TerminalOrientation, Conversation: new SceneObjectConversation(MissionId, Upload, TerminalPackage, Upload));
            scene.Sequences[0] = new SceneSequenceDefinition { World = { new EnsureActorIntent($"create-{TerminalRole}", TerminalRole) } };
            return scene;
        }

        public static void Down(MigrationBuilder migration) => CrucibleMissionDataV1.Remove(migration, MissionId);
    }
}
