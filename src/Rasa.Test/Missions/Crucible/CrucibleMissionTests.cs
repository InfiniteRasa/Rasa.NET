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
            CrucibleOtherSideOfTheCoinV1.MissionId, CrucibleMakingAmendsV1.MissionId, CrucibleMakingAmendsV1.RevengeMissionId
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
        /// Coin's one at Cheung; Making Amends' and Sweet Revenge's three each, at Munit, DeLisi and Breiburg.
        /// </summary>
        internal const int Indicators = 8 + 2 + 6 + 3 + 3 + 9 + 2 + 8 + 1 + 3 + 1 + 10 + 15 + 4 + 3 + 3 + 7 + 2 + 1 + 3 + 3;

        private const string OutpostIntrepid = "20271108000000_Crucible_outpost_intrepid";
        private const string GrupaChiefs = "20271108000100_Crucible_grupa_chiefs";
        private const string WardensAndLashers = "20271108000200_Crucible_wardens_and_lashers";
        private const string JunkyardDogsMarkers = "20271108000300_Crucible_junkyard_dogs_markers";
        private const string MinefieldAndSalvage = "20271108000400_Crucible_minefield_and_salvage";
        private const string StaalWalks = "20271108000500_Crucible_staal_walks";
        private const string StaalSigals = "20271108000600_Crucible_staal_sigals";
        private const string AwolCamp = "20271108000700_Crucible_awol_camp";

        private const uint Shandy = 770000, Mullins = 770001, Munit = 770002;
        private const uint DeLisi = 510019, Drake = 510034, Vitto = 510027, Donal = 510037, Rigs = 510025, DAmbrose = 510023;
        private const uint Bunko = 770003, Yeg = 770004, Roscoe = 770005;
        private const uint Lohen = 510024, Cugin = 510032, Vargo = 510033, Fiorella = 510022, Haggus = 510026, Melching = 510018;
        private const uint Halprin = 770006, WardenBot = 770007, Lasher = 770008;
        private const uint SmartMine = 770014, Reconstructor = 770015, Derge = 520013;
        private const uint Nema = 770019, Pico = 770020, Trista = 770021, ShieldDrone = 770022, Molitor = 770023;
        private const uint Cheung = 510015, Deirdre = 510016, Breiburg = 510017, Sherer = 510030;

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
                (1205, Breiburg, Breiburg, 40)
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
                // where upstream has him. The Staal walks fix: Munit on the lower corridor, Vargo out of the crafting station.
                (Bunko, -197, 319), (Yeg, -153.7, 346.9), (Lohen, -215, 326), (Cugin, -174.3, 280.2), (Fiorella, 959, 111),
                (Haggus, -400, 270), (Vargo, -190.5, 267.2), (Melching, -945.9, 246.9),
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
                (770009, WardenBot, -528.6, 546.6), (770010, WardenBot, -493.6, 581.6), (770011, WardenBot, -578.6, 561.6),
                (770012, Lasher, -785, 390), (770013, Lasher, -790, 420)
            })
            {
                var row = world.SpawnPoolEntries.AsNoTracking().Single(entry => entry.Id == pool);
                Assert.AreEqual((1993U, creature, (byte)3, (byte)3, 1200U), (row.MapContextId, row.Creature1Id, row.Creature1MinCount,
                    row.Creature1MaxCount, (uint)row.RespawnTime), $"pool {pool}");
                Assert.IsLessThan(0.01, System.Math.Abs(row.PosX - x) + System.Math.Abs(row.PosZ - z), $"pool {pool} at ({x}, {z})");
            }

            // Set 4's creatures: the Smart Mine, a copy of the Necromite 570003 (client name 433 "Necromite", class 7528) with its
            // weapon 1/109 and its self-destruct 489 at its own lower damage, damage_type 0 (the client decides); the
            // Reconstructor Bot (class 6960, client name 8033 "Reconstructor Bot Laborer", one attack 1/152 of the client's
            // Weapon_Creature_Arieki_Bot_Reconstructor 7189, 1500 ms); hostile, their own hit points.
            var mine = world.CreatureEntries.AsNoTracking().Single(row => row.Id == SmartMine);
            var necromite = world.CreatureEntries.AsNoTracking().Single(row => row.Id == 570003);
            Assert.AreEqual((433U, 40U, 0U, 7528U, 1500U, 770014U, 770016U, 0U), (mine.NameId, mine.Level, mine.Faction, mine.ClassId,
                mine.MaxHitPoints, mine.Action1, mine.Action2, mine.Action3));
            Assert.AreEqual((necromite.RunSpeed, necromite.WalkSpeed), (mine.RunSpeed, mine.WalkSpeed), "the Necromite's speeds");
            var mineActions = world.Set<CreatureActionEntry>().AsNoTracking().Where(row => row.Id == 770014 || row.Id == 770016).ToDictionary(row => row.Id);
            Assert.AreEqual((1U, 109U, 0U), ((uint)mineActions[770014].ActionId, (uint)mineActions[770014].ActionArgId, (uint)mineActions[770014].DamageType),
                "the Necromite's weapon, damage_type 0");
            Assert.AreEqual((489U, 1U, 0U), ((uint)mineActions[770016].ActionId, (uint)mineActions[770016].ActionArgId, (uint)mineActions[770016].DamageType),
                "necromite_self_destruct, damage_type 0");
            // TaRapedia: "very little damage": well under the Necromite's 320-480 and 733-1448 (ESTIMATE, user review).
            Assert.AreEqual((80U, 120U, 150U, 300U), ((uint)mineActions[770014].MinDamage, (uint)mineActions[770014].MaxDamage,
                (uint)mineActions[770016].MinDamage, (uint)mineActions[770016].MaxDamage), "the mines' low damage");
            Assert.AreEqual(Rasa.Data.DamageType.Physical, Rasa.Data.CreatureWeaponDamage.Of(1, 109), "the client's table: Weapon_Creature_Necromite");
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
                (770024, ShieldDrone, 3, 415, 110), (770025, ShieldDrone, 3, 490, 65), (770026, ShieldDrone, 3, 535, 80),
                (770027, Reconstructor, 3, -490, 480), (770028, Reconstructor, 3, -460, 570), (770029, Molitor, 1, -856, 371)
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
            Assert.AreEqual((531018U, (byte)3, (byte)3, 531019U, (byte)1, (byte)1, 0U, 500U, 10.0),
                (pack.Creature2Id, pack.Creature2MinCount, pack.Creature2MaxCount, pack.Creature3Id, pack.Creature3MinCount, pack.Creature3MaxCount,
                    pack.Creature4Id, (uint)pack.RespawnTime, pack.Radius), "Molitor's pack");
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
                (1205, 9476, new (uint, uint, uint, uint?)[] { (1, 9481, 9482, null), (2, 9484, 9485, null), (3, 15818, 15819, null) })
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
            // 1205: 1 1229 (Munit), 2 1219 (DeLisi), 3 1293 (Breiburg).
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
                (1205, 1, 1229, 1), (1205, 2, 1219, 1), (1205, 3, 1293, 1), (1229, 1, 1219, 1)
            }, talks);
        }

        /// <summary>
        /// The chain 1142 -> 1150 -> 1145 -> 1161 -> 1146, 1147, 1148, 1149 and 1170 -> 1229, 1150 -> 1232 and 1241, 1149 ->
        /// 1203 -> 1204, 1147 -> 1202, 1148 -> 1208, 1170 -> 1185, 1144 -> 1152 -> 1155, 1153 -> 1205, and 1153 while 1152 is in
        /// the mission log (TaRapedia); The Departed, Whiplash and Calendar Girl need none; none repeats.
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
                (1229, MissionPrerequisiteKind.MissionCompleted, 1170),
                (1232, MissionPrerequisiteKind.MissionCompleted, 1150), (1241, MissionPrerequisiteKind.MissionCompleted, 1150)
            }, rows);
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
            // 20271108000500 (to-fix rows 32, 34, 35) one of Munit's and Vargo's new places to 1145, 1146 and 1170.
            foreach (var mission in MissionIds)
                CollectionAssert.AreEqual(new[]
                    {
                        (MissionEvidenceSourceKind.Client, 1.0), (MissionEvidenceSourceKind.Documentation, 0.5),
                        (MissionEvidenceSourceKind.Reconstruction, 0.3)
                    }.Concat(mission is 1147 or 1149 or 1145 or 1146 or 1170 ? new[] { (MissionEvidenceSourceKind.Reconstruction, 0.3) } : []).ToArray(),
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
                (1205, 80000, 5850, new[] { (19157U, 1U), (33084U, 1U), (40373U, 1U), (36638U, 1U) })
            })
            {
                var reward = harness.World.Set<MissionRewardDefinitionEntry>().AsNoTracking()
                    .Single(entry => entry.MissionId == mission && entry.ContentRevision == CrucibleMissionDataV1.Revision);
                Assert.AreEqual((experience, credits, (byte)1), (reward.Experience, reward.Credits, reward.SelectionCount), $"{mission}");
                CollectionAssert.AreEqual(items, harness.World.MissionRewardItemEntries.AsNoTracking()
                    .Where(entry => entry.MissionId == mission && entry.ContentRevision == CrucibleMissionDataV1.Revision)
                    .OrderBy(entry => entry.ItemId).AsEnumerable().Select(entry => (entry.ItemTemplateId, entry.Quantity)).ToArray(), $"{mission} items");
            }
            // The tokens (classes 24749, 24750, 24751, 24752: Larai, Viddia, Top Khana, Labbna Syndicate Token) are fixed: no choice.
            var fixedItems = harness.World.MissionRewardItemEntries.AsNoTracking()
                .Where(entry => entry.ContentRevision == CrucibleMissionDataV1.Revision && entry.Kind == MissionRewardItemKind.Fixed).AsEnumerable()
                .Select(entry => (entry.MissionId, entry.ItemTemplateId)).OrderBy(entry => entry.MissionId).ToArray();
            CollectionAssert.AreEqual(new[] { (1146U, 50314U), (1147U, 50315U), (1148U, 50316U), (1149U, 50317U) }, fixedItems);
        }

        /// <summary>The indicators: all on the Crucible map (MissionMarkerProjection), keyed above UnnamedFrom (the client names none of them).</summary>
        [TestMethod]
        public void IndicatorsAreOnTheCrucibleAndAboveUnnamed()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var indicators = harness.World.MissionIndicatorEntries.AsNoTracking()
                .Where(entry => entry.ContentRevision == CrucibleMissionDataV1.Revision).ToArray();
            Assert.HasCount(Indicators, indicators);
            var catalog = new MissionContentCatalog(harness, new Dictionary<uint, Mission>(), null);
            catalog.Load();
            foreach (var row in indicators)
            {
                Assert.AreEqual(1993U, MissionMarkerProjection.ResolveMap(catalog, row.MissionId, row.ObjectiveId,
                    new MissionIndicator { IndicatorId = row.IndicatorId }), $"mission {row.MissionId} objective {row.ObjectiveId}: on the Crucible");
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
            CollectionAssert.AreEquivalent(new[] { (1143U, 22U), (1144U, 7U) }, hidden.ToArray(),
                "The Departed's item tracker and Calendar Girl's outcome are hidden");
            var objectives = world.MissionObjectiveDefinitionEntries.AsNoTracking().Where(entry => entry.ContentRevision == revision)
                .Select(entry => new { entry.MissionId, entry.ObjectiveId }).ToList();
            Assert.HasCount(9 + 1 + 6 + 3 + 1 + 3 + 1 + 8 + 1 + 1 + 1 + 2 + 3 + 1 + 1 + 3 + 8 + 2 + 1 + 3 + 3, objectives, "the objectives are read");
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
                (1146, 1, -197, 319, 8), (1146, 2, -174.3, 280.2, 8), (1146, 3, -190.5, 267.2, 8), (1146, 4, -153.7, 346.9, 5),
                (1146, 5, -214, 258, 15), (1146, 6, -190.5, 267.2, 8),                    // Bunko, Cugin, Vargo, Yeg, Roscoe, Vargo
                (1148, 1, 959, 111, 8), (1148, 2, -400, 270, 8), (1148, 3, -945.9, 246.9, 8), // Fiorella, Haggus, Melching
                (1149, 1, -452, 58, 25), (1149, 1, -518, 64, 25), (1149, 1, -380, 46, 25),                  // the Flaregasher pools
                (1147, 1, -528.6, 546.6, 20), (1147, 1, -493.6, 581.6, 20), (1147, 1, -578.6, 561.6, 20),     // the Warden Bot pools
                (1147, 2, -528.6, 546.6, 20), (1147, 2, -493.6, 581.6, 20), (1147, 2, -578.6, 561.6, 20),
                (1147, 3, -528.6, 546.6, 20), (1147, 3, -493.6, 581.6, 20), (1147, 3, -578.6, 561.6, 20),
                (1157, 1, -785, 390, 20), (1157, 1, -790, 420, 20),                                          // the Lasher pools
                (1170, 1, -218, 228, 8), (1170, 2, -218, 228, 8), (1170, 3, -300, 320, 8), (1170, 4, -300, 320, 8),
                (1170, 5, -216, 472, 8), (1170, 6, -216, 472, 8), (1170, 7, -392.77, 276.23, 8), (1170, 8, -392.77, 276.23, 8),
                (1229, 1, 952, 106, 8),                                                                      // DeLisi
                (1232, 1, 635, 557, 20), (1232, 1, 585, 577, 20), (1232, 1, 545, 587, 20),                   // the mine pools
                (1241, 1, 859, 476, 15),                                                                     // Derge on the rampart
                (1202, 1, -617, 570, 20), (1202, 1, -545, 486, 20), (1202, 1, -490, 480, 20), (1202, 1, -460, 570, 20), // the Reconstructor pools
                (1208, 1, 415, 110, 20), (1208, 1, 490, 65, 20), (1208, 1, 535, 80, 20),                    // the Shield Drone pools
                (1185, 1, -215, 326, 8), (1185, 2, -856, 371, 20), (1185, 3, -215, 326, 8),                 // Lohen, Molitor, Lohen
                (1144, 1, -230, 324, 8), (1144, 8, -230, 324, 8), (1144, 3, -230, 324, 8),                  // Deirdre
                (1144, 5, 975, 110, 8), (1144, 6, 969, 108, 8), (1144, 4, 969, 108, 8), (1144, 9, 969, 108, 8), // D'Ambrose, Sherer
                (1152, 1, -953, 255, 8), (1152, 2, 975, 110, 8), (1153, 1, -953, 255, 8),                   // Cheung, D'Ambrose, Cheung
                (1155, 1, -218, 228, 8), (1155, 2, 952, 106, 8), (1155, 3, -942.2, 246.9, 8),               // Munit, DeLisi, Breiburg
                (1205, 1, -218, 228, 8), (1205, 2, 952, 106, 8), (1205, 3, -942.2, 246.9, 8)
            };
            // Salvage Duty and More Salvage Duty: each part objective on each of the five bot pools.
            foreach (var (mission, objective) in new (uint, uint)[] { (1203, 1), (1203, 2), (1204, 3), (1204, 4), (1204, 5) })
                foreach (var (x, z) in new[] { (-528.6, 546.6), (-493.6, 581.6), (-578.6, 561.6), (-617.0, 570.0), (-545.0, 486.0) })
                    expected.Add((mission, objective, x, z, 20));
            Assert.HasCount(Indicators, expected);
            foreach (var (mission, objective, x, z, radius) in expected)
                Assert.IsTrue(rows.Any(row => row.MissionId == mission && row.ObjectiveId == objective &&
                        Vector2.Distance(new((float)row.PosX, (float)row.PosZ), new((float)x, (float)z)) < 0.1f && row.Radius == radius),
                    $"{mission} objective {objective}'s marker at ({x}, {z}) radius {radius}");
            Assert.HasCount(Indicators, rows);

            // The moved Flaregasher pools under 1149's markers; the fallen inside their marker, 10 m in from its edge.
            foreach (var (pool, _, now) in CrucibleMarkersClearOfLinksV2.Pools.Take(3))
                Assert.IsTrue(rows.Any(row => row.MissionId == 1149 && Vector2.Distance(new((float)row.PosX, (float)row.PosZ), new((float)now.X, (float)now.Z)) < 0.1f),
                    $"pool {pool} under a marker");
            foreach (var at in CrucibleDepartedV1.CorpsesAt)
                Assert.IsLessThan(CrucibleDepartedV1.FieldRadius - 10, Vector2.Distance(new(at.X, at.Z), new(802.3f, 570f)), $"a corpse at ({at.X}, {at.Z}) in the marker");
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
            Assert.AreEqual(20 + 1, objects, "The Departed's twenty fallen and the terminal");
        }

        /// <summary>
        /// The use argument the client sends for each usable class of set 1 (usabledata lookup[class][0], or 1 if it is
        /// None: Usable.__init__), written out from the client table: the human soldier corpses' entries are None.
        /// </summary>
        internal static readonly Dictionary<uint, uint> ClientUseArguments = new()
        {
            [24995] = 1, [7865] = 1, [24989] = 1, [24990] = 1,     // UsableTreasureDispHumCorpseMaleV01-V04 (1143; lookup[0] None, the client sends 1)
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
                    Assert.AreEqual(12U, actor.NameOverrideId, $"{actor.Role}: the client's name 12 'Fort Intrepid Casualty'");
                }
            Assert.AreEqual(20, used, "The Departed's twenty fallen (1150's terminal talks)");
        }

        /// <summary>
        /// The missions of set 1 (Crucible_outpost_intrepid), set 2 (Crucible_grupa_chiefs), set 3 (Crucible_wardens_and_lashers),
        /// set 4 (Crucible_minefield_and_salvage), set 5 (Crucible_staal_sigals) and set 6 (Crucible_awol_camp).
        /// </summary>
        private static readonly uint[] SetOne = MissionIds.Take(4).ToArray(), SetTwo = MissionIds.Skip(4).Take(4).ToArray(),
            SetThree = MissionIds.Skip(8).Take(4).ToArray(), SetFour = MissionIds.Skip(12).Take(4).ToArray(),
            SetFive = MissionIds.Skip(16).Take(3).ToArray(), SetSix = MissionIds.Skip(19).ToArray();

        /// <summary>
        /// The Crucible block's rows (creature, spawnpool, npc_package, creature_stat), Rigs' package, the Crucible scenes, the
        /// office teleporters and the block's creature attacks: set 1 has 3, 3, 3, 3, 1, 4, 0, 0; set 2 adds Bunko, Yeg and
        /// Roscoe (no pool, no package), the scenes of 1146 and 1148, and the two teleporters; set 3 adds Halprin, the Warden
        /// Bot and the Lasher (Halprin's pool, three Warden Bot pools, two Lasher pools; Halprin's package), the scenes of
        /// 1147, 1157, 1170 and 1229, and the Lasher's attack; set 4 adds the Smart Mine and the Reconstructor Bot (five pools), the
        /// scenes of 1203 and 1204 and three attacks; set 5 adds Nema, Pico and Trista (with their pools and packages), the Shield
        /// Drone and Molitor, six hostile pools, the scenes of 1208 and 1185 and the drone's attack; set 6 adds the five scenes of
        /// 1144, 1152, 1153, 1155 and 1205 (no new row: it moves upstream's).
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
            SetFiveRows = (16, 25, 9, 16, 1, 14, 2, 5), AllRows = (16, 25, 9, 16, 1, 19, 2, 5);

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
            Assert.IsFalse(SetTwo.Concat(SetThree).Concat(SetFour).Concat(SetFive).Concat(SetSix).Any(Has), "sets 2 to 6 are the next migrations'");
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
            Assert.IsFalse(SetTwo.Concat(SetThree).Concat(SetFour).Concat(SetFive).Concat(SetSix).Any(Has));
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
            Assert.IsFalse(SetThree.Concat(SetFour).Concat(SetFive).Concat(SetSix).Any(Has), "sets 3 to 6 are the next migrations'");
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
            Assert.IsFalse(SetThree.Concat(SetFour).Concat(SetFive).Concat(SetSix).Any(Has));
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
            Assert.IsFalse(SetFour.Concat(SetFive).Concat(SetSix).Any(Has), "sets 4 to 6 are later migrations'");
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
            Assert.IsFalse(SetFour.Concat(SetFive).Concat(SetSix).Any(Has));
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
            Assert.IsFalse(SetFive.Concat(SetSix).Any(Has), "sets 5 and 6 are later migrations'");
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

            Check(true, AllRows);
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
            Assert.IsFalse(SetFive.Concat(SetSix).Any(Has));
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
            Assert.IsFalse(SetSix.Any(Has), "set 6 is a later migration's");
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
            Assert.IsFalse(SetSix.Any(Has));
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
            Assert.IsTrue(MissionIds.All(Has));
            Assert.AreEqual(AllRows, Rows(world));
            Check(true);
        }

        internal static List<(uint MissionId, Rasa.Missions.Content.MissionSceneDefinition Scene)> Scenes(WildernessRuntimeTestHarness harness) =>
            harness.World.Set<MissionSceneBindingEntry>().AsNoTracking().Where(entry => entry.ContentRevision == CrucibleMissionDataV1.Revision)
                .ToList().Select(entry => (entry.MissionId, System.Text.Json.JsonSerializer.Deserialize<Rasa.Missions.Content.MissionSceneDefinition>(
                    entry.Bindings, Rasa.Missions.Content.MissionContentCodec.Options))).ToList();
    }
}
