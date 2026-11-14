using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Missions.Ashen
{
    using Rasa.Data;
    using Rasa.Managers;
    using Rasa.Services.Preloader.Missions.Ashen;
    using Rasa.Services.Preloader.Missions.Crucible;
    using Rasa.Structures;
    using Rasa.Structures.World;
    using Rasa.Test.Missions.Wilderness;

    /// <summary>
    /// The Ligo Ashen Desert missions (AshenMissionDataV1, revision ashen_1_16_5): set 1, the lead-ins, Sergeant Bernard Ames'
    /// The Dustiest Place on Earth (1250), Captain Flynn Harris' Report to Stonewall Ridge (1273), Captain Marcus Emmons'
    /// Fithik Infestation (1274), Lt. Colonel Trent Ivan's Good Day to Die (1283) and Lt. Colonel Dania Caldwell's You Can't
    /// Drink Ash (1284), with Ames, Ivan, Caldwell and Captain Akio Yamamoto new and the upstream rows set 1 uses moved
    /// (Ashen_lead_ins); set 2, the Mauna Barrens, Lieutenant Simon Kuang's Angry Angry Atta (1242), Harmful Mutations (1243)
    /// and Chemical Oversight (1244) and Captain Sera Karter's Eyes on the Ground (1245), with Kuang, Doctor Jung Kwon, Karter
    /// and the Ashen Atta new and Thrax Overseer Exing's pool moved (Ashen_mauna_barrens); set 3, the Sheitan Highway, Karter's
    /// Highway to Hell (1247), Lt. Colonel Trent Ivan's Air Superiority (1248) and Lt. Colonel Dania Caldwell's Artillery
    /// Annihilation (1249), with the Thrax Ravager name on the Ashen Thrax Grenadier, the highway's Thrax pools, the Desert
    /// Predator on two beats and the highway's four mortars each its own row (Ashen_sheitan_highway); set 4, the Boneyard,
    /// Lieutenant Jan Blood's Xanxless Job (1262), Captain Akio Yamamoto's Fallen Soldiers (1263) and Lieutenant Gabriel
    /// Infanger's Stalking the Stalkers (1272), with Infanger, the Boneyard's Xanx pools and the Desert Stalker new
    /// (Ashen_boneyard); set 5, the Bane Conscription Base, Yamamoto's Death from Above (1352), Infanger's Turning Out the
    /// Lights (1354) and Blood's The Strongest Link (1355), with the trench mortars each their own row, the Linker pools new and
    /// Overseer Naxveik's pool moved (Ashen_conscription_base); set 6, Formal Complaints and its AFS side, Scavenger Vakiel
    /// Latan's Formal Complaints (1254, the choice of flag 250 or 251) and A Bribe for Your Thoughts (1259), Shinichiro Ito's The
    /// Human Perspective (1258), Yamamoto's Court Martial Arts (1261) and Megumi Yamamoto's Dearly Departed (1285), with
    /// Latan, Ito and Megumi new (Ashen_formal_complaints); set 7, Latan's side (flag 250), her Robotic Salvage Recovery (1264),
    /// Weapon Salvage Recovery (1266) and Heavy Salvage Recovery (1265), with the Nitroglazer and the Hominis Machina Mk XI new
    /// in the Boneyard (Ashen_scavenger); set 8, the AFS side (flag 251), Shinichiro Ito's Robotic Salvage Acquisitions (1267),
    /// Weapon Salvage Acquisitions (1269) and Heavy Salvage Acquisitions (1268), on the creatures of sets 3, 4 and 7
    /// (Ashen_logistician); set 9, Stonewall Ridge, Captain Marcus Emmons' Cycled Frequencies (1275), On The Same Frequency
    /// (1277) and King of the Hill (1278), scene actors on existing rows (Ashen_stonewall_ridge); set 10, Ida Hayner's orders,
    /// Designer Jewelry Gone Wrong (1287), All the Latest Rage! (1293), I Wouldn't Wear It, But... (1289) and A New Fad (1291),
    /// with Sally, Molly, Trudy, Wendy and the Lasher, Beam Manta and Granitour pools new and Ida Hayner moved off the weapon
    /// vendor (Ashen_designer_hides); set 11, White Oasis Post, Captain Daris Linier's Analyzing the Harvesters (1295) and Prying
    /// Eyes (1303), Doctor Meeks' Murky Waters (1298: Agent Hale's choice, flags 257 / 258) and Lieutenant Dania Fontecilla's Gas
    /// Shortages (1356), with Meeks and Hale new and Fontecilla moved off the control point's console (Ashen_white_oasis); set
    /// 12, Murky Waters' two sides, Meeks' Water Purification (1299, flag 258) and Hale's Neurotoxins (1300, flag 257) and
    /// Poisoning the Wellwater (1302), with two Lasher pools new on the ridge west of White Oasis Post (Ashen_murky_waters).
    ///
    /// Why these tests: the client's text ids and packages are written out here from the client tables, not taken from the
    /// data classes, so a wrong id fails; the NPC rows, rewards, prerequisites and markers are pinned; the migration rolls back
    /// to the one directly before it.
    ///
    /// [TestCategory]: none, as PlateauCampResistanceTests.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class AshenMissionTests
    {
        /// <summary>The Ashen Desert missions the World migrations enable on the Ashen revision, for the tests that pin the enabled set.</summary>
        internal static readonly uint[] MissionIds =
        {
            AshenDustiestPlaceV1.MissionId, AshenReportToStonewallV1.MissionId, AshenFithikInfestationV1.MissionId,
            AshenGoodDayToDieV1.MissionId, AshenCantDrinkAshV1.MissionId,
            AshenAngryAngryAttaV1.MissionId, AshenHarmfulMutationsV1.MissionId, AshenChemicalOversightV1.MissionId,
            AshenEyesOnTheGroundV1.MissionId,
            AshenHighwayToHellV1.MissionId, AshenAirSuperiorityV1.MissionId, AshenArtilleryAnnihilationV1.MissionId,
            AshenXanxlessJobV1.MissionId, AshenFallenSoldiersV1.MissionId, AshenStalkingTheStalkersV1.MissionId,
            AshenDeathFromAboveV1.MissionId, AshenTurningOutTheLightsV1.MissionId, AshenStrongestLinkV1.MissionId,
            AshenFormalComplaintsV1.MissionId, AshenHumanPerspectiveV1.MissionId, AshenBribeForYourThoughtsV1.MissionId,
            AshenCourtMartialArtsV1.MissionId, AshenDearlyDepartedV1.MissionId,
            AshenRoboticSalvageV1.MissionId, AshenWeaponSalvageV1.MissionId, AshenHeavySalvageV1.MissionId,
            AshenRoboticAcquisitionsV1.MissionId, AshenWeaponAcquisitionsV1.MissionId, AshenHeavyAcquisitionsV1.MissionId,
            AshenCycledFrequenciesV1.MissionId, AshenOnTheSameFrequencyV1.MissionId, AshenKingOfTheHillV1.MissionId,
            AshenDesignerJewelryV1.MissionId, AshenLatestRageV1.MissionId, AshenWouldntWearItV1.MissionId, AshenNewFadV1.MissionId,
            AshenAnalyzingHarvestersV1.MissionId, AshenMurkyWatersV1.MissionId, AshenPryingEyesV1.MissionId, AshenGasShortagesV1.MissionId,
            AshenWaterPurificationV1.MissionId, AshenNeurotoxinsV1.MissionId, AshenPoisoningTheWellwaterV1.MissionId,
            AshenMissingScientistsV1.MissionId, AshenGodOfStormsV1.MissionId, AshenBondsOfBloodV1.MissionId,
            // Set 14 (Echoes in the Sand 1736 is on the Crucible's revision: AshenAshokaTests pins it).
            AshenSupplyLogisticsV1.MissionId, AshenPreciousToolsV1.MissionId,
            // Set 15 (Indra Caverns, squad instance 2055).
            AshenIndraCavernsV1.MissionId, AshenThresholdV1.MissionId, AshenVindicationV1.MissionId,
            // Set 16 (Indra Caverns, the Gotha plant).
            AshenDiggingForTheTruthV1.MissionId, AshenMeetSergeantDrofV1.MissionId, AshenSubversionV1.MissionId, AshenDesistanceV1.MissionId,
            // Set 17 (Avernus Outpost, squad instance 2110, the outpost).
            AshenOverlySensitiveV1.MissionId, AshenPetrovichsLastStandV1.MissionId, AshenPetrovichsRunV1.MissionId,
            // Set 18 (Avernus Outpost, the hive).
            AshenAssessingTheAttaV1.MissionId, AshenGodSaveTheQueenV1.MissionId, AshenTheBigSecretV1.MissionId,
            // Set 19 (Bane Conscription Facility, squad instance 1988, the entry and the biolab).
            AshenLockdownTheConscriptionFacilityV1.MissionId, AshenRaidTheConscriptionFacilityV1.MissionId, AshenBlastTheBiolabV1.MissionId,
            // Set 20 (Bane Conscription Facility, the Atta queen; 1501 ends on Thunderhead).
            AshenWhatMakesThemTickV1.MissionId, AshenIntoTheLionsDenV1.MissionId, AshenSaveTheQueenV1.MissionId
        };

        /// <summary>
        /// Every visible objective has a marker (EveryVisibleObjectiveHasAMarker): set 1's five (each report's one at its
        /// receiver, Fithik Infestation's on the Stonewall Ridge Fithik pool); set 2's twelve (1242's four on the Atta Soldier and
        /// Worker pools, 1243's two on the Mutated Atta pools, 1244's on Exing, 1245's five on the beacon spots); set 3's ten
        /// (1247's four on the Thrax Ravager and Soldier pools, 1248's two on the Desert Predators' beats, 1249's four on the
        /// mortars); set 4's six (1262's three on the Xanx pools, 1263's one over the six fallen, 1272's two on the Desert Stalker
        /// pools); set 5's twenty (1352's four on the trench mortars, six on the launchers' Detonation Devices and one over the
        /// trench, 1354's three on the generators, 1355's five on the Linker pools and one on Naxveik); set 6's nine (1254's three
        /// at Latan, 1258's and 1259's at Yamamoto, 1261's at Mason Linier, 1285's three on the Boneyard's Thrax pools); set 7's
        /// thirteen (1264's four on the Nitroglazer and Hominis Machina pools, 1266's three on the Boneyard's Thrax pools and two
        /// on the highway's Ravager pools, 1265's two on the Desert Stalker pools and two on the Desert Predators' beats); set 8's
        /// thirteen (Ito's 1267, 1269 and 1268 on the places of Latan's 1264, 1266 and 1265); set 9's seven (1275's on the salvage
        /// crate, 1277's four at the towers' ground teleporters, 1278's two over the four batteries); set 10's fifteen (1287's four
        /// on set 2's Atta Soldier and Worker pools and one on Sally, 1293's two on the Granitour pools and one on Wendy, 1289's two
        /// on the Lasher pools and one on Molly, 1291's three on the Beam Manta pools and one on Trudy); set 11's eleven (1295's
        /// four on the panels, 1298's three at Hale, 1303's on Ulovik, 1356's three on the fuel pumps); set 12's ten (1299's two
        /// on set 10's Granitour pools, 1300's four on the Lasher pools, 1302's four on the panels); set 13's thirteen (1280's on Veyon's body, 1281's ten on the resistors, 1282's on
        /// Maiek's body and at Endala); fix F's one (1287's on its own Ashen Atta Soldier pool 790378); set 14's six (1358's five
        /// at the captains, 1286's on Jirex; 1736's two are on the Crucible's revision, AshenAshokaTests); set 15's one on Ashen
        /// Desert (1559's before the Indra Caverns door; its other nine are on Indra Caverns, CavernsIndicators); fix H's one
        /// (1559's second objective 1 marker, on the Indra Pass valley's lift pad 913).
        /// </summary>
        /// <summary>Echoes in the Sand (1736): an Ashen Desert mission on the Crucible's revision (the 1218 chain), not in MissionIds (the Ashen revision's); AshenAshokaTests pins it.</summary>
        internal static readonly uint[] OnCrucibleRevision = { AshenEchoesInTheSandV1.MissionId };

        /// <summary>1736's two markers (on the obelisk, at Botha), on the Crucible's revision.</summary>
        internal const int EchoesIndicators = 2;

        internal const int Indicators = 5 + 12 + 10 + 6 + 20 + 9 + 13 + 13 + 7 + 15 + 11 + 10 + 13 + 1 + 6 + 1 + 1;

        /// <summary>
        /// The markers on Indra Caverns (2055): set 15's (1559's on Diren, 1360's three, 1395's five: AshenIndraFacilityTests) and set
        /// 16's (1447's ten, 1532's one, 1451's eight, 1452's eight: AshenGothaPlantTests).
        /// </summary>
        internal const int CavernsIndicators = 1 + 3 + 5 + 10 + 1 + 8 + 8;

        /// <summary>The markers on Avernus Outpost (2110): set 17's sixteen (1473's eight, 1535's eight; 1533 has none: AshenAvernusOutpostTests.Markers).</summary>
        internal static readonly int OutpostIndicators = AshenAvernusOutpostTests.Markers().Count();

        /// <summary>Set 18's twelve markers on Avernus Outpost (1536's three, 1537's seven, 1581's two: AshenAvernusHiveTests.Markers).</summary>
        internal static readonly int HiveIndicators = AshenAvernusHiveTests.Markers().Count();

        /// <summary>Set 19's nine markers (1500's two, one before the door on Ashen Desert; 1497's two, 1498's five on the facility: AshenConscriptionEntryTests.Markers).</summary>
        internal static readonly int EntryIndicators = AshenConscriptionEntryTests.Markers().Count();

        /// <summary>Of them, on Ashen Desert: 1500's marker before the facility's door.</summary>
        internal const int EntryDesertIndicators = 1;

        /// <summary>Set 20's fourteen markers (1551's two, 1499's five, 1501's seven: one at Yamamoto on Ashen Desert, one at Jawan on Thunderhead; AshenConscriptionQueenTests.Markers).</summary>
        internal static readonly int QueenIndicators = AshenConscriptionQueenTests.Markers().Count();

        /// <summary>Of them, on Ashen Desert: 1501's at Yamamoto.</summary>
        internal const int QueenDesertIndicators = AshenConscriptionQueenTests.DesertMarkers;

        private const string LeadIns = "20271110000000_Ashen_lead_ins";
        private const string MaunaBarrens = "20271110000100_Ashen_mauna_barrens";
        private const string SheitanHighway = "20271110000200_Ashen_sheitan_highway";
        private const string Boneyard = "20271110000400_Ashen_boneyard";
        private const string ConscriptionBase = "20271110000500_Ashen_conscription_base";
        private const string FormalComplaints = "20271110000600_Ashen_formal_complaints";
        private const string Scavenger = "20271110000700_Ashen_scavenger";
        private const string FixD = "20271110000710_Ashen_fix_d";
        private const string Logistician = "20271110000800_Ashen_logistician";
        private const string StonewallRidge = "20271110000900_Ashen_stonewall_ridge";
        private const string DesignerHides = "20271110001000_Ashen_designer_hides";
        private const string WhiteOasis = "20271110001100_Ashen_white_oasis";
        private const string MurkyWaters = "20271110001200_Ashen_murky_waters";
        private const string IndraPass = "20271110001300_Ashen_indra_pass";
        private const string Ashoka = "20271110001400_Ashen_ashoka";
        private const string IndraCaverns = "20271110001500_Ashen_indra_caverns";
        internal const string GothaPlant = "20271110001600_Ashen_gotha_plant";
        internal const string AvernusOutpost = "20271110001700_Ashen_avernus_outpost";
        internal const string AvernusHive = "20271110001800_Ashen_avernus_hive";
        internal const string ConscriptionEntry = "20271110001900_Ashen_conscription_entry";
        internal const string ConscriptionQueen = "20271110002000_Ashen_conscription_queen";
        private const uint Map = 1734;
        private const double CrateX = 537, CrateZ = -267;     // 1275's crate by the Boneyard wreck at (537, 282, -279) (the probe of 2026-11-10)

        private const uint Ames = 790000, Ivan = 790001, Caldwell = 790002, Yamamoto = 790003;
        private const uint MasonLinier = 510010, Harris = 510008, Emmons = 510009, DarisLinier = 510007;
        private const uint Kuang = 790040, Kwon = 790041, Karter = 790042;
        private const uint AttaSoldier = 790043, AttaWorker = 790044, MutatedSoldier = 790045, Exing = 520007;
        private const uint Ravager = 531009, ThraxSoldier = 531008, Predator = 790080, Mortar = 592011;
        private static readonly uint[] MortarCopies = { 790081, 790082, 790083, 790084 };
        private static readonly uint[] MortarPools = { 592191, 592192, 592193, 592194 };
        private const uint Blood = 510013, Infanger = 790120, Xanx = 540020, Stalker = 790121;
        private static readonly uint[] XanxPools = { 790122, 790123, 790124 };
        private static readonly uint[] StalkerPools = { 790125, 790126 };
        private static readonly uint[] TrenchCopies = { 790160, 790161, 790162, 790163 };
        private static readonly uint[] TrenchPools = { 592199, 592202, 592203, 592204 };
        private static readonly uint[] LinkerPools = { 790164, 790165, 790166, 790167, 790168 };
        private const uint Linker = 531014, Naxveik = 520005;
        private const uint Latan = 790200, Ito = 790201, Megumi = 790202;
        private const uint Nitroglazer = 790240, Machina = 790241;
        private const uint Ida = 510011, Sally = 790360, Lasher = 790362, Molly = 790365, Manta = 790367, Trudy = 790371, Granitour = 790373, Wendy = 790376;
        private const uint Meeks = 790400, Hale = 790401, Fontecilla = 510012, Ulovik = 520006;
        private const uint Odesha = 790480, Endala = 790481;
        private static readonly uint[] NitroPools = { 790243, 790244 };
        private static readonly uint[] MachinaPools = { 790245, 790246 };

        /// <summary>The migration directly before <paramref name="migration"/> (IMigrationsAssembly, ordinal order), as CrucibleMissionTests.Previous.</summary>
        internal static string Previous(DbContext world, string migration) => Rasa.Test.Missions.Crucible.CrucibleMissionTests.Previous(world, migration);

        /// <summary>
        /// The Ashen Desert pools that the migrations sorting after <paramref name="migration"/> add or change: <paramref
        /// name="world"/>'s rows (all migrations) against a database migrated only to <paramref name="migration"/>. A rollback
        /// test that goes down past them and up again only to its own migration leaves these out of its "no other pool changes".
        /// </summary>
        internal static uint[] PoolsOfLaterMigrations(DbContext world, string migration)
        {
            static Dictionary<uint, string> Rows(DbContext context) => context.Set<SpawnPoolEntry>().AsNoTracking()
                .Where(entry => entry.MapContextId == AshenMissionDataV1.MapContextId).AsEnumerable()
                .ToDictionary(entry => entry.Id, entry => new { entry.PosX, entry.PosY, entry.PosZ, entry.Radius, entry.Creature1Id, entry.RespawnTime }.ToString());
            var all = Rows(world);
            Dictionary<uint, string> before;
            using (var fresh = WildernessRuntimeTestHarness.Create(targetWorldMigration: migration))
                before = Rows(fresh.World);
            return all.Where(row => !before.TryGetValue(row.Key, out var was) || was != row.Value).Select(row => row.Key)
                .Concat(before.Keys.Where(id => !all.ContainsKey(id))).OrderBy(id => id).ToArray();
        }

        [TestMethod]
        public void TheMissionsAreEnabledOperationalAndGivenByTheirNpcs()
        {
            var expected = new (uint Mission, uint? Giver, uint? Receiver, uint Level)[]
            {
                (1250, Ames, MasonLinier, 40), (1273, Harris, Emmons, 40), (1274, Emmons, Emmons, 40), (1283, Ivan, Yamamoto, 40),
                (1284, Caldwell, DarisLinier, 40), (1242, Kuang, Kuang, 40), (1243, Kuang, Kwon, 40), (1244, Kuang, Kuang, 41),
                (1245, Karter, Karter, 41), (1247, Karter, Karter, 41), (1248, Ivan, Ivan, 42), (1249, Caldwell, Caldwell, 41),
                (1262, Blood, Blood, 40), (1263, Yamamoto, Yamamoto, 40), (1272, Infanger, Infanger, 41),
                (1352, Yamamoto, Yamamoto, 41), (1354, Infanger, Infanger, 41), (1355, Blood, Blood, 42),
                (1254, Latan, Latan, 40), (1258, Ito, Latan, 41), (1259, Latan, Yamamoto, 41), (1261, Yamamoto, MasonLinier, 41),
                (1285, Megumi, Yamamoto, 42),
                (1264, Latan, Latan, 40), (1266, Latan, Latan, 41), (1265, Latan, Latan, 42),
                (1267, Ito, Ito, 42), (1269, Ito, Ito, 42), (1268, Ito, Ito, 42),
                (1275, Emmons, Emmons, 40), (1277, Emmons, Emmons, 41), (1278, Emmons, Emmons, 42),
                (1287, Ida, Ida, 41), (1293, Ida, Ida, 42), (1289, Ida, Ida, 41), (1291, Ida, Ida, 42),
                (1295, DarisLinier, Meeks, 41), (1298, Meeks, Hale, 41), (1303, DarisLinier, DarisLinier, 41), (1356, Fontecilla, Fontecilla, 41),
                (1299, Meeks, Meeks, 42), (1300, Hale, Hale, 42), (1302, Hale, Hale, 42),
                (1280, Odesha, Odesha, 41), (1281, Odesha, Odesha, 42), (1282, Endala, Endala, 42),
                (1358, AshenAshokaNpcsV1.Chase, AshenAshokaNpcsV1.Chase, 41), (1286, AshenIndraPassNpcsV1.Raji, AshenIndraPassNpcsV1.Raji, 42),
                (1559, Odesha, AshenIndraFacilityNpcsV1.Diren, 42), (1360, AshenIndraFacilityNpcsV1.Diren, AshenIndraFacilityNpcsV1.Drof, 43),
                (1395, AshenIndraFacilityNpcsV1.Drof, AshenIndraFacilityNpcsV1.Drof, 43),
                (1447, AshenIndraFacilityNpcsV1.Drof, null, 43), (1532, null, AshenGothaPlantNpcsV1.Drof, 43),
                (1451, AshenGothaPlantNpcsV1.Drof, AshenGothaPlantNpcsV1.Drof, 44), (1452, AshenGothaPlantNpcsV1.Naimi, AshenIndraFacilityNpcsV1.Diren, 44)
            };
            using var harness = WildernessRuntimeTestHarness.Create();
            var enabled = harness.World.MissionContentDefinitionEntries.AsNoTracking()
                .Where(entry => entry.Enabled && entry.ContentRevision == AshenMissionDataV1.Revision).ToArray();
            CollectionAssert.AreEquivalent(MissionIds, enabled.Select(entry => entry.MissionId).ToArray());
            foreach (var (mission, giver, receiver, level) in expected)
            {
                var entry = enabled.Single(row => row.MissionId == mission);
                Assert.AreEqual<uint?>(giver, entry.GiverId, $"{mission} giver");
                Assert.AreEqual<uint?>(receiver, entry.ReceiverId, $"{mission} receiver");
                Assert.AreEqual<uint?>(level, entry.Level, $"{mission} level");
                Assert.AreEqual<uint?>(10000016U, entry.CategoryId, "Battlefield (Ashen Desert), missioncategorylanguage 10000016");
                // 1355 shareable: TaRapedia updates 2007-08-21 lists "Mission is not shareable" as a known issue.
                Assert.AreEqual(mission == 1355, entry.Shareable, $"{mission} shareable");
            }
            Assert.IsFalse(harness.Manager.LoadMissions().BlocksReadiness);
            foreach (var id in MissionIds)
                Assert.IsTrue(harness.Manager.LoadedMissions[id].IsOperational,
                    $"{id}: {harness.Manager.LoadedMissions[id].OperationalDiagnostic}");
        }

        /// <summary>
        /// The new NPCs: the client's names (creaturenamelanguage 9279, 9261, 9262, 9303) and packages (objectiveconversation
        /// 1283,1 1414; Ames', Ivan's and Caldwell's server-only), friendly, standing still, with their own hit points, on Ashen
        /// Desert; the upstream rows set 1 uses at their new places with their packages (1389, 1412, 1411, 1476).
        /// </summary>
        [TestMethod]
        public void TheNpcsHaveTheClientsNamesAndPackages()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var world = harness.World;
            foreach (var (npc, name, package, cls, level) in new (uint, uint, uint, uint, uint)[]
            {
                (Ames, 9279, 790000, 3846, 40), (Ivan, 9261, 790001, 3846, 40), (Caldwell, 9262, 790002, 3848, 40), (Yamamoto, 9303, 1414, 3846, 49),
                (Kuang, 9291, 790040, 3846, 40), (Kwon, 9298, 790041, 3848, 40), (Karter, 9281, 790042, 3848, 48),
                (Infanger, 9304, 790120, 3846, 40),
                // Set 6: Latan's package 1449 (objectiveconversation 1254,1-3), Ito's and Megumi's server-only; Latan Brann (class
                // 7776, Raji Lasat's), Ito AFS, Megumi the female AFS class.
                (Latan, 9277, 1449, 7776, 40), (Ito, 9272, 790201, 3846, 43), (Megumi, 9295, 790202, 3848, 42),
                // Set 11: Hale's package 1478 (objectiveconversation 1298,1-3), Meeks' server-only; both AFS (Harris' and Emmons' class).
                (Meeks, 9356, 790400, 3846, 40), (Hale, 9357, 1478, 3846, 40),
                // Set 13: Endala's package 1475 (objectiveconversation 1282,2), Odesha's server-only; both Brann (Raji Lasat's class
                // 7776), level 43 (TaRapedia's Endala).
                (Odesha, 9355, 790480, 7776, 43), (Endala, 9354, 1475, 7776, 43)
            })
            {
                var creature = world.CreatureEntries.AsNoTracking().Single(row => row.Id == npc);
                Assert.AreEqual((name, level, 1U, cls, 0U, 0U), (creature.NameId, creature.Level, creature.Faction, creature.ClassId,
                    creature.RunSpeed, creature.WalkSpeed), $"{npc}");
                Assert.AreEqual(package, world.NpcPackageEntries.AsNoTracking().Single(row => row.Id == npc).PackageId, $"{npc} package");
                var stat = world.CreatureStatEntries.AsNoTracking().Single(row => row.Id == npc);
                Assert.AreEqual((4500, 300), (stat.Health, stat.Armor), $"{npc} stats");
                Assert.IsTrue(world.CreatureAppearanceEntries.AsNoTracking().Any(row => row.Id == npc), $"{npc} has a look");
                var pool = world.SpawnPoolEntries.AsNoTracking().Single(row => row.Id == npc);
                Assert.AreEqual((Map, npc, (byte)1, (byte)1), (pool.MapContextId, pool.Creature1Id, pool.Creature1MinCount, pool.Creature1MaxCount), $"{npc} pool");
            }

            // The places (TaRapedia's for Yamamoto; the probe's by the client statics for the others).
            foreach (var (pool, x, z) in new (uint, double, double)[]
            {
                (Ames, 345, -818), (Ivan, -97, -792), (Caldwell, -158, -792), (Yamamoto, 686, -495), (MasonLinier, -132, -875),
                (Harris, 405, -459.5), (Emmons, -62, -414), (DarisLinier, -507.4, 162.4), (Kuang, -205, -736), (Kwon, -89, -822),
                (Karter, -112, -748), (Exing, -260, -310), (Infanger, 676, -505), (Blood, 682.4, -516.4), (Naxveik, 296, -30),
                (Latan, 576, -321), (Ito, 560, -320), (Megumi, -166, -895),
                (Meeks, -498, 164), (Hale, -516, 176), (Fontecilla, -519, 164), (Ulovik, -235, 335),
                // Set 13's NPCs at the Indra Pass valley's south entry (fix G; set 13 had them at (258, 325), (266, 337)).
                (Odesha, 115, 450), (Endala, 112, 458)
            })
            {
                var row = world.SpawnPoolEntries.AsNoTracking().Single(entry => entry.Id == pool);
                Assert.IsLessThan(0.01, System.Math.Abs(row.PosX - x) + System.Math.Abs(row.PosZ - z), $"pool {pool} at ({x}, {z})");
                Assert.AreEqual(Map, row.MapContextId, $"pool {pool} on Ashen Desert");
            }
            var packages = world.NpcPackageEntries.AsNoTracking().Where(row => row.Id == MasonLinier || row.Id == Harris || row.Id == Emmons ||
                row.Id == DarisLinier).AsEnumerable().ToDictionary(row => row.Id, row => row.PackageId);
            Assert.AreEqual((1389U, 1412U, 1411U, 1476U), (packages[MasonLinier], packages[Harris], packages[Emmons], packages[DarisLinier]));
            Assert.AreEqual(1, world.NpcPackageEntries.AsNoTracking().Count(row => row.PackageId == 1414), "only Yamamoto has 1414");
            Assert.AreEqual(1, world.NpcPackageEntries.AsNoTracking().Count(row => row.PackageId == 1449), "only Latan has 1449");
            // Set 4: Blood gives and takes 1262 by his id; the client gives him no talk in it, and he keeps upstream's row (no package).
            var blood = world.CreatureEntries.AsNoTracking().Single(row => row.Id == Blood);
            Assert.AreEqual((9305U, 1U), (blood.NameId, blood.Faction), "Lieutenant Jan Blood, friendly");
            Assert.IsFalse(world.NpcPackageEntries.AsNoTracking().Any(row => row.Id == Blood), "Blood: no package, as upstream");
        }

        /// <summary>
        /// The client's own texts: the ids are written out here from the client tables (missionconversation,
        /// missionobjective, objectiveconversation), not taken from the data classes, so a wrong id in those fails.
        /// </summary>
        [TestMethod]
        public void TitlesAndObjectivesUseTheClientsTexts()
        {
            var expected = new (uint Mission, uint Title, (uint Objective, uint Name, uint Body, uint? Counter)[] Objectives)[]
            {
                (1250, 10000, new (uint, uint, uint, uint?)[] { (1, 10005, 10006, null) }),
                (1273, 10232, new (uint, uint, uint, uint?)[] { (1, 10237, 10238, null) }),
                (1274, 10240, new (uint, uint, uint, uint?)[] { (1, 10245, 10246, 10247) }),
                (1283, 10354, new (uint, uint, uint, uint?)[] { (1, 10359, 10360, null) }),
                (1284, 10362, new (uint, uint, uint, uint?)[] { (1, 10367, 10368, null) }),
                (1242, 9914, new (uint, uint, uint, uint?)[] { (1, 9919, 9920, 9921), (2, 9922, 9923, 9924) }),
                (1243, 9925, new (uint, uint, uint, uint?)[] { (1, 9930, 9931, null) }),
                (1244, 9932, new (uint, uint, uint, uint?)[] { (1, 9937, 9938, null) }),
                (1245, 9939, new (uint, uint, uint, uint?)[]
                {
                    (1, 9944, 9945, null), (2, 9946, 9947, null), (3, 9948, 9949, null), (4, 9950, 9951, null), (5, 9952, 9953, null)
                }),
                (1247, 9962, new (uint, uint, uint, uint?)[] { (1, 9967, 9968, 9969), (3, 9973, 9974, 9975) }),
                (1248, 9976, new (uint, uint, uint, uint?)[] { (1, 9981, 9982, 9983) }),
                (1249, 9984, new (uint, uint, uint, uint?)[] { (1, 9989, 9990, null), (2, 9991, 9992, null), (3, 9993, 9994, null), (4, 9995, 9996, null) }),
                (1262, 10121, new (uint, uint, uint, uint?)[] { (1, 10126, 10127, 10128) }),
                // 1263's 2 is server-only and hidden: it carries 1's texts (the client has no objective 2).
                (1263, 10136, new (uint, uint, uint, uint?)[] { (1, 10141, 10142, null), (2, 10141, 10142, null) }),
                (1272, 10224, new (uint, uint, uint, uint?)[] { (1, 10229, 10230, 10231) }),
                (1352, 11060, new (uint, uint, uint, uint?)[]
                {
                    (1, 11065, 11066, null), (2, 11067, 11068, null), (3, 11069, 11070, null), (4, 11071, 11072, null),
                    (5, 11073, 11074, null), (6, 11075, 11076, null), (7, 11077, 11078, null), (8, 11079, 11080, null),
                    (9, 11081, 11082, null), (10, 11083, 11084, null), (11, 11085, 11086, 11087)
                }),
                (1354, 11133, new (uint, uint, uint, uint?)[] { (1, 11333, 11334, null), (2, 11335, 11336, null), (3, 11337, 11338, null) }),
                (1355, 11144, new (uint, uint, uint, uint?)[] { (1, 11424, 11425, 11426), (2, 11427, 11428, 11429) }),
                // 1254's 4 is server-only and hidden: it carries 1's texts (the client has no objective 4).
                (1254, 10049, new (uint, uint, uint, uint?)[] { (1, 10054, 10055, null), (2, 10060, 10061, null), (3, 10063, 10064, null), (4, 10054, 10055, null) }),
                (1258, 10092, new (uint, uint, uint, uint?)[] { (1, 10097, 10098, null) }),
                (1259, 10100, new (uint, uint, uint, uint?)[] { (1, 10110, 10111, null) }),
                (1261, 10113, new (uint, uint, uint, uint?)[] { (1, 10118, 10119, null) }),
                (1285, 10370, new (uint, uint, uint, uint?)[] { (1, 10375, 10376, null) }),
                (1264, 10143, new (uint, uint, uint, uint?)[] { (1, 10173, 10174, null), (2, 10175, 10176, null) }),
                (1265, 10148, new (uint, uint, uint, uint?)[] { (1, 10177, 10178, null), (2, 10179, 10180, null) }),
                (1266, 10153, new (uint, uint, uint, uint?)[] { (1, 10181, 10182, null), (2, 10183, 10184, null) }),
                (1267, 10158, new (uint, uint, uint, uint?)[] { (1, 10185, 10186, null), (2, 10187, 10188, null) }),
                (1268, 10163, new (uint, uint, uint, uint?)[] { (1, 10189, 10190, null), (2, 10191, 10192, null) }),
                (1269, 10168, new (uint, uint, uint, uint?)[] { (1, 10193, 10194, null), (2, 10195, 10196, null) }),
                // Set 9: 1277's 5 "Ambushes" and 1278's 3-6 (the waves) are the client's helpers, not written.
                (1275, 10248, new (uint, uint, uint, uint?)[] { (1, 10263, 10264, null) }),
                (1277, 10265, new (uint, uint, uint, uint?)[] { (1, 10270, 10271, null), (2, 10272, 10273, null), (3, 10274, 10275, null), (4, 10276, 10277, null) }),
                // 1278's 7 is server-only and hidden (the losses' tally): it carries 2's texts; 2 has no counter text (a counter
                // rule only completes an objective, so the client's 10295 "Towers Lost" is not written: a deviation).
                (1278, 10286, new (uint, uint, uint, uint?)[] { (1, 10291, 10292, null), (2, 10293, 10294, null), (7, 10293, 10294, null) }),
                // Set 10: no counter texts (missionobjective 1287-1293 have none).
                (1287, 10384, new (uint, uint, uint, uint?)[] { (1, 10389, 10390, null), (2, 11045, 11046, null) }),
                (1289, 10398, new (uint, uint, uint, uint?)[] { (1, 10403, 10404, null), (2, 11056, 11057, null) }),
                (1291, 10412, new (uint, uint, uint, uint?)[] { (1, 10417, 10418, null), (2, 11058, 11059, null) }),
                (1293, 10426, new (uint, uint, uint, uint?)[] { (1, 10431, 10432, null), (2, 11054, 11055, null) }),
                // Set 11: no counter texts. 1298's 4 is server-only and hidden (the outcome of the choice): it carries 1's texts.
                (1295, 10442, new (uint, uint, uint, uint?)[] { (1, 11854, 11855, null), (2, 11856, 11857, null), (3, 11858, 11859, null), (4, 11860, 11861, null) }),
                (1298, 10476, new (uint, uint, uint, uint?)[] { (1, 10481, 10482, null), (2, 10487, 10488, null), (3, 10490, 10491, null), (4, 10481, 10482, null) }),
                (1303, 10533, new (uint, uint, uint, uint?)[] { (1, 10538, 10539, null) }),
                (1356, 11149, new (uint, uint, uint, uint?)[] { (1, 11342, 11343, null), (2, 11344, 11345, null), (3, 11346, 11347, null) }),
                // Set 12: no counter texts.
                (1299, 10493, new (uint, uint, uint, uint?)[] { (1, 10498, 10499, null) }),
                (1300, 10500, new (uint, uint, uint, uint?)[] { (1, 10505, 10506, null) }),
                (1302, 10520, new (uint, uint, uint, uint?)[] { (1, 11862, 11863, null), (2, 11864, 11865, null), (3, 11866, 11867, null), (4, 11868, 11869, null) }),
                // Set 13 (missionobjective 1280-1282): 1281's ten "Sabotage Resistor N" and the hidden "Flag Reader" with the
                // client's counter 11177 "Resistors Programmed"; 1282's hidden "Flag Reader" without one.
                (1280, 10312, new (uint, uint, uint, uint?)[] { (1, 10317, 10318, null) }),
                (1281, 10319, new (uint, uint, uint, uint?)[]
                {
                    (1, 10334, 10335, null), (2, 10336, 10337, null), (3, 10338, 10339, null), (4, 10340, 10341, null), (5, 10342, 10343, null),
                    (6, 10344, 10345, null), (7, 10346, 10347, null), (8, 10348, 10349, null), (9, 10350, 10351, null), (10, 10352, 10353, null),
                    (11, 11175, 11176, 11177)
                }),
                (1282, 10324, new (uint, uint, uint, uint?)[] { (1, 10329, 10330, null), (2, 10331, 10332, null), (3, 11173, 11174, null) }),
                // Set 14 (missionobjective 1358, 1286): no counter texts.
                (1358, 11159, new (uint, uint, uint, uint?)[] { (1, 11452, 11453, null), (2, 11455, 11456, null), (3, 11458, 11459, null), (4, 11461, 11462, null), (5, 11464, 11465, null) }),
                (1286, 10377, new (uint, uint, uint, uint?)[] { (1, 10382, 10383, null) }),
                // Set 15 (missionobjective 1559, 1360, 1395; 1395 has no objective 1): no counter texts; the server-only hidden
                // helpers carry a client objective's texts (1360's 4-6 its 1's, 1395's 7 its 2's and 8 its 4's).
                (1559, 14171, new (uint, uint, uint, uint?)[] { (1, 14207, 14208, null), (2, 14209, 14210, null) }),
                (1360, 11183, new (uint, uint, uint, uint?)[] { (1, 11331, 11332, null), (2, 11339, 11340, null), (3, 11707, 11708, null),
                    (4, 11331, 11332, null), (5, 11331, 11332, null), (6, 11331, 11332, null) }),
                (1395, 12023, new (uint, uint, uint, uint?)[] { (2, 12176, 12177, null), (3, 12178, 12179, null), (4, 12180, 12181, null),
                    (5, 12191, 12192, null), (6, 12241, 12242, null), (7, 12176, 12177, null), (8, 12180, 12181, null) }),
                // Set 16 (missionobjective 1447, 1532, 1451, 1452; 1447 has no objective 4): 1447's 12-14 and 1452's 9 the
                // server-only helpers with a client objective's texts (1447's 5's, 1452's 6's).
                (1447, 12708, new (uint, uint, uint, uint?)[] { (1, 12713, 12714, 12715), (2, 12724, 12725, null), (3, 12878, 12879, null),
                    (5, 12882, 12883, null), (6, 13708, 13709, null), (7, 13710, 13711, null), (8, 13712, 13713, null), (9, 13714, 13715, null),
                    (10, 13716, 13717, null), (11, 13718, 13719, null), (12, 12882, 12883, null), (13, 12882, 12883, null), (14, 12882, 12883, null) }),
                (1532, 13691, new (uint, uint, uint, uint?)[] { (1, 13701, 13702, null) }),
                (1451, 12889, new (uint, uint, uint, uint?)[] { (1, 12896, 12897, 12898), (2, 12899, 12900, 12901), (3, 13735, 13736, null),
                    (4, 13737, 13738, null), (5, 13739, 13740, null), (6, 13741, 13742, null), (7, 13743, 13744, null), (8, 13745, 13746, null) }),
                (1452, 12902, new (uint, uint, uint, uint?)[] { (1, 12909, 12910, null), (2, 12911, 12912, null), (3, 12913, 12914, null),
                    (4, 12907, 12908, null), (5, 12915, 12916, null), (6, 12917, 12918, null), (7, 12919, 12920, null), (8, 17113, 17114, null),
                    (9, 12917, 12918, null) }),
                // Set 17 (missionobjective 1473, 1535; 1533 has none): the client's counters 13919, 13922, 13930, 13925; the
                // server-only hidden helpers carry a client objective's texts (1473's 7 its 5's; 1535's 6, 7 its 4's, 8-10 its 1's;
                // 1533's one the mission's log and offer, as Crucible 1525's).
                (1473, 13094, new (uint, uint, uint, uint?)[] { (1, 13915, 13916, null), (2, 13917, 13918, 13919), (3, 13926, 13927, null),
                    (4, 13920, 13921, 13922), (5, 13928, 13929, 13930), (6, 13923, 13924, 13925), (7, 13928, 13929, null) }),
                (1533, 13696, new (uint, uint, uint, uint?)[] { (1, 13697, 13698, null) }),
                (1535, 13754, new (uint, uint, uint, uint?)[] { (1, 13759, 13760, null), (2, 13761, 13762, null), (3, 13763, 13764, null),
                    (4, 13766, 13767, null), (5, 13768, 13769, null), (6, 13766, 13767, null), (7, 13766, 13767, null), (8, 13759, 13760, null),
                    (9, 13759, 13760, null), (10, 13759, 13760, null) }),
                // Set 18 (missionobjective 1536, 1537, 1581): no counters.
                (1536, 13804, new (uint, uint, uint, uint?)[] { (1, 13809, 13810, null), (2, 13811, 13812, null), (3, 13814, 13815, null) }),
                (1537, 13820, new (uint, uint, uint, uint?)[] { (1, 13825, 13826, null), (2, 13835, 13836, null), (3, 13837, 13838, null),
                    (4, 13840, 13841, null), (5, 13854, 13855, null), (6, 13857, 13858, null), (7, 13859, 13860, null) }),
                (1581, 14557, new (uint, uint, uint, uint?)[] { (1, 17157, 17158, null), (2, 17159, 17160, null) }),
                // Set 19 (missionobjective 1500, 1497, 1498): 1498,6 counts 14063; 1498,7 (hidden, the opener) has 6's texts.
                (1500, 13394, new (uint, uint, uint, uint?)[] { (1, 14058, 14059, null) }),
                (1497, 13379, new (uint, uint, uint, uint?)[] { (1, 14011, 14012, null), (2, 14013, 14014, null) }),
                (1498, 13384, new (uint, uint, uint, uint?)[] { (1, 14064, 14065, null), (2, 14066, 14067, null), (3, 14068, 14069, null),
                    (4, 14070, 14071, null), (5, 14073, 14074, null), (6, 14061, 14062, 14063), (7, 14061, 14062, null) }),
                // Set 20 (missionobjective 1551, 1499, 1501): 1551,1 counts 14082 (its 2's 14085 not written); 1551,3 (hidden, the queen's
                // kill) has 2's texts; 1499,7 (hidden, the ledge) 4's, 1499,8 (hidden, either talk) 2's.
                (1551, 14075, new (uint, uint, uint, uint?)[] { (1, 14080, 14081, 14082), (2, 14083, 14084, null), (3, 14083, 14084, null) }),
                (1499, 13389, new (uint, uint, uint, uint?)[] { (1, 14300, 14301, null), (2, 14302, 14303, null), (3, 14304, 14305, null),
                    (4, 14309, 14310, null), (5, 14306, 14307, null), (6, 14311, 14312, null), (7, 14309, 14310, null), (8, 14302, 14303, null), (9, 14304, 14305, null) }),
                (1501, 13399, new (uint, uint, uint, uint?)[] { (1, 14284, 14285, null), (2, 14287, 14288, null), (3, 17309, 17310, null),
                    (4, 17312, 17313, null), (5, 17323, 17324, null), (6, 17326, 17327, null), (7, 17337, 17338, null) })
            };
            using var harness = WildernessRuntimeTestHarness.Create();
            foreach (var (mission, title, objectives) in expected)
            {
                Assert.AreEqual(title, harness.World.MissionContentDefinitionEntries.AsNoTracking()
                    .Single(entry => entry.MissionId == mission && entry.ContentRevision == AshenMissionDataV1.Revision)
                    .ClientNameTextId, $"{mission} title");
                var actual = harness.World.MissionObjectiveDefinitionEntries.AsNoTracking()
                    .Where(entry => entry.MissionId == mission && entry.ContentRevision == AshenMissionDataV1.Revision).ToArray();
                CollectionAssert.AreEquivalent(objectives.Select(row => row.Objective).ToArray(),
                    actual.Select(row => row.ObjectiveId).ToArray(), $"{mission} objectives");
                foreach (var (objective, name, body, counter) in objectives)
                {
                    var row = actual.Single(entry => entry.ObjectiveId == objective);
                    Assert.AreEqual(name, row.ClientNameTextId, $"{mission}.{objective} name");
                    Assert.AreEqual(body, row.ClientBodyTextId, $"{mission}.{objective} body");
                    Assert.AreEqual(counter, row.ClientCounter0TextId, $"{mission}.{objective} counter0");
                    Assert.IsNull(row.ClientCounter1TextId, $"{mission}.{objective} counter1");
                    Assert.IsNull(row.ClientCounter2TextId, $"{mission}.{objective} counter2");
                }
            }

            // The talks at the client's packages (objectiveconversation 1250,1 1389; 1273,1 1411; 1283,1 1414; 1284,1 1476), each
            // with the client's variant 1. Set 6: 1254,1 Latan's choice (1449, variant 1, a trigger for each of its two
            // choices), 1254,2 1449 variant 250, 1254,3 1449 variant 251; 1258,1 and 1259,1 Yamamoto (1414) and 1261,1 Mason
            // Linier (1389), variant 251: the client's variant is the player flag of Formal Complaints' choice.
            var talks = harness.World.Set<MissionTriggerEntry>().AsNoTracking()
                .Where(entry => entry.ContentRevision == AshenMissionDataV1.Revision && entry.Kind == MissionTriggerKind.Conversation)
                .AsEnumerable().Select(entry => (entry.MissionId, entry.ObjectiveId, entry.NpcPackageId, entry.PlayerFlagId))
                .OrderBy(entry => entry.MissionId).ThenBy(entry => entry.ObjectiveId).ToArray();
            CollectionAssert.AreEqual(new (uint, uint, uint?, uint?)[]
            {
                (1250, 1, 1389, 1), (1254, 1, 1449, 1), (1254, 1, 1449, 1), (1254, 2, 1449, 250), (1254, 3, 1449, 251),
                (1258, 1, 1414, 251), (1259, 1, 1414, 251), (1261, 1, 1389, 251),
                (1273, 1, 1411, 1),
                // Set 13: 1282,2 Endala (1475, the client's variant 1).
                (1282, 2, 1475, 1),
                (1283, 1, 1414, 1), (1284, 1, 1476, 1),
                // Set 11: 1298,1 Hale's choice (1478, variant 1, a trigger for each of its two choices), 1298,2 1478 variant 257
                // (the poison), 1298,3 1478 variant 258 (the repair).
                (1298, 1, 1478, 1), (1298, 1, 1478, 1), (1298, 2, 1478, 257), (1298, 3, 1478, 258),
                // Set 14: 1358's five deliveries (objectiveconversation 1358,1-5: 1410 Botha, 1412 Harris, 1411 Emmons, 1414
                // Yamamoto, 1476 Daris Linier; variant 1).
                (1358, 1, 1410, 1), (1358, 2, 1412, 1), (1358, 3, 1411, 1), (1358, 4, 1414, 1), (1358, 5, 1476, 1),
                // Set 15: 1360,2 Drof (1544) and 1395,3 Naimi (1651), variant 1.
                (1360, 2, 1544, 1), (1395, 3, 1651, 1),
                // Set 16: 1532,1 Drof (1544) and 1452,8 Diren (1802), variant 1.
                (1452, 8, 1802, 1),
                // Set 19: 1497,2 the terminal (1929), 1498,4 Slocombe (1933), 1500,1 Peacock (1928), variant 1. Set 20: 1499,5 Humphries
                // (1936), 1499,6 Lukaz (1934), 1501,3 and 1501,5 the queen (1937), 1501,6 Yamamoto (1414), 1501,7 Jawan (1648), variant 1.
                (1497, 2, 1929, 1), (1498, 4, 1933, 1), (1499, 5, 1936, 1), (1499, 6, 1934, 1), (1500, 1, 1928, 1),
                (1501, 3, 1937, 1), (1501, 5, 1937, 1), (1501, 6, 1414, 1), (1501, 7, 1648, 1),
                (1532, 1, 1544, 1),
                // Set 17: 1535,3 Petrovich (1803), variant 1.
                (1535, 3, 1803, 1),
                // Set 18: 1536,2 Tananda (1914), 1536,3 Petrovich (1803), 1537,3/5/7 Vandeweghe (1925), 1581,2 Zuriel (2075), variant 1.
                (1536, 2, 1914, 1), (1536, 3, 1803, 1), (1537, 3, 1925, 1), (1537, 5, 1925, 1), (1537, 7, 1925, 1),
                // Set 20: 1551,2 the DNA sample (1937), variant 1.
                (1551, 2, 1937, 1),
                (1581, 2, 2075, 1)
            }, talks);

            // Set 11: 1295's four panels and 1356's three pumps are scene results 1-4 and 1-3 (counter 1); 1303 Ulovik 520006, once.
            var oasis = harness.World.Set<MissionTriggerEntry>().AsNoTracking()
                .Where(entry => entry.ContentRevision == AshenMissionDataV1.Revision && (entry.MissionId == 1295 || entry.MissionId == 1303 || entry.MissionId == 1356) &&
                                entry.Kind == MissionTriggerKind.ProgressEvent).AsEnumerable()
                .Select(entry => (entry.MissionId, entry.ObjectiveId, entry.EventKind, entry.SubjectId, entry.CounterId, entry.TargetValue))
                .OrderBy(entry => entry.MissionId).ThenBy(entry => entry.ObjectiveId).ToArray();
            var scenario = (byte?)MissionProgressEventKind.ScenarioEvent;
            CollectionAssert.AreEqual(Enumerable.Range(1, 4).Select(n => (1295U, (uint)n, scenario, (uint?)n, (uint?)1, (uint?)null))
                .Append((1303U, 1U, (byte?)MissionProgressEventKind.CreatureKilled, (uint?)Ulovik, (uint?)null, (uint?)null))
                .Concat(Enumerable.Range(1, 3).Select(n => (1356U, (uint)n, scenario, (uint?)n, (uint?)1, (uint?)null))).ToArray(), oasis);

            // Set 13: 1280's body scene result 1 (counter 1); 1281's resistors scene results 1-10 of scenario 1, each its own
            // objective, and the hidden 11 counts the ten of scenario 2 (each its own trigger), to 10; the fork's closing (scenario
            // 19, each open objective's transition 2) on 1281's eleven and 1282's three; 1282's body scene result 1.
            var indra = harness.World.Set<MissionTriggerEntry>().AsNoTracking()
                .Where(entry => entry.ContentRevision == AshenMissionDataV1.Revision && entry.MissionId >= 1280 && entry.MissionId <= 1282 &&
                                entry.Kind == MissionTriggerKind.ProgressEvent).AsEnumerable()
                .Select(entry => (entry.MissionId, entry.ObjectiveId, entry.TransitionId, entry.EventKind, entry.SubjectId, entry.CounterId, entry.TargetValue))
                .OrderBy(entry => entry.MissionId).ThenBy(entry => entry.ObjectiveId).ThenBy(entry => entry.TransitionId).ThenBy(entry => entry.SubjectId).ToArray();
            var indraExpected = new List<(uint, uint, uint, byte?, uint?, uint?, uint?)> { (1280, 1, 1, scenario, 1, 1, null) };
            for (var n = 1U; n <= 10; n++)
                indraExpected.AddRange(new (uint, uint, uint, byte?, uint?, uint?, uint?)[] { (1281, n, 1, scenario, n, 1, null), (1281, n, 2, scenario, 1, 19, null) });
            for (var n = 1U; n <= 10; n++)
                indraExpected.Add((1281, 11, 1, scenario, n, 2, 10));
            indraExpected.Add((1281, 11, 2, scenario, 1, 19, null));
            indraExpected.AddRange(new (uint, uint, uint, byte?, uint?, uint?, uint?)[]
            {
                (1282, 1, 1, scenario, 1, 1, null), (1282, 1, 2, scenario, 1, 19, null), (1282, 2, 2, scenario, 1, 19, null), (1282, 3, 2, scenario, 1, 19, null)
            });
            CollectionAssert.AreEqual(indraExpected.OrderBy(e => e.Item1).ThenBy(e => e.Item2).ThenBy(e => e.Item3).ThenBy(e => e.Item5).ToArray(), indra);

            // Fithik Infestation counts the Fithik species (CreatureFlag 72), counter 0, to 10.
            var kill = harness.World.Set<MissionTriggerEntry>().AsNoTracking()
                .Single(entry => entry.ContentRevision == AshenMissionDataV1.Revision && entry.MissionId == 1274 && entry.Kind == MissionTriggerKind.ProgressEvent);
            Assert.AreEqual(((byte?)MissionProgressEventKind.CreatureFlagKilled, (uint?)72, (uint?)0, (uint?)10),
                (kill.EventKind, kill.SubjectId, kill.CounterId, kill.TargetValue));
            Assert.AreEqual(72, (int)CreatureFlag.SpeciesFithik, "the client's Fithik species flag");

            // Set 2's kills: 1242 the new Ashen Atta Soldier and Worker, ten each on counter 0; 1244 Exing, once.
            var kills = harness.World.Set<MissionTriggerEntry>().AsNoTracking()
                .Where(entry => entry.ContentRevision == AshenMissionDataV1.Revision && (entry.MissionId == 1242 || entry.MissionId == 1244) &&
                                entry.Kind == MissionTriggerKind.ProgressEvent).AsEnumerable()
                .Select(entry => (entry.MissionId, entry.ObjectiveId, entry.EventKind, entry.SubjectId, entry.CounterId, entry.TargetValue))
                .OrderBy(entry => entry.MissionId).ThenBy(entry => entry.ObjectiveId).ToArray();
            var killed = (byte?)MissionProgressEventKind.CreatureKilled;
            CollectionAssert.AreEqual(new (uint, uint, byte?, uint?, uint?, uint?)[]
            {
                (1242, 1, killed, AttaSoldier, 0, 10), (1242, 2, killed, AttaWorker, 0, 10), (1244, 1, killed, Exing, null, null)
            }, kills);
            // 1243 picks up four Mutated Atta Blood Samples (client item class 25409); 1245's five beacons are scene results 1-5.
            var samples = harness.World.Set<MissionTriggerEntry>().AsNoTracking()
                .Single(entry => entry.ContentRevision == AshenMissionDataV1.Revision && entry.MissionId == 1243 && entry.Kind == MissionTriggerKind.ProgressEvent);
            Assert.AreEqual(((byte?)MissionProgressEventKind.ItemAcquired, (uint?)25409, (uint?)4), (samples.EventKind, samples.SubjectId, samples.TargetValue));
            // The sample: a personal drop of the Mutated Atta Soldier 790045 at 50% (ESTIMATE: no source gives a rate), four turned in.
            var drop = Scenes(harness).Single(scene => scene.MissionId == 1243).Scene.Items.Single();
            Assert.AreEqual((97458U, 50, 4U), (drop.ItemTemplateId, drop.Drop.ChancePercent, drop.TurnInQuantity));
            CollectionAssert.AreEqual(new[] { MutatedSoldier }, drop.Drop.CreatureIds.ToArray());
            var beacons = harness.World.Set<MissionTriggerEntry>().AsNoTracking()
                .Where(entry => entry.ContentRevision == AshenMissionDataV1.Revision && entry.MissionId == 1245 && entry.Kind == MissionTriggerKind.ProgressEvent)
                .AsEnumerable().OrderBy(entry => entry.ObjectiveId).Select(entry => (entry.ObjectiveId, entry.EventKind, entry.SubjectId, entry.CounterId)).ToArray();
            CollectionAssert.AreEqual(Enumerable.Range(1, 5).Select(n => ((uint)n, (byte?)MissionProgressEventKind.ScenarioEvent, (uint?)n, (uint?)1)).ToArray(), beacons);

            // Set 3's kills: 1247 the Thrax Ravager (531009) on 1 and the Thrax Soldier Mark VI (531008) on 3, ten each on counter 0;
            // 1248 the Desert Predator once on counter 0; 1249 Mortar n the copy in mortar pool n, once.
            var highway = harness.World.Set<MissionTriggerEntry>().AsNoTracking()
                .Where(entry => entry.ContentRevision == AshenMissionDataV1.Revision && (entry.MissionId == 1247 || entry.MissionId == 1248 ||
                                entry.MissionId == 1249) && entry.Kind == MissionTriggerKind.ProgressEvent).AsEnumerable()
                .Select(entry => (entry.MissionId, entry.ObjectiveId, entry.EventKind, entry.SubjectId, entry.CounterId, entry.TargetValue))
                .OrderBy(entry => entry.MissionId).ThenBy(entry => entry.ObjectiveId).ToArray();
            CollectionAssert.AreEqual(new (uint, uint, byte?, uint?, uint?, uint?)[]
            {
                (1247, 1, killed, Ravager, 0, 10), (1247, 3, killed, ThraxSoldier, 0, 10), (1248, 1, killed, Predator, 0, 1),
                (1249, 1, killed, 790081, null, null), (1249, 2, killed, 790082, null, null), (1249, 3, killed, 790083, null, null),
                (1249, 4, killed, 790084, null, null)
            }, highway);

            // Set 4's kills: 1262 the Xanx - Ashen Desert (540020, "Xanx Burrower") six on counter 0; 1272 the Desert Stalker once
            // on counter 0. 1263's hidden 2 counts the six fallen's uses (scene results 1-6, counter 1, to 6); 1 completes on it.
            var boneyard = harness.World.Set<MissionTriggerEntry>().AsNoTracking()
                .Where(entry => entry.ContentRevision == AshenMissionDataV1.Revision && (entry.MissionId == 1262 || entry.MissionId == 1263 ||
                                entry.MissionId == 1272) && entry.Kind == MissionTriggerKind.ProgressEvent).AsEnumerable()
                .Select(entry => (entry.MissionId, entry.ObjectiveId, entry.EventKind, entry.SubjectId, entry.CounterId, entry.TargetValue))
                .OrderBy(entry => entry.MissionId).ThenBy(entry => entry.ObjectiveId).ThenBy(entry => entry.SubjectId).ToArray();
            var scene = (byte?)MissionProgressEventKind.ScenarioEvent;
            CollectionAssert.AreEqual(new (uint, uint, byte?, uint?, uint?, uint?)[]
            {
                (1262, 1, killed, Xanx, 0, 6),
                (1263, 2, scene, 1, 1, 6), (1263, 2, scene, 2, 1, 6), (1263, 2, scene, 3, 1, 6), (1263, 2, scene, 4, 1, 6),
                (1263, 2, scene, 5, 1, 6), (1263, 2, scene, 6, 1, 6),
                (1272, 1, killed, Stalker, 0, 1)
            }, boneyard);
            var tags = Scenes(harness).Single(entry => entry.MissionId == 1263).Scene;
            CollectionAssert.AreEqual(new uint[] { 2 }, tags.HiddenObjectiveIds.ToArray(), "1263's 2 is hidden");
            var tag = tags.Items.Single();
            Assert.AreEqual((97464U, 6U, 1U), (tag.ItemTemplateId, tag.MaximumQuantity, tag.CounterObjectiveId), "Ashen AFS Dog Tag (client item class 25443), n/6 on 1");

            // Set 5: 1352 Mortar n the copy in trench pool n, once; 11 no trigger (it aggregates 1-4); Launcher n (5-10)
            // scene result n (counter 1). 1354 Generator n scene result n. 1355 the Linker - Ashen Desert (531014) fifteen and
            // Overseer Naxveik (520005) once, each on counter 0.
            var conscription = harness.World.Set<MissionTriggerEntry>().AsNoTracking()
                .Where(entry => entry.ContentRevision == AshenMissionDataV1.Revision && (entry.MissionId == 1352 || entry.MissionId == 1354 ||
                                entry.MissionId == 1355) && entry.Kind == MissionTriggerKind.ProgressEvent).AsEnumerable()
                .Select(entry => (entry.MissionId, entry.ObjectiveId, entry.EventKind, entry.SubjectId, entry.CounterId, entry.TargetValue))
                .OrderBy(entry => entry.MissionId).ThenBy(entry => entry.ObjectiveId).ThenBy(entry => entry.SubjectId).ToArray();
            CollectionAssert.AreEqual(new (uint, uint, byte?, uint?, uint?, uint?)[]
            {
                (1352, 1, killed, 790160, null, null), (1352, 2, killed, 790161, null, null), (1352, 3, killed, 790162, null, null),
                (1352, 4, killed, 790163, null, null),
                (1352, 5, scene, 1, 1, null), (1352, 6, scene, 2, 1, null), (1352, 7, scene, 3, 1, null), (1352, 8, scene, 4, 1, null),
                (1352, 9, scene, 5, 1, null), (1352, 10, scene, 6, 1, null),
                (1354, 1, scene, 1, 1, null), (1354, 2, scene, 2, 1, null), (1354, 3, scene, 3, 1, null),
                (1355, 1, killed, Linker, 0, 15), (1355, 2, killed, Naxveik, 0, 1)
            }, conscription);
            // 1352's 11 "Mortars Destroyed": the count of Mortar 1-4 done, on counter 0 to 4 (distinct mortars, not kills).
            var aggregate = Scenes(harness).Single(entry => entry.MissionId == 1352).Scene.ObjectiveAggregations.Single();
            Assert.AreEqual((11U, "1,2,3,4", 4U, (uint?)0U),
                (aggregate.Key, string.Join(",", aggregate.Value.ChildObjectiveIds), aggregate.Value.TargetCount, aggregate.Value.CounterId), "1352's 11 aggregates 1-4");
            // The explosives: 1352 six High Grade Plastic Explosives (client item class 25490), 1354 three Electromagnetic Charged
            // Explosives (26441), each given at the accept (TaRapedia's known issue of 2007-08-21 not copied).
            foreach (var (mission, template, count) in new (uint, uint, uint)[] { (1352, 97491, 6), (1354, 111184, 3) })
            {
                var bombs = Scenes(harness).Single(entry => entry.MissionId == mission).Scene;
                var binding = bombs.Items.Single();
                Assert.AreEqual((template, count, Rasa.Missions.Scenes.MissionItemScope.AssignmentIssued), (binding.ItemTemplateId, binding.MaximumQuantity, binding.Scope), $"{mission} explosives");
                var issued = (Rasa.Missions.Scenes.IssueMissionItemIntent)bombs.AcceptanceItems.Single();
                Assert.AreEqual((template, count), (issued.ItemTemplateId, issued.Quantity), $"{mission}: all of them at the accept");
            }

            // Set 6: Formal Complaints' choice 1 sets player flag 250 to 1 and 251 to 0, choice 2 251 to 1 and 250 to 0 (the
            // client's variants of 1254,2 and 1254,3); 2 and 3 answer; the hidden 4 aggregates 2 or 3, count 1.
            var flags = harness.World.Set<MissionActionEntry>().AsNoTracking()
                .Where(entry => entry.ContentRevision == AshenMissionDataV1.Revision && entry.Kind == MissionActionKind.SetPlayerFlag).AsEnumerable()
                .Select(entry => (entry.MissionId, entry.ObjectiveId, entry.TransitionId, entry.PlayerFlagId, entry.PlayerFlagValue))
                .OrderBy(entry => entry.MissionId).ThenBy(entry => entry.TransitionId).ThenBy(entry => entry.PlayerFlagId).ToArray();
            // Set 11: Murky Waters' choice 1 (the repair) sets 258 to 1 and 257 to 0, choice 2 (the poison) 257 to 1 and 258 to 0
            // (the client's variants of 1298,3 and 1298,2); its hidden 4 aggregates 2 or 3, count 1.
            CollectionAssert.AreEqual(new (uint, uint, uint, uint?, uint?)[]
            {
                (1254, 1, 1, 250, 1), (1254, 1, 1, 251, 0), (1254, 1, 2, 250, 0), (1254, 1, 2, 251, 1),
                (1298, 1, 1, 257, 0), (1298, 1, 1, 258, 1), (1298, 1, 2, 257, 1), (1298, 1, 2, 258, 0)
            }, flags);
            var murky = Scenes(harness).Single(entry => entry.MissionId == 1298).Scene;
            CollectionAssert.AreEqual(new uint[] { 4 }, murky.HiddenObjectiveIds.ToArray(), "1298's 4 is hidden");
            var murkyOutcome = murky.ObjectiveAggregations.Single();
            Assert.AreEqual((4U, "2,3", 1U), (murkyOutcome.Key, string.Join(",", murkyOutcome.Value.ChildObjectiveIds), murkyOutcome.Value.TargetCount), "1298's 4 aggregates 2 or 3");
            var complaints = Scenes(harness).Single(entry => entry.MissionId == 1254).Scene;
            CollectionAssert.AreEqual(new uint[] { 4 }, complaints.HiddenObjectiveIds.ToArray(), "1254's 4 is hidden");
            var outcome = complaints.ObjectiveAggregations.Single();
            Assert.AreEqual((4U, "2,3", 1U), (outcome.Key, string.Join(",", outcome.Value.ChildObjectiveIds), outcome.Value.TargetCount), "1254's 4 aggregates 2 or 3");
            var choice = complaints.Dialogue.Single();
            Assert.AreEqual((1U, 1449U, 1U, Rasa.Missions.Definitions.MissionDialogueKind.Choice, "1:1,2:2"),
                (choice.ObjectiveId, choice.NpcPackageId, choice.PlayerFlagId, choice.Kind, string.Join(",", choice.Choices.OrderBy(pair => pair.Key).Select(pair => $"{pair.Key}:{pair.Value}"))),
                "Latan's choice: the client's choice 1 and 2 on 1's transitions 1 and 2");
            // 1259 and 1261: Latan's Package (client item class 25439, template 97462), one, at the accept, taken at the talk.
            foreach (var mission in new uint[] { 1259, 1261 })
            {
                var parcel = Scenes(harness).Single(entry => entry.MissionId == mission).Scene;
                var binding = parcel.Items.Single();
                Assert.AreEqual((97462U, 1U, Rasa.Missions.Scenes.MissionItemScope.AssignmentIssued), (binding.ItemTemplateId, binding.MaximumQuantity, binding.Scope), $"{mission} package");
                var issued = (Rasa.Missions.Scenes.IssueMissionItemIntent)parcel.AcceptanceItems.Single();
                Assert.AreEqual((97462U, 1U), (issued.ItemTemplateId, issued.Quantity), $"{mission}: the package at the accept");
            }
            // 1285 picks up Yamamoto's Wedding Ring (client item class 25480, template 97481): a personal 15% drop (ESTIMATE) off the
            // Thrax Soldier (531008) and Thrax Ravager (531009) on Ashen Desert, one, taken at the turn-in.
            var ring = harness.World.Set<MissionTriggerEntry>().AsNoTracking()
                .Single(entry => entry.ContentRevision == AshenMissionDataV1.Revision && entry.MissionId == 1285 && entry.Kind == MissionTriggerKind.ProgressEvent);
            Assert.AreEqual(((byte?)MissionProgressEventKind.ItemAcquired, (uint?)25480, (uint?)1), (ring.EventKind, ring.SubjectId, ring.TargetValue));
            var ringDrop = Scenes(harness).Single(entry => entry.MissionId == 1285).Scene.Items.Single();
            Assert.AreEqual((97481U, 15, 1U, 1U, Map), (ringDrop.ItemTemplateId, ringDrop.Drop.ChancePercent, ringDrop.TurnInQuantity, ringDrop.MaximumQuantity, ringDrop.Drop.MapContextId));
            CollectionAssert.AreEqual(new[] { ThraxSoldier, Ravager }, ringDrop.Drop.CreatureIds.ToArray());

            // Set 7: each objective picks up its client item class (no counter text in the client), each a personal drop of one
            // creature row on Ashen Desert: 1264 Intact Nitroglazer Salvage (25444, template 97465) off the Nitroglazer 790240 and
            // Intact Machina Salvage (25445, 97466) off the Hominis Machina 790241, 15 each at 75%; 1266 Thrax Rifle Salvage
            // (25448, 97469) off the Thrax Soldier 531008 and Thrax Grenade Launcher Salvage (25449, 97470) off the Ravager
            // 531009, 15 each at 75%; 1265 Intact Stalker Salvage (25446, 97467) off the Desert Stalker 790121 and Intact
            // Predator Salvage (25447, 97468) off the Desert Predator 790080, one each at 100% (all ESTIMATE rates). All kept at
            // an abandon, taken at the turn-in. Set 8: Ito's 1267, 1269 and 1268 the same as Latan's 1264, 1266 and 1265 (the
            // client gives the twins the same item classes; TaRapedia: one side only), each binding the mission's own.
            var salvage = harness.World.Set<MissionTriggerEntry>().AsNoTracking()
                .Where(entry => entry.ContentRevision == AshenMissionDataV1.Revision && entry.MissionId >= 1264 && entry.MissionId <= 1269 &&
                                entry.Kind == MissionTriggerKind.ProgressEvent).AsEnumerable()
                .Select(entry => (entry.MissionId, entry.ObjectiveId, entry.EventKind, entry.SubjectId, entry.TargetValue))
                .OrderBy(entry => entry.MissionId).ThenBy(entry => entry.ObjectiveId).ToArray();
            var acquired = (byte?)MissionProgressEventKind.ItemAcquired;
            CollectionAssert.AreEqual(new (uint, uint, byte?, uint?, uint?)[]
            {
                (1264, 1, acquired, 25444, 15), (1264, 2, acquired, 25445, 15),
                (1265, 1, acquired, 25446, 1), (1265, 2, acquired, 25447, 1),
                (1266, 1, acquired, 25448, 15), (1266, 2, acquired, 25449, 15),
                (1267, 1, acquired, 25444, 15), (1267, 2, acquired, 25445, 15),
                (1268, 1, acquired, 25446, 1), (1268, 2, acquired, 25447, 1),
                (1269, 1, acquired, 25448, 15), (1269, 2, acquired, 25449, 15)
            }, salvage);
            foreach (var (mission, drops) in new (uint, (uint Template, uint Objective, uint Creature, int Chance, uint Count)[])[]
            {
                (1264, new[] { (97465U, 1U, Nitroglazer, 75, 15U), (97466U, 2U, Machina, 75, 15U) }),
                (1265, new[] { (97467U, 1U, Stalker, 100, 1U), (97468U, 2U, Predator, 100, 1U) }),
                (1266, new[] { (97469U, 1U, ThraxSoldier, 75, 15U), (97470U, 2U, Ravager, 75, 15U) }),
                (1267, new[] { (97465U, 1U, Nitroglazer, 75, 15U), (97466U, 2U, Machina, 75, 15U) }),
                (1268, new[] { (97467U, 1U, Stalker, 100, 1U), (97468U, 2U, Predator, 100, 1U) }),
                (1269, new[] { (97469U, 1U, ThraxSoldier, 75, 15U), (97470U, 2U, Ravager, 75, 15U) })
            })
            {
                var items = Scenes(harness).Single(entry => entry.MissionId == mission).Scene.Items.OrderBy(item => item.ItemTemplateId).ToArray();
                CollectionAssert.AreEqual(drops.Select(drop => (drop.Template, drop.Objective, drop.Creature, drop.Chance, drop.Count, drop.Count, drop.Count, 1U, Map,
                        Rasa.Missions.Scenes.MissionItemScope.CharacterOwned, Rasa.Missions.Definitions.MissionItemCleanupDisposition.Retain)).ToArray(),
                    items.Select(item => (item.ItemTemplateId, item.Drop.ObjectiveId, item.Drop.CreatureIds.Single(), item.Drop.ChancePercent, item.MaximumQuantity,
                        item.TurnInQuantity, item.MaximumQuantity, item.Drop.Quantity, item.Drop.MapContextId, item.Scope, item.Abandonment)).ToArray(), $"{mission} drops");
            }

            // Set 9: 1275's 1 the crate's use (scenario 1, step 1); 1277's 1-4 each its console's (scenario 1, step n); 1278's 2
            // completes on the hold's end (scenario 2, step 1) and fails (its transition 2) once the hidden 7 has counted the
            // four batteries lost (scenario 3, steps 1-4, to 4); 1278's 1 the battery area, or its deadline of 180 s.
            var stonewall = harness.World.Set<MissionTriggerEntry>().AsNoTracking()
                .Where(entry => entry.ContentRevision == AshenMissionDataV1.Revision && entry.MissionId >= 1275 && entry.MissionId <= 1278).AsEnumerable()
                .Select(entry => (entry.MissionId, entry.ObjectiveId, entry.TransitionId, entry.Kind, entry.EventKind, entry.SubjectId, entry.CounterId, entry.TargetValue))
                .OrderBy(entry => entry.MissionId).ThenBy(entry => entry.ObjectiveId).ThenBy(entry => entry.TransitionId).ThenBy(entry => entry.SubjectId).ToArray();
            var progress = MissionTriggerKind.ProgressEvent;
            CollectionAssert.AreEqual(new (uint, uint, uint, MissionTriggerKind, byte?, uint?, uint?, uint?)[]
            {
                (1275, 1, 1, progress, scene, 1, 1, null),
                (1277, 1, 1, progress, scene, 1, 1, null), (1277, 2, 1, progress, scene, 2, 1, null),
                (1277, 3, 1, progress, scene, 3, 1, null), (1277, 4, 1, progress, scene, 4, 1, null),
                (1278, 1, 1, MissionTriggerKind.AreaEntered, null, null, null, null), (1278, 1, 2, MissionTriggerKind.TimerElapsed, null, null, null, null),
                (1278, 1, 3, MissionTriggerKind.ObjectiveState, null, null, null, null),
                (1278, 2, 1, progress, scene, 1, 2, null), (1278, 2, 2, MissionTriggerKind.ObjectiveState, null, null, null, null),
                (1278, 7, 1, progress, scene, 1, 3, 4), (1278, 7, 1, progress, scene, 2, 3, 4), (1278, 7, 1, progress, scene, 3, 3, 4),
                (1278, 7, 1, progress, scene, 4, 3, 4)
            }, stonewall);
            var deadline = harness.World.Set<MissionTriggerEntry>().AsNoTracking()
                .Single(entry => entry.ContentRevision == AshenMissionDataV1.Revision && entry.MissionId == 1278 && entry.Kind == MissionTriggerKind.TimerElapsed);
            Assert.AreEqual(180U, deadline.DurationSeconds, "TaRapedia's 'Time remaining: 00:03:00'");
            var fourthLost = harness.World.Set<MissionTriggerEntry>().AsNoTracking()
                .Where(entry => entry.ContentRevision == AshenMissionDataV1.Revision && entry.MissionId == 1278 && entry.Kind == MissionTriggerKind.ObjectiveState)
                .AsEnumerable().Select(entry => (entry.ObjectiveId, entry.TransitionId, entry.RelatedObjectiveId, entry.RelatedState))
                .OrderBy(entry => entry.ObjectiveId).ToArray();
            CollectionAssert.AreEqual(new (uint, uint, uint?, byte?)[] { (1, 3, 7, (byte)MissionObjectiveState.Completed), (2, 2, 7, (byte)MissionObjectiveState.Completed) },
                fourthLost, "1278's 1 (before the top) and 2 (after it) fail once the hidden 7 has counted the four losses");
            CollectionAssert.AreEqual(new uint[] { 7 }, Scenes(harness).Single(entry => entry.MissionId == 1278).Scene.HiddenObjectiveIds.ToArray(), "1278's 7 is hidden");
            var transitions = harness.World.Set<MissionObjectiveTransitionEntry>().AsNoTracking()
                .Where(entry => entry.ContentRevision == AshenMissionDataV1.Revision && entry.MissionId == 1278).AsEnumerable()
                .Select(entry => (entry.ObjectiveId, entry.TransitionId, entry.ToState)).OrderBy(entry => entry).ToArray();
            var completedState = (byte?)MissionObjectiveState.Completed;
            var failedState = (byte?)MissionObjectiveState.Failed;
            CollectionAssert.AreEqual(new (uint, uint, byte?)[]
            {
                (1, 1, completedState), (1, 2, failedState), (1, 3, failedState), (2, 1, completedState), (2, 2, failedState), (7, 1, completedState)
            }, transitions, "1278: each objective completes one way and fails the other");
            var area = harness.World.Set<MissionAreaEntry>().AsNoTracking()
                .Single(entry => entry.ContentRevision == AshenMissionDataV1.Revision && entry.MissionId == 1278);
            Assert.AreEqual((Map, 100.5, -528.4, (double?)60.0), (area.MapContextId, area.PosX, area.PosZ, area.Radius), "1278's area over the four batteries");
            var codes = Scenes(harness).Single(entry => entry.MissionId == 1275).Scene.Items.Single();
            Assert.AreEqual((97475U, 1U, Rasa.Missions.Scenes.MissionItemScope.AssignmentIssued), (codes.ItemTemplateId, codes.MaximumQuantity, codes.Scope),
                "Bane Radar Frequency Codes (client item class 25473), one");

            // Set 10: each objective picks up its client item class (itemclass stack 10 and 1), a personal drop on Ashen Desert:
            // 1287 Shiny Atta Carapace (25482, template 97483) off set 2's Ashen Atta Soldier 790043 and Worker 790044 (the
            // client's "Ashen Atta Soldiers and Ashen Atta Harvesters"), Pristine Atta Carapace (25486, 97487) off Sally; 1293
            // Shiny Granitour Carapace (25483, 97484) off the Granitour, Pristine (25487, 97488) off Wendy; 1289 Lasher Leather
            // Hide (25485, 97486) off the Lasher, Pristine (25488, 97489) off Molly; 1291 Beam Manta Leather Hide (25484, 97485)
            // off the Beam Manta, Pristine (25489, 97490) off Trudy. Ten at 75% and one at 100% (ESTIMATE rates), kept at an
            // abandon, taken at the turn-in.
            var hides = harness.World.Set<MissionTriggerEntry>().AsNoTracking()
                .Where(entry => entry.ContentRevision == AshenMissionDataV1.Revision && entry.MissionId >= 1287 && entry.MissionId <= 1293 &&
                                entry.Kind == MissionTriggerKind.ProgressEvent).AsEnumerable()
                .Select(entry => (entry.MissionId, entry.ObjectiveId, entry.EventKind, entry.SubjectId, entry.TargetValue))
                .OrderBy(entry => entry.MissionId).ThenBy(entry => entry.ObjectiveId).ToArray();
            CollectionAssert.AreEqual(new (uint, uint, byte?, uint?, uint?)[]
            {
                (1287, 1, acquired, 25482, 10), (1287, 2, acquired, 25486, 1),
                (1289, 1, acquired, 25485, 10), (1289, 2, acquired, 25488, 1),
                (1291, 1, acquired, 25484, 10), (1291, 2, acquired, 25489, 1),
                (1293, 1, acquired, 25483, 10), (1293, 2, acquired, 25487, 1)
            }, hides);
            foreach (var (mission, drops) in new (uint, (uint Template, uint Objective, uint[] Creatures, int Chance, uint Count)[])[]
            {
                (1287, new[] { (97483U, 1U, new[] { AttaSoldier, AttaWorker }, 75, 10U), (97487U, 2U, new[] { Sally }, 100, 1U) }),
                (1289, new[] { (97486U, 1U, new[] { Lasher }, 75, 10U), (97489U, 2U, new[] { Molly }, 100, 1U) }),
                (1291, new[] { (97485U, 1U, new[] { Manta }, 75, 10U), (97490U, 2U, new[] { Trudy }, 100, 1U) }),
                (1293, new[] { (97484U, 1U, new[] { Granitour }, 75, 10U), (97488U, 2U, new[] { Wendy }, 100, 1U) })
            })
            {
                var items = Scenes(harness).Single(entry => entry.MissionId == mission).Scene.Items.OrderBy(item => item.ItemTemplateId).ToArray();
                CollectionAssert.AreEqual(drops.Select(drop => (drop.Template, drop.Objective, string.Join(",", drop.Creatures), drop.Chance, drop.Count, drop.Count, 1U, Map,
                        Rasa.Missions.Scenes.MissionItemScope.CharacterOwned, Rasa.Missions.Definitions.MissionItemCleanupDisposition.Retain)).ToArray(),
                    items.Select(item => (item.ItemTemplateId, item.Drop.ObjectiveId, string.Join(",", item.Drop.CreatureIds), item.Drop.ChancePercent, item.MaximumQuantity,
                        item.TurnInQuantity, item.Drop.Quantity, item.Drop.MapContextId, item.Scope, item.Abandonment)).ToArray(), $"{mission} drops");
            }

            // Set 11: 1295's Harvester Diagnostic Tool (client item class 25476, template 97477, stack 1) and 1356's Shaped
            // Charges (26442, 111185, stack 6; three, ESTIMATE), given at the accept, gone at the turn-in or an abandon.
            foreach (var (mission, template, count) in new (uint, uint, uint)[] { (1295, 97477, 1), (1356, 111185, 3) })
            {
                var issued = Scenes(harness).Single(entry => entry.MissionId == mission).Scene;
                var item = issued.Items.Single();
                Assert.AreEqual((template, count, Rasa.Missions.Scenes.MissionItemScope.AssignmentIssued, Rasa.Missions.Definitions.MissionItemCleanupDisposition.Remove),
                    (item.ItemTemplateId, item.MaximumQuantity, item.Scope, item.Abandonment), $"{mission}'s item");
                var accept = (Rasa.Missions.Scenes.IssueMissionItemIntent)issued.AcceptanceItems.Single();
                Assert.AreEqual((template, count), (accept.ItemTemplateId, accept.Quantity), $"{mission}: at the accept");
            }

            // Set 12: 1299 12 Granitour Water Glands (client item class 25478, template 97479, stack 12) off set 10's Granitour
            // (not Wendy), 1300 12 Lasher Poison Glands (25479, 97480, stack 12) off set 10's Lasher (not Molly), each at 75%
            // (ESTIMATE), kept at an abandon, taken at the turn-in; 1302's four Water Filtration Systems (25494, 97495, stack 4)
            // given at the accept, gone at an abandon, one taken at each of its panels (scene results 1-4, counter 1).
            var glands = harness.World.Set<MissionTriggerEntry>().AsNoTracking()
                .Where(entry => entry.ContentRevision == AshenMissionDataV1.Revision && (entry.MissionId == 1299 || entry.MissionId == 1300 || entry.MissionId == 1302) &&
                                entry.Kind == MissionTriggerKind.ProgressEvent).AsEnumerable()
                .Select(entry => (entry.MissionId, entry.ObjectiveId, entry.EventKind, entry.SubjectId, entry.CounterId, entry.TargetValue))
                .OrderBy(entry => entry.MissionId).ThenBy(entry => entry.ObjectiveId).ToArray();
            CollectionAssert.AreEqual(new (uint, uint, byte?, uint?, uint?, uint?)[]
                {
                    (1299, 1, acquired, 25478, null, 12), (1300, 1, acquired, 25479, null, 12)
                }.Concat(Enumerable.Range(1, 4).Select(n => (1302U, (uint)n, (byte?)MissionProgressEventKind.ScenarioEvent, (uint?)n, (uint?)1, (uint?)null))).ToArray(), glands);
            foreach (var (mission, template, creature) in new (uint, uint, uint)[] { (1299, 97479, Granitour), (1300, 97480, Lasher) })
            {
                var item = Scenes(harness).Single(entry => entry.MissionId == mission).Scene.Items.Single();
                Assert.AreEqual((template, 1U, creature.ToString(), 75, 12U, 12U, 1U, Map, Rasa.Missions.Scenes.MissionItemScope.CharacterOwned, Rasa.Missions.Definitions.MissionItemCleanupDisposition.Retain),
                    (item.ItemTemplateId, item.Drop.ObjectiveId, string.Join(",", item.Drop.CreatureIds), item.Drop.ChancePercent, item.MaximumQuantity,
                        item.TurnInQuantity, item.Drop.Quantity, item.Drop.MapContextId, item.Scope, item.Abandonment), $"{mission} drops");
            }
            var wellwater = Scenes(harness).Single(entry => entry.MissionId == 1302).Scene;
            var filters = wellwater.Items.Single();
            Assert.AreEqual((97495U, 4U, Rasa.Missions.Scenes.MissionItemScope.AssignmentIssued, Rasa.Missions.Definitions.MissionItemCleanupDisposition.Remove),
                (filters.ItemTemplateId, filters.MaximumQuantity, filters.Scope, filters.Abandonment), "1302's systems");
            var issuedFilters = (Rasa.Missions.Scenes.IssueMissionItemIntent)wellwater.AcceptanceItems.Single();
            Assert.AreEqual((97495U, 4U), (issuedFilters.ItemTemplateId, issuedFilters.Quantity), "1302: four at the accept");
            for (var number = 1U; number <= 4; number++)
            {
                var consume = wellwater.Sequences[number].Character.OfType<Rasa.Missions.Scenes.ConsumeMissionItemIntent>().Single();
                Assert.AreEqual((97495U, 1U), (wellwater.Items.Single(item => item.ItemKey == consume.ItemKey).ItemTemplateId, consume.Quantity), $"1302's panel {number} takes one");
            }
        }

        /// <summary>
        /// Set 2's Atta: the client's names (creaturenamelanguage 9318 Ashen Atta Soldier, 9319 Ashen Atta Worker, 9320 Mutated
        /// Atta Soldier), hostile, the classes, levels, hit points and attacks of the World DB rows they copy (531015, 531099,
        /// 531098), the Atta species flag (82, so Targets of Opportunity 1580's "Desert Exterminator" counts them), in six pools of
        /// three (radius 0) on Ashen Desert; Exing's pool at its new place.
        /// </summary>
        [TestMethod]
        public void TheAttaCopyTheirWorldRowsWithTheClientsNames()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var world = harness.World;
            foreach (var (atta, name, copy, cls, level) in new (uint, uint, uint, uint, uint)[]
            {
                (AttaSoldier, 9318, 531015, 7081, 40), (AttaWorker, 9319, 531099, 7078, 40), (MutatedSoldier, 9320, 531098, 7081, 41)
            })
            {
                var row = world.CreatureEntries.AsNoTracking().Single(entry => entry.Id == atta);
                var source = world.CreatureEntries.AsNoTracking().Single(entry => entry.Id == copy);
                Assert.AreEqual((name, cls, level, 0U), (row.NameId, row.ClassId, row.Level, row.Faction), $"{atta}");
                Assert.AreEqual((source.ClassId, source.Level, source.MaxHitPoints, source.Action1, source.Action2, source.Action3, source.RunSpeed),
                    (row.ClassId, row.Level, row.MaxHitPoints, row.Action1, row.Action2, row.Action3, row.RunSpeed), $"{atta} copies {copy}");
                Assert.IsTrue(CreatureManager.Instance.LoadedCreatures.TryGetValue(atta, out var loaded), $"{atta} loaded");
                Assert.AreEqual(TargetCategory.Hostile, loaded.TargetCategory, $"{atta} hostile");
                Assert.IsTrue(world.CreatureClassFlagEntries.AsNoTracking().Any(flag => flag.ClassId == cls && flag.FlagId == 82),
                    $"class {cls} has the Atta flag 82");
                var pools = world.SpawnPoolEntries.AsNoTracking().Where(pool => pool.Creature1Id == atta).ToList();
                // Fix F adds a third Soldier pool, 1287's own (790378).
                Assert.HasCount(atta == AshenFixFV1.AttaSoldier ? 3 : 2, pools, $"{atta}'s pools");
                foreach (var pool in pools)
                    // Three as set 2 and fix F wrote them; one since fix G (AshenFixGTests).
                    Assert.AreEqual((Map, (byte)AshenFixGV1.AttaCount, (byte)AshenFixGV1.AttaCount, 0.0, 1200U), (pool.MapContextId, pool.Creature1MinCount, pool.Creature1MaxCount,
                        pool.Radius, pool.RespawnTime), $"pool {pool.Id}");
            }
            Assert.AreEqual(82, (int)Rasa.Services.Preloader.TargetsOfOpportunitySeed.Flag.Atta, "the ToO's Atta flag");
        }

        /// <summary>
        /// The lead-ins, Fithik Infestation, Angry Angry Atta and Harmful Mutations need no mission (TaRapedia: no requirement);
        /// Chemical Oversight needs Angry Angry Atta done, Eyes on the Ground Harmful Mutations (TaRapedia); none repeats.
        /// </summary>
        [TestMethod]
        public void OnlyTheFollowUpsRequireAMission()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            // Set 3's three need none (TaRapedia: no requirement; triage chain 13); set 4's three neither (TaRapedia).
            // Set 5's three need both Fallen Soldiers and Xanxless Job (TaRapedia).
            var prerequisites = harness.World.MissionPrerequisiteEntries.AsNoTracking().Where(entry => entry.ContentRevision == AshenMissionDataV1.Revision)
                .AsEnumerable().Select(entry => (entry.MissionId, entry.Kind, entry.RequiredMissionId, entry.RequiredMissionState))
                .OrderBy(entry => entry.MissionId).ThenBy(entry => entry.RequiredMissionId).ToArray();
            var done = (byte)MissionState.Completed;
            CollectionAssert.AreEqual(new (uint, MissionPrerequisiteKind, uint?, byte?)[]
            {
                (1244, MissionPrerequisiteKind.MissionCompleted, 1242, done),
                (1245, MissionPrerequisiteKind.MissionCompleted, 1243, done),
                // Set 6 (TaRapedia): 1258 after Formal Complaints with its choice 2 (player flag 251 at 1), then the chain.
                (1258, MissionPrerequisiteKind.PlayerFlagValue, null, null), (1258, MissionPrerequisiteKind.MissionCompleted, 1254, done),
                (1259, MissionPrerequisiteKind.MissionCompleted, 1258, done), (1261, MissionPrerequisiteKind.MissionCompleted, 1259, done),
                // Set 7 (TaRapedia; triage D1): 1264 after Formal Complaints done with its choice 1 (flag 250 at 1), then the chain.
                (1264, MissionPrerequisiteKind.PlayerFlagValue, null, null), (1264, MissionPrerequisiteKind.MissionCompleted, 1254, done),
                (1265, MissionPrerequisiteKind.MissionCompleted, 1266, done), (1266, MissionPrerequisiteKind.MissionCompleted, 1264, done),
                // Set 8 (TaRapedia "Dearly Departed"; triage D1): 1267 after Dearly Departed and Formal Complaints done with its
                // choice 2 (flag 251 at 1), then the chain 1269, 1268.
                (1267, MissionPrerequisiteKind.PlayerFlagValue, null, null), (1267, MissionPrerequisiteKind.MissionCompleted, 1254, done),
                (1267, MissionPrerequisiteKind.MissionCompleted, 1285, done),
                (1268, MissionPrerequisiteKind.MissionCompleted, 1269, done), (1269, MissionPrerequisiteKind.MissionCompleted, 1267, done),
                // Set 9 (TaRapedia): 1275 none, 1277 after 1275, 1278 after 1277.
                (1277, MissionPrerequisiteKind.MissionCompleted, 1275, done), (1278, MissionPrerequisiteKind.MissionCompleted, 1277, done),
                // Set 13 (TaRapedia): 1280 none, 1281 and 1282 after 1280 (each other's hold is the scene's requirement, not a prerequisite).
                (1281, MissionPrerequisiteKind.MissionCompleted, 1280, done), (1282, MissionPrerequisiteKind.MissionCompleted, 1280, done),
                (1285, MissionPrerequisiteKind.MissionCompleted, 1261, done),
                // Set 10 (TaRapedia): 1287 and 1289 none, 1291 after 1289, 1293 after 1287.
                (1291, MissionPrerequisiteKind.MissionCompleted, 1289, done), (1293, MissionPrerequisiteKind.MissionCompleted, 1287, done),
                // Set 11 (TaRapedia): 1298 after 1295; 1295, 1303 and 1356 none.
                (1298, MissionPrerequisiteKind.MissionCompleted, 1295, done),
                // Set 12 (TaRapedia; triage D3, the set 11 auditor note): 1299 after Murky Waters done with its choice 1 (flag 258
                // at 1), 1300 after it done with its choice 2 (flag 257 at 1), 1302 after 1300 with flag 257 at 1.
                (1299, MissionPrerequisiteKind.PlayerFlagValue, null, null), (1299, MissionPrerequisiteKind.MissionCompleted, 1298, done),
                (1300, MissionPrerequisiteKind.PlayerFlagValue, null, null), (1300, MissionPrerequisiteKind.MissionCompleted, 1298, done),
                (1302, MissionPrerequisiteKind.PlayerFlagValue, null, null), (1302, MissionPrerequisiteKind.MissionCompleted, 1300, done),
                (1352, MissionPrerequisiteKind.MissionCompleted, 1262, done), (1352, MissionPrerequisiteKind.MissionCompleted, 1263, done),
                (1354, MissionPrerequisiteKind.MissionCompleted, 1262, done), (1354, MissionPrerequisiteKind.MissionCompleted, 1263, done),
                (1355, MissionPrerequisiteKind.MissionCompleted, 1262, done), (1355, MissionPrerequisiteKind.MissionCompleted, 1263, done),
                // Set 15 (TaRapedia; 1559 after Missing Scientists, Odesha's chain, ESTIMATE): 1360 after 1559, 1395 after 1360.
                (1360, MissionPrerequisiteKind.MissionCompleted, 1559, done), (1395, MissionPrerequisiteKind.MissionCompleted, 1360, done),
                // Set 16 (TaRapedia: 1447 after 1395, 1451 after 1532; 1532 after 1447 and 1452 after 1451 the story's, ESTIMATE).
                (1447, MissionPrerequisiteKind.MissionCompleted, 1395, done), (1451, MissionPrerequisiteKind.MissionCompleted, 1532, done),
                (1452, MissionPrerequisiteKind.MissionCompleted, 1451, done),
                // Set 19 (TaRapedia: 1497 after 1500, 1498 after 1497; 1500 after 1581 the story's, ESTIMATE).
                (1497, MissionPrerequisiteKind.MissionCompleted, 1500, done), (1498, MissionPrerequisiteKind.MissionCompleted, 1497, done),
                // Set 20 (TaRapedia): 1499 and 1551 after 1498, 1501 after 1499.
                (1499, MissionPrerequisiteKind.MissionCompleted, 1498, done),
                (1500, MissionPrerequisiteKind.MissionCompleted, 1581, done),
                (1501, MissionPrerequisiteKind.MissionCompleted, 1499, done),
                (1532, MissionPrerequisiteKind.MissionCompleted, 1447, done),
                // Set 18 (TaRapedia): 1536 after 1535, 1537 after 1536, 1581 after 1537.
                (1536, MissionPrerequisiteKind.MissionCompleted, 1535, done), (1537, MissionPrerequisiteKind.MissionCompleted, 1536, done),
                (1551, MissionPrerequisiteKind.MissionCompleted, 1498, done),
                (1559, MissionPrerequisiteKind.MissionCompleted, 1280, done),
                (1581, MissionPrerequisiteKind.MissionCompleted, 1537, done)
            }, prerequisites);
            var flags = harness.World.MissionPrerequisiteEntries.AsNoTracking()
                .Where(entry => entry.ContentRevision == AshenMissionDataV1.Revision && entry.Kind == MissionPrerequisiteKind.PlayerFlagValue)
                .AsEnumerable().Select(entry => (entry.MissionId, entry.PlayerFlagId, entry.PlayerFlagValue)).OrderBy(entry => entry.MissionId).ToArray();
            CollectionAssert.AreEqual(new (uint, uint?, uint?)[] { (1258, 251, 1), (1264, 250, 1), (1267, 251, 1), (1299, 258, 1), (1300, 257, 1), (1302, 257, 1) }, flags,
                "1258: flag 251 at 1; 1264: flag 250 at 1; 1267: flag 251 at 1; 1299: flag 258 at 1; 1300 and 1302: flag 257 at 1");
            Assert.IsFalse(harness.World.Set<MissionRepeatPolicyEntry>().AsNoTracking()
                .Any(entry => entry.ContentRevision == AshenMissionDataV1.Revision));
        }

        /// <summary>The missions' sources and how far each is trusted: the client 1.0, TaRapedia 0.5, the reconstruction 0.3.</summary>
        [TestMethod]
        public void TheSourcesAreTrustedAsFarAsTheyGo()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var evidence = harness.World.MissionEvidenceEntries.AsNoTracking()
                .Where(entry => entry.ContentRevision == AshenMissionDataV1.Revision).ToArray();
            foreach (var mission in MissionIds)
                CollectionAssert.AreEqual(new[]
                    {
                        (MissionEvidenceSourceKind.Client, 1.0), (MissionEvidenceSourceKind.Documentation, 0.5),
                        (MissionEvidenceSourceKind.Reconstruction, 0.3)
                    },
                    evidence.Where(entry => entry.MissionId == mission && !(mission == AshenFixAV1.MissionId && entry.EvidenceId == AshenFixAV1.Evidence)
                        && !(mission == AshenFixBV1.MissionId && entry.EvidenceId == AshenFixBV1.Evidence)
                        && !(mission == AshenFixDV1.MissionId && entry.EvidenceId == AshenFixDV1.Evidence)
                        && !(AshenFixEV1.Evidences.Any(fix => fix.Mission == mission) && entry.EvidenceId == AshenFixEV1.Evidence)
                        && !AshenFixFV1.Evidences.Any(fix => fix.Mission == mission && fix.Evidence == entry.EvidenceId && entry.SourceUri == AshenFixFV1.Source)
                        && !AshenFixGV1.Evidences.Any(fix => fix.Mission == mission && fix.Evidence == entry.EvidenceId && entry.SourceUri == AshenFixGV1.Source)
                        && !(mission == AshenFixHV1.Mission && entry.EvidenceId == AshenFixHV1.EvidenceId && entry.SourceUri == AshenFixHV1.Source)
                        && !(mission == AshenFixIV1.Mission && entry.EvidenceId == AshenFixIV1.EvidenceId && entry.SourceUri == AshenFixIV1.Source)
                        && !(mission == AshenFixLV1.Mission && entry.EvidenceId == AshenFixLV1.EvidenceId && entry.SourceUri == AshenFixLV1.Source)
                        && !(mission == AshenFixLV1.Blast && entry.EvidenceId == AshenFixLV1.BlastEvidenceId && entry.SourceUri == AshenFixLV1.Source)
                        && !(entry.SourceUri == AshenFixJV1.Source && entry.Confidence == 0.3 && entry.SourceKind == MissionEvidenceSourceKind.Reconstruction &&
                             ((mission == AshenFixJV1.Threshold && entry.EvidenceId == AshenFixJV1.ThresholdEvidence) ||
                              (mission == AshenFixJV1.Digging && entry.EvidenceId == AshenFixJV1.DiggingEvidence) ||
                              (mission == AshenFixJV1.Desistance && entry.EvidenceId == AshenFixJV1.DesistanceEvidence)))
                        && !(entry.SourceUri == AshenFixMV1.Source && entry.Confidence == 0.3 && entry.SourceKind == MissionEvidenceSourceKind.Reconstruction &&
                             ((entry.EvidenceId == AshenFixMV1.Evidence && (mission == AshenFixMV1.LastStand || mission == AshenFixMV1.Run || mission == AshenFixMV1.Queen || mission == AshenFixMV1.Raid)) ||
                              (entry.EvidenceId == AshenFixMV1.BlastEvidence && mission == AshenFixMV1.Blast)))
                        && !(entry.SourceUri == AshenFixOV1.Source && entry.Confidence == 0.3 && entry.SourceKind == MissionEvidenceSourceKind.Reconstruction &&
                             mission == AshenFixOV1.Run && entry.EvidenceId == AshenFixOV1.Evidence)
                        && !(entry.SourceUri == AshenFixPV1.Source && entry.Confidence == 0.3 && entry.SourceKind == MissionEvidenceSourceKind.Reconstruction &&
                             mission == AshenFixPV1.Run && entry.EvidenceId == AshenFixPV1.Evidence)
                        && !(entry.SourceUri == AshenFixRV1.Source && entry.Confidence == 0.3 && entry.SourceKind == MissionEvidenceSourceKind.Reconstruction &&
                             mission == AshenFixRV1.Queen && entry.EvidenceId == AshenFixRV1.Evidence)
                        && !(entry.SourceUri == AshenFixSV1.Source && entry.Confidence == 0.3 && entry.SourceKind == MissionEvidenceSourceKind.Reconstruction &&
                             mission == AshenFixSV1.Raid && entry.EvidenceId == AshenFixSV1.Evidence)
                        && !(entry.SourceUri == AshenFixS2V1.Source && entry.Confidence == 0.3 && entry.SourceKind == MissionEvidenceSourceKind.Reconstruction &&
                             mission == AshenFixS2V1.Blast && entry.EvidenceId == AshenFixS2V1.Evidence)
                        && !(entry.SourceUri == AshenFixTV1.Source && entry.Confidence == 0.3 && entry.SourceKind == MissionEvidenceSourceKind.Reconstruction &&
                             mission == AshenFixTV1.Blast && entry.EvidenceId == AshenFixTV1.Evidence)
                        && !(entry.SourceUri == AshenFixVV1.Source && entry.Confidence == 0.3 && entry.SourceKind == MissionEvidenceSourceKind.Reconstruction &&
                             mission == AshenFixVV1.Queen && entry.EvidenceId == AshenFixVV1.Evidence)
                        && !(entry.SourceUri == AshenFixV2V1.Source && entry.Confidence == 0.3 && entry.SourceKind == MissionEvidenceSourceKind.Reconstruction &&
                             mission == AshenFixV2V1.Queen && entry.EvidenceId == AshenFixV2V1.Evidence))
                        .OrderBy(entry => entry.EvidenceId)
                        .Select(entry => (entry.SourceKind, entry.Confidence)).ToArray(), $"{mission} evidence");
        }

        /// <summary>The rewards: XP and credits of TaRapedia, the four items (or stacks) to choose one of (written out here).</summary>
        [TestMethod]
        public void TheRewardsAreTaRapediasWithOneItemToChoose()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            foreach (var (mission, experience, credits, items) in new (uint, uint, uint, (uint, uint)[])[]
            {
                (1250, 42500, 4000, new[] { (45065U, 2U), (111029U, 3U), (47600U, 1U), (111019U, 3U) }),
                (1273, 45000, 4100, new[] { (111039U, 3U), (45065U, 2U), (111029U, 3U), (47600U, 1U) }),
                (1274, 90000, 6150, new[] { (123046U, 1U), (123141U, 1U), (123166U, 1U), (123026U, 1U) }),
                (1283, 45000, 4100, new[] { (111019U, 3U), (45063U, 6U), (45447U, 3U), (111233U, 1U) }),
                (1284, 47500, 4200, new[] { (111029U, 3U), (47600U, 1U), (111019U, 3U), (45063U, 6U) }),
                (1242, 90000, 6150, new[] { (49616U, 1U), (48712U, 1U), (48300U, 1U), (15025U, 1U) }),
                (1243, 90000, 6150, new[] { (34116U, 1U), (40502U, 1U), (36787U, 1U), (31088U, 1U) }),
                (1244, 90000, 6150, new[] { (28634U, 1U), (38952U, 1U), (18770U, 1U), (35740U, 1U) }),
                (1245, 90000, 6150, new[] { (33987U, 1U), (23698U, 1U), (21100U, 1U), (46013U, 1U) }),
                (1247, 180000, 8200, new[] { (49576U, 1U), (45777U, 1U), (47948U, 1U), (45762U, 1U) }),
                (1248, 180000, 8200, new[] { (12065U, 1U), (11953U, 1U), (47772U, 1U), (15111U, 1U) }),
                (1249, 90000, 6150, new[] { (24086U, 1U), (20504U, 1U), (36188U, 1U), (19286U, 1U) }),
                (1262, 95000, 6300, new[] { (46029U, 1U), (34374U, 1U), (23569U, 1U), (31241U, 1U) }),
                (1263, 95000, 6300, new[] { (49616U, 1U), (49536U, 1U), (15301U, 1U), (14861U, 1U) }),
                (1272, 190000, 8400, new[] { (15335U, 1U), (48108U, 1U), (12125U, 1U), (14534U, 1U) }),
                (1352, 200000, 8600, new[] { (12099U, 1U), (15442U, 1U), (14980U, 1U), (48108U, 1U) }),
                (1354, 100000, 6450, new[] { (19802U, 1U), (39081U, 1U), (20951U, 1U), (31394U, 1U) }),
                (1355, 200000, 8600, new[] { (33342U, 1U), (24086U, 1U), (19544U, 1U), (36638U, 1U) }),
                // Set 6: TaRapedia's stacks for 1254 and 1261, 1254's for 1258 and 1259 (TaRapedia names none: zone median,
                // ESTIMATE), the lowest Uncommon armour of TaRapedia's four kinds at level 42 for 1285.
                (1254, 45000, 4100, new[] { (111029U, 3U), (47600U, 1U), (111019U, 3U), (45063U, 6U) }),
                (1258, 45000, 4100, new[] { (111029U, 3U), (47600U, 1U), (111019U, 3U), (45063U, 6U) }),
                (1259, 45000, 4100, new[] { (111029U, 3U), (47600U, 1U), (111019U, 3U), (45063U, 6U) }),
                (1261, 45000, 4100, new[] { (45447U, 3U), (111233U, 1U), (111049U, 3U), (45064U, 3U) }),
                (1285, 190000, 8400, new[] { (38823U, 1U), (23828U, 1U), (20802U, 1U), (29093U, 1U) }),
                // Set 7: TaRapedia's XP and credits; 1264 the lowest Uncommon of TaRapedia's four armour kinds (icon 2; Motor Assist
                // has no Uncommon below V04) at level 40; 1266 TaRapedia's one item (the fire injection gun); 1265 TaRapedia names
                // none: 1254's stacks (zone median, ESTIMATE).
                (1264, 95000, 6300, new[] { (23698U, 1U), (29113U, 1U), (20802U, 1U), (36039U, 1U) }), // fix D: Motor Assist Legs 43-47 (TaRapedia "green, 43")
                (1266, 95000, 6300, new[] { (45777U, 1U) }),
                (1265, 200000, 6300, new[] { (111029U, 3U), (47600U, 1U), (111019U, 3U), (45063U, 6U) }),
                // Set 8: TaRapedia's credits (1267, 1269; 1268 none: as 1265); XP as Latan's twins (TaRapedia "?", ESTIMATE); 1267
                // the grade [4] armour modules of TaRapedia's four resistances; 1269 the lowest Uncommon of TaRapedia's four kinds
                // at level 42 (Cryogenic Blade, Area Healing Disc, Sonic Pistol, Pulse Staff); 1268 1254's stacks.
                (1267, 95000, 6300, new[] { (123016U, 1U), (123026U, 1U), (123031U, 1U), (123071U, 1U) }),
                (1269, 95000, 6300, new[] { (12101U, 1U), (119882U, 1U), (14452U, 1U), (15220U, 1U) }),
                (1268, 200000, 6300, new[] { (111029U, 3U), (47600U, 1U), (111019U, 3U), (45063U, 6U) }),
                // Set 9: TaRapedia's XP and credits; the lowest Uncommon of TaRapedia's four kinds at levels 40, 41 and 42 (1278's
                // armour with TaRapedia's version numbers V03, V02, V05, V09).
                (1275, 95000, 6300, new[] { (14658U, 1U), (48712U, 1U), (49172U, 1U), (15025U, 1U) }),
                (1277, 190000, 8400, new[] { (12101U, 1U), (11953U, 1U), (48300U, 1U), (14409U, 1U) }),
                (1278, 190000, 8400, new[] { (19673U, 1U), (20504U, 1U), (37985U, 1U), (45843U, 1U) }),
                // Set 10: TaRapedia's XP and credits; the lowest template of TaRapedia's four kinds at levels 41 (1287, 1289) and 42
                // (1291, 1293); 1287's armour with TaRapedia's version numbers V11, V05, V05, V01.
                (1287, 180000, 8200, new[] { (45986U, 1U), (35019U, 1U), (46069U, 1U), (13043U, 1U) }),
                (1289, 180000, 8200, new[] { (48108U, 1U), (49108U, 1U), (15154U, 1U), (14409U, 1U) }),
                (1291, 190000, 8400, new[] { (15550U, 1U), (12131U, 1U), (49172U, 1U), (15111U, 1U) }),
                (1293, 200000, 8600, new[] { (45773U, 1U), (12077U, 1U), (15550U, 1U), (14492U, 1U) }),
                // Set 11: 1295 TaRapedia's credits and 100000 XP (ESTIMATE), the lowest Uncommon of its four weapons at level 41;
                // 1298 TaRapedia's; 1303 and 1356 no source (D6): the zone median and Formal Complaints' stacks.
                (1295, 100000, 6450, new[] { (48752U, 1U), (12125U, 1U), (47772U, 1U), (11957U, 1U) }),
                (1298, 50000, 4300, new[] { (45063U, 6U), (111233U, 1U), (45447U, 3U), (111049U, 3U) }),
                (1303, 95000, 6300, new[] { (111029U, 3U), (47600U, 1U), (111019U, 3U), (45063U, 6U) }),
                (1356, 95000, 6300, new[] { (111029U, 3U), (47600U, 1U), (111019U, 3U), (45063U, 6U) }),
                // Set 12: 1299 no source (D6): the zone median and Formal Complaints' stacks; 1300 TaRapedia's credits and 100000 XP
                // (ESTIMATE), the lowest of TaRapedia's four armour kinds and versions at level 42; 1302 TaRapedia's credits and
                // 45000 XP (ESTIMATE), the grade [4] resist modules in TaRapedia's order.
                (1299, 95000, 6300, new[] { (111029U, 3U), (47600U, 1U), (111019U, 3U), (45063U, 6U) }),
                (1300, 100000, 6450, new[] { (34374U, 1U), (24086U, 1U), (46037U, 1U), (40631U, 1U) }),
                (1302, 45000, 4000, new[] { (123026U, 1U), (123031U, 1U), (123071U, 1U), (123016U, 1U) }),
                // Set 13: TaRapedia's XP and credits; the lowest Uncommon of TaRapedia's four kinds at levels 41 (1280: V02 Bio
                // Vest, Stealth and Graviton Gloves, Hazmat Helmet) and 42 (1281: physical rocket launcher, electric staff, fire
                // injection gun, physical shotgun; 1282: physical leech gun, electric net gun, physical pistol, fire grenade
                // launcher for the "Incendiary RPG Launcher").
                (1280, 95000, 6300, new[] { (19286U, 1U), (38952U, 1U), (23698U, 1U), (20653U, 1U) }),
                (1281, 190000, 8400, new[] { (49140U, 1U), (15189U, 1U), (12143U, 1U), (11963U, 1U) }),
                (1282, 200000, 8600, new[] { (45763U, 1U), (12131U, 1U), (14409U, 1U), (47916U, 1U) })
            })
            {
                var reward = harness.World.Set<MissionRewardDefinitionEntry>().AsNoTracking()
                    .Single(entry => entry.MissionId == mission && entry.ContentRevision == AshenMissionDataV1.Revision);
                Assert.AreEqual((experience, credits, (byte)1), (reward.Experience, reward.Credits, reward.SelectionCount), $"{mission}");
                CollectionAssert.AreEqual(items, harness.World.MissionRewardItemEntries.AsNoTracking()
                    .Where(entry => entry.MissionId == mission && entry.ContentRevision == AshenMissionDataV1.Revision)
                    .OrderBy(entry => entry.ItemId).AsEnumerable().Select(entry => (entry.ItemTemplateId, entry.Quantity)).ToArray(), $"{mission} items");
            }
        }

        /// <summary>
        /// The indicators: all on Ashen Desert (MissionMarkerProjection), keyed above UnnamedFrom, but Eyes on the Ground's five,
        /// keyed by the client's names 155-159 "Atta Beacon Point 1" .. "5", and Artillery Annihilation's four, 160-163 "Bane
        /// Heavy Mortar 1" .. "4" (missionobjectiveindicatorlanguage).
        /// </summary>
        [TestMethod]
        public void IndicatorsAreOnAshenDesertAndAboveUnnamedButTheClientsNamedOnes()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var indicators = harness.World.MissionIndicatorEntries.AsNoTracking()
                .Where(entry => entry.ContentRevision == AshenMissionDataV1.Revision).ToArray();
            Assert.HasCount(Indicators + CavernsIndicators + OutpostIndicators + HiveIndicators + EntryIndicators + QueenIndicators, indicators);
            var catalog = new MissionContentCatalog(harness, new Dictionary<uint, Mission>(), null);
            catalog.Load();
            foreach (var row in indicators)
            {
                Assert.AreEqual(AshenConscriptionQueenTests.Missions.Contains(row.MissionId) ? AshenConscriptionQueenTests.MarkerMap(row.MissionId, row.IndicatorId) : AshenConscriptionEntryTests.Missions.Contains(row.MissionId) ? AshenConscriptionEntryTests.MarkerMap(row.MissionId, row.IndicatorId) : AshenAvernusOutpostTests.Missions.Contains(row.MissionId) || AshenAvernusHiveTests.Missions.Contains(row.MissionId) ? AshenAvernusOutpostNpcsV1.Outpost : OnCaverns(row.MissionId, row.ObjectiveId) ? AshenIndraFacilityNpcsV1.Caverns : Map, MissionMarkerProjection.ResolveMap(catalog, row.MissionId, row.ObjectiveId,
                    new MissionIndicator { IndicatorId = row.IndicatorId }), $"mission {row.MissionId} objective {row.ObjectiveId}: on its map");
                // Fix H: 1559.1's second marker, on the valley's lift pad, unnamed (the client has no name for a pad).
                var padMarker = row.MissionId == AshenFixHV1.Mission && row.ObjectiveId == AshenFixHV1.Objective && row.IndicatorId == MissionIndicator.UnnamedFrom + AshenFixHV1.Marker;
                if (!padMarker && NamedMarker(row.MissionId, row.ObjectiveId) is { } name)
                    Assert.AreEqual(name, row.IndicatorId, $"mission {row.MissionId} objective {row.ObjectiveId}: the client's name");
                else
                    Assert.IsTrue(row.IndicatorId > MissionIndicator.UnnamedFrom, $"mission {row.MissionId} objective {row.ObjectiveId} indicator {row.IndicatorId}");
            }
        }

        /// <summary>
        /// The client's name of an Ashen marker, where the client has one: Eyes on the Ground's beacon <c>n</c>, "Atta Beacon
        /// Point n" (missionobjectiveindicatorlanguage 154 + n); Artillery Annihilation's mortar <c>n</c>, "Bane Heavy Mortar n"
        /// (159 + n); null for the others.
        /// </summary>
        internal static uint? NamedMarker(uint mission, uint objective) => (mission, objective) switch
        {
            (1245, >= 1 and <= 5) => 154 + objective,
            (1249, >= 1 and <= 4) => 159 + objective,
            // Set 15 (missionobjectiveindicatorlanguage): 1559's 261 (the caverns' door) and 260 (Diren); 1360's 224 (Bendal), 222
            // (the recon team), 223 (the Watch Post); 1395's 225-228 (Aksrah, Naimi, Kuutk, the power supply); 1395,6 has none.
            (1559, 1) => 261, (1559, 2) => 260,
            (1360, 1) => 224, (1360, 2) => 222, (1360, 3) => 223,
            (1395, >= 2 and <= 5) => 223 + objective,
            // Set 16: 1447's 134 (the mining operation), 219 (the cave entrance), 220 (the tunnel exit), 218 (the box), 221 (each
            // drill); 1532's 222 (Drof); 1451's 216 (the valves), 217 (the life support systems); 1452's 213, 212, 214, 210, 211, 209,
            // 215, 260.
            (1447, 1) => 134, (1447, 2) => 219, (1447, 3) => 220, (1447, 5) => 218, (1447, >= 6 and <= 11) => 221,
            (1532, 1) => 222,
            (1451, 1 or 3 or 4) => 216, (1451, 2 or (>= 5 and <= 8)) => 217,
            (1452, 1) => 213, (1452, 2) => 212, (1452, 3) => 214, (1452, 4) => 210, (1452, 5) => 211, (1452, 6) => 209, (1452, 7) => 215,
            (1452, 8) => 260,
            // Set 19: 1497's 251 (Conscription Database Interface, the terminal), 1498's 287 (the Bane teleporter).
            (1497, 2) => 251, (1498, 3) => 287,
            _ => null
        };

        /// <summary>Whether an Ashen-revision marker is on Indra Caverns (set 15's but 1559's first, before the door on Ashen Desert).</summary>
        internal static bool OnCaverns(uint mission, uint objective) =>
            mission is 1360 or 1395 or 1447 or 1532 or 1451 or 1452 || mission == 1559 && objective == 2;

        /// <summary>Every visible Ashen objective has a marker; the hidden objectives are 1263's server-only tally (2), 1254's and 1298's server-only outcomes (4), 1278's losses (7) and set 13's "Flag Reader"s (1281's 11, 1282's 3): the scenes of 1243 and 1245 hold items and spots only.</summary>
        [TestMethod]
        public void EveryVisibleObjectiveHasAMarker()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var world = harness.World;
            var revision = AshenMissionDataV1.Revision;
            var marked = world.MissionIndicatorEntries.AsNoTracking().Where(entry => entry.ContentRevision == revision)
                .Select(entry => new { entry.MissionId, entry.ObjectiveId }).ToList()
                .Select(entry => (entry.MissionId, entry.ObjectiveId)).ToHashSet();
            var scenes = Scenes(harness);
            CollectionAssert.AreEquivalent(new uint[] { 1243, 1245, 1263, 1352, 1354, 1254, 1259, 1261, 1285, 1264, 1265, 1266, 1267, 1268, 1269, 1275, 1277, 1278, 1287, 1289, 1291, 1293, 1295, 1298, 1356, 1299, 1300, 1302, 1280, 1281, 1282, 1358, 1286, 1736, 1559, 1360, 1395, 1447, 1532, 1451, 1452, 1473, 1533, 1535, 1536, 1537, 1581, 1500, 1497, 1498, 1551, 1499, 1501 }, scenes.Select(scene => scene.MissionId).ToArray(), "the scenes");
            var hidden = scenes.SelectMany(scene => (scene.Scene.HiddenObjectiveIds ?? new()).Select(objective => (scene.MissionId, objective))).ToHashSet();
            CollectionAssert.AreEquivalent(new[] { (1263U, 2U), (1254U, 4U), (1278U, 7U), (1298U, 4U), (1281U, 11U), (1282U, 3U), (1736U, 3U), (1736U, 4U), (1736U, 5U), (1736U, 6U),
                    (1360U, 4U), (1360U, 5U), (1360U, 6U), (1395U, 7U), (1395U, 8U), (1447U, 12U), (1447U, 13U), (1447U, 14U), (1452U, 9U),
                    (1473U, 7U), (1533U, 1U), (1535U, 6U), (1535U, 7U), (1535U, 8U), (1535U, 9U), (1535U, 10U),
                    (1498U, 6U), (1498U, 7U), (1551U, 3U), (1499U, 2U), (1499U, 7U), (1499U, 8U), (1499U, 9U) }, hidden.ToArray(),
                "1263's tally, 1254's and 1298's outcomes, 1278's losses, set 13's two Flag Readers (1281's tally, 1282's close), 1736's helpers (the researchers, the ambushes, the first pair's tally) and set 15's (1360's door, tally and opener; 1395's key-pass gate and hand) and set 16's (1447's power, rear exit and opener; 1452's hand) and set 17's (1473's key card, 1533's graveyard, 1535's waves, ready, tracker and leads) and set 19's (1498's gathering counter and opener) and set 20's (1551's queen kill; 1499's master, ledge and either talk), hidden");
            var objectives = world.MissionObjectiveDefinitionEntries.AsNoTracking().Where(entry => entry.ContentRevision == revision)
                .Select(entry => new { entry.MissionId, entry.ObjectiveId }).ToList();
            Assert.HasCount(5 + 2 + 1 + 1 + 5 + 2 + 1 + 4 + 1 + 2 + 1 + 11 + 3 + 2 + 4 + 1 + 1 + 1 + 1 + 2 + 2 + 2 + 2 + 2 + 2 + 1 + 4 + 3 + 2 + 2 + 2 + 2 + 4 + 4 + 1 + 3 + 1 + 1 + 4 + 1 + 11 + 3 + 5 + 1 + 2 + 6 + 7 + 13 + 1 + 8 + 9 + 7 + 1 + 10 + 3 + 7 + 2 + 1 + 2 + 7 + 3 + 9 + 7, objectives, "the objectives are read");
            var missing = objectives.Select(entry => (entry.MissionId, entry.ObjectiveId)).Where(key => !marked.Contains(key) && !hidden.Contains(key))
                .OrderBy(key => key).ToArray();
            Assert.IsFalse(hidden.Any(marked.Contains), "no marker on a hidden objective");
            Assert.IsEmpty(missing, string.Join(", ", missing));
        }

        /// <summary>The markers stand at their objective's target: an NPC, or a Fithik pool on Stonewall Ridge.</summary>
        [TestMethod]
        public void TheMarkersStandAtTheirObjectivesTargets()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var rows = harness.World.MissionIndicatorEntries.AsNoTracking()
                .Where(entry => entry.ContentRevision == AshenMissionDataV1.Revision).ToList();
            var expected = new List<(uint Mission, uint Objective, double X, double Z, double Radius)>
            {
                (1250, 1, -132, -875, 8),                           // Mason Linier
                (1273, 1, -62, -414, 8),                            // Emmons
                (1274, 1, AshenFixAV1.MarkerNow.X, AshenFixAV1.MarkerNow.Z, 30), // Fithik pool 540067, a point since fix job A (540066's Fithik are in an unreachable pocket)
                (1283, 1, 686, -495, 8),                            // Yamamoto
                (1284, 1, -507.4, 162.4, 8),                        // Daris Linier
                (1242, 1, -493, -240, 20), (1242, 1, -429, -240, 20), // the Ashen Atta Soldier pools 790046, 790047
                (1242, 2, -461, -216, 20), (1242, 2, -461, -264, 20), // the Ashen Atta Worker pools 790048, 790049
                (1243, 1, -533, -256, 20), (1243, 1, -405, -272, 20), // the Mutated Atta Soldier pools 790050, 790051
                (1244, 1, -260, -310, 15),                          // Exing, moved (set 2, fix B)
                (1245, 1, -431, -203, 8), (1245, 2, -554, -230, 8), (1245, 3, -596, -83, 8), (1245, 4, -476, -17, 8), (1245, 5, -338, -80, 8),
                (1247, 1, -213, -302, 20), (1247, 1, -125, -359, 20), // the Thrax Ravager pools 790085, 790086 (moved by fix E)
                (1247, 3, -160, -260, 20), (1247, 3, -120, -225, 20), // the Thrax Soldier pools 790087, 790088
                (1248, 1, -174.4, -482.2, 50), (1248, 1, -161.6, -392.2, 40), // the Desert Predators' south and north beats
                (1249, 1, -210.888, -409.662, 10), (1249, 2, -195.561, -337.739, 10), // the mortar pools 592191, 592192
                (1249, 3, -180.58, -282.694, 10), (1249, 4, -140.3, -285.3, 10),       // and 592193, 592194
                (1262, 1, 560, -250, 20), (1262, 1, 600, -230, 20), (1262, 1, 640, -250, 20), // the Xanx pools 790122-790124
                (1263, 1, 512.5, -335.5, 55),                       // over the six fallen
                (1272, 1, 580, -345, 15), (1272, 1, 585, -370, 15), // the Desert Stalker pools 790125, 790126
                (1352, 1, 440.34, -200.983, 10), (1352, 2, 490.114, -194.194, 10), // the trench mortar pools 592199, 592202
                (1352, 3, 524.666, -170.568, 10), (1352, 4, 550.574, -130.494, 10), // and 592203, 592204
                (1352, 5, 376, 117, 10), (1352, 6, 412, 117, 10), (1352, 7, 428, 117, 10), // the launchers' Detonation Devices
                (1352, 8, 376, 155, 10), (1352, 9, 416, 155, 10), (1352, 10, 424, 155, 10),
                (1352, 11, 501.4, -174.1, 75),                      // over the trench, round the four mortars
                (1354, 1, 318, -14, 8), (1354, 2, 326, -34, 8), (1354, 3, 314, -52, 8), // the power generators
                (1355, 1, 385, 82, 15), (1355, 1, 420, 95, 15), (1355, 1, 466, 132, 15), (1355, 1, 430, 160, 15), (1355, 1, 358, 160, 15), // the Linker pools
                (1355, 2, 296, -30, 15),                            // Overseer Naxveik, moved
                (1254, 1, 576, -321, 8), (1254, 2, 576, -321, 8), (1254, 3, 576, -321, 8), // Latan
                (1258, 1, 686, -495, 8), (1259, 1, 686, -495, 8),   // Yamamoto
                (1261, 1, -132, -875, 8),                           // Mason Linier
                (1285, 1, 566, -136, 25), (1285, 1, 487, -261.3, 25), (1285, 1, 487, -219, 25), // the Boneyard's Thrax pools 531038, 531039, 531041 (moved by set 6)
                (1264, 1, 700, -250, 20), (1264, 1, 690, -310, 20), // the Nitroglazer pools 790243, 790244
                (1264, 2, 440, -320, 20), (1264, 2, 440, -360, 20), // the Hominis Machina pools 790245, 790246
                (1266, 1, 566, -136, 25), (1266, 1, 487, -261.3, 25), (1266, 1, 487, -219, 25), // rifles: the Boneyard's Thrax pools
                (1266, 2, -213, -302, 25), (1266, 2, -125, -359, 25), // launchers: the highway's Ravager pools 790085, 790086 (fix E)
                (1265, 1, 580, -345, 15), (1265, 1, 585, -370, 15), // the Desert Stalker pools 790125, 790126
                (1265, 2, -174.4, -482.2, 50), (1265, 2, -161.6, -392.2, 40), // the Desert Predators' beats (as 1248's)
                // Set 8: Ito's twins on the same places as Latan's.
                (1267, 1, 700, -250, 20), (1267, 1, 690, -310, 20), // the Nitroglazer pools 790243, 790244
                (1267, 2, 440, -320, 20), (1267, 2, 440, -360, 20), // the Hominis Machina pools 790245, 790246
                (1269, 1, 566, -136, 25), (1269, 1, 487, -261.3, 25), (1269, 1, 487, -219, 25), // rifles: the Boneyard's Thrax pools 531038, 531039, 531041
                (1269, 2, -213, -302, 25), (1269, 2, -125, -359, 25), // launchers: the highway's Ravager pools 790085, 790086 (fix E)
                (1268, 1, 580, -345, 15), (1268, 1, 585, -370, 15), // the Desert Stalker pools 790125, 790126
                (1268, 2, -174.4, -482.2, 50), (1268, 2, -161.6, -392.2, 40), // the Desert Predators' beats (as 1248's)
                // Set 9: 1275 on the salvage crate; 1277 at the towers' ground teleporters 539, 538, 537, 540 (World DB); 1278 over
                // the four batteries.
                (1275, 1, CrateX, CrateZ, 8),
                (1277, 1, -62.80, -400.71, 8), (1277, 2, -3.80, -533.49, 8), (1277, 3, 59.41, -474.90, 8), (1277, 4, 146.75, -602.82, 8),
                (1278, 1, 108, -510, 70), (1278, 2, 108, -510, 70),
                // Set 10: Ida Hayner's orders, 1 on the hide pools (r20), 2 on the boss (r15).
                (1287, 1, -493, -240, 20), (1287, 1, -429, -240, 20), (1287, 1, -461, -216, 20), (1287, 1, -461, -264, 20), // the Atta pools 790046-790049
                (1287, 1, -355, -210, 20),                          // fix F: 1287's own Ashen Atta Soldier pool 790378
                (1287, 2, -439, -298, 15),                          // Sally (moved by fix E)
                (1289, 1, 330, -585, 20), (1289, 1, 280, -578, 20), // the Lasher pools 790363, 790364
                (1289, 2, 315, -622, 15),                           // Molly (moved by fix E)
                (1291, 1, 770, -400, 20), (1291, 1, 216, 486, 20), (1291, 1, -189, 635, 20), // the Beam Manta pools 790368 (moved by fix E)-790370
                (1291, 2, -250, 632, 15),                      // Trudy (moved by fix E)
                (1293, 1, -185, 520, 20), (1293, 1, -215, 525, 20), // the Granitour pools 790374, 790375
                (1293, 2, -180, 570, 15),                           // Wendy
                // Set 11: 1295 on the four panels, 1298 at Hale, 1303 on Ulovik, 1356 on the fuel pumps (r10: each takes in its device).
                (1295, 1, -305.7, 198.1, 8), (1295, 2, -292, 305, 8), (1295, 3, -375.5, 394.1, 8), (1295, 4, -367, 514, 8),
                (1298, 1, -516, 176, 8), (1298, 2, -516, 176, 8), (1298, 3, -516, 176, 8),
                (1303, 1, -235, 335, 15),
                (1356, 1, -76, 125, 10), (1356, 2, -76, 150, 10), (1356, 3, -95, 145, 10),
                // Set 12: 1299 on set 10's Granitour pools, 1300 on set 10's Lasher pools and the two ridge pools, 1302 on the panels.
                (1299, 1, -185, 520, 20), (1299, 1, -215, 525, 20),
                (1300, 1, 330, -585, 20), (1300, 1, 280, -578, 20), (1300, 1, -648, 314, 20), (1300, 1, -655, 360, 20),
                (1302, 1, -305.7, 198.1, 8), (1302, 2, -292, 305, 8), (1302, 3, -375.5, 394.1, 8), (1302, 4, -367, 514, 8),
                // Set 13: 1280 on Veyon, 1281 on the ten resistors, 1282 on Maiek and at Endala (resistors 1, 2, 3, 7 and Endala's
                // marker moved by fix G).
                (1280, 1, 178, 536, 8),
                (1281, 1, 158, 541, 8), (1281, 2, 60, 551, 8), (1281, 3, 201, 525, 8), (1281, 4, 174, 462, 8), (1281, 5, 130, 500, 8),
                (1281, 6, 176, 496, 8), (1281, 7, 66, 599, 8), (1281, 8, 96, 462, 8), (1281, 9, 124, 532, 8), (1281, 10, 96, 554, 8),
                (1282, 1, 344, -72, 8), (1282, 2, 112, 458, 8),
                // Set 14: 1358 at the five captains (Botha, Harris, Emmons, Yamamoto, Daris Linier), 1286 on Jirex (r15).
                (1358, 1, 378, -458.5, 8), (1358, 2, 405, -459.5, 8), (1358, 3, -62, -414, 8), (1358, 4, 686, -495, 8), (1358, 5, -507.4, 162.4, 8),
                (1286, 1, 426.8, -261, 15),
                // Set 15: 1559 before the Indra Caverns door (Ashen Desert, r6) and on Diren; 1360 on Bendal, Drof and over the
                // Watch Post (r45, both pools); 1395 on Aksrah, Naimi, Kuutk (r10), the crate and before the cell (r5).
                (1559, 1, 305, 326, 6), (1559, 2, -53, -395, 8),
                // Fix H: 1559's second objective 1 marker on the valley's lift pad 913.
                (1559, 1, AshenFixHV1.LowerPadAt.X, AshenFixHV1.LowerPadAt.Z, AshenFixHV1.MarkerRadius),
                // Fix I: 1360's Bendal marker with his body in the pillbox.
                (1360, 1, AshenFixIV1.BendalAt.X, AshenFixIV1.BendalAt.Z, 8), (1360, 2, -5, -221, 8), (1360, 3, AshenFixJV1.MarkerAt.X, AshenFixJV1.MarkerAt.Z, 45),
                (1395, 2, 153, -296, 8), (1395, 3, 9.83, -244.76, 8), (1395, 4, 59, -403, 10), (1395, 5, 24, -284, 8), (1395, 6, 12.5, -243, 5)
            };
            // Set 16: each on its target (AshenGothaPlantTests.Markers).
            expected.AddRange(AshenGothaPlantTests.Markers());
            // Set 17: each on its target (AshenAvernusOutpostTests.Markers).
            expected.AddRange(AshenAvernusOutpostTests.Markers());
            // Set 18: each on its target (AshenAvernusHiveTests.Markers).
            expected.AddRange(AshenAvernusHiveTests.Markers());
            // Set 19: each on its target (AshenConscriptionEntryTests.Markers).
            expected.AddRange(AshenConscriptionEntryTests.Markers());
            // Set 20: each on its target (AshenConscriptionQueenTests.Markers).
            expected.AddRange(AshenConscriptionQueenTests.Markers());
            Assert.HasCount(Indicators + CavernsIndicators + OutpostIndicators + HiveIndicators + EntryIndicators + QueenIndicators, expected);
            foreach (var (mission, objective, x, z, radius) in expected)
                Assert.IsTrue(rows.Any(row => row.MissionId == mission && row.ObjectiveId == objective &&
                        Vector2.Distance(new((float)row.PosX, (float)row.PosZ), new((float)x, (float)z)) < 0.1f && row.Radius == radius),
                    $"{mission} objective {objective}'s marker at ({x}, {z}) radius {radius}");
            Assert.HasCount(Indicators + CavernsIndicators + OutpostIndicators + HiveIndicators + EntryIndicators + QueenIndicators, rows);
            // Set 15: 1360's Watch Post marker holds both pools; each other marker on its target.
            // Fix J: the east pool and the marker at their new places (AshenFixJV1, to-fix row 36).
            foreach (var pool in new[] { AshenIndraFacilityNpcsV1.WatchPostWestAt, AshenFixJV1.PoolAt })
                Assert.IsLessThanOrEqualTo(AshenThresholdV1.WatchPostMarkerRadius - 2, Vector2.Distance(new((float)pool.X, (float)pool.Z),
                    new((float)AshenFixJV1.MarkerAt.X, (float)AshenFixJV1.MarkerAt.Z)), "a Watch Post pool inside 1360.3's marker");
            // Set 9: the four batteries' public pools (World DB rows 550001-550004) inside 1278's markers.
            foreach (var pool in harness.World.SpawnPoolEntries.AsNoTracking().Where(row => row.Id >= 550001 && row.Id <= 550004).ToList())
                foreach (var marker in rows.Where(row => row.MissionId == 1278))
                    Assert.IsLessThanOrEqualTo(marker.Radius - 2, Vector2.Distance(new((float)marker.PosX, (float)marker.PosZ), new((float)pool.PosX, (float)pool.PosZ)),
                        $"battery pool {pool.Id} inside 1278.{marker.ObjectiveId}'s marker");

            // Each Fithik pool's centre inside its marker.
            var pools = harness.World.SpawnPoolEntries.AsNoTracking().Where(row => row.Id == 540067).ToList();
            Assert.HasCount(1, pools);
            foreach (var pool in pools)
                Assert.IsTrue(rows.Any(row => row.MissionId == 1274 && Vector2.Distance(new((float)row.PosX, (float)row.PosZ),
                    new((float)pool.PosX, (float)pool.PosZ)) < 5), $"pool {pool.Id} by a marker");
            var pocket = harness.World.SpawnPoolEntries.AsNoTracking().Single(row => row.Id == AshenFithikInfestationV1.PocketPool);
            Assert.IsFalse(rows.Any(row => row.MissionId == 1274 && Vector2.Distance(new((float)row.PosX, (float)row.PosZ),
                new((float)pocket.PosX, (float)pocket.PosZ)) < 30), "no marker on 540066's unreachable pocket");

            // Set 2: each Atta pool's centre is its marker's centre, on the objective of its kind; Exing's pool under his.
            foreach (var (pool, creature, objective, mission) in new (uint, uint, uint, uint)[]
            {
                (790046, AttaSoldier, 1, 1242), (790047, AttaSoldier, 1, 1242), (790048, AttaWorker, 2, 1242), (790049, AttaWorker, 2, 1242),
                (790050, MutatedSoldier, 1, 1243), (790051, MutatedSoldier, 1, 1243), (Exing, Exing, 1, 1244),
                (790085, Ravager, 1, 1247), (790086, Ravager, 1, 1247), (790087, ThraxSoldier, 3, 1247), (790088, ThraxSoldier, 3, 1247),
                (592191, 790081, 1, 1249), (592192, 790082, 2, 1249), (592193, 790083, 3, 1249), (592194, 790084, 4, 1249),
                (790122, Xanx, 1, 1262), (790123, Xanx, 1, 1262), (790124, Xanx, 1, 1262), (790125, Stalker, 1, 1272), (790126, Stalker, 1, 1272),
                (592199, 790160, 1, 1352), (592202, 790161, 2, 1352), (592203, 790162, 3, 1352), (592204, 790163, 4, 1352),
                (790164, Linker, 1, 1355), (790165, Linker, 1, 1355), (790166, Linker, 1, 1355), (790167, Linker, 1, 1355), (790168, Linker, 1, 1355),
                (Naxveik, Naxveik, 2, 1355),
                (531038, ThraxSoldier, 1, 1285), (531039, ThraxSoldier, 1, 1285), (531041, ThraxSoldier, 1, 1285),
                (790243, Nitroglazer, 1, 1264), (790244, Nitroglazer, 1, 1264), (790245, Machina, 2, 1264), (790246, Machina, 2, 1264),
                (531038, ThraxSoldier, 1, 1266), (531039, ThraxSoldier, 1, 1266), (531041, ThraxSoldier, 1, 1266),
                (790085, Ravager, 2, 1266), (790086, Ravager, 2, 1266), (790125, Stalker, 1, 1265), (790126, Stalker, 1, 1265),
                (790243, Nitroglazer, 1, 1267), (790244, Nitroglazer, 1, 1267), (790245, Machina, 2, 1267), (790246, Machina, 2, 1267),
                (531038, ThraxSoldier, 1, 1269), (531039, ThraxSoldier, 1, 1269),
                (790085, Ravager, 2, 1269), (790086, Ravager, 2, 1269), (790125, Stalker, 1, 1268), (790126, Stalker, 1, 1268)
            })
            {
                var row = harness.World.SpawnPoolEntries.AsNoTracking().Single(entry => entry.Id == pool);
                Assert.AreEqual(creature, row.Creature1Id, $"pool {pool}");
                Assert.IsTrue(rows.Any(marker => marker.MissionId == mission && marker.ObjectiveId == objective &&
                    Vector2.Distance(new((float)marker.PosX, (float)marker.PosZ), new((float)row.PosX, (float)row.PosZ)) < 0.1f), $"pool {pool} under a {mission}.{objective} marker");
            }

            // The Desert Predators: every step of each beat (spawnpool_patrol) inside one 1248 marker, its own.
            var beats = harness.World.Set<SpawnPoolPatrolEntry>().AsNoTracking().Where(row => row.PoolId == 790089 || row.PoolId == 790090)
                .AsEnumerable().GroupBy(row => row.PoolId).ToDictionary(group => group.Key, group => group.ToList());
            Assert.HasCount(2, beats, "two beats");
            // And inside each Predator salvage marker of 1265 and 1268 (objective 2), its own.
            foreach (var (mission, objective) in new (uint, uint)[] { (1248, 1), (1265, 2), (1268, 2) })
            {
                var predatorMarkers = rows.Where(row => row.MissionId == mission && row.ObjectiveId == objective).OrderBy(row => row.IndicatorId).ToList();
                Assert.HasCount(2, predatorMarkers, $"{mission}: a marker on each beat");
                foreach (var ((pool, steps), marker) in beats.OrderBy(beat => beat.Key).Zip(predatorMarkers))
                {
                    Assert.AreEqual(Predator, harness.World.SpawnPoolEntries.AsNoTracking().Single(entry => entry.Id == pool).Creature1Id, $"pool {pool}");
                    foreach (var step in steps)
                        Assert.IsLessThanOrEqualTo(marker.Radius, Vector2.Distance(new((float)marker.PosX, (float)marker.PosZ), new((float)step.PosX, (float)step.PosZ)),
                            $"{mission}: pool {pool} step {step.Step} inside its marker");
                }
            }

            // The six fallen (1263's scene) inside its one marker.
            var field = rows.Single(row => row.MissionId == 1263);
            foreach (var actor in Scenes(harness).Single(scene => scene.MissionId == 1263).Scene.Actors.Values)
                Assert.IsLessThanOrEqualTo(field.Radius, Vector2.Distance(new((float)field.PosX, (float)field.PosZ), new(actor.Position.X, actor.Position.Z)),
                    $"{actor.Role} inside the 1263 marker");

            // Set 5: the four trench mortars inside 1352's 11 marker; each launcher's device and each generator under its
            // objective's marker, the generator's device inside it too.
            var trench = rows.Single(row => row.MissionId == 1352 && row.ObjectiveId == 11);
            foreach (var pool in TrenchPools)
            {
                var row = harness.World.SpawnPoolEntries.AsNoTracking().Single(entry => entry.Id == pool);
                Assert.IsLessThanOrEqualTo(trench.Radius - 5, Vector2.Distance(new((float)trench.PosX, (float)trench.PosZ), new((float)row.PosX, (float)row.PosZ)),
                    $"trench mortar pool {pool} inside 1352's 11 marker");
            }
            for (var number = 1; number <= 6; number++)
            {
                var device = AshenDeathFromAboveV1.DevicesAt[number - 1];
                var marker = rows.Single(row => row.MissionId == 1352 && row.ObjectiveId == 4 + number);
                Assert.IsLessThan(0.1f, Vector2.Distance(new((float)marker.PosX, (float)marker.PosZ), new(device.X, device.Z)), $"launcher {number}'s device under its marker");
            }
            for (var number = 1; number <= 3; number++)
            {
                var marker = rows.Single(row => row.MissionId == 1354 && row.ObjectiveId == number);
                foreach (var at in new[] { AshenTurningOutTheLightsV1.GeneratorsAt[number - 1], AshenTurningOutTheLightsV1.DevicesAt[number - 1] })
                    Assert.IsLessThanOrEqualTo(marker.Radius, Vector2.Distance(new((float)marker.PosX, (float)marker.PosZ), new(at.X, at.Z)), $"generator {number} and its device inside its marker");
            }
        }

        /// <summary>
        /// Set 4's creatures: the Desert Stalker 790121 has the client's name 9321 "Class XI Desert Stalker" and the class, level,
        /// hit points, armour and attacks of the World DB's Stalker 520073, hostile, in two pools of one (radius 0); the Xanx pools
        /// hold the World DB's Xanx - Ashen Desert 540020 (name 471 "Xanx Burrower", L39, unchanged), three each, radius 0. All on
        /// Ashen Desert, 120 s respawn, automatic. Infanger's pool is his own.
        /// </summary>
        [TestMethod]
        public void TheBoneyardCreaturesCopyTheirWorldRows()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var world = harness.World;
            var creatures = world.CreatureEntries.AsNoTracking().ToDictionary(row => row.Id);
            var stats = world.CreatureStatEntries.AsNoTracking().ToDictionary(row => row.Id);
            var stalker = creatures[Stalker];
            var source = creatures[520073];
            Assert.AreEqual((9321U, 3781U, 40U, 0U), (stalker.NameId, stalker.ClassId, stalker.Level, stalker.Faction), "the Desert Stalker");
            Assert.AreEqual((source.MaxHitPoints, source.Action1, source.Action2, source.Action3, source.Action4, source.RunSpeed, source.WalkSpeed),
                (stalker.MaxHitPoints, stalker.Action1, stalker.Action2, stalker.Action3, stalker.Action4, stalker.RunSpeed, stalker.WalkSpeed), "copies 520073");
            Assert.AreEqual((4500, 400), (stats[Stalker].Health, stats[Stalker].Armor));
            Assert.IsTrue(CreatureManager.Instance.LoadedCreatures.TryGetValue(Stalker, out var loaded) && loaded.TargetCategory == TargetCategory.Hostile, "hostile");
            Assert.AreEqual((471U, 7510U, 39U), (creatures[Xanx].NameId, creatures[Xanx].ClassId, creatures[Xanx].Level), "the Xanx row, as upstream");

            foreach (var (pools, creature, count) in new[] { (XanxPools, Xanx, (byte)3), (StalkerPools, Stalker, (byte)1) })
                foreach (var pool in pools)
                {
                    var row = world.SpawnPoolEntries.AsNoTracking().Single(entry => entry.Id == pool);
                    Assert.AreEqual((Map, creature, count, count, 0U, 0.0, 1200U, (byte)0), (row.MapContextId, row.Creature1Id, row.Creature1MinCount,
                        row.Creature1MaxCount, row.Creature2Id, row.Radius, row.RespawnTime, row.Mode), $"pool {pool}");
                }
            CollectionAssert.AreEquivalent(StalkerPools, world.SpawnPoolEntries.AsNoTracking().Where(pool => pool.Creature1Id == Stalker).Select(pool => pool.Id).ToArray(),
                "the Desert Stalker's two pools");
        }

        /// <summary>
        /// Set 3's creatures: the Ashen Thrax Grenadier 531009 has the client's name 9223 "Thrax Ravager" (the triage's D9); the
        /// Thrax Soldier Mark VI 531008 keeps 355. The Desert Predator 790080 has the client's name 9327 and the class, level,
        /// hit points, armour and attacks of 520022, hostile, in two pools of one (radius 0) on Ashen Desert, each with a beat
        /// walked there and back. The highway's mortar pools 592191-592194 each hold their own copy of the Ashen Bane Mortar
        /// 592011 (all of its row, its stats and look but the id, no name id); the other Ashen mortars keep 592011. The Thrax
        /// pools: two of Ravagers and two of Soldiers, three each, radius 0.
        /// </summary>
        [TestMethod]
        public void TheHighwayCreaturesCopyTheirWorldRows()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var world = harness.World;
            var creatures = world.CreatureEntries.AsNoTracking().ToDictionary(row => row.Id);
            var stats = world.CreatureStatEntries.AsNoTracking().ToDictionary(row => row.Id);
            Assert.AreEqual((9223U, 9354U, 39U), (creatures[Ravager].NameId, creatures[Ravager].ClassId, creatures[Ravager].Level), "the Ravager");
            Assert.AreEqual((355U, 20757U), (creatures[ThraxSoldier].NameId, creatures[ThraxSoldier].ClassId), "the Thrax Soldier");

            var predator = creatures[Predator];
            var source = creatures[520022];
            Assert.AreEqual((9327U, 3902U, 42U, 0U), (predator.NameId, predator.ClassId, predator.Level, predator.Faction), "the Desert Predator");
            Assert.AreEqual((source.MaxHitPoints, source.Action1, source.Action2, source.Action3, source.Action4, source.RunSpeed, source.WalkSpeed),
                (predator.MaxHitPoints, predator.Action1, predator.Action2, predator.Action3, predator.Action4, predator.RunSpeed, predator.WalkSpeed), "copies 520022");
            Assert.AreEqual((4700, 410), (stats[Predator].Health, stats[Predator].Armor));
            Assert.IsTrue(CreatureManager.Instance.LoadedCreatures.TryGetValue(Predator, out var loaded) && loaded.TargetCategory == TargetCategory.Hostile, "hostile");
            var predatorPools = world.SpawnPoolEntries.AsNoTracking().Where(pool => pool.Creature1Id == Predator).OrderBy(pool => pool.Id).ToList();
            CollectionAssert.AreEqual(new uint[] { 790089, 790090 }, predatorPools.Select(pool => pool.Id).ToArray());
            var patrols = world.Set<SpawnPoolPatrolEntry>().AsNoTracking().ToList();
            foreach (var pool in predatorPools)
            {
                Assert.AreEqual((Map, (byte)1, (byte)1, 0.0, 1200U, (byte)0), (pool.MapContextId, pool.Creature1MinCount, pool.Creature1MaxCount,
                    pool.Radius, pool.RespawnTime, pool.Mode), $"pool {pool.Id}");
                var steps = patrols.Where(row => row.PoolId == pool.Id).OrderBy(row => row.Step).ToList();
                var (_, home, way) = AshenSheitanHighwayNpcsV1.PredatorPools.Single(entry => entry.Pool == pool.Id);
                Assert.HasCount(2 * way.Length - 2, steps, $"pool {pool.Id}: every step both ways");
                Assert.IsLessThan(0.01, System.Math.Abs(way[home].X - pool.PosX) + System.Math.Abs(way[home].Z - pool.PosZ), $"pool {pool.Id}: its point is the beat's home step");
                Assert.IsTrue(home > 0 && home < way.Length - 1, $"pool {pool.Id}: home inside the beat, not at an end");
                Assert.IsLessThan(0.01, System.Math.Abs(steps[0].PosX - pool.PosX) + System.Math.Abs(steps[0].PosZ - pool.PosZ), $"pool {pool.Id} starts its beat at home");
                // Each end once, each step between twice (out and back); each step leads to a neighbour on the way.
                int Index(SpawnPoolPatrolEntry step) => System.Array.FindIndex(way, at => System.Math.Abs(at.X - step.PosX) + System.Math.Abs(at.Z - step.PosZ) < 0.01);
                var order = steps.Select(Index).ToList();
                Assert.IsFalse(order.Contains(-1), $"pool {pool.Id}: every step on the way");
                for (var i = 0; i < way.Length; i++)
                    Assert.AreEqual(i == 0 || i == way.Length - 1 ? 1 : 2, order.Count(index => index == i), $"pool {pool.Id} way step {i}");
                for (var i = 0; i < order.Count; i++)
                    Assert.AreEqual(1, System.Math.Abs(order[i] - order[(i + 1) % order.Count]), $"pool {pool.Id} step {i} to a neighbour");
                // The leash: every step within MaxChaseDistance less 15 m of home (BehaviorManager.ChasedTooFar).
                foreach (var step in steps)
                    Assert.IsLessThanOrEqualTo(BehaviorManager.MaxChaseDistance - 15f,
                        Vector2.Distance(new((float)pool.PosX, (float)pool.PosZ), new((float)step.PosX, (float)step.PosZ)), $"pool {pool.Id} step {step.Step} near home");
            }

            var mortar = creatures[Mortar];
            var mortarStat = stats[Mortar];
            var look = world.CreatureAppearanceEntries.AsNoTracking().Where(row => row.Id == Mortar).ToList()
                .Select(row => (row.SlotId, row.ClassId, row.Color)).ToList();
            Assert.HasCount(1, look, "the launcher in slot 13");
            for (var index = 0; index < MortarCopies.Length; index++)
            {
                var copy = creatures[MortarCopies[index]];
                Assert.AreEqual((mortar.ClassId, mortar.Faction, mortar.Level, mortar.MaxHitPoints, mortar.NameId, mortar.Action1, mortar.RunSpeed, mortar.WalkSpeed),
                    (copy.ClassId, copy.Faction, copy.Level, copy.MaxHitPoints, copy.NameId, copy.Action1, copy.RunSpeed, copy.WalkSpeed), $"{copy.Id} copies {Mortar}");
                Assert.AreEqual((7482U, 0U, 42U), (copy.ClassId, copy.NameId, copy.Level));
                Assert.AreEqual((mortarStat.Health, mortarStat.Armor), (stats[copy.Id].Health, stats[copy.Id].Armor), $"{copy.Id} stats");
                CollectionAssert.AreEqual(look, world.CreatureAppearanceEntries.AsNoTracking().Where(row => row.Id == copy.Id).ToList()
                    .Select(row => (row.SlotId, row.ClassId, row.Color)).ToList(), $"{copy.Id} look");
                var pool = world.SpawnPoolEntries.AsNoTracking().Single(row => row.Id == MortarPools[index]);
                Assert.AreEqual((Map, copy.Id, (byte)1, (byte)1), (pool.MapContextId, pool.Creature1Id, pool.Creature1MinCount, pool.Creature1MaxCount), $"mortar pool {pool.Id}");
            }
            Assert.IsGreaterThan(5, world.SpawnPoolEntries.AsNoTracking().Count(pool => pool.MapContextId == Map && pool.Creature1Id == Mortar), "the other Ashen mortars keep 592011");

            foreach (var (pool, creature) in new (uint, uint)[] { (790085, Ravager), (790086, Ravager), (790087, ThraxSoldier), (790088, ThraxSoldier) })
            {
                var row = world.SpawnPoolEntries.AsNoTracking().Single(entry => entry.Id == pool);
                var count = creature == Ravager ? (byte)AshenFixEV1.Ravagers : (byte)3; // fix E: the Ravager pools of two
                Assert.AreEqual((Map, creature, count, count, 0U, 0.0, 1200U), (row.MapContextId, row.Creature1Id, row.Creature1MinCount,
                    row.Creature1MaxCount, row.Creature2Id, row.Radius, row.RespawnTime), $"pool {pool}");
            }
        }

        /// <summary>
        /// Ashen_lead_ins takes out its five missions and four NPCs and puts the moved upstream rows back where upstream had them
        /// (MissionNpcSpawnpoolPreloader with StandingHeights' corrections, not Down's own constants); the Crucible missions
        /// stay. Up again, all of it is back. The counts take only set 1's own ids and missions, so later sets do not change them.
        /// </summary>
        [TestMethod]
        public void LeadInsRollBackWithTheirMigration()
        {
            // Set 1's own missions and markers (one at each talk's receiver, one per Fithik pool), from the data classes.
            var leadInMissions = new[]
            {
                AshenDustiestPlaceV1.MissionId, AshenReportToStonewallV1.MissionId, AshenFithikInfestationV1.MissionId,
                AshenGoodDayToDieV1.MissionId, AshenCantDrinkAshV1.MissionId
            };
            var leadInIndicators = leadInMissions.Length - 1 + AshenFithikInfestationV1.Pools.Length;
            using var harness = WildernessRuntimeTestHarness.Create();
            var world = harness.World;
            bool Has(uint mission) => world.MissionContentDefinitionEntries.AsNoTracking()
                .Any(entry => entry.MissionId == mission && entry.ContentRevision == AshenMissionDataV1.Revision);
            int Count(string sql) => world.Database.SqlQueryRaw<int>(sql).AsEnumerable().Single();
            var ids = string.Join(", ", AshenLeadInNpcsV1.New.Select(npc => npc.Id));
            var missions = string.Join(", ", leadInMissions);
            (int, int, int, int, int, int) All() => (
                Count($"SELECT COUNT(*) AS Value FROM creature WHERE id IN ({ids})"),
                Count($"SELECT COUNT(*) AS Value FROM spawnpool WHERE id IN ({ids})"),
                Count($"SELECT COUNT(*) AS Value FROM npc_package WHERE id IN ({ids})"),
                Count($"SELECT COUNT(*) AS Value FROM creature_stat WHERE id IN ({ids})"),
                Count($"SELECT COUNT(*) AS Value FROM creature_appearance WHERE id IN ({ids})"),
                Count($"SELECT COUNT(*) AS Value FROM mission_indicator WHERE content_revision = 'ashen_1_16_5' AND mission_id IN ({missions})"));
            (double X, double Y, double Z) At(uint pool)
            {
                var row = world.SpawnPoolEntries.AsNoTracking().Single(entry => entry.Id == pool);
                return (row.PosX, row.PosY, row.PosZ);
            }
            var npcs = AshenLeadInNpcsV1.New.Length;
            // The copied rows: one appearance row per slot of the NPC each copies.
            var appearances = AshenLeadInNpcsV1.New.Sum(npc =>
                world.Database.SqlQueryRaw<int>($"SELECT COUNT(*) AS Value FROM creature_appearance WHERE id = {npc.Copy}").AsEnumerable().Single());
            Assert.IsGreaterThan(0, appearances);
            void AllUp()
            {
                Assert.IsTrue(leadInMissions.All(Has));
                Assert.AreEqual((npcs, npcs, npcs, npcs, appearances, leadInIndicators), All());
                foreach (var (pool, _, now) in AshenLeadInNpcsV1.Moves)
                    Assert.AreEqual(now, At(pool), $"pool {pool} at its set 1 place");
            }

            AllUp();
            world.GetService<IMigrator>().Migrate(Previous(world, LeadIns));
            world.ChangeTracker.Clear();
            Assert.IsFalse(leadInMissions.Any(Has));
            Assert.AreEqual((0, 0, 0, 0, 0, 0), All());
            Assert.IsFalse(world.MissionEvidenceEntries.Any(entry => entry.ContentRevision == AshenMissionDataV1.Revision && leadInMissions.Contains(entry.MissionId)));
            Assert.IsFalse(world.MissionRewardItemEntries.Any(entry => entry.ContentRevision == AshenMissionDataV1.Revision && leadInMissions.Contains(entry.MissionId)));
            Assert.IsFalse(world.MissionObjectiveDefinitionEntries.Any(entry => entry.ContentRevision == AshenMissionDataV1.Revision && leadInMissions.Contains(entry.MissionId)));
            foreach (var (pool, _, _) in AshenLeadInNpcsV1.Moves)
            {
                var upstream = Upstream(pool);
                var at = At(pool);
                Assert.IsLessThan(0.01, System.Math.Abs(at.X - upstream.X) + System.Math.Abs(at.Y - upstream.Y) + System.Math.Abs(at.Z - upstream.Z),
                    $"pool {pool} back at its upstream place {upstream}, is {at}");
            }
            Assert.IsTrue(world.MissionContentDefinitionEntries.Any(entry => entry.MissionId == CrucibleLastMenStandingV1.MissionId &&
                entry.ContentRevision == CrucibleMissionDataV1.Revision), "the Crucible missions stay");

            world.GetService<IMigrator>().Migrate(LeadIns);
            world.ChangeTracker.Clear();
            AllUp();
        }

        /// <summary>
        /// Ashen_mauna_barrens takes out its four missions, three NPCs, three Atta and six Atta pools, and puts Exing's pool back
        /// where upstream had it; set 1 stays. Up again, all of it is back. The counts take only set 2's own ids and missions.
        /// </summary>
        [TestMethod]
        public void MaunaBarrensRollsBackWithItsMigration()
        {
            var missions = new[]
            {
                AshenAngryAngryAttaV1.MissionId, AshenHarmfulMutationsV1.MissionId, AshenChemicalOversightV1.MissionId, AshenEyesOnTheGroundV1.MissionId
            };
            using var harness = WildernessRuntimeTestHarness.Create();
            var world = harness.World;
            bool Has(uint mission) => world.MissionContentDefinitionEntries.AsNoTracking()
                .Any(entry => entry.MissionId == mission && entry.ContentRevision == AshenMissionDataV1.Revision);
            int Count(string sql) => world.Database.SqlQueryRaw<int>(sql).AsEnumerable().Single();
            var npcs = string.Join(", ", AshenMaunaBarrensNpcsV1.New.Select(npc => npc.Id));
            var creatures = string.Join(", ", AshenMaunaBarrensNpcsV1.New.Select(npc => npc.Id).Concat(AshenMaunaBarrensNpcsV1.Atta.Select(atta => atta.Id)));
            var pools = string.Join(", ", AshenMaunaBarrensNpcsV1.New.Select(npc => npc.Id).Concat(AshenMaunaBarrensNpcsV1.AttaPools.Select(pool => pool.Pool)));
            var ids = string.Join(", ", missions);
            (int, int, int, int, int, int, int) All() => (
                Count($"SELECT COUNT(*) AS Value FROM creature WHERE id IN ({creatures})"),
                Count($"SELECT COUNT(*) AS Value FROM creature_stat WHERE id IN ({creatures})"),
                Count($"SELECT COUNT(*) AS Value FROM spawnpool WHERE id IN ({pools})"),
                Count($"SELECT COUNT(*) AS Value FROM npc_package WHERE id IN ({npcs})"),
                Count($"SELECT COUNT(*) AS Value FROM creature_appearance WHERE id IN ({creatures})"),
                Count($"SELECT COUNT(*) AS Value FROM mission_indicator WHERE content_revision = 'ashen_1_16_5' AND mission_id IN ({ids})"),
                Count($"SELECT COUNT(*) AS Value FROM mission_scene_binding WHERE content_revision = 'ashen_1_16_5' AND mission_id IN ({ids})"));
            (double X, double Y, double Z) ExingAt()
            {
                var row = world.SpawnPoolEntries.AsNoTracking().Single(entry => entry.Id == Exing);
                return (row.PosX, row.PosY, row.PosZ);
            }
            var appearances = AshenMaunaBarrensNpcsV1.New.Sum(npc =>
                world.Database.SqlQueryRaw<int>($"SELECT COUNT(*) AS Value FROM creature_appearance WHERE id = {npc.Copy}").AsEnumerable().Single());
            Assert.IsGreaterThan(0, appearances);
            void AllUp()
            {
                Assert.IsTrue(missions.All(Has));
                Assert.AreEqual((6, 6, 9, 3, appearances, 12, 2), All());
            }

            AllUp();
            Assert.AreEqual(AshenFixBV1.PoolNow, ExingAt(), "Exing at fix B's place (the last migration)");
            world.GetService<IMigrator>().Migrate(Previous(world, MaunaBarrens));
            world.ChangeTracker.Clear();
            Assert.IsFalse(missions.Any(Has));
            Assert.AreEqual((0, 0, 0, 0, 0, 0, 0), All());
            Assert.IsFalse(world.MissionEvidenceEntries.Any(entry => entry.ContentRevision == AshenMissionDataV1.Revision && missions.Contains(entry.MissionId)));
            Assert.IsFalse(world.MissionRewardItemEntries.Any(entry => entry.ContentRevision == AshenMissionDataV1.Revision && missions.Contains(entry.MissionId)));
            Assert.IsFalse(world.MissionObjectiveDefinitionEntries.Any(entry => entry.ContentRevision == AshenMissionDataV1.Revision && missions.Contains(entry.MissionId)));
            Assert.IsFalse(world.MissionPrerequisiteEntries.Any(entry => entry.ContentRevision == AshenMissionDataV1.Revision && missions.Contains(entry.MissionId)));
            var back = ExingAt();
            Assert.IsLessThan(0.01, System.Math.Abs(back.X + 281) + System.Math.Abs(back.Y - 340.4) + System.Math.Abs(back.Z + 252),
                $"Exing back at upstream's (-281, 340.4, -252), is {back}");
            Assert.IsTrue(MissionIds.Take(5).All(Has), "set 1 stays");

            world.GetService<IMigrator>().Migrate(MaunaBarrens);
            world.ChangeTracker.Clear();
            AllUp();
            Assert.AreEqual(AshenMaunaBarrensNpcsV1.ExingAt, ExingAt(), "Exing at his set 2 place (fix B not run again)");
        }

        /// <summary>
        /// Ashen_sheitan_highway takes out its three missions, the Desert Predator and the four mortar copies, the six pools and
        /// the beats, gives the mortar pools 592011 back and takes the Ravager name off 531009; sets 1 and 2 stay. Up again, all
        /// of it is back. The counts take only set 3's own ids and missions.
        /// </summary>
        [TestMethod]
        public void SheitanHighwayRollsBackWithItsMigration()
        {
            var missions = new[] { AshenHighwayToHellV1.MissionId, AshenAirSuperiorityV1.MissionId, AshenArtilleryAnnihilationV1.MissionId };
            using var harness = WildernessRuntimeTestHarness.Create();
            var world = harness.World;
            bool Has(uint mission) => world.MissionContentDefinitionEntries.AsNoTracking()
                .Any(entry => entry.MissionId == mission && entry.ContentRevision == AshenMissionDataV1.Revision);
            int Count(string sql) => world.Database.SqlQueryRaw<int>(sql).AsEnumerable().Single();
            var creatures = string.Join(", ", MortarCopies.Append(Predator));
            var pools = string.Join(", ", AshenSheitanHighwayNpcsV1.ThraxPools.Select(pool => pool.Pool).Concat(AshenSheitanHighwayNpcsV1.PredatorPools.Select(pool => pool.Pool)));
            var ids = string.Join(", ", missions);
            var mortarPools = string.Join(", ", MortarPools);
            (int, int, int, int, int, int, int, int) All() => (
                Count($"SELECT COUNT(*) AS Value FROM creature WHERE id IN ({creatures})"),
                Count($"SELECT COUNT(*) AS Value FROM creature_stat WHERE id IN ({creatures})"),
                Count($"SELECT COUNT(*) AS Value FROM creature_appearance WHERE id IN ({creatures})"),
                Count($"SELECT COUNT(*) AS Value FROM spawnpool WHERE id IN ({pools})"),
                Count($"SELECT COUNT(*) AS Value FROM spawnpool_patrol WHERE pool_id IN ({pools})"),
                Count($"SELECT COUNT(*) AS Value FROM mission_indicator WHERE content_revision = 'ashen_1_16_5' AND mission_id IN ({ids})"),
                Count($"SELECT COUNT(*) AS Value FROM spawnpool WHERE id IN ({mortarPools}) AND creature_1_Id = {Mortar}"),
                Count($"SELECT COUNT(*) AS Value FROM creature WHERE id = {Ravager} AND name_id = 9223"));
            var beatSteps = AshenSheitanHighwayNpcsV1.PredatorPools.Sum(pool => 2 * pool.Way.Length - 2);
            void AllUp()
            {
                Assert.IsTrue(missions.All(Has));
                Assert.AreEqual((5, 5, 4, 6, beatSteps, 10, 0, 1), All());
            }

            AllUp();
            world.GetService<IMigrator>().Migrate(Previous(world, SheitanHighway));
            world.ChangeTracker.Clear();
            Assert.IsFalse(missions.Any(Has));
            Assert.AreEqual((0, 0, 0, 0, 0, 0, 4, 0), All());
            Assert.AreEqual(0U, world.CreatureEntries.AsNoTracking().Single(row => row.Id == Ravager).NameId, "531009 nameless again, as upstream");
            Assert.IsFalse(world.MissionEvidenceEntries.Any(entry => entry.ContentRevision == AshenMissionDataV1.Revision && missions.Contains(entry.MissionId)));
            Assert.IsFalse(world.MissionRewardItemEntries.Any(entry => entry.ContentRevision == AshenMissionDataV1.Revision && missions.Contains(entry.MissionId)));
            Assert.IsFalse(world.MissionObjectiveDefinitionEntries.Any(entry => entry.ContentRevision == AshenMissionDataV1.Revision && missions.Contains(entry.MissionId)));
            Assert.IsTrue(MissionIds.Take(9).All(Has), "sets 1 and 2 stay");

            world.GetService<IMigrator>().Migrate(SheitanHighway);
            world.ChangeTracker.Clear();
            AllUp();
        }

        /// <summary>
        /// Ashen_boneyard takes out its three missions, Infanger, the Desert Stalker and the five pools; Blood and the Xanx row stay
        /// as upstream has them; sets 1-3 stay. Up again, all of it is back. The counts take only set 4's own ids and missions.
        /// </summary>
        [TestMethod]
        public void BoneyardRollsBackWithItsMigration()
        {
            var missions = new[] { AshenXanxlessJobV1.MissionId, AshenFallenSoldiersV1.MissionId, AshenStalkingTheStalkersV1.MissionId };
            using var harness = WildernessRuntimeTestHarness.Create();
            var world = harness.World;
            bool Has(uint mission) => world.MissionContentDefinitionEntries.AsNoTracking()
                .Any(entry => entry.MissionId == mission && entry.ContentRevision == AshenMissionDataV1.Revision);
            int Count(string sql) => world.Database.SqlQueryRaw<int>(sql).AsEnumerable().Single();
            var creatures = $"{Infanger}, {Stalker}";
            var pools = string.Join(", ", XanxPools.Concat(StalkerPools).Append(Infanger));
            var ids = string.Join(", ", missions);
            (int, int, int, int, int, int, int) All() => (
                Count($"SELECT COUNT(*) AS Value FROM creature WHERE id IN ({creatures})"),
                Count($"SELECT COUNT(*) AS Value FROM creature_stat WHERE id IN ({creatures})"),
                Count($"SELECT COUNT(*) AS Value FROM npc_package WHERE id IN ({creatures})"),
                Count($"SELECT COUNT(*) AS Value FROM spawnpool WHERE id IN ({pools})"),
                Count($"SELECT COUNT(*) AS Value FROM mission_indicator WHERE content_revision = 'ashen_1_16_5' AND mission_id IN ({ids})"),
                Count($"SELECT COUNT(*) AS Value FROM mission_scene_binding WHERE content_revision = 'ashen_1_16_5' AND mission_id IN ({ids})"),
                Count($"SELECT COUNT(*) AS Value FROM creature WHERE id IN ({Blood}, {Xanx})"));
            var appearances = Count($"SELECT COUNT(*) AS Value FROM creature_appearance WHERE id = {MasonLinier}");
            Assert.IsGreaterThan(0, appearances);
            void AllUp()
            {
                Assert.IsTrue(missions.All(Has));
                Assert.AreEqual((2, 2, 1, 6, 6, 1, 2), All());
                Assert.AreEqual(appearances, Count($"SELECT COUNT(*) AS Value FROM creature_appearance WHERE id = {Infanger}"), "Infanger's look");
            }

            AllUp();
            world.GetService<IMigrator>().Migrate(Previous(world, Boneyard));
            world.ChangeTracker.Clear();
            Assert.IsFalse(missions.Any(Has));
            Assert.AreEqual((0, 0, 0, 0, 0, 0, 2), All());
            Assert.AreEqual(0, Count($"SELECT COUNT(*) AS Value FROM creature_appearance WHERE id = {Infanger}"));
            Assert.IsFalse(world.MissionEvidenceEntries.Any(entry => entry.ContentRevision == AshenMissionDataV1.Revision && missions.Contains(entry.MissionId)));
            Assert.IsFalse(world.MissionRewardItemEntries.Any(entry => entry.ContentRevision == AshenMissionDataV1.Revision && missions.Contains(entry.MissionId)));
            Assert.IsFalse(world.MissionObjectiveDefinitionEntries.Any(entry => entry.ContentRevision == AshenMissionDataV1.Revision && missions.Contains(entry.MissionId)));
            Assert.IsTrue(MissionIds.Take(12).All(Has), "sets 1-3 stay");

            world.GetService<IMigrator>().Migrate(Boneyard);
            world.ChangeTracker.Clear();
            AllUp();
        }

        /// <summary>
        /// Set 5's creatures: the trench mortar pools 592199, 592202-592204 each hold their own copy of the Ashen Bane Mortar
        /// 592011 (all of its row, its stats and look but the id, no name id); the Linker pools hold the World DB's Linker - Ashen
        /// Desert 531014 (name 414 "Linker Master", L40, unchanged), three each, radius 0, 120 s respawn, automatic, linked to no
        /// control point and 130 m or more from Control Point 16; Overseer Naxveik's row is upstream's (name 9353, L44), his pool
        /// moved, one of him, automatic.
        /// </summary>
        [TestMethod]
        public void TheConscriptionBaseCreaturesCopyTheirWorldRows()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var world = harness.World;
            var creatures = world.CreatureEntries.AsNoTracking().ToDictionary(row => row.Id);
            var stats = world.CreatureStatEntries.AsNoTracking().ToDictionary(row => row.Id);
            var mortar = creatures[Mortar];
            var look = world.CreatureAppearanceEntries.AsNoTracking().Where(row => row.Id == Mortar).ToList()
                .Select(row => (row.SlotId, row.ClassId, row.Color)).ToList();
            for (var index = 0; index < TrenchCopies.Length; index++)
            {
                var copy = creatures[TrenchCopies[index]];
                Assert.AreEqual((mortar.ClassId, mortar.Faction, mortar.Level, mortar.MaxHitPoints, mortar.NameId, mortar.Action1, mortar.RunSpeed, mortar.WalkSpeed),
                    (copy.ClassId, copy.Faction, copy.Level, copy.MaxHitPoints, copy.NameId, copy.Action1, copy.RunSpeed, copy.WalkSpeed), $"{copy.Id} copies {Mortar}");
                Assert.AreEqual((7482U, 0U, 42U), (copy.ClassId, copy.NameId, copy.Level));
                Assert.AreEqual((stats[Mortar].Health, stats[Mortar].Armor), (stats[copy.Id].Health, stats[copy.Id].Armor), $"{copy.Id} stats");
                CollectionAssert.AreEqual(look, world.CreatureAppearanceEntries.AsNoTracking().Where(row => row.Id == copy.Id).ToList()
                    .Select(row => (row.SlotId, row.ClassId, row.Color)).ToList(), $"{copy.Id} look");
                var pool = world.SpawnPoolEntries.AsNoTracking().Single(row => row.Id == TrenchPools[index]);
                Assert.AreEqual((Map, copy.Id, (byte)1, (byte)1, (byte)0), (pool.MapContextId, pool.Creature1Id, pool.Creature1MinCount, pool.Creature1MaxCount, pool.Mode),
                    $"trench mortar pool {pool.Id}");
            }

            Assert.AreEqual((414U, 9751U, 40U, 0U), (creatures[Linker].NameId, creatures[Linker].ClassId, creatures[Linker].Level, creatures[Linker].Faction),
                "the Linker row, as upstream");
            var links = world.ControlPointLinkEntries.AsNoTracking().Select(row => row.ObjectId).ToList();
            var garrison = world.ControlPointEntries.AsNoTracking().Single(row => row.Id == 16);
            foreach (var pool in LinkerPools)
            {
                var row = world.SpawnPoolEntries.AsNoTracking().Single(entry => entry.Id == pool);
                Assert.AreEqual((Map, Linker, (byte)3, (byte)3, 0U, 0.0, 1200U, (byte)0), (row.MapContextId, row.Creature1Id, row.Creature1MinCount,
                    row.Creature1MaxCount, row.Creature2Id, row.Radius, row.RespawnTime, row.Mode), $"pool {pool}");
                Assert.IsGreaterThanOrEqualTo(130.0, System.Math.Sqrt(System.Math.Pow(row.PosX - garrison.PosX, 2) + System.Math.Pow(row.PosZ - garrison.PosZ, 2)),
                    $"pool {pool} 130 m or more from the Conscription Garrison");
                Assert.IsFalse(links.Contains(pool), $"pool {pool} linked to no control point");
            }

            Assert.AreEqual((9353U, 44U, 0U), (creatures[Naxveik].NameId, creatures[Naxveik].Level, creatures[Naxveik].Faction), "Overseer Naxveik's row, as upstream");
            var naxveik = world.SpawnPoolEntries.AsNoTracking().Single(row => row.Id == Naxveik);
            Assert.AreEqual((Map, Naxveik, (byte)1, (byte)1, (byte)0), (naxveik.MapContextId, naxveik.Creature1Id, naxveik.Creature1MinCount, naxveik.Creature1MaxCount, naxveik.Mode));
            Assert.IsGreaterThanOrEqualTo(130.0, System.Math.Sqrt(System.Math.Pow(naxveik.PosX - garrison.PosX, 2) + System.Math.Pow(naxveik.PosZ - garrison.PosZ, 2)),
                "Naxveik 130 m or more from the Conscription Garrison");
            Assert.IsFalse(links.Contains(Naxveik) || TrenchPools.Any(links.Contains), "Naxveik and the trench mortars linked to no control point");
        }

        /// <summary>
        /// Ashen_conscription_base takes out its three missions, the four trench mortar copies and the five Linker pools, gives
        /// the trench pools 592011 back and puts Naxveik back where upstream had him (its float values); the Linker and Naxveik
        /// rows stay; sets 1-4 stay. Up again, all of it is back. The counts take only set 5's own ids and missions.
        /// </summary>
        [TestMethod]
        public void ConscriptionBaseRollsBackWithItsMigration()
        {
            var missions = new[] { AshenDeathFromAboveV1.MissionId, AshenTurningOutTheLightsV1.MissionId, AshenStrongestLinkV1.MissionId };
            using var harness = WildernessRuntimeTestHarness.Create();
            var world = harness.World;
            bool Has(uint mission) => world.MissionContentDefinitionEntries.AsNoTracking()
                .Any(entry => entry.MissionId == mission && entry.ContentRevision == AshenMissionDataV1.Revision);
            int Count(string sql) => world.Database.SqlQueryRaw<int>(sql).AsEnumerable().Single();
            var creatures = string.Join(", ", TrenchCopies);
            var pools = string.Join(", ", LinkerPools);
            var ids = string.Join(", ", missions);
            var trench = string.Join(", ", TrenchPools);
            (int, int, int, int, int, int, int, int) All() => (
                Count($"SELECT COUNT(*) AS Value FROM creature WHERE id IN ({creatures})"),
                Count($"SELECT COUNT(*) AS Value FROM creature_stat WHERE id IN ({creatures})"),
                Count($"SELECT COUNT(*) AS Value FROM creature_appearance WHERE id IN ({creatures})"),
                Count($"SELECT COUNT(*) AS Value FROM spawnpool WHERE id IN ({pools})"),
                Count($"SELECT COUNT(*) AS Value FROM mission_indicator WHERE content_revision = 'ashen_1_16_5' AND mission_id IN ({ids})"),
                Count($"SELECT COUNT(*) AS Value FROM mission_scene_binding WHERE content_revision = 'ashen_1_16_5' AND mission_id IN ({ids})"),
                Count($"SELECT COUNT(*) AS Value FROM spawnpool WHERE id IN ({trench}) AND creature_1_Id = {Mortar}"),
                Count($"SELECT COUNT(*) AS Value FROM creature WHERE id IN ({Linker}, {Naxveik})"));
            (double X, double Y, double Z) NaxveikAt()
            {
                var row = world.SpawnPoolEntries.AsNoTracking().Single(entry => entry.Id == Naxveik);
                return (row.PosX, row.PosY, row.PosZ);
            }
            void AllUp()
            {
                Assert.IsTrue(missions.All(Has));
                Assert.AreEqual((4, 4, 4, 5, 20, 2, 0, 2), All());
                Assert.AreEqual(AshenConscriptionBaseNpcsV1.NaxveikAt, NaxveikAt(), "Naxveik at his set 5 place");
            }

            AllUp();
            world.GetService<IMigrator>().Migrate(Previous(world, ConscriptionBase));
            world.ChangeTracker.Clear();
            Assert.IsFalse(missions.Any(Has));
            Assert.AreEqual((0, 0, 0, 0, 0, 0, 4, 2), All());
            Assert.AreEqual((305.4142150878906, 282.60791015625, 84.41419982910156), NaxveikAt(), "Naxveik back at upstream's place, to the float");
            Assert.IsFalse(world.MissionEvidenceEntries.Any(entry => entry.ContentRevision == AshenMissionDataV1.Revision && missions.Contains(entry.MissionId)));
            Assert.IsFalse(world.MissionRewardItemEntries.Any(entry => entry.ContentRevision == AshenMissionDataV1.Revision && missions.Contains(entry.MissionId)));
            Assert.IsFalse(world.MissionObjectiveDefinitionEntries.Any(entry => entry.ContentRevision == AshenMissionDataV1.Revision && missions.Contains(entry.MissionId)));
            Assert.IsFalse(world.MissionPrerequisiteEntries.Any(entry => entry.ContentRevision == AshenMissionDataV1.Revision && missions.Contains(entry.MissionId)));
            Assert.IsTrue(MissionIds.Take(15).All(Has), "sets 1-4 stay");

            world.GetService<IMigrator>().Migrate(ConscriptionBase);
            world.ChangeTracker.Clear();
            AllUp();
        }

        /// <summary>
        /// Ashen_formal_complaints takes out its five missions and Latan, Ito and Megumi (creature, look, stats, package, pool)
        /// and puts the Boneyard's three Thrax pools back to upstream's rows (point and radius, to the float); the upstream rows
        /// it uses (Mason Linier, Raji Lasat, Ida Hayner) and sets 1-5 stay, and no other Ashen pool changes. Up again, all of
        /// it is back. The counts take only set 6's own ids and missions.
        /// </summary>
        [TestMethod]
        public void FormalComplaintsRollsBackWithItsMigration()
        {
            var missions = new[]
            {
                AshenFormalComplaintsV1.MissionId, AshenHumanPerspectiveV1.MissionId, AshenBribeForYourThoughtsV1.MissionId,
                AshenCourtMartialArtsV1.MissionId, AshenDearlyDepartedV1.MissionId
            };
            using var harness = WildernessRuntimeTestHarness.Create();
            var world = harness.World;
            bool Has(uint mission) => world.MissionContentDefinitionEntries.AsNoTracking()
                .Any(entry => entry.MissionId == mission && entry.ContentRevision == AshenMissionDataV1.Revision);
            int Count(string sql) => world.Database.SqlQueryRaw<int>(sql).AsEnumerable().Single();
            var npcs = string.Join(", ", new[] { Latan, Ito, Megumi });
            var ids = string.Join(", ", missions);
            (int, int, int, int, int, int, int, int) All() => (
                Count($"SELECT COUNT(*) AS Value FROM creature WHERE id IN ({npcs})"),
                Count($"SELECT COUNT(*) AS Value FROM creature_stat WHERE id IN ({npcs})"),
                Count($"SELECT COUNT(*) AS Value FROM npc_package WHERE id IN ({npcs})"),
                Count($"SELECT COUNT(*) AS Value FROM spawnpool WHERE id IN ({npcs})"),
                Count($"SELECT COUNT(*) AS Value FROM mission_indicator WHERE content_revision = 'ashen_1_16_5' AND mission_id IN ({ids})"),
                Count($"SELECT COUNT(*) AS Value FROM mission_scene_binding WHERE content_revision = 'ashen_1_16_5' AND mission_id IN ({ids})"),
                Count($"SELECT COUNT(*) AS Value FROM creature WHERE id IN ({MasonLinier}, 510014, 510011)"),
                Count("SELECT COUNT(*) AS Value FROM spawnpool WHERE id IN (531038, 531039, 531041) AND creature_1_Id = 531008"));
            ((double, double, double) At, double Radius)[] Thrax() => AshenDearlyDepartedV1.Pools
                .Select(pool => world.SpawnPoolEntries.AsNoTracking().Single(entry => entry.Id == pool.Pool))
                .Select(row => ((row.PosX, row.PosY, row.PosZ), row.Radius)).ToArray();
            var thraxIds = AshenDearlyDepartedV1.Pools.Select(pool => pool.Pool).ToArray();
            var later = PoolsOfLaterMigrations(world, FormalComplaints);
            string Others() => string.Join("; ", world.SpawnPoolEntries.AsNoTracking()
                .Where(entry => entry.MapContextId == AshenMissionDataV1.MapContextId && !thraxIds.Contains(entry.Id) && entry.Id != Latan && entry.Id != Ito && entry.Id != Megumi
                    && !later.Contains(entry.Id)) // the later migrations' pools (fix B's Exing, set 7's robots) go down and up too
                .OrderBy(entry => entry.Id).Select(entry => new { entry.Id, entry.PosX, entry.PosY, entry.PosZ, entry.Radius }).AsEnumerable().Select(entry => entry.ToString()));
            var others = Others();
            void AllUp()
            {
                Assert.IsTrue(missions.All(Has));
                Assert.AreEqual((3, 3, 3, 3, 9, 4, 3, 3), All());
                CollectionAssert.AreEqual(AshenDearlyDepartedV1.Pools.Select(pool => (pool.At, 0.0)).ToArray(), Thrax()
                    .Select(pool => ((System.Math.Round(pool.At.Item1, 2), System.Math.Round(pool.At.Item2, 2), System.Math.Round(pool.At.Item3, 2)), pool.Radius)).ToArray(), "the Thrax pools on their points");
                Assert.IsTrue(Count($"SELECT COUNT(*) AS Value FROM creature_appearance WHERE id IN ({npcs})") >= 3, "the three looks");
            }

            AllUp();
            world.GetService<IMigrator>().Migrate(Previous(world, FormalComplaints));
            world.ChangeTracker.Clear();
            Assert.IsFalse(missions.Any(Has));
            Assert.AreEqual((0, 0, 0, 0, 0, 0, 3, 3), All());
            CollectionAssert.AreEqual(new ((double, double, double), double)[]
            {
                ((544.2000122070312, 283.20001220703125, -120), 14.4), ((499, 285.5, -269.29998779296875), 10.7), ((502, 277.5, -198.1999969482422), 12.6)
            }, Thrax(), "upstream's Thrax pools, to the float");
            Assert.AreEqual(others, Others(), "no other Ashen pool changes");
            Assert.AreEqual(0, Count($"SELECT COUNT(*) AS Value FROM creature_appearance WHERE id IN ({npcs})"), "no look left");
            Assert.IsFalse(world.MissionEvidenceEntries.Any(entry => entry.ContentRevision == AshenMissionDataV1.Revision && missions.Contains(entry.MissionId)));
            Assert.IsFalse(world.MissionRewardItemEntries.Any(entry => entry.ContentRevision == AshenMissionDataV1.Revision && missions.Contains(entry.MissionId)));
            Assert.IsFalse(world.MissionObjectiveDefinitionEntries.Any(entry => entry.ContentRevision == AshenMissionDataV1.Revision && missions.Contains(entry.MissionId)));
            Assert.IsFalse(world.MissionPrerequisiteEntries.Any(entry => entry.ContentRevision == AshenMissionDataV1.Revision && missions.Contains(entry.MissionId)));
            Assert.IsFalse(world.Set<MissionActionEntry>().Any(entry => entry.ContentRevision == AshenMissionDataV1.Revision && missions.Contains(entry.MissionId)));
            Assert.IsTrue(MissionIds.Take(18).All(Has), "sets 1-5 stay");

            world.GetService<IMigrator>().Migrate(FormalComplaints);
            world.ChangeTracker.Clear();
            AllUp();
            Assert.AreEqual(others, Others(), "no other Ashen pool changes");
        }

        /// <summary>
        /// Set 7's creatures: the Nitroglazer 790240 (class 7234, the client's name 434) and the Hominis Machina Mk XI 790241 (class
        /// 3868, 7716), hostile, level 40, 8409 hit points and armour 285 (ESTIMATE: the Ashen level-40 rows'); their attacks
        /// their own rows (the World DB rows' actions and reaches at the Ashen Thrax damage), the Machina's revive upstream's
        /// 62059 and its look the Torcastra Machina 660012's; two pools of three of each (radius 0, 120 s respawn, automatic) on
        /// Ashen Desert, the only pools that hold them.
        /// </summary>
        [TestMethod]
        public void TheBoneyardRobotsHaveTheirOwnRows()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var world = harness.World;
            var creatures = world.CreatureEntries.AsNoTracking().ToDictionary(row => row.Id);
            var stats = world.CreatureStatEntries.AsNoTracking().ToDictionary(row => row.Id);
            var actions = world.Set<CreatureActionEntry>().AsNoTracking().ToDictionary(row => row.Id);
            foreach (var (robot, cls, name, attacks, pools) in new (uint, uint, uint, uint[], uint[])[]
            {
                (Nitroglazer, 7234, 434, new uint[] { 790240, 790242 }, NitroPools),
                (Machina, 3868, 7716, new uint[] { 790241, 62059 }, MachinaPools)
            })
            {
                var row = creatures[robot];
                Assert.AreEqual((cls, name, 40U, 0U, 8409U, 9U, 5U), (row.ClassId, row.NameId, row.Level, row.Faction, row.MaxHitPoints, row.RunSpeed, row.WalkSpeed), $"{robot}");
                Assert.AreEqual((attacks[0], attacks[1], 0U), (row.Action1, row.Action2, row.Action3), $"{robot} attacks");
                Assert.AreEqual((8409, 285), (stats[robot].Health, stats[robot].Armor), $"{robot} stats");
                Assert.IsTrue(CreatureManager.Instance.LoadedCreatures.TryGetValue(robot, out var loaded) && loaded.TargetCategory == TargetCategory.Hostile, $"{robot} hostile");
                CollectionAssert.AreEquivalent(pools, world.SpawnPoolEntries.AsNoTracking().Where(pool => pool.Creature1Id == robot).Select(pool => pool.Id).ToArray(), $"{robot}'s pools");
                foreach (var pool in pools)
                {
                    var entry = world.SpawnPoolEntries.AsNoTracking().Single(candidate => candidate.Id == pool);
                    Assert.AreEqual((Map, (byte)3, (byte)3, 0U, 0.0, 1200U, (byte)0), (entry.MapContextId, entry.Creature1MinCount, entry.Creature1MaxCount,
                        entry.Creature2Id, entry.Radius, entry.RespawnTime, entry.Mode), $"pool {pool}");
                }
            }
            // The attacks: action, argument, reach, cooldown, damage, kind (the World DB rows' 710001 / 710002 / 660002 but the damage).
            foreach (var (id, action, arg, min, max, cooldown, low, high) in new (uint, uint, uint, double, double, uint, uint, uint)[]
            {
                (790240, 1, 232, 1, 30, 1500, 247, 370), (790242, 289, 1, 5, 40, 12000, 356, 555), (790241, 1, 97, 1, 20, 800, 247, 370)
            })
            {
                var row = actions[id];
                Assert.AreEqual((action, arg, min, max, cooldown, 0U, low, high, 1U), (row.ActionId, row.ActionArgId, row.RangeMin, row.RangeMax, row.Cooldown,
                    row.Windup, row.MinDamage, row.MaxDamage, row.DamageType), $"action {id}");
            }
            Assert.AreEqual(429U, actions[62059].ActionId, "upstream's machina_revive, unchanged");
            CollectionAssert.AreEquivalent(
                world.CreatureAppearanceEntries.AsNoTracking().Where(row => row.Id == 660012).ToList().Select(row => (row.SlotId, row.ClassId, row.Color)).ToArray(),
                world.CreatureAppearanceEntries.AsNoTracking().Where(row => row.Id == Machina).ToList().Select(row => (row.SlotId, row.ClassId, row.Color)).ToArray(),
                "the Machina's look is 660012's");
        }

        /// <summary>
        /// Ashen_scavenger takes out its three missions, the two robots (creature, stats, look, attacks) and their four pools; the
        /// upstream rows it uses (Latan's set 6 row, the Thrax, the Stalker and Predator of sets 3 and 4, the revive action and
        /// 660012's look) stay, no Ashen pool changes, sets 1-6 stay. Up again, all of it is back. The counts take only set 7's own
        /// ids and missions.
        /// </summary>
        [TestMethod]
        public void ScavengerRollsBackWithItsMigration()
        {
            var missions = new[] { AshenRoboticSalvageV1.MissionId, AshenWeaponSalvageV1.MissionId, AshenHeavySalvageV1.MissionId };
            using var harness = WildernessRuntimeTestHarness.Create();
            var world = harness.World;
            bool Has(uint mission) => world.MissionContentDefinitionEntries.AsNoTracking()
                .Any(entry => entry.MissionId == mission && entry.ContentRevision == AshenMissionDataV1.Revision);
            int Count(string sql) => world.Database.SqlQueryRaw<int>(sql).AsEnumerable().Single();
            var robots = $"{Nitroglazer}, {Machina}";
            var pools = string.Join(", ", NitroPools.Concat(MachinaPools));
            var ids = string.Join(", ", missions);
            (int, int, int, int, int, int, int, int, int) All() => (
                Count($"SELECT COUNT(*) AS Value FROM creature WHERE id IN ({robots})"),
                Count($"SELECT COUNT(*) AS Value FROM creature_stat WHERE id IN ({robots})"),
                Count($"SELECT COUNT(*) AS Value FROM creature_appearance WHERE id IN ({robots})"),
                Count("SELECT COUNT(*) AS Value FROM creature_action WHERE id IN (790240, 790241, 790242)"),
                Count($"SELECT COUNT(*) AS Value FROM spawnpool WHERE id IN ({pools})"),
                Count($"SELECT COUNT(*) AS Value FROM mission_indicator WHERE content_revision = 'ashen_1_16_5' AND mission_id IN ({ids})"),
                Count($"SELECT COUNT(*) AS Value FROM mission_scene_binding WHERE content_revision = 'ashen_1_16_5' AND mission_id IN ({ids})"),
                Count($"SELECT COUNT(*) AS Value FROM creature WHERE id IN ({Latan}, {ThraxSoldier}, {Ravager}, {Stalker}, {Predator}, 660012)"),
                Count("SELECT COUNT(*) AS Value FROM creature_action WHERE id = 62059"));
            var look = Count("SELECT COUNT(*) AS Value FROM creature_appearance WHERE id = 660012");
            Assert.IsGreaterThan(0, look);
            var later = PoolsOfLaterMigrations(world, Scavenger);
            string Others() => string.Join("; ", world.SpawnPoolEntries.AsNoTracking()
                .Where(entry => entry.MapContextId == AshenMissionDataV1.MapContextId && !NitroPools.Contains(entry.Id) && !MachinaPools.Contains(entry.Id) && !later.Contains(entry.Id))
                .OrderBy(entry => entry.Id).Select(entry => new { entry.Id, entry.PosX, entry.PosY, entry.PosZ, entry.Radius, entry.Creature1Id }).AsEnumerable().Select(entry => entry.ToString()));
            var others = Others();
            void AllUp()
            {
                Assert.IsTrue(missions.All(Has));
                Assert.AreEqual((2, 2, look, 3, 4, 13, 3, 6, 1), All());
            }

            AllUp();
            world.GetService<IMigrator>().Migrate(Previous(world, Scavenger));
            world.ChangeTracker.Clear();
            Assert.IsFalse(missions.Any(Has));
            Assert.AreEqual((0, 0, 0, 0, 0, 0, 0, 6, 1), All());
            Assert.AreEqual(others, Others(), "no other Ashen pool changes");
            Assert.IsFalse(world.MissionEvidenceEntries.Any(entry => entry.ContentRevision == AshenMissionDataV1.Revision && missions.Contains(entry.MissionId)));
            Assert.IsFalse(world.MissionRewardItemEntries.Any(entry => entry.ContentRevision == AshenMissionDataV1.Revision && missions.Contains(entry.MissionId)));
            Assert.IsFalse(world.MissionObjectiveDefinitionEntries.Any(entry => entry.ContentRevision == AshenMissionDataV1.Revision && missions.Contains(entry.MissionId)));
            Assert.IsFalse(world.MissionPrerequisiteEntries.Any(entry => entry.ContentRevision == AshenMissionDataV1.Revision && missions.Contains(entry.MissionId)));
            Assert.IsFalse(world.Set<MissionTriggerEntry>().Any(entry => entry.ContentRevision == AshenMissionDataV1.Revision && missions.Contains(entry.MissionId)));
            Assert.IsTrue(MissionIds.Take(23).All(Has), "sets 1-6 stay");

            world.GetService<IMigrator>().Migrate(Scavenger);
            world.ChangeTracker.Clear();
            AllUp();
            Assert.AreEqual(others, Others(), "no other Ashen pool changes");
        }

        /// <summary>
        /// Ashen_fix_d rolls back: Down puts 1264's reward item 2 back to set 7's 29093 and takes out its evidence row; the other
        /// three items, the other missions' rewards and 1264 itself stay; Up again, 29113 and the evidence are back. A second Up
        /// (its SQL run again) changes nothing, and a row holding neither value is not touched by Up or Down.
        /// </summary>
        [TestMethod]
        public void FixDRollsBackWithItsMigration()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var world = harness.World;
            (uint, uint)[] Items() => world.MissionRewardItemEntries.AsNoTracking()
                .Where(entry => entry.MissionId == 1264 && entry.ContentRevision == AshenMissionDataV1.Revision)
                .OrderBy(entry => entry.ItemId).AsEnumerable().Select(entry => (entry.ItemTemplateId, entry.Quantity)).ToArray();
            bool Evidence() => world.MissionEvidenceEntries.AsNoTracking().Any(entry => entry.MissionId == 1264 &&
                entry.ContentRevision == AshenMissionDataV1.Revision && entry.EvidenceId == 4);
            string Others() => string.Join("; ", world.MissionRewardItemEntries.AsNoTracking()
                .Where(entry => entry.ContentRevision == AshenMissionDataV1.Revision && entry.MissionId != 1264 && entry.MissionId < 1267)
                .OrderBy(entry => entry.MissionId).ThenBy(entry => entry.ItemId).AsEnumerable().Select(entry => $"{entry.MissionId}:{entry.ItemTemplateId}x{entry.Quantity}"));
            (double, double, double, double, byte) Hospital() => world.TeleporterEntries.AsNoTracking().Where(row => row.Id == 208)
                .AsEnumerable().Select(row => (row.PosX, row.PosY, row.PosZ, row.Rotation, row.Type)).Single();
            // Fix H's two pads (913, 914; a later migration) go and come back with the roll back to before fix D.
            string OtherTeleporters() => string.Join("; ", world.TeleporterEntries.AsNoTracking().Where(row => row.Id != 208 && row.Id != AshenFixHV1.LowerPad && row.Id != AshenFixHV1.UpperPad &&
                row.Id != AshenAvernusOutpostNpcsV1.CavernPad && row.Id != AshenAvernusOutpostNpcsV1.GraveyardPad && !AshenAvernusHiveTests.Pads.Contains(row.Id) &&
                !AshenConscriptionEntryTests.Pads.Contains(row.Id) && row.Id != AshenFixLV1.PrisonPad && row.Id != AshenFixLV1.OutsidePad &&
                !AshenConscriptionQueenTests.Pads.Contains(row.Id))
                .OrderBy(row => row.Id).AsEnumerable().Select(row => System.FormattableString.Invariant($"{row.Id}:{row.PosX:R},{row.PosY:R},{row.PosZ:R},{row.Rotation:R}")));
            var fixedHospital = (-94.13672, 296.5, -828.72266, 3.7, (byte)5);
            var upstreamHospital = (-94.13672, 269.3, -828.72266, 3.7, (byte)5);
            var fixedItems = new[] { (23698U, 1U), (29113U, 1U), (20802U, 1U), (36039U, 1U) };
            var others = Others();
            var otherTeleporters = OtherTeleporters();
            Assert.AreEqual(fixedHospital, Hospital(), "fix D: hospital 208 at 296.5");
            CollectionAssert.AreEqual(fixedItems, Items(), "fix D: 29113");
            Assert.IsTrue(Evidence(), "fix D's evidence");

            world.GetService<IMigrator>().Migrate(Previous(world, FixD));
            world.ChangeTracker.Clear();
            CollectionAssert.AreEqual(new[] { (23698U, 1U), (29093U, 1U), (20802U, 1U), (36039U, 1U) }, Items(), "set 7's 29093 back");
            Assert.IsFalse(Evidence(), "no fix D evidence");
            Assert.IsTrue(world.MissionContentDefinitionEntries.Any(entry => entry.MissionId == 1264 && entry.ContentRevision == AshenMissionDataV1.Revision && entry.Enabled), "1264 stays (set 7)");
            Assert.AreEqual(others, Others(), "no other reward changes");
            Assert.AreEqual(upstreamHospital, Hospital(), "upstream's 269.3 back, exact");
            Assert.AreEqual(otherTeleporters, OtherTeleporters(), "no other teleporter changes");

            world.GetService<IMigrator>().Migrate(FixD);
            world.ChangeTracker.Clear();
            CollectionAssert.AreEqual(fixedItems, Items(), "29113 again");
            Assert.IsTrue(Evidence(), "fix D's evidence again");
            foreach (var sql in AshenFixDV1.UpSql())
                world.Database.ExecuteSqlRaw(sql);
            CollectionAssert.AreEqual(fixedItems, Items(), "a second Up changes nothing");
            Assert.AreEqual(fixedHospital, Hospital(), "a second Up leaves the hospital at 296.5");
            // The guard: a row that holds neither set 7's value nor fix D's is left alone by both directions.
            const string item2 = "UPDATE mission_reward_item SET item_template_id = {0} WHERE mission_id = 1264 AND content_revision = 'ashen_1_16_5' AND reward_id = 1 AND item_id = 2;";
            world.Database.ExecuteSqlRaw(string.Format(System.Globalization.CultureInfo.InvariantCulture, item2, 12345));
            foreach (var sql in AshenFixDV1.UpSql().Concat(AshenFixDV1.DownSql()))
                world.Database.ExecuteSqlRaw(sql);
            Assert.AreEqual(12345U, Items()[1].Item1, "a row holding another value is not touched");
            world.Database.ExecuteSqlRaw(string.Format(System.Globalization.CultureInfo.InvariantCulture, item2, 29113));
            Assert.AreEqual(others, Others(), "no other reward changes");
            Assert.AreEqual(upstreamHospital, Hospital(), "the Up-then-Down above put the hospital back at 269.3");
            foreach (var sql in AshenFixDV1.UpSql())
                world.Database.ExecuteSqlRaw(sql);
            Assert.AreEqual(fixedHospital, Hospital(), "Up again: 296.5");
            CollectionAssert.AreEqual(fixedItems, Items(), "and 1264's items stay");
            // The same for the hospital: a moved x (or a height that is neither value) is left alone.
            world.Database.ExecuteSqlRaw("UPDATE teleporter SET pos_x = -94.0 WHERE id = 208;");
            foreach (var sql in AshenFixDV1.DownSql().Concat(AshenFixDV1.UpSql()))
                world.Database.ExecuteSqlRaw(sql);
            Assert.AreEqual((-94.0, 296.5, -828.72266, 3.7, (byte)5), Hospital(), "a hospital row with another x is not touched");
            world.Database.ExecuteSqlRaw("UPDATE teleporter SET pos_x = -94.13672, pos_y = 280.0 WHERE id = 208;");
            foreach (var sql in AshenFixDV1.UpSql().Concat(AshenFixDV1.DownSql()))
                world.Database.ExecuteSqlRaw(sql);
            Assert.AreEqual((-94.13672, 280.0, -828.72266, 3.7, (byte)5), Hospital(), "a hospital row at another height is not touched");
            world.Database.ExecuteSqlRaw("UPDATE teleporter SET pos_y = 296.5 WHERE id = 208;");
            Assert.AreEqual(otherTeleporters, OtherTeleporters(), "no other teleporter changes");
        }

        /// <summary>
        /// Ashen_logistician takes out Ito's three missions and nothing else: their definitions, objectives, triggers,
        /// prerequisites, rewards, markers, scenes and evidence go; Latan's twins (set 7, the same item classes), Ito's row, the
        /// creatures and every Ashen pool stay as they were; sets 1-7 stay. Up again, all of it is back. The counts take only set
        /// 8's missions.
        /// </summary>
        [TestMethod]
        public void LogisticianRollsBackWithItsMigration()
        {
            var missions = new[] { AshenRoboticAcquisitionsV1.MissionId, AshenWeaponAcquisitionsV1.MissionId, AshenHeavyAcquisitionsV1.MissionId };
            using var harness = WildernessRuntimeTestHarness.Create();
            var world = harness.World;
            bool Has(uint mission) => world.MissionContentDefinitionEntries.AsNoTracking()
                .Any(entry => entry.MissionId == mission && entry.ContentRevision == AshenMissionDataV1.Revision);
            int Count(string sql) => world.Database.SqlQueryRaw<int>(sql).AsEnumerable().Single();
            const string ids = "1267, 1268, 1269";
            (int, int, int, int, int, int, int, int) All() => (
                Count($"SELECT COUNT(*) AS Value FROM mission_objective_definition WHERE content_revision = 'ashen_1_16_5' AND mission_id IN ({ids})"),
                Count($"SELECT COUNT(*) AS Value FROM mission_trigger WHERE content_revision = 'ashen_1_16_5' AND mission_id IN ({ids})"),
                Count($"SELECT COUNT(*) AS Value FROM mission_prerequisite WHERE content_revision = 'ashen_1_16_5' AND mission_id IN ({ids})"),
                Count($"SELECT COUNT(*) AS Value FROM mission_reward_item WHERE content_revision = 'ashen_1_16_5' AND mission_id IN ({ids})"),
                Count($"SELECT COUNT(*) AS Value FROM mission_indicator WHERE content_revision = 'ashen_1_16_5' AND mission_id IN ({ids})"),
                Count($"SELECT COUNT(*) AS Value FROM mission_scene_binding WHERE content_revision = 'ashen_1_16_5' AND mission_id IN ({ids})"),
                Count($"SELECT COUNT(*) AS Value FROM mission_evidence WHERE content_revision = 'ashen_1_16_5' AND mission_id IN ({ids})"),
                Count($"SELECT COUNT(*) AS Value FROM creature WHERE id IN ({Ito}, {Nitroglazer}, {Machina}, {ThraxSoldier}, {Ravager}, {Stalker}, {Predator})"));
            string Twins() => string.Join("; ", world.MissionIndicatorEntries.AsNoTracking()
                .Where(entry => entry.ContentRevision == AshenMissionDataV1.Revision && entry.MissionId >= 1264 && entry.MissionId <= 1266)
                .OrderBy(entry => entry.MissionId).ThenBy(entry => entry.ObjectiveId).ThenBy(entry => entry.IndicatorId)
                .Select(entry => new { entry.MissionId, entry.ObjectiveId, entry.IndicatorId, entry.PosX, entry.PosZ }).AsEnumerable()
                // 1266's launcher markers move with fix E (a later migration), down and up with it: AshenFixETests.
                .Where(entry => !AshenFixEV1.Markers.Any(marker => marker.Mission == entry.MissionId && marker.Objective == entry.ObjectiveId && marker.Indicator == entry.IndicatorId))
                .Select(entry => entry.ToString()))
                + "|" + string.Join("; ", Scenes(harness).Where(entry => entry.MissionId >= 1264 && entry.MissionId <= 1266).OrderBy(entry => entry.MissionId)
                    .SelectMany(entry => entry.Scene.Items.Select(item => $"{entry.MissionId}:{item.ItemTemplateId}:{item.Drop.ChancePercent}")));
            var later = PoolsOfLaterMigrations(world, Logistician);
            string Others() => string.Join("; ", world.SpawnPoolEntries.AsNoTracking()
                .Where(entry => entry.MapContextId == AshenMissionDataV1.MapContextId && !later.Contains(entry.Id))
                .OrderBy(entry => entry.Id).Select(entry => new { entry.Id, entry.PosX, entry.PosY, entry.PosZ, entry.Radius, entry.Creature1Id }).AsEnumerable().Select(entry => entry.ToString()));
            var others = Others();
            var twins = Twins();
            Assert.Contains("1264:97465:75", twins, "Latan's twins read");
            // Evidence: 9, and fix E's row on 1269 while fix E is up (all migrations).
            void AllUp(int evidence = 9)
            {
                Assert.IsTrue(missions.All(Has));
                Assert.AreEqual((6, 6, 5, 4 + 4 + 4, 4 + 5 + 4, 3, evidence, 7), All());
            }

            AllUp(9 + 1);
            world.GetService<IMigrator>().Migrate(Previous(world, Logistician));
            world.ChangeTracker.Clear();
            Assert.IsFalse(missions.Any(Has));
            Assert.AreEqual((0, 0, 0, 0, 0, 0, 0, 7), All());
            Assert.AreEqual(others, Others(), "no Ashen pool changes");
            Assert.AreEqual(twins, Twins(), "Latan's twins unchanged");
            Assert.IsTrue(MissionIds.Take(26).All(Has), "sets 1-7 stay");

            world.GetService<IMigrator>().Migrate(Logistician);
            world.ChangeTracker.Clear();
            AllUp();
            Assert.AreEqual(others, Others(), "no Ashen pool changes");
            Assert.AreEqual(twins, Twins(), "Latan's twins unchanged");
        }

        /// <summary>
        /// Ashen_stonewall_ridge takes out Emmons' three missions and nothing else: their definitions, objectives, triggers,
        /// prerequisites, rewards, markers, scenes and evidence go; Emmons' row, the AFS Turret, the Thrax and the Desert Stalker
        /// rows and every Ashen pool (the four batteries' 550001-550004 with them) stay as they were; sets 1-8 stay. Up again, all
        /// of it is back. The counts take only set 9's missions.
        /// </summary>
        [TestMethod]
        public void StonewallRidgeRollsBackWithItsMigration()
        {
            var missions = new[] { AshenCycledFrequenciesV1.MissionId, AshenOnTheSameFrequencyV1.MissionId, AshenKingOfTheHillV1.MissionId };
            using var harness = WildernessRuntimeTestHarness.Create();
            var world = harness.World;
            bool Has(uint mission) => world.MissionContentDefinitionEntries.AsNoTracking()
                .Any(entry => entry.MissionId == mission && entry.ContentRevision == AshenMissionDataV1.Revision);
            int Count(string sql) => world.Database.SqlQueryRaw<int>(sql).AsEnumerable().Single();
            const string ids = "1275, 1277, 1278";
            (int, int, int, int, int, int, int, int, int, (int, int, int, int, int)) All() => (
                Count($"SELECT COUNT(*) AS Value FROM mission_objective_definition WHERE content_revision = 'ashen_1_16_5' AND mission_id IN ({ids})"),
                Count($"SELECT COUNT(*) AS Value FROM mission_trigger WHERE content_revision = 'ashen_1_16_5' AND mission_id IN ({ids})"),
                Count($"SELECT COUNT(*) AS Value FROM mission_prerequisite WHERE content_revision = 'ashen_1_16_5' AND mission_id IN ({ids})"),
                Count($"SELECT COUNT(*) AS Value FROM mission_reward_item WHERE content_revision = 'ashen_1_16_5' AND mission_id IN ({ids})"),
                Count($"SELECT COUNT(*) AS Value FROM mission_indicator WHERE content_revision = 'ashen_1_16_5' AND mission_id IN ({ids})"),
                Count($"SELECT COUNT(*) AS Value FROM mission_scene_binding WHERE content_revision = 'ashen_1_16_5' AND mission_id IN ({ids})"),
                Count($"SELECT COUNT(*) AS Value FROM mission_evidence WHERE content_revision = 'ashen_1_16_5' AND mission_id IN ({ids})"),
                Count($"SELECT COUNT(*) AS Value FROM mission_area WHERE content_revision = 'ashen_1_16_5' AND mission_id IN ({ids})"),
                Count($"SELECT COUNT(*) AS Value FROM creature WHERE id IN ({Emmons}, 550001, {ThraxSoldier}, {Ravager}, {Stalker})"),
                (Count($"SELECT COUNT(*) AS Value FROM mission_action WHERE content_revision = 'ashen_1_16_5' AND mission_id IN ({ids})"),
                 Count($"SELECT COUNT(*) AS Value FROM mission_scenario WHERE content_revision = 'ashen_1_16_5' AND mission_id IN ({ids})"),
                 Count($"SELECT COUNT(*) AS Value FROM mission_scenario_step WHERE content_revision = 'ashen_1_16_5' AND mission_id IN ({ids})"),
                 Count($"SELECT COUNT(*) AS Value FROM mission_objective_transition WHERE content_revision = 'ashen_1_16_5' AND mission_id IN ({ids})"),
                 Count($"SELECT COUNT(*) AS Value FROM mission_reward_definition WHERE content_revision = 'ashen_1_16_5' AND mission_id IN ({ids})")));
            var later = PoolsOfLaterMigrations(world, StonewallRidge);
            string Others() => string.Join("; ", world.SpawnPoolEntries.AsNoTracking()
                .Where(entry => entry.MapContextId == AshenMissionDataV1.MapContextId && !later.Contains(entry.Id))
                .OrderBy(entry => entry.Id).Select(entry => new { entry.Id, entry.PosX, entry.PosY, entry.PosZ, entry.Radius, entry.Creature1Id }).AsEnumerable().Select(entry => entry.ToString()));
            var others = Others();
            Assert.Contains("Id = 550004", others, "the batteries' pools read");
            void AllUp(int evidence = 9)
            {
                Assert.IsTrue(missions.All(Has));
                // Actions: 1275 its reward; 1277 tower 1's reward; 1278 1's reveal, open and start, 2's reward. Scenarios 1 + 1 + 3,
                // their steps 1 + 4 + (0 + 1 + 4). Transitions 1 + 4 + (3 + 2 + 1). Reward definitions one each. Evidence 9, and
                // fix E's row on 1277 while fix E is up (all migrations).
                Assert.AreEqual((1 + 4 + 3, 1 + 4 + 9, 2, 4 + 4 + 4, 1 + 4 + 2, 3, evidence, 1, 5, (1 + 1 + 4, 1 + 1 + 3, 1 + 4 + 5, 1 + 4 + 6, 3)), All());
            }

            AllUp(9 + 1);
            world.GetService<IMigrator>().Migrate(Previous(world, StonewallRidge));
            world.ChangeTracker.Clear();
            Assert.IsFalse(missions.Any(Has));
            Assert.AreEqual((0, 0, 0, 0, 0, 0, 0, 0, 5, (0, 0, 0, 0, 0)), All());
            Assert.AreEqual(others, Others(), "no Ashen pool changes");
            Assert.IsTrue(MissionIds.Take(29).All(Has), "sets 1-8 stay");

            world.GetService<IMigrator>().Migrate(StonewallRidge);
            world.ChangeTracker.Clear();
            AllUp();
            Assert.AreEqual(others, Others(), "no Ashen pool changes");
        }

        /// <summary>
        /// Set 10's creatures, written out: the normals copy the World DB rows of their kind (all of the row but id and comment:
        /// the Lasher 770008, the Beam Manta 540040 "Beam Manta Pilot", the Granitour 540031, with their stats), in pools of
        /// three (radius 0, 120 s respawn, automatic): two Lasher, three Beam Manta, two Granitour pools. The bosses the client's
        /// names (9359 Sally, 9360 Molly, 9361 Trudy, 9362 Wendy) on the boss classes (22627, 10331, 10317, 25620), level 44,
        /// 4900 hit points, armour 420 (the Ashen boss rows' 520002-520007), the actions of the rows they copy (520002, 770034,
        /// 520049, 540031; Wendy's spit fix W's 470/1 row 790243), each in a pool of one (radius 0, 50 s). All hostile, on Ashen Desert, the only pools that hold them.
        /// </summary>
        [TestMethod]
        public void TheHideCreaturesCopyTheirWorldRows()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var world = harness.World;
            var creatures = world.CreatureEntries.AsNoTracking().ToDictionary(row => row.Id);
            var stats = world.CreatureStatEntries.AsNoTracking().ToDictionary(row => row.Id);
            foreach (var (creature, copy, cls, name, pools) in new (uint, uint, uint, uint, uint[])[]
            {
                (Lasher, 770008, 7477, 0, new uint[] { 790363, 790364, 790440, 790441 }), // set 12 adds the two ridge pools (Ashen_murky_waters)
                (Manta, 540040, 6441, 8023, new uint[] { 790368, 790369, 790370 }),
                (Granitour, 540031, 10164, 0, new uint[] { 790374, 790375 })
            })
            {
                var row = creatures[creature];
                var source = creatures[copy];
                Assert.AreEqual((cls, name, 0U), (row.ClassId, row.NameId, row.Faction), $"{creature}");
                Assert.AreEqual((source.ClassId, source.Level, source.MaxHitPoints, source.NameId, source.Action1, source.Action2, source.Action3, source.RunSpeed, source.WalkSpeed),
                    (row.ClassId, row.Level, row.MaxHitPoints, row.NameId, row.Action1, row.Action2, row.Action3, row.RunSpeed, row.WalkSpeed), $"{creature} copies {copy}");
                Assert.AreEqual((stats[copy].Health, stats[copy].Armor), (stats[creature].Health, stats[creature].Armor), $"{creature} stats");
                Assert.IsTrue(CreatureManager.Instance.LoadedCreatures.TryGetValue(creature, out var loaded) && loaded.TargetCategory == TargetCategory.Hostile, $"{creature} hostile");
                CollectionAssert.AreEquivalent(pools, world.SpawnPoolEntries.AsNoTracking().Where(pool => pool.Creature1Id == creature).Select(pool => pool.Id).ToArray(), $"{creature}'s pools");
                foreach (var pool in pools)
                {
                    var entry = world.SpawnPoolEntries.AsNoTracking().Single(candidate => candidate.Id == pool);
                    Assert.AreEqual((Map, (byte)3, (byte)3, 0U, 0.0, 1200U, (byte)0), (entry.MapContextId, entry.Creature1MinCount, entry.Creature1MaxCount,
                        entry.Creature2Id, entry.Radius, entry.RespawnTime, entry.Mode), $"pool {pool}");
                }
            }
            foreach (var (boss, copy, cls, name, pool) in new (uint, uint, uint, uint, uint)[]
            {
                (Sally, 520002, 22627, 9359, 790361), (Molly, 770034, 10331, 9360, 790366), (Trudy, 520049, 10317, 9361, 790372), (Wendy, 540031, 25620, 9362, 790377)
            })
            {
                var row = creatures[boss];
                var source = creatures[copy];
                Assert.AreEqual((cls, name, 44U, 4900U, 0U), (row.ClassId, row.NameId, row.Level, row.MaxHitPoints, row.Faction), $"{boss}");
                // Fix W (AshenFixWV1): Wendy spits with the adult Granitour's 470/1 (790243), which her model animates, not 540031's 472/1.
                var spit = boss == Wendy ? AshenFixWV1.WendySpit : source.Action2;
                Assert.AreEqual((source.Action1, spit, source.Action3, source.RunSpeed, source.WalkSpeed), (row.Action1, row.Action2, row.Action3, row.RunSpeed, row.WalkSpeed), $"{boss}: {copy}'s actions");
                Assert.AreEqual((4900, 420), (stats[boss].Health, stats[boss].Armor), $"{boss} stats");
                Assert.IsTrue(CreatureManager.Instance.LoadedCreatures.TryGetValue(boss, out var loaded) && loaded.TargetCategory == TargetCategory.Hostile, $"{boss} hostile");
                CollectionAssert.AreEqual(new[] { pool }, world.SpawnPoolEntries.AsNoTracking().Where(entry => entry.Creature1Id == boss).Select(entry => entry.Id).ToArray(), $"{boss}'s pool");
                var entry = world.SpawnPoolEntries.AsNoTracking().Single(candidate => candidate.Id == pool);
                Assert.AreEqual((Map, (byte)1, (byte)1, 0U, 0.0, AshenFixFV1.BossRespawnTenths, (byte)0), (entry.MapContextId, entry.Creature1MinCount, entry.Creature1MaxCount,
                    entry.Creature2Id, entry.Radius, entry.RespawnTime, entry.Mode), $"pool {pool}");
            }
            Assert.AreEqual(0, world.CreatureAppearanceEntries.AsNoTracking().Count(row => row.Id >= 790360 && row.Id <= 790399), "no look rows (the rows copied have none)");
        }

        /// <summary>
        /// Ashen_designer_hides takes out Ida Hayner's four missions, the seven creatures (creature, stats) and their eleven
        /// pools, and puts Ida's pool back on the weapon vendor's spot, exact; the rows it copies or uses (Ida, set 2's Atta, the
        /// World DB's 520002, 770008, 770034, 540040, 520049, 540031) stay, no other Ashen pool changes, sets 1-9 stay. Up again,
        /// all of it is back. Every table the migration writes is counted (set 10's ids and missions only); a second Down and Up
        /// (the SQL run again) change nothing, and Ida's update leaves a pool that stands elsewhere alone.
        /// </summary>
        [TestMethod]
        public void DesignerHidesRollsBackWithItsMigration()
        {
            var missions = new[] { AshenDesignerJewelryV1.MissionId, AshenLatestRageV1.MissionId, AshenWouldntWearItV1.MissionId, AshenNewFadV1.MissionId };
            using var harness = WildernessRuntimeTestHarness.Create();
            var world = harness.World;
            bool Has(uint mission) => world.MissionContentDefinitionEntries.AsNoTracking()
                .Any(entry => entry.MissionId == mission && entry.ContentRevision == AshenMissionDataV1.Revision);
            int Count(string sql) => world.Database.SqlQueryRaw<int>(sql).AsEnumerable().Single();
            const string ids = "1287, 1289, 1291, 1293";
            const string creatures = "790360, 790362, 790365, 790367, 790371, 790373, 790376";
            const string pools = "790361, 790363, 790364, 790366, 790368, 790369, 790370, 790372, 790374, 790375, 790377";
            // Every table the migration writes (its Up and its Down: AshenMissionDataV1.Remove clears all eighteen mission tables).
            var tables = new[]
            {
                "mission_content_definition", "mission_objective_definition", "mission_objective_transition", "mission_trigger", "mission_action",
                "mission_prerequisite", "mission_reward_definition", "mission_reward_item", "mission_indicator", "mission_scene_binding", "mission_evidence",
                "mission_scenario", "mission_scenario_step", "mission_channel_policy", "mission_repeat_policy", "mission_spawn", "mission_spawn_group", "mission_area"
            };
            string All() => string.Join(", ", tables.Select(table => $"{table} {Count($"SELECT COUNT(*) AS Value FROM {table} WHERE content_revision = 'ashen_1_16_5' AND mission_id IN ({ids})")}")
                .Append($"creature {Count($"SELECT COUNT(*) AS Value FROM creature WHERE id IN ({creatures})")}")
                .Append($"creature_stat {Count($"SELECT COUNT(*) AS Value FROM creature_stat WHERE id IN ({creatures})")}")
                .Append($"spawnpool {Count($"SELECT COUNT(*) AS Value FROM spawnpool WHERE id IN ({pools})")}")
                .Append($"kept {Count($"SELECT COUNT(*) AS Value FROM creature WHERE id IN ({Ida}, {AttaSoldier}, {AttaWorker}, 520002, 770008, 770034, 540040, 520049, 540031)")}"));
            (double X, double Y, double Z) IdaAt()
            {
                var row = world.SpawnPoolEntries.AsNoTracking().Single(entry => entry.Id == Ida);
                return (row.PosX, row.PosY, row.PosZ);
            }
            var later = PoolsOfLaterMigrations(world, DesignerHides);
            string Others() => string.Join("; ", world.SpawnPoolEntries.AsNoTracking()
                .Where(entry => entry.MapContextId == AshenMissionDataV1.MapContextId && entry.Id != Ida && !(entry.Id >= 790360 && entry.Id <= 790399) && !later.Contains(entry.Id))
                .OrderBy(entry => entry.Id).Select(entry => new { entry.Id, entry.PosX, entry.PosY, entry.PosZ, entry.Radius, entry.Creature1Id }).AsEnumerable().Select(entry => entry.ToString()));
            var others = Others();
            var upstream = (-167.10000610351562, 296.27, -709.0);
            // Each mission: two objectives, one transition and one trigger each, one reward action, one reward (four items),
            // its markers (5, 3, 4, 3), a scene, three evidence rows; 1291 and 1293 one prerequisite each. No scenarios, no
            // channel or repeat policy, spawns or areas.
            const string up = "mission_content_definition 4, mission_objective_definition 8, mission_objective_transition 8, mission_trigger 8, " +
                "mission_action 4, mission_prerequisite 2, mission_reward_definition 4, mission_reward_item 16, mission_indicator 15, " +
                "mission_scene_binding 4, mission_evidence 12, mission_scenario 0, mission_scenario_step 0, mission_channel_policy 0, " +
                "mission_repeat_policy 0, mission_spawn 0, mission_spawn_group 0, mission_area 0, creature 7, creature_stat 7, spawnpool 11, kept 9";
            const string down = "mission_content_definition 0, mission_objective_definition 0, mission_objective_transition 0, mission_trigger 0, " +
                "mission_action 0, mission_prerequisite 0, mission_reward_definition 0, mission_reward_item 0, mission_indicator 0, " +
                "mission_scene_binding 0, mission_evidence 0, mission_scenario 0, mission_scenario_step 0, mission_channel_policy 0, " +
                "mission_repeat_policy 0, mission_spawn 0, mission_spawn_group 0, mission_area 0, creature 0, creature_stat 0, spawnpool 0, kept 9";
            // Fix E (a later migration) adds an evidence row on 1287, 1289 and 1291 while it is up (all migrations); fix F one on
            // each of the four and 1287's marker on its own Atta pool; fix G one more on 1287.
            void AllUp(bool fixE = false)
            {
                Assert.IsTrue(missions.All(Has));
                Assert.AreEqual(fixE ? up.Replace("mission_evidence 12", "mission_evidence 20").Replace("mission_indicator 15", "mission_indicator 16") : up, All());
                Assert.AreEqual(AshenDesignerHidesNpcsV1.IdaAt, IdaAt(), "Ida moved");
            }

            AllUp(fixE: true);
            world.GetService<IMigrator>().Migrate(Previous(world, DesignerHides));
            world.ChangeTracker.Clear();
            Assert.IsFalse(missions.Any(Has));
            Assert.AreEqual(down, All());
            Assert.AreEqual(upstream, IdaAt(), "Ida back on upstream's spot, exact");
            Assert.AreEqual(others, Others(), "no other Ashen pool changes");
            Assert.IsTrue(MissionIds.Take(32).All(Has), "sets 1-9 stay");
            foreach (var sql in AshenDesignerHidesNpcsV1.Down())
                world.Database.ExecuteSqlRaw(CrucibleSql.Guard(sql));
            Assert.AreEqual(down, All(), "a second Down changes nothing");
            Assert.AreEqual(upstream, IdaAt(), "a second Down leaves Ida on upstream's spot");

            world.GetService<IMigrator>().Migrate(DesignerHides);
            world.ChangeTracker.Clear();
            AllUp();
            foreach (var sql in AshenDesignerHidesNpcsV1.Up())
                world.Database.ExecuteSqlRaw(CrucibleSql.Guard(sql));
            AllUp();
            Assert.AreEqual(others, Others(), "no other Ashen pool changes");
            // The guard: a pool that stands neither where upstream nor where set 10 put it is left alone by both directions.
            world.Database.ExecuteSqlRaw($"UPDATE spawnpool SET pos_x = -150.5 WHERE id = {Ida};");
            foreach (var sql in AshenDesignerHidesNpcsV1.Down().Take(1).Concat(AshenDesignerHidesNpcsV1.Up().TakeLast(1)))
                world.Database.ExecuteSqlRaw(sql);
            Assert.AreEqual(-150.5, IdaAt().X, "a pool moved elsewhere is not touched");
            world.Database.ExecuteSqlRaw(System.FormattableString.Invariant($"UPDATE spawnpool SET pos_x = {AshenDesignerHidesNpcsV1.IdaAt.X:R} WHERE id = {Ida};"));
            AllUp();
        }

        /// <summary>
        /// Ashen_white_oasis takes out the four White Oasis missions and Meeks and Hale (creature, stats, look, package, pool), and
        /// puts Fontecilla's pool back on upstream's spot (the control point's console), exact; the rows it copies or uses
        /// (Harris, Emmons, Daris Linier, Fontecilla, Ulovik) stay, no other Ashen pool changes, sets 1-10 stay. Up again, all of it
        /// is back. Every table the migration writes is counted (set 11's ids and missions only); a second Down and Up (the SQL
        /// run again) change nothing, and Fontecilla's update leaves a pool that stands elsewhere alone.
        /// </summary>
        [TestMethod]
        public void WhiteOasisRollsBackWithItsMigration()
        {
            var missions = new[] { AshenAnalyzingHarvestersV1.MissionId, AshenMurkyWatersV1.MissionId, AshenPryingEyesV1.MissionId, AshenGasShortagesV1.MissionId };
            using var harness = WildernessRuntimeTestHarness.Create();
            var world = harness.World;
            bool Has(uint mission) => world.MissionContentDefinitionEntries.AsNoTracking()
                .Any(entry => entry.MissionId == mission && entry.ContentRevision == AshenMissionDataV1.Revision);
            int Count(string sql) => world.Database.SqlQueryRaw<int>(sql).AsEnumerable().Single();
            const string ids = "1295, 1298, 1303, 1356";
            const string npcs = "790400, 790401";
            // Every table the migration writes (its Up and its Down: AshenMissionDataV1.Remove clears all eighteen mission tables).
            var tables = new[]
            {
                "mission_content_definition", "mission_objective_definition", "mission_objective_transition", "mission_trigger", "mission_action",
                "mission_prerequisite", "mission_reward_definition", "mission_reward_item", "mission_indicator", "mission_scene_binding", "mission_evidence",
                "mission_scenario", "mission_scenario_step", "mission_channel_policy", "mission_repeat_policy", "mission_spawn", "mission_spawn_group", "mission_area"
            };
            string All() => string.Join(", ", tables.Select(table => $"{table} {Count($"SELECT COUNT(*) AS Value FROM {table} WHERE content_revision = 'ashen_1_16_5' AND mission_id IN ({ids})")}")
                .Concat(new[] { "creature", "creature_stat", "creature_appearance", "npc_package", "spawnpool" }
                    .Select(table => $"{table} {Count($"SELECT COUNT(*) AS Value FROM {table} WHERE id IN ({npcs})")}"))
                .Append($"kept {Count($"SELECT COUNT(*) AS Value FROM creature WHERE id IN ({Harris}, {Emmons}, {DarisLinier}, {Fontecilla}, {Ulovik})")}"));
            (double X, double Y, double Z) FontecillaAt()
            {
                var row = world.SpawnPoolEntries.AsNoTracking().Single(entry => entry.Id == Fontecilla);
                return (row.PosX, row.PosY, row.PosZ);
            }
            var later = PoolsOfLaterMigrations(world, WhiteOasis);
            string Others() => string.Join("; ", world.SpawnPoolEntries.AsNoTracking()
                .Where(entry => entry.MapContextId == AshenMissionDataV1.MapContextId && entry.Id != Fontecilla && !(entry.Id >= 790400 && entry.Id <= 790439) && !later.Contains(entry.Id))
                .OrderBy(entry => entry.Id).Select(entry => new { entry.Id, entry.PosX, entry.PosY, entry.PosZ, entry.Radius, entry.Creature1Id }).AsEnumerable().Select(entry => entry.ToString()));
            var others = Others();
            var upstream = (-511.1000061035156, 281.76, 164.39999389648438);
            // 1295: four objectives, transitions and triggers, one reward action, a scenario of four steps; 1298: four objectives
            // (two choice transitions on 1, one each on 2 and 3), four talk triggers, ten actions (eight of the choices, two
            // rewards), the prerequisite; 1303: one of each; 1356: three objectives, transitions, triggers and reward actions, a
            // scenario of three steps. Each one reward (four items) and three evidence rows; markers 4, 3, 1, 3; scenes for
            // 1295, 1298 and 1356. No channel or repeat policy, spawns or areas. Meeks and Hale: a creature, its stats, six look
            // rows (Harris', Emmons'), a package and a pool each.
            const string up = "mission_content_definition 4, mission_objective_definition 12, mission_objective_transition 12, mission_trigger 12, " +
                "mission_action 15, mission_prerequisite 1, mission_reward_definition 4, mission_reward_item 16, mission_indicator 11, " +
                "mission_scene_binding 3, mission_evidence 12, mission_scenario 2, mission_scenario_step 7, mission_channel_policy 0, " +
                "mission_repeat_policy 0, mission_spawn 0, mission_spawn_group 0, mission_area 0, creature 2, creature_stat 2, " +
                "creature_appearance 12, npc_package 2, spawnpool 2, kept 5";
            const string down = "mission_content_definition 0, mission_objective_definition 0, mission_objective_transition 0, mission_trigger 0, " +
                "mission_action 0, mission_prerequisite 0, mission_reward_definition 0, mission_reward_item 0, mission_indicator 0, " +
                "mission_scene_binding 0, mission_evidence 0, mission_scenario 0, mission_scenario_step 0, mission_channel_policy 0, " +
                "mission_repeat_policy 0, mission_spawn 0, mission_spawn_group 0, mission_area 0, creature 0, creature_stat 0, " +
                "creature_appearance 0, npc_package 0, spawnpool 0, kept 5";
            // Fix F (a later migration) adds an evidence row on 1356 while it is up (all migrations).
            void AllUp(bool fixF = false)
            {
                Assert.IsTrue(missions.All(Has));
                Assert.AreEqual(fixF ? up.Replace("mission_evidence 12", "mission_evidence 13") : up, All());
                Assert.AreEqual(AshenWhiteOasisNpcsV1.FontecillaAt, FontecillaAt(), "Fontecilla moved");
            }

            AllUp(fixF: true);
            world.GetService<IMigrator>().Migrate(Previous(world, WhiteOasis));
            world.ChangeTracker.Clear();
            Assert.IsFalse(missions.Any(Has));
            Assert.AreEqual(down, All());
            Assert.AreEqual(upstream, FontecillaAt(), "Fontecilla back on upstream's spot, exact");
            Assert.AreEqual(others, Others(), "no other Ashen pool changes");
            Assert.IsTrue(MissionIds.Take(36).All(Has), "sets 1-10 stay");
            foreach (var sql in AshenWhiteOasisNpcsV1.Down())
                world.Database.ExecuteSqlRaw(CrucibleSql.Guard(sql));
            Assert.AreEqual(down, All(), "a second Down changes nothing");
            Assert.AreEqual(upstream, FontecillaAt(), "a second Down leaves Fontecilla on upstream's spot");

            world.GetService<IMigrator>().Migrate(WhiteOasis);
            world.ChangeTracker.Clear();
            AllUp();
            foreach (var sql in AshenWhiteOasisNpcsV1.Up())
                world.Database.ExecuteSqlRaw(CrucibleSql.Guard(sql));
            AllUp();
            Assert.AreEqual(others, Others(), "no other Ashen pool changes");
            // The guard: a pool that stands neither where upstream nor where set 11 put it is left alone by both directions.
            world.Database.ExecuteSqlRaw($"UPDATE spawnpool SET pos_x = -530.5 WHERE id = {Fontecilla};");
            foreach (var sql in AshenWhiteOasisNpcsV1.Down().Take(1).Concat(AshenWhiteOasisNpcsV1.Up().TakeLast(1)))
                world.Database.ExecuteSqlRaw(sql);
            Assert.AreEqual(-530.5, FontecillaAt().X, "a pool moved elsewhere is not touched");
            world.Database.ExecuteSqlRaw(System.FormattableString.Invariant($"UPDATE spawnpool SET pos_x = {AshenWhiteOasisNpcsV1.FontecillaAt.X:R} WHERE id = {Fontecilla};"));
            AllUp();
        }

        /// <summary>
        /// Ashen_murky_waters takes out the three missions of Murky Waters' sides and the two ridge Lasher pools; the rows they
        /// use (set 10's Lasher and Granitour, set 11's Meeks and Hale, Murky Waters) stay. Every table the migration writes is
        /// counted (set 12's ids and missions only: it updates nothing). Nothing else changes: the other Ashen pools, missions and
        /// markers as a database migrated only up to Ashen_murky_waters holds them (a later migration may follow and is left out).
        /// Up again, all of it is back; a second Down and Up (the SQL run again) change nothing.
        /// </summary>
        [TestMethod]
        public void MurkyWatersRollsBackWithItsMigration()
        {
            var missions = new[] { AshenWaterPurificationV1.MissionId, AshenNeurotoxinsV1.MissionId, AshenPoisoningTheWellwaterV1.MissionId };
            // At its migration (a later set's rows are not this one's "nothing else").
            using var harness = WildernessRuntimeTestHarness.Create(targetWorldMigration: MurkyWaters);
            var world = harness.World;
            bool Has(uint mission) => world.MissionContentDefinitionEntries.AsNoTracking()
                .Any(entry => entry.MissionId == mission && entry.ContentRevision == AshenMissionDataV1.Revision);
            int Count(string sql) => world.Database.SqlQueryRaw<int>(sql).AsEnumerable().Single();
            const string ids = "1299, 1300, 1302";
            const string pools = "790440, 790441";
            // Every table the migration writes (its Up and its Down: AshenMissionDataV1.Remove clears all eighteen mission tables).
            var tables = new[]
            {
                "mission_content_definition", "mission_objective_definition", "mission_objective_transition", "mission_trigger", "mission_action",
                "mission_prerequisite", "mission_reward_definition", "mission_reward_item", "mission_indicator", "mission_scene_binding", "mission_evidence",
                "mission_scenario", "mission_scenario_step", "mission_channel_policy", "mission_repeat_policy", "mission_spawn", "mission_spawn_group", "mission_area"
            };
            string All() => string.Join(", ", tables.Select(table => $"{table} {Count($"SELECT COUNT(*) AS Value FROM {table} WHERE content_revision = 'ashen_1_16_5' AND mission_id IN ({ids})")}")
                .Append($"spawnpool {Count($"SELECT COUNT(*) AS Value FROM spawnpool WHERE id IN ({pools})")}")
                .Append($"kept {Count($"SELECT COUNT(*) AS Value FROM creature WHERE id IN ({Lasher}, {Granitour}, {Meeks}, {Hale})")}")
                .Append($"murky {Count("SELECT COUNT(*) AS Value FROM mission_content_definition WHERE content_revision = 'ashen_1_16_5' AND mission_id = 1298")}"));
            static string Others(DbContext db) => string.Join("; ", db.Set<SpawnPoolEntry>().AsNoTracking()
                    // Fix F (a later migration) adds 1287's Atta pool and its marker: not set 12's.
                    .Where(entry => entry.MapContextId == AshenMissionDataV1.MapContextId && entry.Id != 790440 && entry.Id != 790441 && entry.Id != AshenFixFV1.AttaPool).OrderBy(entry => entry.Id).AsEnumerable()
                    .Select(entry => System.FormattableString.Invariant($"{entry.Id}:{entry.PosX:R},{entry.PosY:R},{entry.PosZ:R},{entry.Radius:R},{entry.Creature1Id},{entry.Creature1MaxCount}")))
                + " | " + string.Join("; ", db.Set<MissionContentDefinitionEntry>().AsNoTracking()
                    .Where(entry => entry.MissionId != 1299 && entry.MissionId != 1300 && entry.MissionId != 1302).OrderBy(entry => entry.MissionId).ThenBy(entry => entry.ContentRevision)
                    .AsEnumerable().Select(entry => $"{entry.MissionId}/{entry.ContentRevision}"))
                + " | " + string.Join("; ", db.Set<MissionIndicatorEntry>().AsNoTracking()
                    .Where(entry => entry.ContentRevision == AshenMissionDataV1.Revision && entry.MissionId != 1299 && entry.MissionId != 1300 && entry.MissionId != 1302)
                    .OrderBy(entry => entry.MissionId).ThenBy(entry => entry.ObjectiveId).ThenBy(entry => entry.IndicatorId).AsEnumerable()
                    .Where(entry => !(entry.MissionId == AshenDesignerJewelryV1.MissionId && entry.IndicatorId == AshenFixFV1.MarkerIndicator))
                    .Select(entry => System.FormattableString.Invariant($"{entry.MissionId}.{entry.ObjectiveId}/{entry.IndicatorId}:{entry.PosX:R},{entry.PosZ:R}")));
            string others;
            using (var atMurky = WildernessRuntimeTestHarness.Create(targetWorldMigration: MurkyWaters))
                others = Others(atMurky.World);
            // 1299 and 1300: one objective, transition, trigger and reward action each, two prerequisites (Murky Waters, the
            // flag); 1302: four objectives, transitions and triggers, one reward action, a scenario of four steps, two
            // prerequisites (Neurotoxins, the flag). Each one reward (four items) and three evidence rows; markers 2, 4, 4; three
            // scenes. No channel or repeat policy, spawns or areas. Two pools.
            const string up = "mission_content_definition 3, mission_objective_definition 6, mission_objective_transition 6, mission_trigger 6, " +
                "mission_action 3, mission_prerequisite 6, mission_reward_definition 3, mission_reward_item 12, mission_indicator 10, " +
                "mission_scene_binding 3, mission_evidence 9, mission_scenario 1, mission_scenario_step 4, mission_channel_policy 0, " +
                "mission_repeat_policy 0, mission_spawn 0, mission_spawn_group 0, mission_area 0, spawnpool 2, kept 4, murky 1";
            const string down = "mission_content_definition 0, mission_objective_definition 0, mission_objective_transition 0, mission_trigger 0, " +
                "mission_action 0, mission_prerequisite 0, mission_reward_definition 0, mission_reward_item 0, mission_indicator 0, " +
                "mission_scene_binding 0, mission_evidence 0, mission_scenario 0, mission_scenario_step 0, mission_channel_policy 0, " +
                "mission_repeat_policy 0, mission_spawn 0, mission_spawn_group 0, mission_area 0, spawnpool 0, kept 4, murky 1";
            void AllUp(string when)
            {
                Assert.IsTrue(missions.All(Has), $"{when}: the three missions");
                Assert.AreEqual(up, All(), when);
                Assert.AreEqual(others, Others(world), $"{when}: nothing else changes");
            }

            AllUp("at its migration");
            world.GetService<IMigrator>().Migrate(Previous(world, MurkyWaters));
            world.ChangeTracker.Clear();
            Assert.IsFalse(missions.Any(Has));
            Assert.AreEqual(down, All());
            Assert.AreEqual(others, Others(world), "Down: nothing else changes");
            Assert.IsTrue(MissionIds.Take(40).All(Has), "sets 1-11 stay");
            foreach (var sql in AshenMurkyWatersNpcsV1.Down())
                world.Database.ExecuteSqlRaw(CrucibleSql.Guard(sql));
            Assert.AreEqual(down, All(), "a second Down changes nothing");

            world.GetService<IMigrator>().Migrate(MurkyWaters);
            world.ChangeTracker.Clear();
            AllUp("Up again");
            foreach (var sql in AshenMurkyWatersNpcsV1.Up())
                world.Database.ExecuteSqlRaw(CrucibleSql.Guard(sql));
            AllUp("a second Up");
        }

        /// <summary>
        /// Ashen_indra_pass takes out the three Indra Pass missions and Odesha and Endala (creature, stats, look, package, pool);
        /// the rows they copy or use (Raji Lasat, Naxveik) stay; it updates nothing. Every table the migration writes is counted
        /// (set 13's ids and missions only). Nothing else changes: the other Ashen pools, missions and markers as a database
        /// migrated only up to Ashen_indra_pass holds them (a later migration may follow and is left out). Up again, all of it is
        /// back; a second Down and Up (the SQL run again) change nothing.
        /// </summary>
        [TestMethod]
        public void IndraPassRollsBackWithItsMigration()
        {
            var missions = new[] { AshenMissingScientistsV1.MissionId, AshenGodOfStormsV1.MissionId, AshenBondsOfBloodV1.MissionId };
            // At its migration (a later set's rows are not this one's "nothing else").
            using var harness = WildernessRuntimeTestHarness.Create(targetWorldMigration: IndraPass);
            var world = harness.World;
            bool Has(uint mission) => world.MissionContentDefinitionEntries.AsNoTracking()
                .Any(entry => entry.MissionId == mission && entry.ContentRevision == AshenMissionDataV1.Revision);
            int Count(string sql) => world.Database.SqlQueryRaw<int>(sql).AsEnumerable().Single();
            const string ids = "1280, 1281, 1282";
            const string npcs = "790480, 790481";
            // Every table the migration writes (its Up and its Down: AshenMissionDataV1.Remove clears all eighteen mission tables).
            var tables = new[]
            {
                "mission_content_definition", "mission_objective_definition", "mission_objective_transition", "mission_trigger", "mission_action",
                "mission_prerequisite", "mission_reward_definition", "mission_reward_item", "mission_indicator", "mission_scene_binding", "mission_evidence",
                "mission_scenario", "mission_scenario_step", "mission_channel_policy", "mission_repeat_policy", "mission_spawn", "mission_spawn_group", "mission_area"
            };
            string All() => string.Join(", ", tables.Select(table => $"{table} {Count($"SELECT COUNT(*) AS Value FROM {table} WHERE content_revision = 'ashen_1_16_5' AND mission_id IN ({ids})")}")
                .Concat(new[] { "creature", "creature_stat", "creature_appearance", "npc_package", "spawnpool" }
                    .Select(table => $"{table} {Count($"SELECT COUNT(*) AS Value FROM {table} WHERE id IN ({npcs})")}"))
                .Append($"kept {Count($"SELECT COUNT(*) AS Value FROM creature WHERE id IN ({AshenIndraPassNpcsV1.Raji}, {Naxveik})")}"));
            static string Others(DbContext db) => string.Join("; ", db.Set<SpawnPoolEntry>().AsNoTracking()
                    .Where(entry => entry.MapContextId == AshenMissionDataV1.MapContextId && entry.Id != 790480 && entry.Id != 790481).OrderBy(entry => entry.Id).AsEnumerable()
                    .Select(entry => System.FormattableString.Invariant($"{entry.Id}:{entry.PosX:R},{entry.PosY:R},{entry.PosZ:R},{entry.Radius:R},{entry.Creature1Id},{entry.Creature1MaxCount}")))
                + " | " + string.Join("; ", db.Set<MissionContentDefinitionEntry>().AsNoTracking()
                    .Where(entry => entry.MissionId != 1280 && entry.MissionId != 1281 && entry.MissionId != 1282).OrderBy(entry => entry.MissionId).ThenBy(entry => entry.ContentRevision)
                    .AsEnumerable().Select(entry => $"{entry.MissionId}/{entry.ContentRevision}"))
                + " | " + string.Join("; ", db.Set<MissionIndicatorEntry>().AsNoTracking()
                    .Where(entry => entry.ContentRevision == AshenMissionDataV1.Revision && entry.MissionId != 1280 && entry.MissionId != 1281 && entry.MissionId != 1282)
                    .OrderBy(entry => entry.MissionId).ThenBy(entry => entry.ObjectiveId).ThenBy(entry => entry.IndicatorId).AsEnumerable()
                    .Select(entry => System.FormattableString.Invariant($"{entry.MissionId}.{entry.ObjectiveId}/{entry.IndicatorId}:{entry.PosX:R},{entry.PosZ:R}")));
            string others;
            using (var atIndra = WildernessRuntimeTestHarness.Create(targetWorldMigration: IndraPass))
                others = Others(atIndra.World);
            // 1280: one objective, transition, trigger and reward action, a scenario of one step. 1281: eleven objectives, each
            // with its completing and its failing (closed by 1282) transition; ten resistor triggers, ten counting triggers on 11
            // and eleven failing ones; a reward and a closing action on 11; scenarios 1 and 2 of ten steps, 19 (closed) of one, 20
            // (close the other) of none; the prerequisite. 1282: three objectives, two transitions each; the body's, the talk's and
            // 3's ObjectiveState triggers and three failing ones; reveal, activate, reward and closing actions; scenarios 1 and 19
            // of one step, 20 of none; the prerequisite. Each one reward (four items) and three evidence rows; markers 1, 10, 2;
            // three scenes. No channel or repeat policy, spawns or areas. Odesha and Endala: a creature, its stats, five look rows
            // (Raji Lasat's), a package and a pool each.
            const string up = "mission_content_definition 3, mission_objective_definition 15, mission_objective_transition 29, mission_trigger 38, " +
                "mission_action 7, mission_prerequisite 2, mission_reward_definition 3, mission_reward_item 12, mission_indicator 13, " +
                "mission_scene_binding 3, mission_evidence 9, mission_scenario 8, mission_scenario_step 24, mission_channel_policy 0, " +
                "mission_repeat_policy 0, mission_spawn 0, mission_spawn_group 0, mission_area 0, creature 2, creature_stat 2, " +
                "creature_appearance 10, npc_package 2, spawnpool 2, kept 2";
            const string down = "mission_content_definition 0, mission_objective_definition 0, mission_objective_transition 0, mission_trigger 0, " +
                "mission_action 0, mission_prerequisite 0, mission_reward_definition 0, mission_reward_item 0, mission_indicator 0, " +
                "mission_scene_binding 0, mission_evidence 0, mission_scenario 0, mission_scenario_step 0, mission_channel_policy 0, " +
                "mission_repeat_policy 0, mission_spawn 0, mission_spawn_group 0, mission_area 0, creature 0, creature_stat 0, " +
                "creature_appearance 0, npc_package 0, spawnpool 0, kept 2";
            void AllUp(string when)
            {
                Assert.IsTrue(missions.All(Has), $"{when}: the three missions");
                Assert.AreEqual(up, All(), when);
                Assert.AreEqual(others, Others(world), $"{when}: nothing else changes");
            }

            AllUp("at its migration");
            world.GetService<IMigrator>().Migrate(Previous(world, IndraPass));
            world.ChangeTracker.Clear();
            Assert.IsFalse(missions.Any(Has));
            Assert.AreEqual(down, All());
            Assert.AreEqual(others, Others(world), "Down: nothing else changes");
            Assert.IsTrue(MissionIds.Take(43).All(Has), "sets 1-12 stay");
            foreach (var sql in AshenIndraPassNpcsV1.Down())
                world.Database.ExecuteSqlRaw(CrucibleSql.Guard(sql));
            Assert.AreEqual(down, All(), "a second Down changes nothing");

            world.GetService<IMigrator>().Migrate(IndraPass);
            world.ChangeTracker.Clear();
            AllUp("Up again");
            foreach (var sql in AshenIndraPassNpcsV1.Down())
                world.Database.ExecuteSqlRaw(CrucibleSql.Guard(sql));
            foreach (var sql in AshenIndraPassNpcsV1.Up())
                world.Database.ExecuteSqlRaw(CrucibleSql.Guard(sql));
            AllUp("the NPC rows run again");
        }

        /// <summary>
        /// Ashen_ashoka takes out Echoes in the Sand (on the Crucible's revision, as the 1218 chain it ends), Supply Logistics and
        /// Precious Tools, and Chase, the two researchers and Jirex (creature, stats, look, package, pools); the rows they copy
        /// (Harris, Raji, Tiernan, Mitecht) stay; it updates nothing. Every table the migration writes is counted (set 14's ids and
        /// missions only). Nothing else changes: the other Ashen pools, missions and markers as a database migrated only up to
        /// Ashen_ashoka holds them. Up again, all of it is back; the NPC SQL run again changes nothing.
        /// </summary>
        [TestMethod]
        public void AshokaRollsBackWithItsMigration()
        {
            var missions = new[] { (AshenEchoesInTheSandV1.MissionId, AshenEchoesInTheSandV1.Revision), (AshenSupplyLogisticsV1.MissionId, AshenMissionDataV1.Revision),
                (AshenPreciousToolsV1.MissionId, AshenMissionDataV1.Revision) };
            // At its migration (a later set's rows are not this one's "nothing else").
            using var harness = WildernessRuntimeTestHarness.Create(targetWorldMigration: Ashoka);
            var world = harness.World;
            bool Has((uint Mission, string Revision) mission) => world.MissionContentDefinitionEntries.AsNoTracking()
                .Any(entry => entry.MissionId == mission.Mission && entry.ContentRevision == mission.Revision);
            int Count(string sql) => world.Database.SqlQueryRaw<int>(sql).AsEnumerable().Single();
            const string ids = "(mission_id = 1736 AND content_revision = 'crucible_1_16_5' OR mission_id IN (1358, 1286) AND content_revision = 'ashen_1_16_5')";
            const string npcs = "790520, 790521, 790522, 790523, 790524";
            var tables = new[]
            {
                "mission_content_definition", "mission_objective_definition", "mission_objective_transition", "mission_trigger", "mission_action",
                "mission_prerequisite", "mission_reward_definition", "mission_reward_item", "mission_indicator", "mission_scene_binding", "mission_evidence",
                "mission_scenario", "mission_scenario_step", "mission_channel_policy", "mission_repeat_policy", "mission_spawn", "mission_spawn_group", "mission_area"
            };
            string All() => string.Join(", ", tables.Select(table => $"{table} {Count($"SELECT COUNT(*) AS Value FROM {table} WHERE {ids}")}")
                .Concat(new[] { "creature", "creature_stat", "creature_appearance", "npc_package", "spawnpool" }
                    .Select(table => $"{table} {Count($"SELECT COUNT(*) AS Value FROM {table} WHERE id IN ({npcs})")}"))
                .Append($"kept {Count($"SELECT COUNT(*) AS Value FROM creature WHERE id IN ({AshenAshokaNpcsV1.Harris}, {AshenAshokaNpcsV1.Raji}, {AshenAshokaNpcsV1.Tiernan}, {AshenAshokaNpcsV1.Mitecht})")}"));
            static string Others(DbContext db) => string.Join("; ", db.Set<SpawnPoolEntry>().AsNoTracking()
                    .Where(entry => entry.MapContextId == AshenMissionDataV1.MapContextId && entry.Id != 790520 && entry.Id != 790524).OrderBy(entry => entry.Id).AsEnumerable()
                    .Select(entry => System.FormattableString.Invariant($"{entry.Id}:{entry.PosX:R},{entry.PosY:R},{entry.PosZ:R},{entry.Radius:R},{entry.Creature1Id},{entry.Creature1MaxCount}")))
                + " | " + string.Join("; ", db.Set<MissionContentDefinitionEntry>().AsNoTracking()
                    .Where(entry => entry.MissionId != 1736 && entry.MissionId != 1358 && entry.MissionId != 1286).OrderBy(entry => entry.MissionId).ThenBy(entry => entry.ContentRevision)
                    .AsEnumerable().Select(entry => $"{entry.MissionId}/{entry.ContentRevision}"))
                + " | " + string.Join("; ", db.Set<MissionIndicatorEntry>().AsNoTracking()
                    .Where(entry => (entry.ContentRevision == AshenMissionDataV1.Revision || entry.ContentRevision == CrucibleMissionDataV1.Revision)
                        && entry.MissionId != 1736 && entry.MissionId != 1358 && entry.MissionId != 1286)
                    .OrderBy(entry => entry.MissionId).ThenBy(entry => entry.ObjectiveId).ThenBy(entry => entry.IndicatorId).AsEnumerable()
                    .Select(entry => System.FormattableString.Invariant($"{entry.MissionId}.{entry.ObjectiveId}/{entry.IndicatorId}:{entry.PosX:R},{entry.PosZ:R}")));
            string others;
            using (var atAshoka = WildernessRuntimeTestHarness.Create(targetWorldMigration: Ashoka))
                others = Others(atAshoka.World);
            // 1736: six objectives (3-6 hidden), six transitions, two talks, an area trigger, two ObjectiveState triggers and
            // 6's two counting ones; four scenario starts, reveal, activate and reward actions; scenarios 1-5 (4 of two steps,
            // the first pair's deaths; the others none: data.sequence), one area, the prerequisite. 1358: five objectives, transitions and talks, five consume actions and the reward. 1286:
            // one objective, transition, item trigger and reward action. Each one reward (four items) and three evidence rows;
            // markers 2, 5, 1; three scenes. Chase, the researchers and Jirex: a creature and stats each; the look rows of the
            // rows copied (Harris' six, Raji's five, Tiernan's six; Jirex none); Chase's package; Chase's and Jirex's pools.
            const string up = "mission_content_definition 3, mission_objective_definition 12, mission_objective_transition 12, mission_trigger 13, " +
                "mission_action 14, mission_prerequisite 1, mission_reward_definition 3, mission_reward_item 12, mission_indicator 8, " +
                "mission_scene_binding 3, mission_evidence 9, mission_scenario 5, mission_scenario_step 2, mission_channel_policy 0, " +
                "mission_repeat_policy 0, mission_spawn 0, mission_spawn_group 0, mission_area 1, creature 4, creature_stat 4, " +
                "creature_appearance 17, npc_package 1, spawnpool 2, kept 4";
            const string down = "mission_content_definition 0, mission_objective_definition 0, mission_objective_transition 0, mission_trigger 0, " +
                "mission_action 0, mission_prerequisite 0, mission_reward_definition 0, mission_reward_item 0, mission_indicator 0, " +
                "mission_scene_binding 0, mission_evidence 0, mission_scenario 0, mission_scenario_step 0, mission_channel_policy 0, " +
                "mission_repeat_policy 0, mission_spawn 0, mission_spawn_group 0, mission_area 0, creature 0, creature_stat 0, " +
                "creature_appearance 0, npc_package 0, spawnpool 0, kept 4";
            void AllUp(string when)
            {
                Assert.IsTrue(missions.All(Has), $"{when}: the three missions");
                Assert.AreEqual(up, All(), when);
                Assert.AreEqual(others, Others(world), $"{when}: nothing else changes");
            }

            AllUp("at its migration");
            world.GetService<IMigrator>().Migrate(Previous(world, Ashoka));
            world.ChangeTracker.Clear();
            Assert.IsFalse(missions.Any(Has));
            Assert.AreEqual(down, All());
            Assert.AreEqual(others, Others(world), "Down: nothing else changes");
            Assert.IsTrue(MissionIds.Where(id => id is not (1736 or 1358 or 1286 or 1559 or 1360 or 1395 or 1447 or 1532 or 1451 or 1452 or 1473 or 1533 or 1535 or 1536 or 1537 or 1581 or 1500 or 1497 or 1498 or 1551 or 1499 or 1501)).All(id => Has((id, AshenMissionDataV1.Revision))), "sets 1-13 stay");
            foreach (var sql in AshenAshokaNpcsV1.Down())
                world.Database.ExecuteSqlRaw(CrucibleSql.Guard(sql));
            Assert.AreEqual(down, All(), "a second Down changes nothing");

            world.GetService<IMigrator>().Migrate(Ashoka);
            world.ChangeTracker.Clear();
            AllUp("Up again");
            foreach (var sql in AshenAshokaNpcsV1.Down())
                world.Database.ExecuteSqlRaw(CrucibleSql.Guard(sql));
            foreach (var sql in AshenAshokaNpcsV1.Up())
                world.Database.ExecuteSqlRaw(CrucibleSql.Guard(sql));
            AllUp("the NPC rows run again");
        }

        /// <summary>
        /// Ashen_indra_caverns takes out Indra Caverns, Threshold and Vindication, and Diren, Drof, Naimi and Kuutk (creature,
        /// stats, look, packages) with the set's six pools on Indra Caverns; the rows they copy (Harris, Mitecht) stay; it updates
        /// nothing. Every table the migration writes is counted (set 15's ids and missions only). Nothing else changes: the other
        /// pools, missions and markers as a database migrated only up to Ashen_indra_caverns holds them. Up again, all of it is
        /// back; the NPC SQL run again changes nothing.
        /// </summary>
        [TestMethod]
        public void IndraCavernsRollsBackWithItsMigration()
        {
            var missions = new[] { AshenIndraCavernsV1.MissionId, AshenThresholdV1.MissionId, AshenVindicationV1.MissionId };
            // At its migration (a later set's rows are not this one's "nothing else").
            using var harness = WildernessRuntimeTestHarness.Create(targetWorldMigration: IndraCaverns);
            var world = harness.World;
            bool Has(uint mission) => world.MissionContentDefinitionEntries.AsNoTracking()
                .Any(entry => entry.MissionId == mission && entry.ContentRevision == AshenMissionDataV1.Revision);
            int Count(string sql) => world.Database.SqlQueryRaw<int>(sql).AsEnumerable().Single();
            const string ids = "(mission_id IN (1559, 1360, 1395) AND content_revision = 'ashen_1_16_5')";
            const string npcs = "790560, 790561, 790562, 790563, 790564, 790565, 790566";
            var tables = new[]
            {
                "mission_content_definition", "mission_objective_definition", "mission_objective_transition", "mission_trigger", "mission_action",
                "mission_prerequisite", "mission_reward_definition", "mission_reward_item", "mission_indicator", "mission_scene_binding", "mission_evidence",
                "mission_scenario", "mission_scenario_step", "mission_channel_policy", "mission_repeat_policy", "mission_spawn", "mission_spawn_group", "mission_area"
            };
            string All() => string.Join(", ", tables.Select(table => $"{table} {Count($"SELECT COUNT(*) AS Value FROM {table} WHERE {ids}")}")
                .Concat(new[] { "creature", "creature_stat", "creature_appearance", "npc_package", "spawnpool" }
                    .Select(table => $"{table} {Count($"SELECT COUNT(*) AS Value FROM {table} WHERE id IN ({npcs})")}"))
                .Append($"kept {Count($"SELECT COUNT(*) AS Value FROM creature WHERE id IN ({AshenIndraFacilityNpcsV1.Harris}, {AshenIndraFacilityNpcsV1.Mitecht})")}"));
            static string Others(DbContext db) => string.Join("; ", db.Set<SpawnPoolEntry>().AsNoTracking()
                    .Where(entry => (entry.MapContextId == AshenMissionDataV1.MapContextId || entry.MapContextId == AshenIndraFacilityNpcsV1.Caverns) &&
                        (entry.Id < 790560 || entry.Id > 790566)).OrderBy(entry => entry.Id).AsEnumerable()
                    .Select(entry => System.FormattableString.Invariant($"{entry.Id}:{entry.PosX:R},{entry.PosY:R},{entry.PosZ:R},{entry.Radius:R},{entry.Creature1Id},{entry.Creature1MaxCount}")))
                + " | " + string.Join("; ", db.Set<MissionContentDefinitionEntry>().AsNoTracking()
                    .Where(entry => entry.MissionId != 1559 && entry.MissionId != 1360 && entry.MissionId != 1395).OrderBy(entry => entry.MissionId).ThenBy(entry => entry.ContentRevision)
                    .AsEnumerable().Select(entry => $"{entry.MissionId}/{entry.ContentRevision}"))
                + " | " + string.Join("; ", db.Set<MissionIndicatorEntry>().AsNoTracking()
                    .Where(entry => (entry.ContentRevision == AshenMissionDataV1.Revision || entry.ContentRevision == CrucibleMissionDataV1.Revision)
                        && entry.MissionId != 1559 && entry.MissionId != 1360 && entry.MissionId != 1395)
                    .OrderBy(entry => entry.MissionId).ThenBy(entry => entry.ObjectiveId).ThenBy(entry => entry.IndicatorId).AsEnumerable()
                    .Select(entry => System.FormattableString.Invariant($"{entry.MissionId}.{entry.ObjectiveId}/{entry.IndicatorId}:{entry.PosX:R},{entry.PosZ:R}")));
            string others;
            using (var atIndraCaverns = WildernessRuntimeTestHarness.Create(targetWorldMigration: IndraCaverns))
                others = Others(atIndraCaverns.World);
            // 1559: two objectives, transitions, area triggers (a map, a cylinder) and areas; reveal, activate and reward
            // actions. 1360: six objectives (4-6 hidden), five transitions (5 an aggregate); triggers: the search, the two pools,
            // the door, the opener's ObjectiveState, the talk; actions: the card given (1), the card taken and the door's opening (4),
            // reveal and activate, the package's consume and the reward; scenarios 1-3 (1 and 2 one step each). 1395: seven objectives (7 an aggregate, 8 hidden), six
            // transitions; triggers: the search, the kill, the cell, the talk, the crate, the hand; actions: the hand taken and the
            // cell's opening, reveal and activate twice, the reward; scenarios 1-4 (1-3 one step each). Each one prerequisite, one reward (four
            // items), three evidence rows; markers 2, 3, 5; three scenes. Diren, Drof, Naimi: a creature, stats, Harris' six look
            // rows and a package each; Kuutk a creature and stats; six pools.
            const string up = "mission_content_definition 3, mission_objective_definition 15, mission_objective_transition 13, mission_trigger 14, " +
                "mission_action 17, mission_prerequisite 3, mission_reward_definition 3, mission_reward_item 12, mission_indicator 10, " +
                "mission_scene_binding 3, mission_evidence 9, mission_scenario 7, mission_scenario_step 5, mission_channel_policy 0, " +
                "mission_repeat_policy 0, mission_spawn 0, mission_spawn_group 0, mission_area 2, creature 4, creature_stat 4, " +
                "creature_appearance 18, npc_package 3, spawnpool 6, kept 2";
            const string down = "mission_content_definition 0, mission_objective_definition 0, mission_objective_transition 0, mission_trigger 0, " +
                "mission_action 0, mission_prerequisite 0, mission_reward_definition 0, mission_reward_item 0, mission_indicator 0, " +
                "mission_scene_binding 0, mission_evidence 0, mission_scenario 0, mission_scenario_step 0, mission_channel_policy 0, " +
                "mission_repeat_policy 0, mission_spawn 0, mission_spawn_group 0, mission_area 0, creature 0, creature_stat 0, " +
                "creature_appearance 0, npc_package 0, spawnpool 0, kept 2";
            void AllUp(string when)
            {
                Assert.IsTrue(missions.All(Has), $"{when}: the three missions");
                Assert.AreEqual(up, All(), when);
                Assert.AreEqual(others, Others(world), $"{when}: nothing else changes");
            }

            AllUp("at its migration");
            world.GetService<IMigrator>().Migrate(Previous(world, IndraCaverns));
            world.ChangeTracker.Clear();
            Assert.IsFalse(missions.Any(Has));
            Assert.AreEqual(down, All());
            Assert.AreEqual(others, Others(world), "Down: nothing else changes");
            Assert.IsTrue(MissionIds.Where(id => !missions.Contains(id) && !AshenGothaPlantTests.Missions.Contains(id) && !AshenAvernusOutpostTests.Missions.Contains(id) &&
                !AshenAvernusHiveTests.Missions.Contains(id) && !AshenConscriptionEntryTests.Missions.Contains(id) &&
                !AshenConscriptionQueenTests.Missions.Contains(id)).All(Has), "sets 1-14 stay");
            foreach (var sql in AshenIndraFacilityNpcsV1.Down())
                world.Database.ExecuteSqlRaw(CrucibleSql.Guard(sql));
            Assert.AreEqual(down, All(), "a second Down changes nothing");

            world.GetService<IMigrator>().Migrate(IndraCaverns);
            world.ChangeTracker.Clear();
            AllUp("Up again");
            foreach (var sql in AshenIndraFacilityNpcsV1.Down())
                world.Database.ExecuteSqlRaw(CrucibleSql.Guard(sql));
            foreach (var sql in AshenIndraFacilityNpcsV1.Up())
                world.Database.ExecuteSqlRaw(CrucibleSql.Guard(sql));
            AllUp("the NPC rows run again");
        }

        /// <summary>
        /// A scene use object takes the use argument the client sends (QUEST-RULES): usabledata lookup[class][0], or 1 where it
        /// is None (Usable.__init__), written out here. 7847 UsableItemAccHumGpsBeacon sends 3 (as CrucibleMissionTests pins it);
        /// 1245's five spots carry the client's name 71 "Atta Sensor Beacon" (usablenameoverride LIGO_ASHEN_ATTA_BEACON). The
        /// human corpses UsableTreasureDispHumCorpseMale V01-V04 (24995, 7865, 24989, 24990) have None: they send 1; 1263's six
        /// fallen carry 63 "Fallen Soldier" (LIGO_ASHEN_FALLENSOLDIER). The bomb acceptor UsableItemAccHumBombV01 (7334) sends 1
        /// (usabledata lookup[7334][0]); 1352's and 1354's Detonation Devices keep the client's own name. Set 9: the human monitor
        /// UsableTwoStateHumComputerV01V06 (9478) sends 1 (usabledata lookup[9478][0], as PoolsTotalSlaughterV1), 1277's consoles
        /// named 65 "Radar Operations Console"; the Bane crate UsableTreasureDispBaneCrateV02 (7904) has None and sends 1, 1275's
        /// named 66 "Bane Dropship Salvage" (LIGO_ASHEN_RADARCRATE). Set 11: 1295's panels are the same monitor (9478, 1) named 69
        /// "Harvester Control Panel" (LIGO_ASHEN_MAJI_DIAGNOSTIC); 1356's Detonation Devices 7334 (1), the client's own name. Set
        /// 12: 1302's panels the same monitor (9478, 1), named 69 as 1295's (TaRapedia's 1302 "Harvester Control Panel"). Set 13:
        /// the Brann corpse UsableTreasureDispBrannCorpseMaleV01 (7928) has None and sends 1, 1280's named 67 "Corpse of Researcher
        /// Veyon" (LIGO_ASHEN_DEAD_ISHIN_VEYON), 1282's Maiek the client's own name (0: Maiek has none); the resistor
        /// UsableTwoStateBrannLightningResistorV01 (10196) has None and sends 1, named 68 "Indra Lightning Resistor"
        /// (LIGO_ASHEN_INDRA_RESISTOR). Set 15: the Brann door UsableDoorBrannCorridor04mV01 (10749), the Bane cell door
        /// UsableDoorBaneEnslaverCellV01 (24910) and the crate UsableTreasureDispHumCrateV01 (7861) have None and send 1; 1360's
        /// Bendal (24995) named 121, 1395's Aksrah (7865) 134 and crate 143 (usablenameoverride); the doors their own names. Fix
        /// I: 1360's facility door the entrance door UsableDoorBrannCorridorEntranceDoorV01 (23160), None, sends 1, its own name.
        /// Set 17: 1473's courier bag UsableItemDispCourierBag (21841) sends 3 (usabledata lookup[21841][0]); its two corpses
        /// (24995), the servers UsableTwoStateHumComputerDellXpsTowerV01 (26500) and the teleporter UsableTwoStateHumTeleporter
        /// (7895) have None and send 1; the teleporter named 52 "Universal Interior Teleporter" (usablenameoverride), the others
        /// their own names (the client has no 1473 name). Set 19: the charges UsableBombHumV01 (7870) and the teleporter
        /// UsableTwoStateBaneTeleporterV01 (8044) have None and send 1, their own names. Set 20: 1499's Cell Door Control
        /// (7468, AshenGothaPlantTests.SwitchNames: 140) sends 1; 1501's Life Support System UsableThreeStateAttaQueenControlDeviceV01
        /// (26423) has None and sends 1, named 153 "Life Support System" (usablenameoverride); fix V2: 1501's use is a console 7468
        /// (lookup[7468] (1, 0, 0, 0, 0.0): sends 1) named 153, the device a visual with no use.
        /// </summary>
        [TestMethod]
        public void EveryUsableSceneObjectTakesTheClientsUseArgument()
        {
            var clientUseArguments = new Dictionary<uint, uint>
            {
                [7847] = 3, [24995] = 1, [7865] = 1, [24989] = 1, [24990] = 1, [7334] = 1, [9478] = 1, [7904] = 1,
                [7928] = 1, [10196] = 1, [10749] = 1, [24910] = 1, [7861] = 1, [23160] = 1,
                [7468] = 1, [24805] = 1, [7870] = 1,
                [21841] = 3, [26500] = 1, [7895] = 1,
                [8044] = 1,
                [26423] = 1
            };
            var clientUseNames = new Dictionary<uint, uint>
            {
                [7847] = 71, [24995] = 63, [7865] = 63, [24989] = 63, [24990] = 63, [7334] = 0, [9478] = 65, [7904] = 66,
                [7928] = 67, [10196] = 68, [10749] = 0, [24910] = 0, [7861] = 143, [23160] = 0,
                [24805] = 0, [7870] = 0,
                [21841] = 0, [26500] = 0, [7895] = 52,
                [8044] = 0,
                [26423] = 153
            };
            using var harness = WildernessRuntimeTestHarness.Create();
            var used = 0;
            foreach (var scene in Scenes(harness))
                foreach (var actor in scene.Scene.Actors.Values.Where(actor => actor.UseAction != null))
                {
                    used++;
                    Assert.IsTrue(clientUseArguments.TryGetValue(actor.TemplateId, out var argument),
                        $"{scene.MissionId} {actor.Role}: class {actor.TemplateId} has no pinned client use argument");
                    Assert.AreEqual(argument, actor.UseAction.ActionArgId, $"{scene.MissionId} {actor.Role}: the client's argument");
                    var name = actor.TemplateId == 9478U && (scene.MissionId == 1295U || scene.MissionId == 1302U) ? 69U
                        : actor.TemplateId == 7928U && scene.MissionId == 1282U ? 0U
                        : actor.TemplateId == 24995U && scene.MissionId == 1360U ? 121U
                        : actor.TemplateId == 7865U && scene.MissionId == 1395U ? 134U
                        : actor.TemplateId == 24995U && scene.MissionId == 1473U ? 0U
                        : actor.TemplateId == 7468U && scene.MissionId == 1501U ? 153U
                        : actor.TemplateId == 7468U ? AshenGothaPlantTests.SwitchNames[actor.Role] : clientUseNames[actor.TemplateId];
                    Assert.AreEqual(name, actor.NameOverrideId, $"{scene.MissionId} {actor.Role}: the client's name");
                }
            Assert.AreEqual(5 + 6 + 6 + 3 + 1 + 4 + 4 + 3 + 4 + 1 + 10 + 1 + 2 + 6 + 3 + 6 + 6 + 8 + 1 + 2 + 1 + 1, used,
                "1245's five beacon spots, 1263's six fallen, 1352's six and 1354's three Detonation Devices, 1275's crate, 1277's four consoles, " +
                "1295's four panels, 1356's three Detonation Devices, 1302's four panels, 1280's Veyon, 1281's ten resistors, 1282's Maiek, " +
                "1360's Bendal and door, 1395's three doors, Aksrah, the crate and the cell, 1447's box and two teleporters, 1451's six switches, " +
                "1452's three consoles, the charge, the Teleportation Console and the teleporter, 1473's bag, two corpses, four servers and the teleporter, 1497's charge, 1498's teleporter and charge, 1499's Cell Door Control, 1501's Life Support System");
        }

        /// <summary>Where upstream puts a pool: its MissionNpcSpawnpoolPreloader row, with StandingHeights' correction.</summary>
        private static (double X, double Y, double Z) Upstream(uint pool)
        {
            var row = new Rasa.Services.Preloader.MissionNpcSpawnpoolPreloader().Rows().Single(values => System.Convert.ToUInt32(values[0]) == pool);
            (double X, double Y, double Z) at = (System.Convert.ToDouble(row[4]), System.Convert.ToDouble(row[5]), System.Convert.ToDouble(row[6]));
            foreach (var (id, x, y, z) in Rasa.Services.Preloader.StandingHeights.Corrected())
                if (id == pool)
                    at = (x ?? at.X, y, z ?? at.Z);
            return at;
        }

        /// <remarks>Echoes in the Sand (1736) included: an Ashen Desert scene on the Crucible's revision (the 1218 chain).</remarks>
        internal static List<(uint MissionId, Rasa.Missions.Content.MissionSceneDefinition Scene)> Scenes(WildernessRuntimeTestHarness harness) =>
            harness.World.Set<MissionSceneBindingEntry>().AsNoTracking().Where(entry => entry.ContentRevision == AshenMissionDataV1.Revision ||
                    entry.MissionId == AshenEchoesInTheSandV1.MissionId && entry.ContentRevision == CrucibleMissionDataV1.Revision)
                .ToList().Select(entry => (entry.MissionId, System.Text.Json.JsonSerializer.Deserialize<Rasa.Missions.Content.MissionSceneDefinition>(
                    entry.Bindings, Rasa.Missions.Content.MissionContentCodec.Options))).ToList();
    }
}
