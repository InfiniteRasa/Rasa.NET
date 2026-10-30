using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Plateau
{
    /// <summary>
    /// Mind Bender (native mission 905), Plateau: Special Agent Perdu gives the player a prototype implant
    /// extractor and sends them to four mind-controlled soldiers between Fort Defiance and Trinity Bridge;
    /// the player takes each one's implant and brings the four, and the extractor, back to Perdu.
    ///
    /// Native: name/log/offer/turn-in/reminder texts 4804-4807 and 6395 (missionconversation); objectives
    /// (missionobjective, no counters) and the soldiers' lines (objectiveconversation, flag 1):
    ///  - find: 11 "Find Lt. Jackson." 8233/8234 (package 633, "What are you doing? Who are you?" 8235),
    ///    12 Hoyle 8240/8241 (635, 8242), 13 Tillman 8243/8244 (634, 8245), 14 Harris 8246/8247 (637, 8248);
    ///  - extract: 3 "Extract Jackson's implant." 4810/6397 (633, the second line, "Leave me alone!" 19820),
    ///    8 Hoyle 4812/6398 (635, 19821), 9 Tillman 4814/6399 (634, 19822), 10 Harris 4816/6400 (637, 19823).
    /// Items: Mind Control Chip Extractor class 21027 (template 42409, a weapon); Lieutenant Jackson's,
    /// Major Hoyle's, Private Tillman's, Sergeant Harris' Implant classes 24549-24552 (templates 50179-50182).
    /// Text 4807 (turn-in): "I can't reward you until you give back that prototype extractor."
    ///
    /// Sourced (TaRapedia Mind Bender, stub): giver and receiver Agent Perdu, level "32 (possibly less)",
    /// requirement Clean Slate, follow-up Photo Ops; 45,000 XP, 4,800 credits, "Equipment"; each soldier's
    /// implant 0/1; "The soldiers will automatically return to base after you remove their implants. There
    /// is no escort portion"; "you must have possession of the extractor when you turn in the mission".
    ///
    /// Reconstructed (ESTIMATE): level 32. The extractor is given with the mission and taken back at
    /// turn-in. Each soldier's first talk finds him and opens his extraction; Perdu takes the four. The
    /// soldiers stay where they are. "Equipment" read as the four Uncommon V04 gloves of the 30-34 band,
    /// one of each armor type, no module.
    ///
    /// The extraction: <see cref="Up"/> wrote it as a second talk, which the client has no line for (only
    /// the REMINDER, type 2: "ERROR: 23: Missing translation for objectiveconversation ID 905/3/633/1/1",
    /// to-fix #49). The migration Plateau_mind_bender_extract_on_find completes it with the find, so one
    /// talk gives the implant (<see cref="ExtractOnFind"/>). Retail extracted with a use of the extractor
    /// on the soldier (class 21027 attacks with TOOL_HEALING_DISC 147 argument 3: actionArguments 4000 ms
    /// windup, 5 m, no damage); here a mission-owned item cannot be drawn (MissionItemProtection), so the
    /// extractor is only given and taken back (the user's choice of 2026-10-30).
    /// </summary>
    public static class PlateauMindBenderV1
    {
        public const uint MissionId = 905;
        public const uint Level = 32;
        public const uint Perdu = PlateauPerduNpcsV1.Perdu;
        public const uint ExtractorTemplate = 42409, ExtractorClass = 21027;
        public const string ExtractorKey = "implant-extractor";

        public const uint Experience = 45000;
        public const uint Credits = 4800;
        public const uint BioGloves = 46011, GravitonGloves = 24950, MechGloves = 34076, StealthGloves = 40203;

        /// <summary>Per soldier (PlateauPerduNpcsV1.Implanted order): find and extract objectives and texts, implant.</summary>
        public static readonly (uint Find, uint FindName, uint FindBody, uint Extract, uint ExtractName, uint ExtractBody,
            uint Implant, uint ImplantClass, string Key)[] Soldiers =
        {
            (11, 8233, 8234, 3, 4810, 6397, 50179, 24549, "jackson-implant"),
            (12, 8240, 8241, 8, 4812, 6398, 50180, 24550, "hoyle-implant"),
            (13, 8243, 8244, 9, 4814, 6399, 50181, 24551, "tillman-implant"),
            (14, 8246, 8247, 10, 4816, 6400, 50182, 24552, "harris-implant"),
        };

        public static void Up(MigrationBuilder migration)
        {
            var mission = new PlateauMissionDataV1(migration, MissionId, "Mind Bender", 4804, Perdu, Perdu, Level);
            mission.Reward(Experience, Credits, new[] { BioGloves, GravitonGloves, MechGloves, StealthGloves });
            mission.Prerequisite(PlateauCleanSlateV1.MissionId);

            var items = new List<MissionItemBinding>
            {
                new(ExtractorKey, ExtractorTemplate, MissionItemScope.AssignmentIssued, 1,
                    MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove,
                    MissionItemCleanupDisposition.Remove, TurnInQuantity: 1)
            };
            for (var index = 0; index < Soldiers.Length; index++)
            {
                var soldier = Soldiers[index];
                var npc = PlateauPerduNpcsV1.Implanted[index];
                mission.Objective(soldier.Find, soldier.FindName, soldier.FindBody, (uint)index, MissionObjectiveState.Incomplete);
                mission.Objective(soldier.Extract, soldier.ExtractName, soldier.ExtractBody, (uint)(index + Soldiers.Length),
                    MissionObjectiveState.Inactive);

                // Find: his first talk opens his extraction.
                mission.Transition(soldier.Find);
                mission.Conversation(soldier.Find, npc.Package);
                mission.Action(soldier.Find, 1, MissionActionKind.RevealObjective, targetObjective: soldier.Extract);
                mission.Action(soldier.Find, 2, MissionActionKind.ActivateObjective, targetObjective: soldier.Extract,
                    state: MissionObjectiveState.Incomplete);

                // Extract: his second talk gives the implant.
                mission.Transition(soldier.Extract);
                mission.Conversation(soldier.Extract, npc.Package);
                mission.Action(soldier.Extract, 1, MissionActionKind.IssueMissionItem,
                    itemIntent: new IssueMissionItemIntent($"extract-{soldier.Key}", MissionId, soldier.Key, soldier.Implant, 1));

                mission.Indicator(soldier.Find, (uint)index + 1, npc.At.X, npc.At.Y, npc.At.Z);
                mission.Indicator(soldier.Extract, (uint)index + 5, npc.At.X, npc.At.Y, npc.At.Z);

                items.Add(new MissionItemBinding(soldier.Key, soldier.Implant, MissionItemScope.AssignmentIssued, 1,
                    MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove,
                    MissionItemCleanupDisposition.Remove, TurnInQuantity: 1));
            }

            // The one GrantReward names the turn-in package; Perdu pays when he takes the mission back.
            mission.Action(Soldiers[0].Extract, 2, MissionActionKind.GrantReward, reward: 1);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/905",
                "Native find 11-14 (8233/8234, 8240/8241, 8243/8244, 8246/8247), extract 3, 8, 9, 10 (4810/6397, 4812/6398, " +
                "4814/6399, 4816/6400), no counters; objectiveconversation 905,11/3 package633, 12/8 635, 13/9 634, 14/10 637 " +
                "(texts 8235/19820, 8242/19821, 8245/19822, 8248/19823). Texts 4804-4807, 6395. Extractor class21027/template42409; " +
                "implants classes24549-24552/templates50179-50182. Soldiers' names 6764, 6768, 6767, 6766.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Mind_Bender",
                "Stub. Giver/receiver Agent Perdu; level '32 (possibly less)'; requirement Clean Slate; follow-up Photo Ops; 45000XP, " +
                "4800 credits, 'Equipment'; the soldiers return to base after the extraction; the extractor needed at turn-in.", 0.4);
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:PlateauMindBenderV1",
                "Level32. Extractor42409 given with the mission, taken at turn-in. First talk finds, second talk gives the implant " +
                "(no extractor shot); Perdu takes the four. Soldiers stay put; their places are estimates (PlateauPerduNpcsV1). " +
                "Equipment read as Uncommon V04 gloves 30-34: 46011, 24950, 34076, 40203.", 0.3);

            mission.Enable(new MissionSceneDefinition
            {
                Items = items,
                AcceptanceItems = new()
                {
                    new IssueMissionItemIntent("issue-extractor", MissionId, ExtractorKey, ExtractorTemplate, 1)
                }
            });
        }

        public static void Down(MigrationBuilder migration) => PlateauMissionDataV1.Remove(migration, MissionId);

        /// <summary>
        /// Each extraction completes when its find does (an ObjectiveStateReached trigger), in place of the
        /// soldier's second talk; the extraction's own action still gives the implant.
        /// </summary>
        public static void ExtractOnFind(MigrationBuilder migration)
        {
            DeleteExtractTriggers(migration, MissionTriggerKind.Conversation);
            var mission = PlateauMissionDataV1.Existing(migration, MissionId);
            foreach (var soldier in Soldiers)
                mission.ObjectiveState(soldier.Extract, soldier.Find, MissionObjectiveState.Completed);
        }

        /// <summary>Back to <see cref="Up"/>'s second talk.</summary>
        public static void ExtractByTalk(MigrationBuilder migration)
        {
            DeleteExtractTriggers(migration, MissionTriggerKind.ObjectiveState);
            var mission = PlateauMissionDataV1.Existing(migration, MissionId);
            for (var index = 0; index < Soldiers.Length; index++)
                mission.Conversation(Soldiers[index].Extract, PlateauPerduNpcsV1.Implanted[index].Package);
        }

        private static void DeleteExtractTriggers(MigrationBuilder migration, MissionTriggerKind kind) =>
            migration.Sql($"DELETE FROM mission_trigger WHERE mission_id = {MissionId} AND content_revision = " +
                $"'{PlateauMissionDataV1.Revision}' AND kind = {(byte)kind} AND objective_id IN " +
                $"({string.Join(", ", Soldiers.Select(soldier => soldier.Extract))});");
    }
}
