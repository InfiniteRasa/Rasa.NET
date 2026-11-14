using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Ashen
{
    /// <summary>
    /// Supply Logistics (native mission 1358), Ligo Ashen Desert: Lieutenant Daniel Chase at Shadow's Edge Post has the player
    /// carry tactical supplies to the five captains in the field: Botha and Harris at Ashoka Settlement, Emmons on Stonewall
    /// Ridge, Yamamoto at Boneyard Outpost and Daris Linier at White Oasis Post.
    ///
    /// Native: texts 11159-11162, 11163 (missionconversation): log "Deliver the Tactical Military Supplies to the 5 Captains in the
    /// field. ... Report back to Lieutenant Chase once you have made all of your deliveries.", offer "We got five Captains in the
    /// field ... Here's the stuff.", turn-in "Yep, all of them got their supplies."; objectives (missionobjective) 1 "Deliver to
    /// Captain Botha" 11452/11453, 2 "Deliver to Captain Harris" 11455/11456, 3 "Deliver to Captain Emmons" 11458/11459, 4
    /// "Deliver to Captain Yamamoto" 11461/11462, 5 "Deliver to Captain Linier" 11464/11465, no counters; objective
    /// conversations (objectiveconversation, variant 1 each) 1358,1 package 1410 (Botha) 11454, 1358,2 1412 (Harris) 11457,
    /// 1358,3 1411 (Emmons) 11460, 1358,4 1414 (Captain Akio Yamamoto) 11463, 1358,5 1476 (Daris Linier) 11466. Items (item
    /// templates, mission items): Supplies for Captain Botha 97522 (class 25645), Harris 97524 (25647), Emmons 97523 (25648),
    /// Yamamoto 97526 (25649), Linier 97525 (25650). Name 9289 Chase.
    ///
    /// Sourced: TaRapedia Supply Logistics: giver and reward giver Lieutenant Daniel Chase (Shadow's Edge Post); five
    /// deliveries (Botha and Harris at Ashoka, Yamamoto in the Boneyard, Emmons on Stonewall Ridge, Daris Linier at White Oasis
    /// Post); 95,000 XP, 6,300 credits; Teleract Mech Armor Vest 4, ChiTech Graviton Armor Legs 1, ChiTech Hazmat Armor Boots 2,
    /// Audiodyne Reflective Armor Boots 5. No requirement (TaRapedia names none; the triage: no link).
    ///
    /// Reconstructed (ESTIMATE, confidence 0.3):
    ///  - Level 41 (triage D5; TaRapedia's XP is 1280's, level 41).
    ///  - The five supplies at the accept (a bag without room for all five refuses the accept), each taken by its captain at the
    ///    talk; gone at an abandon or a failure. The five open at once and are done in any order (the client's log names no
    ///    order). The captains where the sets put them: Botha 770040 and Harris 510008 by the Ashoka waypoint (upstream and
    ///    PR #224's rows; not moved: both stand on the client's floor by the waypoint), Emmons 510009 and Daris Linier 510007
    ///    where set 1 put them, Yamamoto 790003 (set 1's) at Boneyard Outpost. A marker at each.
    ///  - White Oasis Post: while the Bane hold it, Daris Linier is gone (the control point's service rule, ControlPointServices)
    ///    and 5 waits (no fail), as every White Oasis mission (triage "Control points").
    ///  - The items: TaRapedia's names read as kind and version (the number), the lowest template of that version in the band
    ///    of level 41 (client entityclass names): Mech V04 Vest 34503 (40-44, Uncommon), Graviton V01 Legs 13043 (40-44,
    ///    Common: the version has no Uncommon), Hazmat V02 Boots 20355 (39-43, Uncommon), Reflective V05 Boots 37836 (39-43, Rare:
    ///    the version has only Rare); one to choose.
    /// </summary>
    public static class AshenSupplyLogisticsV1
    {
        public const uint MissionId = 1358;
        public const uint Level = 41;
        public const uint Chase = AshenAshokaNpcsV1.Chase;
        public const uint Variant = 1;
        public const double MarkerRadius = 8;

        /// <summary>The deliveries: objective, texts, captain (pool), package, supplies template and key, the item class.</summary>
        public static readonly (uint Objective, uint Name, uint Body, uint Captain, uint Package, uint Template, uint Class, string Key)[] Deliveries =
        {
            (1, 11452, 11453, Botha, BothaPackage, 97522, 25645, "supplies-for-captain-botha"),
            (2, 11455, 11456, AshenLeadInNpcsV1.Harris, AshenLeadInNpcsV1.HarrisPackage, 97524, 25647, "supplies-for-captain-harris"),
            (3, 11458, 11459, AshenLeadInNpcsV1.Emmons, AshenLeadInNpcsV1.EmmonsPackage, 97523, 25648, "supplies-for-captain-emmons"),
            (4, 11461, 11462, AshenLeadInNpcsV1.Yamamoto, AshenLeadInNpcsV1.YamamotoPackage, 97526, 25649, "supplies-for-captain-yamamoto"),
            (5, 11464, 11465, AshenLeadInNpcsV1.DarisLinier, AshenLeadInNpcsV1.DarisLinierPackage, 97525, 25650, "supplies-for-captain-linier")
        };

        /// <summary>Captain Amandla Botha (PR #224's row at Ashoka) and her package (objectiveconversation 1358,1 and 1736,2).</summary>
        public const uint Botha = 770040, BothaPackage = 1410;

        /// <summary>Where each captain stands in the World DB (pool = creature id): Botha's and Harris' rows by the Ashoka waypoint, set 1's others.</summary>
        public static readonly (uint Captain, (double X, double Y, double Z) At)[] CaptainsAt =
        {
            (Botha, (378, 280.69, -458.5)),
            (AshenLeadInNpcsV1.Harris, (405, 281.41, -459.5)),
            (AshenLeadInNpcsV1.Emmons, (-62, 350.48, -414)),
            (AshenLeadInNpcsV1.Yamamoto, AshenLeadInNpcsV1.YamamotoAt),
            (AshenLeadInNpcsV1.DarisLinier, (-507.4, 282.73, 162.4))
        };

        public const uint Experience = 95000;
        public const uint Credits = 6300;
        public const uint MechVest = 34503;             // Armor_T3_Mech_V04_UNC_Vest_40_to_44
        public const uint GravitonLegs = 13043;         // Armor_T3_Graviton_V01_CMN_Legs_40_to_44
        public const uint HazmatBoots = 20355;          // Armor_T2_Hazmat_V02_UNC_Boots_39_to_43
        public const uint ReflectiveBoots = 37836;      // Armor_T2_Reflective_V05_RAR_Boots_39_to_43

        public static void Up(MigrationBuilder migration)
        {
            var mission = new AshenMissionDataV1(migration, MissionId, "Supply Logistics", 11159, Chase, Chase, Level);
            mission.Reward(Experience, Credits, new[] { MechVest, GravitonLegs, HazmatBoots, ReflectiveBoots });

            var ordinal = 0U;
            foreach (var delivery in Deliveries)
            {
                mission.Objective(delivery.Objective, delivery.Name, delivery.Body, ordinal++, MissionObjectiveState.Incomplete);
                mission.Transition(delivery.Objective);
                mission.Conversation(delivery.Objective, delivery.Package, Variant);
                mission.Action(delivery.Objective, 1, MissionActionKind.ConsumeMissionItem,
                    itemIntent: new ConsumeMissionItemIntent($"{delivery.Key}-delivered", MissionId, delivery.Key, 1, MissionItemScope.AssignmentIssued));
                if (delivery.Objective == 1)
                    mission.Action(delivery.Objective, 2, MissionActionKind.GrantReward, reward: 1);   // the reward's reference (one, as 1287's)
                var at = System.Array.Find(CaptainsAt, captain => captain.Captain == delivery.Captain).At;
                mission.Indicator(delivery.Objective, delivery.Objective, at.X, at.Y, at.Z, MarkerRadius);
            }

            mission.Evidence(1, MissionEvidenceSourceKind.Client, "native:missionobjective/1358",
                "Native1-5 'Deliver to Captain Botha / Harris / Emmons / Yamamoto / Linier' 11452/11453, 11455/11456, 11458/11459, " +
                "11461/11462, 11464/11465, no counters; objectiveconversation 1358,1 pkg1410 11454, 1358,2 pkg1412 11457, 1358,3 " +
                "pkg1411 11460, 1358,4 pkg1414 11463, 1358,5 pkg1476 11466 (variant 1). Texts 11159-11163. Items templates97522, " +
                "97524, 97523, 97526, 97525 (classes 25645, 25647-25650). Name 9289 Chase.");
            mission.Evidence(2, MissionEvidenceSourceKind.Documentation, "https://tabularasa.fandom.com/wiki/Supply_Logistics",
                "Giver/reward giver Lieutenant Daniel Chase (Shadow's Edge Post); Botha and Harris at Ashoka, Yamamoto in the Boneyard, " +
                "Emmons on Stonewall Ridge, Daris Linier at White Oasis Post; 95000XP, 6300 credits; Teleract Mech Armor Vest 4, ChiTech " +
                "Graviton Armor Legs 1, ChiTech Hazmat Armor Boots 2, Audiodyne Reflective Armor Boots 5.");
            mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:AshenSupplyLogisticsV1",
                "Level41 estimated; no prerequisite. Chase 790520 at (-120, -805) in Shadow's Edge Post. The five supplies at the " +
                "accept, each taken by its captain at the talk, in any order; gone at an abandon. 5 waits while the Bane hold White " +
                "Oasis. Markers at the five captains. Items 34503, 13043, 20355, 37836, one to choose.", 0.3);

            mission.Enable(Scene());
        }

        /// <summary>The five supplies at the accept.</summary>
        public static MissionSceneDefinition Scene()
        {
            var scene = new MissionSceneDefinition
            {
                Script = "data.sequence",
                MapContextId = AshenMissionDataV1.MapContextId,
                Items = new(),
                AcceptanceItems = new()
            };
            foreach (var delivery in Deliveries)
            {
                scene.Items.Add(new MissionItemBinding(delivery.Key, delivery.Template, MissionItemScope.AssignmentIssued, 1,
                    MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove));
                scene.AcceptanceItems.Add(new IssueMissionItemIntent($"chase-gives-{delivery.Key}", MissionId, delivery.Key, delivery.Template, 1));
            }
            scene.Sequences[0] = new SceneSequenceDefinition();
            return scene;
        }

        public static void Down(MigrationBuilder migration) => AshenMissionDataV1.Remove(migration, MissionId);
    }
}
