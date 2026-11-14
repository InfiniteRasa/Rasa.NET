using Microsoft.EntityFrameworkCore.Migrations;
using Rasa.Data;
using Rasa.Missions.Content;
using Rasa.Missions.Definitions;
using Rasa.Missions.Scenes;
using Rasa.Structures.World;

namespace Rasa.Services.Preloader.Missions.Crucible
{
    /// <summary>
    /// Making Amends (native mission 1155) and Sweet Revenge (1205), Ligo Crucible: the two ends of the AWOL fork with the same
    /// three steps. Sergeant Breiburg at the Awol Camp gives the player a crate of Bane rifles for Top Khana Grupa Munit at
    /// Staal, who trades it for a business ledger that proves D'Ambrose sells AFS weapons; Captain DeLisi at Outpost Intrepid
    /// reads the ledger; Breiburg hears that D'Ambrose is discredited. Making Amends after Stray Cats (the camp betrayed: "You
    /// betrayed us"), Sweet Revenge after The Other Side Of The Coin (the secret kept: "Have I got some dirt on him!").
    ///
    /// Native: 1155 texts 8449-8452, 8453, objectives 1 "Trade with Grupa Munit" 9470/9471, 2 "Show the ledger to Delisi."
    /// 9473/9474, 3 "Inform Sgt. Brieberg." 15821/15822; objectiveconversation (1155,1) package 1229 Munit 9472 ("Take the
    /// ledger, leave the crate"), (1155,2) 1219 DeLisi 9475 ("Let me see that ledger! ... I'll launch a full investigation
    /// immediately"), (1155,3) 1293 Breiburg 15823. 1205 texts 9476-9479, 9480, objectives 1 "Trade with Grupa Munit"
    /// 9481/9482, 2 "Show the ledger to Delisi." 9484/9485, 3 "Inform Sgt. Breiburg." 15818/15819; objectiveconversation
    /// (1205,1) 1229 9483, (1205,2) 1219 9486, (1205,3) 1293 15820. No counters. Item classes 24600 Top Khana Weapons Crate
    /// (MisCrucibleItemKhanaGunDelivery, template 50219) and 25074 Top Khana Business Ledger (MisCrucibleItemTopKhanaLedger,
    /// 50432).
    ///
    /// Sourced: TaRapedia Making Amends: giver and reward giver Awol Sergeant Breiburg; requires Stray Cats (completed);
    /// 80,000 XP, 5,850 credits; one of Vitalius Bio Armor Gloves, Dynamo Graviton Armor Helmet, Teleract Mech Armor Gloves,
    /// Phoenix Stealth Armor Gloves (level 40). TaRapedia Sweet Revenge: Breiburg; requires The Other Side Of The Coin
    /// (option 1); 80,000 XP, 5,850 credits; one of Hellstrom Bio Armor Legs (icon 2), Hellstrom Mech Armor Legs (2), Pulsar
    /// Stealth Armor Helmet (6), Teleract Reflective Armor Helmet (3); the steps (trade "a crate of Bane rifles" with Munit,
    /// show DeLisi, return to Breiburg). TR MMO DB: level 40 for both.
    ///
    /// Reconstructed (ESTIMATE, confidence 0.3):
    ///  - Receiver Breiburg. One of the two per player: Stray Cats done and The Other Side Of The Coin done shut each other
    ///    (CrucibleStrayCatsV1). The objectives open one by one (the client's order).
    ///  - The crate (24600, the only crate the client has for a delivery to the Top Khana) issued at the accept; Munit takes it
    ///    and gives the ledger at 1; DeLisi keeps the ledger at 2 ("I'll launch a full investigation"); an abandon takes them.
    ///  - The items the lowest Uncommon templates of each kind and icon number in the band of level 40; 1155 has no icon
    ///    numbers (the lowest, V02); 1205's Stealth helmet 6 has no Uncommon V06 at 40, so the nearest V04: 1155 Bio V02 Gloves
    ///    18899, Graviton V02 Helmet 23828, Mech V02 Gloves 32826, Stealth V02 Gloves 38952; 1205 Bio V02 Legs 19157, Mech V02
    ///    Legs 33084, Stealth V04 Helmet 40373, Reflective V03 Helmet 36638 (T2 39-43). One to choose. A marker at each NPC.
    /// </summary>
    public static class CrucibleMakingAmendsV1
    {
        public const uint MissionId = 1155, RevengeMissionId = 1205;
        public const uint Level = 40;
        public const uint Breiburg = CrucibleAwolCampNpcsV1.Breiburg;
        public const uint Trade = 1, ShowLedger = 2, InformBreiburg = 3;
        public const uint CrateTemplate = 50219, CrateClass = 24600;
        public const uint LedgerTemplate = 50432, LedgerClass = 25074;
        public const string CrateKey = "top-khana-weapons-crate", LedgerKey = "top-khana-business-ledger";
        public const double MarkerRadius = 8;

