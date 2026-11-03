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
            MarshesCoreOfTheMatterV2.MissionId, MarshesThisCallsForDestructionV1.MissionId, MarshesDataDisasterV1.MissionId,
            MarshesNeedALittleHelpHereV1.MissionId, MarshesMissingInActionV1.MissionId, MarshesLightTheFiresV1.MissionId,
            MarshesNowCanYouHearMeV1.MissionId, MarshesMawFlowersV1.MissionId, MarshesGunControlV1.MissionId,
            MarshesGunControlPartTwoV1.MissionId, MarshesGunControlPartThreeV1.MissionId, MarshesGunControlPartFourV1.LouMissionId,
            MarshesGunControlPartFourV1.DuvallMissionId, MarshesForeanFluV1.MissionId, MarshesTreadLightlyV1.MissionId,
            MarshesAwolAndyV1.MissionId, MarshesRestAndRecreationV1.MissionId, MarshesHighwayRobberyV1.MissionId,
            MarshesProductionDestructionV1.VincentMissionId, MarshesProductionDestructionV1.HollowayMissionId, MarshesTrappedV1.MissionId,
            MarshesRetreadArenaV1.BeatStreet, MarshesRetreadArenaV1.Smackdown, MarshesRetreadArenaV1.LegendOrFall,
            MarshesCallingAllPartsV1.MissionId, MarshesBringDownTheKaelV1.MissionId, MarshesBringingTheSpiritHomeV1.MissionId,
            MarshesHeadEastV2.MissionId, MarshesSoyuzSalvageV1.MissionId, MarshesTasteOfTheirOwnMedicineV1.MissionId,
            MarshesFreedomsJustAnotherWordV2.MissionId, MarshesLeggoMyLogosV1.MissionId, MarshesSlayOnTargetV2.MissionId,
            MarshesSubterraneanChamberV1.MissionId, MarshesTheLostOnesV1.MissionId, MarshesNeedleInABaneCampV2.MissionId,
            MarshesTheChoicesWeMakeV2.MissionId, MarshesDirtySecretsV1.MissionId, MarshesNeverSurrenderV1.MissionId,
            MarshesPickingUpThePiecesV1.MissionId, MarshesCleaningHouseV3.MissionId, MarshesSurvivorsV2.MissionId,
            MarshesBeinvenidosDelVoroV1.MissionId, MarshesInsertionPointV1.MissionId
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
        /// pool. Bring Down the Kael's three along the Long Walk; Bringing the Spirit Home's three, on the two new Machina pools
        /// (2) and the client's "Forean Spirit Altar" over the braziers (1); Head East, Young Man's three, the client's "Guard
        /// Station" at Shaw (1), round the station (2) and at Meeko (3); Soyuz Salvage: Marshes' five, one at each debris piece.
        /// Set 13 (the Logos Research Facility, map 1700, and the Eloh chamber): A Taste of Their Own Medicine's four, at Hax
        /// (5, in the facility and at its door on the Marshes), round the Security Checkpoint (1) and at Hax in it (2); Freedom's
        /// Just Another Word's four, at the Commander (1) and the client's "Cellblock A/B/C" (2, 4, 5); Leggo My Logos' nine, one
        /// at each objective's object or NPC (the escape's two round the entrance hall); Slay on Target's eight, at each boss (1-3),
        /// round each boss room (4), at the dispenser (5) and the teleporter (6); To a Subterranean Chamber's two, the client's
        /// "Eloh Chamber" at Jumna and one at the lake's teleporter.
        /// Set 14 (the Village Ruins, map 1743): The Lost Ones' three, at the Marshes door and the travel point (2) and the spike
        /// spot (1); Needle in a Bane Camp's two, at the Marshes door and the client's "Undefended Village" at the elder; The
        /// Choices We Make's four, at Myrquar, the Logos, the client's "Bane Equipment" round the control room and Toida on the
        /// Marshes; Dirty Secrets' three, the same three but Myrquar's.
        /// Set 15 (the Village Ruins): Never Surrender's two, round P'reo Das and at Kirii; Picking Up the Pieces' three, round
        /// the fallen of P'reo Das and of Ra'a Das and at Rakla; Cleaning House's fifteen, round the pumps (1), at each pump
        /// (8-11), at each camp (6), at each prisoner and survivor (2, 3, 13; 14-16) and at Tola (12).
        /// Set 16 (the Bane Supply Depot): Survivors' six, at the depot's door on the Marshes and round the camp (1), round the
        /// camp (5, the fight), at the wreck's two crates and at Ortega (3); Beinvenidos del Voro's five, the client's "Bane Terminal" at the first console, the other two
        /// consoles, the door and Fitz; Insertion Point's eighteen, the door and the front (4), the door and the lower entrance
        /// (3), the client's "Bane Terminal" at the terminal and the depot's thirteen mortars (6).
        /// </summary>
        internal const int Indicators = 227;

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
        private const string EastAndNorth = "20271104001100_Marshes_east_and_north";
        private const string LogosResearch = "20271104001200_Marshes_logos_research";
        private const string PreoDas = "20271104001300_Marshes_preo_das";
        private const string PreoDasVillage = "20271104001400_Marshes_preo_das_village";
        private const string BaneSupplyDepot = "20271104001500_Marshes_bane_supply_depot";
        private const string RewardBlocks = "20271104001600_Marshes_reward_blocks";
        private const string SafeGround = "20271104001700_Marshes_safe_ground";
        private const string TrappedApproach = "20271104001800_Marshes_trapped_approach";
        private const string PlaytestFixes = "20271104001900_Marshes_playtest_fixes";
        private const string CleaningHouseV2 = "20271104002000_Marshes_cleaning_house_v2";
        private const string CoreOfTheMatterV2 = "20271104002100_Marshes_core_of_the_matter_v2";
        private const string NpcsStandStill = "20271104002200_Marshes_npcs_stand_still";
        private const string CleaningHouseV3 = "20271104002300_Marshes_cleaning_house_v3";
        private const string QuestNpcsHold = "20271104002400_Marshes_quest_npcs_hold";
        private const string FoodChainTroughs = "20271104002500_Marshes_food_chain_troughs";
        private const string HarvestersDryGround = "20271104002600_Marshes_harvesters_dry_ground";

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
                (1085, 760344, 760344, 35), (1087, 760344, 760344, 35), (1088, 760344, 760344, 35), (1084, 510168, 510168, 33),
                (542, 510160, 510160, 33), (864, 760352, 760352, 36), (920, 760339, 760361, 33), (2009, 510171, 510171, 37),
                (546, 510170, 760362, 33), (548, 760362, 760363, 35), (550, 760363, 510170, 35), (718, 760362, null, 35),
                (1674, 510168, 760372, 38),
                (1076, 510162, null, 33), (893, null, 760374, 35), (1078, null, 510160, 33), (1077, 760373, 510160, 35),
                (891, 760376, 760377, 35), (892, 760378, 760378, 35), (894, 760377, 760376, 35),
                (664, null, 760382, 35), (919, null, 510170, 35), (663, 510160, null, 37)
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
                // 1052's 3 and 920's 4: server-only hidden approaches (to-fix 10), each with its 1's texts.
                (1052, marshes, 5912, new (uint, uint, uint, uint?)[] { (1, 6082, 7014, null), (2, 6083, 7015, null), (3, 6082, 7014, null) }),
                // The arena's 1: counter 0 and 1 the client's two fighters (1085, 1087); 1088's 6201 ("Baker") is not bound.
                (1085, marshes, 6152, new (uint, uint, uint, uint?)[] { (1, 6156, 7196, 6157), (2, 6163, 7195, null) }),
                (1087, marshes, 6170, new (uint, uint, uint, uint?)[] { (1, 6176, 7273, 6179), (2, 6174, 7272, null) }),
                (1088, marshes, 6188, new (uint, uint, uint, uint?)[] { (1, 6198, 7175, 6199), (2, 6192, 7174, null), (3, 7274, 7275, null) }),
                (1084, marshes, 6145, new (uint, uint, uint, uint?)[] { (1, 6149, 8455, null), (2, 6150, 8456, null), (3, 6151, 8457, null) }),
                (542, marshes, 1458, new (uint, uint, uint, uint?)[] { (1, 1528, 7555, 1529) }),
                (864, marshes, 4510, new (uint, uint, uint, uint?)[] { (1, 4532, 9372, 4533), (2, 4731, 9371, null) }),
                (920, marshes, 4911, new (uint, uint, uint, uint?)[] { (1, 5041, 7312, null), (2, 5043, 7313, null), (3, 5044, 7314, null),
                    (4, 5041, 7312, null) }),
                (2009, marshes, 21589, new (uint, uint, uint, uint?)[] { (1, 21608, 21609, null) }),
                (546, marshes, 1474, new (uint, uint, uint, uint?)[] { (5, 6244, 7156, null), (1, 3423, 7157, null),
                    (2, 4706, 7158, null), (3, 4707, 7159, null), (4, 4708, 7160, null) }),
                (548, marshes, 1482, new (uint, uint, uint, uint?)[] { (1, 3422, 7299, null), (2, 3431, 7300, null),
                    (4, 4710, 7301, null), (5, 4711, 7302, null) }),
                (550, marshes, 1490, new (uint, uint, uint, uint?)[] { (3, 9009, 9010, null), (1, 3424, 7305, 6255),
                    (5, 9044, 9045, null), (9, 10569, 10570, null), (2, 6256, 7306, null), (4, 9011, 9012, null),
                    (6, 9049, 9050, null), (8, 9292, 9293, null), (7, 9053, 9054, null), (10, 17329, 17330, null) }),
                // 718's 3: the client's counter1 9008 ("Boss Hit") is not bound (the alarm is hidden 7's); hidden 8 (server only,
                // to-fix 9: Mesgel dead before the alarm) borrows hidden 7's texts.
                (718, marshes, 3427, new (uint, uint, uint, uint?)[] { (1, 3432, 7308, 3434), (2, 3433, 7309, 3435),
                    (3, 4715, 7310, null), (4, 8980, 8981, 8982), (5, 8983, 8984, null), (6, 8987, 8988, null), (7, 8985, 8986, null),
                    (8, 8985, 8986, null) }),
                (1674, marshes, 16109, new (uint, uint, uint, uint?)[] { (1, 16119, 16120, null) }),
                (1076, marshes, 6069, new (uint, uint, uint, uint?)[] { (2, 8349, 8350, null), (1, 6081, 7277, null) }),
                (893, marshes, 4686, new (uint, uint, uint, uint?)[] { (1, 4696, 7286, null) }),
                // 1078's and 1077's hidden lab tallies (5, 4) carry their lab objective's texts.
                (1078, marshes, 6077, new (uint, uint, uint, uint?)[] { (1, 6136, 7279, null), (2, 6137, 7280, null),
                    (3, 17190, 17191, null), (4, 17192, 17193, null), (5, 17190, 17191, null) }),
                (1077, marshes, 6073, new (uint, uint, uint, uint?)[] { (1, 6135, 7289, null), (2, 17185, 17186, null),
                    (3, 17187, 17188, null), (4, 17185, 17186, null) }),
                // Set 15: 891's hidden arrival and kill tally (2, 3) and 892's hidden tally (2) carry objective 1's texts.
                (891, marshes, 4676, new (uint, uint, uint, uint?)[] { (1, 4694, 7284, null), (2, 4694, 7284, null), (3, 4694, 7284, null) }),
                (892, marshes, 4680, new (uint, uint, uint, uint?)[] { (1, 4695, 7282, null), (2, 4695, 7282, null) }),
                (894, marshes, 4690, new (uint, uint, uint, uint?)[] { (1, 6127, 7291, 6128), (8, 17144, 17145, null),
                    (9, 17146, 17147, null), (10, 17148, 17149, null), (11, 17150, 17151, null), (6, 6186, 7292, 6187),
                    (2, 6129, 7293, null), (3, 6131, 7294, null), (13, 17152, 17153, null), (14, 17168, 17169, null),
                    (15, 17171, 17172, null), (16, 17174, 17175, null), (12, 17155, 17156, null) }),
                // Set 16: 664's 5 the client's kill objective with its counter 6254 "Bane Remaining" (to-fix 5), its hidden crate
                // tally (4) 3's texts, 919's tally (2) 1's; 663's hidden
                // way in (5) the client's 7501/7502 "Get inside" (missionobjective 663,5), 6's counter 17360.
                (664, marshes, 2647, new (uint, uint, uint, uint?)[] { (1, 2651, 7243, null), (5, 6253, 7244, 6254),
                    (3, 2655, 7245, null), (4, 2655, 7245, null) }),
                (919, marshes, 4907, new (uint, uint, uint, uint?)[] { (1, 6237, 7097, null), (2, 6237, 7097, null) }),
                (663, marshes, 2642, new (uint, uint, uint, uint?)[] { (4, 6247, 6999, null), (3, 6248, 7000, null),
                    (5, 7501, 7502, null), (6, 17358, 17359, 17360) })
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
            // Set 12: 920's at Shaw's (objectiveconversation 920,1 package 1012) and Meeko's (920,3 package 1013); 542, 864 and
            // 2009 have none.
            CollectionAssert.AreEqual(new (uint, uint?)[] { (1, 1012), (3, 1013) }, Talks(920, marshes));
            foreach (var mission in new uint[] { 542, 864, 2009 })
                Assert.IsEmpty(Talks(mission, marshes), $"{mission}");
            // Set 13: 546's at Hax's (objectiveconversation 546,5 package 960), 550's at Russell's (550,7 package 284) and
            // Brock's (550,10 package 2203; its second way is 7 done), 1674's at Jumna's (1674,1 package 2036); 548 and 718 have none.
            CollectionAssert.AreEqual(new (uint, uint?)[] { (5, 960) }, Talks(546, marshes));
            CollectionAssert.AreEqual(new (uint, uint?)[] { (7, 284), (10, 2203) }, Talks(550, marshes));
            CollectionAssert.AreEqual(new (uint, uint?)[] { (1, 2036) }, Talks(1674, marshes));
            // Set 14: 893's at the elder's (893,1 package 958), 1078's and 1077's at Toida's (1078,4 and 1077,3 package 288);
            // one each, flag 1 (the fork between 893 and 1078 is a scene signal, not a second talk); 1076 has none.
            CollectionAssert.AreEqual(new (uint, uint?)[] { (1, 958) }, Talks(893, marshes));
            CollectionAssert.AreEqual(new (uint, uint?)[] { (4, 288) }, Talks(1078, marshes));
            CollectionAssert.AreEqual(new (uint, uint?)[] { (3, 288) }, Talks(1077, marshes));
            Assert.IsEmpty(Talks(1076, marshes), "1076");
            // Set 15: 894's six at the prisoners' client packages (894,2 and 14 package 1158, 3 and 15 1159, 13 and 16 1160);
            // 891 and 892 have none.
            CollectionAssert.AreEqual(new (uint, uint?)[] { (2, 1158), (3, 1159), (13, 1160), (14, 1158), (15, 1159), (16, 1160) },
                Talks(894, marshes));
            Assert.IsEmpty(Talks(891, marshes), "891");
            Assert.IsEmpty(Talks(892, marshes), "892");
            // Set 16: none (664's 5 is the fight, to-fix 5; the client's 664,5 and 664,3 talks at package 542 are not used).
            Assert.IsEmpty(Talks(664, marshes), "664");
            Assert.IsEmpty(Talks(919, marshes), "919");
            Assert.IsEmpty(Talks(663, marshes), "663");
            foreach (var mission in new uint[] { 548, 718 })
                Assert.IsEmpty(Talks(mission, marshes), $"{mission}");
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
        /// The arena rounds follow each other, Beat Street needs none; Calling all Parts follows Legend or Fall. Set 12 needs
        /// none. Freedom's Just Another Word and Slay on Target follow A Taste of Their Own Medicine, Leggo My Logos follows
        /// Freedom's Just Another Word, To a Subterranean Chamber Leggo My Logos (TaRapedia). Needle in a Bane Camp and The Choices We
        /// Make follow The Lost Ones (TaRapedia); Dirty Secrets needs none. Cleaning House follows Never Surrender (TaRapedia); Never
        /// Surrender and Picking Up the Pieces need none. None repeats.
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
                    (1087U, (uint?)1085), (1088U, (uint?)1087), (1084U, (uint?)1088),
                    (548U, (uint?)546), (718U, (uint?)546), (550U, (uint?)548), (1674U, (uint?)550),
                    (893U, (uint?)1076), (1078U, (uint?)1076), (894U, (uint?)891)
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
                .Where(entry => entry.ContentRevision == MarshesMissionDataV1.Revision && entry.Kind == MissionPrerequisiteKind.PlayerFlagValue)
                .AsEnumerable().Select(entry => (entry.MissionId, entry.PlayerFlagId, entry.PlayerFlagValue)).ToArray());
            // And one level: Insertion Point from 37 (TaRapedia).
            CollectionAssert.AreEquivalent(new[] { (663U, MissionPrerequisiteKind.PlayerLevelAtLeast, (uint?)37) },
                harness.World.MissionPrerequisiteEntries.AsNoTracking()
                .Where(entry => entry.ContentRevision == MarshesMissionDataV1.Revision && entry.Kind != MissionPrerequisiteKind.MissionCompleted &&
                    entry.Kind != MissionPrerequisiteKind.PlayerFlagValue)
                .AsEnumerable().Select(entry => (entry.MissionId, entry.Kind, entry.RequiredLevel)).ToArray());
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
                // Sets 1-4: the client's per-mission blocks where one exists (Marshes_reward_blocks, MarshesRewardBlocksV1).
                (541, marshes, 75000, 5700, new (uint, uint)[] { (120105, 1), (120106, 1), (120107, 1), (120108, 1) }),
                (498, marshes, 65000, 5400, new (uint, uint)[] { (120041, 1), (120042, 1), (120043, 1), (120044, 1) }),
                (539, marshes, 65000, 5400, new (uint, uint)[] { (120133, 1), (120134, 1), (120135, 1), (120136, 1) }),
                (917, marshes, 65000, 5400, new (uint, uint)[] { (120049, 1), (120050, 1), (120051, 1), (120052, 1) }),
                // Armor Module: Resist Electric, Fire, Ice, Virulent [4].
                (959, marshes, 65000, 5400, new (uint, uint)[] { (123016, 1), (123026, 1), (123031, 1), (123071, 1) }),
                // 2 Class VII EMP Bomb, 2 Class VII Adrenaline Booster, 4 Class VII Fragmentation, 4 Class VII Cryogenic Grenade.
                (961, marshes, 32500, 3600, new (uint, uint)[] { (111232, 2), (118814, 2), (45446, 4), (111048, 4) }),
                // Hazmat, Mech, Motor Assist, Reflective vests.
                (1673, PlateauMissionDataV1.Revision, 75000, 3800, new (uint, uint)[] { (120101, 1), (120102, 1), (120103, 1), (120104, 1) }),
                // Bio vest, Stealth boots, Hazmat legs, Reflective gloves (30-34 / 29-33).
                (881, marshes, 65000, 5400, new (uint, uint)[] { (19246, 1), (38783, 1), (20762, 1), (35700, 1) }),
                // Bio boots, Graviton boots, Mech helmet, Stealth vest (35-39).
                (890, marshes, 65000, 5400, new (uint, uint)[] { (18750, 1), (23549, 1), (8855, 1), (10637, 1) }),
                (913, marshes, 140000, 0, new (uint, uint)[] { (18750, 1), (23549, 1), (8855, 1), (10637, 1) }),
                // The client's block: Electric blade 36-40, Area healing disc 40-44, Fire staff 40-44, Physical shotgun 37-41.
                (968, marshes, 160000, 7800, new (uint, uint)[] { (120033, 1), (120034, 1), (120035, 1), (120036, 1) }),
                // 2 Class VII Advanced Med Pack, 2 Power Charger, 4 Fragmentation, 4 Cryogenic Grenade.
                (925, marshes, 35000, 3700, new (uint, uint)[] { (45062, 2), (118907, 2), (45446, 4), (111048, 4) }),
                // 2 Class VII Advanced Med Pack, 2 Armor Charger, 4 Concussion, 4 Fragmentation Grenade.
                (1028, marshes, 35500, 3700, new (uint, uint)[] { (45062, 2), (118898, 2), (111018, 4), (45446, 4) }),
                // The client's block: Incendiary Injector Gun 37-41, Electric Chaingun 38-42, Pulse Pistol 35-39, Pulse Cannon 34-38.
                (1029, marshes, 32500, 5550, new (uint, uint)[] { (120089, 1), (120090, 1), (120091, 1), (120092, 1) }),
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
                (1084, marshes, 70000, 5550, new (uint, uint)[] { (15654, 1), (49164, 1), (14734, 1), (48256, 1) }),
                // Graviton vest 30-34, Reflective vest 29-33, Bio boots 30-34, Motor Assist vest 33-37 (no client block).
                (542, marshes, 150000, 7600, new (uint, uint)[] { (24046, 1), (36148, 1), (18730, 1), (29227, 1) }),
                // The client's block: Hazmat legs 39-43, Motor Assist legs 38-42, Reflective legs 39-43, Stealth legs 35-39.
                (864, marshes, 75000, 5700, new (uint, uint)[] { (120045, 1), (120046, 1), (120047, 1), (120048, 1) }),
                // The client's block: Bio helmet 35-39, Hazmat legs 34-38, Motor Assist legs 38-42, Reflective legs 34-38.
                (920, marshes, 70000, 5550, new (uint, uint)[] { (120069, 1), (120070, 1), (120071, 1), (120072, 1) }),
                // Set 13. The client's blocks (ESTIMATE which): Bio gloves, Graviton helmet, Mech gloves, Stealth gloves 35;
                // Virulent Disperser, Pulse Staff, Cryogenic Net Gun, Leech Gun.
                (546, marshes, 70000, 5550, new (uint, uint)[] { (120017, 1), (120018, 1), (120019, 1), (120020, 1) }),
                (548, marshes, 75000, 5700, new (uint, uint)[] { (96268, 1), (96269, 1), (96270, 1), (96271, 1) }),
                // Sonic Torqueshell Rifle 33-37, Cryogenic Blade 31-35, Pulse Cannon 34-38, Virulent Leech Gun 34-38 (no client
                // block; XP 80000 ESTIMATE).
                (550, marshes, 80000, 7600, new (uint, uint)[] { (49652, 1), (47648, 1), (15437, 1), (15295, 1) }),
                // Class VII EMP Bomb, Cryogenic Grenade, Standard Med Pack (stacks ESTIMATE).
                (718, marshes, 75000, 5700, new (uint, uint)[] { (111232, 2), (111048, 4), (45061, 2) }),
                // Class VII Advanced Med Pack, Armor Charger, EMP Grenade, Concussion Grenade.
                (1674, marshes, 40000, 3900, new (uint, uint)[] { (45062, 2), (118898, 2), (111028, 4), (111018, 4) }),
                // Set 14. 3 Class VII Advanced Med Pack, 3 Armor Charger, 6 Cryogenic Grenade, 6 Incendiary Grenade.
                (1076, marshes, 30000, 3500, new (uint, uint)[] { (45062, 3), (118898, 3), (111048, 6), (111038, 6) }),
                // 3 Class VII EMP Bomb, 3 Power Charger, 6 Fragmentation Grenade (XP 30000 ESTIMATE).
                (893, marshes, 30000, 3600, new (uint, uint)[] { (111232, 3), (118907, 3), (45446, 6) }),
                // Electric Torqueshell Rifle 33-37, Photonic Blade 31-35, Pulse Cannon 29-33, Cryogenic Leech Gun 29-33 (XP 65000
                // ESTIMATE).
                (1078, marshes, 65000, 7000, new (uint, uint)[] { (49572, 1), (47668, 1), (17681, 1), (47764, 1) }),
                // No item (XP 35000 ESTIMATE).
                (1077, marshes, 35000, 1750, new (uint, uint)[0]),
                // Set 15. 3 Class VII Standard Med Pack, 2 Advanced Med Pack, 3 Incendiary Grenade, 3 EMP Grenade.
                (891, marshes, 130000, 7000, new (uint, uint)[] { (45061, 3), (45062, 2), (111038, 3), (111028, 3) }),
                // The client's block 96333-96336: Propellant Gun Fire, Staff Ice, Net Gun Ice, Leech Gun Virulent (Rare).
                (892, marshes, 60000, 5250, new (uint, uint)[] { (96333, 1), (96334, 1), (96335, 1), (96336, 1) }),
                // Sonic Torqueshell Rifle 33-37, Laser Pistol 35-39, Electric Chaingun 33-37, Sonic Rocket Launcher 34-38.
                (894, marshes, 130000, 7000, new (uint, uint)[] { (49652, 1), (14487, 1), (15105, 1), (49168, 1) }),
                // Set 16 (TaRapedia's lists). 12 Basic Med Pack, 6 Fragmentation Grenade, 2 EMP Bomb, 9 Cryogenic Grenade.
                (664, marshes, 80000, 6000, new (uint, uint)[] { (45063, 12), (45447, 6), (111233, 2), (111049, 9) }),
                // 6 Advanced Med Pack, 9 EMP Grenade, 3 Res Trauma Kit, 9 Concussion Grenade (all Class VIII).
                (663, marshes, 170000, 6000, new (uint, uint)[] { (45065, 6), (111029, 9), (47600, 3), (111019, 9) }),
                // ESTIMATE templates of TaRapedia's four: Bio Helmet, Stealth Gloves, Graviton Legs, Motor Assist Legs (UNC, L35 bands).
                (919, marshes, 75000, 5750, new (uint, uint)[] { (19008, 1), (38932, 1), (23937, 1), (29073, 1) })
            })
            {
                var reward = harness.World.Set<MissionRewardDefinitionEntry>().AsNoTracking()
                    .Single(entry => entry.MissionId == mission && entry.ContentRevision == revision);
                Assert.AreEqual((experience, credits, (byte)(items.Length > 0 ? 1 : 0)), (reward.Experience, reward.Credits, reward.SelectionCount),
                    $"{mission}");
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
            // 2009: the Soyuz ISS Model Rocket 122834, fixed (the sister missions); XP and credits Starling's 498's.
            var soyuz = harness.World.Set<MissionRewardDefinitionEntry>().AsNoTracking()
                .Single(entry => entry.MissionId == 2009 && entry.ContentRevision == marshes);
            Assert.AreEqual((65000U, 5400U, (byte)0), (soyuz.Experience, soyuz.Credits, soyuz.SelectionCount), "2009");
            CollectionAssert.AreEqual(new[] { (MissionRewardItemKind.Fixed, 122834U, 1U) },
                harness.World.MissionRewardItemEntries.AsNoTracking().Where(entry => entry.MissionId == 2009 && entry.ContentRevision == marshes)
                    .AsEnumerable().Select(entry => (entry.Kind, entry.ItemTemplateId, entry.Quantity)).ToArray(), "2009 items");
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
                // Set 13's facility missions are on the Logos Research Facility (map 1700), 546's door marker on the Marshes.
                var facility = row.MissionId is 546 or 548 or 550 or 718 &&
                    !(row.MissionId == 546 && row.IndicatorId == MissionIndicator.UnnamedFrom + 2);
                // Set 14's on the Village Ruins (map 1743), but the Marshes door's (1076 and 893: unnamed 1) and Toida's (the
                // return: 1078 unnamed 3, 1077 unnamed 2).
                var marshesDoorOrToida = row.MissionId is 1076 or 893 && row.IndicatorId == MissionIndicator.UnnamedFrom + 1 ||
                    row.MissionId == 1078 && row.IndicatorId == MissionIndicator.UnnamedFrom + 3 ||
                    row.MissionId == 1077 && row.IndicatorId == MissionIndicator.UnnamedFrom + 2;
                // Set 15's all on the Village Ruins.
                var ruinsMap = row.MissionId is 1076 or 893 or 1078 or 1077 && !marshesDoorOrToida || row.MissionId is 891 or 892 or 894;
                // Set 16's on the Bane Supply Depot (map 1451), but the Marshes door's (664 unnamed 1, 919 unnamed 3, 663 unnamed 1
                // and 3) and Fitz's (919 unnamed 4).
                var marshesSide = row.MissionId == 664 && row.IndicatorId == MissionIndicator.UnnamedFrom + 1 ||
                    row.MissionId == 919 && row.IndicatorId is MissionIndicator.UnnamedFrom + 3 or MissionIndicator.UnnamedFrom + 4 ||
                    row.MissionId == 663 && row.IndicatorId is MissionIndicator.UnnamedFrom + 1 or MissionIndicator.UnnamedFrom + 3;
                var depot = row.MissionId is 664 or 919 or 663 && !marshesSide;
                Assert.AreEqual(facility ? MarshesLogosResearchNpcsV1.MapContextId : ruinsMap ? MarshesPreoDasNpcsV1.MapContextId :
                    depot ? MarshesBaneSupplyDepotNpcsV1.MapContextId : 1454U,
                    MissionMarkerProjection.ResolveMap(catalog, row.MissionId, row.ObjectiveId, new MissionIndicator { IndicatorId = row.IndicatorId }),
                    $"mission {row.MissionId} objective {row.ObjectiveId}: on its map");
                // 1674's second marker, at the lake's teleporter, has no client name.
                var unnamedSecond = row.MissionId == 1674 && row.IndicatorId == MissionIndicator.UnnamedFrom + 1 ||
                    row.MissionId == 893 && row.IndicatorId == MissionIndicator.UnnamedFrom + 1 ||  // 893's at the Marshes door
                    row.MissionId is 919 or 663 && row.IndicatorId > MissionIndicator.UnnamedFrom;   // set 16's beside the named one
                if (!unnamedSecond && NamedMarker(row.MissionId, row.ObjectiveId) is { } name)
                    Assert.AreEqual(name, row.IndicatorId, $"mission {row.MissionId} objective {row.ObjectiveId}: the client's name");
                else
                    Assert.IsTrue(row.IndicatorId > MissionIndicator.UnnamedFrom,
                        $"mission {row.MissionId} objective {row.ObjectiveId} indicator {row.IndicatorId}");
            }
            foreach (var (mission, objective) in new (uint, uint)[] { (919, 1), (663, 6) })
                Assert.AreEqual(1, indicators.Count(row => row.MissionId == mission && row.ObjectiveId == objective && row.IndicatorId == 71),
                    $"{mission}'s Bane Terminal");
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
            (864, 1) => 141U,                               // Forean Spirit Altar
            (920, 1) => 340U,                               // Guard Station
            (548, 2) => 345U,                               // Cellblock A
            (548, 4) => 346U,                               // Cellblock B
            (548, 5) => 347U,                               // Cellblock C
            (1674, 1) => 384U,                              // Eloh Chamber (the second, unnamed, at the lake's teleporter)
            (893, 1) => 10000003U,                          // Undefended Village (the first, unnamed, at the Marshes door)
            (1078, 3) => 72U,                               // Bane Equipment
            (1077, 2) => 72U,                               // Bane Equipment
            (919, 1) => 71U,                                // Bane Terminal (the first console; the others unnamed)
            (663, 6) => 71U,                                // Bane Terminal (the terminal; the mortars unnamed)
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
            Assert.HasCount(175, objectives, "the objectives are read");
            Assert.IsTrue(hidden.Contains((1078U, 5U)) && hidden.Contains((1077U, 4U)), "1078's and 1077's lab tallies are hidden");
            Assert.IsTrue(hidden.Contains((891U, 2U)) && hidden.Contains((891U, 3U)) && hidden.Contains((892U, 2U)),
                "891's arrival and kill tally and 892's tally are hidden");
            Assert.IsTrue(!hidden.Contains((664U, 5U)) && hidden.Contains((664U, 4U)) && hidden.Contains((919U, 2U)) && hidden.Contains((663U, 5U)),
                "664's kill and crate tallies, 919's tally and 663's way in are hidden");
            Assert.IsTrue(hidden.Contains((546U, 3U)) && hidden.Contains((546U, 4U)), "546's tally and hack are hidden");
            Assert.IsTrue(hidden.Contains((550U, 8U)) && hidden.Contains((718U, 7U)) && hidden.Contains((718U, 8U)),
                "550's failed escape, 718's alarm and its quiet end are hidden");
            Assert.IsTrue(hidden.Contains((881U, 2U)), "881's tally is hidden");
            Assert.IsTrue(hidden.Contains((1052U, 3U)) && hidden.Contains((920U, 4U)), "1052's and 920's approaches are hidden");
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
                (1878, 1, 428, -580, 20),                           // round the three core crates west of the depot door (to-fix 14)
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
                (1052, 1, 188.4, -673.1, 8), (1052, 2, 188.4, -673.1, 30),                     // Samuel, round him (to-fix 15)
                (1085, 2, -219.9, -799.1, 8), (1085, 1, -219.9, -761.1, 25),                   // Foster, the arena
                (1087, 2, -219.9, -799.1, 8), (1087, 1, -219.9, -761.1, 25),
                (1088, 2, -219.9, -799.1, 8), (1088, 1, -219.9, -761.1, 25), (1088, 3, -219.9, -761.1, 25),
                (1084, 1, -664.6, -583, 25), (1084, 2, -318, -570.1, 25), (1084, 2, 180.7, -767.7, 25),   // Stalker 540002, Caretakers
                (1084, 2, 165, -726.7, 25), (1084, 3, -650, -620, 25),                         // 531283, 531307, 531286; the Machina
                (542, 1, 480, 320, 60), (542, 1, 500, 100, 60), (542, 1, 420, -300, 60),      // along the Long Walk
                (864, 2, -250, 200, 25), (864, 2, -100, 150, 25), (864, 1, -208, 28, 35),     // the Machina pools, the braziers
                (920, 1, 90.4, 535, 8), (920, 2, 90.4, 535, 40), (920, 3, 95, 540, 8),        // Shaw, the station, Meeko
                (2009, 1, -560, 350, 40), (2009, 1, -550, -150, 40), (2009, 1, 250, 600, 40), // the debris pieces
                (2009, 1, 200, 150, 40), (2009, 1, 300, -100, 40),
                (546, 5, 814, -110, 8), (546, 5, -318.36, -497.8, 10),                        // Hax in the facility, its door
                (546, 1, 760.2, -136, 25), (546, 2, 748, -130, 12),                           // the Security Checkpoint, Hax in it
                (548, 1, 728.5, -176, 15), (548, 2, 728, -220, 12), (548, 4, 728, -240, 12),   // the Commander, the cell blocks
                (548, 5, 744, -262, 12),                                                       // block C on its terminal (to-fix 7)
                (550, 3, 580, -190, 8), (550, 1, 608, -212, 8), (550, 5, 608, -44, 8),         // the crate, the tanks
                (550, 9, 604, -296, 8), (550, 2, 608, -346, 8),                               // the console, the analyzer
                (550, 4, 868, -120, 12), (550, 6, 868, -120, 12),                             // the entrance hall
                (550, 7, 866, -112, 8), (550, 10, 871, -118, 8),                              // Russell, Brock
                (718, 1, 776, -104, 15), (718, 2, 688, -24, 15), (718, 3, 648, -200, 15),     // the bosses
                (718, 4, 776, -104, 15), (718, 4, 688, -24, 15), (718, 4, 648, -200, 15),     // their rooms
                (718, 5, 632, -200, 8), (718, 6, 608, -104, 8),                               // the dispenser, the teleporter
                (1674, 1, -160, -902, 8), (1674, 1, -160, -868, 6),                           // Jumna, the lake's teleporter
                (1076, 2, 755.08, 727.67, 10), (1076, 2, -107, -355, 15), (1076, 1, 181, -118, 8),   // the door, the travel point, the spot
                (893, 1, 755.08, 727.67, 10), (893, 1, 127, -35, 10),  // the elder moved (to-fix 4, 15)                          // the door, the elder
                (1078, 1, 25, 270, 12), (1078, 2, -33, 390, 8), (1078, 3, -31, 396, 18),       // Myrquar, the Logos, the control room
                (1078, 4, -320.8, -6.8, 8),                                                   // Toida
                (1077, 1, -33, 390, 8), (1077, 2, -31, 396, 18), (1077, 3, -320.8, -6.8, 8),   // the Logos, the control room, Toida
                (891, 1, 100.8, -236.8, 40), (891, 1, 165, -228, 8),                          // P'reo Das, Kirii (to-fix 17)
                (892, 1, 97, -227, 25), (892, 1, 239.2, 0.4, 15), (892, 1, 67, -254, 8),      // the fallen, Rakla
                (894, 1, 25.2, 315.3, 50), (894, 8, 10, 330, 8), (894, 9, 37.2, 330.75, 8),       // the pumps
                (894, 10, -19.49, 300.31, 8), (894, 11, 69.81, 299.78, 8),
                (894, 6, 250, 8, 15), (894, 6, 75, -122, 15), (894, 6, 145, 14, 15),          // the camps
                (894, 2, 245, 0, 8), (894, 14, 256.8, 19.15, 8), (894, 3, 60, -120, 8),       // the prisoners and survivors
                (894, 15, 90, -125, 8), (894, 13, 140, 12, 8), (894, 16, 151.2, 15.55, 8),
                (894, 12, -115.2, -352, 15),                                                  // Tola
                (664, 1, 451.56, -581.94, 10), (664, 1, 129.9, 312.9, 20), (664, 5, 129.9, 312.9, 20), // the door, the camp
                (664, 3, 128, 340, 8), (664, 3, 140, 340, 8), (664, 3, 125, 318, 8),          // the wreck's crates, Ortega
                (919, 1, -80.4, 198.9, 8), (919, 1, 1.23, 92.77, 8), (919, 1, 1.6, 171.9, 8), // the consoles
                (919, 1, 451.56, -581.94, 10), (919, 1, -325.5, -9, 8),                       // the door, Fitz
                (663, 4, 451.56, -581.94, 10), (663, 4, -284, 36, 15),                        // the door, the front
                (663, 3, 451.56, -581.94, 10), (663, 3, 68, 120, 12),                         // the door, the lower entrance
                (663, 6, -88.2, -160.4, 8)                                                    // the terminal
            };
            // 663's thirteen: the depot's Bane Mortar pools (World DB 592144-592156).
            foreach (var (x, _, z) in MarshesInsertionPointV1.MortarsAt)
                expected.Add((663, 6, x, z, 10));
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
            foreach (var at in MarshesHarvesterPlacesV2.Harvesters.Concat(MarshesHarvesterPlacesV2.Taps))
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
            // spike spots, 1871's five flowers; 914's two supply crates, 916's and 1033's five kiosks each; 864's two braziers,
            // 2009's five debris pieces; 548's three cell terminals, 550's crate, two tanks, two charges, console, analyzer and
            // its keyboard, 718's six pieces of lab equipment, dispenser and teleporter; 1076's spike spot, 1078's and 1077's three
            // pieces of lab equipment and toxin crate each; 892's six corpses and knapsacks, 894's four pumps and acceptors; 664's two
            // crates, 919's three consoles, 663's terminal.
            Assert.AreEqual(14 + 14 + 5 + 8 + 1 + 2 + 3 + 5 + 1 + 4 + 1 + 5 + 3 + 5 + 2 + 10 + 2 + 5 + 3 + 8 + 8 + 1 + 4 + 4 + 12 + 8 + 2 + 3 + 1, objects);
        }

        /// <summary>
        /// The client's use argument of each class a Marshes scene lets the player use: usabledata lookup[class][0] of the
        /// 1.16.5.0 client (written out here). The client sends that argument, and the server takes a use only with the
        /// scene's own (SceneApplication.CanUseObject), so a scene object with another one can never be used in the client.
        /// </summary>
        private static readonly Dictionary<uint, uint> ClientUseArguments = new()
        {
            [11096] = 3,    // UsableItemDisp_item_bag_hum_v01 (917 Helka's pack, 892 the fallen's knapsacks)
            [11116] = 3,    // UsableItemDisp_item_crate_bane_v01CONVERT2TD (881 sample taps, 718 DNA dispenser, 1077/1078 toxin crates)
            [7334] = 1,     // UsableItemAccHumBombV01 (913 bomb receptacles, 894 pump acceptors)
            [21824] = 1,    // UsableItemAccForeanTreebackFood (961 feed acceptors)
            [11114] = 3,    // UsableItemDisp_item_canister_hum_v01 (1030 viral scrapper; g_usabledata.dis:19364)
            [22105] = 1,    // UsableItemDispBlackBox (1875 black box; lookup[0] None, the client sends 1)
            [20631] = 1,    // UsableStatelessBaneConsoleKeyboard (1876 Bane computer, 548 cell terminals, 550 console and
                            // keyboard; lookup[0] None, the client sends 1)
            [7468] = 1,     // UsableTwoStateBaneConsoleSwitchV01 (1879 Bane computers; lookup[0] 1)
            [7870] = 1,     // UsableBombHumV01 (1879 generator charge, 550 tank charges; lookup[0] None, the client sends 1)
            [21568] = 3,    // the bomb crate (550; as PlateauBombVoyageV1's crates)
            [6137] = 1,     // UsableTwoStateForeanFirePitV01 (1864 fires; lookup[0] None, the client sends 1)
            [7847] = 3,     // UsableItemAccHumGpsBeacon (1869 spike spots, 1076 signal spike spot; lookup[0] 3)
            [22518] = 3, [22519] = 3, [22520] = 3, [22521] = 3, [22522] = 3,   // UsableItemDispMawflowerV01-V05 (1871 flowers; lookup[0] 3)
            [11078] = 3,    // UsableItemDisp_item_crate_hum_v01 (914 supply crates; lookup[0] 3)
            [7636] = 1,     // UsableTwoStateForeanElohBrazierV01 (864 altars; lookup[0] None, the client sends 1)
            [29783] = 1, [29784] = 1,   // UsableTwoStateHumSoyuzCapsuleDebrisV01/V02 (2009 debris; lookup[0] 1)
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
            // fires, 1869's three spots, 1871's five flowers (1863's body talks); 914's two crates; 864's two braziers, 2009's
            // five debris pieces; 548's three terminals, 550's crate, two charges, console and keyboard, 718's dispenser; 1076's
            // spot, 1078's and 1077's toxin crates; 892's six knapsacks, 894's four acceptors; 664's two crates, 919's three consoles,
            // 663's terminal.
            Assert.AreEqual(1 + 12 + 7 + 8 + 1 + 2 + 3 + 4 + 4 + 5 + 3 + 5 + 2 + 2 + 5 + 3 + 5 + 1 + 1 + 2 + 6 + 4 + 2 + 3 + 1, used);
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
            // fighters (the Rifleman's slot each), the Machina and its pool, and four scenes (1085, 1087, 1088, 1084); set 12
            // Bargas and Shaw (pool, package, Toida's slot and Morrison's six), two Machina pools, the prisoner (no slot) and his
            // guard, Shaw's stand-in (package, Morrison's six), the soldier (the Rifleman's slot), the Stalker, Meeko (package,
            // Taavik's slot), and three scenes (864, 920, 2009); set 13 Hax and Sakai (pool, package, slots), the medic, the
            // prisoner, Russell and Brock (slots; Russell's and Brock's packages), the Commander and the three bosses, Jumna
            // (pool, package), and four scenes (546, 548, 550, 718); set 14 Qunle and the elder (pool, package, Toida's slot),
            // Myrquar, and four scenes (1076, 893, 1078, 1077); set 15 Tola, Kirii and Rakla (pool, package, Veska's, Toida's and
            // Siva's slot), the three prisoners (package), and three scenes (891, 892, 894); set 16 Ortega (pool, package,
            // Morrison's six slots) and three scenes (664, 919, 663); the 894 fix gives the three survivors their own creature
            // and package rows (to-fix 16).
            Assert.AreEqual((75, 51, 50, 203, 3, 4, 55), All(), "541, 968, 1874, 1868, 1904, 542 and 1674 have no scene");
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
            // At the head the quest NPCs have their stat rows too (Marshes_quest_npcs_hold, to-fix 17); up to the set again they have none.
            Assert.AreEqual((6, 11, 4, 12, 4, 17, 3), SetFour());
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
            // At the head Dikah has his stat row too (Marshes_safe_ground, to-fix 4), and McDonald and Kiang theirs (Marshes_quest_npcs_hold, to-fix 17); up to set 5 again he has none.
            Assert.AreEqual((7, 20, 11, 12, 3, 16, 2, 2), SetFive());
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
            // At the head the quest NPCs have their stat rows too (Marshes_quest_npcs_hold, to-fix 17); up to the set again they have none.
            Assert.AreEqual((6, 12, 11, 15, 4, 16, 6), SetEight());
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
            // At the head the quest NPCs have their stat rows too (Marshes_quest_npcs_hold, to-fix 17); up to the set again they have none.
            Assert.AreEqual((10, 43, 18, 12, 4, 16, 0), SetNine());
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
            // At the head the quest NPCs have their stat rows too (Marshes_quest_npcs_hold, to-fix 17); up to the set again they have none.
            Assert.AreEqual((7, 32, 12, 12, 4, 16, 4), SetTen());
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
            // At the head the quest NPCs have their stat rows too (Marshes_quest_npcs_hold, to-fix 17); up to the set again they have none.
            Assert.AreEqual((11, 27, 12, 12, 4, 16, 3), SetEleven());
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

        /// <summary>
        /// Marshes_east_and_north takes out its four missions, Bargas, the Machina pools, the caravan (its squad), Shaw, his
        /// stand-in, the station's soldier, the Stalker and Meeko; the copied rows (Toida, Taavik, Morrison, the Torcastra
        /// prisoner, the Thrax Soldier, the Stalker, the Rifleman), the Machina, Starling and Figgins stay. Up again, all are back.
        /// </summary>
        [TestMethod]
        public void EastAndNorthRollsBackWithItsMigration()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var world = harness.World;
            bool Has(uint mission) => world.MissionContentDefinitionEntries.AsNoTracking()
                .Any(entry => entry.MissionId == mission && entry.ContentRevision == MarshesMissionDataV1.Revision);
            int Count(string sql) => world.Database.SqlQueryRaw<int>(sql).AsEnumerable().Single();
            const string missions = "(542, 864, 920, 2009)";
            (int, int, int, int, int, int, int, int) SetTwelve() => (
                Count("SELECT COUNT(*) AS Value FROM creature WHERE id BETWEEN 760352 AND 760361") +
                    Count("SELECT COUNT(*) AS Value FROM spawnpool WHERE id BETWEEN 760352 AND 760361"),
                Count("SELECT COUNT(*) AS Value FROM npc_package WHERE id BETWEEN 760352 AND 760361") +
                    Count("SELECT COUNT(*) AS Value FROM creature_appearance WHERE id BETWEEN 760352 AND 760361") +
                    Count("SELECT COUNT(*) AS Value FROM creature_stat WHERE id BETWEEN 760352 AND 760361"),
                Count("SELECT COUNT(*) AS Value FROM npc_squad_type WHERE id = 760355") +
                    Count("SELECT COUNT(*) AS Value FROM npc_squad_member WHERE type_id = 760355") +
                    Count("SELECT COUNT(*) AS Value FROM npc_squad_path WHERE id = 760355") +
                    Count("SELECT COUNT(*) AS Value FROM npc_squad_path_point WHERE path_id = 760355") +
                    Count("SELECT COUNT(*) AS Value FROM npc_squad_spawn WHERE id = 760355"),
                Count($"SELECT COUNT(*) AS Value FROM mission_indicator WHERE mission_id IN {missions}"),
                Count($"SELECT COUNT(*) AS Value FROM mission_evidence WHERE mission_id IN {missions}"),
                Count($"SELECT COUNT(*) AS Value FROM mission_scene_binding WHERE mission_id IN {missions}"),
                Count($"SELECT COUNT(*) AS Value FROM mission_reward_item WHERE mission_id IN {missions}"),
                Count($"SELECT COUNT(*) AS Value FROM mission_prerequisite WHERE mission_id IN {missions}"));
            int Kept() => Count("SELECT COUNT(*) AS Value FROM creature WHERE id IN " +
                "(510160, 510164, 510162, 660023, 531045, 540002, 670003, 760351, 510171, 760339)");
            var setTwelve = new uint[] { 542, 864, 920, 2009 };
            var setEleven = new uint[] { 1085, 1087, 1088, 1084 };
            // 8 creatures and 4 pools (Bargas, the two Machina pools, Shaw); 4 packages, Toida's 1 + Morrison's 6 twice + the Rifleman's 1 + Taavik's 1 slots
            // (the Torcastra prisoner 660023 has no creature_appearance row: World DB checked 2026-11-03, so his copy has none), 4
            // stats; the squad type, 2 member rows, the path, 12 points, the spawn; 3 + 3 + 3 + 5 markers; 12 sources; 3 scenes
            // (542 has none); 3 x 4 + 1 items; no requirement.
            var full = (12, 23, 17, 14, 12, 3, 13, 0);

            Assert.IsTrue(setTwelve.All(Has));
            // At the head the quest NPCs have their stat rows too (Marshes_quest_npcs_hold, to-fix 17); up to the set again they have none.
            Assert.AreEqual((12, 25, 17, 14, 12, 3, 13, 0), SetTwelve());
            Assert.AreEqual(10, Kept());

            world.GetService<IMigrator>().Migrate(Previous(world, EastAndNorth));
            Assert.IsFalse(setTwelve.Any(Has));
            Assert.AreEqual((0, 0, 0, 0, 0, 0, 0, 0), SetTwelve());
            Assert.AreEqual(10, Kept(), "the copied and used rows stay");
            Assert.IsTrue(setEleven.All(Has), "set 11 stays");

            world.GetService<IMigrator>().Migrate(EastAndNorth);
            Assert.IsTrue(setTwelve.All(Has));
            Assert.AreEqual(full, SetTwelve());
        }

        /// <summary>
        /// Marshes_logos_research takes out its five missions, Hax, Sakai, the medic, the Commander, the three bosses, the
        /// prisoner, Russell, Brock, Jumna and the lake's two teleporters; the copied rows (Fitz, Bosley, Holloway, Perdu, DuBois,
        /// the Maligo prisoner, Veryin, the Marshes Commander and Caretaker) and Vincent stay. Up again, all are back.
        /// </summary>
        [TestMethod]
        public void LogosResearchRollsBackWithItsMigration()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var world = harness.World;
            bool Has(uint mission) => world.MissionContentDefinitionEntries.AsNoTracking()
                .Any(entry => entry.MissionId == mission && entry.ContentRevision == MarshesMissionDataV1.Revision);
            int Count(string sql) => world.Database.SqlQueryRaw<int>(sql).AsEnumerable().Single();
            const string missions = "(546, 548, 550, 718, 1674)";
            (int, int, int, int, int, int, int, int) SetThirteen() => (
                Count("SELECT COUNT(*) AS Value FROM creature WHERE id BETWEEN 760362 AND 760372") +
                    Count("SELECT COUNT(*) AS Value FROM spawnpool WHERE id BETWEEN 760362 AND 760372"),
                Count("SELECT COUNT(*) AS Value FROM npc_package WHERE id BETWEEN 760362 AND 760372") +
                    Count("SELECT COUNT(*) AS Value FROM creature_appearance WHERE id BETWEEN 760362 AND 760372") +
                    Count("SELECT COUNT(*) AS Value FROM creature_stat WHERE id BETWEEN 760362 AND 760372"),
                Count("SELECT COUNT(*) AS Value FROM teleporter WHERE id IN (907, 908)") +
                    Count("SELECT COUNT(*) AS Value FROM local_teleporter_link WHERE from_teleporter_id IN (907, 908) OR to_teleporter_id IN (907, 908)"),
                Count($"SELECT COUNT(*) AS Value FROM mission_indicator WHERE mission_id IN {missions}"),
                Count($"SELECT COUNT(*) AS Value FROM mission_evidence WHERE mission_id IN {missions}"),
                Count($"SELECT COUNT(*) AS Value FROM mission_scene_binding WHERE mission_id IN {missions}"),
                Count($"SELECT COUNT(*) AS Value FROM mission_reward_item WHERE mission_id IN {missions}"),
                Count($"SELECT COUNT(*) AS Value FROM mission_prerequisite WHERE mission_id IN {missions}"));
            int Kept() => Count("SELECT COUNT(*) AS Value FROM creature WHERE id IN " +
                "(510170, 510176, 510199, 510193, 510161, 750105, 750051, 531049, 531048, 510168)");
            var setThirteen = new uint[] { 546, 548, 550, 718, 1674 };
            var setTwelve = new uint[] { 542, 864, 920, 2009 };
            // 11 creatures and 3 pools (Hax, Sakai, Jumna); 5 packages, 35 appearance slots (Hax, Sakai, the medic, the prisoner,
            // Russell, Brock), 7 stats (Hax, Sakai, the medic, the Commander, the three bosses); 2 teleporters, 2 links; 4 + 4 + 9
            // + 8 + 2 markers; 15 sources; 4 scenes (1674 has none); 4 x 4 + 3 items; 4 requirements.
            var full = (14, 47, 4, 27, 15, 4, 19, 4);

            Assert.IsTrue(setThirteen.All(Has));
            // At the head the quest NPCs have their stat rows too (Marshes_quest_npcs_hold, to-fix 17); up to the set again they have none.
            Assert.AreEqual((14, 48, 4, 27, 15, 4, 19, 4), SetThirteen());
            Assert.AreEqual(10, Kept());

            world.GetService<IMigrator>().Migrate(Previous(world, LogosResearch));
            Assert.IsFalse(setThirteen.Any(Has));
            Assert.AreEqual((0, 0, 0, 0, 0, 0, 0, 0), SetThirteen());
            Assert.AreEqual(10, Kept(), "the copied and used rows stay");
            Assert.IsTrue(setTwelve.All(Has), "set 12 stays");

            world.GetService<IMigrator>().Migrate(LogosResearch);
            Assert.IsTrue(setThirteen.All(Has));
            Assert.AreEqual(full, SetThirteen());
        }

        /// <summary>
        /// Marshes_preo_das takes out its four missions, Qunle, the encampment's elder and Myrquar; the copied rows (Toida, the
        /// Marshes Commander), Morrison and the Thrax Soldier stay. Up again, all are back.
        /// </summary>
        [TestMethod]
        public void PreoDasRollsBackWithItsMigration()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var world = harness.World;
            bool Has(uint mission) => world.MissionContentDefinitionEntries.AsNoTracking()
                .Any(entry => entry.MissionId == mission && entry.ContentRevision == MarshesMissionDataV1.Revision);
            int Count(string sql) => world.Database.SqlQueryRaw<int>(sql).AsEnumerable().Single();
            const string missions = "(1076, 893, 1078, 1077)";
            (int, int, int, int, int, int, int, int) SetFourteen() => (
                Count("SELECT COUNT(*) AS Value FROM creature WHERE id BETWEEN 760373 AND 760375") +
                    Count("SELECT COUNT(*) AS Value FROM spawnpool WHERE id BETWEEN 760373 AND 760375"),
                Count("SELECT COUNT(*) AS Value FROM npc_package WHERE id BETWEEN 760373 AND 760375") +
                    Count("SELECT COUNT(*) AS Value FROM creature_appearance WHERE id BETWEEN 760373 AND 760375") +
                    Count("SELECT COUNT(*) AS Value FROM creature_stat WHERE id BETWEEN 760373 AND 760375"),
                Count($"SELECT COUNT(*) AS Value FROM mission_channel_policy WHERE mission_id IN {missions}"),
                Count($"SELECT COUNT(*) AS Value FROM mission_indicator WHERE mission_id IN {missions}"),
                Count($"SELECT COUNT(*) AS Value FROM mission_evidence WHERE mission_id IN {missions}"),
                Count($"SELECT COUNT(*) AS Value FROM mission_scene_binding WHERE mission_id IN {missions}"),
                Count($"SELECT COUNT(*) AS Value FROM mission_reward_item WHERE mission_id IN {missions}"),
                Count($"SELECT COUNT(*) AS Value FROM mission_prerequisite WHERE mission_id IN {missions}"));
            int Kept() => Count("SELECT COUNT(*) AS Value FROM creature WHERE id IN (510160, 510162, 531049, 531045)");
            var setFourteen = new uint[] { 1076, 893, 1078, 1077 };
            var setThirteen = new uint[] { 546, 548, 550, 718, 1674 };
            // 3 creatures and 2 pools (Qunle, the elder); 2 packages, 2 appearance slots (Toida's one each), 1 stat (Myrquar); 3
            // channel policies (1076 by radio in, 893 and 1078 by radio out); 3 + 2 + 4 + 3 markers; 12 sources; 4 scenes; 4 + 3
            // + 4 items (1077 has none); 2 requirements (893 and 1078 on 1076).
            var full = (5, 5, 3, 12, 12, 4, 11, 2);

            Assert.IsTrue(setFourteen.All(Has));
            // At the head the quest NPCs have their stat rows too (Marshes_quest_npcs_hold, to-fix 17); up to the set again they have none.
            Assert.AreEqual((5, 7, 3, 12, 12, 4, 11, 2), SetFourteen());
            Assert.AreEqual(4, Kept());

            world.GetService<IMigrator>().Migrate(Previous(world, PreoDas));
            Assert.IsFalse(setFourteen.Any(Has));
            Assert.AreEqual((0, 0, 0, 0, 0, 0, 0, 0), SetFourteen());
            Assert.AreEqual(4, Kept(), "the copied and used rows stay");
            Assert.IsTrue(setThirteen.All(Has), "set 13 stays");

            world.GetService<IMigrator>().Migrate(PreoDas);
            Assert.IsTrue(setFourteen.All(Has));
            Assert.AreEqual(full, SetFourteen());
        }

        /// <summary>
        /// Marshes_preo_das_village takes out its three missions, Tola, Kirii, Rakla and the three prisoners; the copied rows
        /// (Veska, Toida, Siva, the Torcastra prisoner), the Thrax and set 14 stay. Up again, all are back.
        /// </summary>
        [TestMethod]
        public void PreoDasVillageRollsBackWithItsMigration()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var world = harness.World;
            bool Has(uint mission) => world.MissionContentDefinitionEntries.AsNoTracking()
                .Any(entry => entry.MissionId == mission && entry.ContentRevision == MarshesMissionDataV1.Revision);
            int Count(string sql) => world.Database.SqlQueryRaw<int>(sql).AsEnumerable().Single();
            const string missions = "(891, 892, 894)";
            (int, int, int, int, int, int, int, int) SetFifteen() => (
                Count("SELECT COUNT(*) AS Value FROM creature WHERE id BETWEEN 760376 AND 760381") +
                    Count("SELECT COUNT(*) AS Value FROM spawnpool WHERE id BETWEEN 760376 AND 760381"),
                Count("SELECT COUNT(*) AS Value FROM npc_package WHERE id BETWEEN 760376 AND 760381") +
                    Count("SELECT COUNT(*) AS Value FROM creature_appearance WHERE id BETWEEN 760376 AND 760381"),
                Count($"SELECT COUNT(*) AS Value FROM mission_objective_definition WHERE mission_id IN {missions}"),
                Count($"SELECT COUNT(*) AS Value FROM mission_indicator WHERE mission_id IN {missions}"),
                Count($"SELECT COUNT(*) AS Value FROM mission_evidence WHERE mission_id IN {missions}"),
                Count($"SELECT COUNT(*) AS Value FROM mission_scene_binding WHERE mission_id IN {missions}"),
                Count($"SELECT COUNT(*) AS Value FROM mission_reward_item WHERE mission_id IN {missions}"),
                Count($"SELECT COUNT(*) AS Value FROM mission_prerequisite WHERE mission_id IN {missions}"));
            int Kept() => Count("SELECT COUNT(*) AS Value FROM creature WHERE id IN (760302, 510160, 760301, 660022, 531045, 531046)");
            var setFifteen = new uint[] { 891, 892, 894 };
            var setFourteen = new uint[] { 1076, 893, 1078, 1077 };
            // 6 creatures and 3 pools (Tola, Kirii, Rakla); 6 packages, 3 appearance slots; 3 + 2 + 13 objectives; 2 + 3 + 15
            // markers; 9 sources; 3 scenes; 4 + 4 + 4 items; 1 requirement (894 on 891).
            var full = (9, 9, 18, 20, 9, 3, 12, 1);

            Assert.IsTrue(setFifteen.All(Has));
            Assert.AreEqual(full, SetFifteen());
            Assert.AreEqual(6, Kept());

            world.GetService<IMigrator>().Migrate(Previous(world, PreoDasVillage));
            Assert.IsFalse(setFifteen.Any(Has));
            Assert.AreEqual((0, 0, 0, 0, 0, 0, 0, 0), SetFifteen());
            Assert.AreEqual(6, Kept(), "the copied and used rows stay");
            Assert.IsTrue(setFourteen.All(Has), "set 14 stays");

            world.GetService<IMigrator>().Migrate(PreoDasVillage);
            Assert.IsTrue(setFifteen.All(Has));
            Assert.AreEqual(full, SetFifteen());
        }

        /// <summary>
        /// Marshes_bane_supply_depot takes out its three missions and Ortega; Morrison (the copied row), Toida, Fitz, the guards'
        /// Thrax, the depot's mortars and set 15 stay. Up again, all are back.
        /// </summary>
        [TestMethod]
        public void BaneSupplyDepotRollsBackWithItsMigration()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var world = harness.World;
            bool Has(uint mission) => world.MissionContentDefinitionEntries.AsNoTracking()
                .Any(entry => entry.MissionId == mission && entry.ContentRevision == MarshesMissionDataV1.Revision);
            int Count(string sql) => world.Database.SqlQueryRaw<int>(sql).AsEnumerable().Single();
            const string missions = "(664, 919, 663)";
            (int, int, int, int, int, int, int, int) SetSixteen() => (
                Count("SELECT COUNT(*) AS Value FROM creature WHERE id = 760382") +
                    Count("SELECT COUNT(*) AS Value FROM spawnpool WHERE id = 760382"),
                Count("SELECT COUNT(*) AS Value FROM npc_package WHERE id = 760382") +
                    Count("SELECT COUNT(*) AS Value FROM creature_appearance WHERE id = 760382"),
                Count($"SELECT COUNT(*) AS Value FROM mission_objective_definition WHERE mission_id IN {missions}"),
                Count($"SELECT COUNT(*) AS Value FROM mission_indicator WHERE mission_id IN {missions}"),
                Count($"SELECT COUNT(*) AS Value FROM mission_evidence WHERE mission_id IN {missions}"),
                Count($"SELECT COUNT(*) AS Value FROM mission_scene_binding WHERE mission_id IN {missions}"),
                Count($"SELECT COUNT(*) AS Value FROM mission_reward_item WHERE mission_id IN {missions}"),
                Count($"SELECT COUNT(*) AS Value FROM mission_prerequisite WHERE mission_id IN {missions}"));
            int Kept() => Count("SELECT COUNT(*) AS Value FROM creature WHERE id IN (510162, 510160, 510170, 531045, 531046, 592006)");
            var setSixteen = new uint[] { 664, 919, 663 };
            var setFifteen = new uint[] { 891, 892, 894 };
            // Ortega and his pool; his package and six appearance slots; 4 + 2 + 4 objectives; 6 + 5 + 18 markers; 9 sources;
            // 3 scenes; 4 + 4 + 4 items; 1 requirement (663's level).
            var full = (2, 7, 10, 29, 9, 3, 12, 1);

            Assert.IsTrue(setSixteen.All(Has));
            Assert.AreEqual(full, SetSixteen());
            Assert.AreEqual(6, Kept());

            world.GetService<IMigrator>().Migrate(Previous(world, BaneSupplyDepot));
            Assert.IsFalse(setSixteen.Any(Has));
            Assert.AreEqual((0, 0, 0, 0, 0, 0, 0, 0), SetSixteen());
            Assert.AreEqual(6, Kept(), "the copied and used rows stay");
            Assert.IsTrue(setFifteen.All(Has), "set 15 stays");

            world.GetService<IMigrator>().Migrate(BaneSupplyDepot);
            Assert.IsTrue(setSixteen.All(Has));
            // Set 16 as applied: 664 with its hidden kill tally and the camp's marker on 5 still (Marshes_playtest_fixes
            // writes it again, to-fix 13).
            Assert.AreEqual((2, 7, 11, 28, 9, 3, 12, 1), SetSixteen());
        }

        /// <summary>
        /// The playtest-fix migrations (to-fix 10, 13-17, 19 and 20, 894's notes) roll back one by one to the rows the live server had, and come
        /// back: Marshes_playtest_fixes writes 664, 548, 718, 920, 893 and 1078 as sets 12-16 applied them (664's hidden tally
        /// 2, 548's block C marker at 728, no 718 hidden 8, no 920 approach, no choice flag on 893's and 1078's offers; the
        /// elder's marker as Marshes_safe_ground moved it);
        /// Marshes_trapped_approach writes 1052 without its approach (no objective 3, the Retread at 202, -672).
        /// </summary>
        [TestMethod]
        public void ThePlaytestFixesRollBackWithTheirMigrations()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var world = harness.World;
            int Count(string sql) => world.Database.SqlQueryRaw<int>(sql).AsEnumerable().Single();
            var revision = MarshesMissionDataV1.Revision;
            int Objective(uint mission, uint objective) => Count($"SELECT COUNT(*) AS Value FROM mission_objective_definition WHERE mission_id = {mission} AND objective_id = {objective} AND content_revision = '{revision}'");
            int BlockCMarkerX() => Count($"SELECT CAST(pos_x AS INTEGER) AS Value FROM mission_indicator WHERE mission_id = 548 AND objective_id = 5 AND content_revision = '{revision}'");
            int ChoiceFlag() => Count($"SELECT COUNT(*) AS Value FROM mission_channel_policy WHERE mission_id IN (893, 1078) AND content_revision = '{revision}' AND radio_sources LIKE '%893001%'");
            int ElderMarkerX() => Count($"SELECT CAST(pos_x AS INTEGER) AS Value FROM mission_indicator WHERE mission_id = 893 AND indicator_id = 10000003 AND content_revision = '{revision}'");
            (int, int, int, int, int, int, int) Fixes() => (Objective(664, 2), BlockCMarkerX(), Objective(718, 8), Objective(1052, 3),
                Objective(920, 4), ChoiceFlag(), ElderMarkerX());
            var migrator = world.GetService<IMigrator>();

            int PumpsRadius() => Count($"SELECT CAST(radius AS INTEGER) AS Value FROM mission_indicator WHERE mission_id = 894 AND objective_id = 1 AND content_revision = '{revision}'");
            int FollowTimers() => Count($"SELECT COUNT(*) AS Value FROM mission_scene_binding WHERE mission_id = 894 AND content_revision = '{revision}' AND bindings LIKE '%sets-off%'");
            Assert.AreEqual((0, 744, 1, 1, 1, 2, 127), Fixes(), "head");
            Assert.AreEqual((50, 1), (PumpsRadius(), FollowTimers()), "894 V2");
            var still = string.Join(", ", MarshesNpcsStandStillV1.StandStill);
            int Still() => Count($"SELECT COUNT(*) AS Value FROM creature WHERE id IN ({still}) AND run_speed = 0 AND walk_speed = 0");
            int CrewMoved() => Count($"SELECT COUNT(*) AS Value FROM mission_scene_binding WHERE mission_id = 1052 AND content_revision = '{revision}' AND bindings LIKE '%188.4%'");
            Assert.AreEqual((10, 1), (Still(), CrewMoved()), "to-fix 15");
            var survivors = string.Join(", ", MarshesCleaningHouseSurvivorsV1.Survivors);
            (int, int, int) Survivors() => (Count($"SELECT COUNT(*) AS Value FROM creature WHERE id IN ({survivors})"),
                Count($"SELECT COUNT(*) AS Value FROM npc_package WHERE id IN ({survivors})"),
                Count($"SELECT COUNT(*) AS Value FROM mission_scene_binding WHERE mission_id = 894 AND content_revision = '{revision}' AND bindings LIKE '%760383%'"));
            Assert.AreEqual((3, 3, 1), Survivors(), "to-fix 16");
            var hold = string.Join(", ", MarshesQuestNpcsHoldV1.Hold.Select(npc => npc.Creature));
            int Kirii(double x) => Count($"SELECT COUNT(*) AS Value FROM spawnpool WHERE id = {MarshesPreoDasVillageNpcsV1.Kirii} AND CAST(pos_x AS INTEGER) = {(int)x}") +
                Count($"SELECT COUNT(*) AS Value FROM mission_indicator WHERE mission_id = 891 AND objective_id = 1 AND content_revision = '{revision}' AND CAST(pos_x AS INTEGER) = {(int)x}");
            (int, int, int) Hold() => (Count($"SELECT COUNT(*) AS Value FROM creature_stat WHERE id IN ({hold}) AND health = 4500 AND armor = 300"),
                Count($"SELECT COUNT(*) AS Value FROM creature WHERE id IN ({hold}) AND max_hp = 4500"), Kirii(MarshesQuestNpcsHoldV1.KiriiAt.X));
            Assert.AreEqual((33, 33, 2), Hold(), "to-fix 17");
            int Troughs() => Count($"SELECT COUNT(*) AS Value FROM mission_scene_binding WHERE mission_id = 961 AND content_revision = '{revision}' AND bindings LIKE '%216.743%'") +
                Count($"SELECT COUNT(*) AS Value FROM mission_indicator WHERE mission_id = 961 AND content_revision = '{revision}' AND pos_y < 217");
            Assert.AreEqual(13, Troughs(), "to-fix 19: the feeders at their troughs, the twelve markers with them");
            int Dry() => Count($"SELECT COUNT(*) AS Value FROM mission_scene_binding WHERE mission_id IN (881, 890) AND content_revision = '{revision}' AND bindings LIKE '%217.14%'");
            Assert.AreEqual(2, Dry(), "to-fix 20: 881's and 890's harvesters on dry ground");
            migrator.Migrate(Previous(world, HarvestersDryGround));
            Assert.AreEqual(0, Dry(), "881 and 890 as Marshes_harvesters applied them");
            Assert.AreEqual(2, world.MissionContentDefinitionEntries.Count(entry => (entry.MissionId == 881 || entry.MissionId == 890) && entry.ContentRevision == revision), "881 and 890 stay");
            Assert.AreEqual(1, Count($"SELECT COUNT(*) AS Value FROM mission_prerequisite WHERE mission_id = 890 AND content_revision = '{revision}'"), "890 still needs 881");
            Assert.AreEqual(13, Troughs(), "to-fix 19 stays");
            migrator.Migrate(Previous(world, FoodChainTroughs));
            Assert.AreEqual(0, Troughs(), "961 as Marshes_falcon_hold applied it");
            Assert.IsTrue(world.MissionContentDefinitionEntries.Any(entry => entry.MissionId == 961 && entry.ContentRevision == revision), "961 stays");
            Assert.AreEqual((33, 33, 2), Hold(), "to-fix 17 stays");
            migrator.Migrate(Previous(world, QuestNpcsHold));
            Assert.AreEqual((0, 0, 0), Hold(), "no stat rows, Kirii at the 2200 place as applied");
            Assert.AreEqual(2, Kirii(MarshesQuestNpcsHoldV1.KiriiBefore.X), "Kirii and his marker at the 2200 place");
            Assert.AreEqual(1, Count($"SELECT COUNT(*) AS Value FROM creature WHERE id = {MarshesGunControlNpcsV1.Masai} AND max_hp = 400"), "Masai's own max_hp back");
            Assert.AreEqual((3, 3, 1), Survivors(), "to-fix 16 stays");
            migrator.Migrate(Previous(world, CleaningHouseV3));
            Assert.AreEqual((0, 0, 0), Survivors(), "894's survivors on the prisoners' rows as applied");
            Assert.AreEqual((50, 1), (PumpsRadius(), FollowTimers()), "894 V2 under V3");
            Assert.AreEqual((10, 1), (Still(), CrewMoved()));
            migrator.Migrate(Previous(world, NpcsStandStill));
            Assert.AreEqual((0, 0), (Still(), CrewMoved()), "the speeds and the crew as applied");
            Assert.AreEqual((0, 744, 1, 1, 1, 2, 127), Fixes());
            int CratesRadius() => Count($"SELECT CAST(radius AS INTEGER) AS Value FROM mission_indicator WHERE mission_id = 1878 AND objective_id = 1 AND content_revision = '{revision}'");
            Assert.AreEqual(20, CratesRadius(), "1878 V2");
            migrator.Migrate(Previous(world, CoreOfTheMatterV2));
            Assert.AreEqual(30, CratesRadius(), "1878 as applied");
            migrator.Migrate(Previous(world, CleaningHouseV2));
            Assert.AreEqual((35, 0), (PumpsRadius(), FollowTimers()), "894 as applied");
            Assert.AreEqual((0, 744, 1, 1, 1, 2, 127), Fixes());
            migrator.Migrate(Previous(world, PlaytestFixes));
            Assert.AreEqual((1, 728, 0, 1, 0, 0, 127), Fixes(), "664, 548, 718, 920, 893 and 1078 as applied (the elder's marker moved)");
            migrator.Migrate(Previous(world, TrappedApproach));
            Assert.AreEqual((1, 728, 0, 0, 0, 0, 127), Fixes(), "1052 as applied");
            Assert.AreEqual(1, Count($"SELECT COUNT(*) AS Value FROM mission_scene_binding WHERE mission_id = 1052 AND content_revision = '{revision}' AND bindings LIKE '%-672%'"),
                "the Retread at its first place");

            migrator.Migrate(NpcsStandStill);
            Assert.AreEqual(20, CratesRadius(), "1878 V2 back");
            Assert.AreEqual((10, 1), (Still(), CrewMoved()), "to-fix 15 back");
            Assert.AreEqual((0, 744, 1, 1, 1, 2, 127), Fixes(), "all back");
            Assert.AreEqual((50, 1), (PumpsRadius(), FollowTimers()), "894 V2 back");
            migrator.Migrate(CleaningHouseV3);
            Assert.AreEqual((3, 3, 1), Survivors(), "to-fix 16 back");
            Assert.AreEqual((50, 1), (PumpsRadius(), FollowTimers()), "894 V3 back");
            migrator.Migrate(QuestNpcsHold);
            Assert.AreEqual((33, 33, 2), Hold(), "to-fix 17 back");
            migrator.Migrate(FoodChainTroughs);
            Assert.AreEqual(13, Troughs(), "to-fix 19 back");
            migrator.Migrate(HarvestersDryGround);
            Assert.AreEqual(2, Dry(), "to-fix 20 back");
        }

        /// <summary>
        /// Marshes_reward_blocks moves seven missions' four rewards to the client's blocks and nothing else; down, the former
        /// templates are back (the other missions' rewards untouched both ways). Up again, the blocks.
        /// </summary>
        [TestMethod]
        public void RewardBlocksRollBackWithTheirMigration()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var world = harness.World;
            uint[] Items(uint mission, string revision) => world.MissionRewardItemEntries.AsNoTracking()
                .Where(entry => entry.MissionId == mission && entry.ContentRevision == revision)
                .OrderBy(entry => entry.ItemId).AsEnumerable().Select(entry => entry.ItemTemplateId).ToArray();
            int Others() => world.MissionRewardItemEntries.AsNoTracking().Count(entry =>
                entry.ContentRevision == MarshesMissionDataV1.Revision && !new uint[] { 541, 498, 539, 917, 968, 1029 }.Contains(entry.MissionId));
            var others = Others();
            Assert.HasCount(7, MarshesRewardBlocksV1.Moves);
            foreach (var move in MarshesRewardBlocksV1.Moves)
                CollectionAssert.AreEqual(Enumerable.Range(0, 4).Select(index => move.Block + (uint)index).ToArray(),
                    Items(move.Mission, move.Revision), $"{move.Mission} on its block");

            world.GetService<IMigrator>().Migrate(Previous(world, RewardBlocks));
            foreach (var move in MarshesRewardBlocksV1.Moves)
                CollectionAssert.AreEqual(move.Before, Items(move.Mission, move.Revision), $"{move.Mission} as before");
            Assert.AreEqual(others, Others(), "the other rewards untouched");

            world.GetService<IMigrator>().Migrate(RewardBlocks);
            foreach (var move in MarshesRewardBlocksV1.Moves)
                CollectionAssert.AreEqual(Enumerable.Range(0, 4).Select(index => move.Block + (uint)index).ToArray(),
                    Items(move.Mission, move.Revision), $"{move.Mission} on its block again");
            Assert.AreEqual(others, Others());
        }
    }
}
