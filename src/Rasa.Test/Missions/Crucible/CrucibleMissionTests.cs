using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Missions.Crucible
{
    using Rasa.Managers;
    using Rasa.Services.Preloader.Missions.Crucible;
    using Rasa.Services.Preloader.Missions.Marshes;
    using Rasa.Structures;
    using Rasa.Structures.World;
    using Rasa.Test.Missions.Wilderness;

    /// <summary>
    /// The Ligo Crucible missions (CrucibleMissionDataV1, revision crucible_1_16_5): set 1 at Outpost Intrepid, Corporal
    /// Shandy's The Last Men Standing (1142), Captain DeLisi's A Fort Called Intrepid (1150) and The Wire (1145), and Private
    /// Drake's The Departed (1143), with Shandy, Recon Sergeant Mullins and Top Khana Grupa Munit, and the upstream rows set 1
    /// uses moved and given the client's packages (Crucible_outpost_intrepid); set 2, the Grupa chiefs, Captain DeLisi's The
    /// Boss Of All Bosses (1161), Larai Grupa Vitto's The Protection Racket (1146), Top Khana Grupa Munit's Smoking Guns (1148)
    /// and Labbna Grupa Rigs' Junkyard Dogs (1149), with Bunko, Merchant Yeg and Roscoe, the upstream rows set 2 uses moved and
    /// the Kappa Grupa's office teleporters (Crucible_grupa_chiefs); set 3, wardens and lashers, Viddia Grupa Donal's Game
    /// Wardens (1147), Medical Technician Halprin's Whiplash (1157) and Kappa Grupa Lohen's Earning Respect (1170) and
    /// Business Proposal (1229), with Halprin and the new Warden Bot and Lasher pools (Crucible_wardens_and_lashers); set 4,
    /// the minefield and the salvage, Private Drake's Minesweeper (1232), Captain DeLisi's Funeral Derge (1241) and Labbna
    /// Sigal Haggus' Salvage Duty (1203) and More Salvage Duty (1204), with the new Smart Mine and Reconstructor Bot pools and
    /// Derge moved onto the rampart (Crucible_minefield_and_salvage); set 5, the Staal Sigals, Viddia Sigal Nema's Architects Of
    /// Destruction (1202), Top Khana Sigal Pico's Out Of Sight (1208) and Trista's Irreconcilable Differences (1185), with Nema,
    /// Pico and Trista, two more Reconstructor pools, the new Shield Drone pools and Preceptor Molitor's pool
    /// (Crucible_staal_sigals); set 6, the AWOL Camp, Lieutenant Sherer's Calendar Girl (1144), Intelligence Officer
    /// D'Ambrose's Stray Cats (1152), Awol Sergeant Breiburg's The Other Side Of The Coin (1153), Making Amends (1155) and Sweet
    /// Revenge (1205), with Cheung, Deirdre, Sherer and D'Ambrose moved (Crucible_awol_camp).
    ///
    /// Why these tests: the client's text ids, packages and item classes are written out here from the client tables, not
    /// taken from the data classes, so a wrong id fails; the NPC rows, rewards, prerequisites and markers are pinned; the
    /// migration rolls back to the one directly before it.
    ///
    /// [TestCategory]: none, as PlateauCampResistanceTests.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class CrucibleMissionTests
    {
        /// <summary>The Crucible missions the World migrations enable on the Crucible revision, for the tests that pin the enabled set.</summary>
        internal static readonly uint[] MissionIds =
        {
            CrucibleLastMenStandingV1.MissionId, CrucibleDepartedV1.MissionId, CrucibleFortCalledIntrepidV1.MissionId,
            CrucibleWireV1.MissionId, CrucibleBossOfAllBossesV1.MissionId, CrucibleProtectionRacketV1.MissionId,
            CrucibleSmokingGunsV1.MissionId, CrucibleJunkyardDogsV1.MissionId, CrucibleGameWardensV1.MissionId,
            CrucibleWhiplashV1.MissionId, CrucibleEarningRespectV1.MissionId, CrucibleBusinessProposalV1.MissionId,
            CrucibleMinesweeperV1.MissionId, CrucibleFuneralDergeV1.MissionId, CrucibleSalvageDutyV1.MissionId,
            CrucibleSalvageDutyV1.MoreMissionId, CrucibleArchitectsOfDestructionV1.MissionId, CrucibleOutOfSightV1.MissionId,
            CrucibleIrreconcilableDifferencesV1.MissionId, CrucibleCalendarGirlV1.MissionId, CrucibleStrayCatsV1.MissionId,
            CrucibleOtherSideOfTheCoinV1.MissionId, CrucibleMakingAmendsV1.MissionId, CrucibleMakingAmendsV1.RevengeMissionId,
            CrucibleHeadsWillRollV1.MissionId, CrucibleCarefulNegotiationV1.MissionId, CruciblePartOfTheBargainV1.MissionId,
            CrucibleNotMyJunkV1.MissionId, CruciblePrometheusRisingV1.MissionId, CrucibleConfirmedKillsV1.MissionId,
            CruciblePointOfImpactV1.MissionId, CrucibleItCameFromErdasV1.MissionId, CrucibleIronGiantsV1.MissionId,
            CrucibleWhosInChargeHereV1.MissionId, CrucibleBigKahunaV1.MissionId, CruciblePumpUpTheJamV1.MissionId,
            CrucibleStrikeZoneV1.MissionId, CrucibleSadisticSabotageV1.MissionId, CrucibleLostInTheAsylumV1.MissionId,
            CrucibleRehabilitationAtGunpointV1.MissionId, CrucibleGetToChaukasV1.MissionId, CrucibleControlTheBrainV1.MissionId,
            CrucibleFixTheFactoryV1.MissionId, CrucibleDarkGenesisV1.MissionId, CrucibleGeneMachineV1.MissionId,
            CrucibleDesignerGenesV1.MissionId, CrucibleGenomeSweetGenomeV1.MissionId, CrucibleLeakyPipesV1.MissionId,
            CrucibleFactoryFortificationV1.MissionId, CrucibleInformationIsKeyV1.MissionId,
            CrucibleBloodFireSteelV1.MissionId
        };

        /// <summary>
        /// Every visible objective has a marker (EveryVisibleObjectiveHasAMarker): The Last Men Standing's one at DeLisi; The
        /// Departed's one round the fallen (hidden 22 has none); A Fort Called Intrepid's one at the terminal; The Wire's five,
        /// one at each NPC; The Boss Of All Bosses' two, at Lohen and the lower office teleporter; The Protection Racket's six,
        /// one at each merchant, one at Roscoe and one at Vargo for his money; Smoking Guns' three, one at each receiver;
        /// Junkyard Dogs' three, one at each Flaregasher pool before the Staal Junkyard bridge; Game Wardens' nine, one per part
        /// on each Warden Bot pool (20271108000300); Whiplash's two, one at each Lasher pool; Earning Respect's eight, one at each Grupa for his
        /// talk and one for his token; Business Proposal's one at DeLisi; Minesweeper's three, one on each mine pool; Funeral
        /// Derge's one on Derge; Salvage Duty's ten and More Salvage Duty's fifteen, one per part on each of the five bot pools;
        /// Architects Of Destruction's four, one on each Reconstructor pool; Out Of Sight's three, one on each Shield Drone pool;
        /// Irreconcilable Differences' three, at Lohen (1 and 3) and on Molitor's pool (2); Calendar Girl's seven, one at the
        /// NPC of each visible objective (hidden 7 has none); Stray Cats' two, at Cheung and D'Ambrose; The Other Side Of The
        /// Coin's one at Cheung; Making Amends' and Sweet Revenge's three each, at Munit, DeLisi and Breiburg; Heads Will Roll's
        /// three, one per officer at the rock bridge; Careful Negotiation's three, at Kalamit, the Junkyard bridge road and Cirkin
        /// (in the Junkyard); Your Part of the Bargain's ten, in the Junkyard, one on each Technician pool, each Dolomite Block,
        /// the Ultra Reconstructor and Cirkin (hidden 5, 6 and 7 have none); Not My Junk's five, at the bridge road and Cirkin (1),
        /// Lamontix (2), the lockbox (4) and Cheung (3); Prometheus Rising's one at Keplinger; Confirmed Kills' two, in the west and
        /// north trenches of Khulago Base; Point Of Impact's one at Tiernan (hidden 2 has none); It Came From Erdas' two, at
        /// Tiernan (1) and at Botha on Ashen Desert (2); Iron Giants' three, one on each Juggernaut pool; Who's In Charge Here?'s
        /// two, at the terminal (1 and 2); The Big Kahuna's one on Ugoretz; Pump Up The Jam's six, one on each jamming tower;
        /// Strike Zone's three, one on each beacon spot; Sadistic Sabotage's two, before the Ward's door (Crucible) and at Sheira
        /// (Ward); Lost in the Asylum's three, one at each find (Ward); Rehabilitation at Gunpoint's fourteen, on Jaikev, on
        /// Parvatee, on each of the nine Incurable pools (Ward) and before the Ward's door for each objective (Crucible); Get to
        /// Chaukas' two, before the Chaukas door (Crucible) and at Brian (Chaukas); Control the Brain's eight, the Brain room's
        /// mouth, each of its three Claviger pools, the container and Brian at the Brain for 7, 5 and 6 (hidden 9, 10 and 11 have
        /// none; Chaukas); Fix the Factory's two, one on each switch (Chaukas); Dark Genesis' two, at Capriulo and Eugin; The Gene
        /// Machine's four, at Rigs and round the salvage yard for 1, 2 and 3 (hidden 4 has none); Designer Genes' four, on Molitor, the rock bridge trench, Ugoretz and Derge; Genome Sweet Genome's six, the
        /// tunnel pad and Skeev (7), Skeev (4), the terminal (5), Eugin (2) and Capriulo (3); Leaky Pipes' five, on Jaikev, Dhimand,
        /// the water system, Dhimand's repair place and Sheira (hidden 6 has none; Ward); Factory Fortification's one, on the factory
        /// room's floor (hidden 2 and 3 have none; Chaukas); Information is Key none (its one objective is hidden).
        /// </summary>
        internal const int Indicators = 8 + 2 + 6 + 3 + 3 + 9 + 2 + 8 + 1 + 3 + 1 + 10 + 15 + 4 + 3 + 3 + 7 + 2 + 1 + 3 + 3 + 3 + 3 + 10 + 5 + 1 + 2 + 1 + 2
            + 3 + 2 + 1 + 6 + 3 + 2 + 3 + 14 + 2 + 8 + 2 + 2 + 4 + 4 + 6 + 5 + 1
            + 6;   // set 15: Atali, Umank, Shanul, the north doors, the Headsman, the Sanctum door (1401)

        private const string OutpostIntrepid = "20271108000000_Crucible_outpost_intrepid";
        private const string GrupaChiefs = "20271108000100_Crucible_grupa_chiefs";
        private const string WardensAndLashers = "20271108000200_Crucible_wardens_and_lashers";
        private const string JunkyardDogsMarkers = "20271108000300_Crucible_junkyard_dogs_markers";
        private const string MinefieldAndSalvage = "20271108000400_Crucible_minefield_and_salvage";
        private const string StaalWalks = "20271108000500_Crucible_staal_walks";
        private const string StaalSigals = "20271108000600_Crucible_staal_sigals";
        private const string AwolCamp = "20271108000700_Crucible_awol_camp";
        private const string WardenPoolsAndVargo = "20271108000800_Crucible_warden_pools_and_vargo";
        private const string StaalJunkyard = "20271108000900_Crucible_staal_junkyard";
        private const string FixF = "20271108001000_Crucible_fix_f";
        private const string KardashAndPrometheus = "20271108001100_Crucible_kardash_and_prometheus";
        private const string FixG = "20271108001200_Crucible_fix_g";
        private const string PrometheusOutpost = "20271108001300_Crucible_prometheus_outpost";
        private const string KhulagoStrikes = "20271108001400_Crucible_khulago_strikes";
        private const string FixH = "20271108001500_Crucible_fix_h";
        private const string IncurablesWard = "20271108001600_Crucible_incurables_ward";
        private const string Chaukas = "20271108001700_Crucible_chaukas";
        private const string HybridArc = "20271108001800_Crucible_hybrid_arc";
        private const string FixI = "20271108001900_Crucible_fix_i";
        private const string IncurablesWardWings = "20271108002000_Crucible_incurables_ward_wings";
        private const string FixJ = "20271108002200_Crucible_fix_j";
        private const string ChaukasFactory = "20271108002300_Crucible_chaukas_factory";
        private const string BloodFireSteel = "20271108002400_Crucible_blood_fire_steel";

        private const uint Shandy = 770000, Mullins = 770001, Munit = 770002;
        private const uint DeLisi = 510019, Drake = 510034, Vitto = 510027, Donal = 510037, Rigs = 510025, DAmbrose = 510023;
        private const uint Bunko = 770003, Yeg = 770004, Roscoe = 770005;
        private const uint Lohen = 510024, Cugin = 510032, Vargo = 510033, Fiorella = 510022, Haggus = 510026, Melching = 510018;
        private const uint Halprin = 770006, WardenBot = 770007, Lasher = 770008;
        private const uint SmartMine = 770014, Reconstructor = 770015, Derge = 520013;
        private const uint Nema = 770019, Pico = 770020, Trista = 770021, ShieldDrone = 770022, Molitor = 770023;
        private const uint Cheung = 510015, Deirdre = 510016, Breiburg = 510017, Sherer = 510030;
        private const uint Cirkin = 770030, Krubb = 770031, Mungrel = 770032, Sedge = 770033, Kalamit = 770034, Lamontix = 770035;
        private const uint Technician = 770036, UltraReconstructor = 770038;
        private const uint Keplinger = 510031, Madias = 510029, Tiernan = 770039, Botha = 770040, Harris = 510008;
        private const uint Juggernaut = 770041, Ugoretz = 770042;
        private const uint Tiersky = 510021;
        private const uint Sheira = 510038, Tempa = 510028, Shanul = 770045, Dhimand = 770046, Brute = 770047, Slayer = 770048;
        private const uint Jaikev = 770049, Parvatee = 770050;
        private const uint Ruben = 770058, Brian = 770059;
        private const uint Stark = 770060, SkeevTalker = 770061, SkeevFighter = 770062, Capriulo = 510035, Eugin = 510036;
        private const uint RefittedBot = 770063;

        /// <summary>
        /// The migration directly before <paramref name="migration"/> (IMigrationsAssembly, ordinal order), the target that
        /// rolls <paramref name="migration"/> back: not a fixed id, so another migration slotted in between cannot be rolled
        /// back with it unseen.
        /// </summary>
        internal static string Previous(DbContext world, string migration)
        {
            var ids = world.GetService<IMigrationsAssembly>().Migrations.Keys.OrderBy(id => id, System.StringComparer.Ordinal).ToList();
            var index = ids.IndexOf(migration);
            Assert.IsGreaterThan(0, index, $"{migration} has a migration before it");
            return ids[index - 1];
        }

        [TestMethod]
        public void TheMissionsAreEnabledOperationalAndGivenByTheirNpcs()
        {
            var expected = new (uint Mission, uint? Giver, uint? Receiver, uint Level)[]
            {
                (1142, Shandy, DeLisi, 40), (1143, Drake, Drake, 40), (1150, DeLisi, DeLisi, 40), (1145, DeLisi, DeLisi, 40),
                (1161, DeLisi, Lohen, 40), (1146, Vitto, Vitto, 35), (1148, Munit, Munit, 35), (1149, Rigs, Rigs, 40),
                (1147, Donal, Donal, 35), (1157, Halprin, Halprin, 40), (1170, Lohen, Lohen, 35), (1229, Lohen, DeLisi, 35),
                (1232, Drake, Drake, 40), (1241, DeLisi, DeLisi, 40), (1203, Haggus, Haggus, 35), (1204, Haggus, Haggus, 35),
                (1202, Nema, Nema, 35), (1208, Pico, Pico, 35), (1185, Trista, Trista, 35),
                (1144, Sherer, Sherer, 40), (1152, DAmbrose, DAmbrose, 40), (1153, Breiburg, Breiburg, 40), (1155, Breiburg, Breiburg, 40),
                (1205, Breiburg, Breiburg, 40),
                (1206, Melching, Melching, 45), (1519, DeLisi, Cirkin, 40), (1529, Cirkin, Cirkin, 40), (1538, Cheung, Cheung, 40),
                (1210, DeLisi, Keplinger, 35), (1212, Madias, Madias, 35), (1217, Cheung, null, 40), (1218, null, Botha, 35),
                (1213, Madias, Madias, 35), (1215, Madias, Madias, 35), (1216, Madias, Madias, 35),
                (1224, Tiersky, Tiersky, 36), (1214, Keplinger, Keplinger, 35),
                (1445, Nema, Sheira, 35), (1444, Sheira, Sheira, 40), (1402, Tempa, Tempa, 35),
                (1577, Ruben, Brian, 40), (1521, Brian, Brian, 35), (1522, Brian, Brian, 35),
                (1856, Stark, Eugin, 36), (1857, Eugin, Eugin, 35), (1881, Eugin, Eugin, 35), (1899, Eugin, Capriulo, 35),
                (1398, Dhimand, Sheira, 40),
                (1523, Brian, Brian, 35), (1525, Brian, Ruben, 35),
                (1401, Shanul, Shanul, 40)
            };
            using var harness = WildernessRuntimeTestHarness.Create();
            var enabled = harness.World.MissionContentDefinitionEntries.AsNoTracking()
                .Where(entry => entry.Enabled && entry.ContentRevision == CrucibleMissionDataV1.Revision).ToArray();
            CollectionAssert.AreEquivalent(MissionIds, enabled.Select(entry => entry.MissionId).ToArray());
            foreach (var (mission, giver, receiver, level) in expected)
            {
                var entry = enabled.Single(row => row.MissionId == mission);
                Assert.AreEqual<uint?>(giver, entry.GiverId, $"{mission} giver");
                Assert.AreEqual<uint?>(receiver, entry.ReceiverId, $"{mission} receiver");
                Assert.AreEqual<uint?>(level, entry.Level, $"{mission} level");
                Assert.AreEqual<uint?>(1U, entry.CategoryId, "Battlefield (Crucible), missioncategorylanguage 1");
            }
            Assert.IsFalse(harness.Manager.LoadMissions().BlocksReadiness);
            foreach (var id in MissionIds)
                Assert.IsTrue(harness.Manager.LoadedMissions[id].IsOperational,
                    $"{id}: {harness.Manager.LoadedMissions[id].OperationalDiagnostic}");
        }

        /// <summary>
        /// The new NPCs: the client's names (creaturenamelanguage 9060, 9328, 9062) and packages (objectiveconversation 1145,6
        /// 1454 and 1145,3 1229; Shandy's server-only), friendly, standing still, with their own hit points, on the Crucible;
        /// the upstream rows set 1 uses at their new places, Rigs with the client's 1231, D'Ambrose with 1301 (not Munit's).
        /// </summary>
        [TestMethod]
        public void TheNpcsHaveTheClientsNamesAndPackages()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var world = harness.World;
            foreach (var (npc, name, package, cls, level) in new (uint, uint, uint, uint, uint)[]
            {
                (Shandy, 9060, 770000, 3846, 39), (Mullins, 9328, 1454, 3848, 40), (Munit, 9062, 1229, 7776, 40),
                (Bunko, 9070, 1224, 7776, 35), (Yeg, 9136, 1272, 7776, 35), (Halprin, 9114, 770006, 3846, 38),
                (Nema, 9069, 770019, 7776, 35), (Pico, 9063, 770020, 7776, 35), (Trista, 9072, 770021, 7776, 37)
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
                Assert.AreEqual((1993U, npc, (byte)1, (byte)1), (pool.MapContextId, pool.Creature1Id, pool.Creature1MinCount, pool.Creature1MaxCount), $"{npc} pool");
            }

            // The places (TaRapedia's for Shandy, DeLisi, Drake and Rigs; the quarters' region points for the Grupas).
            foreach (var (pool, x, z) in new (uint, double, double)[]
            {
                (Shandy, 987.07, 59.65), (Mullins, 292.49, 736.36), (Munit, -218, 228), (DeLisi, 952, 106), (Drake, 952.46, 110.89),
                (Vitto, -300, 320), (Donal, -216, 472), (Rigs, -392.77, 276.23),
                // Set 2: the two new merchants, Lohen in his office, Cugin on the Centrum floor, Fiorella and Haggus; Melching
                // where upstream has him. The Staal walks fix: Munit on the lower corridor; 20271108000800: Vargo clear of the crafting station.
                (Bunko, -197, 319), (Yeg, -153.7, 346.9), (Lohen, -215, 326), (Cugin, -174.3, 280.2), (Fiorella, 959, 111),
                (Haggus, -400, 270), (Vargo, -186, 270), (Melching, -945.9, 246.9),
                // Set 3: Halprin by the Outpost Intrepid hospital. Set 5: Nema by Donal, Pico by Munit, Trista by Lohen.
                (Halprin, 989, 108), (Nema, -219.5, 474), (Pico, -216, 231.5), (Trista, -211.5, 325.5),
                // Set 6: Cheung at the Awol Camp, Deirdre in the Staal Centrum, Sherer and D'Ambrose at Outpost Intrepid; Breiburg
                // where upstream has him.
                (Cheung, -953, 255), (Deirdre, -230, 324), (Sherer, 969, 108), (DAmbrose, 975, 110), (Breiburg, -942.2000122070312, 246.89999389648438)
            })
            {
                var row = world.SpawnPoolEntries.AsNoTracking().Single(entry => entry.Id == pool);
                Assert.IsLessThan(0.01, System.Math.Abs(row.PosX - x) + System.Math.Abs(row.PosZ - z), $"pool {pool} at ({x}, {z})");
            }
            var packages = world.NpcPackageEntries.AsNoTracking().Where(row => row.Id == Rigs || row.Id == DAmbrose || row.Id == DeLisi ||
                row.Id == Vitto || row.Id == Donal).AsEnumerable().ToDictionary(row => row.Id, row => row.PackageId);
            Assert.AreEqual((1231U, 1301U, 1219U, 1225U, 1227U), (packages[Rigs], packages[DAmbrose], packages[DeLisi], packages[Vitto], packages[Donal]));
            Assert.AreEqual(1, world.NpcPackageEntries.AsNoTracking().Count(row => row.PackageId == 1229), "only Munit has 1229");

            // Set 2's upstream packages (objectiveconversation 1161,1 1222; 1146,2 1269; 1146,3 1271; 1148,1 1296; 1148,2 1232;
            // 1148,3 1295), each on one NPC only.
            foreach (var (npc, package) in new (uint, uint)[]
                { (Lohen, 1222), (Cugin, 1269), (Vargo, 1271), (Fiorella, 1296), (Haggus, 1232), (Melching, 1295), (Bunko, 1224), (Yeg, 1272),
                  // Set 6's upstream packages (objectiveconversation 1144,1 1304; 1144,4 1297; 1144,5 1301; 1152,1 1292; 1155,3 1293).
                  (Cheung, 1292), (Deirdre, 1304), (Breiburg, 1293), (Sherer, 1297), (DAmbrose, 1301) })
            {
                Assert.AreEqual(package, world.NpcPackageEntries.AsNoTracking().Single(row => row.Id == npc).PackageId, $"{npc} package");
                Assert.AreEqual(1, world.NpcPackageEntries.AsNoTracking().Count(row => row.PackageId == package), $"only {npc} has {package}");
            }

            // Roscoe: the client's name 9140, hostile, the Flaregasher's class 7338 and attacks, his own hit points; no pool (the
            // scene brings him).
            var roscoe = world.CreatureEntries.AsNoTracking().Single(row => row.Id == Roscoe);
            var flaregasher = world.CreatureEntries.AsNoTracking().Single(row => row.Id == 540030);
            Assert.AreEqual((9140U, 35U, 0U, 7338U, 3000U), (roscoe.NameId, roscoe.Level, roscoe.Faction, roscoe.ClassId, roscoe.MaxHitPoints));
            Assert.AreEqual((flaregasher.Action1, flaregasher.Action2), (roscoe.Action1, roscoe.Action2), "the Flaregasher's attacks");
            var roscoeStat = world.CreatureStatEntries.AsNoTracking().Single(row => row.Id == Roscoe);
            Assert.AreEqual((3000, 150), (roscoeStat.Health, roscoeStat.Armor));
            Assert.IsFalse(world.SpawnPoolEntries.AsNoTracking().Any(row => row.Id == Roscoe || row.Creature1Id == Roscoe), "no pool");

            // The Kappa Grupa's office: two local teleporters (type 1, no class: the client draws the Brann statics) at the
            // statics' origins (MapUsables), linked both ways.
            var pads = world.TeleporterEntries.AsNoTracking().Where(row => row.Id == 909 || row.Id == 910).ToDictionary(row => row.Id);
            Assert.HasCount(2, pads);
            foreach (var (id, x, y, z) in new (uint, double, double, double)[] { (909, -206.738, 183.3619, 317.945), (910, -214.899, 244.4991, 319.7714) })
            {
                var pad = pads[id];
                Assert.AreEqual((1U, 0U, 1993U), ((uint)pad.Type, (uint)pad.ClassId, pad.MapContextId), $"pad {id}");
                Assert.IsLessThan(0.01, System.Math.Abs(pad.PosX - x) + System.Math.Abs(pad.PosY - y) + System.Math.Abs(pad.PosZ - z), $"pad {id} at its static");
            }
            CollectionAssert.AreEquivalent(new[] { (909U, 910U), (910U, 909U) }, world.LocalTeleporterLinkEntries.AsNoTracking()
                .Where(row => row.FromTeleporterId == 909 || row.FromTeleporterId == 910).AsEnumerable()
                .Select(row => (row.FromTeleporterId, row.ToTeleporterId)).ToArray());

            // Set 3's creatures: the Warden Bot, one rank under the Jailor 531044 (client name 8045 "Warden Bot Screw", class
            // 6961, the Jailor's laser, shock and death blast), and the Lasher (class 7477, no client name, its one attack 174/28 of the client's
            // Weapon_Creature_Lasher 7476); hostile, their own hit points; three of each in their pools.
            var warden = world.CreatureEntries.AsNoTracking().Single(row => row.Id == WardenBot);
            var jailor = world.CreatureEntries.AsNoTracking().Single(row => row.Id == 531044);
            Assert.AreEqual((8045U, 35U, 0U, 6961U, 4970U), (warden.NameId, warden.Level, warden.Faction, warden.ClassId, warden.MaxHitPoints));
            Assert.AreEqual((jailor.Action1, jailor.Action2, jailor.Action3), (warden.Action1, warden.Action2, warden.Action3), "the Jailor's attacks");
            var lasher = world.CreatureEntries.AsNoTracking().Single(row => row.Id == Lasher);
            Assert.AreEqual((0U, 40U, 0U, 7477U, 8470U, 770008U, 0U), (lasher.NameId, lasher.Level, lasher.Faction, lasher.ClassId,
                lasher.MaxHitPoints, lasher.Action1, lasher.Action2));
            var whip = world.Set<CreatureActionEntry>().AsNoTracking().Single(row => row.Id == 770008);
            Assert.AreEqual((174U, 28U, 2100U, 0U), ((uint)whip.ActionId, (uint)whip.ActionArgId, (uint)whip.Cooldown, (uint)whip.DamageType),
                "the client's Lasher melee; damage_type 0, so the client's weapon table gives it Virulent");
            Assert.AreEqual(Rasa.Data.DamageType.Virulent, Rasa.Data.CreatureWeaponDamage.Of(174, 28), "the client's table: Weapon_Creature_Lasher is Virulent");
            Assert.AreNotEqual(0U, warden.Action3, "the Jailor's death blast too");
            foreach (var (creature, health, armor) in new[] { (WardenBot, 4970, 247), (Lasher, 8470, 285) })
            {
                var stat = world.CreatureStatEntries.AsNoTracking().Single(row => row.Id == creature);
                Assert.AreEqual((health, armor), (stat.Health, stat.Armor), $"{creature} stats");
            }
            foreach (var (pool, creature, x, z) in new (uint, uint, double, double)[]
            {
                // 20271108000800 moved the Warden Bot pools clear of the facility's door.
                (770009, WardenBot, -575, 491), (770010, WardenBot, -499, 434), (770011, WardenBot, -636, 543),  // fix job F: 770011 off the lava
                (770012, Lasher, -752, 368), (770013, Lasher, -780, 356)          // fix job F moved the Lasher pools off the lava
            })
            {
                var row = world.SpawnPoolEntries.AsNoTracking().Single(entry => entry.Id == pool);
                Assert.AreEqual((1993U, creature, (byte)3, (byte)3, 1200U), (row.MapContextId, row.Creature1Id, row.Creature1MinCount,
                    row.Creature1MaxCount, (uint)row.RespawnTime), $"pool {pool}");
                Assert.IsLessThan(0.01, System.Math.Abs(row.PosX - x) + System.Math.Abs(row.PosZ - z), $"pool {pool} at ({x}, {z})");
            }

            // Set 4's creatures: the Smart Mine, a copy of the Necromite 570003 (client name 433 "Necromite", class 7528); since
            // fix job J (CrucibleFixJV1) one attack, row 770014 the Thrax's Virulent decay 450/1 (a damage over time, 20-30 a tick,
            // reach 0.5-3 m, every 10 s), no self-destruct (set 4's row 770016 stays in the table, on no creature); the
            // Reconstructor Bot (class 6960, client name 8033 "Reconstructor Bot Laborer", one attack 1/152 of the client's
            // Weapon_Creature_Arieki_Bot_Reconstructor 7189, 1500 ms); hostile, their own hit points.
            var mine = world.CreatureEntries.AsNoTracking().Single(row => row.Id == SmartMine);
            var necromite = world.CreatureEntries.AsNoTracking().Single(row => row.Id == 570003);
            Assert.AreEqual((433U, 40U, 0U, 7528U, 1500U, 770014U, 0U, 0U), (mine.NameId, mine.Level, mine.Faction, mine.ClassId,
                mine.MaxHitPoints, mine.Action1, mine.Action2, mine.Action3));
            Assert.AreEqual((necromite.RunSpeed, necromite.WalkSpeed), (mine.RunSpeed, mine.WalkSpeed), "the Necromite's speeds");
            var mineActions = world.Set<CreatureActionEntry>().AsNoTracking().Where(row => row.Id == 770014 || row.Id == 770016).ToDictionary(row => row.Id);
            var mineAttack = mineActions[770014];
            // TaRapedia: "very little damage", a small virulent DOT (ESTIMATE: the numbers, user review).
            Assert.AreEqual(("Smart Mine Crucible thrax_decay 450/1 (fix J)", 450U, 1U, 0.5, 3.0, 10000U, 500U, 20U, 30U, 4U),
                (mineAttack.Description, mineAttack.ActionId, mineAttack.ActionArgId, mineAttack.RangeMin, mineAttack.RangeMax, mineAttack.Cooldown, mineAttack.Windup,
                    mineAttack.MinDamage, mineAttack.MaxDamage, mineAttack.DamageType), "the Thrax's decay, Virulent");
            Assert.AreEqual((489U, 1U, 0U, 150U, 300U), ((uint)mineActions[770016].ActionId, (uint)mineActions[770016].ActionArgId,
                (uint)mineActions[770016].DamageType, mineActions[770016].MinDamage, mineActions[770016].MaxDamage), "set 4's self-destruct row, unused");
            var reconstructor = world.CreatureEntries.AsNoTracking().Single(row => row.Id == Reconstructor);
            Assert.AreEqual((8033U, 35U, 0U, 6960U, 4970U, 770015U, 0U), (reconstructor.NameId, reconstructor.Level, reconstructor.Faction,
                reconstructor.ClassId, reconstructor.MaxHitPoints, reconstructor.Action1, reconstructor.Action2));
            var shot = world.Set<CreatureActionEntry>().AsNoTracking().Single(row => row.Id == 770015);
            Assert.AreEqual((1U, 152U, 1500U, 0U), ((uint)shot.ActionId, (uint)shot.ActionArgId, (uint)shot.Cooldown, (uint)shot.DamageType),
                "the client's Reconstructor weapon; damage_type 0");
            foreach (var (creature, health, armor) in new[] { (SmartMine, 1500, 150), (Reconstructor, 4970, 247) })
            {
                var stat = world.CreatureStatEntries.AsNoTracking().Single(row => row.Id == creature);
                Assert.AreEqual((health, armor), (stat.Health, stat.Armor), $"{creature} stats");
            }
            foreach (var (pool, creature, count, x, z) in new (uint, uint, byte, double, double)[]
            {
                (770014, SmartMine, 4, 635, 557), (770015, SmartMine, 4, 585, 577), (770016, SmartMine, 4, 545, 587),
                (770017, Reconstructor, 3, -617, 570), (770018, Reconstructor, 3, -545, 486), (520013, Derge, 1, 859, 476),
                // Set 5: the Shield Drones north of Khulago Base, two more Reconstructor pools, Molitor north-east of the Awol Camp.
                (770024, ShieldDrone, 3, 415, 113), (770025, ShieldDrone, 3, 490, 65), (770026, ShieldDrone, 3, 535, 80),   // fix job G: 770026 a point
                (770027, Reconstructor, 3, -490, 480), (770028, Reconstructor, 3, -454, 495), (770029, Molitor, 1, -853, 359)   // fix job F moved 770024 and 770029 off the lava
            })
            {
                var row = world.SpawnPoolEntries.AsNoTracking().Single(entry => entry.Id == pool);
                Assert.AreEqual((1993U, creature, count, count), (row.MapContextId, row.Creature1Id, row.Creature1MinCount, row.Creature1MaxCount), $"pool {pool}");
                Assert.IsLessThan(0.01, System.Math.Abs(row.PosX - x) + System.Math.Abs(row.PosZ - z), $"pool {pool} at ({x}, {z})");
            }

            // Set 5's creatures: the Shield Drone, a new row of the client's Bane_Shield_Drone 7233 (client name 10766 "Shield Drone
            // Aggressor"), its attack 396/1 (every zone drone's) at its own damage, damage_type 0 (the client decides), and the
            // drones' heal aura 45; Preceptor Molitor (client name 9172), a copy of Derge 520013 (class 10504, his six attacks),
            // with his pack of three Crucible Thrax Soldiers 531018 and one Thrax Grenadier 531019 in his pool; hostile.
            var drone = world.CreatureEntries.AsNoTracking().Single(row => row.Id == ShieldDrone);
            Assert.AreEqual((10766U, 35U, 0U, 7233U, 5473U, 770022U, 45U, 0U), (drone.NameId, drone.Level, drone.Faction, drone.ClassId,
                drone.MaxHitPoints, drone.Action1, drone.Action2, drone.Action3));
            var droneAttack = world.Set<CreatureActionEntry>().AsNoTracking().Single(row => row.Id == 770022);
            Assert.AreEqual((396U, 1U, 1000U, 120U, 358U, 0U), ((uint)droneAttack.ActionId, (uint)droneAttack.ActionArgId, (uint)droneAttack.Cooldown,
                (uint)droneAttack.MinDamage, (uint)droneAttack.MaxDamage, (uint)droneAttack.DamageType), "the drones' attack, damage_type 0");
            var molitor = world.CreatureEntries.AsNoTracking().Single(row => row.Id == Molitor);
            var derge = world.CreatureEntries.AsNoTracking().Single(row => row.Id == Derge);
            Assert.AreEqual((9172U, 43U, 0U, 10504U, 10905U), (molitor.NameId, molitor.Level, molitor.Faction, molitor.ClassId, molitor.MaxHitPoints));
            Assert.AreEqual((derge.Action1, derge.Action2, derge.Action3, derge.Action4, derge.Action5, derge.Action6),
                (molitor.Action1, molitor.Action2, molitor.Action3, molitor.Action4, molitor.Action5, molitor.Action6), "Derge's six attacks");
            Assert.AreNotEqual(0U, molitor.Action6, "action6 from 20261011000000_Wire_missing_abilities");
            foreach (var (creature, health, armor) in new[] { (ShieldDrone, 5473, 248), (Molitor, 10905, 308) })
            {
                var stat = world.CreatureStatEntries.AsNoTracking().Single(row => row.Id == creature);
                Assert.AreEqual((health, armor), (stat.Health, stat.Armor), $"{creature} stats");
            }
            var pack = world.SpawnPoolEntries.AsNoTracking().Single(entry => entry.Id == 770029);
            Assert.AreEqual((531018U, (byte)3, (byte)3, 531019U, (byte)1, (byte)1, 0U, 500U, (double)CrucibleFixFV1.PointRadius),
                (pack.Creature2Id, pack.Creature2MinCount, pack.Creature2MaxCount, pack.Creature3Id, pack.Creature3MinCount, pack.Creature3MaxCount,
                    pack.Creature4Id, (uint)pack.RespawnTime, pack.Radius), "Molitor's pack (set 5 radius 10; a point since fix job F)");

            // Set 7: Cirkin (client name 9643, package 1742: objectiveconversation 1538,1), a copy of Lohen's row and look, in the
            // Staal Junkyard (2138); the three officers (9175, 9176, 9177) copies of Derge, Davinx and Orax with their attacks, in
            // no pool but NPC squad 770031 with a Shield Drone; Kalamit (9720, the Lasher boss class 10331, the Lasher's attack);
            // Lamontix (9641, a copy of Derge); the Junkyard's Technician (a copy of the Crucible's 531020 at level 40); the
            // Ultra Reconstructor (9640, the Reconstructor boss class 10314, the Reconstructor's attack).
            var cirkin = world.CreatureEntries.AsNoTracking().Single(row => row.Id == Cirkin);
            Assert.AreEqual((9643U, 42U, 1U, 7776U, 0U, 0U), (cirkin.NameId, cirkin.Level, cirkin.Faction, cirkin.ClassId, cirkin.RunSpeed, cirkin.WalkSpeed));
            Assert.AreEqual(1742U, world.NpcPackageEntries.AsNoTracking().Single(row => row.Id == Cirkin).PackageId);
            Assert.AreEqual(1, world.NpcPackageEntries.AsNoTracking().Count(row => row.PackageId == 1742), "only Cirkin has 1742");
            Assert.IsTrue(world.CreatureAppearanceEntries.AsNoTracking().Any(row => row.Id == Cirkin), "Cirkin has a look");
            foreach (var (creature, name, level, cls, copy, health, armor) in new (uint, uint, uint, uint, uint, int, int)[]
            {
                (Krubb, 9175, 45, 10504, Derge, 12000, 330), (Mungrel, 9176, 45, 10502, 520037, 12000, 330), (Sedge, 9177, 45, 21882, 520061, 12000, 330),
                (Lamontix, 9641, 42, 10504, Derge, 12000, 300), (Technician, 8066, 40, 7043, 531020, 8409, 285)
            })
            {
                var row = world.CreatureEntries.AsNoTracking().Single(entry => entry.Id == creature);
                var model = world.CreatureEntries.AsNoTracking().Single(entry => entry.Id == copy);
                Assert.AreEqual((name, level, 0U, cls, (uint)health), (row.NameId, row.Level, row.Faction, row.ClassId, row.MaxHitPoints), $"{creature}");
                Assert.AreEqual((model.Action1, model.Action2, model.Action3, model.Action4, model.Action5, model.Action6),
                    (row.Action1, row.Action2, row.Action3, row.Action4, row.Action5, row.Action6), $"{creature}: {copy}'s attacks");
                var stat = world.CreatureStatEntries.AsNoTracking().Single(entry => entry.Id == creature);
                Assert.AreEqual((health, armor), (stat.Health, stat.Armor), $"{creature} stats");
            }
            foreach (var (creature, name, cls, attack) in new (uint, uint, uint, uint)[] { (Kalamit, 9720, 10331, 770008), (UltraReconstructor, 9640, 10314, 770015) })
            {
                var row = world.CreatureEntries.AsNoTracking().Single(entry => entry.Id == creature);
                Assert.AreEqual((name, 42U, 0U, cls, 12000U, attack, 0U), (row.NameId, row.Level, row.Faction, row.ClassId, row.MaxHitPoints,
                    row.Action1, row.Action2), $"{creature}");
                var stat = world.CreatureStatEntries.AsNoTracking().Single(entry => entry.Id == creature);
                Assert.AreEqual((12000, 300), (stat.Health, stat.Armor), $"{creature} stats");
            }
            foreach (var (pool, map, creature, count, x, z) in new (uint, uint, uint, byte, double, double)[]
            {
                (Cirkin, 2138, Cirkin, 1, 187, -233), (770034, 1993, Kalamit, 1, -486, 120), (Lamontix, 2138, Lamontix, 1, 350, -52),
                (770036, 2138, Technician, 3, 330, -160), (770037, 2138, Technician, 3, 306, -106), (UltraReconstructor, 2138, UltraReconstructor, 1, 164, -76)   // fix job H moved it off the ledge
            })
            {
                var row = world.SpawnPoolEntries.AsNoTracking().Single(entry => entry.Id == pool);
                Assert.AreEqual((map, creature, count, count), (row.MapContextId, row.Creature1Id, row.Creature1MinCount, row.Creature1MaxCount), $"pool {pool}");
                Assert.IsLessThan(0.01, System.Math.Abs(row.PosX - x) + System.Math.Abs(row.PosZ - z), $"pool {pool} at ({x}, {z})");
            }
            Assert.IsFalse(world.SpawnPoolEntries.AsNoTracking().Any(row => row.Creature1Id == Krubb || row.Creature1Id == Mungrel || row.Creature1Id == Sedge),
                "the officers come with their squad only");
            var squad = world.Set<NpcSquadTypeEntry>().AsNoTracking().Single(row => row.Id == Krubb);
            Assert.AreEqual(((byte)0, 3.0, 30.0, (byte)0), (squad.Formation, squad.Spacing, squad.Tether, squad.LeaderBystander), "a wedge, 30 m tether, no bystander");
            CollectionAssert.AreEqual(new[] { (0, Krubb), (1, Mungrel), (2, Sedge), (3, ShieldDrone) }, world.Set<NpcSquadMemberEntry>().AsNoTracking()
                .Where(row => row.TypeId == Krubb).OrderBy(row => row.Slot).AsEnumerable().Select(row => ((int)row.Slot, row.CreatureId)).ToArray(),
                "Krubb leads, Mungrel, Sedge and a Shield Drone");
            var beat = world.Set<NpcSquadPathEntry>().AsNoTracking().Single(row => row.Id == Krubb);
            Assert.AreEqual((1993U, (byte)1), (beat.MapContextId, beat.Mode), "on the Crucible, there and back");
            Assert.AreEqual(7, world.Set<NpcSquadPathPointEntry>().AsNoTracking().Count(row => row.PathId == Krubb));
            var spawn = world.Set<NpcSquadSpawnEntry>().AsNoTracking().Single(row => row.Id == Krubb);
            Assert.AreEqual((1993U, Krubb, Krubb, 300U, (byte)1), (spawn.MapContextId, spawn.TypeId, spawn.PathId, spawn.RespawnSeconds, spawn.Enabled));

            // Set 8a: Tiernan (client name 9365, package 1483: objectiveconversation 1217,1 and 1218,1), a copy of Keplinger's
            // row and look, on the Crucible; Botha (9278, package 1410: 1218,2), a copy of Harris's, on Ashen Desert (1734);
            // Keplinger (1337: 1210,1) and Madias moved off the Prometheus Outpost control point.
            foreach (var (npc, name, package, copy, map, x, z) in new (uint, uint, uint, uint, uint, double, double)[]
                { (Tiernan, 9365, 1483, Keplinger, 1993, 589.5, 250), (Botha, 9278, 1410, Harris, 1734, 378, -458.5) })
            {
                var creature = world.CreatureEntries.AsNoTracking().Single(row => row.Id == npc);
                var model = world.CreatureEntries.AsNoTracking().Single(row => row.Id == copy);
                Assert.AreEqual((name, 40U, 1U, model.ClassId, 0U, 0U), (creature.NameId, creature.Level, creature.Faction, creature.ClassId,
                    creature.RunSpeed, creature.WalkSpeed), $"{npc}");
                Assert.AreEqual(package, world.NpcPackageEntries.AsNoTracking().Single(row => row.Id == npc).PackageId, $"{npc} package");
                Assert.AreEqual(1, world.NpcPackageEntries.AsNoTracking().Count(row => row.PackageId == package), $"only {npc} has {package}");
                var stat = world.CreatureStatEntries.AsNoTracking().Single(row => row.Id == npc);
                Assert.AreEqual((4500, 300), (stat.Health, stat.Armor), $"{npc} stats");
                Assert.AreEqual(world.CreatureAppearanceEntries.AsNoTracking().Count(row => row.Id == copy),
                    world.CreatureAppearanceEntries.AsNoTracking().Count(row => row.Id == npc), $"{npc} has {copy}'s look");
                var pool = world.SpawnPoolEntries.AsNoTracking().Single(row => row.Id == npc);
                Assert.AreEqual((map, npc, (byte)1, (byte)1), (pool.MapContextId, pool.Creature1Id, pool.Creature1MinCount, pool.Creature1MaxCount), $"{npc} pool");
                Assert.IsLessThan(0.01, System.Math.Abs(pool.PosX - x) + System.Math.Abs(pool.PosZ - z), $"pool {npc} at ({x}, {z})");
            }
            Assert.AreEqual(1337U, world.NpcPackageEntries.AsNoTracking().Single(row => row.Id == Keplinger).PackageId);
            Assert.AreEqual(1, world.NpcPackageEntries.AsNoTracking().Count(row => row.PackageId == 1337), "only Keplinger has 1337");
            foreach (var (pool, x, z) in new (uint, double, double)[] { (Keplinger, 379, 384), (Madias, 378.9, 367.6) })
            {
                var row = world.SpawnPoolEntries.AsNoTracking().Single(entry => entry.Id == pool);
                Assert.IsLessThan(0.01, System.Math.Abs(row.PosX - x) + System.Math.Abs(row.PosZ - z), $"pool {pool} at ({x}, {z})");
            }
            Assert.AreEqual(0.0, world.SpawnPoolEntries.AsNoTracking().Single(row => row.Id == 770026).Radius, "fix job G: Shield Drone pool 770026 a point");

            // Set 8b: the Juggernaut, a new row of the client's Vehicle_Bane_Juggernaut 7722 (name 0: the class's name
            // "Juggernaut"), the Devil's Den Juggernaut 567038's two attacks 1/180 and 428/1 at its own damage, damage_type 0;
            // Maxxum Ugoretz (client name 9317), a copy of Derge 520013 (class 10504, his six attacks); hostile.
            var juggernaut = world.CreatureEntries.AsNoTracking().Single(row => row.Id == Juggernaut);
            Assert.AreEqual((0U, 42U, 0U, 7722U, 16700U, 770041U, 770042U, 0U), (juggernaut.NameId, juggernaut.Level, juggernaut.Faction,
                juggernaut.ClassId, juggernaut.MaxHitPoints, juggernaut.Action1, juggernaut.Action2, juggernaut.Action3));
            var juggernautAttacks = world.Set<CreatureActionEntry>().AsNoTracking().Where(row => row.Id == 770041 || row.Id == 770042).ToDictionary(row => row.Id);
            Assert.AreEqual((1U, 180U, 1500U, 480U, 720U, 0U), ((uint)juggernautAttacks[770041].ActionId, (uint)juggernautAttacks[770041].ActionArgId,
                (uint)juggernautAttacks[770041].Cooldown, (uint)juggernautAttacks[770041].MinDamage, (uint)juggernautAttacks[770041].MaxDamage,
                (uint)juggernautAttacks[770041].DamageType), "the Juggernaut's weapon, damage_type 0");
            Assert.AreEqual((428U, 1U, 8000U, 1080U, 1540U, 0U), ((uint)juggernautAttacks[770042].ActionId, (uint)juggernautAttacks[770042].ActionArgId,
                (uint)juggernautAttacks[770042].Cooldown, (uint)juggernautAttacks[770042].MinDamage, (uint)juggernautAttacks[770042].MaxDamage,
                (uint)juggernautAttacks[770042].DamageType), "the Juggernaut's cannon, damage_type 0");
            var ugoretz = world.CreatureEntries.AsNoTracking().Single(row => row.Id == Ugoretz);
            Assert.AreEqual((9317U, 43U, 0U, 10504U, 10905U), (ugoretz.NameId, ugoretz.Level, ugoretz.Faction, ugoretz.ClassId, ugoretz.MaxHitPoints));
            Assert.AreEqual((derge.Action1, derge.Action2, derge.Action3, derge.Action4, derge.Action5, derge.Action6),
                (ugoretz.Action1, ugoretz.Action2, ugoretz.Action3, ugoretz.Action4, ugoretz.Action5, ugoretz.Action6), "Derge's six attacks");
            foreach (var (creature, health, armor) in new[] { (Juggernaut, 16700, 300), (Ugoretz, 10905, 308) })
            {
                var stat = world.CreatureStatEntries.AsNoTracking().Single(row => row.Id == creature);
                Assert.AreEqual((health, armor), (stat.Health, stat.Armor), $"{creature} stats");
            }
            foreach (var (pool, creature, count, respawn, x, z) in new (uint, uint, byte, uint, double, double)[]
            {
                (770041, Juggernaut, 2, 1200, 383, -185), (770042, Juggernaut, 2, 1200, 440, -190), (770043, Juggernaut, 2, 1200, 310, -230),
                (770044, Ugoretz, 1, 500, 340, -130)
            })
            {
                var row = world.SpawnPoolEntries.AsNoTracking().Single(entry => entry.Id == pool);
                Assert.AreEqual((1993U, creature, count, count, respawn, 0.0), (row.MapContextId, row.Creature1Id, row.Creature1MinCount,
                    row.Creature1MaxCount, (uint)row.RespawnTime, row.Radius), $"pool {pool}: a point");
                Assert.IsLessThan(0.01, System.Math.Abs(row.PosX - x) + System.Math.Abs(row.PosZ - z), $"pool {pool} at ({x}, {z})");
            }

            // Set 9: Captain Tiersky (client name 9214, no package: 1224 has no objective conversation) moved off the
            // Prometheus Outpost control point, still its garrison link; no new row (set 11a's rows start at 770045).
            var tiersky = world.CreatureEntries.AsNoTracking().Single(row => row.Id == Tiersky);
            Assert.AreEqual((9214U, 1U), (tiersky.NameId, tiersky.Faction));
            Assert.IsFalse(world.NpcPackageEntries.AsNoTracking().Any(row => row.Id == Tiersky), "Tiersky has no package");
            var tierskyPool = world.SpawnPoolEntries.AsNoTracking().Single(row => row.Id == Tiersky);
            Assert.AreEqual((1993U, Tiersky, 379.0, 168.36, 400.0), (tierskyPool.MapContextId, tierskyPool.Creature1Id,
                System.Math.Round(tierskyPool.PosX, 2), System.Math.Round(tierskyPool.PosY, 2), System.Math.Round(tierskyPool.PosZ, 2)));
            Assert.IsTrue(world.Set<ControlPointLinkEntry>().AsNoTracking().Any(link => link.ControlPointId == 31 &&
                link.Kind == ControlPointLinkEntry.KindAfsPool && link.ObjectId == Tiersky), "Tiersky's pool stays control point 31's AFS link (kind 2)");

            // Set 11a: Shanul (client name 9550, package 1664: objectiveconversation 1444,1) and Dhimand (9614, 1700: 1444,3),
            // copies of Lohen's row and look, in the Incurables Ward (2141); the Incurable Brute (9521, class 23191) and Slayer
            // (9617, class 25611), two of each in nine pools; Jaikev (9539, class 25547) and Parvatee (9622, class 25617), each
            // in a point pool; all four with the one attack 770047, the client's weapon 23198 (174/51), damage_type 0; Sheira
            // (package 1640: 1445,1) and Tempa moved. Set 11b: Dhimand walks (run 7, walk 2, 1398's escort), his pool posed Standing.
            foreach (var (npc, name, package, x, z, run, walk) in new (uint, uint, uint, double, double, uint, uint)[]
                { (Shanul, 9550, 1664, 60, -70, 0, 0), (Dhimand, 9614, 1700, -40, 386, 7, 2) })
            {
                var creature = world.CreatureEntries.AsNoTracking().Single(row => row.Id == npc);
                Assert.AreEqual((name, 40U, 1U, 7776U, run, walk), (creature.NameId, creature.Level, creature.Faction, creature.ClassId,
                    (uint)creature.RunSpeed, (uint)creature.WalkSpeed), $"{npc}");
                Assert.AreEqual(package, world.NpcPackageEntries.AsNoTracking().Single(row => row.Id == npc).PackageId, $"{npc} package");
                Assert.AreEqual(1, world.NpcPackageEntries.AsNoTracking().Count(row => row.PackageId == package), $"only {npc} has {package}");
                var stat = world.CreatureStatEntries.AsNoTracking().Single(row => row.Id == npc);
                Assert.AreEqual((4500, 300), (stat.Health, stat.Armor), $"{npc} stats");
                Assert.AreEqual(world.CreatureAppearanceEntries.AsNoTracking().Count(row => row.Id == Lohen),
                    world.CreatureAppearanceEntries.AsNoTracking().Count(row => row.Id == npc), $"{npc} has Lohen's look");
                var pool = world.SpawnPoolEntries.AsNoTracking().Single(row => row.Id == npc);
                Assert.AreEqual((2141U, npc, (byte)1, (byte)1), (pool.MapContextId, pool.Creature1Id, pool.Creature1MinCount, pool.Creature1MaxCount), $"{npc} pool");
                Assert.IsLessThan(0.01, System.Math.Abs(pool.PosX - x) + System.Math.Abs(pool.PosZ - z), $"pool {npc} at ({x}, {z})");
            }
            Assert.AreEqual(1640U, world.NpcPackageEntries.AsNoTracking().Single(row => row.Id == Sheira).PackageId);
            Assert.AreEqual(1, world.NpcPackageEntries.AsNoTracking().Count(row => row.PackageId == 1640), "only Sheira has 1640");
            Assert.IsFalse(world.NpcPackageEntries.AsNoTracking().Any(row => row.Id == Tempa), "Tempa has no package (1402 has no objective conversation)");
            foreach (var (pool, map, x, z) in new (uint, uint, double, double)[] { (Sheira, 2141, -18, -88), (Tempa, 1993, -307, 326) })
            {
                var row = world.SpawnPoolEntries.AsNoTracking().Single(entry => entry.Id == pool);
                Assert.AreEqual(map, row.MapContextId, $"pool {pool}");
                Assert.IsLessThan(0.01, System.Math.Abs(row.PosX - x) + System.Math.Abs(row.PosZ - z), $"pool {pool} at ({x}, {z})");
            }
            foreach (var (creature, name, cls, level, health, armor) in new (uint, uint, uint, uint, int, int)[]
            {
                (Brute, 9521, 23191, 40, 8409, 285), (Slayer, 9617, 25611, 40, 8409, 285), (Jaikev, 9539, 25547, 42, 12000, 300),
                (Parvatee, 9622, 25617, 42, 12000, 300)
            })
            {
                var row = world.CreatureEntries.AsNoTracking().Single(entry => entry.Id == creature);
                Assert.AreEqual((name, level, 0U, cls, (uint)health, 770047U, 0U), (row.NameId, row.Level, row.Faction, row.ClassId, row.MaxHitPoints,
                    row.Action1, row.Action2), $"{creature}");
                var stat = world.CreatureStatEntries.AsNoTracking().Single(entry => entry.Id == creature);
                Assert.AreEqual((health, armor), (stat.Health, stat.Armor), $"{creature} stats");
                Assert.IsFalse(world.CreatureAppearanceEntries.AsNoTracking().Any(entry => entry.Id == creature), $"{creature}: the Brann redshirt class draws itself");
            }
            var blade = world.Set<CreatureActionEntry>().AsNoTracking().Single(row => row.Id == 770047);
            Assert.AreEqual((174U, 51U, 2100U, 557U, 779U, 0U), ((uint)blade.ActionId, (uint)blade.ActionArgId, (uint)blade.Cooldown,
                (uint)blade.MinDamage, (uint)blade.MaxDamage, (uint)blade.DamageType), "the Incurables' weapon 23198, damage_type 0");
            foreach (var (pool, x, z) in new (uint, double, double)[]
            {
                (770047, 76, 90), (770048, 76, 132), (770049, 94, 178), (770050, 124, 184), (770051, -22, 416), (770052, -40, 464),
                (770053, 94, 488), (770054, -90, 316), (770055, -82, 348)
            })
            {
                var row = world.SpawnPoolEntries.AsNoTracking().Single(entry => entry.Id == pool);
                Assert.AreEqual((2141U, Brute, (byte)2, (byte)2, Slayer, (byte)2, (byte)2, 0U, 900U, pool is 770053 or 770055 or CrucibleBloodFireSteelV1.PointedPool ? 0.0 : 5.0), (row.MapContextId, row.Creature1Id,
                    row.Creature1MinCount, row.Creature1MaxCount, row.Creature2Id, row.Creature2MinCount, row.Creature2MaxCount, row.Creature3Id,
                    (uint)row.RespawnTime, row.Radius), $"pool {pool}: two Brutes, two Slayers (770053, 770055 and, from set 15, 770048 points)");
                Assert.IsLessThan(0.01, System.Math.Abs(row.PosX - x) + System.Math.Abs(row.PosZ - z), $"pool {pool} at ({x}, {z})");
            }
            foreach (var (pool, creature, x, z) in new (uint, uint, double, double)[] { (770056, Jaikev, -98, 332), (770057, Parvatee, -90, 340) })
            {
                var row = world.SpawnPoolEntries.AsNoTracking().Single(entry => entry.Id == pool);
                Assert.AreEqual((2141U, creature, (byte)1, (byte)1, 0U, 900U, 0.0), (row.MapContextId, row.Creature1Id, row.Creature1MinCount,
                    row.Creature1MaxCount, row.Creature2Id, (uint)row.RespawnTime, row.Radius), $"pool {pool}: a point");
                Assert.IsLessThan(0.01, System.Math.Abs(row.PosX - x) + System.Math.Abs(row.PosZ - z), $"pool {pool} at ({x}, {z})");
            }
            Assert.IsFalse(world.CreatureEntries.AsNoTracking().Any(row => row.Id >= 770051 && row.Id <= 770057), "set 11a adds no more creatures");

            // Set 12a: Ruben (client name 9724, server-only package 770058: no objective talk with him) on the Crucible, a copy of
            // Sherer's row and look (class 3848, female); Brian (9725, package 1800: objectiveconversation 1577,1, 1521,5 and 1521,7)
            // in Chaukas (2146), a copy of DeLisi's (class 3846), able to walk (run 7, walk 2: 1521's copy follows) and posed
            // Standing at his pool (he keeps his post).
            foreach (var (npc, name, package, copy, cls, run, walk, map, x, z) in new (uint, uint, uint, uint, uint, uint, uint, uint, double, double)[]
            {
                (Ruben, 9724, 770058, Sherer, 3848, 0, 0, 1993, 966, 156), (Brian, 9725, 1800, DeLisi, 3846, 7, 2, 2146, -38, -256)
            })
            {
                var creature = world.CreatureEntries.AsNoTracking().Single(row => row.Id == npc);
                Assert.AreEqual((name, 40U, 1U, cls, run, walk), (creature.NameId, creature.Level, creature.Faction, creature.ClassId,
                    (uint)creature.RunSpeed, (uint)creature.WalkSpeed), $"{npc}");
                Assert.AreEqual(package, world.NpcPackageEntries.AsNoTracking().Single(row => row.Id == npc).PackageId, $"{npc} package");
                Assert.AreEqual(1, world.NpcPackageEntries.AsNoTracking().Count(row => row.PackageId == package), $"only {npc} has {package}");
                var stat = world.CreatureStatEntries.AsNoTracking().Single(row => row.Id == npc);
                Assert.AreEqual((4500, 300), (stat.Health, stat.Armor), $"{npc} stats");
                Assert.AreEqual(world.CreatureAppearanceEntries.AsNoTracking().Count(row => row.Id == copy),
                    world.CreatureAppearanceEntries.AsNoTracking().Count(row => row.Id == npc), $"{npc} has {copy}'s look");
                var pool = world.SpawnPoolEntries.AsNoTracking().Single(row => row.Id == npc);
                Assert.AreEqual((map, npc, (byte)1, (byte)1, 0.0), (pool.MapContextId, pool.Creature1Id, pool.Creature1MinCount, pool.Creature1MaxCount, pool.Radius), $"{npc} pool");
                Assert.IsLessThan(0.01, System.Math.Abs(pool.PosX - x) + System.Math.Abs(pool.PosZ - z), $"pool {npc} at ({x}, {z})");
            }
            Assert.AreEqual(1, world.Database.SqlQueryRaw<int>($"SELECT pose AS Value FROM spawnpool_pose WHERE id = {Brian}").AsEnumerable().Single(),
                "Brian's pool posed Standing (Data.NpcPose 1)");
            Assert.AreEqual(1, world.Database.SqlQueryRaw<int>($"SELECT pose AS Value FROM spawnpool_pose WHERE id = {Dhimand}").AsEnumerable().Single(),
                "Dhimand's pool posed Standing (set 11b)");

            // Set 13: Stark (client name 10300, server-only package 770060: no objective talk with him) at Outpost Intrepid, a copy
            // of DeLisi's row and look (class 3846); the two Skeevs (6734), copies of the Wilderness Skeev 630130 (class 28589, its
            // look and rifle), level 40, Molitor's hit points and armour, no pool (1899's scene creatures): the talker neutral
            // (faction 3) with package 2371 (objectiveconversation 1899,7), the fighter hostile with no package; Capriulo and
            // Eugin (upstream) moved to TaRapedia's places.
            var stark = world.CreatureEntries.AsNoTracking().Single(row => row.Id == Stark);
            Assert.AreEqual((10300U, 40U, 1U, 3846U, 0U, 0U), (stark.NameId, stark.Level, stark.Faction, stark.ClassId, (uint)stark.RunSpeed,
                (uint)stark.WalkSpeed), "Stark");
            Assert.AreEqual(770060U, world.NpcPackageEntries.AsNoTracking().Single(row => row.Id == Stark).PackageId, "Stark package");
            var starkStat = world.CreatureStatEntries.AsNoTracking().Single(row => row.Id == Stark);
            Assert.AreEqual((4500, 300), (starkStat.Health, starkStat.Armor), "Stark stats");
            Assert.AreEqual(world.CreatureAppearanceEntries.AsNoTracking().Count(row => row.Id == DeLisi),
                world.CreatureAppearanceEntries.AsNoTracking().Count(row => row.Id == Stark), "Stark has DeLisi's look");
            var starkPool = world.SpawnPoolEntries.AsNoTracking().Single(row => row.Id == Stark);
            Assert.AreEqual((1993U, Stark, (byte)1, (byte)1, 0.0), (starkPool.MapContextId, starkPool.Creature1Id, starkPool.Creature1MinCount,
                starkPool.Creature1MaxCount, starkPool.Radius), "Stark pool");
            Assert.IsLessThan(0.01, System.Math.Abs(starkPool.PosX - 1019) + System.Math.Abs(starkPool.PosZ - 50), "Stark at (1019, 50)");
            foreach (var (npc, faction, run, walk) in new (uint, uint, uint, uint)[] { (SkeevTalker, 3, 0, 0), (SkeevFighter, 0, 9, 5) })
            {
                var creature = world.CreatureEntries.AsNoTracking().Single(row => row.Id == npc);
                Assert.AreEqual((6734U, 40U, faction, 28589U, run, walk, 41U), (creature.NameId, creature.Level, creature.Faction, creature.ClassId,
                    (uint)creature.RunSpeed, (uint)creature.WalkSpeed, creature.Action1), $"{npc}");
                var stat = world.CreatureStatEntries.AsNoTracking().Single(row => row.Id == npc);
                Assert.AreEqual((10905, 308), (stat.Health, stat.Armor), $"{npc} stats");
                Assert.AreEqual(1, world.CreatureAppearanceEntries.AsNoTracking().Count(row => row.Id == npc), $"{npc} has Skeev's look");
                Assert.IsFalse(world.SpawnPoolEntries.AsNoTracking().Any(row => row.Id == npc), $"{npc}: no pool");
            }
            Assert.AreEqual(2371U, world.NpcPackageEntries.AsNoTracking().Single(row => row.Id == SkeevTalker).PackageId, "the talker's package");
            Assert.IsFalse(world.NpcPackageEntries.AsNoTracking().Any(row => row.Id == SkeevFighter), "the fighter has no package");
            foreach (var (pool, package, x, z) in new (uint, uint, double, double)[] { (Capriulo, 2335, -303, 376), (Eugin, 2337, -232, 473) })
            {
                Assert.AreEqual(package, world.NpcPackageEntries.AsNoTracking().Single(row => row.Id == pool).PackageId, $"{pool} package");
                var row = world.SpawnPoolEntries.AsNoTracking().Single(entry => entry.Id == pool);
                Assert.IsLessThan(0.01, System.Math.Abs(row.PosX - x) + System.Math.Abs(row.PosZ - z), $"pool {pool} at ({x}, {z})");
            }
            // Set 12b: the Refitted Warden Bot (client name 9529), class 26395 NPC_Prison_Bot_Warden (the friendly Warden Bot),
            // friendly, level 40, 4500/300, no speed, no attack, no look, no pool (1523's scene creature) and no package.
            var bot = world.CreatureEntries.AsNoTracking().Single(row => row.Id == RefittedBot);
            Assert.AreEqual((9529U, 40U, 1U, 26395U, 0U, 0U, 0U), (bot.NameId, bot.Level, bot.Faction, bot.ClassId, (uint)bot.RunSpeed,
                (uint)bot.WalkSpeed, bot.Action1), "the Refitted Warden Bot");
            var botStat = world.CreatureStatEntries.AsNoTracking().Single(row => row.Id == RefittedBot);
            Assert.AreEqual((4500, 300), (botStat.Health, botStat.Armor), "the Refitted Warden Bot's stats");
            Assert.IsFalse(world.CreatureAppearanceEntries.AsNoTracking().Any(row => row.Id == RefittedBot), "no look: the class's own model");
            Assert.IsFalse(world.SpawnPoolEntries.AsNoTracking().Any(row => row.Id == RefittedBot), "no pool");
            Assert.IsFalse(world.NpcPackageEntries.AsNoTracking().Any(row => row.Id == RefittedBot), "no package");
            Assert.AreEqual(CrucibleFactoryFortificationV1.NextFreeId, RefittedBot + 1);
            Assert.IsFalse(world.CreatureEntries.AsNoTracking().Any(row => row.Id >= 770064 && row.Id <= 770069), "set 12b's last creature is 770063 (set 15 from 770070)");
            Assert.IsFalse(world.SpawnPoolEntries.AsNoTracking().Any(row => row.Id >= 770061 && row.Id <= 770069), "set 13's last pool is 770060 (set 15 from 770070)");
        }

        /// <summary>
        /// The client's own texts: the ids are written out here from the client tables (missionconversation,
        /// missionobjective), not taken from the data classes, so a wrong id in those fails.
        /// </summary>
        [TestMethod]
        public void TitlesAndObjectivesUseTheClientsTexts()
        {
            var expected = new (uint Mission, uint Title, (uint Objective, uint Name, uint Body, uint? Counter)[] Objectives)[]
            {
                (1142, 8344, new (uint, uint, uint, uint?)[] { (1, 8533, 8534, null) }),
                (1143, 8353, new (uint, uint, uint, uint?)[] { (1, 8358, 8359, null), (22, 20331, 20332, 20333) }),
                (1150, 8414, new (uint, uint, uint, uint?)[] { (1, 8574, 8575, null) }),
                (1145, 8386, new (uint, uint, uint, uint?)[] { (6, 9997, 9998, null), (1, 8647, 8648, null), (2, 8650, 8651, null),
                    (3, 8653, 8654, null), (5, 8662, 8663, null) }),
                (1161, 8483, new (uint, uint, uint, uint?)[] { (1, 8704, 8705, null) }),
                (1146, 8394, new (uint, uint, uint, uint?)[] { (1, 8917, 8918, null), (2, 8920, 8921, null), (3, 8923, 8924, null),
                    (4, 8931, 8932, null), (5, 8926, 8927, null), (6, 8928, 8929, null) }),
                (1148, 8404, new (uint, uint, uint, uint?)[] { (1, 9346, 9347, null), (2, 9349, 9350, null), (3, 9352, 9353, null) }),
                (1149, 8409, new (uint, uint, uint, uint?)[] { (1, 9357, 9358, 9359) }),
                (1147, 8399, new (uint, uint, uint, uint?)[] { (1, 8960, 8961, null), (2, 8962, 8963, null), (3, 8964, 8965, null) }),
                (1157, 8463, new (uint, uint, uint, uint?)[] { (1, 8602, 8603, null) }),
                (1170, 8707, new (uint, uint, uint, uint?)[] { (1, 8714, 8715, null), (2, 8717, 8718, null), (3, 8719, 8720, null),
                    (4, 8722, 8723, null), (5, 8724, 8725, null), (6, 8727, 8728, null), (7, 8734, 8735, null), (8, 8737, 8738, null) }),
                (1229, 9768, new (uint, uint, uint, uint?)[] { (1, 9778, 9779, null) }),
                (1232, 9807, new (uint, uint, uint, uint?)[] { (1, 9812, 9813, 9814) }),
                (1241, 9902, new (uint, uint, uint, uint?)[] { (1, 9907, 9908, null) }),
                (1203, 9434, new (uint, uint, uint, uint?)[] { (1, 9439, 9440, null), (2, 9441, 9442, null) }),
                (1204, 9453, new (uint, uint, uint, uint?)[] { (3, 9462, 9463, null), (4, 9464, 9465, null), (5, 9466, 9467, null) }),
                (1202, 9426, new (uint, uint, uint, uint?)[] { (1, 9431, 9432, 9433) }),
                (1208, 9502, new (uint, uint, uint, uint?)[] { (1, 9507, 9508, null) }),
                (1185, 9062, new (uint, uint, uint, uint?)[] { (1, 9076, 9077, null), (2, 9079, 9080, null), (3, 9081, 9082, null) }),
                (1144, 8360, new (uint, uint, uint, uint?)[] { (1, 9160, 9161, null), (3, 9168, 9169, null), (4, 9170, 9171, null),
                    (5, 9175, 9176, null), (6, 9181, 9182, null), (7, 9183, 9184, null), (8, 9172, 9173, null), (9, 9211, 9212, null) }),
                (1152, 8434, new (uint, uint, uint, uint?)[] { (1, 9106, 9107, null), (2, 9109, 9110, null) }),
                (1153, 8439, new (uint, uint, uint, uint?)[] { (1, 9112, 9113, null) }),
                (1155, 8449, new (uint, uint, uint, uint?)[] { (1, 9470, 9471, null), (2, 9473, 9474, null), (3, 15821, 15822, null) }),
                (1205, 9476, new (uint, uint, uint, uint?)[] { (1, 9481, 9482, null), (2, 9484, 9485, null), (3, 15818, 15819, null) }),
                (1206, 9487, new (uint, uint, uint, uint?)[] { (1, 9509, 9510, null), (2, 9511, 9512, null), (3, 9513, 9514, null) }),
                (1519, 13547, new (uint, uint, uint, uint?)[] { (1, 13629, 13630, null), (2, 13631, 13632, null) }),
                (1529, 13656, new (uint, uint, uint, uint?)[] { (1, 13721, 13722, 13723), (2, 13724, 13725, null), (3, 13726, 13727, null),
                    (4, 13728, 13729, null), (5, 13730, 13731, 13732), (6, 13730, 13731, null), (7, 13730, 13731, 13734) }),
                (1538, 13862, new (uint, uint, uint, uint?)[] { (1, 13877, 13878, null), (2, 13880, 13881, null), (3, 13882, 13883, null),
                    (4, 13964, 13965, null) }),
                (1210, 9549, new (uint, uint, uint, uint?)[] { (1, 9635, 9636, null) }),
                (1212, 9559, new (uint, uint, uint, uint?)[] { (1, 9638, 9639, 9640) }),
                (1217, 9584, new (uint, uint, uint, uint?)[] { (1, 10540, 10541, null), (2, 10540, 10541, null) }),
                (1218, 9589, new (uint, uint, uint, uint?)[] { (1, 10543, 10544, null), (2, 14819, 14820, null) }),
                (1213, 9564, new (uint, uint, uint, uint?)[] { (1, 9662, 9663, 9664) }),
                (1215, 9574, new (uint, uint, uint, uint?)[] { (1, 9797, 9798, null), (2, 9800, 9801, null) }),
                (1216, 9579, new (uint, uint, uint, uint?)[] { (1, 9803, 9804, null) }),
                (1224, 9641, new (uint, uint, uint, uint?)[] { (1, 9646, 9647, null), (2, 9648, 9649, null), (3, 9650, 9651, null),
                    (4, 9652, 9653, null), (5, 9654, 9655, null), (6, 9656, 9657, null) }),
                (1214, 9569, new (uint, uint, uint, uint?)[] { (1, 9665, 9666, null), (2, 9667, 9668, null), (3, 9669, 9670, null) }),
                (1445, 12643, new (uint, uint, uint, uint?)[] { (1, 12648, 12649, null) }),
                (1444, 12638, new (uint, uint, uint, uint?)[] { (1, 12682, 12683, null), (2, 12685, 12686, null), (3, 12688, 12689, null) }),
                (1402, 12067, new (uint, uint, uint, uint?)[] { (1, 12651, 12652, 12653), (2, 12654, 12655, 12656), (3, 12657, 12658, 12659) }),
                (1577, 14453, new (uint, uint, uint, uint?)[] { (1, 14458, 14459, null) }),
                // 1521: the client's 3-8; hidden 9 and 10 (the tracker and its opener) with 8's texts, hidden 11 (6's tally) with 6's.
                (1521, 13581, new (uint, uint, uint, uint?)[] { (3, 13648, 13649, null), (4, 13650, 13651, null), (5, 13652, 13653, null),
                    (6, 13654, 13655, null), (7, 14861, 14862, null), (8, 20199, 20200, null), (9, 20199, 20200, null), (10, 20199, 20200, null), (11, 13654, 13655, null) }),
                (1522, 13586, new (uint, uint, uint, uint?)[] { (1, 13939, 13940, null), (2, 13941, 13942, null) }),
                (1856, 18911, new (uint, uint, uint, uint?)[] { (1, 19042, 19043, null), (2, 19184, 19185, null) }),
                // 1857: the client's 15 and 1-3, and its hidden 4 ("no desc") as the cells' tally; 5-14 are not written.
                (1857, 18916, new (uint, uint, uint, uint?)[] { (15, 19187, 19188, null), (1, 19061, 19062, null), (2, 19063, 19064, null),
                    (3, 19065, 19066, null), (4, 19067, 19068, null) }),
                (1881, 19215, new (uint, uint, uint, uint?)[] { (1, 19489, 19490, null) }),
                // 1899: the client's 7, 4, 5, 2 and 3; 6 ("hidden failure conditions objective") is not written.
                (1899, 19491, new (uint, uint, uint, uint?)[] { (7, 19643, 19644, null), (4, 19514, 19515, null), (5, 19516, 19517, null),
                    (2, 19498, 19499, null), (3, 19500, 19501, null) }),
                // 1398: the client's 1-5; hidden 6 (the waves' tally) with 4's texts.
                (1398, 12047, new (uint, uint, uint, uint?)[] { (1, 12691, 12692, null), (2, 12695, 12696, null), (3, 12698, 12699, null),
                    (4, 12704, 12705, null), (5, 12706, 12707, null), (6, 12704, 12705, null) }),
                // 1523: the client's 1; hidden 2 (the room) and 3 (the Thrax' tally) with 1's texts.
                (1523, 13591, new (uint, uint, uint, uint?)[] { (1, 14380, 14381, null), (2, 14380, 14381, null), (3, 14380, 14381, null) }),
                // 1525: the client has no objective; the one hidden objective takes the log and offer texts (13602, 13603).
                (1525, 13601, new (uint, uint, uint, uint?)[] { (1, 13602, 13603, null) }),
                // 1401: the client's 1-6 (3 hidden, no counter); hidden 7 (3's opener) with 3's texts, hidden 8-11 (the doors' takes,
                // their tracker and the Headsman's wake) with 5's.
                (1401, 12062, new (uint, uint, uint, uint?)[] { (1, 12660, 12661, null), (2, 12662, 12663, null), (3, 12664, 12665, null),
                    (4, 12667, 12668, null), (5, 12670, 12671, null), (6, 12693, 12694, null), (7, 12664, 12665, null), (8, 12670, 12671, null),
                    (9, 12670, 12671, null), (10, 12670, 12671, null), (11, 12670, 12671, null) })
            };
            using var harness = WildernessRuntimeTestHarness.Create();
            foreach (var (mission, title, objectives) in expected)
            {
                Assert.AreEqual(title, harness.World.MissionContentDefinitionEntries.AsNoTracking()
                    .Single(entry => entry.MissionId == mission && entry.ContentRevision == CrucibleMissionDataV1.Revision)
                    .ClientNameTextId, $"{mission} title");
                var actual = harness.World.MissionObjectiveDefinitionEntries.AsNoTracking()
                    .Where(entry => entry.MissionId == mission && entry.ContentRevision == CrucibleMissionDataV1.Revision).ToArray();
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

            // The talks at the client's packages (objectiveconversation 1142,1 1219; 1150,1 1245; 1145,1 1225; 1145,2 1227;
            // 1145,3 1229; 1145,5 1231; 1145,6 1454; 1161,1 1222; 1146,1 1224; 1146,2 1269; 1146,3 1271; 1146,4 1272; 1146,6
            // 1271; 1148,1 1296; 1148,2 1232), each with the client's variant 1, and Melching's 1148,3 1295 with each of the
            // client's three variants (177, 178, 203); set 3: 1170,1 1229; 1170,3 1225; 1170,5 1227; 1170,7 1231; 1229,1 1219; set 5:
            // 1185,1 1222 and 1185,3 1222; set 6: 1144,1 1304 (two choices); 1144,3 1304; 1144,4 1297; 1144,5 1301 (two choices);
            // 1144,6 1297 (two choices); 1144,8 1304; 1144,9 1297; 1152,1 1292; 1152,2 1301; 1153,1 1292 (two choices); 1155 and
            // 1205: 1 1229 (Munit), 2 1219 (DeLisi), 3 1293 (Breiburg); set 7: 1538,1 1742 (Cirkin); set 8a: 1210,1 1337
            // (Keplinger), 1217,1 and 1218,1 1483 (Tiernan), 1218,2 1410 (Botha); set 8b: 1215,1 and 1215,2 1453 (the Bane terminal);
            // set 11a: 1445,1 1640 (Sheira), 1444,1 1664 (Shanul), 1444,2 1727 (Paliv's body), 1444,3 1700 (Dhimand); set 12a:
            // 1577,1, 1521,5 and 1521,7 1800 (Brian); set 13: 1856,1 and 1899,3 2335 (Capriulo), 1856,2 and 1899,2 2337 (Eugin), 1857,15
            // 1231 (Rigs), 1899,7 2371 (Skeev: variant 1 for one choice, 27 for two), 1899,5 2372 (the terminal); set 11b: 1398,2 and
            // 1398,4 1700 (Dhimand), 1398,5 1640 (Sheira).
            var talks = harness.World.Set<MissionTriggerEntry>().AsNoTracking()
                .Where(entry => entry.ContentRevision == CrucibleMissionDataV1.Revision && entry.Kind == MissionTriggerKind.Conversation)
                .AsEnumerable().Select(entry => (entry.MissionId, entry.ObjectiveId, entry.NpcPackageId, entry.PlayerFlagId))
                .OrderBy(entry => entry.MissionId).ThenBy(entry => entry.ObjectiveId).ThenBy(entry => entry.PlayerFlagId).ToArray();
            CollectionAssert.AreEqual(new (uint, uint, uint?, uint?)[]
            {
                (1142, 1, 1219, 1), (1144, 1, 1304, 1), (1144, 1, 1304, 1), (1144, 3, 1304, 1), (1144, 4, 1297, 1), (1144, 5, 1301, 1),
                (1144, 5, 1301, 1), (1144, 6, 1297, 1), (1144, 6, 1297, 1), (1144, 8, 1304, 1), (1144, 9, 1297, 1), (1145, 1, 1225, 1), (1145, 2, 1227, 1), (1145, 3, 1229, 1), (1145, 5, 1231, 1), (1145, 6, 1454, 1),
                (1146, 1, 1224, 1), (1146, 2, 1269, 1), (1146, 3, 1271, 1), (1146, 4, 1272, 1), (1146, 6, 1271, 1),
                (1148, 1, 1296, 1), (1148, 2, 1232, 1), (1148, 3, 1295, 177), (1148, 3, 1295, 178), (1148, 3, 1295, 203),
                (1150, 1, 1245, 1), (1152, 1, 1292, 1), (1152, 2, 1301, 1), (1153, 1, 1292, 1), (1153, 1, 1292, 1),
                (1155, 1, 1229, 1), (1155, 2, 1219, 1), (1155, 3, 1293, 1), (1161, 1, 1222, 1),
                (1170, 1, 1229, 1), (1170, 3, 1225, 1), (1170, 5, 1227, 1), (1170, 7, 1231, 1), (1185, 1, 1222, 1), (1185, 3, 1222, 1),
                (1205, 1, 1229, 1), (1205, 2, 1219, 1), (1205, 3, 1293, 1), (1210, 1, 1337, 1), (1215, 1, 1453, 1), (1215, 2, 1453, 1), (1217, 1, 1483, 1), (1218, 1, 1483, 1), (1218, 2, 1410, 1),
                (1229, 1, 1219, 1), (1398, 2, 1700, 1), (1398, 4, 1700, 1), (1398, 5, 1640, 1), (1401, 4, 1664, 1), (1444, 1, 1664, 1), (1444, 2, 1727, 1), (1444, 3, 1700, 1), (1445, 1, 1640, 1),
                (1521, 5, 1800, 1), (1521, 7, 1800, 1), (1538, 1, 1742, 1), (1577, 1, 1800, 1),
                (1856, 1, 2335, 1), (1856, 2, 2337, 1), (1857, 15, 1231, 1),
                (1899, 2, 2337, 1), (1899, 3, 2335, 1), (1899, 5, 2372, 1), (1899, 7, 2371, 1), (1899, 7, 2371, 27), (1899, 7, 2371, 27)
            }, talks);
        }

        /// <summary>
        /// The chain 1142 -> 1150 -> 1145 -> 1161 -> 1146, 1147, 1148, 1149 and 1170 -> 1229, 1150 -> 1232 and 1241, 1149 ->
        /// 1203 -> 1204, 1147 -> 1202, 1148 -> 1208, 1170 -> 1185, 1144 -> 1152 -> 1155, 1153 -> 1205, 1229 -> 1519 -> 1529, and
        /// 1153 while 1152 is in the mission log, 1229 -> 1210 and 1212, 1212 -> 1213 -> 1215 -> 1216, 1538 -> 1217 -> 1218, 1224 -> 1214, 1147 -> 1445 -> 1444, 1577 -> 1521 -> 1522 -> 1523 -> 1525, level 36 -> 1856 -> 1857 -> 1881 -> 1899 (TaRapedia); The Departed, Whiplash, Calendar Girl, Pump Up The Jam, Rehabilitation at Gunpoint, Get to Chaukas and Leaky Pipes need none; 1206 and 1538
        /// take an either-or (their scenes' requirements, CrucibleStaalJunkyardTests); none repeats.
        /// </summary>
        [TestMethod]
        public void OnlyTheFollowUpsRequireAMission()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var rows = harness.World.MissionPrerequisiteEntries.AsNoTracking()
                .Where(entry => entry.ContentRevision == CrucibleMissionDataV1.Revision).AsEnumerable()
                .Select(entry => (entry.MissionId, entry.Kind, entry.RequiredMissionId)).OrderBy(entry => entry.MissionId).ToArray();
            CollectionAssert.AreEqual(new (uint, MissionPrerequisiteKind, uint?)[]
            {
                (1145, MissionPrerequisiteKind.MissionCompleted, 1150), (1146, MissionPrerequisiteKind.MissionCompleted, 1161),
                (1147, MissionPrerequisiteKind.MissionCompleted, 1161),
                (1148, MissionPrerequisiteKind.MissionCompleted, 1161), (1149, MissionPrerequisiteKind.MissionCompleted, 1161),
                (1150, MissionPrerequisiteKind.MissionCompleted, 1142), (1152, MissionPrerequisiteKind.MissionCompleted, 1144),
                (1153, MissionPrerequisiteKind.MissionAccepted, 1152), (1155, MissionPrerequisiteKind.MissionCompleted, 1152),
                (1161, MissionPrerequisiteKind.MissionCompleted, 1145),
                (1170, MissionPrerequisiteKind.MissionCompleted, 1161), (1185, MissionPrerequisiteKind.MissionCompleted, 1170),
                (1202, MissionPrerequisiteKind.MissionCompleted, 1147), (1203, MissionPrerequisiteKind.MissionCompleted, 1149),
                (1204, MissionPrerequisiteKind.MissionCompleted, 1203), (1205, MissionPrerequisiteKind.MissionCompleted, 1153),
                (1208, MissionPrerequisiteKind.MissionCompleted, 1148),
                (1210, MissionPrerequisiteKind.MissionCompleted, 1229), (1212, MissionPrerequisiteKind.MissionCompleted, 1229),
                (1213, MissionPrerequisiteKind.MissionCompleted, 1212), (1214, MissionPrerequisiteKind.MissionCompleted, 1224),
                (1215, MissionPrerequisiteKind.MissionCompleted, 1213),
                (1216, MissionPrerequisiteKind.MissionCompleted, 1215),
                (1217, MissionPrerequisiteKind.MissionCompleted, 1538), (1218, MissionPrerequisiteKind.MissionCompleted, 1217),
                (1229, MissionPrerequisiteKind.MissionCompleted, 1170),
                (1232, MissionPrerequisiteKind.MissionCompleted, 1150), (1241, MissionPrerequisiteKind.MissionCompleted, 1150),
                (1444, MissionPrerequisiteKind.MissionCompleted, 1445), (1445, MissionPrerequisiteKind.MissionCompleted, 1147),
                (1519, MissionPrerequisiteKind.MissionCompleted, 1229), (1521, MissionPrerequisiteKind.MissionCompleted, 1577),
                (1522, MissionPrerequisiteKind.MissionCompleted, 1521), (1523, MissionPrerequisiteKind.MissionCompleted, 1522),
                (1525, MissionPrerequisiteKind.MissionCompleted, 1523), (1529, MissionPrerequisiteKind.MissionCompleted, 1519),
                (1856, MissionPrerequisiteKind.PlayerLevelAtLeast, null), (1857, MissionPrerequisiteKind.MissionCompleted, 1856),
                (1881, MissionPrerequisiteKind.MissionCompleted, 1857), (1899, MissionPrerequisiteKind.MissionCompleted, 1881)
            }, rows);
            Assert.AreEqual((uint?)36U, harness.World.MissionPrerequisiteEntries.AsNoTracking()
                .Single(entry => entry.ContentRevision == CrucibleMissionDataV1.Revision && entry.MissionId == 1856).RequiredLevel, "1856: level 36");
            Assert.IsFalse(harness.World.Set<MissionRepeatPolicyEntry>().AsNoTracking()
                .Any(entry => entry.ContentRevision == CrucibleMissionDataV1.Revision));
        }

        /// <summary>The missions' sources and how far each is trusted: the client 1.0, TaRapedia 0.5, the reconstruction 0.3.</summary>
        [TestMethod]
        public void TheSourcesAreTrustedAsFarAsTheyGo()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var evidence = harness.World.MissionEvidenceEntries.AsNoTracking()
                .Where(entry => entry.ContentRevision == CrucibleMissionDataV1.Revision).ToArray();
            // 20271108000300 (to-fix rows 23-25) adds a reconstruction of the new marker places to 1147 and 1149;
            // 20271108000500 (to-fix rows 32, 34, 35) one of Munit's and Vargo's new places to 1145, 1146 and 1170;
            // 20271108000800 (to-fix rows 43, 44) one of the bot pools' and Vargo's new places to 1147, 1203, 1204, 1202 and 1146.
            // 20271108001000 (fix job F) one to 1232, 1147, 1203, 1204, 1202, 1157, 1149, 1208 and 1185; 20271108001200 (fix job G)
            // one to 1208; 20271108001500 (fix job H) one to 1529; 20271108001900 (fix job I) one to 1445, 1402 and 1521;
            // 20271108002200 (fix job J) one to 1232. 1401's documentation is 0.7: TaRapedia's route checked on the client's
            // geometry (NOTES-crucible-research-group4.md "1401 verify").
            int Fixes(uint mission) => (mission is 1147 or 1149 or 1145 or 1146 or 1170 ? 1 : 0) + (mission is 1147 or 1203 or 1204 or 1202 or 1146 ? 1 : 0)
                + (CrucibleFixFV1.EvidenceIds.Any(id => id.Mission == mission) ? 1 : 0) + (mission == CrucibleOutOfSightV1.MissionId ? 1 : 0)
                + (mission == CruciblePartOfTheBargainV1.MissionId ? 1 : 0) + (CrucibleFixIV1.Missions.Contains(mission) ? 1 : 0)
                + (mission == CrucibleMinesweeperV1.MissionId ? 1 : 0);
            foreach (var mission in MissionIds)
                CollectionAssert.AreEqual(new[]
                    {
                        (MissionEvidenceSourceKind.Client, 1.0), (MissionEvidenceSourceKind.Documentation, mission == CrucibleBloodFireSteelV1.MissionId ? 0.7 : 0.5),
                        (MissionEvidenceSourceKind.Reconstruction, 0.3)
                    }.Concat(Enumerable.Repeat((MissionEvidenceSourceKind.Reconstruction, 0.3), Fixes(mission))).ToArray(),
                    evidence.Where(entry => entry.MissionId == mission).OrderBy(entry => entry.EvidenceId)
                        .Select(entry => (entry.SourceKind, entry.Confidence)).ToArray(), $"{mission} evidence");
        }

        /// <summary>
        /// The rewards: XP and credits of TaRapedia, the items (or stacks) to choose one of, then each Grupa's syndicate token,
        /// which comes with every choice (written out here).
        /// </summary>
        [TestMethod]
        public void TheRewardsAreTaRapediasWithOneItemToChoose()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            foreach (var (mission, experience, credits, items) in new (uint, uint, uint, (uint, uint)[])[]
            {
                (1142, 37500, 3800, new[] { (111232U, 2U), (118907U, 2U), (111038U, 4U), (111028U, 4U) }),
                (1143, 75000, 5700, new[] { (19802U, 1U), (34116U, 1U), (36488U, 1U), (24474U, 1U) }),
                (1150, 75000, 5700, new[] { (39985U, 1U), (25119U, 1U), (21100U, 1U), (31547U, 1U) }),
                (1145, 75000, 5700, new[] { (33729U, 1U), (20802U, 1U), (23569U, 1U), (30167U, 1U) }),
                (1161, 40000, 3900, new[] { (111018U, 3U), (45060U, 6U), (45446U, 3U), (111232U, 1U) }),
                (1146, 75000, 0, new[] { (14444U, 1U), (14815U, 1U), (50314U, 1U) }),
                (1148, 75000, 0, new[] { (19524U, 1U), (25357U, 1U), (50316U, 1U) }),
                (1149, 75000, 0, new[] { (20355U, 1U), (28634U, 1U), (33193U, 1U), (35591U, 1U), (50317U, 1U) }),
                (1147, 75000, 0, new[] { (19395U, 1U), (39061U, 1U), (50315U, 1U) }),
                (1157, 75000, 5700, new[] { (49140U, 1U), (15513U, 1U), (15301U, 1U), (15025U, 1U) }),
                (1170, 160000, 7800, new[] { (49592U, 1U), (15542U, 1U), (49136U, 1U), (15295U, 1U) }),
                (1229, 40000, 3900, new[] { (111028U, 3U), (47599U, 1U), (111018U, 3U), (45060U, 6U) }),
                (1232, 75000, 5700, new[] { (33342U, 1U), (38823U, 1U), (25248U, 1U), (30474U, 1U) }),
                (1241, 75000, 5700, new[] { (19673U, 1U), (33600U, 1U), (24861U, 1U), (36188U, 1U) }),
                (1203, 80000, 5850, new[] { (15470U, 1U), (15247U, 1U), (49136U, 1U), (14610U, 1U) }),
                (1204, 160000, 7800, new[] { (123041U, 1U), (122986U, 1U), (122961U, 1U), (123176U, 1U) }),
                (1202, 80000, 5850, new[] { (47688U, 1U), (48104U, 1U), (15437U, 1U), (14569U, 1U) }),
                (1208, 80000, 5850, new[] { (32806U, 1U), (10637U, 1U), (20484U, 1U), (29533U, 1U) }),
                (1185, 160000, 3900, new[] { (111048U, 3U), (45061U, 3U), (111038U, 3U), (45062U, 2U) }),
                (1144, 75000, 5700, new[] { (49596U, 1U), (15189U, 1U), (48712U, 1U), (14861U, 1U) }),
                (1152, 75000, 5700, new[] { (45796U, 1U), (15550U, 1U), (15442U, 1U), (14699U, 1U) }),
                (1153, 75000, 5700, new[] { (15475U, 1U), (15588U, 1U), (48680U, 1U), (49140U, 1U) }),
                (1155, 80000, 5850, new[] { (18899U, 1U), (23828U, 1U), (32826U, 1U), (38952U, 1U) }),
                (1205, 80000, 5850, new[] { (19157U, 1U), (33084U, 1U), (40373U, 1U), (36638U, 1U) }),
                (1206, 160000, 7800, new[] { (12105U, 1U), (12143U, 1U), (12027U, 1U), (11973U, 1U) }),
                (1519, 90000, 6150, new[] { (14658U, 1U), (49172U, 1U), (15301U, 1U), (15111U, 1U) }),
                (1529, 180000, 8200, new[] { (25119U, 1U), (21400U, 1U), (36339U, 1U), (46021U, 1U) }),
                // Not My Junk: the Eloh Resonator only, with no choice (TaRapedia).
                (1538, 85000, 0, new[] { (117177U, 1U) }),
                // Set 8a: TaRapedia's items by their client names (1210, 1217) or the band's Uncommon templates (1212, 1218).
                (1210, 42500, 4000, new[] { (47600U, 1U), (111019U, 3U), (45063U, 6U), (45447U, 3U) }),
                (1212, 170000, 8000, new[] { (40482U, 1U), (46004U, 1U), (36767U, 1U), (29994U, 1U) }),
                (1217, 42500, 4000, new[] { (111233U, 1U), (111049U, 3U), (45064U, 3U), (111039U, 3U) }),
                (1218, 85000, 6000, new[] { (33987U, 1U), (24086U, 1U), (20971U, 1U), (28940U, 1U) }),
                // Set 8b: the lowest template of each kind whose Uncommon band holds level 35 (1215's cipher tool: quality 2).
                (1213, 170000, 8000, new[] { (47648U, 1U), (14855U, 1U), (119370U, 1U), (14774U, 1U) }),
                (1215, 85000, 6000, new[] { (15582U, 1U), (14691U, 1U), (3853U, 1U), (49136U, 1U) }),
                (1216, 85000, 2000, new[] { (47628U, 1U), (120030U, 1U), (47808U, 1U), (48256U, 1U) }),
                // Set 9: 1224 the lowest template of each kind whose Uncommon band holds level 36; 1214 the lowest Uncommon of each
                // kind and the nearest icon number in the band of level 35.
                (1224, 85000, 6000, new[] { (15181U, 1U), (49136U, 1U), (18260U, 1U), (14444U, 1U) }),
                (1214, 170000, 8000, new[] { (40611U, 1U), (25099U, 1U), (21380U, 1U), (28614U, 1U) }),
                // Set 11a: 1445 TaRapedia's stacks by their client names (as 1210); 1444 the lowest Uncommon of each kind and the
                // nearest icon number in the band of level 40; 1402 the grade [4] modules (as 1204).
                (1445, 42500, 4000, new[] { (45063U, 6U), (45447U, 3U), (111233U, 1U), (111049U, 3U) }),
                (1444, 170000, 8000, new[] { (34116U, 1U), (40373U, 1U), (21249U, 1U), (23569U, 1U) }),
                (1402, 180000, 8200, new[] { (122971U, 1U), (123046U, 1U), (123041U, 1U), (123276U, 1U) }),
                // Set 12a: 1577 the lowest Uncommon of each kind and the nearest icon number in the band of level 40; 1521 the lowest
                // Uncommon of each kind in the band of level 35; 1522 the grade [4] modules (as 1402).
                (1577, 90000, 8200, new[] { (33858U, 1U), (24086U, 1U), (21699U, 1U), (31241U, 1U) }),
                (1521, 90000, 8200, new[] { (47688U, 1U), (48676U, 1U), (15437U, 1U), (14815U, 1U) }),
                (1522, 85000, 8000, new[] { (122951U, 1U), (122986U, 1U), (123001U, 1U), (123006U, 1U) }),
                // Set 13: 1856 TaRapedia's consumables by their client rows (class VII / 07, level 35; the stacks as 1142); 1857, 1881
                // the client's Uncommon rows of TaRapedia's kinds and levels; 1899 none (TR MMO DB 0 XP, 0 credits; the Thrax hybrid).
                (1856, 37500, 3800, new[] { (45062U, 2U), (118907U, 2U), (111048U, 4U), (111038U, 4U) }),
                (1857, 75000, 5700, new[] { (14780U, 1U), (49168U, 1U), (48396U, 1U), (15301U, 1U) }),
                (1881, 150000, 7600, new[] { (23678U, 1U), (29247U, 1U), (36188U, 1U), (20931U, 1U) }),
                (1899, 0, 0, new (uint, uint)[0]),
                // Set 11b: 1398 no items (no source has any; ESTIMATE: 1444's XP and credits).
                (1398, 170000, 8000, new (uint, uint)[0]),
                // Set 12b: TaRapedia's stacks by their client names, Class VIII (as 1445).
                (1523, 45000, 4100, new[] { (111233U, 2U), (45063U, 3U), (111039U, 3U), (47600U, 3U) }),
                (1525, 42500, 6000, new[] { (45064U, 3U), (111039U, 3U), (45065U, 2U), (111029U, 3U) }),
                // Set 15: 1401 no items (no source; ESTIMATE: 1398's and 1444's XP and credits).
                (1401, 170000, 8000, new (uint, uint)[0])
            })
            {
                var reward = harness.World.Set<MissionRewardDefinitionEntry>().AsNoTracking()
                    .Single(entry => entry.MissionId == mission && entry.ContentRevision == CrucibleMissionDataV1.Revision);
                Assert.AreEqual((experience, credits, (byte)(mission is 1538 or 1899 or 1398 or 1401 ? 0 : 1)), (reward.Experience, reward.Credits, reward.SelectionCount), $"{mission}");
                CollectionAssert.AreEqual(items, harness.World.MissionRewardItemEntries.AsNoTracking()
                    .Where(entry => entry.MissionId == mission && entry.ContentRevision == CrucibleMissionDataV1.Revision)
                    .OrderBy(entry => entry.ItemId).AsEnumerable().Select(entry => (entry.ItemTemplateId, entry.Quantity)).ToArray(), $"{mission} items");
            }
            // The tokens (classes 24749, 24750, 24751, 24752: Larai, Viddia, Top Khana, Labbna Syndicate Token) are fixed: no choice.
            var fixedItems = harness.World.MissionRewardItemEntries.AsNoTracking()
                .Where(entry => entry.ContentRevision == CrucibleMissionDataV1.Revision && entry.Kind == MissionRewardItemKind.Fixed).AsEnumerable()
                .Select(entry => (entry.MissionId, entry.ItemTemplateId)).OrderBy(entry => entry.MissionId).ToArray();
            CollectionAssert.AreEqual(new[] { (1146U, 50314U), (1147U, 50315U), (1148U, 50316U), (1149U, 50317U), (1538U, 117177U) }, fixedItems);
        }

        /// <summary>
        /// The indicators: on the Crucible map (MissionMarkerProjection), but for the ones in the Staal Junkyard (2138): 1519's
        /// on Cirkin, all of 1529's, 1538's on Cirkin, Lamontix and the lockbox; for the ones in the Incurables Ward (2141): 1445's
        /// on Sheira, all of 1444's and 1398's, 1402's on the bosses and the pools; for the ones in Chaukas (2146): 1577's on Brian, all of
        /// 1521's, 1522's and 1523's; on Ashen Desert (1734) 1218's on Botha; keyed
        /// above UnnamedFrom (the client names none of them).
        /// </summary>
        [TestMethod]
        public void IndicatorsAreOnTheirMapsAndAboveUnnamed()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var indicators = harness.World.MissionIndicatorEntries.AsNoTracking()
                .Where(entry => entry.ContentRevision == CrucibleMissionDataV1.Revision).ToArray();
            Assert.HasCount(Indicators, indicators);
            var catalog = new MissionContentCatalog(harness, new Dictionary<uint, Mission>(), null);
            catalog.Load();
            bool InTheJunkyard(uint mission, uint indicator) => mission == 1529 ||
                mission == 1519 && indicator == MissionIndicator.UnnamedFrom + 3 ||
                mission == 1538 && indicator is MissionIndicator.UnnamedFrom + 2 or MissionIndicator.UnnamedFrom + 3 or MissionIndicator.UnnamedFrom + 4;
            bool OnAshenDesert(uint mission, uint indicator) => mission == 1218 && indicator == MissionIndicator.UnnamedFrom + 2;
            bool InTheWard(uint mission, uint indicator) => mission is 1444 or 1398 or 1401 ||
                mission == 1445 && indicator == MissionIndicator.UnnamedFrom + 2 ||
                mission == 1402 && indicator <= MissionIndicator.UnnamedFrom + 11;
            Assert.AreEqual(1 + 10 + 3, indicators.Count(row => InTheJunkyard(row.MissionId, row.IndicatorId)));
            bool InChaukas(uint mission, uint indicator) => mission is 1521 or 1522 or 1523 ||
                mission == 1577 && indicator == MissionIndicator.UnnamedFrom + 2;
            Assert.AreEqual(1 + 3 + 11 + 5 + 6, indicators.Count(row => InTheWard(row.MissionId, row.IndicatorId)));
            Assert.AreEqual(1 + 8 + 2 + 1, indicators.Count(row => InChaukas(row.MissionId, row.IndicatorId)));
            Assert.AreEqual(1, indicators.Count(row => OnAshenDesert(row.MissionId, row.IndicatorId)));
            foreach (var row in indicators)
            {
                var map = InTheJunkyard(row.MissionId, row.IndicatorId) ? 2138U : InTheWard(row.MissionId, row.IndicatorId) ? 2141U
                    : InChaukas(row.MissionId, row.IndicatorId) ? 2146U : OnAshenDesert(row.MissionId, row.IndicatorId) ? 1734U : 1993U;
                Assert.AreEqual(map, MissionMarkerProjection.ResolveMap(catalog, row.MissionId, row.ObjectiveId,
                    new MissionIndicator { IndicatorId = row.IndicatorId }), $"mission {row.MissionId} objective {row.ObjectiveId}: on map {map}");
                Assert.IsTrue(row.IndicatorId > MissionIndicator.UnnamedFrom, $"mission {row.MissionId} objective {row.ObjectiveId} indicator {row.IndicatorId}");
            }
        }

        /// <summary>Every visible Crucible objective has a marker; hidden objectives have none.</summary>
        [TestMethod]
        public void EveryVisibleObjectiveHasAMarker()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var world = harness.World;
            var revision = CrucibleMissionDataV1.Revision;
            var marked = world.MissionIndicatorEntries.AsNoTracking().Where(entry => entry.ContentRevision == revision)
                .Select(entry => new { entry.MissionId, entry.ObjectiveId }).ToList()
                .Select(entry => (entry.MissionId, entry.ObjectiveId)).ToHashSet();
            var hidden = world.Set<MissionSceneBindingEntry>().AsNoTracking().Where(entry => entry.ContentRevision == revision).ToList()
                .SelectMany(entry => System.Text.RegularExpressions.Regex.Match(entry.Bindings, "\"HiddenObjectiveIds\"\\s*:\\s*\\[([^\\]]*)\\]",
                        System.Text.RegularExpressions.RegexOptions.IgnoreCase).Groups[1].Value
                    .Split(',', System.StringSplitOptions.RemoveEmptyEntries).Select(id => (entry.MissionId, uint.Parse(id.Trim()))))
                .ToHashSet();
            CollectionAssert.AreEquivalent(new[] { (1143U, 22U), (1144U, 7U), (1529U, 5U), (1529U, 6U), (1529U, 7U), (1217U, 2U), (1521U, 9U), (1521U, 10U), (1521U, 11U), (1857U, 4U), (1398U, 6U),
                    (1523U, 2U), (1523U, 3U), (1525U, 1U),
                (1401U, 3U), (1401U, 7U), (1401U, 8U), (1401U, 9U), (1401U, 10U), (1401U, 11U) }, hidden.ToArray(),
                "The Departed's item tracker, Calendar Girl's outcome, Your Part of the Bargain's tracker, its opener and its block count, Point Of Impact's walk to the obelisk, " +
                "Control the Brain's tracker, its opener and its kill tally, The Gene Machine's cell tally, Leaky Pipes' wave tally, " +
                "Factory Fortification's room and Thrax tally and Information is Key's one objective (the client has none) and Blood, Fire, and Steel's card counter, its opener, the doors' takes, their tracker and its opener are hidden");
            var objectives = world.MissionObjectiveDefinitionEntries.AsNoTracking().Where(entry => entry.ContentRevision == revision)
                .Select(entry => new { entry.MissionId, entry.ObjectiveId }).ToList();
            Assert.HasCount(9 + 1 + 6 + 3 + 1 + 3 + 1 + 8 + 1 + 1 + 1 + 2 + 3 + 1 + 1 + 3 + 8 + 2 + 1 + 3 + 3 + 3 + 2 + 7 + 4 + 1 + 1 + 2 + 2 + 1 + 2 + 1 + 6 + 3 + 1 + 3 + 3 + 1 + 9 + 2 + 2 + 5 + 1 + 5 + 6 + 3 + 1 + 11, objectives, "the objectives are read");
            var missing = objectives.Select(entry => (entry.MissionId, entry.ObjectiveId))
                .Where(key => !marked.Contains(key) && !hidden.Contains(key)).OrderBy(key => key).ToArray();
            Assert.IsEmpty(missing, string.Join(", ", missing));
            Assert.IsFalse(marked.Any(hidden.Contains), "no marker on a hidden objective");
        }

        /// <summary>The markers stand at their objective's target: an NPC, the terminal, the field of the fallen.</summary>
        [TestMethod]
        public void TheMarkersStandAtTheirObjectivesTargets()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var rows = harness.World.MissionIndicatorEntries.AsNoTracking()
                .Where(entry => entry.ContentRevision == CrucibleMissionDataV1.Revision).ToList();
            var expected = new List<(uint Mission, uint Objective, double X, double Z, double Radius)>
            {
                (1142, 1, 952, 106, 8),                             // DeLisi
                (1143, 1, 802.3, 570, 70),                          // round the twenty fallen
                (1150, 1, 809.74, 676.74, 10),                      // the terminal
                (1145, 6, 292.49, 736.36, 8), (1145, 1, -300, 320, 8), (1145, 2, -216, 472, 8), (1145, 3, -218, 228, 8),
                (1145, 5, -392.77, 276.23, 8),                      // Mullins, Vitto, Donal, Munit, Rigs
                (1161, 1, -215, 326, 8), (1161, 1, -206.738, 317.945, 4),                   // Lohen, the lower office teleporter
                (1146, 1, -197, 319, 8), (1146, 2, -174.3, 280.2, 8), (1146, 3, -186, 270, 8), (1146, 4, -153.7, 346.9, 5),
                (1146, 5, -214, 258, 15), (1146, 6, -186, 270, 8),                    // Bunko, Cugin, Vargo, Yeg, Roscoe, Vargo
                (1148, 1, 959, 111, 8), (1148, 2, -400, 270, 8), (1148, 3, -945.9, 246.9, 8), // Fiorella, Haggus, Melching
                (1149, 1, -452, 58, 25), (1149, 1, -518, 64, 25), (1149, 1, -368, 70, 25),                  // the Flaregasher pools (fix job F: 540141 off the lava)
                (1147, 1, -575, 491, 20), (1147, 1, -499, 434, 20), (1147, 1, -636, 543, 20),     // the Warden Bot pools (20271108000800; fix job F: 770011)
                (1147, 2, -575, 491, 20), (1147, 2, -499, 434, 20), (1147, 2, -636, 543, 20),
                (1147, 3, -575, 491, 20), (1147, 3, -499, 434, 20), (1147, 3, -636, 543, 20),
                (1157, 1, -752, 368, 20), (1157, 1, -780, 356, 20),                                          // the Lasher pools (fix job F)
                (1170, 1, -218, 228, 8), (1170, 2, -218, 228, 8), (1170, 3, -300, 320, 8), (1170, 4, -300, 320, 8),
                (1170, 5, -216, 472, 8), (1170, 6, -216, 472, 8), (1170, 7, -392.77, 276.23, 8), (1170, 8, -392.77, 276.23, 8),
                (1229, 1, 952, 106, 8),                                                                      // DeLisi
                (1232, 1, 635, 557, 20), (1232, 1, 585, 577, 20), (1232, 1, 545, 587, 20),                   // the mine pools
                (1241, 1, 859, 476, 15),                                                                     // Derge on the rampart
                (1202, 1, -617, 570, 20), (1202, 1, -545, 486, 20), (1202, 1, -490, 480, 20), (1202, 1, -454, 495, 20), // the Reconstructor pools
                (1208, 1, 415, 113, 20), (1208, 1, 490, 65, 20), (1208, 1, 535, 80, 20),                    // the Shield Drone pools
                (1185, 1, -215, 326, 8), (1185, 2, -853, 359, 20), (1185, 3, -215, 326, 8),                 // Lohen, Molitor, Lohen
                (1144, 1, -230, 324, 8), (1144, 8, -230, 324, 8), (1144, 3, -230, 324, 8),                  // Deirdre
                (1144, 5, 975, 110, 8), (1144, 6, 969, 108, 8), (1144, 4, 969, 108, 8), (1144, 9, 969, 108, 8), // D'Ambrose, Sherer
                (1152, 1, -953, 255, 8), (1152, 2, 975, 110, 8), (1153, 1, -953, 255, 8),                   // Cheung, D'Ambrose, Cheung
                (1155, 1, -218, 228, 8), (1155, 2, 952, 106, 8), (1155, 3, -942.2, 246.9, 8),               // Munit, DeLisi, Breiburg
                (1205, 1, -218, 228, 8), (1205, 2, 952, 106, 8), (1205, 3, -942.2, 246.9, 8),
                // Set 7: the rock bridge (1206); Kalamit, the Junkyard bridge road, Cirkin (1519); the Technician pools, the six
                // blocks, the Ultra Reconstructor, Cirkin (1529); the bridge road, Cirkin, Lamontix, the lockbox, Cheung (1538).
                (1206, 1, -796, 528, 60), (1206, 2, -796, 528, 60), (1206, 3, -796, 528, 60),
                (1519, 1, -486, 120, 20), (1519, 2, -492, -115, 8), (1519, 2, 187, -233, 10),
                (1529, 1, 330, -160, 15), (1529, 1, 306, -106, 15), (1529, 2, 306, -65, 3), (1529, 2, 325, -22, 3), (1529, 2, 225, -172, 3),
                (1529, 2, 231, -48, 3), (1529, 2, 28, -219, 3), (1529, 2, -331, -36, 3), (1529, 3, 164, -76, 15) /* fix job H */, (1529, 4, 187, -233, 10),
                (1538, 1, -492, -115, 8), (1538, 1, 187, -233, 10), (1538, 2, 350, -52, 10), (1538, 4, 343, -68, 3), (1538, 3, -953, 255, 10),
                // Set 8a: Keplinger (1210); the west and north trenches (1212); Tiernan (1217, 1218); Botha on Ashen Desert (1218).
                (1210, 1, 379, 384, 8), (1212, 1, 320, -128.5, 35), (1212, 1, 388, -26, 35), (1217, 1, 589.5, 250, 8),
                (1218, 1, 589.5, 250, 8), (1218, 2, 378, -458.5, 8),
                // Set 8b: the Juggernaut pools (1213); the Bane terminal (1215); Ugoretz (1216).
                (1213, 1, 383, -185, 15), (1213, 1, 440, -190, 15), (1213, 1, 310, -230, 15),
                (1215, 1, 390, -236, 10), (1215, 2, 390, -236, 10), (1216, 1, 340, -130, 15),
                // Set 9: the six jamming towers on the rim of Khulago Base (1224); the three beacon spots inside it (1214).
                (1224, 1, 433, 2, 10), (1224, 2, 523, -68, 10), (1224, 3, 523, -208, 10), (1224, 4, 363, -368, 10),
                (1224, 5, 323, -258, 10), (1224, 6, 233, -158, 10),
                (1214, 1, 403, -168, 8), (1214, 2, 363, -208, 8), (1214, 3, 383, -268, 8),
                // Set 11a: before the Ward's door on the Crucible (fix job I: 8.5 m outside the door's circle, r3) and Sheira (1445);
                // Shanul, Paliv's body, Dhimand (1444); Jaikev, Parvatee, the nine Incurable pools and the door for each objective (1402).
                (1445, 1, -216.8, 405.2, 3), (1445, 1, -18, -88, 8),
                (1444, 1, 60, -70, 8), (1444, 2, 94, 421, 8), (1444, 3, -40, 386, 8),
                (1402, 1, -98, 332, 10), (1402, 2, -90, 340, 10), (1402, 1, -216.8, 405.2, 3), (1402, 2, -216.8, 405.2, 3), (1402, 3, -216.8, 405.2, 3),
                // Set 11b: Jaikev, Dhimand, the water system, Dhimand's repair place, Sheira (1398).
                (1398, 1, -98, 332, 10), (1398, 2, -40, 386, 8), (1398, 3, -366, 212, 8), (1398, 4, -366, 208, 15), (1398, 5, -18, -88, 8),
                // Set 15: Atali, Umank, Shanul, the north doors, the Headsman, before the Sanctum door (1401).
                (1401, 1, -18, -6, 10), (1401, 2, 202, 210, 10), (1401, 4, 60, -70, 8), (1401, 5, 78, 45, 8), (1401, 5, 78, 62, 10), (1401, 6, 94, 397, 6),
                // Set 12a: before the Chaukas door on the Crucible and Brian (1577); the Brain room's mouth, its three Claviger pools
                // (their radius and 4 m), Brian at the Brain (7, 5, 6), the container (1521); the two switches (1522).
                (1577, 1, -528.6, 563.6, 6), (1577, 1, -38, -256, 8),
                (1521, 3, -14, 4, 8), (1521, 8, -16, 10.1, 10), (1521, 8, -15.6, 114.5, 10.5), (1521, 8, 23.6, 37.8, 19),
                (1521, 7, -24, 38, 8), (1521, 4, -16.4, -166.6, 3) /* fix job I: the container's south side */, (1521, 5, -24, 38, 8), (1521, 6, -24, 38, 15),
                (1522, 1, 88, -176, 3), (1522, 2, -146, -55, 3),
                // Set 12b: the factory room's floor, east of the factory (1523).
                (1523, 1, 120, -150, 12),
                // Set 13: Capriulo and Eugin (1856); Rigs and round the salvage yard's eleven piles (1857); Molitor, the rock bridge
                // trench, Ugoretz and Derge (1881); the tunnel pad, Skeev (7 and 4), the terminal, Eugin and Capriulo (1899).
                (1856, 1, -303, 376, 8), (1856, 2, -232, 473, 8),
                (1857, 15, -392.77, 276.23, 8), (1857, 1, -424, 252, 45), (1857, 2, -424, 252, 45), (1857, 3, -424, 252, 45),
                (1881, 1, -853, 359, 20), (1881, 1, -796, 528, 60), (1881, 1, 340, -130, 15), (1881, 1, 859, 476, 15),
                (1899, 7, 114, -370, 4), (1899, 7, -136, -288, 8), (1899, 4, -136, -288, 15), (1899, 5, -131, -284, 6),
                (1899, 2, -232, 473, 8), (1899, 3, -303, 376, 8)
            };
            foreach (var (x, z) in new[] { (76.0, 90.0), (76.0, 132.0), (94.0, 178.0), (124.0, 184.0), (-22.0, 416.0), (-40.0, 464.0), (94.0, 488.0),
                (-90.0, 316.0), (-82.0, 348.0) })
                expected.Add((1402, 3, x, z, 15));                                                           // the Incurable pools
            // Salvage Duty and More Salvage Duty: each part objective on each of the five bot pools.
            foreach (var (mission, objective) in new (uint, uint)[] { (1203, 1), (1203, 2), (1204, 3), (1204, 4), (1204, 5) })
                foreach (var (x, z) in new[] { (-575.0, 491.0), (-499.0, 434.0), (-636.0, 543.0), (-617.0, 570.0), (-545.0, 486.0) }) // fix job F: 770011
                    expected.Add((mission, objective, x, z, 20));
            Assert.HasCount(Indicators, expected);
            foreach (var (mission, objective, x, z, radius) in expected)
                Assert.IsTrue(rows.Any(row => row.MissionId == mission && row.ObjectiveId == objective &&
                        Vector2.Distance(new((float)row.PosX, (float)row.PosZ), new((float)x, (float)z)) < 0.1f && row.Radius == radius),
                    $"{mission} objective {objective}'s marker at ({x}, {z}) radius {radius}");
            Assert.HasCount(Indicators, rows);

            // The moved Flaregasher pools under 1149's markers; the fallen inside their marker, 10 m in from its edge.
            foreach (var (pool, _, place) in CrucibleMarkersClearOfLinksV2.Pools.Take(3))
            {
                var now = CrucibleFixFV1.Now(pool) ?? place; // fix job F moved 540141 off the lava
                Assert.IsTrue(rows.Any(row => row.MissionId == 1149 && Vector2.Distance(new((float)row.PosX, (float)row.PosZ), new((float)now.X, (float)now.Z)) < 0.1f),
                    $"pool {pool} under a marker");
            }
            foreach (var at in CrucibleDepartedV1.CorpsesAt)
                Assert.IsLessThan(CrucibleDepartedV1.FieldRadius - 10, Vector2.Distance(new(at.X, at.Z), new(802.3f, 570f)), $"a corpse at ({at.X}, {at.Z}) in the marker");
            // The Gene Machine's eleven piles inside the yard's marker, 4 m in from its edge.
            foreach (var (_, _, at, _) in CrucibleGeneMachineV1.Piles)
                Assert.IsLessThan(CrucibleGeneMachineV1.YardRadius - 4, Vector2.Distance(new(at.X, at.Z), new(-424f, 252f)), $"a pile at ({at.X}, {at.Z}) in the marker");
        }

        /// <summary>No scene object of set 1 is of a class the client draws as its "Default" pyramid (mesh 776).</summary>
        [TestMethod]
        public void NoSceneObjectIsOfAClassTheClientDrawsAsDefault()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var meshes = harness.World.EntityClassEntries.AsNoTracking().ToDictionary(entry => entry.Id, entry => entry.MeshId);
            var objects = 0;
            foreach (var scene in Scenes(harness))
                foreach (var actor in scene.Scene.Actors.Values.Where(actor => actor.Kind == Rasa.Missions.Scenes.SceneActorKind.Object))
                {
                    objects++;
                    Assert.IsTrue(meshes.TryGetValue(actor.TemplateId, out var mesh), $"{scene.MissionId} {actor.Role}: class {actor.TemplateId}");
                    Assert.AreNotEqual(776U, mesh, $"{scene.MissionId} {actor.Role}: class {actor.TemplateId} has the Default mesh");
                }
            Assert.AreEqual(20 + 1 + 6 + 1 + 1 + 2 + 6 + 3 + 1 + 1 + 2 + 11 + 1 + 3, objects,
                "The Departed's twenty fallen, the terminal, 1529's six Dolomite Blocks, 1538's lockbox, 1217's obelisk, 1215's two Bane terminals, " +
                "1224's six jamming towers, 1214's three beacon spots, 1444's Paliv, 1521's container, 1522's two switches, 1857's eleven piles, " +
                "1899's terminal and 1401's three doors");
        }

        /// <summary>
        /// The use argument the client sends for each usable class of set 1 (usabledata lookup[class][0], or 1 if it is
        /// None: Usable.__init__), written out from the client table: the human soldier corpses' entries are None.
        /// </summary>
        internal static readonly Dictionary<uint, uint> ClientUseArguments = new()
        {
            [24995] = 1, [7865] = 1, [24989] = 1, [24990] = 1,     // UsableTreasureDispHumCorpseMaleV01-V04 (1143; lookup[0] None, the client sends 1)
            [7007] = 3,                                             // UsableItemDispCrystalspecimenCONVERT2TD (1529's Dolomite Blocks)
            [11116] = 3,                                            // UsableItemDisp_item_crate_bane_v01CONVERT2TD (1538's Bane Lockbox)
            [7847] = 3,                                             // UsableItemAccHumGpsBeacon (1214's beacon spots; lookup[0] 3, as MarshesMissionTests')
            [26580] = 1,                                            // UsableTreasureDispWardenbotCrateV01 (1521's container; lookup[0] None)
            [26578] = 1,                                            // UsableTwoStateWardenbotGenericSwitchV01 (1522's switches; lookup[0] None)
            [7931] = 1,                                             // UsableTreasureDispBrannCrateV01 (1857's piles; lookup[0] None)
            [7934] = 1,                                             // UsableTreasureDispBrannCrateV02 (1857's piles; lookup[0] None)
            [24980] = 1,                                            // UsableTreasureDispBrannConsoleV01 (1857's two consoles; lookup[0] None)
            [10749] = 1,                                            // UsableDoorBrannCorridor04mV01 (1401's three doors; lookup all None)
        };

        /// <summary>
        /// The client's usablenameoverride name of each usable class: 12 "Fort Intrepid Casualty", 166 "Dolomite Block", 175 "Bane
        /// Lockbox"; 7847 none (0): its own class name "GPS Beacon" (physicalentityclassnamelanguage).
        /// </summary>
        private static readonly Dictionary<uint, uint> ClientUseNames = new()
        {
            [24995] = 12, [7865] = 12, [24989] = 12, [24990] = 12, [7007] = 166, [11116] = 175, [7847] = 0, [26580] = 189,
            [7931] = 272, [7934] = 272, [24980] = 272, [10749] = 0
        };

        /// <summary>The names of a class the scenes use under two names, by role: 190 "Factory Reset Switch", 191 "Ore Machine Override Switch" (1522, class 26578).</summary>
        private static readonly Dictionary<string, uint> ClientRoleUseNames = new()
        {
            [CrucibleFixTheFactoryV1.FactoryRole] = 190, [CrucibleFixTheFactoryV1.OreRole] = 191
        };

        [TestMethod]
        public void EveryUsableSceneObjectTakesTheClientsUseArgument()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var used = 0;
            foreach (var scene in Scenes(harness))
                foreach (var actor in scene.Scene.Actors.Values.Where(actor => actor.UseAction != null))
                {
                    used++;
                    Assert.IsTrue(ClientUseArguments.TryGetValue(actor.TemplateId, out var argument),
                        $"{scene.MissionId} {actor.Role}: class {actor.TemplateId} has no pinned client use argument");
                    Assert.AreEqual(argument, actor.UseAction.ActionArgId,
                        $"{scene.MissionId} {actor.Role}: class {actor.TemplateId} is used with the client's argument");
                    Assert.AreEqual(ClientRoleUseNames.TryGetValue(actor.Role, out var byRole) ? byRole : ClientUseNames[actor.TemplateId],
                        actor.NameOverrideId, $"{actor.Role}: the client's name");
                }
            Assert.AreEqual(20 + 6 + 1 + 3 + 1 + 2 + 11 + 3, used,
                "The Departed's twenty fallen, 1529's six blocks, 1538's lockbox, 1214's three beacon spots, 1521's container, 1522's two switches, " +
                "1857's eleven piles, 1401's three doors (1150's and 1899's terminals talk)");
        }

        /// <summary>
        /// The missions of set 1 (Crucible_outpost_intrepid), set 2 (Crucible_grupa_chiefs), set 3 (Crucible_wardens_and_lashers),
        /// set 4 (Crucible_minefield_and_salvage), set 5 (Crucible_staal_sigals) and set 6 (Crucible_awol_camp).
        /// </summary>
        private static readonly uint[] SetOne = MissionIds.Take(4).ToArray(), SetTwo = MissionIds.Skip(4).Take(4).ToArray(),
            SetThree = MissionIds.Skip(8).Take(4).ToArray(), SetFour = MissionIds.Skip(12).Take(4).ToArray(),
            SetFive = MissionIds.Skip(16).Take(3).ToArray(), SetSix = MissionIds.Skip(19).Take(5).ToArray(),
            SetSeven = MissionIds.Skip(24).Take(4).ToArray(), SetEight = MissionIds.Skip(28).Take(4).ToArray(),
            SetEightB = MissionIds.Skip(32).Take(3).ToArray(), SetNine = MissionIds.Skip(35).Take(2).ToArray(),
            SetElevenA = MissionIds.Skip(37).Take(3).ToArray(), SetTwelveA = MissionIds.Skip(40).Take(3).ToArray(),
            SetThirteen = MissionIds.Skip(43).Take(4).ToArray(), SetElevenB = MissionIds.Skip(47).Take(1).ToArray(),
            SetTwelveB = MissionIds.Skip(48).Take(2).ToArray(), SetFifteen = MissionIds.Skip(50).ToArray();

        /// <summary>Sets 1-6, before set 7's migration; sets 1-7, before set 8a's; sets 1-8a, before set 8b's; sets 1-8b, before set 9's.</summary>
        private static readonly uint[] SetsOneToSix = MissionIds.Take(24).ToArray(), SetsOneToSeven = MissionIds.Take(28).ToArray(),
            SetsOneToEightA = MissionIds.Take(32).ToArray(), SetsOneToEightB = MissionIds.Take(35).ToArray(),
            SetsOneToNine = MissionIds.Take(37).ToArray(), SetsOneToElevenA = MissionIds.Take(40).ToArray(),
            SetsOneToTwelveA = MissionIds.Take(43).ToArray(), SetsOneToThirteen = MissionIds.Take(47).ToArray(),
            SetsOneToElevenB = MissionIds.Take(48).ToArray(), SetsOneToTwelveB = MissionIds.Take(50).ToArray();

        /// <summary>
        /// The Crucible block's rows (creature, spawnpool, npc_package, creature_stat), Rigs' package, the Crucible scenes, the
        /// office teleporters and the block's creature attacks: set 1 has 3, 3, 3, 3, 1, 4, 0, 0; set 2 adds Bunko, Yeg and
        /// Roscoe (no pool, no package), the scenes of 1146 and 1148, and the two teleporters; set 3 adds Halprin, the Warden
        /// Bot and the Lasher (Halprin's pool, three Warden Bot pools, two Lasher pools; Halprin's package), the scenes of
        /// 1147, 1157, 1170 and 1229, and the Lasher's attack; set 4 adds the Smart Mine and the Reconstructor Bot (five pools), the
        /// scenes of 1203 and 1204 and three attacks; set 5 adds Nema, Pico and Trista (with their pools and packages), the Shield
        /// Drone and Molitor, six hostile pools, the scenes of 1208 and 1185 and the drone's attack; set 6 adds the five scenes of
        /// 1144, 1152, 1153, 1155 and 1205 (no new row: it moves upstream's); set 7 adds Cirkin (pool, package), Krubb, Mungrel,
        /// Sedge (their squad, no pool), Kalamit, Lamontix, the Technician and the Ultra Reconstructor (five pools), and the scenes of
        /// 1206, 1519, 1529 and 1538 (no new attack); set 8a adds Tiernan and Botha (pools, packages) and the scenes of 1217 and
        /// 1218 (Keplinger and Madias are upstream's, moved); set 8b adds the Juggernaut and Ugoretz (four pools), the scene of 1215
        /// and the Juggernaut's two attacks; set 9 adds the scenes of 1224 and 1214 (no new row: it moves upstream's Tiersky); set
        /// 11a adds Shanul and Dhimand (pools, packages), the Brute, the Slayer, Jaikev and Parvatee (eleven pools), the scenes of
        /// 1445 (its markers' maps), 1444 and 1402 and the Incurables' attack (it moves upstream's Sheira and Tempa); set 12a adds
        /// Ruben and Brian (pools, packages) and the scenes of 1577 (its markers' maps), 1521 and 1522; set 13 adds Stark (pool,
        /// package) and the two Skeevs (no pool; the talker's package) and the scenes of 1857, 1881 and 1899 (1856 has none; its
        /// two pads, 911 and 912, are not counted here); set 11b adds the scene of 1398 (no new row: Dhimand is set 11a's); set 12b
        /// adds the Refitted Warden Bot (no pool, no package) and the scenes of 1523 and 1525; set 15 adds
        /// Atali, Umank and the Headsman (rows, stats), four pools (Atali's, Umank's and their guards') and the scene of 1401.
        /// </summary>
        private static (int, int, int, int, int, int, int, int) Rows(DbContext world)
        {
            int Count(string sql) => world.Database.SqlQueryRaw<int>(sql).AsEnumerable().Single();
            return (
                Count("SELECT COUNT(*) AS Value FROM creature WHERE id BETWEEN 770000 AND 770999"),
                Count("SELECT COUNT(*) AS Value FROM spawnpool WHERE id BETWEEN 770000 AND 770999"),
                Count("SELECT COUNT(*) AS Value FROM npc_package WHERE id BETWEEN 770000 AND 770999"),
                Count("SELECT COUNT(*) AS Value FROM creature_stat WHERE id BETWEEN 770000 AND 770999"),
                Count("SELECT COUNT(*) AS Value FROM npc_package WHERE id = 510025"),
                Count("SELECT COUNT(*) AS Value FROM mission_scene_binding WHERE content_revision = 'crucible_1_16_5'"),
                Count("SELECT COUNT(*) AS Value FROM teleporter WHERE id IN (909, 910)"),
                Count("SELECT COUNT(*) AS Value FROM creature_action WHERE id BETWEEN 770000 AND 770999"));
        }

        private static readonly (int, int, int, int, int, int, int, int) SetOneRows = (3, 3, 3, 3, 1, 4, 0, 0),
            SetTwoRows = (6, 5, 5, 6, 1, 6, 2, 0), SetThreeRows = (9, 11, 6, 9, 1, 10, 2, 1), SetFourRows = (11, 16, 6, 11, 1, 12, 2, 4),
            SetFiveRows = (16, 25, 9, 16, 1, 14, 2, 5), SetSixRows = (16, 25, 9, 16, 1, 19, 2, 5), SetSevenRows = (24, 31, 10, 24, 1, 23, 2, 5),
            SetEightARows = (26, 33, 12, 26, 1, 25, 2, 5), SetEightBRows = (28, 37, 12, 28, 1, 26, 2, 7), SetNineRows = (28, 37, 12, 28, 1, 28, 2, 7),
            SetElevenARows = (34, 50, 14, 34, 1, 31, 2, 8), SetTwelveARows = (36, 52, 16, 36, 1, 34, 2, 8),
            SetThirteenRows = (39, 53, 18, 39, 1, 37, 2, 8), SetElevenBRows = (39, 53, 18, 39, 1, 38, 2, 8),
            SetTwelveBRows = (40, 53, 18, 40, 1, 40, 2, 8),
            AllRows = (43, 57, 18, 43, 1, 41, 2, 8);

        private static (double, double, double) At(Rasa.Context.World.WorldContext world, uint pool)
        {
            var row = world.SpawnPoolEntries.AsNoTracking().Single(entry => entry.Id == pool);
            return (row.PosX, row.PosY, row.PosZ);
        }

        /// <summary>
        /// Crucible_outpost_intrepid takes out its four missions and three NPCs and puts the upstream rows back (places and
        /// packages), with sets 2 and 3 after it; the Marshes missions stay. Up again, set 1 is back.
        /// </summary>
        [TestMethod]
        public void OutpostIntrepidRollsBackWithItsMigration()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var world = harness.World;
            bool Has(uint mission) => world.MissionContentDefinitionEntries.AsNoTracking()
                .Any(entry => entry.MissionId == mission && entry.ContentRevision == CrucibleMissionDataV1.Revision);
            uint Package(uint npc) => world.NpcPackageEntries.AsNoTracking().Single(row => row.Id == npc).PackageId;

            Assert.IsTrue(MissionIds.All(Has));
            Assert.AreEqual(AllRows, Rows(world));
            Assert.AreEqual(1301U, Package(DAmbrose));
            Assert.AreEqual((952.0, 144.5, 106.0), At(world, DeLisi));

            world.GetService<IMigrator>().Migrate(Previous(world, OutpostIntrepid));
            world.ChangeTracker.Clear();
            Assert.IsFalse(MissionIds.Any(Has));
            Assert.AreEqual((0, 0, 0, 0, 0, 0, 0, 0), Rows(world));
            Assert.IsFalse(world.MissionEvidenceEntries.Any(entry => entry.ContentRevision == CrucibleMissionDataV1.Revision));
            Assert.IsFalse(world.MissionIndicatorEntries.Any(entry => entry.ContentRevision == CrucibleMissionDataV1.Revision));
            Assert.IsFalse(world.MissionRewardItemEntries.Any(entry => entry.ContentRevision == CrucibleMissionDataV1.Revision));
            Assert.AreEqual(1229U, Package(DAmbrose), "D'Ambrose has upstream's package again");
            foreach (var (pool, before, _) in CrucibleOutpostIntrepidNpcsV1.Moves)
                Assert.AreEqual(before, At(world, pool), $"pool {pool} back at its upstream place");
            Assert.IsTrue(world.MissionContentDefinitionEntries.Any(entry => entry.MissionId == MarshesWelcomeWagonV1.MissionId &&
                entry.ContentRevision == MarshesMissionDataV1.Revision), "the Marshes missions stay");

            world.GetService<IMigrator>().Migrate(OutpostIntrepid);
            world.ChangeTracker.Clear();
            Assert.IsTrue(SetOne.All(Has));
            Assert.IsFalse(SetTwo.Concat(SetThree).Concat(SetFour).Concat(SetFive).Concat(SetSix).Concat(SetSeven).Concat(SetEight).Concat(SetEightB).Concat(SetNine).Concat(SetElevenA).Concat(SetTwelveA).Concat(SetThirteen).Concat(SetElevenB).Concat(SetTwelveB).Concat(SetFifteen).Any(Has), "sets 2 to 6 are the next migrations'");
            Assert.AreEqual(SetOneRows, Rows(world));
            Assert.AreEqual(1301U, Package(DAmbrose));
        }

        /// <summary>
        /// Crucible_grupa_chiefs takes out its four missions, Bunko, Yeg and Roscoe and the office teleporters, and puts the
        /// upstream rows it moved back (with set 3 after it); set 1 stays as it was. Up again, set 2 is back.
        /// </summary>
        [TestMethod]
        public void GrupaChiefsRollsBackWithItsMigration()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var world = harness.World;
            bool Has(uint mission) => world.MissionContentDefinitionEntries.AsNoTracking()
                .Any(entry => entry.MissionId == mission && entry.ContentRevision == CrucibleMissionDataV1.Revision);
            int Links() => world.LocalTeleporterLinkEntries.AsNoTracking().Count(row => row.FromTeleporterId == 909 || row.FromTeleporterId == 910);

            Assert.IsTrue(MissionIds.All(Has));
            Assert.AreEqual(AllRows, Rows(world));
            Assert.AreEqual(2, Links());
            foreach (var (pool, _, now) in CrucibleGrupaChiefsNpcsV1.Moves)
                Assert.AreEqual(now, At(world, pool), $"pool {pool} at its set 2 place");

            Assert.AreEqual(OutpostIntrepid, Previous(world, GrupaChiefs), "set 2 follows set 1");
            world.GetService<IMigrator>().Migrate(Previous(world, GrupaChiefs));
            world.ChangeTracker.Clear();
            Assert.IsTrue(SetOne.All(Has), "set 1 stays");
            Assert.IsFalse(SetTwo.Concat(SetThree).Concat(SetFour).Concat(SetFive).Concat(SetSix).Concat(SetSeven).Concat(SetEight).Concat(SetEightB).Concat(SetNine).Concat(SetElevenA).Concat(SetTwelveA).Concat(SetThirteen).Concat(SetElevenB).Concat(SetTwelveB).Concat(SetFifteen).Any(Has));
            Assert.AreEqual(SetOneRows, Rows(world));
            Assert.AreEqual(0, Links());
            Assert.IsFalse(world.MissionEvidenceEntries.Any(entry => entry.ContentRevision == CrucibleMissionDataV1.Revision &&
                SetTwo.Contains(entry.MissionId)));
            foreach (var (pool, before, _) in CrucibleGrupaChiefsNpcsV1.Moves)
                Assert.AreEqual(before, At(world, pool), $"pool {pool} back at its upstream place");
            Assert.AreEqual((952.0, 144.5, 106.0), At(world, DeLisi), "set 1's moves stay");

            world.GetService<IMigrator>().Migrate(GrupaChiefs);
            world.ChangeTracker.Clear();
            Assert.IsTrue(SetOne.Concat(SetTwo).All(Has));
            Assert.IsFalse(SetThree.Concat(SetFour).Concat(SetFive).Concat(SetSix).Concat(SetSeven).Concat(SetEight).Concat(SetEightB).Concat(SetNine).Concat(SetElevenA).Concat(SetTwelveA).Concat(SetThirteen).Concat(SetElevenB).Concat(SetTwelveB).Concat(SetFifteen).Any(Has), "sets 3 to 6 are the next migrations'");
            Assert.AreEqual(SetTwoRows, Rows(world));
            Assert.AreEqual(2, Links());
        }

        /// <summary>
        /// Crucible_wardens_and_lashers takes out its four missions, Halprin, the Warden Bot and the Lasher (with its attack)
        /// and their five pools; sets 1 and 2 stay as they were. Up again, all of it is back.
        /// </summary>
        [TestMethod]
        public void WardensAndLashersRollsBackWithItsMigration()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var world = harness.World;
            bool Has(uint mission) => world.MissionContentDefinitionEntries.AsNoTracking()
                .Any(entry => entry.MissionId == mission && entry.ContentRevision == CrucibleMissionDataV1.Revision);
            int Pools() => world.SpawnPoolEntries.AsNoTracking().Count(row => row.Id >= 770006 && row.Id <= 770013);

            Assert.IsTrue(MissionIds.All(Has));
            Assert.AreEqual(AllRows, Rows(world));
            Assert.AreEqual(6, Pools(), "Halprin's, three Warden Bot and two Lasher pools");

            Assert.AreEqual(GrupaChiefs, Previous(world, WardensAndLashers), "set 3 follows set 2");
            world.GetService<IMigrator>().Migrate(Previous(world, WardensAndLashers));
            world.ChangeTracker.Clear();
            Assert.IsTrue(SetOne.Concat(SetTwo).All(Has), "sets 1 and 2 stay");
            Assert.IsFalse(SetThree.Concat(SetFour).Concat(SetFive).Concat(SetSix).Concat(SetSeven).Concat(SetEight).Concat(SetEightB).Concat(SetNine).Concat(SetElevenA).Concat(SetTwelveA).Concat(SetThirteen).Concat(SetElevenB).Concat(SetTwelveB).Concat(SetFifteen).Any(Has));
            Assert.AreEqual(SetTwoRows, Rows(world));
            Assert.AreEqual(0, Pools());
            Assert.IsFalse(world.MissionEvidenceEntries.Any(entry => entry.ContentRevision == CrucibleMissionDataV1.Revision &&
                SetThree.Contains(entry.MissionId)));
            Assert.IsFalse(world.MissionIndicatorEntries.Any(entry => entry.ContentRevision == CrucibleMissionDataV1.Revision &&
                SetThree.Contains(entry.MissionId)));
            Assert.AreEqual((952.0, 144.5, 106.0), At(world, DeLisi), "set 1's moves stay");
            foreach (var (pool, _, now) in CrucibleGrupaChiefsNpcsV1.Moves)
                Assert.AreEqual(now, At(world, pool), $"set 2's pool {pool} stays at its set 2 place");

            world.GetService<IMigrator>().Migrate(WardensAndLashers);
            world.ChangeTracker.Clear();
            Assert.IsTrue(SetOne.Concat(SetTwo).Concat(SetThree).All(Has));
            Assert.IsFalse(SetFour.Concat(SetFive).Concat(SetSix).Concat(SetSeven).Concat(SetEight).Concat(SetEightB).Concat(SetNine).Concat(SetElevenA).Concat(SetTwelveA).Concat(SetThirteen).Concat(SetElevenB).Concat(SetTwelveB).Concat(SetFifteen).Any(Has), "sets 4 to 6 are later migrations'");
            Assert.AreEqual(SetThreeRows, Rows(world));
            Assert.AreEqual(6, Pools());
        }

        /// <summary>
        /// Crucible_junkyard_dogs_markers (to-fix rows 23-25) puts the four Flaregasher pools and the markers of 1149 and 1147
        /// back at set 2's and set 3's values, and takes out 1147's six new markers and the two evidence rows; Up again, the
        /// new values are back.
        /// </summary>
        [TestMethod]
        public void JunkyardDogsMarkersRollBackWithTheirMigration()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var world = harness.World;
            List<(uint, uint, double, double, double, double)> Markers(uint mission) => world.MissionIndicatorEntries.AsNoTracking()
                .Where(entry => entry.MissionId == mission && entry.ContentRevision == CrucibleMissionDataV1.Revision).AsEnumerable()
                .Select(entry => (entry.ObjectiveId, entry.IndicatorId, entry.PosX, entry.PosY, entry.PosZ, entry.Radius))
                .OrderBy(entry => entry.ObjectiveId).ThenBy(entry => entry.IndicatorId).ToList();
            int Evidence() => world.MissionEvidenceEntries.AsNoTracking().Count(entry => entry.ContentRevision == CrucibleMissionDataV1.Revision &&
                (entry.MissionId == 1147 || entry.MissionId == 1149) && entry.EvidenceId == 4);
            void AssertNow()
            {
                foreach (var (pool, _, now) in CrucibleMarkersClearOfLinksV2.Pools)
                    Assert.AreEqual(now, At(world, pool), $"pool {pool} at its new place");
                CollectionAssert.AreEqual(CrucibleMarkersClearOfLinksV2.GasherMarkers.Select(marker => (1U, marker.Indicator, marker.At.X, marker.At.Y, marker.At.Z, 25.0)).ToList(),
                    Markers(1149), "1149's markers on the moved pools");
                CollectionAssert.AreEqual(CrucibleMarkersClearOfLinksV2.WardenMarkers.Select(marker => (marker.Objective, marker.Indicator, marker.At.X, marker.At.Y, marker.At.Z, 20.0))
                    .OrderBy(entry => entry.Objective).ThenBy(entry => entry.Indicator).ToList(), Markers(1147), "1147's nine markers");
                Assert.AreEqual(2, Evidence());
            }

            // 20271108000800 moves 1147's markers again (to-fix row 44): this fix's values are checked at its own migration.
            world.GetService<IMigrator>().Migrate(JunkyardDogsMarkers);
            world.ChangeTracker.Clear();
            AssertNow();
            Assert.AreEqual(WardensAndLashers, Previous(world, JunkyardDogsMarkers), "the fix follows set 3");
            world.GetService<IMigrator>().Migrate(Previous(world, JunkyardDogsMarkers));
            world.ChangeTracker.Clear();
            foreach (var (pool, before, _) in CrucibleMarkersClearOfLinksV2.Pools)
                Assert.AreEqual(before, At(world, pool), $"pool {pool} back at its upstream place");
            CollectionAssert.AreEqual(new List<(uint, uint, double, double, double, double)>
            {
                (1, 1000000001, -492.1, 236.9, -149.1, 25), (1, 1000000002, -533.2, 236.9, -251.1, 25), (1, 1000000003, -432.8, 236.9, -56.4, 25)
            }, Markers(1149), "set 2's markers");
            CollectionAssert.AreEqual(new List<(uint, uint, double, double, double, double)>
            {
                (1, 1000000001, -533.6, 249.7, 563.3, 55), (2, 1000000002, -533.6, 249.7, 563.3, 55), (3, 1000000003, -533.6, 249.7, 563.3, 55)
            }, Markers(1147), "set 3's markers");
            Assert.AreEqual(0, Evidence());
            Assert.AreEqual(SetThreeRows, Rows(world), "sets 1-3's rows stay (set 4 comes after the fix)");

            world.GetService<IMigrator>().Migrate(JunkyardDogsMarkers);
            world.ChangeTracker.Clear();
            AssertNow();
        }

        /// <summary>
        /// Crucible_minefield_and_salvage takes out its four missions, the Smart Mine and the Reconstructor Bot (with their
        /// three actions) and their five pools, and puts Derge back on the control point flag; sets 1-3 and the marker fix stay
        /// as they were. Up again, all of it is back.
        /// </summary>
        [TestMethod]
        public void MinefieldAndSalvageRollsBackWithItsMigration()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var world = harness.World;
            bool Has(uint mission) => world.MissionContentDefinitionEntries.AsNoTracking()
                .Any(entry => entry.MissionId == mission && entry.ContentRevision == CrucibleMissionDataV1.Revision);
            int Pools() => world.SpawnPoolEntries.AsNoTracking().Count(row => row.Id >= 770014 && row.Id <= 770018);

            Assert.IsTrue(MissionIds.All(Has));
            Assert.AreEqual(AllRows, Rows(world));
            Assert.AreEqual(5, Pools(), "three Smart Mine and two Reconstructor pools");
            Assert.AreEqual(CrucibleMinefieldAndSalvageNpcsV1.DergeAt, At(world, Derge), "Derge on the rampart");

            Assert.AreEqual(JunkyardDogsMarkers, Previous(world, MinefieldAndSalvage), "set 4 follows the marker fix");
            world.GetService<IMigrator>().Migrate(Previous(world, MinefieldAndSalvage));
            world.ChangeTracker.Clear();
            Assert.IsTrue(SetOne.Concat(SetTwo).Concat(SetThree).All(Has), "sets 1-3 stay");
            Assert.IsFalse(SetFour.Concat(SetFive).Concat(SetSix).Concat(SetSeven).Concat(SetEight).Concat(SetEightB).Concat(SetNine).Concat(SetElevenA).Concat(SetTwelveA).Concat(SetThirteen).Concat(SetElevenB).Concat(SetTwelveB).Concat(SetFifteen).Any(Has));
            Assert.AreEqual(SetThreeRows, Rows(world));
            Assert.AreEqual(0, Pools());
            Assert.AreEqual(CrucibleMinefieldAndSalvageNpcsV1.DergeBefore, At(world, Derge), "Derge back at upstream's place");
            Assert.IsFalse(world.MissionEvidenceEntries.Any(entry => entry.ContentRevision == CrucibleMissionDataV1.Revision &&
                SetFour.Contains(entry.MissionId)));
            Assert.IsFalse(world.MissionIndicatorEntries.Any(entry => entry.ContentRevision == CrucibleMissionDataV1.Revision &&
                SetFour.Contains(entry.MissionId)));
            foreach (var (pool, _, now) in CrucibleMarkersClearOfLinksV2.Pools)
                Assert.AreEqual(now, At(world, pool), $"the fix's pool {pool} stays");

            world.GetService<IMigrator>().Migrate(MinefieldAndSalvage);
            world.ChangeTracker.Clear();
            Assert.IsTrue(SetOne.Concat(SetTwo).Concat(SetThree).Concat(SetFour).All(Has));
            Assert.IsFalse(SetFive.Concat(SetSix).Concat(SetSeven).Concat(SetEight).Concat(SetEightB).Concat(SetNine).Concat(SetElevenA).Concat(SetTwelveA).Concat(SetThirteen).Concat(SetElevenB).Concat(SetTwelveB).Concat(SetFifteen).Any(Has), "sets 5 and 6 are later migrations'");
            Assert.AreEqual(SetFourRows, Rows(world));
            Assert.AreEqual(5, Pools());
        }

        /// <summary>
        /// Crucible_staal_walks (to-fix rows 32, 34, 35) puts Munit and Vargo and their five markers back at sets 1's and 2's
        /// places, Yeg's marker back at r8, and takes out its three evidence rows; set 4 stays. Up again, the new values are back.
        /// </summary>
        [TestMethod]
        public void StaalWalksRollBackWithTheirMigration()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var world = harness.World;
            (double, double, double, double) Marker(uint mission, uint objective, uint indicator) => world.MissionIndicatorEntries.AsNoTracking()
                .Where(entry => entry.MissionId == mission && entry.ObjectiveId == objective && entry.IndicatorId == indicator &&
                    entry.ContentRevision == CrucibleMissionDataV1.Revision).AsEnumerable()
                .Select(entry => (entry.PosX, entry.PosY, entry.PosZ, entry.Radius)).Single();
            int Evidence() => world.MissionEvidenceEntries.AsNoTracking().Count(entry => entry.ContentRevision == CrucibleMissionDataV1.Revision &&
                (entry.MissionId == 1145 || entry.MissionId == 1146 || entry.MissionId == 1170) && entry.EvidenceId == 4);
            var yeg = CrucibleStaalWalksV1.YegMarker;
            void Check(bool now, (int, int, int, int, int, int, int, int) rows)
            {
                foreach (var (pool, before, after) in CrucibleStaalWalksV1.Moves)
                    Assert.AreEqual(now ? after : before, At(world, pool), $"pool {pool}");
                foreach (var (mission, objective, indicator, pool) in CrucibleStaalWalksV1.Markers)
                {
                    var move = CrucibleStaalWalksV1.Moves.Single(entry => entry.Pool == pool);
                    var at = now ? move.Now : move.Before;
                    Assert.AreEqual((at.X, at.Y, at.Z, 8.0), Marker(mission, objective, indicator),
                        $"{mission}.{objective}/{indicator} on pool {pool}");
                }
                Assert.AreEqual(now ? CrucibleStaalWalksV1.YegMarkerNow : CrucibleStaalWalksV1.YegMarkerBefore,
                    Marker(yeg.Mission, yeg.Objective, yeg.Indicator).Item4, "Yeg's marker");
                Assert.AreEqual(now ? 3 : 0, Evidence());
                Assert.AreEqual(rows, Rows(world), "sets 1-4's rows stay");
            }

            // 20271108000800 moves Vargo and his markers again (to-fix row 43): this fix's values are checked at its own migration.
            world.GetService<IMigrator>().Migrate(StaalWalks);
            world.ChangeTracker.Clear();
            Check(true, SetFourRows);
            Assert.AreEqual(MinefieldAndSalvage, Previous(world, StaalWalks), "the fix follows set 4");
            world.GetService<IMigrator>().Migrate(MinefieldAndSalvage);
            world.ChangeTracker.Clear();
            Check(false, SetFourRows);
            world.GetService<IMigrator>().Migrate(StaalWalks);
            world.ChangeTracker.Clear();
            Check(true, SetFourRows);
        }

        /// <summary>
        /// Crucible_staal_sigals takes out its three missions, Nema, Pico and Trista, the Shield Drone and Molitor (with the
        /// drone's attack) and their six hostile pools; sets 1-4 and the Staal walks fix stay as they were (Munit, Vargo). Up again,
        /// all of it is back.
        /// </summary>
        [TestMethod]
        public void StaalSigalsRollsBackWithItsMigration()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var world = harness.World;
            bool Has(uint mission) => world.MissionContentDefinitionEntries.AsNoTracking()
                .Any(entry => entry.MissionId == mission && entry.ContentRevision == CrucibleMissionDataV1.Revision);
            int Pools() => world.SpawnPoolEntries.AsNoTracking().Count(row => row.Id >= 770019 && row.Id <= 770029);

            Assert.IsTrue(MissionIds.All(Has));
            Assert.AreEqual(AllRows, Rows(world));
            Assert.AreEqual(3 + 3 + 2 + 1, Pools(), "Nema's, Pico's and Trista's, three drone, two Reconstructor pools and Molitor's");

            Assert.AreEqual(StaalWalks, Previous(world, StaalSigals), "set 5 follows the Staal walks fix");
            world.GetService<IMigrator>().Migrate(Previous(world, StaalSigals));
            world.ChangeTracker.Clear();
            Assert.IsTrue(SetOne.Concat(SetTwo).Concat(SetThree).Concat(SetFour).All(Has), "sets 1-4 stay");
            Assert.IsFalse(SetFive.Concat(SetSix).Concat(SetSeven).Concat(SetEight).Concat(SetEightB).Concat(SetNine).Concat(SetElevenA).Concat(SetTwelveA).Concat(SetThirteen).Concat(SetElevenB).Concat(SetTwelveB).Concat(SetFifteen).Any(Has));
            Assert.AreEqual(SetFourRows, Rows(world));
            Assert.AreEqual(0, Pools());
            Assert.IsFalse(world.MissionEvidenceEntries.Any(entry => entry.ContentRevision == CrucibleMissionDataV1.Revision &&
                SetFive.Contains(entry.MissionId)));
            Assert.IsFalse(world.MissionIndicatorEntries.Any(entry => entry.ContentRevision == CrucibleMissionDataV1.Revision &&
                SetFive.Contains(entry.MissionId)));
            foreach (var (pool, _, now) in CrucibleStaalWalksV1.Moves)
                Assert.AreEqual(now, At(world, pool), $"the Staal walks fix's pool {pool} stays");

            world.GetService<IMigrator>().Migrate(StaalSigals);
            world.ChangeTracker.Clear();
            Assert.IsTrue(SetOne.Concat(SetTwo).Concat(SetThree).Concat(SetFour).Concat(SetFive).All(Has));
            Assert.IsFalse(SetSix.Concat(SetSeven).Concat(SetEight).Concat(SetEightB).Concat(SetNine).Concat(SetElevenA).Concat(SetTwelveA).Concat(SetThirteen).Concat(SetElevenB).Concat(SetTwelveB).Concat(SetFifteen).Any(Has), "sets 6 and 7 are later migrations'");
            Assert.AreEqual(SetFiveRows, Rows(world));
            Assert.AreEqual(9, Pools());
        }

        /// <summary>
        /// Crucible_awol_camp takes out its five missions (with their talks, markers and evidence) and puts Cheung, Deirdre,
        /// Sherer and D'Ambrose back at upstream's places; sets 1-5 stay as they were (D'Ambrose keeps set 1's package 1301,
        /// Breiburg and Melching stay). Up again, all of it is back.
        /// </summary>
        [TestMethod]
        public void AwolCampRollsBackWithItsMigration()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var world = harness.World;
            bool Has(uint mission) => world.MissionContentDefinitionEntries.AsNoTracking()
                .Any(entry => entry.MissionId == mission && entry.ContentRevision == CrucibleMissionDataV1.Revision);
            uint Package(uint npc) => world.NpcPackageEntries.AsNoTracking().Single(row => row.Id == npc).PackageId;
            void Check(bool now)
            {
                foreach (var (pool, before, after) in CrucibleAwolCampNpcsV1.Moves)
                    Assert.AreEqual(now ? after : before, At(world, pool), $"pool {pool}");
                foreach (var (pool, at) in CrucibleAwolCampNpcsV1.Kept)
                    Assert.AreEqual(at, At(world, pool), $"pool {pool} kept");
                Assert.AreEqual(CrucibleGrupaChiefsNpcsV1.MelchingAt, At(world, Melching), "Melching stays");
                Assert.AreEqual((1292U, 1304U, 1293U, 1297U, 1301U), (Package(Cheung), Package(Deirdre), Package(Breiburg), Package(Sherer),
                    Package(DAmbrose)), "the packages stay");
            }

            Assert.IsTrue(MissionIds.All(Has));
            Assert.AreEqual(AllRows, Rows(world));
            Check(true);

            Assert.AreEqual(StaalSigals, Previous(world, AwolCamp), "set 6 follows set 5");
            world.GetService<IMigrator>().Migrate(Previous(world, AwolCamp));
            world.ChangeTracker.Clear();
            Assert.IsTrue(SetOne.Concat(SetTwo).Concat(SetThree).Concat(SetFour).Concat(SetFive).All(Has), "sets 1-5 stay");
            Assert.IsFalse(SetSix.Concat(SetSeven).Concat(SetEight).Concat(SetEightB).Concat(SetNine).Concat(SetElevenA).Concat(SetTwelveA).Concat(SetThirteen).Concat(SetElevenB).Concat(SetTwelveB).Concat(SetFifteen).Any(Has));
            Assert.AreEqual(SetFiveRows, Rows(world));
            Check(false);
            foreach (var entries in new System.Func<bool>[]
            {
                () => world.MissionEvidenceEntries.AsNoTracking().Any(entry => entry.ContentRevision == CrucibleMissionDataV1.Revision && SetSix.Contains(entry.MissionId)),
                () => world.MissionIndicatorEntries.AsNoTracking().Any(entry => entry.ContentRevision == CrucibleMissionDataV1.Revision && SetSix.Contains(entry.MissionId)),
                () => world.MissionPrerequisiteEntries.AsNoTracking().Any(entry => entry.ContentRevision == CrucibleMissionDataV1.Revision && SetSix.Contains(entry.MissionId)),
                () => world.Set<MissionTriggerEntry>().AsNoTracking().Any(entry => entry.ContentRevision == CrucibleMissionDataV1.Revision && SetSix.Contains(entry.MissionId))
            })
                Assert.IsFalse(entries(), "set 6's rows go");
            Assert.AreEqual(CrucibleStaalWalksV1.Moves.Single(move => move.Pool == CrucibleOutpostIntrepidNpcsV1.Munit).Now,
                At(world, CrucibleOutpostIntrepidNpcsV1.Munit), "the Staal walks fix stays");

            world.GetService<IMigrator>().Migrate(AwolCamp);
            world.ChangeTracker.Clear();
            Assert.IsTrue(SetsOneToSix.All(Has));
            Assert.IsFalse(SetSeven.Concat(SetEight).Concat(SetEightB).Concat(SetNine).Concat(SetElevenA).Concat(SetTwelveA).Concat(SetThirteen).Concat(SetElevenB).Concat(SetTwelveB).Concat(SetFifteen).Any(Has), "set 7 is a later migration's");
            Assert.AreEqual(SetSixRows, Rows(world));
            Check(true);
        }

        /// <summary>
        /// Crucible_warden_pools_and_vargo (to-fix rows 43, 44) puts the three Warden Bot pools, Reconstructor pool 770028 and
        /// Vargo and their markers (1147's nine, 1203's six, 1204's nine, 1202's one, 1146's two) back at the earlier
        /// migrations' places and takes out its five evidence rows; sets 1-6 stay. Up again, the new values are back.
        /// </summary>
        [TestMethod]
        public void WardenPoolsAndVargoRollBackWithTheirMigration()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var world = harness.World;
            var fix = CrucibleWardenPoolsAndVargoV1.Markers.ToList();
            Assert.HasCount(9 + 2 * 3 + 3 * 3 + 1 + 2, fix, "the markers on the moved pools");
            (double, double, double) Marker(uint mission, uint objective, uint indicator) => world.MissionIndicatorEntries.AsNoTracking()
                .Where(entry => entry.MissionId == mission && entry.ObjectiveId == objective && entry.IndicatorId == indicator &&
                    entry.ContentRevision == CrucibleMissionDataV1.Revision).AsEnumerable()
                .Select(entry => (entry.PosX, entry.PosY, entry.PosZ)).Single();
            int Evidence() => CrucibleWardenPoolsAndVargoV1.EvidenceIds.Count(id => world.MissionEvidenceEntries.AsNoTracking()
                .Any(entry => entry.ContentRevision == CrucibleMissionDataV1.Revision && entry.MissionId == id.Mission && entry.EvidenceId == id.Evidence));
            void Check(bool now, bool full = false)
            {
                // Fully migrated, fix job F (20271108001000) has moved Warden Bot pool 770011 and its markers off the lava again.
                (double, double, double) Latest((double, double, double) at, uint pool) => full ? CrucibleFixFV1.Now(pool) ?? at : at;
                foreach (var (pool, before, after) in CrucibleWardenPoolsAndVargoV1.Moves)
                    Assert.AreEqual(now ? Latest(after, pool) : before, At(world, pool), $"pool {pool}");
                foreach (var (mission, objective, indicator, pool) in fix)
                {
                    var move = CrucibleWardenPoolsAndVargoV1.Moves.Single(entry => entry.Pool == pool);
                    Assert.AreEqual(now ? Latest(move.Now, pool) : move.Before, Marker(mission, objective, indicator), $"{mission}.{objective}/{indicator} on pool {pool}");
                }
                Assert.AreEqual(now ? 5 : 0, Evidence());
                Assert.IsTrue(SetsOneToSix.All(Has), "sets 1-6 stay");
                Assert.AreEqual(full ? AllRows : SetSixRows, Rows(world), "no row comes or goes");
            }
            bool Has(uint mission) => world.MissionContentDefinitionEntries.AsNoTracking()
                .Any(entry => entry.MissionId == mission && entry.ContentRevision == CrucibleMissionDataV1.Revision);

            Check(true, full: true);
            Assert.AreEqual(AwolCamp, Previous(world, WardenPoolsAndVargo), "the fix follows set 6");
            world.GetService<IMigrator>().Migrate(AwolCamp);
            world.ChangeTracker.Clear();
            Check(false);
            Assert.AreEqual(CrucibleStaalWalksV1.Now(CrucibleWardenPoolsAndVargoV1.Vargo).Value, At(world, CrucibleWardenPoolsAndVargoV1.Vargo),
                "Vargo back at the Staal walks fix's place");
            world.GetService<IMigrator>().Migrate(WardenPoolsAndVargo);
            world.ChangeTracker.Clear();
            Check(true);
        }

        /// <summary>
        /// Crucible_staal_junkyard takes out its four missions (with their talks, markers, areas and evidence), Cirkin, the three
        /// officers and their squad, Kalamit, Lamontix, the Technicians and the Ultra Reconstructor; sets 1-6 and the fix before
        /// it stay. Up again, all of it is back.
        /// </summary>
        [TestMethod]
        public void StaalJunkyardRollsBackWithItsMigration()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var world = harness.World;
            bool Has(uint mission) => world.MissionContentDefinitionEntries.AsNoTracking()
                .Any(entry => entry.MissionId == mission && entry.ContentRevision == CrucibleMissionDataV1.Revision);
            int Squads() => world.Set<NpcSquadTypeEntry>().AsNoTracking().Count(row => row.Id == Krubb) +
                world.Set<NpcSquadMemberEntry>().AsNoTracking().Count(row => row.TypeId == Krubb) +
                world.Set<NpcSquadPathEntry>().AsNoTracking().Count(row => row.Id == Krubb) +
                world.Set<NpcSquadPathPointEntry>().AsNoTracking().Count(row => row.PathId == Krubb) +
                world.Set<NpcSquadSpawnEntry>().AsNoTracking().Count(row => row.Id == Krubb);
            bool Any(System.Func<uint, bool> rows) => SetSeven.Any(rows);

            Assert.IsTrue(MissionIds.All(Has));
            Assert.AreEqual(AllRows, Rows(world));
            Assert.AreEqual(1 + 4 + 1 + 7 + 1, Squads(), "the squad's type, four members, its beat of seven points and its spawn");

            Assert.AreEqual(WardenPoolsAndVargo, Previous(world, StaalJunkyard), "set 7 follows the warden pools fix");
            world.GetService<IMigrator>().Migrate(Previous(world, StaalJunkyard));
            world.ChangeTracker.Clear();
            Assert.IsTrue(SetsOneToSix.All(Has), "sets 1-6 stay");
            Assert.IsFalse(SetSeven.Concat(SetEight).Concat(SetEightB).Concat(SetNine).Concat(SetElevenA).Concat(SetTwelveA).Concat(SetThirteen).Concat(SetElevenB).Concat(SetTwelveB).Concat(SetFifteen).Any(Has));
            Assert.AreEqual(SetSixRows, Rows(world));
            Assert.AreEqual(0, Squads());
            Assert.IsFalse(Any(mission => world.MissionEvidenceEntries.AsNoTracking().Any(entry => entry.ContentRevision == CrucibleMissionDataV1.Revision && entry.MissionId == mission)));
            Assert.IsFalse(Any(mission => world.MissionIndicatorEntries.AsNoTracking().Any(entry => entry.ContentRevision == CrucibleMissionDataV1.Revision && entry.MissionId == mission)));
            Assert.IsFalse(Any(mission => world.Set<MissionAreaEntry>().AsNoTracking().Any(entry => entry.ContentRevision == CrucibleMissionDataV1.Revision && entry.MissionId == mission)));
            Assert.IsFalse(Any(mission => world.Set<MissionTriggerEntry>().AsNoTracking().Any(entry => entry.ContentRevision == CrucibleMissionDataV1.Revision && entry.MissionId == mission)));
            Assert.IsFalse(world.CreatureAppearanceEntries.AsNoTracking().Any(row => row.Id == Cirkin), "Cirkin's look goes");
            foreach (var (pool, _, now) in CrucibleWardenPoolsAndVargoV1.Moves)
                Assert.AreEqual(now, At(world, pool), $"the warden pools fix's pool {pool} stays");

            world.GetService<IMigrator>().Migrate(StaalJunkyard);
            world.ChangeTracker.Clear();
            Assert.IsTrue(SetsOneToSeven.All(Has));
            Assert.IsFalse(SetEight.Concat(SetEightB).Concat(SetNine).Concat(SetElevenA).Concat(SetTwelveA).Concat(SetThirteen).Concat(SetElevenB).Concat(SetTwelveB).Concat(SetFifteen).Any(Has), "sets 8a and 8b are later migrations'");
            Assert.AreEqual(SetSevenRows, Rows(world));
            Assert.AreEqual(1 + 4 + 1 + 7 + 1, Squads());
        }

        /// <summary>
        /// Crucible_fix_f (fix job F) puts the twelve pools it made points back at their radii (6; upstream's Flaregasher pool
        /// 540141 15, Molitor's 10), the six it moved off lava (the Lasher pools, 540141, 770011, 770024, Molitor's) and their
        /// markers back where the earlier migrations put them, and takes out its nine evidence rows and nothing else: the other
        /// pools' places, the evidence before it, the missions and every row of sets 1-7 stay. Up again, all of it is back.
        /// </summary>
        [TestMethod]
        public void FixFRollsBackWithItsMigration()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var world = harness.World;
            Assert.HasCount(12, CrucibleFixFV1.Points);
            Assert.HasCount(6, CrucibleFixFV1.Moves);
            Assert.HasCount(2 + 1 + 8 + 1 + 1, CrucibleFixFV1.Markers.ToList(), "1157's two, 1149's one, 770011's eight, 1208's one, 1185's one");
            CollectionAssert.AreEqual(new uint[] { 6, 6, 6, 6, 6, 6, 6, 6, 6, 15, 6, 10 }, CrucibleFixFV1.Points.Select(point => point.Before).ToArray(),
                "the radii before: set 3's, set 4's and set 5's pools 6, upstream's 540141 15, Molitor's 10");
            List<(uint, uint)> Evidence() => world.MissionEvidenceEntries.AsNoTracking()
                .Where(entry => entry.ContentRevision == CrucibleMissionDataV1.Revision).AsEnumerable()
                .Where(entry => CrucibleFixFV1.EvidenceIds.Contains((entry.MissionId, entry.EvidenceId)))
                .Select(entry => (entry.MissionId, entry.EvidenceId)).ToList();
            // Set 8a, fix job G, set 8b, set 9, fix job H and set 11a (later migrations) are rolled back with the fix and not put back: their evidence is not counted.
            int All() => world.MissionEvidenceEntries.AsNoTracking().Count(entry => entry.ContentRevision == CrucibleMissionDataV1.Revision &&
                !SetEight.Contains(entry.MissionId) && !SetEightB.Contains(entry.MissionId) && !SetNine.Contains(entry.MissionId) &&
                !SetElevenA.Contains(entry.MissionId) && !SetTwelveA.Contains(entry.MissionId) && !SetThirteen.Contains(entry.MissionId) && !SetElevenB.Contains(entry.MissionId) && !SetTwelveB.Contains(entry.MissionId) && !SetFifteen.Contains(entry.MissionId) &&
                !(entry.MissionId == CrucibleOutOfSightV1.MissionId && entry.EvidenceId == CrucibleFixGV1.Evidence) &&
                !(entry.MissionId == CruciblePartOfTheBargainV1.MissionId && entry.EvidenceId == CrucibleFixHV1.Evidence) &&
                !(entry.MissionId == CrucibleMinesweeperV1.MissionId && entry.EvidenceId == CrucibleFixJV1.Evidence));
            double Radius(uint pool) => world.SpawnPoolEntries.AsNoTracking().Single(entry => entry.Id == pool).Radius;
            var kept = CrucibleFixFV1.Points.Where(point => CrucibleFixFV1.Now(point.Pool) == null).Select(point => point.Pool).ToList();
            Assert.HasCount(6, kept, "six of the twelve stay where they are");
            var places = kept.Select(pool => At(world, pool)).ToList();
            (double, double, double) Marker(uint mission, uint objective, uint indicator) => world.MissionIndicatorEntries.AsNoTracking()
                .Where(entry => entry.MissionId == mission && entry.ObjectiveId == objective &&
                    entry.IndicatorId == indicator && entry.ContentRevision == CrucibleMissionDataV1.Revision).AsEnumerable()
                .Select(entry => (entry.PosX, entry.PosY, entry.PosZ)).Single();
            void Check(bool now, bool full = false)
            {
                foreach (var (pool, before) in CrucibleFixFV1.Points)
                    Assert.AreEqual(now ? CrucibleFixFV1.PointRadius : before, (uint)Radius(pool), $"pool {pool}'s radius");
                CollectionAssert.AreEqual(places, kept.Select(pool => At(world, pool)).ToList(), "the places stay");
                foreach (var (pool, before, after) in CrucibleFixFV1.Moves)
                    Assert.AreEqual(now ? after : before, At(world, pool), $"pool {pool}");
                foreach (var (mission, objective, indicator, pool) in CrucibleFixFV1.Markers)
                {
                    var move = CrucibleFixFV1.Moves.Single(entry => entry.Pool == pool);
                    Assert.AreEqual(now ? move.Now : move.Before, Marker(mission, objective, indicator), $"{mission}.{objective}/{indicator} on pool {pool}");
                }
                Assert.HasCount(now ? 9 : 0, Evidence());
                Assert.IsTrue((full ? MissionIds : SetsOneToSeven).All(mission => world.MissionContentDefinitionEntries.AsNoTracking()
                    .Any(entry => entry.MissionId == mission && entry.ContentRevision == CrucibleMissionDataV1.Revision)), "the missions stay");
                Assert.AreEqual(full ? AllRows : SetSevenRows, Rows(world), "no other row comes or goes");
            }

            Check(true, full: true);
            foreach (var (mission, evidence) in CrucibleFixFV1.EvidenceIds)
            {
                var row = world.MissionEvidenceEntries.AsNoTracking().Single(entry => entry.ContentRevision == CrucibleMissionDataV1.Revision &&
                    entry.MissionId == mission && entry.EvidenceId == evidence);
                Assert.AreEqual((MissionEvidenceSourceKind.Reconstruction, "repository:CrucibleFixFV1", CrucibleFixFV1.NoteOf(mission), 0.3),
                    (row.SourceKind, row.SourceUri, row.ReconstructionNote, row.Confidence), $"{mission}/{evidence}");
            }
            var all = All();

            Assert.AreEqual(StaalJunkyard, Previous(world, FixF), "the fix follows set 7");
            world.GetService<IMigrator>().Migrate(StaalJunkyard);
            world.ChangeTracker.Clear();
            Check(false);
            Assert.AreEqual(all - 9, All(), "only the fix's rows go");

            world.GetService<IMigrator>().Migrate(FixF);
            world.ChangeTracker.Clear();
            Check(true);
            Assert.AreEqual(all, All());
        }

        /// <summary>
        /// Crucible_kardash_and_prometheus takes out its four missions (with their talks, markers, areas, scenes and evidence),
        /// Tiernan and Botha (rows, looks, stats, packages, pools), and puts Keplinger and Madias back on the control point;
        /// sets 1-7 and fix job F stay. Up again, all of it is back (fix job G, set 8b and set 9, later migrations, stay out).
        /// </summary>
        [TestMethod]
        public void KardashAndPrometheusRollsBackWithItsMigration()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var world = harness.World;
            bool Has(uint mission) => world.MissionContentDefinitionEntries.AsNoTracking()
                .Any(entry => entry.MissionId == mission && entry.ContentRevision == CrucibleMissionDataV1.Revision);
            bool Any(System.Func<uint, bool> rows) => SetEight.Any(rows);
            void Check(bool now, bool full = false)
            {
                foreach (var (pool, before, after) in CrucibleKardashAndPrometheusNpcsV1.Moves)
                    Assert.AreEqual(now ? after : before, At(world, pool), $"pool {pool}");
                foreach (var npc in new[] { Tiernan, Botha })
                {
                    Assert.AreEqual(now, world.CreatureEntries.AsNoTracking().Any(row => row.Id == npc), $"{npc}");
                    Assert.AreEqual(now, world.CreatureAppearanceEntries.AsNoTracking().Any(row => row.Id == npc), $"{npc}'s look");
                    Assert.AreEqual(now, world.CreatureStatEntries.AsNoTracking().Any(row => row.Id == npc), $"{npc}'s stats");
                    Assert.AreEqual(now, world.NpcPackageEntries.AsNoTracking().Any(row => row.Id == npc), $"{npc}'s package");
                    Assert.AreEqual(now, world.SpawnPoolEntries.AsNoTracking().Any(row => row.Id == npc), $"{npc}'s pool");
                }
                Assert.AreEqual(1337U, world.NpcPackageEntries.AsNoTracking().Single(row => row.Id == Keplinger).PackageId, "Keplinger keeps upstream's package");
                Assert.IsTrue(SetsOneToSeven.All(Has), "sets 1-7 stay");
                Assert.AreEqual(now, SetEight.All(Has));
                Assert.AreEqual(now, SetEight.Any(Has));
                Assert.AreEqual(full, SetEightB.Concat(SetNine).Concat(SetElevenA).Concat(SetTwelveA).Concat(SetThirteen).Concat(SetElevenB).Concat(SetTwelveB).Concat(SetFifteen).Any(Has), "sets 8b, 9 and 11a are later migrations'");
                Assert.AreEqual(full ? AllRows : now ? SetEightARows : SetSevenRows, Rows(world));
                foreach (var (pool, _, after) in CrucibleFixFV1.Moves)
                    Assert.AreEqual(after, At(world, pool), $"fix job F's pool {pool} stays");
            }

            Check(true, full: true);
            Assert.AreEqual(FixF, Previous(world, KardashAndPrometheus), "set 8a follows fix job F");
            world.GetService<IMigrator>().Migrate(FixF);
            world.ChangeTracker.Clear();
            Check(false);
            Assert.IsFalse(Any(mission => world.MissionEvidenceEntries.AsNoTracking().Any(entry => entry.ContentRevision == CrucibleMissionDataV1.Revision && entry.MissionId == mission)));
            Assert.IsFalse(Any(mission => world.MissionIndicatorEntries.AsNoTracking().Any(entry => entry.ContentRevision == CrucibleMissionDataV1.Revision && entry.MissionId == mission)));
            Assert.IsFalse(Any(mission => world.Set<MissionAreaEntry>().AsNoTracking().Any(entry => entry.ContentRevision == CrucibleMissionDataV1.Revision && entry.MissionId == mission)));
            Assert.IsFalse(Any(mission => world.Set<MissionTriggerEntry>().AsNoTracking().Any(entry => entry.ContentRevision == CrucibleMissionDataV1.Revision && entry.MissionId == mission)));
            Assert.IsFalse(Any(mission => world.Set<MissionSceneBindingEntry>().AsNoTracking().Any(entry => entry.ContentRevision == CrucibleMissionDataV1.Revision && entry.MissionId == mission)));
            Assert.IsFalse(Any(mission => world.MissionRewardItemEntries.AsNoTracking().Any(entry => entry.ContentRevision == CrucibleMissionDataV1.Revision && entry.MissionId == mission)));

            world.GetService<IMigrator>().Migrate(KardashAndPrometheus);
            world.ChangeTracker.Clear();
            Check(true);
        }

        /// <summary>
        /// Crucible_fix_g (fix job G, to-fix row 61) puts Shield Drone pool 770026 back at radius 6 and takes out its evidence
        /// row and nothing else: the pool's place, its marker, the other drone pools, the missions and every row of sets 1-8a
        /// stay. Up again, the point is back.
        /// </summary>
        [TestMethod]
        public void FixGRollsBackWithItsMigration()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var world = harness.World;
            var (pool, before) = CrucibleFixGV1.Point;
            Assert.AreEqual((770026U, 6U), (pool, before), "the third Shield Drone pool, set 5's radius 6");
            double Radius(uint id) => world.SpawnPoolEntries.AsNoTracking().Single(entry => entry.Id == id).Radius;
            bool Evidence() => world.MissionEvidenceEntries.AsNoTracking().Any(entry => entry.ContentRevision == CrucibleMissionDataV1.Revision &&
                entry.MissionId == CrucibleOutOfSightV1.MissionId && entry.EvidenceId == CrucibleFixGV1.Evidence);
            int All() => world.MissionEvidenceEntries.AsNoTracking().Count(entry => entry.ContentRevision == CrucibleMissionDataV1.Revision &&
                !SetEightB.Contains(entry.MissionId) && !SetNine.Contains(entry.MissionId) && !SetElevenA.Contains(entry.MissionId) && !SetTwelveA.Contains(entry.MissionId) && !SetThirteen.Contains(entry.MissionId) && !SetElevenB.Contains(entry.MissionId) && !SetTwelveB.Contains(entry.MissionId) && !SetFifteen.Contains(entry.MissionId) &&
                !(entry.MissionId == CruciblePartOfTheBargainV1.MissionId && entry.EvidenceId == CrucibleFixHV1.Evidence) &&
                !(entry.MissionId == CrucibleMinesweeperV1.MissionId && entry.EvidenceId == CrucibleFixJV1.Evidence));
            bool Has(uint mission) => world.MissionContentDefinitionEntries.AsNoTracking()
                .Any(entry => entry.MissionId == mission && entry.ContentRevision == CrucibleMissionDataV1.Revision);
            var place = At(world, pool);
            var others = new[] { 770024U, 770025U }.ToDictionary(id => id, id => (At(world, id), Radius(id)));
            void Check(bool now, bool full = false)
            {
                Assert.AreEqual(now ? (double)CrucibleFixFV1.PointRadius : before, Radius(pool), $"pool {pool}'s radius");
                Assert.AreEqual(place, At(world, pool), "the place stays");
                foreach (var (id, (at, radius)) in others)
                    Assert.AreEqual((at, radius), (At(world, id), Radius(id)), $"pool {id} stays");
                Assert.AreEqual(now, Evidence());
                Assert.IsTrue(SetsOneToEightA.All(Has), "sets 1-8a stay");
                Assert.AreEqual(full, SetEightB.Concat(SetNine).Concat(SetElevenA).Concat(SetTwelveA).Concat(SetThirteen).Concat(SetElevenB).Concat(SetTwelveB).Concat(SetFifteen).Any(Has), "sets 8b, 9 and 11a are later migrations'");
                Assert.AreEqual(full ? AllRows : SetEightARows, Rows(world), "no other row comes or goes");
            }

            Check(true, full: true);
            var row = world.MissionEvidenceEntries.AsNoTracking().Single(entry => entry.ContentRevision == CrucibleMissionDataV1.Revision &&
                entry.MissionId == CrucibleOutOfSightV1.MissionId && entry.EvidenceId == CrucibleFixGV1.Evidence);
            Assert.AreEqual((MissionEvidenceSourceKind.Reconstruction, CrucibleFixGV1.Source, CrucibleFixGV1.Note, 0.3),
                (row.SourceKind, row.SourceUri, row.ReconstructionNote, row.Confidence));
            Assert.IsTrue(world.MissionIndicatorEntries.AsNoTracking().Any(entry => entry.ContentRevision == CrucibleMissionDataV1.Revision &&
                entry.MissionId == CrucibleOutOfSightV1.MissionId && System.Math.Abs(entry.PosX - place.Item1) + System.Math.Abs(entry.PosZ - place.Item3) < 0.01),
                "1208's marker stays on the pool");
            var all = All();

            Assert.AreEqual(KardashAndPrometheus, Previous(world, FixG), "the fix follows set 8a");
            world.GetService<IMigrator>().Migrate(KardashAndPrometheus);
            world.ChangeTracker.Clear();
            Check(false);
            Assert.AreEqual(all - 1, All(), "only the fix's row goes");

            world.GetService<IMigrator>().Migrate(FixG);
            world.ChangeTracker.Clear();
            Check(true);
            Assert.AreEqual(all, All());
        }

        /// <summary>
        /// Crucible_prometheus_outpost takes out its three missions (with their talks, markers, scene and evidence), the
        /// Juggernaut and Ugoretz (rows, stats, attacks, pools); sets 1-8a and fix job G stay. Up again, all of it is back (set 9,
        /// a later migration, stays out).
        /// </summary>
        [TestMethod]
        public void PrometheusOutpostRollsBackWithItsMigration()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var world = harness.World;
            bool Has(uint mission) => world.MissionContentDefinitionEntries.AsNoTracking()
                .Any(entry => entry.MissionId == mission && entry.ContentRevision == CrucibleMissionDataV1.Revision);
            bool Any(System.Func<uint, bool> rows) => SetEightB.Any(rows);
            var pools = CruciblePrometheusOutpostNpcsV1.JuggernautPools.Select(pool => pool.Pool).Append(CruciblePrometheusOutpostNpcsV1.UgoretzPool).ToArray();
            void Check(bool now, bool full = false)
            {
                foreach (var creature in new[] { Juggernaut, Ugoretz })
                {
                    Assert.AreEqual(now, world.CreatureEntries.AsNoTracking().Any(row => row.Id == creature), $"{creature}");
                    Assert.AreEqual(now, world.CreatureStatEntries.AsNoTracking().Any(row => row.Id == creature), $"{creature}'s stats");
                }
                Assert.AreEqual(now ? 2 : 0, world.Set<CreatureActionEntry>().AsNoTracking().Count(row => row.Id == 770041 || row.Id == 770042), "the Juggernaut's attacks");
                Assert.AreEqual(now ? 4 : 0, world.SpawnPoolEntries.AsNoTracking().Count(row => pools.Contains(row.Id)), "the four pools");
                Assert.IsTrue(SetsOneToEightA.All(Has), "sets 1-8a stay");
                Assert.AreEqual(now, SetEightB.All(Has));
                Assert.AreEqual(now, SetEightB.Any(Has));
                Assert.AreEqual(full, SetNine.Concat(SetElevenA).Concat(SetTwelveA).Concat(SetThirteen).Concat(SetElevenB).Concat(SetTwelveB).Concat(SetFifteen).Any(Has), "sets 9 and 11a are later migrations'");
                Assert.AreEqual(full ? AllRows : now ? SetEightBRows : SetEightARows, Rows(world));
                Assert.AreEqual((double)CrucibleFixFV1.PointRadius, world.SpawnPoolEntries.AsNoTracking().Single(row => row.Id == CrucibleFixGV1.Point.Pool).Radius,
                    "fix job G's point stays");
            }

            Check(true, full: true);
            Assert.AreEqual(FixG, Previous(world, PrometheusOutpost), "set 8b follows fix job G");
            world.GetService<IMigrator>().Migrate(FixG);
            world.ChangeTracker.Clear();
            Check(false);
            Assert.IsFalse(Any(mission => world.MissionEvidenceEntries.AsNoTracking().Any(entry => entry.ContentRevision == CrucibleMissionDataV1.Revision && entry.MissionId == mission)));
            Assert.IsFalse(Any(mission => world.MissionIndicatorEntries.AsNoTracking().Any(entry => entry.ContentRevision == CrucibleMissionDataV1.Revision && entry.MissionId == mission)));
            Assert.IsFalse(Any(mission => world.Set<MissionTriggerEntry>().AsNoTracking().Any(entry => entry.ContentRevision == CrucibleMissionDataV1.Revision && entry.MissionId == mission)));
            Assert.IsFalse(Any(mission => world.Set<MissionSceneBindingEntry>().AsNoTracking().Any(entry => entry.ContentRevision == CrucibleMissionDataV1.Revision && entry.MissionId == mission)));
            Assert.IsFalse(Any(mission => world.MissionRewardItemEntries.AsNoTracking().Any(entry => entry.ContentRevision == CrucibleMissionDataV1.Revision && entry.MissionId == mission)));
            Assert.IsFalse(Any(mission => world.MissionPrerequisiteEntries.AsNoTracking().Any(entry => entry.ContentRevision == CrucibleMissionDataV1.Revision && entry.MissionId == mission)));

            world.GetService<IMigrator>().Migrate(PrometheusOutpost);
            world.ChangeTracker.Clear();
            Check(true);
        }

        /// <summary>
        /// Crucible_khulago_strikes takes out its two missions (with their markers, scenes, rewards, prerequisite and evidence)
        /// and puts Captain Tiersky back on the Prometheus Outpost control point; sets 1-8b, Keplinger and Madias stay. Up again,
        /// all of it is back.
        /// </summary>
        [TestMethod]
        public void KhulagoStrikesRollsBackWithItsMigration()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var world = harness.World;
            bool Has(uint mission) => world.MissionContentDefinitionEntries.AsNoTracking()
                .Any(entry => entry.MissionId == mission && entry.ContentRevision == CrucibleMissionDataV1.Revision);
            bool Any(System.Func<uint, bool> rows) => SetNine.Any(rows);
            CollectionAssert.AreEqual(new[] { CruciblePumpUpTheJamV1.MissionId, CrucibleStrikeZoneV1.MissionId }, SetNine, "set 9 is 1224 and 1214");
            void Check(bool now, bool full = false)
            {
                foreach (var (pool, before, after) in CrucibleKhulagoStrikesNpcsV1.Moves)
                    Assert.AreEqual(now ? after : before, At(world, pool), $"pool {pool}");
                Assert.AreEqual((404.20001220703125, 169.77, 387.8999938964844), CrucibleKhulagoStrikesNpcsV1.Moves.Single().Before, "upstream's place, as the World DB has it");
                foreach (var (pool, _, after) in CrucibleKardashAndPrometheusNpcsV1.Moves)
                    Assert.AreEqual(after, At(world, pool), $"set 8a's pool {pool} stays");
                Assert.IsTrue(SetsOneToEightB.All(Has), "sets 1-8b stay");
                Assert.AreEqual(now, SetNine.All(Has));
                Assert.AreEqual(now, SetNine.Any(Has));
                Assert.AreEqual(full, SetElevenA.Concat(SetTwelveA).Concat(SetThirteen).Concat(SetElevenB).Concat(SetTwelveB).Concat(SetFifteen).Any(Has), "sets 11a and 12a are later migrations'");
                Assert.AreEqual(full ? AllRows : now ? SetNineRows : SetEightBRows, Rows(world));
            }

            Check(true, full: true);
            Assert.AreEqual(PrometheusOutpost, Previous(world, KhulagoStrikes), "set 9 follows set 8b");
            world.GetService<IMigrator>().Migrate(PrometheusOutpost);
            world.ChangeTracker.Clear();
            Check(false);
            Assert.IsFalse(Any(mission => world.MissionEvidenceEntries.AsNoTracking().Any(entry => entry.ContentRevision == CrucibleMissionDataV1.Revision && entry.MissionId == mission)));
            Assert.IsFalse(Any(mission => world.MissionIndicatorEntries.AsNoTracking().Any(entry => entry.ContentRevision == CrucibleMissionDataV1.Revision && entry.MissionId == mission)));
            Assert.IsFalse(Any(mission => world.Set<MissionTriggerEntry>().AsNoTracking().Any(entry => entry.ContentRevision == CrucibleMissionDataV1.Revision && entry.MissionId == mission)));
            Assert.IsFalse(Any(mission => world.Set<MissionSceneBindingEntry>().AsNoTracking().Any(entry => entry.ContentRevision == CrucibleMissionDataV1.Revision && entry.MissionId == mission)));
            Assert.IsFalse(Any(mission => world.MissionRewardItemEntries.AsNoTracking().Any(entry => entry.ContentRevision == CrucibleMissionDataV1.Revision && entry.MissionId == mission)));
            Assert.IsFalse(Any(mission => world.MissionPrerequisiteEntries.AsNoTracking().Any(entry => entry.ContentRevision == CrucibleMissionDataV1.Revision && entry.MissionId == mission)));

            world.GetService<IMigrator>().Migrate(KhulagoStrikes);
            world.ChangeTracker.Clear();
            Check(true);
        }

        /// <summary>
        /// Crucible_fix_h (fix job H, to-fix row 64) puts the Ultra Reconstructor's point pool 770038 and 1529's marker 9 back on
        /// the ledge and takes out its evidence row and nothing else: the pool stays a point, the other Junkyard pools, 1529's
        /// other markers and every row of sets 1-9 stay. Up again, the move is back.
        /// </summary>
        [TestMethod]
        public void FixHRollsBackWithItsMigration()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var world = harness.World;
            Assert.AreEqual((770038U, (-14.0, 335.14, 244.0)), (CrucibleFixHV1.Pool, CrucibleFixHV1.Before), "set 7's place");
            bool Evidence() => world.MissionEvidenceEntries.AsNoTracking().Any(entry => entry.ContentRevision == CrucibleMissionDataV1.Revision &&
                entry.MissionId == CruciblePartOfTheBargainV1.MissionId && entry.EvidenceId == CrucibleFixHV1.Evidence);
            // Set 11a (a later migration) is rolled back with the fix and not put back: its evidence is not counted.
            int All() => world.MissionEvidenceEntries.AsNoTracking().Count(entry => entry.ContentRevision == CrucibleMissionDataV1.Revision &&
                !SetElevenA.Contains(entry.MissionId) && !SetTwelveA.Contains(entry.MissionId) && !SetThirteen.Contains(entry.MissionId) && !SetElevenB.Contains(entry.MissionId) && !SetTwelveB.Contains(entry.MissionId) && !SetFifteen.Contains(entry.MissionId) &&
                !(entry.MissionId == CrucibleMinesweeperV1.MissionId && entry.EvidenceId == CrucibleFixJV1.Evidence));
            List<(uint, double, double, double)> Markers() => world.MissionIndicatorEntries.AsNoTracking()
                .Where(entry => entry.ContentRevision == CrucibleMissionDataV1.Revision && entry.MissionId == CruciblePartOfTheBargainV1.MissionId)
                .AsEnumerable().OrderBy(entry => entry.IndicatorId).Select(entry => (entry.IndicatorId, entry.PosX, entry.PosY, entry.PosZ)).ToList();
            var others = new uint[] { 770030, 770035, 770036, 770037 }.ToDictionary(id => id, id => At(world, id));
            var markers = Markers();
            Assert.HasCount(10, markers, "1529's ten markers");
            void Check(bool now)
            {
                var place = now ? CrucibleFixHV1.Now : CrucibleFixHV1.Before;
                Assert.AreEqual(place, At(world, CrucibleFixHV1.Pool), "the pool's place");
                Assert.AreEqual(0.0, world.SpawnPoolEntries.AsNoTracking().Single(entry => entry.Id == CrucibleFixHV1.Pool).Radius, "a point pool");
                foreach (var (id, at) in others)
                    Assert.AreEqual(at, At(world, id), $"pool {id} stays");
                var rows = Markers();
                Assert.AreEqual((CrucibleFixHV1.Marker, place.X, place.Y, place.Z), rows.Single(row => row.Item1 == CrucibleFixHV1.Marker), "marker 9 on the pool");
                CollectionAssert.AreEqual(markers.Where(row => row.Item1 != CrucibleFixHV1.Marker).ToList(),
                    rows.Where(row => row.Item1 != CrucibleFixHV1.Marker).ToList(), "the other markers stay");
                Assert.AreEqual(now, Evidence());
                Assert.IsTrue(MissionIds.Take(37).All(mission => world.MissionContentDefinitionEntries.AsNoTracking()
                    .Any(entry => entry.MissionId == mission && entry.ContentRevision == CrucibleMissionDataV1.Revision)), "sets 1-9 stay");
            }

            Check(true);
            var row = world.MissionEvidenceEntries.AsNoTracking().Single(entry => entry.ContentRevision == CrucibleMissionDataV1.Revision &&
                entry.MissionId == CruciblePartOfTheBargainV1.MissionId && entry.EvidenceId == CrucibleFixHV1.Evidence);
            Assert.AreEqual((MissionEvidenceSourceKind.Reconstruction, CrucibleFixHV1.Source, CrucibleFixHV1.Note, 0.3),
                (row.SourceKind, row.SourceUri, row.ReconstructionNote, row.Confidence));
            Assert.AreEqual(AllRows, Rows(world));
            var all = All();

            Assert.AreEqual(KhulagoStrikes, Previous(world, FixH), "the fix follows set 9");
            world.GetService<IMigrator>().Migrate(KhulagoStrikes);
            world.ChangeTracker.Clear();
            Check(false);
            Assert.AreEqual(all - 1, All(), "only the fix's row goes");
            Assert.AreEqual(SetNineRows, Rows(world), "no other row comes or goes (set 11a, a later migration, goes too)");

            world.GetService<IMigrator>().Migrate(FixH);
            world.ChangeTracker.Clear();
            Check(true);
            Assert.AreEqual(all, All());
            Assert.AreEqual(SetNineRows, Rows(world), "no other row comes or goes");
            Assert.IsFalse(SetElevenA.Concat(SetTwelveA).Concat(SetThirteen).Concat(SetElevenB).Concat(SetTwelveB).Concat(SetFifteen).Any(mission => world.MissionContentDefinitionEntries.AsNoTracking()
                .Any(entry => entry.MissionId == mission && entry.ContentRevision == CrucibleMissionDataV1.Revision)), "sets 11a and 12a are later migrations'");
        }

        /// <summary>
        /// Crucible_incurables_ward takes out its three missions (with their talks, markers, scenes, rewards, prerequisites and
        /// evidence), Shanul and Dhimand (rows, looks, stats, packages, pools), the Incurables, Jaikev and Parvatee (rows, stats,
        /// the attack, eleven pools), and puts Supervisor Sheira and Larai Sigal Tempa back at upstream's places; sets 1-9 and fix
        /// job H stay. Up again, all of it is back.
        /// </summary>
        [TestMethod]
        public void IncurablesWardRollsBackWithItsMigration()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var world = harness.World;
            bool Has(uint mission) => world.MissionContentDefinitionEntries.AsNoTracking()
                .Any(entry => entry.MissionId == mission && entry.ContentRevision == CrucibleMissionDataV1.Revision);
            bool Any(System.Func<uint, bool> rows) => SetElevenA.Any(rows);
            CollectionAssert.AreEqual(new[] { CrucibleSadisticSabotageV1.MissionId, CrucibleLostInTheAsylumV1.MissionId, CrucibleRehabilitationAtGunpointV1.MissionId },
                SetElevenA, "set 11a is 1445, 1444 and 1402");
            Assert.AreEqual(((-18.0, 62.61000061035156, -99.80000305175781), (-20.399999618530273, 187.77, 319.70001220703125)),
                (CrucibleIncurablesWardNpcsV1.Moves[0].Before, CrucibleIncurablesWardNpcsV1.Moves[1].Before), "upstream's places, as the World DB has them");
            var pools = new[] { Shanul, Dhimand, CrucibleIncurablesWardNpcsV1.JaikevPool, CrucibleIncurablesWardNpcsV1.ParvateePool }
                .Concat(CrucibleIncurablesWardNpcsV1.IncurablePools.Select(pool => pool.Pool)).ToArray();
            void Check(bool now, bool full = false)
            {
                foreach (var (pool, before, after) in CrucibleIncurablesWardNpcsV1.Moves)
                    Assert.AreEqual(now ? after : before, At(world, pool), $"pool {pool}");
                foreach (var creature in new[] { Shanul, Dhimand, Brute, Slayer, Jaikev, Parvatee })
                {
                    Assert.AreEqual(now, world.CreatureEntries.AsNoTracking().Any(row => row.Id == creature), $"{creature}");
                    Assert.AreEqual(now, world.CreatureStatEntries.AsNoTracking().Any(row => row.Id == creature), $"{creature}'s stats");
                }
                foreach (var npc in new[] { Shanul, Dhimand })
                {
                    Assert.AreEqual(now, world.CreatureAppearanceEntries.AsNoTracking().Any(row => row.Id == npc), $"{npc}'s look");
                    Assert.AreEqual(now, world.NpcPackageEntries.AsNoTracking().Any(row => row.Id == npc), $"{npc}'s package");
                }
                Assert.AreEqual(now, world.Set<CreatureActionEntry>().AsNoTracking().Any(row => row.Id == 770047), "the Incurables' attack");
                Assert.AreEqual(now ? pools.Length : 0, world.SpawnPoolEntries.AsNoTracking().Count(row => pools.Contains(row.Id)), "the thirteen pools");
                Assert.AreEqual(1640U, world.NpcPackageEntries.AsNoTracking().Single(row => row.Id == Sheira).PackageId, "Sheira keeps upstream's package");
                Assert.AreEqual(CrucibleFixHV1.Now, At(world, CrucibleFixHV1.Pool), "fix job H's move stays");
                Assert.IsTrue(SetsOneToNine.All(Has), "sets 1-9 stay");
                Assert.AreEqual(now, SetElevenA.All(Has));
                Assert.AreEqual(now, SetElevenA.Any(Has));
                Assert.AreEqual(full, SetTwelveA.Concat(SetThirteen).Concat(SetElevenB).Concat(SetTwelveB).Concat(SetFifteen).Any(Has), "sets 12a and 13 are later migrations'");
                Assert.AreEqual(full ? AllRows : now ? SetElevenARows : SetNineRows, Rows(world));
            }

            Check(true, full: true);
            Assert.AreEqual(FixH, Previous(world, IncurablesWard), "set 11a follows fix job H");
            world.GetService<IMigrator>().Migrate(FixH);
            world.ChangeTracker.Clear();
            Check(false);
            Assert.IsFalse(Any(mission => world.MissionEvidenceEntries.AsNoTracking().Any(entry => entry.ContentRevision == CrucibleMissionDataV1.Revision && entry.MissionId == mission)));
            Assert.IsFalse(Any(mission => world.MissionIndicatorEntries.AsNoTracking().Any(entry => entry.ContentRevision == CrucibleMissionDataV1.Revision && entry.MissionId == mission)));
            Assert.IsFalse(Any(mission => world.Set<MissionTriggerEntry>().AsNoTracking().Any(entry => entry.ContentRevision == CrucibleMissionDataV1.Revision && entry.MissionId == mission)));
            Assert.IsFalse(Any(mission => world.Set<MissionSceneBindingEntry>().AsNoTracking().Any(entry => entry.ContentRevision == CrucibleMissionDataV1.Revision && entry.MissionId == mission)));
            Assert.IsFalse(Any(mission => world.MissionRewardItemEntries.AsNoTracking().Any(entry => entry.ContentRevision == CrucibleMissionDataV1.Revision && entry.MissionId == mission)));
            Assert.IsFalse(Any(mission => world.MissionPrerequisiteEntries.AsNoTracking().Any(entry => entry.ContentRevision == CrucibleMissionDataV1.Revision && entry.MissionId == mission)));

            world.GetService<IMigrator>().Migrate(IncurablesWard);
            world.ChangeTracker.Clear();
            Check(true);
        }

        /// <summary>
        /// Crucible_chaukas takes out its three missions (with their talks, markers, scenes, rewards, prerequisites and
        /// evidence), Ruben and Brian (rows, looks, stats, packages, pools, Brian's pose); sets 1-11a stay, and the facility's
        /// upstream Claviger pools. Up again, all of it is back.
        /// </summary>
        [TestMethod]
        public void ChaukasRollsBackWithItsMigration()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var world = harness.World;
            bool Has(uint mission) => world.MissionContentDefinitionEntries.AsNoTracking()
                .Any(entry => entry.MissionId == mission && entry.ContentRevision == CrucibleMissionDataV1.Revision);
            bool Any(System.Func<uint, bool> rows) => SetTwelveA.Any(rows);
            CollectionAssert.AreEqual(new[] { CrucibleGetToChaukasV1.MissionId, CrucibleControlTheBrainV1.MissionId, CrucibleFixTheFactoryV1.MissionId },
                SetTwelveA, "set 12a is 1577, 1521 and 1522");
            var clavigers = world.SpawnPoolEntries.AsNoTracking().Count(row => row.MapContextId == CrucibleChaukasNpcsV1.Chaukas && row.Creature1Id == CrucibleChaukasNpcsV1.Claviger);
            Assert.AreEqual(15, clavigers, "the facility's fifteen upstream Claviger pools");
            int Pose() => world.Database.SqlQueryRaw<int>($"SELECT COUNT(*) AS Value FROM spawnpool_pose WHERE id = {Brian}").AsEnumerable().Single();
            void Check(bool now, bool full = false)
            {
                foreach (var npc in new[] { Ruben, Brian })
                {
                    Assert.AreEqual(now, world.CreatureEntries.AsNoTracking().Any(row => row.Id == npc), $"{npc}");
                    Assert.AreEqual(now, world.CreatureStatEntries.AsNoTracking().Any(row => row.Id == npc), $"{npc}'s stats");
                    Assert.AreEqual(now, world.CreatureAppearanceEntries.AsNoTracking().Any(row => row.Id == npc), $"{npc}'s look");
                    Assert.AreEqual(now, world.NpcPackageEntries.AsNoTracking().Any(row => row.Id == npc), $"{npc}'s package");
                    Assert.AreEqual(now, world.SpawnPoolEntries.AsNoTracking().Any(row => row.Id == npc), $"{npc}'s pool");
                }
                Assert.AreEqual(now ? 1 : 0, Pose(), "Brian's pose");
                Assert.AreEqual(clavigers, world.SpawnPoolEntries.AsNoTracking().Count(row => row.MapContextId == CrucibleChaukasNpcsV1.Chaukas &&
                    row.Creature1Id == CrucibleChaukasNpcsV1.Claviger), "the Claviger pools stay");
                Assert.IsTrue(SetsOneToElevenA.All(Has), "sets 1-11a stay");
                Assert.AreEqual(now, SetTwelveA.All(Has));
                Assert.AreEqual(now, SetTwelveA.Any(Has));
                Assert.AreEqual(full, SetThirteen.Concat(SetElevenB).Concat(SetTwelveB).Concat(SetFifteen).Any(Has), "sets 13 and 11b are later migrations'");
                Assert.AreEqual(full ? AllRows : now ? SetTwelveARows : SetElevenARows, Rows(world));
            }

            Check(true, full: true);
            Assert.AreEqual(IncurablesWard, Previous(world, Chaukas), "set 12a follows set 11a");
            world.GetService<IMigrator>().Migrate(IncurablesWard);
            world.ChangeTracker.Clear();
            Check(false);
            Assert.IsFalse(Any(mission => world.MissionEvidenceEntries.AsNoTracking().Any(entry => entry.ContentRevision == CrucibleMissionDataV1.Revision && entry.MissionId == mission)));
            Assert.IsFalse(Any(mission => world.MissionIndicatorEntries.AsNoTracking().Any(entry => entry.ContentRevision == CrucibleMissionDataV1.Revision && entry.MissionId == mission)));
            Assert.IsFalse(Any(mission => world.Set<MissionTriggerEntry>().AsNoTracking().Any(entry => entry.ContentRevision == CrucibleMissionDataV1.Revision && entry.MissionId == mission)));
            Assert.IsFalse(Any(mission => world.Set<MissionSceneBindingEntry>().AsNoTracking().Any(entry => entry.ContentRevision == CrucibleMissionDataV1.Revision && entry.MissionId == mission)));
            Assert.IsFalse(Any(mission => world.MissionRewardItemEntries.AsNoTracking().Any(entry => entry.ContentRevision == CrucibleMissionDataV1.Revision && entry.MissionId == mission)));
            Assert.IsFalse(Any(mission => world.MissionPrerequisiteEntries.AsNoTracking().Any(entry => entry.ContentRevision == CrucibleMissionDataV1.Revision && entry.MissionId == mission)));
            Assert.IsFalse(Any(mission => world.Set<MissionScenarioEntry>().AsNoTracking().Any(entry => entry.ContentRevision == CrucibleMissionDataV1.Revision && entry.MissionId == mission)));

            world.GetService<IMigrator>().Migrate(Chaukas);
            world.ChangeTracker.Clear();
            Check(true);
        }

        /// <summary>
        /// Crucible_hybrid_arc takes out its four missions (with their talks, markers, scenes, rewards, prerequisites and
        /// evidence), Stark and the two Skeevs (rows, looks, stats, packages, Stark's pool), the two laboratory pads and their
        /// links, and puts Capriulo and Eugin back at upstream's places; sets 1-12a stay. Up again, all of it is back.
        /// </summary>
        [TestMethod]
        public void HybridArcRollsBackWithItsMigration()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var world = harness.World;
            bool Has(uint mission) => world.MissionContentDefinitionEntries.AsNoTracking()
                .Any(entry => entry.MissionId == mission && entry.ContentRevision == CrucibleMissionDataV1.Revision);
            bool Any(System.Func<uint, bool> rows) => SetThirteen.Any(rows);
            CollectionAssert.AreEqual(new[] { CrucibleDarkGenesisV1.MissionId, CrucibleGeneMachineV1.MissionId, CrucibleDesignerGenesV1.MissionId,
                CrucibleGenomeSweetGenomeV1.MissionId }, SetThirteen, "set 13 is 1856, 1857, 1881 and 1899");
            Assert.AreEqual((CrucibleHybridArcNpcsV1.Moves[0].Before, CrucibleHybridArcNpcsV1.Moves[1].Before),
                ((-17.5, 187.77, 317.6000061035156), (-13.899999618530273, 187.77, 317.6000061035156)), "upstream's places, as the World DB has them");
            int Count(string sql) => world.Database.SqlQueryRaw<int>(sql).AsEnumerable().Single();
            void Check(bool now, bool full = false)
            {
                foreach (var (pool, before, after) in CrucibleHybridArcNpcsV1.Moves)
                    Assert.AreEqual(now ? after : before, At(world, pool), $"pool {pool}");
                foreach (var npc in new[] { Stark, SkeevTalker, SkeevFighter })
                {
                    Assert.AreEqual(now, world.CreatureEntries.AsNoTracking().Any(row => row.Id == npc), $"{npc}");
                    Assert.AreEqual(now, world.CreatureStatEntries.AsNoTracking().Any(row => row.Id == npc), $"{npc}'s stats");
                    Assert.AreEqual(now, world.CreatureAppearanceEntries.AsNoTracking().Any(row => row.Id == npc), $"{npc}'s look");
                }
                foreach (var npc in new[] { Stark, SkeevTalker })
                    Assert.AreEqual(now, world.NpcPackageEntries.AsNoTracking().Any(row => row.Id == npc), $"{npc}'s package");
                Assert.AreEqual(now, world.SpawnPoolEntries.AsNoTracking().Any(row => row.Id == Stark), "Stark's pool");
                Assert.AreEqual(now ? 2 : 0, Count("SELECT COUNT(*) AS Value FROM teleporter WHERE id IN (911, 912)"), "the laboratory pads");
                Assert.AreEqual(now ? 2 : 0, Count("SELECT COUNT(*) AS Value FROM local_teleporter_link WHERE from_teleporter_id IN (911, 912)"), "their links");
                Assert.AreEqual(2, Count("SELECT COUNT(*) AS Value FROM local_teleporter_link WHERE from_teleporter_id IN (909, 910)"), "the Kappa pads' links stay");
                Assert.IsTrue(SetsOneToTwelveA.All(Has), "sets 1-12a stay");
                Assert.AreEqual(now, SetThirteen.All(Has));
                Assert.AreEqual(now, SetThirteen.Any(Has));
                Assert.AreEqual(full, SetElevenB.Concat(SetTwelveB).Concat(SetFifteen).Any(Has), "sets 11b and 12b are later migrations'");
                Assert.AreEqual(full ? AllRows : now ? SetThirteenRows : SetTwelveARows, Rows(world));
            }

            Check(true, full: true);
            Assert.AreEqual(Chaukas, Previous(world, HybridArc), "set 13 follows set 12a");
            world.GetService<IMigrator>().Migrate(Chaukas);
            world.ChangeTracker.Clear();
            Check(false);
            Assert.IsFalse(Any(mission => world.MissionEvidenceEntries.AsNoTracking().Any(entry => entry.ContentRevision == CrucibleMissionDataV1.Revision && entry.MissionId == mission)));
            Assert.IsFalse(Any(mission => world.MissionIndicatorEntries.AsNoTracking().Any(entry => entry.ContentRevision == CrucibleMissionDataV1.Revision && entry.MissionId == mission)));
            Assert.IsFalse(Any(mission => world.Set<MissionTriggerEntry>().AsNoTracking().Any(entry => entry.ContentRevision == CrucibleMissionDataV1.Revision && entry.MissionId == mission)));
            Assert.IsFalse(Any(mission => world.Set<MissionSceneBindingEntry>().AsNoTracking().Any(entry => entry.ContentRevision == CrucibleMissionDataV1.Revision && entry.MissionId == mission)));
            Assert.IsFalse(Any(mission => world.MissionRewardItemEntries.AsNoTracking().Any(entry => entry.ContentRevision == CrucibleMissionDataV1.Revision && entry.MissionId == mission)));
            Assert.IsFalse(Any(mission => world.MissionPrerequisiteEntries.AsNoTracking().Any(entry => entry.ContentRevision == CrucibleMissionDataV1.Revision && entry.MissionId == mission)));
            Assert.IsFalse(Any(mission => world.Set<MissionScenarioEntry>().AsNoTracking().Any(entry => entry.ContentRevision == CrucibleMissionDataV1.Revision && entry.MissionId == mission)));

            world.GetService<IMigrator>().Migrate(Chaukas);
            world.ChangeTracker.Clear();
            Check(false);
            world.GetService<IMigrator>().Migrate(HybridArc);
            world.ChangeTracker.Clear();
            Check(true);
            world.GetService<IMigrator>().Migrate(HybridArc);
            world.ChangeTracker.Clear();
            Check(true);
        }

        /// <summary>
        /// Crucible_fix_i (fix job I, to-fix row 69, triage item 24) puts the Ward door markers of 1445 and 1402 back at
        /// (-216, 180.83, 400) r6 and 1521's container marker back on the container, and takes out its three evidence rows and
        /// nothing else: every other marker and row stays. Up again, the moves are back.
        /// </summary>
        [TestMethod]
        public void FixIRollsBackWithItsMigration()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var world = harness.World;
            Assert.AreEqual(((-216.0, 180.83, 400.0), 6.0), (CrucibleFixIV1.DoorWayBefore, CrucibleFixIV1.DoorRadiusBefore), "set 11a's door marker");
            Assert.AreEqual((-16.0, -164.0), (CrucibleFixIV1.ContainerMarkerBefore.X, CrucibleFixIV1.ContainerMarkerBefore.Z), "set 12a's container marker");
            List<(uint, uint, uint, double, double, double, double)> Markers() => world.MissionIndicatorEntries.AsNoTracking()
                .Where(entry => entry.ContentRevision == CrucibleMissionDataV1.Revision).AsEnumerable()
                .OrderBy(entry => entry.MissionId).ThenBy(entry => entry.ObjectiveId).ThenBy(entry => entry.IndicatorId)
                .Select(entry => (entry.MissionId, entry.ObjectiveId, entry.IndicatorId, entry.PosX, entry.PosY, entry.PosZ, entry.Radius)).ToList();
            bool Moved((uint Mission, uint Objective, uint Indicator, double, double, double, double) row) =>
                CrucibleFixIV1.DoorMarkers.Contains((row.Mission, row.Objective, row.Indicator)) ||
                row == (CrucibleControlTheBrainV1.MissionId, CrucibleFixIV1.ContainerObjective, CrucibleFixIV1.ContainerMarker, row.Item4, row.Item5, row.Item6, row.Item7);
            int Evidence() => world.MissionEvidenceEntries.AsNoTracking().Count(entry => entry.ContentRevision == CrucibleMissionDataV1.Revision &&
                CrucibleFixIV1.Missions.Contains(entry.MissionId) && entry.EvidenceId == CrucibleFixIV1.Evidence);
            // Set 11b, fix job J and set 12b (later migrations) are rolled back with the fix and not put back: their evidence is not counted.
            int All() => world.MissionEvidenceEntries.AsNoTracking().Count(entry => entry.ContentRevision == CrucibleMissionDataV1.Revision &&
                !SetElevenB.Contains(entry.MissionId) && !SetTwelveB.Contains(entry.MissionId) && !SetFifteen.Contains(entry.MissionId) &&
                !(entry.MissionId == CrucibleMinesweeperV1.MissionId && entry.EvidenceId == CrucibleFixJV1.Evidence));
            var markers = Markers();
            Assert.HasCount(Indicators, markers);
            Assert.AreEqual(5, markers.Count(Moved), "four door markers and the container's");
            void Check(bool now)
            {
                var rows = Markers();
                var door = now ? CrucibleFixIV1.DoorWayNow : CrucibleFixIV1.DoorWayBefore;
                var radius = now ? CrucibleFixIV1.DoorRadiusNow : CrucibleFixIV1.DoorRadiusBefore;
                foreach (var (mission, objective, indicator) in CrucibleFixIV1.DoorMarkers)
                    Assert.AreEqual((door.X, door.Y, door.Z, radius), rows.Where(row => (row.Item1, row.Item2, row.Item3) == (mission, objective, indicator))
                        .Select(row => (row.Item4, row.Item5, row.Item6, row.Item7)).Single(), $"{mission}.{objective}/{indicator}");
                var container = now ? CrucibleFixIV1.ContainerMarkerNow : CrucibleFixIV1.ContainerMarkerBefore;
                Assert.AreEqual((container.X, container.Y, container.Z, (double)CrucibleControlTheBrainV1.ContainerRadius), rows
                    .Where(row => (row.Item1, row.Item2, row.Item3) == (CrucibleControlTheBrainV1.MissionId, CrucibleFixIV1.ContainerObjective, CrucibleFixIV1.ContainerMarker))
                    .Select(row => (row.Item4, row.Item5, row.Item6, row.Item7)).Single(), "1521's container marker");
                CollectionAssert.AreEqual(markers.Where(row => !Moved(row) && row.Item1 != CrucibleLeakyPipesV1.MissionId && row.Item1 != CrucibleFactoryFortificationV1.MissionId && row.Item1 != CrucibleBloodFireSteelV1.MissionId).ToList(),
                    rows.Where(row => !Moved(row) && row.Item1 != CrucibleLeakyPipesV1.MissionId && row.Item1 != CrucibleFactoryFortificationV1.MissionId && row.Item1 != CrucibleBloodFireSteelV1.MissionId).ToList(), "the other markers stay");
                Assert.AreEqual(now ? 3 : 0, Evidence());
                Assert.IsTrue(SetsOneToThirteen.All(mission => world.MissionContentDefinitionEntries.AsNoTracking()
                    .Any(entry => entry.MissionId == mission && entry.ContentRevision == CrucibleMissionDataV1.Revision)), "sets 1-13 stay");
            }

            Check(true);
            foreach (var mission in CrucibleFixIV1.Missions)
            {
                var row = world.MissionEvidenceEntries.AsNoTracking().Single(entry => entry.ContentRevision == CrucibleMissionDataV1.Revision &&
                    entry.MissionId == mission && entry.EvidenceId == CrucibleFixIV1.Evidence);
                Assert.AreEqual((MissionEvidenceSourceKind.Reconstruction, CrucibleFixIV1.Source,
                        mission == CrucibleControlTheBrainV1.MissionId ? CrucibleFixIV1.ContainerNote : CrucibleFixIV1.DoorNote, 0.3),
                    (row.SourceKind, row.SourceUri, row.ReconstructionNote, row.Confidence), $"{mission}");
            }
            var all = All();

            Assert.AreEqual(HybridArc, Previous(world, FixI), "the fix follows set 13");
            world.GetService<IMigrator>().Migrate(HybridArc);
            world.ChangeTracker.Clear();
            Check(false);
            Assert.AreEqual(all - 3, All(), "only the fix's rows go");
            Assert.AreEqual(SetThirteenRows, Rows(world), "no other row comes or goes (sets 11b and 12b, later migrations, go too)");

            world.GetService<IMigrator>().Migrate(FixI);
            world.ChangeTracker.Clear();
            Check(true);
            Assert.AreEqual(all, All());
            Assert.AreEqual(SetThirteenRows, Rows(world));
            Assert.IsFalse(SetElevenB.Concat(SetTwelveB).Concat(SetFifteen).Any(mission => world.MissionContentDefinitionEntries.AsNoTracking()
                .Any(entry => entry.MissionId == mission && entry.ContentRevision == CrucibleMissionDataV1.Revision)), "sets 11b and 12b are later migrations'");
        }

        /// <summary>
        /// Crucible_incurables_ward_wings takes out Leaky Pipes (its talks, markers, area, scene, scenarios, reward and evidence),
        /// puts Dhimand's speeds back to 0 and takes out his pool's pose; sets 1-13 and fix job I stay. Up again, all of it is back.
        /// </summary>
        [TestMethod]
        public void IncurablesWardWingsRollsBackWithItsMigration()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var world = harness.World;
            bool Has(uint mission) => world.MissionContentDefinitionEntries.AsNoTracking()
                .Any(entry => entry.MissionId == mission && entry.ContentRevision == CrucibleMissionDataV1.Revision);
            bool Any(System.Func<uint, bool> rows) => SetElevenB.Any(rows);
            CollectionAssert.AreEqual(new[] { CrucibleLeakyPipesV1.MissionId }, SetElevenB, "set 11b is 1398 (1401 is set 15's)");
            int Pose() => world.Database.SqlQueryRaw<int>($"SELECT COUNT(*) AS Value FROM spawnpool_pose WHERE id = {Dhimand}").AsEnumerable().Single();
            void Check(bool now, bool full = false)
            {
                var dhimand = world.CreatureEntries.AsNoTracking().Single(row => row.Id == Dhimand);
                Assert.AreEqual(now ? (7U, 2U) : (0U, 0U), ((uint)dhimand.RunSpeed, (uint)dhimand.WalkSpeed), "Dhimand's speeds");
                Assert.AreEqual(now ? 1 : 0, Pose(), "Dhimand's pose");
                Assert.IsTrue(world.SpawnPoolEntries.AsNoTracking().Any(row => row.Id == Dhimand), "Dhimand's pool stays (set 11a's)");
                Assert.IsTrue(SetsOneToThirteen.All(Has), "sets 1-13 stay");
                Assert.AreEqual(now, SetElevenB.All(Has));
                Assert.AreEqual(full, SetTwelveB.Concat(SetFifteen).Any(Has), "sets 12b and 15 are later migrations'");
                Assert.AreEqual(full ? AllRows : now ? SetElevenBRows : SetThirteenRows, Rows(world));
                Assert.AreEqual(3, world.MissionEvidenceEntries.AsNoTracking().Count(entry => entry.ContentRevision == CrucibleMissionDataV1.Revision &&
                    CrucibleFixIV1.Missions.Contains(entry.MissionId) && entry.EvidenceId == CrucibleFixIV1.Evidence), "fix job I stays");
            }

            Check(true, full: true);
            Assert.AreEqual(FixI, Previous(world, IncurablesWardWings), "set 11b follows fix job I");
            world.GetService<IMigrator>().Migrate(FixI);
            world.ChangeTracker.Clear();
            Check(false);
            Assert.IsFalse(Any(mission => world.MissionEvidenceEntries.AsNoTracking().Any(entry => entry.ContentRevision == CrucibleMissionDataV1.Revision && entry.MissionId == mission)));
            Assert.IsFalse(Any(mission => world.MissionIndicatorEntries.AsNoTracking().Any(entry => entry.ContentRevision == CrucibleMissionDataV1.Revision && entry.MissionId == mission)));
            Assert.IsFalse(Any(mission => world.Set<MissionTriggerEntry>().AsNoTracking().Any(entry => entry.ContentRevision == CrucibleMissionDataV1.Revision && entry.MissionId == mission)));
            Assert.IsFalse(Any(mission => world.Set<MissionSceneBindingEntry>().AsNoTracking().Any(entry => entry.ContentRevision == CrucibleMissionDataV1.Revision && entry.MissionId == mission)));
            Assert.IsFalse(Any(mission => world.Set<MissionRewardDefinitionEntry>().AsNoTracking().Any(entry => entry.ContentRevision == CrucibleMissionDataV1.Revision && entry.MissionId == mission)));
            Assert.IsFalse(Any(mission => world.Set<MissionScenarioEntry>().AsNoTracking().Any(entry => entry.ContentRevision == CrucibleMissionDataV1.Revision && entry.MissionId == mission)));
            Assert.IsFalse(Any(mission => world.Set<MissionAreaEntry>().AsNoTracking().Any(entry => entry.ContentRevision == CrucibleMissionDataV1.Revision && entry.MissionId == mission)));

            world.GetService<IMigrator>().Migrate(IncurablesWardWings);
            world.ChangeTracker.Clear();
            Check(true);
            world.GetService<IMigrator>().Migrate(IncurablesWardWings);
            world.ChangeTracker.Clear();
            Check(true);
        }

        /// <summary>
        /// Crucible_chaukas_factory takes out Factory Fortification and Information is Key (their objectives, triggers, areas,
        /// markers, scenes, scenarios, rewards, prerequisites and evidence) and the Refitted Warden Bot's row and stats; sets 1-11b,
        /// fix jobs I and J and set 14's tints stay. Up again, all of it is back. Its rollback target is the migration before it
        /// (Previous; 20271108002200_Crucible_fix_j now).
        /// </summary>
        [TestMethod]
        public void ChaukasFactoryRollsBackWithItsMigration()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var world = harness.World;
            bool Has(uint mission) => world.MissionContentDefinitionEntries.AsNoTracking()
                .Any(entry => entry.MissionId == mission && entry.ContentRevision == CrucibleMissionDataV1.Revision);
            bool Any(System.Func<uint, bool> rows) => SetTwelveB.Any(rows);
            CollectionAssert.AreEqual(new[] { CrucibleFactoryFortificationV1.MissionId, CrucibleInformationIsKeyV1.MissionId }, SetTwelveB,
                "set 12b is 1523 and 1525");
            int Tints() => world.Database.SqlQueryRaw<int>("SELECT COUNT(*) AS Value FROM creature_tint").AsEnumerable().Single();
            var tints = Tints();
            void Check(bool now, bool full = false)
            {
                Assert.AreEqual(now, world.CreatureEntries.AsNoTracking().Any(row => row.Id == RefittedBot), "the Refitted Warden Bot");
                Assert.AreEqual(now, world.CreatureStatEntries.AsNoTracking().Any(row => row.Id == RefittedBot), "its stats");
                Assert.IsTrue(SetsOneToElevenB.All(Has), "sets 1-11b stay");
                Assert.AreEqual(now, SetTwelveB.All(Has));
                Assert.AreEqual(now, SetTwelveB.Any(Has));
                Assert.AreEqual(full, SetFifteen.Any(Has), "set 15 is a later migration's: it goes with the rollback and is not put back");
                Assert.AreEqual(full ? AllRows : now ? SetTwelveBRows : SetElevenBRows, Rows(world));
                Assert.AreEqual(tints, Tints(), "set 14's tints stay");
                Assert.AreEqual(3, world.MissionEvidenceEntries.AsNoTracking().Count(entry => entry.ContentRevision == CrucibleMissionDataV1.Revision &&
                    CrucibleFixIV1.Missions.Contains(entry.MissionId) && entry.EvidenceId == CrucibleFixIV1.Evidence), "fix job I stays");
            }

            Check(true, full: true);
            var previous = Previous(world, ChaukasFactory);
            Assert.AreEqual(FixJ, previous, "set 12b follows fix job J");
            world.GetService<IMigrator>().Migrate(previous);
            world.ChangeTracker.Clear();
            Check(false);
            Assert.IsFalse(Any(mission => world.MissionEvidenceEntries.AsNoTracking().Any(entry => entry.ContentRevision == CrucibleMissionDataV1.Revision && entry.MissionId == mission)));
            Assert.IsFalse(Any(mission => world.MissionIndicatorEntries.AsNoTracking().Any(entry => entry.ContentRevision == CrucibleMissionDataV1.Revision && entry.MissionId == mission)));
            Assert.IsFalse(Any(mission => world.MissionObjectiveDefinitionEntries.AsNoTracking().Any(entry => entry.ContentRevision == CrucibleMissionDataV1.Revision && entry.MissionId == mission)));
            Assert.IsFalse(Any(mission => world.Set<MissionTriggerEntry>().AsNoTracking().Any(entry => entry.ContentRevision == CrucibleMissionDataV1.Revision && entry.MissionId == mission)));
            Assert.IsFalse(Any(mission => world.Set<MissionSceneBindingEntry>().AsNoTracking().Any(entry => entry.ContentRevision == CrucibleMissionDataV1.Revision && entry.MissionId == mission)));
            Assert.IsFalse(Any(mission => world.Set<MissionRewardDefinitionEntry>().AsNoTracking().Any(entry => entry.ContentRevision == CrucibleMissionDataV1.Revision && entry.MissionId == mission)));
            Assert.IsFalse(Any(mission => world.MissionRewardItemEntries.AsNoTracking().Any(entry => entry.ContentRevision == CrucibleMissionDataV1.Revision && entry.MissionId == mission)));
            Assert.IsFalse(Any(mission => world.MissionPrerequisiteEntries.AsNoTracking().Any(entry => entry.ContentRevision == CrucibleMissionDataV1.Revision && entry.MissionId == mission)));
            Assert.IsFalse(Any(mission => world.Set<MissionScenarioEntry>().AsNoTracking().Any(entry => entry.ContentRevision == CrucibleMissionDataV1.Revision && entry.MissionId == mission)));
            Assert.IsFalse(Any(mission => world.Set<MissionAreaEntry>().AsNoTracking().Any(entry => entry.ContentRevision == CrucibleMissionDataV1.Revision && entry.MissionId == mission)));

            world.GetService<IMigrator>().Migrate(previous);
            world.ChangeTracker.Clear();
            Check(false);
            world.GetService<IMigrator>().Migrate(ChaukasFactory);
            world.ChangeTracker.Clear();
            Check(true);
            world.GetService<IMigrator>().Migrate(ChaukasFactory);
            world.ChangeTracker.Clear();
            Check(true);
        }

        /// <summary>
        /// Crucible_blood_fire_steel takes out Blood, Fire, and Steel (its talk, markers, scene, scenarios, reward and evidence),
        /// Atali, Umank and the Headsman (rows, stats) and their four pools, and puts Incurable pool 770048's radius back to 5;
        /// sets 1-12b stay. Up again, all of it is back. Its rollback target is the migration before it
        /// (Previous; 20271108002300_Crucible_chaukas_factory now).
        /// </summary>
        [TestMethod]
        public void BloodFireSteelRollsBackWithItsMigration()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var world = harness.World;
            bool Has(uint mission) => world.MissionContentDefinitionEntries.AsNoTracking()
                .Any(entry => entry.MissionId == mission && entry.ContentRevision == CrucibleMissionDataV1.Revision);
            bool Any(System.Func<uint, bool> rows) => SetFifteen.Any(rows);
            CollectionAssert.AreEqual(new[] { CrucibleBloodFireSteelV1.MissionId }, SetFifteen, "set 15 is 1401");
            var creatures = new[] { CrucibleBloodFireSteelV1.Atali, CrucibleBloodFireSteelV1.Umank, CrucibleBloodFireSteelV1.Headsman };
            var pools = new[] { CrucibleBloodFireSteelV1.AtaliPool, CrucibleBloodFireSteelV1.UmankPool, CrucibleBloodFireSteelV1.AtaliGuards,
                CrucibleBloodFireSteelV1.UmankGuards };
            double Radius(uint pool) => world.SpawnPoolEntries.AsNoTracking().Single(entry => entry.Id == pool).Radius;
            void Check(bool now)
            {
                Assert.AreEqual(now, SetFifteen.All(Has));
                Assert.IsTrue(SetsOneToTwelveB.All(Has), "sets 1-12b stay");
                Assert.AreEqual(now ? AllRows : SetTwelveBRows, Rows(world));
                Assert.AreEqual(now ? 3 : 0, world.CreatureEntries.AsNoTracking().Count(row => creatures.Contains(row.Id)), "Atali, Umank, the Headsman");
                Assert.AreEqual(now ? 4 : 0, world.SpawnPoolEntries.AsNoTracking().Count(row => pools.Contains(row.Id)), "their pools");
                Assert.AreEqual(now ? 0.0 : CrucibleBloodFireSteelV1.PointedPoolRadiusBefore, Radius(CrucibleBloodFireSteelV1.PointedPool), "pool 770048's radius");
            }

            Check(true);
            Assert.AreEqual(ChaukasFactory, Previous(world, BloodFireSteel), "set 15 follows set 12b");
            world.GetService<IMigrator>().Migrate(Previous(world, BloodFireSteel));
            world.ChangeTracker.Clear();
            Check(false);
            Assert.IsFalse(Any(mission => world.MissionEvidenceEntries.AsNoTracking().Any(entry => entry.ContentRevision == CrucibleMissionDataV1.Revision && entry.MissionId == mission)));
            Assert.IsFalse(Any(mission => world.MissionIndicatorEntries.AsNoTracking().Any(entry => entry.ContentRevision == CrucibleMissionDataV1.Revision && entry.MissionId == mission)));
            Assert.IsFalse(Any(mission => world.Set<MissionTriggerEntry>().AsNoTracking().Any(entry => entry.ContentRevision == CrucibleMissionDataV1.Revision && entry.MissionId == mission)));
            Assert.IsFalse(Any(mission => world.Set<MissionSceneBindingEntry>().AsNoTracking().Any(entry => entry.ContentRevision == CrucibleMissionDataV1.Revision && entry.MissionId == mission)));
            Assert.IsFalse(Any(mission => world.Set<MissionRewardDefinitionEntry>().AsNoTracking().Any(entry => entry.ContentRevision == CrucibleMissionDataV1.Revision && entry.MissionId == mission)));
            Assert.IsFalse(Any(mission => world.Set<MissionScenarioEntry>().AsNoTracking().Any(entry => entry.ContentRevision == CrucibleMissionDataV1.Revision && entry.MissionId == mission)));

            world.GetService<IMigrator>().Migrate(BloodFireSteel);
            world.ChangeTracker.Clear();
            Check(true);
            world.GetService<IMigrator>().Migrate(BloodFireSteel);
            world.ChangeTracker.Clear();
            Check(true);
        }

        internal static List<(uint MissionId, Rasa.Missions.Content.MissionSceneDefinition Scene)> Scenes(WildernessRuntimeTestHarness harness) =>
            harness.World.Set<MissionSceneBindingEntry>().AsNoTracking().Where(entry => entry.ContentRevision == CrucibleMissionDataV1.Revision)
                .ToList().Select(entry => (entry.MissionId, System.Text.Json.JsonSerializer.Deserialize<Rasa.Missions.Content.MissionSceneDefinition>(
                    entry.Bindings, Rasa.Missions.Content.MissionContentCodec.Options))).ToList();
    }
}
