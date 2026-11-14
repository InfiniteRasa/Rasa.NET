using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Ashen
{
    /// <summary>
    /// Water Purification (native mission 1299), Ligo Ashen Desert: the repair side of Murky Waters (1298, choice 1, flag 258).
    /// Doctor Harold Meeks needs the water glands of the Granitour of Coheri Pools to repair the harvesters' filtration.
    ///
    /// Native: texts 10493-10496, 10497 (missionconversation): title "Water Purification", log "Dr. Harold Meeks at White Oasis
    /// Base requires the Water Glands from Granitour at Coheri Pools. Recover 12 Granitour Water Glands and bring them to Dr.
    /// Meeks to he can repair the filtration system on the Water Harvesters. The Coheri Pools are on the east side of Maji
    /// Field.", offer "This is going to sound strange, but to repair those harvesters I'm going to need you to collect spit
    /// glands from the Granitour in Coheri Pools ...", turn-in "Thank you. This looks like it will be enough to do the job.
    /// ...", reminder 10497; objective 1 "Recover 12 Granitour Water Glands" 10498/10499, no counter (missionobjective); no
    /// objective conversation. Item class 25478 Mis_Ligo_Ashen_Maji_GranitourGland "Granitour Water Gland" (template 97479,
    /// stack 12: itemclass). Murky Waters' 3 is heard in variant 258 (objectiveconversation 1298,3).
    ///
    /// Sourced: TaRapedia has no page for it; Murky Waters' page names it a follow-up; Neurotoxins "requires Murky Waters (Choice
    /// A)". TaRapedia Granitour: "Granitour spit can be used as a highly efficient filtration medium".
    ///
    /// Reconstructed (ESTIMATE, confidence 0.3):
    ///  - Water Purification is Murky Waters' other choice, the repair, and flag 258 is the repair's (triage D3: the reading of
    ///    the client's variant 258 of 1298,3 "Well, that is your choice" after choice 1 "I can't do that"; research group 1
    ///    infers the branch from Neurotoxins' Choice A).
    ///  - Giver and receiver Meeks (the client's log and offer). Offered after Murky Waters is done with flag 258 at 1 (choice 1,
    ///    the repair): the flags stay at an abandon of 1298, so a gate on the flag alone would open it before 1298 is done
    ///    (auditor note on set 11; as set 7's 1264 and set 6's 1258). Level 42 (triage D5: the follow-up of a level-41
    ///    mission).
    ///  - The gland a personal 75% drop (as set 10's parts) off set 10's Granitour (AshenDesignerHidesNpcsV1) on Ashen Desert
    ///    while 1 is open, 12 at most; taken at the turn-in, kept at an abandon. The bindings are this mission's own (All the
    ///    Latest Rage!'s carapace drops beside it). Wendy drops none (the client names the Granitour).
    ///  - Markers r20 on set 10's two Coheri Pools Granitour pools.
    ///  - Rewards (triage D6: no source): the zone median 95,000 XP and 6,300 credits and the stacks of the zone's talks
    ///    (Formal Complaints'), one to choose.
    /// </summary>
    public static class AshenWaterPurificationV1
    {
        public const uint MissionId = 1299;
        public const uint Level = 42;
        public const uint Meeks = AshenWhiteOasisNpcsV1.Meeks;
        public const uint Glands = 1;
        public const uint Count = 12;
        public const int Chance = 75;
        public const uint GlandTemplate = 97479, GlandClass = 25478;
        public const string GlandKey = "granitour-water-gland";
        public const double MarkerRadius = 20;

        public const uint Experience = 95000;
        public const uint Credits = 6300;
        public const uint EmpGrenade = AshenFormalComplaintsV1.EmpGrenade, EmpGrenades = AshenFormalComplaintsV1.EmpGrenades;
        public const uint ResTraumaKit = AshenFormalComplaintsV1.ResTraumaKit, ResTraumaKits = AshenFormalComplaintsV1.ResTraumaKits;
        public const uint ConcussionGrenade = AshenFormalComplaintsV1.ConcussionGrenade, ConcussionGrenades = AshenFormalComplaintsV1.ConcussionGrenades;
        public const uint BasicMedPack = AshenFormalComplaintsV1.BasicMedPack, BasicMedPacks = AshenFormalComplaintsV1.BasicMedPacks;

        public static void Up(MigrationBuilder migration)
        {
            var mission = new AshenMissionDataV1(migration, MissionId, "Water Purification", 10493, Meeks, Meeks, Level);
            mission.Reward(Experience, Credits,
                new[] { (EmpGrenade, EmpGrenades), (ResTraumaKit, ResTraumaKits), (ConcussionGrenade, ConcussionGrenades), (BasicMedPack, BasicMedPacks) },
                new (uint, uint)[0]);
            mission.Prerequisite(AshenMurkyWatersV1.MissionId, 1);
            mission.FlagPrerequisite(AshenMurkyWatersV1.RepairFlag, AshenMurkyWatersV1.Chosen, "Murky Waters choice 1, the repair (flag 258)", 2);

            mission.Objective(Glands, 10498, 10499, 0, MissionObjectiveState.Incomplete);
            mission.Transition(Glands);
            mission.Progress(Glands, MissionProgressEventKind.ItemAcquired, GlandClass, Count);
            mission.Action(Glands, 1, MissionActionKind.GrantReward, reward: 1);

            var marker = 1U;
            foreach (var (_, at) in AshenDesignerHidesNpcsV1.GranitourPools)
                mission.Indicator(Glands, marker++, at.X, at.Y, at.Z, MarkerRadius);

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1299",
                "Native1 'Recover 12 Granitour Water Glands' 10498/10499, no counter; no objectiveconversation. Texts 10493-10497 " +
                "(Dr. Harold Meeks at White Oasis; the Granitour at Coheri Pools, the east side of Maji Field). Item class25478 " +
                "Granitour Water Gland (template97479, stack12). Flag 258 the variant of 1298,3 (the repair).");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Murky_Waters",
                "No page for Water Purification; Murky Waters names it a follow-up; Neurotoxins requires Murky Waters (Choice A). " +
                "Granitour: their spit a highly efficient filtration medium.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:AshenWaterPurificationV1",
                "Giver and receiver Meeks 790400. The repair side (Choice B, inferred from Neurotoxins' Choice A): after 1298 with " +
                "flag258 at 1. Level42 estimated. The gland a personal 75% drop off " +
                "set 10's Granitour 790373 on Ashen Desert, 12 at most; taken at the turn-in, kept at an abandon; markers r20 on the " +
                "pools 790374, 790375. 95000XP, 6300 credits (zone median); stacks 111029x3, 47600x1, 111019x3, 45063x6, one to " +
                "choose.", 0.3);

            var scene = new MissionSceneDefinition
            {
                ExistingFactObjectiveIds = new() { Glands },
                Items = new()
                {
                    new MissionItemBinding(GlandKey, GlandTemplate, MissionItemScope.CharacterOwned, Count,
                        MissionItemCleanupDisposition.Retain, MissionItemCleanupDisposition.Retain, MissionItemCleanupDisposition.Retain,
                        TurnInQuantity: Count,
                        Drop: new MissionItemDropDefinition(new[] { AshenDesignerHidesNpcsV1.Granitour }, Glands, AshenMissionDataV1.MapContextId, Chance, 1))
                }
            };
            mission.Enable(scene);
        }

        public static void Down(MigrationBuilder migration) => AshenMissionDataV1.Remove(migration, MissionId);
    }
}
