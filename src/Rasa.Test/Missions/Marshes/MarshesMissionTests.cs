using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Missions.Marshes
{
    using Rasa.Managers;
    using Rasa.Services.Preloader.Missions.Marshes;
    using Rasa.Services.Preloader.Missions.Plateau;
    using Rasa.Services.Preloader.Missions.Pools;
    using Rasa.Structures;
    using Rasa.Structures.World;
    using Rasa.Test.Missions.Wilderness;

    /// <summary>
    /// The Valverde Marshes missions (MarshesMissionDataV1, revision marshes_1_16_5): set 1 at Paludos, Elder Toida's Roll
    /// out the Welcome Wagon (541) and Divided We Fall (917), Sergeant Starling's Cannister Collection (498) and Shaman
    /// Geli's What's that Gas? (539), with Ranger Alia, Shaman Siva, Warrior Veska and the mission pools east of Paludos
    /// (Marshes_paludos); set 2 at Falcon Hold, Research Chemist Horsh's Tastes Like Grubber (959) and Fixing the Food Chain
    /// (961), with Horsh and two Grubber pools west of Falcon Hold, and Retread Vincent's It Lies in Ruins (1673), which
    /// stays on the Plateau revision with Retreads to Retreads before it (PlateauMissionTests pins it there)
    /// (Marshes_falcon_hold); set 3, the Harvesters, Researcher Vetter's Reaping the Harvest (881) and the three that follow
    /// it, Retread Kline's Hassling the Harvesters (890), Lieutenant Brazos's Beware: Buried Gas Lines (913) and Ranger
    /// Taavik's A Butcher, a Baker, and a Caretaker (968), with Brazos (Marshes_harvesters); set 4, the Regulator, Retread
    /// McCormick's The Regulator: Part I (925) and Parts II-IV (1028, 1029, 1030), with Retread Wilson, Conscript Kaav and a
    /// Strider pool (Marshes_regulator); set 5, the sentry tower of Far West Watch, Sergeant McDonald's Mortar Madness (1874),
    /// Warrior Dikah's Under the Radar (1875) and Lt. Kiang's two versions of Under the Radar - Part II (1876, the control
    /// point one; 1877, Overseer Nelg's), with McDonald, Dikah, Kiang and Nelg (Marshes_sentry_tower); set 6, the sentry tower's
    /// second part, McDonald's The Core of the Matter (1878) and Data Disaster (1870), Kiang's This Calls For Destruction (1879)
    /// and Archer Thoung's Need a Little Help Here (1873), with Thoung, Brady, Zui, Dahn and Nez (Marshes_sentry_tower_two); set 7,
    /// Stalker Woods, Retread Jeska's Missing in Action (1863), Retread Dudley's Light the Fires (1864) and Now Can You Hear Me?
    /// (1869), and Retread Duvall's Stop and Smell the Maw Flowers (1871), with Jeska, Dudley, Duvall and a Maw pool
    /// (Marshes_stalker_woods); set 8, Gun Control, Retread Lou's Gun Control (1865), Part II (1866) and Part III (1867, Retread
    /// Duvall's choice) and its two ends, Part IV for Lou (1868) and for Duvall (1904), with Lou, Shaman Masai and a
    /// Lightbender pool (Marshes_gun_control); set 9, the escorts, Shaman Masai's Forean Flu (497), Retread Duvall's Tread
    /// Lightly (888), MP Price's AWOL Andy (889) and Sergeant Elway's Rest and Recreation (1023), with the escorted, Lieutenants
    /// Belfast, Delaio and Urqhart, and Price's package 386 moved to Lieutenant Bruncke (Marshes_escorts); set 10, Retread City,
    /// Retread Vincent's Highway Robbery (914, steal or tell Commander Figgins), its two Production Destructions (916 for
    /// Vincent, 1033 for Lieutenant Holloway with a Mech squadmate) and Trapped! (1052, Retread Samuel's crew defended), with
    /// Figgins, a Marshes Holloway, the mech and Samuel's stand-in and crew (Marshes_retread_city); set 11, the Retread Arena,
    /// Retread Karl's Beat Street (1085), Smackdown (1087) and Legend or Fall (1088), and Retread Vincent's Calling all Parts
    /// (1084), with Karl, Retread Foster, the five fighters and a Forean Machina pool (Marshes_retread_arena).
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class MarshesMissionTests
    {
        /// <summary>The Marshes missions the World migrations enable on the Marshes revision, for the tests that pin the enabled set.</summary>
        internal static readonly uint[] MissionIds =
        {
            MarshesWelcomeWagonV1.MissionId, MarshesCannisterCollectionV1.MissionId, MarshesWhatsThatGasV1.MissionId,
            MarshesDividedWeFallV1.MissionId, MarshesTastesLikeGrubberV1.MissionId, MarshesFixingTheFoodChainV1.MissionId,
            MarshesReapingTheHarvestV1.MissionId, MarshesHasslingTheHarvestersV1.MissionId, MarshesBuriedGasLinesV1.MissionId,
            MarshesButcherBakerCaretakerV1.MissionId, MarshesRegulatorPartOneV1.MissionId, MarshesRegulatorPartTwoV1.MissionId,
            MarshesRegulatorPartThreeV1.MissionId, MarshesRegulatorPartFourV1.MissionId, MarshesMortarMadnessV1.MissionId,
            MarshesUnderTheRadarV1.MissionId, MarshesUnderTheRadarPartTwoV1.MissionId, MarshesUnderTheRadarNelgV1.MissionId,
            MarshesCoreOfTheMatterV1.MissionId, MarshesThisCallsForDestructionV1.MissionId, MarshesDataDisasterV1.MissionId,
            MarshesNeedALittleHelpHereV1.MissionId, MarshesMissingInActionV1.MissionId, MarshesLightTheFiresV1.MissionId,
            MarshesNowCanYouHearMeV1.MissionId, MarshesMawFlowersV1.MissionId, MarshesGunControlV1.MissionId,
            MarshesGunControlPartTwoV1.MissionId, MarshesGunControlPartThreeV1.MissionId, MarshesGunControlPartFourV1.LouMissionId,
            MarshesGunControlPartFourV1.DuvallMissionId, MarshesForeanFluV1.MissionId, MarshesTreadLightlyV1.MissionId,
            MarshesAwolAndyV1.MissionId, MarshesRestAndRecreationV1.MissionId, MarshesHighwayRobberyV1.MissionId,
            MarshesProductionDestructionV1.VincentMissionId, MarshesProductionDestructionV1.HollowayMissionId, MarshesTrappedV1.MissionId,
            MarshesRetreadArenaV1.BeatStreet, MarshesRetreadArenaV1.Smackdown, MarshesRetreadArenaV1.LegendOrFall,
            MarshesCallingAllPartsV1.MissionId
        };

        /// <summary>It Lies in Ruins: a Marshes mission on the Plateau revision (in PlateauMissionTests.MissionIds).</summary>
        private const uint ItLiesInRuins = MarshesItLiesInRuinsV1.MissionId;

        /// <summary>
        /// Every visible objective has a marker (EveryVisibleObjectiveHasAMarker): Roll out the Welcome Wagon's one, on
        /// Cast'ka; Cannister Collection's three, on the western Thrax pools; What's that Gas?'s seven (the "Calla Ferns" one
        /// round the Grubber pools, and one on each of the six mission pools for 2-4); Divided We Fall's four (Alia's beat,
        /// Siva, Veska, Helka). Tastes Like Grubber's two, on its pools; Fixing the Food Chain's twelve, one on each feeder.
        /// It Lies in Ruins' one, at Morrison, is counted with the Plateau revision's (PlateauMissionTests.Indicators).
        /// Reaping the Harvest's one and Hassling the Harvesters' one, the client's "Bane Harvester" round the harvester field;
        /// Beware: Buried Gas Lines' four, the client's "Pipeline Supply Point" on each joint; A Butcher, a Baker, and a
        /// Caretaker's three, on the bosses' pools. The Regulator's four: Part I's at Wilson, Part II's at Kaav, Part III's on
        /// the Strider pool, Part IV's the client's "Paludos Ruins" at the scrapper. Mortar Madness' eight, one on each mortar
        /// of the eastern wall line; Under the Radar's the client's "Downed Dropship" at the black box; Part II's the client's
        /// "Bane Terminal" at the computer (1876) and one at Nelg's post (1877). The Core of the Matter's one round the crates;
        /// This Calls For Destruction's two, round the consoles and at the generator; Data Disaster's one at the terminal; Need a
        /// Little Help Here's five, round the crates and at each of the four soldiers. Missing in Action's one at Koffman's body;
        /// Light the Fires' five, one at each fire; Now Can You Hear Me?'s three, one at each spike spot; Stop and Smell the Maw
        /// Flowers' one round the flowers. Gun Control's four, on the three technician pools nearest the camp and the
        /// Lightbenders; Part II's two, on the Mox pools; Part III's three, at Masai (1 and 3) and Duvall; each Part IV's one at
        /// its receiver. Forean Flu's one at DuBois, Tread Lightly's at Vincent; AWOL Andy's nine: the client's "Possible location
        /// of Private Bruncke" on each search place (4-7), one at each place for 1 and one at Price for 2; Rest and Recreation's
        /// seven, at each leg's officer (Elway for 4) and at the officer each report goes to. Highway Robbery's four, at each
        /// supply crate (1), Figgins (2) and Vincent (4); each Production Destruction's three, round the kiosks and at Vesh and
        /// Koorva; Trapped!'s two, at Samuel (1) and round him (2). Each arena round's at Foster (2) and round the arena (1; 1088
        /// also 3); Calling all Parts' five, on the Stalker pool, the three Caretaker pools nearest Retread City and the Machina
        /// pool.
        /// </summary>
        internal const int Indicators = 125;

        private const string Paludos = "20271104000000_Marshes_paludos";
        private const string FalconHold = "20271104000100_Marshes_falcon_hold";
        private const string Harvesters = "20271104000200_Marshes_harvesters";
        private const string Regulator = "20271104000300_Marshes_regulator";
        private const string SentryTower = "20271104000400_Marshes_sentry_tower";
        private const string SentryTowerTwo = "20271104000500_Marshes_sentry_tower_two";
        private const string StalkerWoods = "20271104000600_Marshes_stalker_woods";
        private const string GunControl = "20271104000700_Marshes_gun_control";
        private const string Escorts = "20271104000800_Marshes_escorts";
        private const string RetreadCity = "20271104000900_Marshes_retread_city";
        private const string RetreadArena = "20271104001000_Marshes_retread_arena";

        /// <summary>
        /// The migration directly before <paramref name="migration"/> (IMigrationsAssembly, ordinal order), the target that
        /// rolls <paramref name="migration"/> back: not a fixed id, so another migration slotted in between cannot be rolled
        /// back with it unseen.
        /// </summary>
        private static string Previous(Microsoft.EntityFrameworkCore.DbContext world, string migration)
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
                (541, 510160, 510160, 36), (498, 510171, 510171, 35), (539, 510172, 510172, 34), (917, 510160, 510160, 35),
                (959, 760309, 760309, 33), (961, 760309, 760309, 37),
                (881, 510165, 510165, 33), (890, 510166, 510166, 37), (913, 760312, 760312, 37), (968, 510164, 510164, 36),
                (925, 510201, 760313, 35), (1028, 760313, 760314, 37), (1029, 760314, 760313, 37), (1030, 760313, 510201, 35),
                (1874, 760316, 760316, 37), (1875, 760317, 760318, 37), (1876, 760318, 760318, 37), (1877, 760318, 760318, 37),
                (1878, 760316, 760316, 37), (1879, 760318, 760318, 37), (1870, 760316, 760316, 37), (1873, 760320, 760320, 37),
                (1863, 760325, 760325, 36), (1864, 760326, 760326, 36), (1869, 760326, 760326, 36), (1871, 760327, 760327, 36),
                (1865, 760329, 760329, 36), (1866, 760329, 760329, 36), (1867, 760329, 760330, 36), (1868, 760330, 760329, 36),
                (1904, null, 760327, 36),
                (497, 760330, 510161, 33), (888, 760327, 510168, 35), (889, 510163, 510163, 37), (1023, 510169, 510169, 33),
                (914, 510168, 510168, 36), (916, 510168, 510168, 35), (1033, 760340, 760340, 35), (1052, 510168, 510168, 38),
                (1085, 760344, 760344, 35), (1087, 760344, 760344, 35), (1088, 760344, 760344, 35), (1084, 510168, 510168, 33)
            };
            using var harness = WildernessRuntimeTestHarness.Create();
            var enabled = harness.World.MissionContentDefinitionEntries.AsNoTracking()
                .Where(entry => entry.Enabled && entry.ContentRevision == MarshesMissionDataV1.Revision).ToArray();
            CollectionAssert.AreEquivalent(MissionIds, enabled.Select(entry => entry.MissionId).ToArray());
            foreach (var (mission, giver, receiver, level) in expected)
            {
                var entry = enabled.Single(row => row.MissionId == mission);
                Assert.AreEqual<uint?>(giver, entry.GiverId, $"{mission} giver");
                Assert.AreEqual<uint?>(receiver, entry.ReceiverId, $"{mission} receiver");
                Assert.AreEqual<uint?>(level, entry.Level, $"{mission} level");
                Assert.AreEqual<uint?>(10000051U, entry.CategoryId, "Battlefield (Marshes)");
            }

            // It Lies in Ruins: Vincent to Morrison, level 35, on the Plateau revision but in the Marshes' category.
            var ruins = harness.World.MissionContentDefinitionEntries.AsNoTracking()
                .Single(entry => entry.MissionId == ItLiesInRuins && entry.Enabled);
            Assert.AreEqual(PlateauMissionDataV1.Revision, ruins.ContentRevision);
            Assert.AreEqual(((uint?)510168, (uint?)510162, (uint?)35, (uint?)10000051), (ruins.GiverId, ruins.ReceiverId, ruins.Level, ruins.CategoryId));

            // Alia, Siva and Veska: friendly Foreans of level 35 with the client's names and packages, on Valverde Marshes;
            // Siva and Veska in pools, Alia a squad of one (no pool).
            foreach (var (npc, name, package, cls) in new (uint, uint, uint, uint)[]
            {
                (760300, 8601, 985, 7036), (760301, 8602, 986, 7035), (760302, 8600, 984, 7034)
            })
            {
                var creature = harness.World.CreatureEntries.AsNoTracking().Single(row => row.Id == npc);
                Assert.AreEqual((name, 35U, 1U, cls), (creature.NameId, creature.Level, creature.Faction, creature.ClassId), $"{npc}");
                Assert.AreEqual(package, harness.World.NpcPackageEntries.AsNoTracking().Single(row => row.Id == npc).PackageId, $"{npc} package");
            }
            foreach (var npc in new uint[] { 760301, 760302, 760309 })
                Assert.AreEqual(1454U, harness.World.SpawnPoolEntries.AsNoTracking().Single(row => row.Id == npc).MapContextId, $"{npc} map");
            Assert.IsFalse(harness.World.SpawnPoolEntries.AsNoTracking().Any(row => row.Id == 760300), "Alia: a squad, no pool");
            var type = harness.World.NpcSquadTypeEntries.AsNoTracking().Single(row => row.Id == 760300);
            Assert.AreEqual((byte)1, type.LeaderBystander, "Alia takes up no fight");
            CollectionAssert.AreEqual(new[] { (760300U, (byte)1, (byte)1) },
                harness.World.NpcSquadMemberEntries.AsNoTracking().Where(row => row.TypeId == 760300).AsEnumerable()
                    .Select(row => (row.CreatureId, row.MinCount, row.MaxCount)).ToArray(), "Alia alone");
            var path = harness.World.NpcSquadPathEntries.AsNoTracking().Single(row => row.Id == 760300);
            Assert.AreEqual((1454U, (byte)1), (path.MapContextId, path.Mode), "on the Marshes, there and back");
            Assert.AreEqual(4, harness.World.NpcSquadPathPointEntries.AsNoTracking().Count(row => row.PathId == 760300), "four corners");

            // Horsh: the client's name 3142, a server-only package (his id), Vetter's class, level and look (510165).
            var horsh = harness.World.CreatureEntries.AsNoTracking().Single(row => row.Id == 760309);
            Assert.AreEqual((3142U, 3846U, 10U, 1U), (horsh.NameId, horsh.ClassId, horsh.Level, horsh.Faction), "Horsh");
            Assert.AreEqual(760309U, harness.World.NpcPackageEntries.AsNoTracking().Single(row => row.Id == 760309).PackageId, "Horsh's package");
            int Slots(uint id) => harness.World.Database.SqlQueryRaw<int>(
                $"SELECT COUNT(*) AS Value FROM creature_appearance WHERE id = {id}").AsEnumerable().Single();
            Assert.AreEqual(6, Slots(760309), "Horsh wears Vetter's six slots");
            Assert.AreEqual(Slots(510165), Slots(760309));

            // Brazos: the client's name 8595, a server-only package (his id), Morrison's class, level and look (510162), in a
            // pool of his own on the Marshes.
            var brazos = harness.World.CreatureEntries.AsNoTracking().Single(row => row.Id == 760312);
            Assert.AreEqual((8595U, 3846U, 10U, 1U), (brazos.NameId, brazos.ClassId, brazos.Level, brazos.Faction), "Brazos");
            Assert.AreEqual(760312U, harness.World.NpcPackageEntries.AsNoTracking().Single(row => row.Id == 760312).PackageId, "Brazos's package");
            Assert.AreEqual(6, Slots(760312), "Brazos wears Morrison's six slots");
            Assert.AreEqual(Slots(510162), Slots(760312));
            var brazosPool = harness.World.SpawnPoolEntries.AsNoTracking().Single(row => row.Id == 760312);
            Assert.AreEqual((1454U, 760312U), (brazosPool.MapContextId, brazosPool.Creature1Id), "Brazos's pool");

            // Wilson: the client's name 8579 and package 969, McCormick's class, level and look (510201), in a pool of his own.
            var wilson = harness.World.CreatureEntries.AsNoTracking().Single(row => row.Id == 760313);
            Assert.AreEqual((8579U, 3846U, 10U, 1U), (wilson.NameId, wilson.ClassId, wilson.Level, wilson.Faction), "Wilson");
            Assert.AreEqual(969U, harness.World.NpcPackageEntries.AsNoTracking().Single(row => row.Id == 760313).PackageId, "Wilson's package");
            Assert.AreEqual(6, Slots(760313), "Wilson wears McCormick's six slots");
            Assert.AreEqual(Slots(510201), Slots(760313));
            // Kaav: the client's name 8580 and package 970, a neutral Thrax NPC of level 37 (Nakrinx's class 11320).
            var kaav = harness.World.CreatureEntries.AsNoTracking().Single(row => row.Id == 760314);
            Assert.AreEqual((8580U, 11320U, 37U, 3U, 8024U), (kaav.NameId, kaav.ClassId, kaav.Level, kaav.Faction, kaav.MaxHitPoints), "Kaav");
            Assert.AreEqual(970U, harness.World.NpcPackageEntries.AsNoTracking().Single(row => row.Id == 760314).PackageId, "Kaav's package");
            // The Strider: the Pools' Strider class 9804 and name 9099 at level 36 with 10406 hit points, a pool of one.
            var strider = harness.World.CreatureEntries.AsNoTracking().Single(row => row.Id == 760315);
            Assert.AreEqual((9099U, 9804U, 36U, 0U, 10406U), (strider.NameId, strider.ClassId, strider.Level, strider.Faction, strider.MaxHitPoints), "Strider");
            foreach (var (pool, creature) in new (uint, uint)[] { (760313, 760313), (760314, 760314), (760315, 760315) })
            {
                var row = harness.World.SpawnPoolEntries.AsNoTracking().Single(entry => entry.Id == pool);
                Assert.AreEqual((1454U, creature, (byte)1, (byte)1), (row.MapContextId, row.Creature1Id, row.Creature1MinCount,
                    row.Creature1MaxCount), $"pool {pool}");
            }

            // McDonald and Kiang: the client's names 10327 and 10326, server-only packages (their ids), the class, level and
            // look of Elway (510169) and Morrison (510162). Dikah: the client's name 10325, a friendly Forean Spearman of level 37
            // with the spear. Each in a pool of one on the Marshes. Nelg: the client's name 10339, Nivvik's overseer class 10504,
            // level 38, 12000 hit points, hostile, no pool.
            foreach (var (npc, name, source) in new (uint, uint, uint)[] { (760316, 10327, 510169), (760318, 10326, 510162) })
            {
                var creature = harness.World.CreatureEntries.AsNoTracking().Single(row => row.Id == npc);
                Assert.AreEqual((name, 3846U, 10U, 1U), (creature.NameId, creature.ClassId, creature.Level, creature.Faction), $"{npc}");
                Assert.AreEqual(npc, harness.World.NpcPackageEntries.AsNoTracking().Single(row => row.Id == npc).PackageId, $"{npc} package");
                Assert.AreEqual(6, Slots(npc), $"{npc} wears six slots");
                Assert.AreEqual(Slots(source), Slots(npc));
            }
            var dikah = harness.World.CreatureEntries.AsNoTracking().Single(row => row.Id == 760317);
            Assert.AreEqual((10325U, 7034U, 37U, 1U), (dikah.NameId, dikah.ClassId, dikah.Level, dikah.Faction), "Dikah");
            Assert.AreEqual(760317U, harness.World.NpcPackageEntries.AsNoTracking().Single(row => row.Id == 760317).PackageId, "Dikah's package");
            Assert.AreEqual(1, Slots(760317), "Dikah's spear");
            foreach (var npc in new uint[] { 760316, 760317, 760318 })
            {
                var row = harness.World.SpawnPoolEntries.AsNoTracking().Single(entry => entry.Id == npc);
                Assert.AreEqual((1454U, npc, (byte)1, (byte)1), (row.MapContextId, row.Creature1Id, row.Creature1MinCount,
                    row.Creature1MaxCount), $"pool {npc}");
            }
            var nelg = harness.World.CreatureEntries.AsNoTracking().Single(row => row.Id == 760319);
            Assert.AreEqual((10339U, 10504U, 38U, 0U, 12000U), (nelg.NameId, nelg.ClassId, nelg.Level, nelg.Faction, nelg.MaxHitPoints), "Nelg");
            Assert.IsFalse(harness.World.SpawnPoolEntries.AsNoTracking().Any(row => row.Id == 760319 || row.Creature1Id == 760319), "Nelg: no pool");

            // 539's six pools of three: two each of Swamp Grubber, Maw and Mox; 959's two Swamp Grubber pools; all on the Marshes.
            foreach (var (id, creature) in new (uint, uint)[]
            {
                (760303, 540014), (760304, 540014), (760305, 531052), (760306, 531052), (760307, 540013), (760308, 540013),
                (760310, 540014), (760311, 540014)
            })
            {
                var row = harness.World.SpawnPoolEntries.AsNoTracking().Single(entry => entry.Id == id);
                Assert.AreEqual((1454U, creature, (byte)3, (byte)3), (row.MapContextId, row.Creature1Id, row.Creature1MinCount,
                    row.Creature1MaxCount), $"pool {id}");
            }

            Assert.IsFalse(harness.Manager.LoadMissions().BlocksReadiness);
            foreach (var id in MissionIds.Append(ItLiesInRuins))
                Assert.IsTrue(harness.Manager.LoadedMissions[id].IsOperational,
                    $"{id}: {harness.Manager.LoadedMissions[id].OperationalDiagnostic}");
        }

        /// <summary>
        /// The client's own texts: the ids are written out here from the client tables (missionconversation,
        /// missionobjective), not taken from the data classes, so a wrong id in those fails.
        /// </summary>
        [TestMethod]
        public void TitlesAndObjectivesUseTheClientsTexts()
        {
            var marshes = MarshesMissionDataV1.Revision;
            var expected = new (uint Mission, string Revision, uint Title, (uint Objective, uint Name, uint Body, uint? Counter)[] Objectives)[]
            {
                (541, marshes, 1454, new (uint, uint, uint, uint?)[] { (1, 1496, 7423, 1497) }),
                (498, marshes, 1238, new (uint, uint, uint, uint?)[] { (1, 1433, 8304, null) }),
                (539, marshes, 1446, new (uint, uint, uint, uint?)[] { (1, 1542, 9361, null), (2, 1543, 9362, null), (3, 1544, 9363, null),
                    (4, 1545, 9364, null) }),
                (917, marshes, 4899, new (uint, uint, uint, uint?)[] { (1, 4958, 7105, null), (2, 4960, 7106, null), (3, 4962, 7107, null),
                    (4, 4964, 7108, null) }),
                (959, marshes, 5211, new (uint, uint, uint, uint?)[] { (1, 5215, 7002, null) }),
                (961, marshes, 5221, new (uint, uint, uint, uint?)[] { (1, 5225, 7004, 5268) }),
                (1673, PlateauMissionDataV1.Revision, 16101, new (uint, uint, uint, uint?)[] { (1, 16106, 16107, null) }),
                // 881's objective 2 is the server's hidden tally of the five samples, under the client's objective 1 texts.
                (881, marshes, 4615, new (uint, uint, uint, uint?)[] { (1, 4619, 7228, null), (2, 4619, 7228, null) }),
                (890, marshes, 4672, new (uint, uint, uint, uint?)[] { (1, 4684, 9806, 4685) }),
                (913, marshes, 4883, new (uint, uint, uint, uint?)[] { (1, 4956, 9334, 4957), (2, 9377, 9378, 9379), (3, 9380, 9381, 9382),
                    (4, 9383, 9384, 9385) }),
                (968, marshes, 5285, new (uint, uint, uint, uint?)[] { (1, 5289, 9374, 5290), (2, 5291, 9375, 5292), (3, 5293, 9376, 5294) }),
                (925, marshes, 4943, new (uint, uint, uint, uint?)[] { (1, 4947, 7331, null) }),
                (1028, marshes, 5707, new (uint, uint, uint, uint?)[] { (1, 5711, 9821, null) }),
                (1029, marshes, 5713, new (uint, uint, uint, uint?)[] { (1, 5717, 7834, null) }),
                (1030, marshes, 5718, new (uint, uint, uint, uint?)[] { (1, 5722, 8070, null) }),
                (1874, marshes, 19154, new (uint, uint, uint, uint?)[] { (1, 19553, 19554, 19555) }),
                (1875, marshes, 19159, new (uint, uint, uint, uint?)[] { (1, 19563, 19564, null) }),
                (1876, marshes, 19164, new (uint, uint, uint, uint?)[] { (1, 19687, 19688, null) }),
                (1877, marshes, 19169, new (uint, uint, uint, uint?)[] { (1, 19685, 19686, null) }),
                // 1878's objective 2 and 1873's 6 are the server's hidden tallies, under the client's objective 1 texts.
                (1878, marshes, 19174, new (uint, uint, uint, uint?)[] { (1, 19556, 19557, null), (2, 19556, 19557, null) }),
                (1879, marshes, 19179, new (uint, uint, uint, uint?)[] { (1, 19558, 19559, 19560), (2, 19561, 19562, null) }),
                (1870, marshes, 19134, new (uint, uint, uint, uint?)[] { (1, 19550, 19551, null) }),
                (1873, marshes, 19149, new (uint, uint, uint, uint?)[] { (1, 19565, 19566, null), (2, 19567, 19568, null),
                    (3, 19570, 19571, null), (4, 19573, 19574, null), (5, 19576, 19577, null), (6, 19565, 19566, null) }),
                (1863, marshes, 19099, new (uint, uint, uint, uint?)[] { (1, 19365, 19366, null) }),
                (1864, marshes, 19104, new (uint, uint, uint, uint?)[] { (1, 19368, 19369, 19370), (2, 19371, 19372, 19373),
                    (3, 19374, 19375, 19376), (4, 19377, 19378, 19379), (5, 19380, 19381, 19382) }),
                (1869, marshes, 19129, new (uint, uint, uint, uint?)[] { (1, 19383, 19384, 19385), (2, 19386, 19387, 19388),
                    (3, 19389, 19390, 19391) }),
                // 1871's objective 2 is the server's hidden tally, under the client's objective 1 texts.
                (1871, marshes, 19139, new (uint, uint, uint, uint?)[] { (1, 19446, 19447, null), (2, 19446, 19447, null) }),
                (1865, marshes, 19109, new (uint, uint, uint, uint?)[] { (1, 19392, 19393, null), (2, 19394, 19395, null) }),
                (1866, marshes, 19114, new (uint, uint, uint, uint?)[] { (1, 19438, 19439, 19440) }),
                // 1867's objective 2 counter 19545 is empty in the client: not bound.
                (1867, marshes, 19119, new (uint, uint, uint, uint?)[] { (1, 19441, 19442, null), (2, 19444, 19445, null),
                    (3, 19483, 19484, null), (4, 19546, 19547, null) }),
                (1868, marshes, 19124, new (uint, uint, uint, uint?)[] { (1, 19486, 19487, null) }),
                (1904, marshes, 19693, new (uint, uint, uint, uint?)[] { (1, 19698, 19699, null) }),
                (497, marshes, 1233, new (uint, uint, uint, uint?)[] { (1, 4942, 7166, null) }),
                // 888's objective 2 (4655 "no desc") is not bound.
                (888, marshes, 4646, new (uint, uint, uint, uint?)[] { (1, 4654, 7006, null) }),
                // 889's objective 3 (4660, the older "back to Paludos") is not bound.
                (889, marshes, 4650, new (uint, uint, uint, uint?)[] { (1, 4658, 7085, null), (2, 4659, 7086, null),
                    (4, 4661, 7088, null), (5, 4662, 7089, null), (6, 4663, 7090, null), (7, 4664, 7091, null) }),
                (1023, marshes, 5635, new (uint, uint, uint, uint?)[] { (1, 5639, 7213, null), (2, 5640, 7215, null),
                    (3, 5641, 7217, null), (4, 5642, 7219, null), (5, 5723, 7214, null), (6, 5725, 7216, null), (7, 5726, 7218, null) }),
                (914, marshes, 4887, new (uint, uint, uint, uint?)[] { (1, 5733, 6407, null), (2, 5734, 6408, null),
                    (3, 5744, 6409, null), (4, 5745, 6410, null) }),
                (916, marshes, 4895, new (uint, uint, uint, uint?)[] { (1, 5756, 6979, 5757), (2, 5758, 6980, 5759), (3, 5760, 6981, 5761) }),
                (1033, marshes, 5762, new (uint, uint, uint, uint?)[] { (1, 5766, 6416, 5767), (2, 5768, 6417, 5769), (3, 5770, 6418, 5771) }),
                (1052, marshes, 5912, new (uint, uint, uint, uint?)[] { (1, 6082, 7014, null), (2, 6083, 7015, null) }),
                // The arena's 1: counter 0 and 1 the client's two fighters (1085, 1087); 1088's 6201 ("Baker") is not bound.
                (1085, marshes, 6152, new (uint, uint, uint, uint?)[] { (1, 6156, 7196, 6157), (2, 6163, 7195, null) }),
                (1087, marshes, 6170, new (uint, uint, uint, uint?)[] { (1, 6176, 7273, 6179), (2, 6174, 7272, null) }),
                (1088, marshes, 6188, new (uint, uint, uint, uint?)[] { (1, 6198, 7175, 6199), (2, 6192, 7174, null), (3, 7274, 7275, null) }),
                (1084, marshes, 6145, new (uint, uint, uint, uint?)[] { (1, 6149, 8455, null), (2, 6150, 8456, null), (3, 6151, 8457, null) })
            };
            using var harness = WildernessRuntimeTestHarness.Create();
            foreach (var (mission, revision, title, objectives) in expected)
            {
                Assert.AreEqual(title, harness.World.MissionContentDefinitionEntries.AsNoTracking()
                    .Single(entry => entry.MissionId == mission && entry.ContentRevision == revision)
                    .ClientNameTextId, $"{mission} title");
                var actual = harness.World.MissionObjectiveDefinitionEntries.AsNoTracking()
                    .Where(entry => entry.MissionId == mission && entry.ContentRevision == revision).ToArray();
                CollectionAssert.AreEquivalent(objectives.Select(row => row.Objective).ToArray(),
                    actual.Select(row => row.ObjectiveId).ToArray(), $"{mission} objectives");
                foreach (var (objective, name, body, counter) in objectives)
                {
                    var row = actual.Single(entry => entry.ObjectiveId == objective);
                    Assert.AreEqual(name, row.ClientNameTextId, $"{mission}.{objective} name");
                    Assert.AreEqual(body, row.ClientBodyTextId, $"{mission}.{objective} body");
                    Assert.AreEqual(counter, row.ClientCounter0TextId, $"{mission}.{objective} counter0");
                    Assert.AreEqual((mission, objective) switch { (1085, 1) => 6158U, (1087, 1) => 6178U, _ => (uint?)null },
                        row.ClientCounter1TextId, $"{mission}.{objective} counter1");
                    Assert.IsNull(row.ClientCounter2TextId, $"{mission}.{objective} counter2");
                }
            }

            (uint, uint?)[] Talks(uint mission, string revision) => harness.World.Set<MissionTriggerEntry>().AsNoTracking()
                .Where(entry => entry.MissionId == mission && entry.ContentRevision == revision &&
                    entry.Kind == MissionTriggerKind.Conversation).AsEnumerable()
                .Select(entry => (entry.ObjectiveId, entry.NpcPackageId)).OrderBy(entry => entry.ObjectiveId).ToArray();
            // 917's three talks at the client's packages (objectiveconversation 917,1 985; 917,2 986; 917,3 984).
            CollectionAssert.AreEqual(new (uint, uint?)[] { (1, 985), (2, 986), (3, 984) }, Talks(917, marshes));
            // 1673's at Morrison's (objectiveconversation 1673,1 package 299); 959 and 961 have none.
            CollectionAssert.AreEqual(new (uint, uint?)[] { (1, 299) }, Talks(1673, PlateauMissionDataV1.Revision));
            Assert.IsEmpty(Talks(959, marshes));
            Assert.IsEmpty(Talks(961, marshes));
            // Set 3 has none (no objectiveconversation for 881, 890, 913, 968).
            foreach (var mission in new uint[] { 881, 890, 913, 968 })
                Assert.IsEmpty(Talks(mission, marshes), $"{mission}");
            // Set 4: 925's at Wilson's (objectiveconversation 925,1 package 969), 1028's at the Bane Traitor's (1028,1 package
            // 970); 1029 and 1030 have none.
            CollectionAssert.AreEqual(new (uint, uint?)[] { (1, 969) }, Talks(925, marshes));
            CollectionAssert.AreEqual(new (uint, uint?)[] { (1, 970) }, Talks(1028, marshes));
            Assert.IsEmpty(Talks(1029, marshes));
            Assert.IsEmpty(Talks(1030, marshes));
            // Set 5 has none (no objectiveconversation for 1874-1877).
            foreach (var mission in new uint[] { 1874, 1875, 1876, 1877 })
                Assert.IsEmpty(Talks(mission, marshes), $"{mission}");
            // Set 6: 1870's at the CommLink terminal's (objectiveconversation 1870,1 package 2374), 1873's at the four soldiers'
            // (1873,2-5 packages 2375-2378); 1878 and 1879 have none.
            CollectionAssert.AreEqual(new (uint, uint?)[] { (1, 2374) }, Talks(1870, marshes));
            CollectionAssert.AreEqual(new (uint, uint?)[] { (2, 2375), (3, 2376), (4, 2377), (5, 2378) }, Talks(1873, marshes));
            Assert.IsEmpty(Talks(1878, marshes));
            Assert.IsEmpty(Talks(1879, marshes));
            // Set 7: 1863's at Koffman's body (objectiveconversation 1863,1 package 2367); 1864, 1869 and 1871 have none.
            CollectionAssert.AreEqual(new (uint, uint?)[] { (1, 2367) }, Talks(1863, marshes));
            foreach (var mission in new uint[] { 1864, 1869, 1871 })
                Assert.IsEmpty(Talks(mission, marshes), $"{mission}");
            // Set 8: 1867's at Masai's (objectiveconversation 1867,1 and 1867,3 package 991) and Duvall's (1867,2 package 950,
            // the choice's two ways), 1868's at Lou's (1868,1 package 2356), 1904's at Duvall's (1904,1 package 950); 1865 and
            // 1866 have none.
            CollectionAssert.AreEqual(new (uint, uint?)[] { (1, 991), (2, 950), (2, 950), (3, 991) }, Talks(1867, marshes));
            CollectionAssert.AreEqual(new (uint, uint?)[] { (1, 2356) }, Talks(1868, marshes));
            CollectionAssert.AreEqual(new (uint, uint?)[] { (1, 950) }, Talks(1904, marshes));
            Assert.IsEmpty(Talks(1865, marshes));
            Assert.IsEmpty(Talks(1866, marshes));
            // Set 9: 889's at Bruncke's (objectiveconversation 889,1 package 386), 1023's at Belfast's, Delaio's and Urqhart's
            // (1023,5 package 1101; 1023,6 1102; 1023,7 1100); 497 and 888 have none.
            CollectionAssert.AreEqual(new (uint, uint?)[] { (1, 386) }, Talks(889, marshes));
            CollectionAssert.AreEqual(new (uint, uint?)[] { (5, 1101), (6, 1102), (7, 1100) }, Talks(1023, marshes));
            Assert.IsEmpty(Talks(497, marshes));
            Assert.IsEmpty(Talks(888, marshes));
            // Set 10: 914's at Figgins' (objectiveconversation 914,2 package 1011) and Vincent's (914,4 package 951, the two
            // variants 131 and 130: one a way each), 1052's at Samuel's (1052,1 package 1120); 916 and 1033 have none.
            CollectionAssert.AreEqual(new (uint, uint?)[] { (2, 1011), (4, 951), (4, 951) }, Talks(914, marshes));
            CollectionAssert.AreEqual(new (uint, uint?)[] { (1, 1120) }, Talks(1052, marshes));
            Assert.IsEmpty(Talks(916, marshes));
            Assert.IsEmpty(Talks(1033, marshes));
            // Set 11: each round's at Foster's (objectiveconversation 1085,2, 1087,2, 1088,2 package 1161; 1088's choice two
            // ways); 1084 has none.
            CollectionAssert.AreEqual(new (uint, uint?)[] { (2, 1161) }, Talks(1085, marshes));
            CollectionAssert.AreEqual(new (uint, uint?)[] { (2, 1161) }, Talks(1087, marshes));
            CollectionAssert.AreEqual(new (uint, uint?)[] { (2, 1161), (2, 1161) }, Talks(1088, marshes));
            Assert.IsEmpty(Talks(1084, marshes));
        }

        /// <summary>
        /// Set 1 has no requirement (TaRapedia, TR MMO DB). Fixing the Food Chain follows Tastes Like Grubber, and It Lies in
        /// Ruins Retreads to Retreads (TaRapedia), on one revision each. Hassling the Harvesters, Beware: Buried Gas Lines and A
        /// Butcher, a Baker, and a Caretaker follow Reaping the Harvest, which needs none (TaRapedia). The Regulator's parts
        /// follow each other, Part I needs none (TaRapedia). Both versions of Under the Radar - Part II follow Under the Radar;
        /// Mortar Madness and Under the Radar need none (TaRapedia). The Core of the Matter follows Under the Radar (TaRapedia);
        /// This Calls For Destruction and Data Disaster follow either version of Part II, by its mission flag; Need a Little
        /// Help Here needs none. Set 7 needs none. Gun Control's parts follow each other; the two Part IVs also need Part III's
        /// ending, by its mission flag (1867001: 1 the gun given, 1868; 2 sold, 1904). Set 9 needs none. Both Production
        /// Destructions follow Highway Robbery, each on its ending (914001: 1 stolen, 916; 2 told, 1033); Trapped! needs none.
        /// The arena rounds follow each other, Beat Street needs none; Calling all Parts follows Legend or Fall. None repeats.
        /// </summary>
        [TestMethod]
        public void OnlyTheFollowUpsRequireAMission()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            CollectionAssert.AreEquivalent(new[]
                {
                    (961U, (uint?)959), (890U, (uint?)881), (913U, (uint?)881), (968U, (uint?)881),
                    (1028U, (uint?)925), (1029U, (uint?)1028), (1030U, (uint?)1029),
                    (1876U, (uint?)1875), (1877U, (uint?)1875), (1878U, (uint?)1875),
                    (1866U, (uint?)1865), (1867U, (uint?)1866), (1868U, (uint?)1867), (1904U, (uint?)1867),
                    (916U, (uint?)914), (1033U, (uint?)914),
                    (1087U, (uint?)1085), (1088U, (uint?)1087), (1084U, (uint?)1088)
                },
                harness.World.MissionPrerequisiteEntries.AsNoTracking()
                .Where(entry => entry.ContentRevision == MarshesMissionDataV1.Revision && entry.Kind == MissionPrerequisiteKind.MissionCompleted)
                .AsEnumerable().Select(entry => (entry.MissionId, entry.RequiredMissionId)).ToArray());
            Assert.IsTrue(harness.World.MissionPrerequisiteEntries.AsNoTracking()
                .Where(entry => entry.ContentRevision == MarshesMissionDataV1.Revision && entry.Kind == MissionPrerequisiteKind.MissionCompleted)
                .AsEnumerable().All(entry => entry.RequiredMissionState == (byte)Rasa.Data.MissionState.Completed));
            // The others: 1870 and 1879 on the "Part II done" flag (1876001 = 1), of 1876 and 1877; 1868 and 1904 on Part III's
            // ending (1867001 = 1 or 2).
            CollectionAssert.AreEquivalent(new[]
                {
                    (1870U, (uint?)1876001, (uint?)1), (1879U, (uint?)1876001, (uint?)1),
                    (1868U, (uint?)1867001, (uint?)1), (1904U, (uint?)1867001, (uint?)2),
                    (916U, (uint?)914001, (uint?)1), (1033U, (uint?)914001, (uint?)2)
                },
                harness.World.MissionPrerequisiteEntries.AsNoTracking()
                .Where(entry => entry.ContentRevision == MarshesMissionDataV1.Revision && entry.Kind != MissionPrerequisiteKind.MissionCompleted)
                .AsEnumerable().Select(entry => (entry.MissionId, entry.PlayerFlagId, entry.PlayerFlagValue)).ToArray());
            Assert.IsTrue(harness.World.MissionPrerequisiteEntries.AsNoTracking()
                .Where(entry => entry.ContentRevision == MarshesMissionDataV1.Revision && entry.Kind != MissionPrerequisiteKind.MissionCompleted)
                .AsEnumerable().All(entry => entry.Kind == MissionPrerequisiteKind.PlayerFlagValue));
            var ruins = harness.World.MissionPrerequisiteEntries.AsNoTracking().Single(entry => entry.MissionId == ItLiesInRuins);
            Assert.AreEqual((PlateauMissionDataV1.Revision, MissionPrerequisiteKind.MissionCompleted, (uint?)1751),
                (ruins.ContentRevision, ruins.Kind, ruins.RequiredMissionId));
            Assert.AreEqual(PlateauMissionDataV1.Revision, harness.World.MissionContentDefinitionEntries.AsNoTracking()
                .Single(entry => entry.MissionId == 1751 && entry.Enabled).ContentRevision, "1751 on the same revision");
            Assert.IsFalse(harness.World.Set<MissionRepeatPolicyEntry>().AsNoTracking()
                .Any(entry => entry.ContentRevision == MarshesMissionDataV1.Revision || entry.MissionId == ItLiesInRuins));
        }

        /// <summary>The missions' sources and how far each is trusted: the client 1.0, TaRapedia 0.5, the reconstruction 0.3.</summary>
        [TestMethod]
        public void TheSourcesAreTrustedAsFarAsTheyGo()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var evidence = harness.World.MissionEvidenceEntries.AsNoTracking()
                .Where(entry => entry.ContentRevision == MarshesMissionDataV1.Revision ||
                    entry.ContentRevision == PlateauMissionDataV1.Revision && entry.MissionId == ItLiesInRuins).ToArray();
            foreach (var mission in MissionIds.Append(ItLiesInRuins))
                CollectionAssert.AreEqual(new[]
                    {
                        (MissionEvidenceSourceKind.Client, 1.0), (MissionEvidenceSourceKind.Documentation, 0.5),
                        (MissionEvidenceSourceKind.Reconstruction, 0.3)
                    },
                    evidence.Where(entry => entry.MissionId == mission).OrderBy(entry => entry.EvidenceId)
                        .Select(entry => (entry.SourceKind, entry.Confidence)).ToArray(), $"{mission} evidence");
        }

        /// <summary>
        /// The rewards: XP and credits of TaRapedia, the four items (or stacks) to choose one of (written out here); Beware: Buried
        /// Gas Lines gives its level-33 boots with every choice.
        /// </summary>
        [TestMethod]
        public void TheRewardsAreTaRapediasWithOneItemToChoose()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var marshes = MarshesMissionDataV1.Revision;
            foreach (var (mission, revision, experience, credits, items) in new (uint, string, uint, uint, (uint, uint)[])[]
            {
                (541, marshes, 75000, 5700, new (uint, uint)[] { (45762, 1), (45812, 1), (48680, 1), (14937, 1) }),
                (498, marshes, 65000, 5400, new (uint, uint)[] { (48108, 1), (15150, 1), (14738, 1), (49136, 1) }),
                (539, marshes, 65000, 5400, new (uint, uint)[] { (47944, 1), (47728, 1), (15513, 1), (14404, 1) }),
                (917, marshes, 65000, 5400, new (uint, uint)[] { (20335, 1), (28614, 1), (35571, 1), (39191, 1) }),
                // Armor Module: Resist Electric, Fire, Ice, Virulent [4].
                (959, marshes, 65000, 5400, new (uint, uint)[] { (123016, 1), (123026, 1), (123031, 1), (123071, 1) }),
                // 2 Class VII EMP Bomb, 2 Class VII Adrenaline Booster, 4 Class VII Fragmentation, 4 Class VII Cryogenic Grenade.
                (961, marshes, 32500, 3600, new (uint, uint)[] { (111232, 2), (118814, 2), (45446, 4), (111048, 4) }),
                // Hazmat, Mech, Motor Assist, Reflective vests.
                (1673, PlateauMissionDataV1.Revision, 75000, 3800, new (uint, uint)[] { (20951, 1), (33193, 1), (29247, 1), (36188, 1) }),
                // Bio vest, Stealth boots, Hazmat legs, Reflective gloves (30-34 / 29-33).
                (881, marshes, 65000, 5400, new (uint, uint)[] { (19246, 1), (38783, 1), (20762, 1), (35700, 1) }),
                // Bio boots, Graviton boots, Mech helmet, Stealth vest (35-39).
                (890, marshes, 65000, 5400, new (uint, uint)[] { (18750, 1), (23549, 1), (8855, 1), (10637, 1) }),
                (913, marshes, 140000, 0, new (uint, uint)[] { (18750, 1), (23549, 1), (8855, 1), (10637, 1) }),
                // Electric blade, Direct healing disc, Physical shotgun, Fire staff.
                (968, marshes, 160000, 7800, new (uint, uint)[] { (15475, 1), (18367, 1), (14820, 1), (49496, 1) }),
                // 2 Class VII Advanced Med Pack, 2 Power Charger, 4 Fragmentation, 4 Cryogenic Grenade.
                (925, marshes, 35000, 3700, new (uint, uint)[] { (45062, 2), (118907, 2), (45446, 4), (111048, 4) }),
                // 2 Class VII Advanced Med Pack, 2 Armor Charger, 4 Concussion, 4 Fragmentation Grenade.
                (1028, marshes, 35500, 3700, new (uint, uint)[] { (45062, 2), (118898, 2), (111018, 4), (45446, 4) }),
                // Incendiary Injector Gun 37-41, Electric Chaingun 38-42, Pulse Pistol 35-39, Pulse Cannon 34-38.
                (1029, marshes, 32500, 5550, new (uint, uint)[] { (45777, 1), (15111, 1), (14569, 1), (15437, 1) }),
                // Bio boots, Graviton boots, Mech helmet, Stealth vest (35-39), as 890.
                (1030, marshes, 70000, 5550, new (uint, uint)[] { (18750, 1), (23549, 1), (8855, 1), (10637, 1) }),
                // The client's mission blocks: Motor Assist helmet 33-37, Stealth legs 35-39, Hazmat and Reflective helmets 34-38.
                (1874, marshes, 130000, 7200, new (uint, uint)[] { (121221, 1), (121222, 1), (121223, 1), (121224, 1) }),
                // EMP machine gun 33-37, Physical shotgun 37-41, Virulent injection gun 37-41, Sonic rocket launcher 34-38.
                (1875, marshes, 65000, 5400, new (uint, uint)[] { (121237, 1), (121238, 1), (121239, 1), (121240, 1) }),
                // Bio, Graviton, Stealth, Mech boots 35-39, one block each.
                (1876, marshes, 140000, 7400, new (uint, uint)[] { (121241, 1), (121242, 1), (121243, 1), (121244, 1) }),
                (1877, marshes, 140000, 7400, new (uint, uint)[] { (121245, 1), (121246, 1), (121247, 1), (121248, 1) }),
                // Bio gloves, Graviton helmet, Mech gloves, Stealth gloves 35-39.
                (1878, marshes, 150000, 7600, new (uint, uint)[] { (121229, 1), (121230, 1), (121231, 1), (121232, 1) }),
                // Stealth vest, Bio vest, Mech legs, Graviton vest 35-39.
                (1879, marshes, 65000, 5400, new (uint, uint)[] { (121233, 1), (121234, 1), (121235, 1), (121236, 1) }),
                // Reflective and Hazmat legs 34-38, Motor Assist legs 38-42, Mech vest 35-39.
                (1870, marshes, 70000, 5550, new (uint, uint)[] { (121205, 1), (121206, 1), (121207, 1), (121208, 1) }),
                // Weapon Module: Crit Hit Bonus, Armor Module: Armor Absorption, Tool Module: Threat Reduction, Weapon Module:
                // Armor Piercing [4], as 932.
                (1873, marshes, 70000, 5550, new (uint, uint)[] { (123161, 1), (122951, 1), (123141, 1), (123146, 1) }),
                // 2 Class VII Advanced Med Pack, 2 Armor Charger, 4 Cryogenic, 4 Fragmentation Grenade.
                (1863, marshes, 65000, 5400, new (uint, uint)[] { (45062, 2), (118898, 2), (111048, 4), (45446, 4) }),
                // 2 Class VII EMP Bomb, 2 Power Charger, 4 Concussion, 4 Fragmentation Grenade.
                (1864, marshes, 32500, 3600, new (uint, uint)[] { (111232, 2), (118907, 2), (111018, 4), (45446, 4) }),
                // Area healing disc 35-39, Ice staff 35-39, Sonic pistol 35-39, Ice blade 36-40.
                (1869, marshes, 70000, 5550, new (uint, uint)[] { (121201, 1), (121202, 1), (121203, 1), (121204, 1) }),
                // Area repair tool 35-39, Sonic torqueshell rifle 33-37, Rifle 36-40, Sonic propellant gun 37-41.
                (1871, marshes, 65000, 5400, new (uint, uint)[] { (121225, 1), (121226, 1), (121227, 1), (121228, 1) }),
                // Hazmat gloves 34-38, Motor Assist gloves 33-37, Bio helmet 35-39, Reflective gloves 34-38.
                (1865, marshes, 65000, 5400, new (uint, uint)[] { (121209, 1), (121210, 1), (121211, 1), (121212, 1) }),
                // Mech helmet, Graviton legs, Stealth helmet, Bio legs 35-39.
                (1866, marshes, 65000, 5400, new (uint, uint)[] { (121213, 1), (121214, 1), (121215, 1), (121216, 1) }),
                // Rare EMP machine gun 33-37, Physical shotgun 37-41, Virulent injection gun 37-41, Sonic rocket launcher 34-38;
                // both Part IVs (one page on TaRapedia).
                (1868, marshes, 32500, 3600, new (uint, uint)[] { (121217, 1), (121218, 1), (121219, 1), (121220, 1) }),
                (1904, marshes, 32500, 3600, new (uint, uint)[] { (121217, 1), (121218, 1), (121219, 1), (121220, 1) }),
                // Bio and Mech boots, Graviton helmet 35-39, Reflective legs 34-38.
                (497, marshes, 60000, 5250, new (uint, uint)[] { (18750, 1), (32677, 1), (23808, 1), (36019, 1) }),
                // 6 Class VII Fragmentation Grenade, 2 EMP Bomb, 6 Cryogenic Grenade, 8 Standard Med Pack.
                (888, marshes, 65000, 5400, new (uint, uint)[] { (45446, 6), (111232, 2), (111048, 6), (45061, 8) }),
                // The client's block: Bio legs, Graviton legs, Mech helmet, Stealth helmet V04 35-39.
                (889, marshes, 130000, 7200, new (uint, uint)[] { (120013, 1), (120014, 1), (120015, 1), (120016, 1) }),
                // Weapon Module: Crit Hit Bonus, Armor Module: Body, Mind, Spirit Bonus [4].
                (1023, marshes, 70000, 5550, new (uint, uint)[] { (123161, 1), (122966, 1), (122976, 1), (122991, 1) }),
                // 2 Class VII Advanced Med Pack, 2 Adrenaline Booster, 4 Incendiary Grenade, 4 EMP Grenade.
                (914, marshes, 35000, 3700, new (uint, uint)[] { (45062, 2), (118814, 2), (111038, 4), (111028, 4) }),
                // The clients' blocks: the four T3 armour kinds V04 35-39.
                (916, marshes, 150000, 7600, new (uint, uint)[] { (119318, 1), (119319, 1), (119320, 1), (119321, 1) }),
                (1033, marshes, 150000, 7600, new (uint, uint)[] { (120009, 1), (120010, 1), (120011, 1), (120012, 1) }),
                // The client's block: Physical grenade launcher 35-39, Virulent leech gun 39-43, Ice net gun 36-40, EMP rifle 36-40.
                (1052, marshes, 75000, 5800, new (uint, uint)[] { (120097, 1), (120098, 1), (120099, 1), (120100, 1) }),
                // The arena rounds' Class VII stacks (TaRapedia): EMP Bomb, Power Charger, EMP / Concussion (Sonic) grenades;
                // EMP Bomb, Adrenaline Booster, Concussion, Frag; EMP Bomb, Armor Charger, Cryo, Incendiary.
                (1085, marshes, 35000, 3700, new (uint, uint)[] { (111232, 2), (118907, 2), (111028, 4), (111018, 4) }),
                (1087, marshes, 35000, 3700, new (uint, uint)[] { (111232, 2), (118814, 2), (111018, 4), (45446, 4) }),
                (1088, marshes, 35000, 3700, new (uint, uint)[] { (111232, 2), (118898, 2), (111048, 2), (111038, 2) }),
                // Virulent Injection Gun 32-36, Sonic Rocket Launcher 29-33, Electric Rifle 31-35, Laser Machine Gun 33-37.
                (1084, marshes, 70000, 5550, new (uint, uint)[] { (15654, 1), (49164, 1), (14734, 1), (48256, 1) })
            })
            {
                var reward = harness.World.Set<MissionRewardDefinitionEntry>().AsNoTracking()
                    .Single(entry => entry.MissionId == mission && entry.ContentRevision == revision);
                Assert.AreEqual((experience, credits, (byte)1), (reward.Experience, reward.Credits, reward.SelectionCount), $"{mission}");
                var rows = harness.World.MissionRewardItemEntries.AsNoTracking()
                    .Where(entry => entry.MissionId == mission && entry.ContentRevision == revision &&
                        entry.Kind == MissionRewardItemKind.Selectable)
                    .OrderBy(entry => entry.ItemId).ToArray();
                CollectionAssert.AreEqual(items, rows.Select(entry => (entry.ItemTemplateId, entry.Quantity)).ToArray(), $"{mission} items");
                var fixedItems = harness.World.MissionRewardItemEntries.AsNoTracking()
                    .Where(entry => entry.MissionId == mission && entry.ContentRevision == revision &&
                        entry.Kind != MissionRewardItemKind.Selectable).AsEnumerable()
                    .Select(entry => (entry.Kind, entry.ItemTemplateId, entry.Quantity)).ToArray();
                // 913: Motor Assist boots 33-37 with every choice (TaRapedia "one lvl 33 item plus one of four"); 1030: the
                // Nanoviral Regulator 42436 (class 21092, the client's turn-in text).
                CollectionAssert.AreEqual(mission switch
                    {
                        913 => new[] { (MissionRewardItemKind.Fixed, 28614U, 1U) },
                        1030 => new[] { (MissionRewardItemKind.Fixed, 42436U, 1U) },
                        _ => new (MissionRewardItemKind, uint, uint)[0]
                    },
                    fixedItems, $"{mission} fixed items");
            }
            // 1867: XP and credits only (TaRapedia names no item).
            var partThree = harness.World.Set<MissionRewardDefinitionEntry>().AsNoTracking()
                .Single(entry => entry.MissionId == 1867 && entry.ContentRevision == marshes);
            Assert.AreEqual((65000U, 5400U, (byte)0), (partThree.Experience, partThree.Credits, partThree.SelectionCount), "1867");
            Assert.IsFalse(harness.World.MissionRewardItemEntries.AsNoTracking()
                .Any(entry => entry.MissionId == 1867 && entry.ContentRevision == marshes), "1867 items");
        }

        /// <summary>
        /// The indicators: all on the Marshes map, keyed above UnnamedFrom but those keyed by the client's own names
        /// (missionobjectiveindicatorlanguage): 539's fern marker 5 "Calla Ferns", 881's and 890's 106 "Bane Harvester", 913's
        /// four 138 "Pipeline Supply Point", 1030's 146 "Paludos Ruins", 1875's 28 "Downed Dropship", 1876's 71 "Bane Terminal".
        /// It Lies in Ruins' marker names the Marshes map too.
        /// </summary>
        [TestMethod]
        public void IndicatorsAreOnTheMarshesAndOnlyTheClientsNamesAreBelowUnnamed()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var indicators = harness.World.MissionIndicatorEntries.AsNoTracking()
                .Where(entry => entry.ContentRevision == MarshesMissionDataV1.Revision).ToArray();
            Assert.HasCount(Indicators, indicators);
            var ruins = harness.World.MissionIndicatorEntries.AsNoTracking().Where(entry => entry.MissionId == ItLiesInRuins).ToArray();
            Assert.HasCount(1, ruins);
            // Each marker's map as the server finds it (MissionMarkerProjection.ResolveMap: the scene's IndicatorMaps, its map,
            // the areas or the pools); the row has no map.
            var catalog = new MissionContentCatalog(harness, new Dictionary<uint, Rasa.Structures.Mission>(), null);
            catalog.Load();
            foreach (var row in indicators.Concat(ruins))
            {
                Assert.AreEqual(1454U, MissionMarkerProjection.ResolveMap(catalog, row.MissionId, row.ObjectiveId, new MissionIndicator { IndicatorId = row.IndicatorId }), $"mission {row.MissionId} objective {row.ObjectiveId}: on the Marshes");
                if (NamedMarker(row.MissionId, row.ObjectiveId) is { } name)
                    Assert.AreEqual(name, row.IndicatorId, $"mission {row.MissionId} objective {row.ObjectiveId}: the client's name");
                else
                    Assert.IsTrue(row.IndicatorId > MissionIndicator.UnnamedFrom,
                        $"mission {row.MissionId} objective {row.ObjectiveId} indicator {row.IndicatorId}");
            }
        }

        /// <summary>The client's indicator name a Marshes objective's marker is keyed by, if it has one (also WildernessMigrationTests).</summary>
        internal static uint? NamedMarker(uint mission, uint objective) => (mission, objective) switch
        {
            (539, 1) => 5U,                                 // Calla Ferns
            (881, 1) or (890, 1) => 106U,                   // Bane Harvester
            (913, >= 1 and <= 4) => 138U,                   // Pipeline Supply Point
            (1030, 1) => 146U,                              // Paludos Ruins
            (1875, 1) => 28U,                               // Downed Dropship
            (1876, 1) => 71U,                               // Bane Terminal
            (889, >= 4 and <= 7) => 140U,                   // Possible location of Private Bruncke
            _ => null
        };

        /// <summary>Every visible Marshes objective has a marker; hidden objectives have none.</summary>
        [TestMethod]
        public void EveryVisibleObjectiveHasAMarker()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var world = harness.World;
            var revision = MarshesMissionDataV1.Revision;
            var marked = world.MissionIndicatorEntries.AsNoTracking().Where(entry => entry.ContentRevision == revision)
                .Select(entry => new { entry.MissionId, entry.ObjectiveId }).ToList()
                .Select(entry => (entry.MissionId, entry.ObjectiveId)).ToHashSet();
            var hidden = world.Set<MissionSceneBindingEntry>().AsNoTracking().Where(entry => entry.ContentRevision == revision).ToList()
                .SelectMany(entry => System.Text.RegularExpressions.Regex.Match(entry.Bindings, "\"HiddenObjectiveIds\"\\s*:\\s*\\[([^\\]]*)\\]",
                        System.Text.RegularExpressions.RegexOptions.IgnoreCase).Groups[1].Value
                    .Split(',', System.StringSplitOptions.RemoveEmptyEntries).Select(id => (entry.MissionId, uint.Parse(id.Trim()))))
                .ToHashSet();
            var objectives = world.MissionObjectiveDefinitionEntries.AsNoTracking().Where(entry => entry.ContentRevision == revision)
                .Select(entry => new { entry.MissionId, entry.ObjectiveId }).ToList();
            Assert.HasCount(98, objectives, "the objectives are read");
            Assert.IsTrue(hidden.Contains((881U, 2U)), "881's tally is hidden");
            Assert.IsTrue(hidden.Contains((1878U, 2U)) && hidden.Contains((1873U, 6U)), "1878's and 1873's tallies are hidden");
            Assert.IsTrue(hidden.Contains((1871U, 2U)), "1871's tally is hidden");
            Assert.IsTrue(hidden.Contains((1867U, 4U)), "1867's outcome is hidden");
            Assert.IsTrue(hidden.Contains((914U, 3U)), "914's outcome is hidden");
            var missing = objectives.Select(entry => (entry.MissionId, entry.ObjectiveId))
                .Where(key => !marked.Contains(key) && !hidden.Contains(key)).OrderBy(key => key).ToArray();
            Assert.IsEmpty(missing, string.Join(", ", missing));
            Assert.IsFalse(marked.Any(hidden.Contains), "no marker on a hidden objective");
            Assert.IsTrue(world.MissionIndicatorEntries.AsNoTracking().Any(entry => entry.MissionId == ItLiesInRuins && entry.ObjectiveId == 1),
                "It Lies in Ruins' one objective too");
        }

        /// <summary>The markers stand at their objective's target: a creature's pool, an NPC, a beat, a body, a feeder.</summary>
        [TestMethod]
        public void TheMarkersStandAtTheirObjectivesTargets()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var rows = harness.World.MissionIndicatorEntries.AsNoTracking()
                .Where(entry => entry.ContentRevision == MarshesMissionDataV1.Revision || entry.MissionId == ItLiesInRuins).ToList();
            var expected = new List<(uint Mission, uint Objective, double X, double Z, double Radius)>
            {
                (541, 1, 125, -364, 15),                            // Cast'ka's pool 520032 (World DB)
                (498, 1, -470, 39.3, 30), (498, 1, -262.8, -532.4, 30), (498, 1, -319.2, -566.8, 40),   // Thrax pools 531293, 531297, 531283
                (539, 1, -120, 20, 45),                             // between the Grubber pools
                (539, 2, -140, 0, 20), (539, 2, -100, 40, 20),
                (539, 3, -158.8, 40.4, 20), (539, 3, -120, 90, 20),
                (539, 4, -200, -20, 20), (539, 4, -150, 150, 20),
                (917, 1, -387, 492.2, 85), (917, 2, -291, 25, 8), (917, 3, -38, 49, 8), (917, 4, -564, -53, 8),
                (959, 1, -250, 600, 20), (959, 1, -290, 620, 20),   // the steak pools 760310, 760311
                (1673, 1, -142.7, 618, 8),                          // Morrison's pool 510162 (World DB)
                (881, 1, 26, 646, 95), (890, 1, 26, 646, 95),       // round the harvester field east of Falcon Hold
                (913, 1, 732, -276, 20), (913, 2, 734, -149, 20), (913, 3, 860, -68, 20), (913, 4, 700, 107, 20),   // pipeline ends
                (968, 1, 459, 11, 15), (968, 2, 362, -168, 15), (968, 3, 490, 413, 15),  // Fik 520028, Sto 520030, Barzak 520029
                (925, 1, -138, -472, 8), (1028, 1, 440, -590, 8),   // Wilson, Kaav
                (1029, 1, 300, -650, 20), (1030, 1, -2, 29, 15),    // the Strider pool 760315, the scrapper (TaRapedia /loc)
                (1875, 1, -51, -366, 15), (1876, 1, 101, -289, 8),  // the black box, the computer (Ten Ton Hammer)
                (1877, 1, 170, 55, 15),                             // Nelg's post (Ten Ton Hammer)
                (1878, 1, 443, -585, 30),                           // round the three core crates before the depot door
                (1879, 1, 420, -728, 20), (1879, 2, 400, -730, 10), // round the three consoles, the generator
                (1870, 1, 178, 46, 10),                             // the CommLink terminal on the deck by Nelg's post
                (1873, 1, 178, 43, 15),                             // round the four supply crates
                (1873, 2, -110, -320, 8), (1873, 3, -110, -335, 8), (1873, 4, -125, -335, 8), (1873, 5, -132.11, -330.6, 8),  // the soldiers
                (1863, 1, -700, -560, 10),                          // Koffman's body
                (1864, 1, -740, -500, 8), (1864, 2, -420, -40, 8), (1864, 3, -430, 20, 8), (1864, 4, -410, 70, 8),
                (1864, 5, -786.5, -522.9, 8),                       // the five fires
                (1869, 1, -760, -508, 8), (1869, 2, -730, -490, 8), (1869, 3, -720, -550, 8),   // the spike spots
                (1871, 1, -748, -516, 15),                          // round the five flowers
                (1865, 1, -318, -570.1, 25), (1865, 1, -470, 39.3, 25), (1865, 1, 24, -368.3, 25),   // technician pools 531283, 531293, 531288
                (1865, 2, -680, -520, 25),                          // the Lightbender pool 760331
                (1866, 1, -495.3, -257.7, 20), (1866, 1, -595.9, -166.8, 20),   // Mox pools 540038, 540039
                (1867, 1, -735, 171, 8), (1867, 2, -593, -438, 8), (1867, 3, -735, 171, 8),   // Masai, Duvall, Masai
                (1868, 1, -776, -542, 8), (1904, 1, -593, -438, 8), // Lou, Duvall
                (497, 1, -143.9, 614.2, 15), (888, 1, -276.8, -761, 15),    // DuBois, Vincent
                (889, 2, -317.6, -2.8, 25),                         // Price
                (1023, 1, 110, 405, 15), (1023, 2, 100, 530.4, 15), (1023, 3, 45.5, 704, 15), (1023, 4, -142.7, 609.2, 20),
                (1023, 5, 110, 405, 8), (1023, 6, 100, 530.4, 8), (1023, 7, 45.5, 704, 8), // Belfast, Delaio, Urqhart, Elway
                (914, 1, -310, 126, 8), (914, 1, -150, 612, 8),     // the supply crates by the Paludos and Falcon Hold med tents
                (914, 2, -184, 627, 8), (914, 4, -276.8, -761, 8),  // Figgins, Vincent
                (916, 1, 330, -525, 40), (916, 2, 350, -525, 15), (916, 3, 320, -525, 15),      // the kiosks, Vesh 520035, Koorva 520033
                (1033, 1, 330, -525, 40), (1033, 2, 350, -525, 15), (1033, 3, 320, -525, 15),
                (1052, 1, 196.4, -669.1, 8), (1052, 2, 196.4, -669.1, 30),                     // Samuel, round him
                (1085, 2, -219.9, -799.1, 8), (1085, 1, -219.9, -761.1, 25),                   // Foster, the arena
                (1087, 2, -219.9, -799.1, 8), (1087, 1, -219.9, -761.1, 25),
                (1088, 2, -219.9, -799.1, 8), (1088, 1, -219.9, -761.1, 25), (1088, 3, -219.9, -761.1, 25),
                (1084, 1, -664.6, -583, 25), (1084, 2, -318, -570.1, 25), (1084, 2, 180.7, -767.7, 25),   // Stalker 540002, Caretakers
                (1084, 2, 165, -726.7, 25), (1084, 3, -650, -620, 25)                          // 531283, 531307, 531286; the Machina
            };
            // 889's search places: the client's name on 4-7, and 1's marker at each.
            foreach (var (objective, x, z) in new (uint, double, double)[] { (4, -520, 40), (5, -450, 120), (6, -330, -160), (7, -200, -100) })
            {
                expected.Add((889, objective, x, z, 20));
                expected.Add((889, 1, x, z, 20));
            }
            // 1874's eight: the Bane Mortar pools of the eastern wall line (World DB 592164-592171).
            foreach (var (x, z) in new (double, double)[]
            {
                (176.586, -554.06), (247.697, -467.506), (294.206, -411.35), (377.255, -269.534), (409.053, -193.944),
                (434.858, -121.008), (446.902, -50.398), (462.255, 78.515)
            })
                expected.Add((1874, 1, x, z, 15));
            // 961's twelve: the map's Treeback Feeders (client map statics ArchForeanTreebackfeederV01).
            foreach (var (x, z) in new (double, double)[]
            {
                (-148.1, 751.5), (134.2, 297.7), (243.7, 427.2), (139.4, 668), (-1.6, 778.2), (-264.2, 663.2),
                (-422.2, 553.8), (-329.5, 448.9), (-230.3, 425.3), (-142.4, 461.8), (-78.9, 407.3), (-155.6, 321.5)
            })
                expected.Add((961, 1, x, z, 8));
            Assert.HasCount(Indicators + 1, expected);
            foreach (var (mission, objective, x, z, radius) in expected)
                Assert.IsTrue(rows.Any(row => row.MissionId == mission && row.ObjectiveId == objective &&
                        Vector2.Distance(new((float)row.PosX, (float)row.PosZ), new((float)x, (float)z)) < 1f && row.Radius == radius),
                    $"{mission} objective {objective}'s marker at ({x}, {z}) radius {radius}");
            Assert.HasCount(Indicators + 1, rows);

            // Each of 539's pools inside its own marker; Alia's whole beat inside hers.
            foreach (var (_, _, x, _, z) in MarshesPaludosNpcsV1.GasPools.Where(pool => pool.Creature == MarshesPaludosNpcsV1.SwampGrubber))
                Assert.IsLessThan(MarshesWhatsThatGasV1.FernMarkerRadius - 10, Vector2.Distance(new((float)x, (float)z), new(-120, 20)), "a Grubber pool in the fern marker");
            foreach (var (x, _, z) in MarshesPaludosNpcsV1.AliaBeat)
                Assert.IsLessThan(MarshesPaludosNpcsV1.AliaBeatRadius, Vector2.Distance(new((float)x, (float)z), new(-387, 492.2f)), "the beat in her marker");
            // Every harvester inside the field's marker; both receptacles of each joint inside its joint's.
            foreach (var at in MarshesHarvestersNpcsV1.Harvesters.Concat(MarshesReapingTheHarvestV1.Taps))
                Assert.IsLessThan(MarshesHarvestersNpcsV1.FieldRadius - 5, Vector2.Distance(new(at.X, at.Z), new(26, 646)), "a harvester in the field marker");
            foreach (var (_, _, _, _, joint, receptacles) in MarshesBuriedGasLinesV1.JointRows)
                foreach (var at in receptacles)
                    Assert.IsLessThan(MarshesBuriedGasLinesV1.JointRadius, Vector2.Distance(new(at.X, at.Z), new(joint.X, joint.Z)), "a receptacle in its joint's marker");
        }

        /// <summary>No scene object of the Marshes is of a class the client draws as its "Default" pyramid (mesh 776).</summary>
        [TestMethod]
        public void NoSceneObjectIsOfAClassTheClientDrawsAsDefault()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var meshes = harness.World.EntityClassEntries.AsNoTracking().ToDictionary(entry => entry.Id, entry => entry.MeshId);
            var objects = 0;
            foreach (var binding in harness.World.Set<MissionSceneBindingEntry>().AsNoTracking().ToList()
                .Where(entry => entry.ContentRevision == MarshesMissionDataV1.Revision))
            {
                var scene = System.Text.Json.JsonSerializer.Deserialize<Rasa.Missions.Content.MissionSceneDefinition>(binding.Bindings,
                    Rasa.Missions.Content.MissionContentCodec.Options);
                foreach (var actor in scene.Actors.Values.Where(actor => actor.Kind == Rasa.Missions.Scenes.SceneActorKind.Object))
                {
                    objects++;
                    Assert.IsTrue(meshes.TryGetValue(actor.TemplateId, out var mesh), $"{binding.MissionId} {actor.Role}: class {actor.TemplateId}");
                    Assert.AreNotEqual(776U, mesh, $"{binding.MissionId} {actor.Role}: class {actor.TemplateId} has the Default mesh");
                }
            }
            // Helka's corpse and pack, the twelve feed acceptors; 881's seven harvesters and their taps, 890's five harvesters,
            // 913's eight receptacles; 1030's scrapper; 1875's black box, 1876's computer; 1878's three crates, 1879's three
            // consoles, generator and charge, 1870's terminal, 1873's four crates; 1863's body, 1864's five fires, 1869's three
            // spike spots, 1871's five flowers; 914's two supply crates, 916's and 1033's five kiosks each.
            Assert.AreEqual(14 + 14 + 5 + 8 + 1 + 2 + 3 + 5 + 1 + 4 + 1 + 5 + 3 + 5 + 2 + 10, objects);
        }

        /// <summary>
        /// The client's use argument of each class a Marshes scene lets the player use: usabledata lookup[class][0] of the
        /// 1.16.5.0 client (written out here). The client sends that argument, and the server takes a use only with the
        /// scene's own (SceneApplication.CanUseObject), so a scene object with another one can never be used in the client.
        /// </summary>
        private static readonly Dictionary<uint, uint> ClientUseArguments = new()
        {
            [11096] = 3,    // UsableItemDisp_item_bag_hum_v01 (917 Helka's pack)
            [11116] = 3,    // UsableItemDisp_item_crate_bane_v01CONVERT2TD (881 sample taps)
            [7334] = 1,     // UsableItemAccHumBombV01 (913 bomb receptacles)
            [21824] = 1,    // UsableItemAccForeanTreebackFood (961 feed acceptors)
            [11114] = 3,    // UsableItemDisp_item_canister_hum_v01 (1030 viral scrapper; g_usabledata.dis:19364)
            [22105] = 1,    // UsableItemDispBlackBox (1875 black box; lookup[0] None, the client sends 1)
            [20631] = 1,    // UsableStatelessBaneConsoleKeyboard (1876 Bane computer; lookup[0] None, the client sends 1)
            [7468] = 1,     // UsableTwoStateBaneConsoleSwitchV01 (1879 Bane computers; lookup[0] 1)
            [7870] = 1,     // UsableBombHumV01 (1879 generator charge; lookup[0] None, the client sends 1)
            [6137] = 1,     // UsableTwoStateForeanFirePitV01 (1864 fires; lookup[0] None, the client sends 1)
            [7847] = 3,     // UsableItemAccHumGpsBeacon (1869 spike spots; lookup[0] 3)
            [22518] = 3, [22519] = 3, [22520] = 3, [22521] = 3, [22522] = 3,   // UsableItemDispMawflowerV01-V05 (1871 flowers; lookup[0] 3)
            [11078] = 3,    // UsableItemDisp_item_crate_hum_v01 (914 supply crates; lookup[0] 3)
        };

        [TestMethod]
        public void EveryUsableSceneObjectTakesTheClientsUseArgument()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var used = 0;
            foreach (var binding in harness.World.Set<MissionSceneBindingEntry>().AsNoTracking().ToList()
                .Where(entry => entry.ContentRevision == MarshesMissionDataV1.Revision))
            {
                var scene = System.Text.Json.JsonSerializer.Deserialize<Rasa.Missions.Content.MissionSceneDefinition>(binding.Bindings,
                    Rasa.Missions.Content.MissionContentCodec.Options);
                foreach (var actor in scene.Actors.Values.Where(actor => actor.UseAction != null))
                {
                    used++;
                    Assert.IsTrue(ClientUseArguments.TryGetValue(actor.TemplateId, out var argument),
                        $"{binding.MissionId} {actor.Role}: class {actor.TemplateId} has no pinned client use argument");
                    Assert.AreEqual(argument, actor.UseAction.ActionArgId,
                        $"{binding.MissionId} {actor.Role}: class {actor.TemplateId} is used with the client's argument");
                }
            }
            // Helka's pack, the twelve feeders, the seven taps, the eight receptacles, the scrapper, the black box, the computer;
            // 1878's three crates, 1879's three consoles and the charge, 1873's four crates (1870's terminal talks); 1864's five
            // fires, 1869's three spots, 1871's five flowers (1863's body talks); 914's two crates.
            Assert.AreEqual(1 + 12 + 7 + 8 + 1 + 2 + 3 + 4 + 4 + 5 + 3 + 5 + 2, used);
        }

        /// <summary>
        /// Marshes_paludos takes out its four missions, the three Foreans, Alia's squad and the six pools; the upstream
        /// Paludos NPCs, Cast'ka and the Pools missions stay. Rolled back below it, Marshes_falcon_hold's rows go first.
        /// </summary>
        [TestMethod]
        public void PaludosRollsBackWithItsMigration()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var world = harness.World;
            bool Has(uint mission) => world.MissionContentDefinitionEntries.AsNoTracking()
                .Any(entry => entry.MissionId == mission && entry.ContentRevision == MarshesMissionDataV1.Revision);
            int Count(string sql) => world.Database.SqlQueryRaw<int>(sql).AsEnumerable().Single();
            (int, int, int, int, int, int, int) All() => (
                Count("SELECT COUNT(*) AS Value FROM creature WHERE id BETWEEN 760300 AND 760399"),
                Count("SELECT COUNT(*) AS Value FROM spawnpool WHERE id BETWEEN 760300 AND 760399"),
                Count("SELECT COUNT(*) AS Value FROM npc_package WHERE id BETWEEN 760300 AND 760399"),
                Count("SELECT COUNT(*) AS Value FROM creature_appearance WHERE id BETWEEN 760300 AND 760399"),
                Count("SELECT COUNT(*) AS Value FROM npc_squad_spawn WHERE id = 760300") +
                    Count("SELECT COUNT(*) AS Value FROM npc_squad_type WHERE id = 760300") +
                    Count("SELECT COUNT(*) AS Value FROM npc_squad_path WHERE id = 760300"),
                Count("SELECT COUNT(*) AS Value FROM npc_squad_path_point WHERE path_id = 760300"),
                Count("SELECT COUNT(*) AS Value FROM mission_scene_binding WHERE content_revision = 'marshes_1_16_5'"));
            int Upstream() => Count("SELECT COUNT(*) AS Value FROM creature WHERE id IN (510160, 510171, 510172, 520032)");
            var setOne = new uint[] { 541, 498, 539, 917 };

            Assert.IsTrue(MissionIds.All(Has));
            // Set 2 adds Horsh (his pool and six appearance slots), two pools and two scenes (959, 961); set 3 Brazos (his pool
            // and six slots) and three scenes (881, 890, 913); set 4 Wilson (his pool, package and six slots), Kaav (pool,
            // package), the Strider and its pool, and four scenes (925, 1028, 1029, 1030); set 5 McDonald, Dikah and Kiang (pool,
            // package, slots: six, the spear, six), Nelg, and three scenes (1875, 1876, 1877); set 6 Thoung, Brady, Zui, Dahn and
            // Nez (pool, package, slots: the bow, Remick's six, the bow, the spear, the staff) and four scenes (1878, 1879, 1870,
            // 1873); set 7 Jeska, Dudley and Duvall (pool, package, Vincent's six slots), the Maw pool, and four scenes (1863,
            // 1864, 1869, 1871); set 8 Lou and Masai (pool, package, slots: Vincent's six, Moawi's staff), the Lightbender and
            // its pool, and three scenes (1865, 1866, 1867); set 9 the warrior, Otto, Bruncke and the soldier (slots: the spear,
            // Vincent's six, Morrison's six, the rifle; Bruncke's package), Belfast, Delaio and Urqhart (pool, package, Morrison's
            // six slots), and four scenes (497, 888, 889, 1023); set 10 Figgins and Holloway (pool, package, Morrison's and the
            // Pools Holloway's six slots), the mech, Samuel's stand-in (package, Samuel's six) and the Retread (Samuel's six),
            // and four scenes (914, 916, 1033, 1052); set 11 Karl and Foster (pool, package, Vincent's six slots), the five
            // fighters (the Rifleman's slot each), the Machina and its pool, and four scenes (1085, 1087, 1088, 1084).
            Assert.AreEqual((43, 38, 29, 142, 3, 4, 38), All(), "541, 968, 1874, 1868 and 1904 have no scene");
            Assert.AreEqual(4, Upstream());

            world.GetService<IMigrator>().Migrate(Previous(world, Paludos));
            Assert.IsFalse(MissionIds.Any(Has));
            Assert.AreEqual((0, 0, 0, 0, 0, 0, 0), All());
            Assert.IsFalse(world.MissionEvidenceEntries.Any(entry => entry.ContentRevision == MarshesMissionDataV1.Revision));
            Assert.IsFalse(world.MissionIndicatorEntries.Any(entry => entry.ContentRevision == MarshesMissionDataV1.Revision));
            Assert.IsFalse(world.MissionRewardItemEntries.Any(entry => entry.ContentRevision == MarshesMissionDataV1.Revision));
            Assert.AreEqual(4, Upstream(), "the upstream NPCs and Cast'ka stay");
            Assert.IsTrue(world.MissionContentDefinitionEntries.Any(entry => entry.MissionId == PoolsBuildingMaterialsV1.MissionId &&
                entry.ContentRevision == PoolsMissionDataV1.Revision), "the Pools missions stay");

            world.GetService<IMigrator>().Migrate(Paludos);
            Assert.IsTrue(setOne.All(Has));
            Assert.AreEqual((3, 8, 3, 3, 3, 4, 3), All());
        }

        /// <summary>
        /// Marshes_falcon_hold takes out It Lies in Ruins, Tastes Like Grubber, Fixing the Food Chain, Horsh and the two
        /// steak pools; set 1, Retreads to Retreads and the upstream NPCs it copied or used (Vetter, Morrison, Vincent) stay.
        /// Up again, all are back.
        /// </summary>
        [TestMethod]
        public void FalconHoldRollsBackWithItsMigration()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var world = harness.World;
            bool Has(uint mission) => world.MissionContentDefinitionEntries.AsNoTracking().Any(entry => entry.MissionId == mission);
            int Count(string sql) => world.Database.SqlQueryRaw<int>(sql).AsEnumerable().Single();
            (int, int, int, int, int) SetTwo() => (
                Count("SELECT COUNT(*) AS Value FROM creature WHERE id = 760309"),
                Count("SELECT COUNT(*) AS Value FROM spawnpool WHERE id BETWEEN 760309 AND 760311"),
                Count("SELECT COUNT(*) AS Value FROM npc_package WHERE id = 760309") +
                    Count("SELECT COUNT(*) AS Value FROM creature_appearance WHERE id = 760309"),
                Count("SELECT COUNT(*) AS Value FROM mission_indicator WHERE mission_id IN (959, 961, 1673)"),
                Count("SELECT COUNT(*) AS Value FROM mission_evidence WHERE mission_id IN (959, 961, 1673)"));
            int Kept() => Count("SELECT COUNT(*) AS Value FROM creature WHERE id IN (510162, 510165, 510168)");
            var setTwo = new uint[] { 959, 961, ItLiesInRuins };
            var setOne = new uint[] { 541, 498, 539, 917 };

            Assert.IsTrue(setTwo.All(Has));
            Assert.AreEqual((1, 3, 7, 15, 9), SetTwo());
            Assert.AreEqual(3, Kept());

            world.GetService<IMigrator>().Migrate(Previous(world, FalconHold));
            Assert.IsFalse(setTwo.Any(Has));
            Assert.AreEqual((0, 0, 0, 0, 0), SetTwo());
            Assert.AreEqual(3, Kept(), "Vetter, Morrison and Vincent stay");
            Assert.IsTrue(setOne.All(Has), "set 1 stays");
            Assert.IsTrue(Has(1751), "Retreads to Retreads stays");

            world.GetService<IMigrator>().Migrate(FalconHold);
            Assert.IsTrue(setTwo.All(Has));
            Assert.AreEqual((1, 3, 7, 15, 9), SetTwo());
            Assert.AreEqual(MarshesMissionDataV1.CategoryId, world.MissionContentDefinitionEntries.AsNoTracking()
                .Single(entry => entry.MissionId == ItLiesInRuins).CategoryId, "the Marshes category again");
        }

        /// <summary>
        /// Marshes_harvesters takes out Reaping the Harvest, Hassling the Harvesters, Beware: Buried Gas Lines, A Butcher, a
        /// Baker, and a Caretaker and Brazos; sets 1 and 2 and the upstream NPCs and bosses they use or copy (Vetter, Kline,
        /// Taavik, Morrison, Fik, Barzak, Sto) stay. Up again, all are back.
        /// </summary>
        [TestMethod]
        public void HarvestersRollBackWithTheirMigration()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var world = harness.World;
            bool Has(uint mission) => world.MissionContentDefinitionEntries.AsNoTracking()
                .Any(entry => entry.MissionId == mission && entry.ContentRevision == MarshesMissionDataV1.Revision);
            int Count(string sql) => world.Database.SqlQueryRaw<int>(sql).AsEnumerable().Single();
            (int, int, int, int, int, int, int) SetThree() => (
                Count("SELECT COUNT(*) AS Value FROM creature WHERE id = 760312") +
                    Count("SELECT COUNT(*) AS Value FROM spawnpool WHERE id = 760312"),
                Count("SELECT COUNT(*) AS Value FROM npc_package WHERE id = 760312") +
                    Count("SELECT COUNT(*) AS Value FROM creature_appearance WHERE id = 760312"),
                Count("SELECT COUNT(*) AS Value FROM mission_indicator WHERE mission_id IN (881, 890, 913, 968)"),
                Count("SELECT COUNT(*) AS Value FROM mission_evidence WHERE mission_id IN (881, 890, 913, 968)"),
                Count("SELECT COUNT(*) AS Value FROM mission_scene_binding WHERE mission_id IN (881, 890, 913, 968)"),
                Count("SELECT COUNT(*) AS Value FROM mission_reward_item WHERE mission_id IN (881, 890, 913, 968)"),
                Count("SELECT COUNT(*) AS Value FROM mission_prerequisite WHERE mission_id IN (881, 890, 913, 968)"));
            int Kept() => Count("SELECT COUNT(*) AS Value FROM creature WHERE id IN (510162, 510164, 510165, 510166, 520028, 520029, 520030)");
            var setThree = new uint[] { 881, 890, 913, 968 };
            var before = new uint[] { 541, 498, 539, 917, 959, 961 };
            var full = (2, 7, 9, 12, 3, 17, 3);

            Assert.IsTrue(setThree.All(Has));
            Assert.AreEqual(full, SetThree());
            Assert.AreEqual(7, Kept());

            world.GetService<IMigrator>().Migrate(Previous(world, Harvesters));
            Assert.IsFalse(setThree.Any(Has));
            Assert.AreEqual((0, 0, 0, 0, 0, 0, 0), SetThree());
            Assert.AreEqual(7, Kept(), "the upstream NPCs and bosses stay");
            Assert.IsTrue(before.All(Has), "sets 1 and 2 stay");

            world.GetService<IMigrator>().Migrate(Harvesters);
            Assert.IsTrue(setThree.All(Has));
            Assert.AreEqual(full, SetThree());
        }

        /// <summary>
        /// Marshes_regulator takes out the four parts of The Regulator, Wilson, Kaav and the Strider with their pools; sets 1-3
        /// and the rows they copy or use (McCormick, the Pools' Strider, Daddy Long-Legs) stay. Up again, all are back.
        /// </summary>
        [TestMethod]
        public void TheRegulatorRollsBackWithItsMigration()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var world = harness.World;
            bool Has(uint mission) => world.MissionContentDefinitionEntries.AsNoTracking()
                .Any(entry => entry.MissionId == mission && entry.ContentRevision == MarshesMissionDataV1.Revision);
            int Count(string sql) => world.Database.SqlQueryRaw<int>(sql).AsEnumerable().Single();
            (int, int, int, int, int, int, int) SetFour() => (
                Count("SELECT COUNT(*) AS Value FROM creature WHERE id BETWEEN 760313 AND 760315") +
                    Count("SELECT COUNT(*) AS Value FROM spawnpool WHERE id BETWEEN 760313 AND 760315"),
                Count("SELECT COUNT(*) AS Value FROM npc_package WHERE id BETWEEN 760313 AND 760315") +
                    Count("SELECT COUNT(*) AS Value FROM creature_appearance WHERE id BETWEEN 760313 AND 760315") +
                    Count("SELECT COUNT(*) AS Value FROM creature_stat WHERE id BETWEEN 760313 AND 760315"),
                Count("SELECT COUNT(*) AS Value FROM mission_indicator WHERE mission_id IN (925, 1028, 1029, 1030)"),
                Count("SELECT COUNT(*) AS Value FROM mission_evidence WHERE mission_id IN (925, 1028, 1029, 1030)"),
                Count("SELECT COUNT(*) AS Value FROM mission_scene_binding WHERE mission_id IN (925, 1028, 1029, 1030)"),
                Count("SELECT COUNT(*) AS Value FROM mission_reward_item WHERE mission_id IN (925, 1028, 1029, 1030)"),
                Count("SELECT COUNT(*) AS Value FROM mission_prerequisite WHERE mission_id IN (925, 1028, 1029, 1030)"));
            int Kept() => Count("SELECT COUNT(*) AS Value FROM creature WHERE id IN (510201, 750100, 520031)");
            var setFour = new uint[] { 925, 1028, 1029, 1030 };
            var before = new uint[] { 541, 498, 539, 917, 959, 961, 881, 890, 913, 968 };
            // 3 creatures and 3 pools; 2 packages, Wilson's 6 slots, 2 stats; 4 markers; 12 sources; 4 scenes; 4 x 4 items and
            // the Regulator; 3 prerequisites.
            var full = (6, 10, 4, 12, 4, 17, 3);

            Assert.IsTrue(setFour.All(Has));
            Assert.AreEqual(full, SetFour());
            Assert.AreEqual(3, Kept());

            world.GetService<IMigrator>().Migrate(Previous(world, Regulator));
            Assert.IsFalse(setFour.Any(Has));
            Assert.AreEqual((0, 0, 0, 0, 0, 0, 0), SetFour());
            Assert.AreEqual(3, Kept(), "McCormick, the Pools' Strider and Daddy Long-Legs stay");
            Assert.IsTrue(before.All(Has), "sets 1-3 stay");

            world.GetService<IMigrator>().Migrate(Regulator);
            Assert.IsTrue(setFour.All(Has));
            Assert.AreEqual(full, SetFour());
        }

        /// <summary>
        /// Marshes_sentry_tower takes out Mortar Madness, Under the Radar and both versions of its Part II, McDonald, Dikah, Kiang
        /// and Nelg; sets 1-4 and the rows they copy or use (Elway, Morrison, Nivvik, the Bane Mortar) stay. Up again, all are
        /// back.
        /// </summary>
        [TestMethod]
        public void TheSentryTowerRollsBackWithItsMigration()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var world = harness.World;
            bool Has(uint mission) => world.MissionContentDefinitionEntries.AsNoTracking()
                .Any(entry => entry.MissionId == mission && entry.ContentRevision == MarshesMissionDataV1.Revision);
            int Count(string sql) => world.Database.SqlQueryRaw<int>(sql).AsEnumerable().Single();
            (int, int, int, int, int, int, int, int) SetFive() => (
                Count("SELECT COUNT(*) AS Value FROM creature WHERE id BETWEEN 760316 AND 760319") +
                    Count("SELECT COUNT(*) AS Value FROM spawnpool WHERE id BETWEEN 760316 AND 760319"),
                Count("SELECT COUNT(*) AS Value FROM npc_package WHERE id BETWEEN 760316 AND 760319") +
                    Count("SELECT COUNT(*) AS Value FROM creature_appearance WHERE id BETWEEN 760316 AND 760319") +
                    Count("SELECT COUNT(*) AS Value FROM creature_stat WHERE id BETWEEN 760316 AND 760319"),
                Count("SELECT COUNT(*) AS Value FROM mission_indicator WHERE mission_id IN (1874, 1875, 1876, 1877)"),
                Count("SELECT COUNT(*) AS Value FROM mission_evidence WHERE mission_id IN (1874, 1875, 1876, 1877)"),
                Count("SELECT COUNT(*) AS Value FROM mission_scene_binding WHERE mission_id IN (1874, 1875, 1876, 1877)"),
                Count("SELECT COUNT(*) AS Value FROM mission_reward_item WHERE mission_id IN (1874, 1875, 1876, 1877)"),
                Count("SELECT COUNT(*) AS Value FROM mission_prerequisite WHERE mission_id IN (1874, 1875, 1876, 1877)"),
                Count("SELECT COUNT(*) AS Value FROM mission_action WHERE mission_id IN (1876, 1877) AND player_flag_id = 1876001"));
            int Kept() => Count("SELECT COUNT(*) AS Value FROM creature WHERE id IN (510169, 510162, 520034, 592006)");
            var setFive = new uint[] { 1874, 1875, 1876, 1877 };
            var before = new uint[] { 541, 498, 539, 917, 959, 961, 881, 890, 913, 968, 925, 1028, 1029, 1030 };
            // 4 creatures and 3 pools; 3 packages, 6 + 1 + 6 slots, Nelg's stat; 8 + 1 + 1 + 1 markers; 12 sources; 3 scenes;
            // 4 x 4 items; 2 prerequisites; the "Part II done" flag of 1876 and 1877.
            var full = (7, 17, 11, 12, 3, 16, 2, 2);

            Assert.IsTrue(setFive.All(Has));
            Assert.AreEqual(full, SetFive());
            Assert.AreEqual(4, Kept());

            world.GetService<IMigrator>().Migrate(Previous(world, SentryTower));
            Assert.IsFalse(setFive.Any(Has));
            Assert.AreEqual((0, 0, 0, 0, 0, 0, 0, 0), SetFive());
            Assert.AreEqual(4, Kept(), "Elway, Morrison, Nivvik and the Bane Mortar stay");
            Assert.IsTrue(before.All(Has), "sets 1-4 stay");

            world.GetService<IMigrator>().Migrate(SentryTower);
            Assert.IsTrue(setFive.All(Has));
            Assert.AreEqual(full, SetFive());
        }

        /// <summary>
        /// Marshes_sentry_tower_two takes out The Core of the Matter, This Calls For Destruction, Data Disaster and Need a Little
        /// Help Here, Thoung, Brady, Zui, Dahn and Nez; sets 1-5 and the rows they copy or use (Remick, McDonald, Kiang, the
        /// "Part II done" flag's actions) stay. Up again, all are back.
        /// </summary>
        [TestMethod]
        public void TheSentryTowersSecondPartRollsBackWithItsMigration()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var world = harness.World;
            bool Has(uint mission) => world.MissionContentDefinitionEntries.AsNoTracking()
                .Any(entry => entry.MissionId == mission && entry.ContentRevision == MarshesMissionDataV1.Revision);
            int Count(string sql) => world.Database.SqlQueryRaw<int>(sql).AsEnumerable().Single();
            (int, int, int, int, int, int, int) SetSix() => (
                Count("SELECT COUNT(*) AS Value FROM creature WHERE id BETWEEN 760320 AND 760324") +
                    Count("SELECT COUNT(*) AS Value FROM spawnpool WHERE id BETWEEN 760320 AND 760324"),
                Count("SELECT COUNT(*) AS Value FROM npc_package WHERE id BETWEEN 760320 AND 760324") +
                    Count("SELECT COUNT(*) AS Value FROM creature_appearance WHERE id BETWEEN 760320 AND 760324"),
                Count("SELECT COUNT(*) AS Value FROM mission_indicator WHERE mission_id IN (1878, 1879, 1870, 1873)"),
                Count("SELECT COUNT(*) AS Value FROM mission_evidence WHERE mission_id IN (1878, 1879, 1870, 1873)"),
                Count("SELECT COUNT(*) AS Value FROM mission_scene_binding WHERE mission_id IN (1878, 1879, 1870, 1873)"),
                Count("SELECT COUNT(*) AS Value FROM mission_reward_item WHERE mission_id IN (1878, 1879, 1870, 1873)"),
                Count("SELECT COUNT(*) AS Value FROM mission_prerequisite WHERE mission_id IN (1878, 1879, 1870, 1873)"));
            int Kept() => Count("SELECT COUNT(*) AS Value FROM creature WHERE id IN (510188, 760316, 760318)") +
                Count("SELECT COUNT(*) AS Value FROM mission_action WHERE mission_id IN (1876, 1877) AND player_flag_id = 1876001");
            var setSix = new uint[] { 1878, 1879, 1870, 1873 };
            var before = new uint[] { 541, 498, 539, 917, 959, 961, 881, 890, 913, 968, 925, 1028, 1029, 1030, 1874, 1875, 1876, 1877 };
            // 5 creatures and 5 pools; 5 packages, 1 + 6 + 1 + 1 + 1 slots; 1 + 2 + 1 + 5 markers; 12 sources; 4 scenes; 4 x 4
            // items; 3 prerequisites (1875, the flag twice).
            var full = (10, 15, 9, 12, 4, 16, 3);

            Assert.IsTrue(setSix.All(Has));
            Assert.AreEqual(full, SetSix());
            Assert.AreEqual(5, Kept());

            world.GetService<IMigrator>().Migrate(Previous(world, SentryTowerTwo));
            Assert.IsFalse(setSix.Any(Has));
            Assert.AreEqual((0, 0, 0, 0, 0, 0, 0), SetSix());
            Assert.AreEqual(5, Kept(), "Remick, McDonald, Kiang and the flag's actions stay");
            Assert.IsTrue(before.All(Has), "sets 1-5 stay");

            world.GetService<IMigrator>().Migrate(SentryTowerTwo);
            Assert.IsTrue(setSix.All(Has));
            Assert.AreEqual(full, SetSix());
        }

        /// <summary>
        /// Marshes_stalker_woods takes out Missing in Action, Light the Fires, Now Can You Hear Me? and Stop and Smell the Maw
        /// Flowers, Jeska, Dudley, Duvall and the Maw pool; sets 1-6 and the rows they copy or use (Vincent, the Marshes Maw)
        /// stay. Up again, all are back.
        /// </summary>
        [TestMethod]
        public void StalkerWoodsRollsBackWithItsMigration()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var world = harness.World;
            bool Has(uint mission) => world.MissionContentDefinitionEntries.AsNoTracking()
                .Any(entry => entry.MissionId == mission && entry.ContentRevision == MarshesMissionDataV1.Revision);
            int Count(string sql) => world.Database.SqlQueryRaw<int>(sql).AsEnumerable().Single();
            (int, int, int, int, int, int, int) SetSeven() => (
                Count("SELECT COUNT(*) AS Value FROM creature WHERE id BETWEEN 760325 AND 760328") +
                    Count("SELECT COUNT(*) AS Value FROM spawnpool WHERE id BETWEEN 760325 AND 760328"),
                Count("SELECT COUNT(*) AS Value FROM npc_package WHERE id BETWEEN 760325 AND 760328") +
                    Count("SELECT COUNT(*) AS Value FROM creature_appearance WHERE id BETWEEN 760325 AND 760328"),
                Count("SELECT COUNT(*) AS Value FROM mission_indicator WHERE mission_id IN (1863, 1864, 1869, 1871)"),
                Count("SELECT COUNT(*) AS Value FROM mission_evidence WHERE mission_id IN (1863, 1864, 1869, 1871)"),
                Count("SELECT COUNT(*) AS Value FROM mission_scene_binding WHERE mission_id IN (1863, 1864, 1869, 1871)"),
                Count("SELECT COUNT(*) AS Value FROM mission_reward_item WHERE mission_id IN (1863, 1864, 1869, 1871)"),
                Count("SELECT COUNT(*) AS Value FROM mission_prerequisite WHERE mission_id IN (1863, 1864, 1869, 1871)"));
            int Kept() => Count("SELECT COUNT(*) AS Value FROM creature WHERE id IN (510168, 531052)");
            var setSeven = new uint[] { 1863, 1864, 1869, 1871 };
            var before = new uint[] { 541, 498, 539, 917, 959, 961, 881, 890, 913, 968, 925, 1028, 1029, 1030, 1874, 1875, 1876, 1877,
                1878, 1879, 1870, 1873 };
            // 3 creatures and 4 pools; 3 packages, 3 x 6 slots; 1 + 5 + 3 + 1 markers; 12 sources; 4 scenes; 4 x 4 items; no
            // prerequisite.
            var full = (7, 21, 10, 12, 4, 16, 0);

            Assert.IsTrue(setSeven.All(Has));
            Assert.AreEqual(full, SetSeven());
            Assert.AreEqual(2, Kept());

            world.GetService<IMigrator>().Migrate(Previous(world, StalkerWoods));
            Assert.IsFalse(setSeven.Any(Has));
            Assert.AreEqual((0, 0, 0, 0, 0, 0, 0), SetSeven());
            Assert.AreEqual(2, Kept(), "Vincent and the Marshes Maw stay");
            Assert.IsTrue(before.All(Has), "sets 1-6 stay");

            world.GetService<IMigrator>().Migrate(StalkerWoods);
            Assert.IsTrue(setSeven.All(Has));
            Assert.AreEqual(full, SetSeven());
        }

        /// <summary>
        /// Marshes_gun_control takes out the five Gun Control missions, Lou, Masai and the Lightbender with its pool; sets 1-7
        /// and the rows they copy or use (Vincent, Moawi, the Caves of Donn Lightbender, the Marshes Thrax Technician and Mox,
        /// Duvall) stay. Up again, all are back.
        /// </summary>
        [TestMethod]
        public void GunControlRollsBackWithItsMigration()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var world = harness.World;
            bool Has(uint mission) => world.MissionContentDefinitionEntries.AsNoTracking()
                .Any(entry => entry.MissionId == mission && entry.ContentRevision == MarshesMissionDataV1.Revision);
            int Count(string sql) => world.Database.SqlQueryRaw<int>(sql).AsEnumerable().Single();
            const string missions = "(1865, 1866, 1867, 1868, 1904)";
            (int, int, int, int, int, int, int) SetEight() => (
                Count("SELECT COUNT(*) AS Value FROM creature WHERE id BETWEEN 760329 AND 760331") +
                    Count("SELECT COUNT(*) AS Value FROM spawnpool WHERE id BETWEEN 760329 AND 760331"),
                Count("SELECT COUNT(*) AS Value FROM npc_package WHERE id BETWEEN 760329 AND 760331") +
                    Count("SELECT COUNT(*) AS Value FROM creature_appearance WHERE id BETWEEN 760329 AND 760331") +
                    Count("SELECT COUNT(*) AS Value FROM creature_stat WHERE id BETWEEN 760329 AND 760331"),
                Count($"SELECT COUNT(*) AS Value FROM mission_indicator WHERE mission_id IN {missions}"),
                Count($"SELECT COUNT(*) AS Value FROM mission_evidence WHERE mission_id IN {missions}"),
                Count($"SELECT COUNT(*) AS Value FROM mission_scene_binding WHERE mission_id IN {missions}") +
                    Count($"SELECT COUNT(*) AS Value FROM mission_channel_policy WHERE mission_id IN {missions}"),
                Count($"SELECT COUNT(*) AS Value FROM mission_reward_item WHERE mission_id IN {missions}"),
                Count($"SELECT COUNT(*) AS Value FROM mission_prerequisite WHERE mission_id IN {missions}"));
            int Kept() => Count("SELECT COUNT(*) AS Value FROM creature WHERE id IN (510168, 38, 566016, 531047, 540013, 760327)");
            var setEight = new uint[] { 1865, 1866, 1867, 1868, 1904 };
            var before = new uint[] { 541, 498, 539, 917, 959, 961, 881, 890, 913, 968, 925, 1028, 1029, 1030, 1874, 1875, 1876, 1877,
                1878, 1879, 1870, 1873, 1863, 1864, 1869, 1871 };
            // 3 creatures and 3 pools; 2 packages, 6 + 1 slots, the Lightbender's stat; 4 + 2 + 3 + 1 + 1 markers; 15 sources;
            // 3 scenes and 1904's radio policy; 4 x 4 items (1867 none); 1 + 1 + 2 + 2 prerequisites.
            var full = (6, 10, 11, 15, 4, 16, 6);

            Assert.IsTrue(setEight.All(Has));
            Assert.AreEqual(full, SetEight());
            Assert.AreEqual(6, Kept());

            world.GetService<IMigrator>().Migrate(Previous(world, GunControl));
            Assert.IsFalse(setEight.Any(Has));
            Assert.AreEqual((0, 0, 0, 0, 0, 0, 0), SetEight());
            Assert.AreEqual(6, Kept(), "the copied and used rows and Duvall stay");
            Assert.IsTrue(before.All(Has), "sets 1-7 stay");

            world.GetService<IMigrator>().Migrate(GunControl);
            Assert.IsTrue(setEight.All(Has));
            Assert.AreEqual(full, SetEight());
        }

        /// <summary>
        /// Marshes_escorts takes out the four escorts, their creatures and the three lieutenants with their pools, and gives MP
        /// Price his package 386 back; sets 1-8 and the rows they copy or use (the Holy Vengeance warrior, Vincent, Morrison, the
        /// Alia Das Rifleman, Masai, Duvall, DuBois, Elway) stay. Up again, all are back.
        /// </summary>
        [TestMethod]
        public void EscortsRollBackWithTheirMigration()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var world = harness.World;
            bool Has(uint mission) => world.MissionContentDefinitionEntries.AsNoTracking()
                .Any(entry => entry.MissionId == mission && entry.ContentRevision == MarshesMissionDataV1.Revision);
            int Count(string sql) => world.Database.SqlQueryRaw<int>(sql).AsEnumerable().Single();
            const string missions = "(497, 888, 889, 1023)";
            (int, int, int, int, int, int, int) SetNine() => (
                Count("SELECT COUNT(*) AS Value FROM creature WHERE id BETWEEN 760332 AND 760338") +
                    Count("SELECT COUNT(*) AS Value FROM spawnpool WHERE id BETWEEN 760332 AND 760338"),
                Count("SELECT COUNT(*) AS Value FROM npc_package WHERE id BETWEEN 760332 AND 760338") +
                    Count("SELECT COUNT(*) AS Value FROM creature_appearance WHERE id BETWEEN 760332 AND 760338") +
                    Count("SELECT COUNT(*) AS Value FROM creature_stat WHERE id BETWEEN 760332 AND 760338"),
                Count($"SELECT COUNT(*) AS Value FROM mission_indicator WHERE mission_id IN {missions}"),
                Count($"SELECT COUNT(*) AS Value FROM mission_evidence WHERE mission_id IN {missions}"),
                Count($"SELECT COUNT(*) AS Value FROM mission_scene_binding WHERE mission_id IN {missions}"),
                Count($"SELECT COUNT(*) AS Value FROM mission_reward_item WHERE mission_id IN {missions}"),
                Count($"SELECT COUNT(*) AS Value FROM mission_prerequisite WHERE mission_id IN {missions}"));
            int Kept() => Count("SELECT COUNT(*) AS Value FROM creature WHERE id IN (750110, 510168, 510162, 670003, 760330, 760327, 510161, 510169, 510163)");
            uint PricePackage() => world.NpcPackageEntries.AsNoTracking().Single(row => row.Id == 510163).PackageId;
            var setNine = new uint[] { 497, 888, 889, 1023 };
            var before = new uint[] { 541, 498, 539, 917, 959, 961, 881, 890, 913, 968, 925, 1028, 1029, 1030, 1874, 1875, 1876, 1877,
                1878, 1879, 1870, 1873, 1863, 1864, 1869, 1871, 1865, 1866, 1867, 1868, 1904 };
            // 7 creatures and 3 pools; 4 packages, 1 + 6 + 6 + 1 + 3 x 6 slots, 4 stats; 1 + 1 + 9 + 7 markers; 12 sources;
            // 4 scenes; 4 x 4 items; no prerequisite.
            var full = (10, 40, 18, 12, 4, 16, 0);

            Assert.IsTrue(setNine.All(Has));
            Assert.AreEqual(full, SetNine());
            Assert.AreEqual(9, Kept());
            Assert.AreEqual(510163U, PricePackage(), "Price's own package");
            Assert.AreEqual(386U, world.NpcPackageEntries.AsNoTracking().Single(row => row.Id == 760334).PackageId, "Bruncke's");

            world.GetService<IMigrator>().Migrate(Previous(world, Escorts));
            Assert.IsFalse(setNine.Any(Has));
            Assert.AreEqual((0, 0, 0, 0, 0, 0, 0), SetNine());
            Assert.AreEqual(9, Kept(), "the copied and used rows stay");
            Assert.AreEqual(386U, PricePackage(), "Price has 386 again");
            Assert.IsTrue(before.All(Has), "sets 1-8 stay");

            world.GetService<IMigrator>().Migrate(Escorts);
            Assert.IsTrue(setNine.All(Has));
            Assert.AreEqual(full, SetNine());
            Assert.AreEqual(510163U, PricePackage());
        }

        /// <summary>
        /// Marshes_retread_city takes out its four missions, Figgins and Holloway with their pools, the mech, Samuel's stand-in
        /// and the Retread; sets 1-9 and the rows they copy or use (Morrison, the Pools Holloway, Retread Samuel and his pool,
        /// the Pools Mech Defender, Vincent, Vesh, Koorva, the Marshes Thrax) stay. Up again, all are back.
        /// </summary>
        [TestMethod]
        public void RetreadCityRollsBackWithItsMigration()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var world = harness.World;
            bool Has(uint mission) => world.MissionContentDefinitionEntries.AsNoTracking()
                .Any(entry => entry.MissionId == mission && entry.ContentRevision == MarshesMissionDataV1.Revision);
            int Count(string sql) => world.Database.SqlQueryRaw<int>(sql).AsEnumerable().Single();
            const string missions = "(914, 916, 1033, 1052)";
            (int, int, int, int, int, int, int) SetTen() => (
                Count("SELECT COUNT(*) AS Value FROM creature WHERE id BETWEEN 760339 AND 760343") +
                    Count("SELECT COUNT(*) AS Value FROM spawnpool WHERE id BETWEEN 760339 AND 760343"),
                Count("SELECT COUNT(*) AS Value FROM npc_package WHERE id BETWEEN 760339 AND 760343") +
                    Count("SELECT COUNT(*) AS Value FROM creature_appearance WHERE id BETWEEN 760339 AND 760343") +
                    Count("SELECT COUNT(*) AS Value FROM creature_stat WHERE id BETWEEN 760339 AND 760343"),
                Count($"SELECT COUNT(*) AS Value FROM mission_indicator WHERE mission_id IN {missions}"),
                Count($"SELECT COUNT(*) AS Value FROM mission_evidence WHERE mission_id IN {missions}"),
                Count($"SELECT COUNT(*) AS Value FROM mission_scene_binding WHERE mission_id IN {missions}"),
                Count($"SELECT COUNT(*) AS Value FROM mission_reward_item WHERE mission_id IN {missions}"),
                Count($"SELECT COUNT(*) AS Value FROM mission_prerequisite WHERE mission_id IN {missions}"));
            int Kept() => Count("SELECT COUNT(*) AS Value FROM creature WHERE id IN (510162, 510199, 510167, 680001, 510168, 520035, 520033, 531045, 531046)") +
                Count("SELECT COUNT(*) AS Value FROM spawnpool WHERE id = 510167");
            var setTen = new uint[] { 914, 916, 1033, 1052 };
            var setNine = new uint[] { 497, 888, 889, 1023 };
            // 5 creatures and 2 pools; 3 packages, 4 x 6 slots, 3 stats; 4 + 3 + 3 + 2 markers; 12 sources; 4 scenes; 4 x 4
            // items; 916's and 1033's two each (914 done, its flag).
            var full = (7, 30, 12, 12, 4, 16, 4);

            Assert.IsTrue(setTen.All(Has));
            Assert.AreEqual(full, SetTen());
            Assert.AreEqual(10, Kept());

            world.GetService<IMigrator>().Migrate(Previous(world, RetreadCity));
            Assert.IsFalse(setTen.Any(Has));
            Assert.AreEqual((0, 0, 0, 0, 0, 0, 0), SetTen());
            Assert.AreEqual(10, Kept(), "the copied and used rows stay");
            Assert.IsTrue(setNine.All(Has), "set 9 stays");

            world.GetService<IMigrator>().Migrate(RetreadCity);
            Assert.IsTrue(setTen.All(Has));
            Assert.AreEqual(full, SetTen());
        }

        /// <summary>
        /// Marshes_retread_arena takes out its four missions, Karl and Foster with their pools, the five fighters and the
        /// Machina with its pool; sets 1-10 and the rows they copy or use (Vincent, the Alia Das Rifleman, the Caves Machina,
        /// the Stalker and Caretaker rows) stay. Up again, all are back.
        /// </summary>
        [TestMethod]
        public void RetreadArenaRollsBackWithItsMigration()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var world = harness.World;
            bool Has(uint mission) => world.MissionContentDefinitionEntries.AsNoTracking()
                .Any(entry => entry.MissionId == mission && entry.ContentRevision == MarshesMissionDataV1.Revision);
            int Count(string sql) => world.Database.SqlQueryRaw<int>(sql).AsEnumerable().Single();
            const string missions = "(1085, 1087, 1088, 1084)";
            (int, int, int, int, int, int, int) SetEleven() => (
                Count("SELECT COUNT(*) AS Value FROM creature WHERE id BETWEEN 760344 AND 760351") +
                    Count("SELECT COUNT(*) AS Value FROM spawnpool WHERE id BETWEEN 760344 AND 760351"),
                Count("SELECT COUNT(*) AS Value FROM npc_package WHERE id BETWEEN 760344 AND 760351") +
                    Count("SELECT COUNT(*) AS Value FROM creature_appearance WHERE id BETWEEN 760344 AND 760351") +
                    Count("SELECT COUNT(*) AS Value FROM creature_stat WHERE id BETWEEN 760344 AND 760351"),
                Count($"SELECT COUNT(*) AS Value FROM mission_indicator WHERE mission_id IN {missions}"),
                Count($"SELECT COUNT(*) AS Value FROM mission_evidence WHERE mission_id IN {missions}"),
                Count($"SELECT COUNT(*) AS Value FROM mission_scene_binding WHERE mission_id IN {missions}"),
                Count($"SELECT COUNT(*) AS Value FROM mission_reward_item WHERE mission_id IN {missions}"),
                Count($"SELECT COUNT(*) AS Value FROM mission_prerequisite WHERE mission_id IN {missions}"));
            int Kept() => Count("SELECT COUNT(*) AS Value FROM creature WHERE id IN (510168, 670003, 566012, 540002, 531048)");
            (int, int) Pads() => (Count("SELECT COUNT(*) AS Value FROM teleporter WHERE id IN (905, 906)"),
                Count("SELECT COUNT(*) AS Value FROM local_teleporter_link WHERE from_teleporter_id IN (905, 906) OR to_teleporter_id IN (905, 906)"));
            var setEleven = new uint[] { 1085, 1087, 1088, 1084 };
            var setTen = new uint[] { 914, 916, 1033, 1052 };
            // 8 creatures and 3 pools; 2 packages, 2 x 6 + 5 slots, 6 stats; 2 + 2 + 3 + 5 markers; 12 sources; 4 scenes; 4 x 4
            // items; 1087's, 1088's and 1084's one each.
            var full = (11, 25, 12, 12, 4, 16, 3);

            Assert.IsTrue(setEleven.All(Has));
            Assert.AreEqual(full, SetEleven());
            Assert.AreEqual((2, 1), Pads(), "the dome's pad, the exit's and the way out");
            Assert.AreEqual(5, Kept());

            world.GetService<IMigrator>().Migrate(Previous(world, RetreadArena));
            Assert.IsFalse(setEleven.Any(Has));
            Assert.AreEqual((0, 0, 0, 0, 0, 0, 0), SetEleven());
            Assert.AreEqual((0, 0), Pads());
            Assert.AreEqual(5, Kept(), "the copied and used rows stay");
            Assert.IsTrue(setTen.All(Has), "set 10 stays");

            world.GetService<IMigrator>().Migrate(RetreadArena);
            Assert.IsTrue(setEleven.All(Has));
            Assert.AreEqual(full, SetEleven());
            Assert.AreEqual((2, 1), Pads());
        }
    }
}
