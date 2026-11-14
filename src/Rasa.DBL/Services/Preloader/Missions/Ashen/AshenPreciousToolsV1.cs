using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Ashen
{
    /// <summary>
    /// Precious Tools (native mission 1286), Ligo Ashen Desert: a Thrax took Mechanic Raji Lasat's lucky wrench while he
    /// scavenged in the Boneyard; the player takes it back from Overseer Jirex.
    ///
    /// Native: texts 10377-10380, 10381 "none" (missionconversation): log "Mechanic Raji Lasat in Ashoka Settlement has lost his
    /// lucky wrench while scavenging for parts in the Boneyard. He believes that one of the Thrax took it. ...", offer "Lost my
    /// wrench, yes. Ugly Thrax took it. ...", turn-in "My wrench! Oh, I missed you. ..."; objective 1 "Recover Raji's Lucky
    /// Wrench" 10382/10383, no counter (missionobjective); no objective conversation. Item class 25481 Mechanic Raji's Lucky
    /// Wrench (template 97482). Names 9270 Raji, 9793 Overseer Jirex.
    ///
    /// Sourced: TaRapedia Precious Tools: giver and reward giver Mechanic Raji Lasat (Ashoka); no requirement; 190,000 XP,
    /// 8,400 credits; ChiTech Bio Armor Gloves, Teleract Graviton Armor Helmet, Teleract Mech Armor Gloves, ChiTech Stealth Armor
    /// Gloves 2; the wrench "Dropped by Overseer Jirex", "Near a pillbox, northeast of Ashoka Settlement Waypoint".
    ///
    /// Reconstructed (ESTIMATE, confidence 0.3):
    ///  - Level 42 (triage D5; TaRapedia's XP is 1281's, level 42). Raji 510014 where upstream has him, by the Ashoka waypoint
    ///    (his row has no package: he gives and takes by mission only, as the client).
    ///  - The wrench a personal drop off Jirex (AshenAshokaNpcsV1, near the destroyed pillbox) at 100% (TaRapedia names one
    ///    dropper), one at most; taken by Raji at the turn-in, kept at an abandon (as 1287's Pristine Carapace). A marker r15 on
    ///    Jirex.
    ///  - The items: the lowest Uncommon template of the lowest version of each kind in the band of level 42 (TaRapedia names no
    ///    version but the Stealth Gloves' 2; client entityclass names): Bio V02 Gloves 18899, Graviton V02 Helmet 23828, Mech V02
    ///    Gloves 32826 (40-44), Stealth V02 Gloves 38952 (40-44); one to choose.
    /// </summary>
    public static class AshenPreciousToolsV1
    {
        public const uint MissionId = 1286;
        public const uint Level = 42;
        public const uint Raji = AshenIndraPassNpcsV1.Raji;
        public const uint Recover = 1;
        public const int Chance = 100;
        public const uint WrenchTemplate = 97482, WrenchClass = 25481;
        public const string WrenchKey = "rajis-lucky-wrench";
        public const double MarkerRadius = 15;

        public const uint Experience = 190000;
        public const uint Credits = 8400;
        public const uint BioGloves = 18899;            // Armor_T3_Bio_V02_UNC_Gloves_40_to_44
        public const uint GravitonHelmet = 23828;       // Armor_T3_Graviton_V02_UNC_Helmet_40_to_44
        public const uint MechGloves = 32826;           // Armor_T3_Mech_V02_UNC_Gloves_40_to_44
        public const uint StealthGloves = 38952;        // Armor_T3_Stealth_V02_UNC_Gloves_40_to_44

        public static void Up(MigrationBuilder migration)
        {
            var mission = new AshenMissionDataV1(migration, MissionId, "Precious Tools", 10377, Raji, Raji, Level);
            mission.Reward(Experience, Credits, new[] { BioGloves, GravitonHelmet, MechGloves, StealthGloves });

            mission.Objective(Recover, 10382, 10383, 0, MissionObjectiveState.Incomplete);
            mission.Transition(Recover);
            mission.Progress(Recover, MissionProgressEventKind.ItemAcquired, WrenchClass, 1);
            mission.Action(Recover, 1, MissionActionKind.GrantReward, reward: 1);

            var jirex = AshenAshokaNpcsV1.JirexAt;
            mission.Indicator(Recover, 1, jirex.X, jirex.Y, jirex.Z, MarkerRadius);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1286",
                "Native1 'Recover Raji's Lucky Wrench' 10382/10383, no counter; no objectiveconversation. Texts 10377-10380, 10381 " +
                "'none' (a Thrax took the wrench in the Boneyard). Item class25481 Mechanic Raji's Lucky Wrench (template97482). " +
                "Names 9270 Raji, 9793 Overseer Jirex.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Precious_Tools",
                "Giver/reward giver Mechanic Raji Lasat (Ashoka); no requirement; 190000XP, 8400 credits; ChiTech Bio Armor Gloves, " +
                "Teleract Graviton Armor Helmet, Teleract Mech Armor Gloves, ChiTech Stealth Armor Gloves 2; dropped by Overseer " +
                "Jirex, near a pillbox northeast of Ashoka Settlement Waypoint (TaRapedia Overseer Jirex).");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:AshenPreciousToolsV1",
                "Level42 estimated. Jirex 790523 (the Overseers' class 10504, L44, pool 790524 at (426.8, -261), 30 m west of the " +
                "destroyed pillbox (454, -273), respawn 300 s); the wrench a personal 100% drop off him, one at most, taken at the " +
                "turn-in, kept at an abandon; marker r15 on him. Items 18899, 23828, 32826, 38952, one to choose.", 0.3);

            mission.Enable(new MissionSceneDefinition
            {
                ExistingFactObjectiveIds = new() { Recover },
                Items = new()
                {
                    new MissionItemBinding(WrenchKey, WrenchTemplate, MissionItemScope.CharacterOwned, 1,
                        MissionItemCleanupDisposition.Retain, MissionItemCleanupDisposition.Retain, MissionItemCleanupDisposition.Retain,
                        TurnInQuantity: 1,
                        Drop: new MissionItemDropDefinition(new[] { AshenAshokaNpcsV1.Jirex }, Recover, AshenMissionDataV1.MapContextId, Chance, 1))
                }
            });
        }

        public static void Down(MigrationBuilder migration) => AshenMissionDataV1.Remove(migration, MissionId);
    }
}