        public const uint Experience = 80000;
        public const uint Credits = 5850;
        public const uint BioGloves = 18899;            // Armor_T3_Bio_V02_UNC_Gloves_40_to_44
        public const uint GravitonHelmet = 23828;       // Armor_T3_Graviton_V02_UNC_Helmet_40_to_44
        public const uint MechGloves = 32826;           // Armor_T3_Mech_V02_UNC_Gloves_40_to_44
        public const uint StealthGloves = 38952;        // Armor_T3_Stealth_V02_UNC_Gloves_40_to_44
        public const uint BioLegs = 19157;              // Armor_T3_Bio_V02_UNC_Legs_40_to_44
        public const uint MechLegs = 33084;             // Armor_T3_Mech_V02_UNC_Legs_40_to_44
        public const uint StealthHelmet = 40373;        // Armor_T3_Stealth_V04_UNC_Helmet_40_to_44
        public const uint ReflectiveHelmet = 36638;     // Armor_T2_Reflective_V03_UNC_Helmet_39_to_43

        /// <summary>Each mission: id, name, title, its objectives' name and body ids, the prerequisite, the items.</summary>
        public static readonly (uint Mission, string Name, uint Title, (uint Name, uint Body)[] Objectives, uint Prerequisite, uint[] Items)[] Missions =
        {
            (MissionId, "Making Amends", 8449, new[] { (9470U, 9471U), (9473U, 9474U), (15821U, 15822U) },
                CrucibleStrayCatsV1.MissionId, new[] { BioGloves, GravitonHelmet, MechGloves, StealthGloves }),
            (RevengeMissionId, "Sweet Revenge", 9476, new[] { (9481U, 9482U), (9484U, 9485U), (15818U, 15819U) },
                CrucibleOtherSideOfTheCoinV1.MissionId, new[] { BioLegs, MechLegs, StealthHelmet, ReflectiveHelmet })
        };

