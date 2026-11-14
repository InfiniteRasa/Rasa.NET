using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Crucible
{
    /// <summary>
    /// The Wire (native mission 1145), Ligo Crucible: Captain DeLisi sends the player, wearing an audio surveillance bug,
    /// to meet Recon Sergeant Mullins at Convict Pass and then the Grupas of the four Brann syndicates at Staal.
    ///
    /// Native: texts 8386-8389, 8390 (missionconversation): log "Inflitrate all four Brann Syndicates "Grupas" at Staal,
    /// question them and record the conversation, but don't get caught wearing a wire! To get to Staal, rendezvous with
    /// Recon Sergeant Mullins at Convict Pass, then travel through the pass to reach the facility.", turn-in "That's all the
    /// info we need, soldier."; objectives (missionobjective) 6 "Rendezvous with Mullins" 9997/9998, 1 "Find the Larai
    /// Gruppa." 8647/8648, 2 "Find the Viddia Gruppa." 8650/8651, 3 "Find the Top Khana Gruppa." 8653/8654, 5 "Find the
    /// Labbna Gruppa." 8662/8663 (no 4), no counters; objectiveconversation (1145,1) package 1225 Vitto 8649, (1145,2) 1227
    /// Donal 8659, (1145,3) 1229 Munit 8660, (1145,5) 1231 Rigs 8664, (1145,6) 1454 Mullins 9999. Item class 24715 Audio
    /// Surveillance Bug (template 50305).
    ///
    /// Sourced: TaRapedia The Wire: giver Captain DeLisi (Outpost Intrepid); requires A Fort Called Intrepid; follow-up The
    /// Boss Of All Bosses; 75,000 XP, 5,700 credits; one of Voltrox Mech Armor Legs (icon 3), Olympia Hazmat Armor Legs (2),
    /// Teleract Graviton Armor Boots (1), Hellstrom Motor Assist Armor Boots (6); "Convict Pass, due west of the fort.
    /// Recon Sergeant Mullins is stationed near the entrance."
    ///
    /// Reconstructed (ESTIMATE):
    ///  - Level 40 (no source: the level of 1142 and 1143). Receiver DeLisi (the turn-in text; TaRapedia names no reward
    ///    giver).
    ///  - The bug is issued at the accept and goes at the turn-in. "Don't get caught wearing a wire" has no client objective:
    ///    nothing fails the mission.
    ///  - The five talks in any order (objective 6 first in the log; Staal has its own waypoints, so a gate on 6 would send a
    ///    player who flew in back to the pass). The NPCs: CrucibleOutpostIntrepidNpcsV1 (Mullins and Munit new; Vitto, Donal
    ///    and Rigs moved into their quarters; Rigs given the client's package 1231).
    ///  - The items: the lowest Uncommon template of each kind and icon number in the band of level 40: Mech V03 Legs 33729,
    ///    Hazmat V02 Legs 20802 (39-43), Graviton Boots 23569 (V02: the client has no Uncommon V01 boots at 40), Motor Assist
    ///    V06 Boots 30167 (38-42); one to choose. A marker at each of the five.
    /// </summary>
    public static class CrucibleWireV1
    {
        public const uint MissionId = 1145;
        public const uint Level = 40;
        public const uint DeLisi = CrucibleOutpostIntrepidNpcsV1.DeLisi;
        public const uint Mullins = 6, Larai = 1, Viddia = 2, TopKhana = 3, Labbna = 5;

        public const uint BugTemplate = 50305, BugClass = 24715;
        public const string BugKey = "audio-surveillance-bug";
        public const double MarkerRadius = 8;

        /// <summary>The five talks: objective, name, body, the NPC's pool, its package.</summary>
        public static readonly (uint Objective, uint Name, uint Body, uint Npc, uint Package)[] Talks =
        {
            (Mullins, 9997, 9998, CrucibleOutpostIntrepidNpcsV1.Mullins, CrucibleOutpostIntrepidNpcsV1.MullinsPackage),
            (Larai, 8647, 8648, CrucibleOutpostIntrepidNpcsV1.Vitto, CrucibleOutpostIntrepidNpcsV1.VittoPackage),
            (Viddia, 8650, 8651, CrucibleOutpostIntrepidNpcsV1.Donal, CrucibleOutpostIntrepidNpcsV1.DonalPackage),
            (TopKhana, 8653, 8654, CrucibleOutpostIntrepidNpcsV1.Munit, CrucibleOutpostIntrepidNpcsV1.MunitPackage),
            (Labbna, 8662, 8663, CrucibleOutpostIntrepidNpcsV1.Rigs, CrucibleOutpostIntrepidNpcsV1.RigsPackage)
        };

        public const uint Experience = 75000;
        public const uint Credits = 5700;
        public const uint MechLegs = 33729;             // Armor_T3_Mech_V03_UNC_Legs_40_to_44
        public const uint HazmatLegs = 20802;           // Armor_T2_Hazmat_V02_UNC_Legs_39_to_43
        public const uint GravitonBoots = 23569;        // Armor_T3_Graviton_V02_UNC_Boots_40_to_44
        public const uint MotorAssistBoots = 30167;     // Armor_T1_MotorAssist_V06_UNC_Boots_38_to_42

        public static void Up(MigrationBuilder migration)
        {
            var mission = new CrucibleMissionDataV1(migration, MissionId, "The Wire", 8386, DeLisi, DeLisi, Level);
            mission.Reward(Experience, Credits, new[] { MechLegs, HazmatLegs, GravitonBoots, MotorAssistBoots });
            mission.Prerequisite(CrucibleFortCalledIntrepidV1.MissionId);

            for (var index = 0; index < Talks.Length; index++)
            {
                var (objective, name, body, npc, package) = Talks[index];
                mission.Objective(objective, name, body, (uint)index, MissionObjectiveState.Incomplete);
                mission.Transition(objective);
                mission.Conversation(objective, package);
                if (index == 0)
                    mission.Action(objective, 1, MissionActionKind.GrantReward, reward: 1);
                var at = CrucibleOutpostIntrepidNpcsV1.At(npc);
                mission.Indicator(objective, (uint)index + 1, at.X, at.Y, at.Z, MarkerRadius);
            }

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1145",
                "Native6 'Rendezvous with Mullins' 9997/9998, 1 'Find the Larai Gruppa.' 8647/8648, 2 Viddia 8650/8651, 3 Top Khana " +
                "8653/8654, 5 Labbna 8662/8663, no counters; objectiveconversation 1145,1 package1225 8649, 1145,2 1227 8659, 1145,3 " +
                "1229 8660, 1145,5 1231 8664, 1145,6 1454 9999. Texts 8386-8390. Item class24715 Audio Surveillance Bug (50305).");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/The_Wire",
                "Giver Captain DeLisi (Outpost Intrepid); requires A Fort Called Intrepid; follow-up The Boss Of All Bosses; 75000XP, " +
                "5700 credits; one of Voltrox Mech Armor Legs 3, Olympia Hazmat Armor Legs 2, Teleract Graviton Armor Boots 1, " +
                "Hellstrom Motor Assist Armor Boots 6; Mullins near the entrance of Convict Pass, due west of the fort.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:CrucibleWireV1",
                "Level40 and receiver DeLisi estimated. The bug issued at the accept, gone at the turn-in; no fail for being caught. " +
                "The five talks any order: Mullins 770001 (new) at the east mouth of Convict Pass, Vitto, Donal, Munit 770002 (new) " +
                "and Rigs in their quarters. Items 33729, 20802, 23569, 30167, one to choose. A marker at each NPC.", 0.3);

            var scene = new MissionSceneDefinition
            {
                Script = "data.sequence",
                MapContextId = CrucibleMissionDataV1.MapContextId,
                Items = new()
                {
                    new MissionItemBinding(BugKey, BugTemplate, MissionItemScope.AssignmentIssued, 1,
                        MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove)
                },
                AcceptanceItems = new() { new IssueMissionItemIntent("issue-surveillance-bug", MissionId, BugKey, BugTemplate, 1) }
            };
            scene.Sequences[0] = new SceneSequenceDefinition();

            mission.Enable(scene);
        }

        public static void Down(MigrationBuilder migration) => CrucibleMissionDataV1.Remove(migration, MissionId);
    }
}
