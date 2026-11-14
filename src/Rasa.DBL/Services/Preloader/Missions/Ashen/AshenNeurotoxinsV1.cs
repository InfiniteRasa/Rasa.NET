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
    /// Neurotoxins (native mission 1300), Ligo Ashen Desert: the poison side of Murky Waters (1298, choice 2, flag 257). Agent
    /// Hale wants the poison glands of the Lashers to poison the Water Harvesters' filters; Poisoning the Wellwater (1302) follows.
    ///
    /// Native: texts 10500-10503, 10504 (missionconversation): title "Neurotoxins", log "Recover 12 Lasher Poison Glands and
    /// return them to Agent Hale at White Oasis so that he can sabotage the Water Harvester filtration system. Lashers can be
    /// found throughout the Ashen Desert, including the ridge west of White Oasis.", offer "We will use the neurotoxin found in
    /// the glands of the Lasher. ...", turn-in "With this poison, we can make a permanent mark on the Bane. ...", reminder
    /// 10504; objective 1 "Recover 12 Lasher Poison Glands" 10505/10506, no counter (missionobjective); no objective
    /// conversation. Item class 25479 Mis_Ligo_Ashen_Maji_LasherGland "Lasher Poison Gland" (template 97480, stack 12:
    /// itemclass). Murky Waters' 2 is heard in variant 257 (objectiveconversation 1298,2).
    ///
    /// Sourced: TaRapedia Neurotoxins: giver and reward giver Agent Hale; requires Murky Waters (Choice A); follow-up Poisoning
    /// the Wellwater; 6,450 credits, XP "Unknown"; 12 glands; one of Astra Mech Armor Legs 4, Wellcare Graviton Armor Vest 2,
    /// Phoenix Bio Armor Vest 4, Wellcare Stealth Armor Vest 4; "The Lasher are all throughout the desert."
    ///
    /// Reconstructed (ESTIMATE, confidence 0.3):
    ///  - Flag 257 is the poison's (triage D3: the reading of the client's variant 257 of 1298,2 "You are wise, indeed" after
    ///    choice 2 "I agree with you").
    ///  - Offered after Murky Waters is done with flag 257 at 1 (choice 2, "Choice A" on TaRapedia: Hale's side): the flags stay
    ///    at an abandon of 1298, so a gate on the flag alone would open it before 1298 is done (as Water Purification). Level 42
    ///    (D5). 100,000 XP (TaRapedia "Unknown": the zone's 6,450-credit missions, as Analyzing the Harvesters).
    ///  - The gland a personal 75% drop (as set 10's parts) off set 10's Lasher 790362 on Ashen Desert while 1 is open, 12 at
    ///    most; taken at the turn-in, kept at an abandon. The Lasher stand in set 10's two pools west of Ashoka and in the two new
    ///    pools on the ridge west of White Oasis Post (AshenMurkyWatersNpcsV1: the client's log). Molly drops none.
    ///  - Markers r20 on the four Lasher pools.
    ///  - The items: the lowest template of TaRapedia's kind and version number in the band of level 42 (World DB entityclass
    ///    names, as Designer Jewelry Gone Wrong): Mech V04 Legs 34374, Graviton V02 Vest 24086, Bio V04 Vest 46037, Stealth V04
    ///    Vest 40631 (all 40-44); one to choose.
    /// </summary>
    public static class AshenNeurotoxinsV1
    {
        public const uint MissionId = 1300;
        public const uint Level = 42;
        public const uint Hale = AshenWhiteOasisNpcsV1.Hale;
        public const uint Glands = 1;
        public const uint Count = 12;
        public const int Chance = 75;
        public const uint GlandTemplate = 97480, GlandClass = 25479;
        public const string GlandKey = "lasher-poison-gland";
        public const double MarkerRadius = 20;

        public const uint Experience = 100000;
        public const uint Credits = 6450;
        public const uint MechLegs = 34374;             // Armor_T3_Mech_V04_UNC_Legs_40_to_44
        public const uint GravitonVest = 24086;         // Armor_T3_Graviton_V02_UNC_Vest_40_to_44
        public const uint BioVest = 46037;              // Armor_T3_Bio_V04_UNC_Vest_40_to_44
        public const uint StealthVest = 40631;          // Armor_T3_Stealth_V04_UNC_Vest_40_to_44

        /// <summary>The Lasher pools the markers stand on: set 10's two west of Ashoka, then the two on the ridge west of White Oasis.</summary>
        public static readonly (uint Pool, (double X, double Y, double Z) At)[] LasherPools =
            AshenDesignerHidesNpcsV1.LasherPools.Concat(AshenMurkyWatersNpcsV1.RidgePools).ToArray();

        public static void Up(MigrationBuilder migration)
        {
            var mission = new AshenMissionDataV1(migration, MissionId, "Neurotoxins", 10500, Hale, Hale, Level);
            mission.Reward(Experience, Credits, new[] { MechLegs, GravitonVest, BioVest, StealthVest });
            mission.Prerequisite(AshenMurkyWatersV1.MissionId, 1);
            mission.FlagPrerequisite(AshenMurkyWatersV1.PoisonFlag, AshenMurkyWatersV1.Chosen, "Murky Waters choice 2, the poison (flag 257)", 2);

            mission.Objective(Glands, 10505, 10506, 0, MissionObjectiveState.Incomplete);
            mission.Transition(Glands);
            mission.Progress(Glands, MissionProgressEventKind.ItemAcquired, GlandClass, Count);
            mission.Action(Glands, 1, MissionActionKind.GrantReward, reward: 1);

            var marker = 1U;
            foreach (var (_, at) in LasherPools)
                mission.Indicator(Glands, marker++, at.X, at.Y, at.Z, MarkerRadius);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1300",
                "Native1 'Recover 12 Lasher Poison Glands' 10505/10506, no counter; no objectiveconversation. Texts 10500-10504 " +
                "(Agent Hale at White Oasis; Lashers throughout the Ashen Desert, including the ridge west of White Oasis). Item " +
                "class25479 Lasher Poison Gland (template97480, stack12). Flag 257 the variant of 1298,2 (the poison).");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Neurotoxins",
                "Giver/reward giver Agent Hale; requires Murky Waters (Choice A); follow-up Poisoning the Wellwater; 6450 credits, " +
                "XP Unknown; 12 glands; Astra Mech Armor Legs 4, Wellcare Graviton Armor Vest 2, Phoenix Bio Armor Vest 4, Wellcare " +
                "Stealth Armor Vest 4; the Lasher all throughout the desert.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:AshenNeurotoxinsV1",
                "After 1298 with flag257 at 1. Level42 and 100000XP estimated. The gland a personal 75% drop off set 10's Lasher " +
                "790362 on Ashen Desert, 12 at most (pools 790363, 790364 west of Ashoka; 790440, 790441 new on the ridge west of " +
                "White Oasis); taken at the turn-in, kept at an abandon; markers r20 on the four pools. Items 34374, 24086, 46037, " +
                "40631, one to choose.", 0.3);

            var scene = new MissionSceneDefinition
            {
                ExistingFactObjectiveIds = new() { Glands },
                Items = new()
                {
                    new MissionItemBinding(GlandKey, GlandTemplate, MissionItemScope.CharacterOwned, Count,
                        MissionItemCleanupDisposition.Retain, MissionItemCleanupDisposition.Retain, MissionItemCleanupDisposition.Retain,
                        TurnInQuantity: Count,
                        Drop: new MissionItemDropDefinition(new[] { AshenDesignerHidesNpcsV1.Lasher }, Glands, AshenMissionDataV1.MapContextId, Chance, 1))
                }
            };
            mission.Enable(scene);
        }

        public static void Down(MigrationBuilder migration) => AshenMissionDataV1.Remove(migration, MissionId);
    }
}