        public static void Up(MigrationBuilder migration)
        {
            foreach (var (id, name, title, objectives, prerequisite, items) in Missions)
            {
                var mission = new CrucibleMissionDataV1(migration, id, name, title, Breiburg, Breiburg, Level);
                mission.Reward(Experience, Credits, items);
                mission.Prerequisite(prerequisite);

                mission.Objective(Trade, objectives[0].Name, objectives[0].Body, 0, MissionObjectiveState.Incomplete);
                mission.Objective(ShowLedger, objectives[1].Name, objectives[1].Body, 1, MissionObjectiveState.Inactive);
                mission.Objective(InformBreiburg, objectives[2].Name, objectives[2].Body, 2, MissionObjectiveState.Inactive);

                // 1: Munit takes the crate and gives the ledger.
                mission.Transition(Trade);
                mission.Conversation(Trade, CrucibleAwolCampNpcsV1.MunitPackage);
                mission.Action(Trade, 1, MissionActionKind.ConsumeMissionItem,
                    itemIntent: new ConsumeMissionItemIntent("munit-takes-the-crate", id, CrateKey, 1, MissionItemScope.AssignmentIssued));
                mission.Action(Trade, 2, MissionActionKind.IssueMissionItem,
                    itemIntent: new IssueMissionItemIntent("munit-gives-the-ledger", id, LedgerKey, LedgerTemplate, 1));
                CrucibleCalendarGirlV1.Open(mission, Trade, 1, 3, ShowLedger);

                // 2: DeLisi keeps the ledger.
                mission.Transition(ShowLedger);
                mission.Conversation(ShowLedger, CrucibleAwolCampNpcsV1.DeLisiPackage);
                mission.Action(ShowLedger, 1, MissionActionKind.ConsumeMissionItem,
                    itemIntent: new ConsumeMissionItemIntent("delisi-keeps-the-ledger", id, LedgerKey, 1, MissionItemScope.AssignmentIssued));
                CrucibleCalendarGirlV1.Open(mission, ShowLedger, 1, 2, InformBreiburg);

                // 3: Breiburg hears it.
                mission.Transition(InformBreiburg);
                mission.Conversation(InformBreiburg, CrucibleAwolCampNpcsV1.BreiburgPackage);
                mission.Action(InformBreiburg, 1, MissionActionKind.GrantReward, reward: 1);

                var munit = CrucibleAwolCampNpcsV1.At(CrucibleAwolCampNpcsV1.Munit);
                var delisi = CrucibleAwolCampNpcsV1.At(CrucibleAwolCampNpcsV1.DeLisi);
                var breiburg = CrucibleAwolCampNpcsV1.At(Breiburg);
                mission.Indicator(Trade, 1, munit.X, munit.Y, munit.Z, MarkerRadius);
                mission.Indicator(ShowLedger, 2, delisi.X, delisi.Y, delisi.Z, MarkerRadius);
                mission.Indicator(InformBreiburg, 3, breiburg.X, breiburg.Y, breiburg.Z, MarkerRadius);

                var making = id == MissionId;
                mission.Evidence(1, MissionEvidenceSourceKind.Client, $"native:missionobjective/{id}",
                    $"Native1 'Trade with Grupa Munit' {objectives[0].Name}/{objectives[0].Body}, 2 'Show the ledger to Delisi.' " +
                    $"{objectives[1].Name}/{objectives[1].Body}, 3 'Inform Sgt. {(making ? "Brieberg" : "Breiburg")}.' " +
                    $"{objectives[2].Name}/{objectives[2].Body}, no counters; objectiveconversation {id},1 pkg1229 " +
                    $"{(making ? 9472 : 9483)}, {id},2 pkg1219 {(making ? 9475 : 9486)}, {id},3 pkg1293 {(making ? 15823 : 15820)}. " +
                    $"Texts {title}-{title + 4} ({(making ? "'You betrayed us'" : "'Have I got some dirt on him'")}). Crate " +
                    "class24600 (template50219), ledger class25074 (template50432).");
                mission.Evidence(2, MissionEvidenceSourceKind.Documentation,
                    making ? "https://tabularasa.fandom.com/wiki/Making_Amends" : "https://tabularasa.fandom.com/wiki/Sweet_Revenge",
                    making
                        ? "Awol Sergeant Breiburg gives and rewards; requires Stray Cats (completed); 80000XP, 5850 credits; Vitalius " +
                          "Bio Armor Gloves, Dynamo Graviton Armor Helmet, Teleract Mech Armor Gloves, Phoenix Stealth Armor Gloves " +
                          "(lvl40); the betrayal branch. TR MMO DB level40."
                        : "Awol Sergeant Breiburg; requires The Other Side Of The Coin (option 1); 80000XP, 5850 credits; Hellstrom " +
                          "Bio Armor Legs 2, Hellstrom Mech Armor Legs 2, Pulsar Stealth Armor Helmet 6, Teleract Reflective Armor " +
                          "Helmet 3; trade a crate of Bane rifles with Munit, show DeLisi, return to Breiburg. TR MMO DB level40.");
                mission.Evidence(3, MissionEvidenceSourceKind.Reconstruction, "repository:CrucibleMakingAmendsV1",
                    "Receiver Breiburg; one of 1155/1205 per player (1152 and 1153 shut each other). Crate at the accept; Munit " +
                    "takes it and gives the ledger; DeLisi keeps it; objectives open one by one. Items " +
                    $"{string.Join(", ", items)}, one to choose. A marker at each NPC.", 0.3);

                mission.Enable(Scene(id));
            }
        }

        /// <summary>The crate at the accept; the ledger at Munit's talk.</summary>
        public static MissionSceneDefinition Scene(uint mission)
        {
            var scene = new MissionSceneDefinition
            {
                Script = "data.sequence",
                MapContextId = CrucibleMissionDataV1.MapContextId,
                Items = new()
                {
                    new MissionItemBinding(CrateKey, CrateTemplate, MissionItemScope.AssignmentIssued, 1,
                        MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove),
                    new MissionItemBinding(LedgerKey, LedgerTemplate, MissionItemScope.AssignmentIssued, 1,
                        MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove, MissionItemCleanupDisposition.Remove)
                },
                AcceptanceItems = new() { new IssueMissionItemIntent("issue-crate", mission, CrateKey, CrateTemplate, 1) }
            };
            scene.Sequences[0] = new SceneSequenceDefinition();
            return scene;
        }

        public static void Down(MigrationBuilder migration)
        {
            CrucibleMissionDataV1.Remove(migration, RevengeMissionId);
            CrucibleMissionDataV1.Remove(migration, MissionId);
        }
    }
}
