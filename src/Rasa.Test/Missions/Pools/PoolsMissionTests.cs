using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Missions.Pools
{
    using Rasa.Services.Preloader.Missions.Plateau;
    using Rasa.Services.Preloader.Missions.Pools;
    using Rasa.Structures;
    using Rasa.Structures.World;
    using Rasa.Test.Missions.Wilderness;

    /// <summary>
    /// The Valverde Pools missions (PoolsMissionDataV1, revision pools_1_16_5): the Snake Pit's Building Materials (1314)
    /// and Medicinal Measures (1066), with Supply Chief Riggins and Dr. Hester (Pools_snake_pit); Riggins's Death Patrol (1315),
    /// More Clips and Tags (1321) and The Thirsty Hermit (1328), and Snake's move to his sourced place (Pools_death_patrol);
    /// Looking For Logos? (1308), Mapping The Pools (1025) and Bane Investigation (932), with Spymaster Timlin, Specialist
    /// Allen and Lieutenant Feldman (Pools_snake_pit_contacts); Sabotage the Tower (937), Bane Jamming (938) and
    /// Elimination (941), with the western fort's Eloh obelisk and a Pools Linker (Pools_bane_chain); the Retread chain, A
    /// Cult Divided (560), Testing the Pools (561), One Man's Trash (562), Retread Planning (563) and Retread Planning Part II
    /// (1330), with McKormic, Venma, Buckley and Rickert at the Retread Camp (Pools_retread_chain); the supply chain, The
    /// Supply Master (1322), Eastern Listening Post (1323), Smash and Grab (1324) and Spoils of War (1326), with Nakrinx's
    /// patrol and Lieutenant Chester's package (Pools_supply_chain); the spy chain, Spy Networking (1331), Poison Control
    /// (1368) and Taking the Poison (1590), with Corporal Sheen's package and her stand-in (Pools_spy_chain); the Lamna
    /// Armory's Bane Battle Plans (896), Operation Overlord (898) and Blow Them All Up (903), with its givers, key bosses
    /// and Overlord (Pools_lamna_armory); the Retread Caves' Stick Your Neck Out (1329) and Mox Clearing chain, Mox
    /// Clearing (1132), Mox Shouldn't Smoke (1135), A Mox On Both Our Houses (1133), Deadly Moxins, er-Toxins (1134),
    /// Miasma Mutations (1131), Miasma Testing (1136), Total Slaughter (1138) and The Mad Bomber (856), with the caves'
    /// Retreads, mission creatures and pools (Pools_retread_caves); Turpis Refinery's Things That Blow Up (882), Hard Core
    /// (884), Chemistry 101 (885), The Bad Doctor (886) and Cost of Doing Business (1079), with its givers and the
    /// Toxicologist, and Corporal Hash's Turpis Transport (1305) on the Pools (Pools_turpis_refinery). Amee Corman's Retreads to Retreads (1751) is on the
    /// Plateau's revision (PlateauMissionTests): it follows Weapons Trafficking.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class PoolsMissionTests
    {
        /// <summary>The Pools missions the World migrations enable, for the tests that pin the enabled set.</summary>
        internal static readonly uint[] MissionIds =
        {
            PoolsBuildingMaterialsV1.MissionId, PoolsMedicinalMeasuresV1.MissionId, PoolsDeathPatrolV1.MissionId,
            PoolsMoreClipsAndTagsV1.MissionId, PoolsThirstyHermitV1.MissionId, PoolsLookingForLogosV1.MissionId,
            PoolsMappingThePoolsV1.MissionId, PoolsBaneInvestigationV1.MissionId, PoolsSabotageTheTowerV1.MissionId,
            PoolsBaneJammingV1.MissionId, PoolsEliminationV1.MissionId, PoolsCultDividedV1.MissionId,
            PoolsTestingThePoolsV1.MissionId, PoolsOneMansTrashV1.MissionId, PoolsRetreadPlanningV1.MissionId,
            PoolsRetreadPlanningPartTwoV1.MissionId, PoolsSupplyMasterV1.MissionId, PoolsEasternListeningPostV1.MissionId,
            PoolsSmashAndGrabV1.MissionId, PoolsSpoilsOfWarV1.MissionId, PoolsSpyNetworkingV1.MissionId,
            PoolsPoisonControlV1.MissionId, PoolsTakingThePoisonV1.MissionId, PoolsBaneBattlePlansV1.MissionId,
            PoolsOperationOverlordV1.MissionId, PoolsBlowThemAllUpV1.MissionId, PoolsStickYourNeckOutV1.MissionId,
            PoolsMoxClearingV1.MissionId, PoolsMoxShouldntSmokeV1.MissionId, PoolsMoxOnBothOurHousesV1.MissionId,
            PoolsDeadlyMoxinsV1.MissionId, PoolsMiasmaMutationsV1.MissionId, PoolsMiasmaTestingV1.MissionId,
            PoolsTotalSlaughterV1.MissionId, PoolsMadBomberV1.MissionId, PoolsThingsThatBlowUpV1.MissionId,
            PoolsHardCoreV1.MissionId, PoolsChemistry101V1.MissionId, PoolsTheBadDoctorV1.MissionId,
            PoolsCostOfDoingBusinessV1.MissionId, PoolsTurpisTransportV1.MissionId
        };

        /// <summary>
        /// Every visible objective has a marker (EveryVisibleObjectiveHasAMarker), in the instances too. Medicinal Measures'
        /// one, on the Mawflower field; The Thirsty Hermit's three (Hester, the cabinet, the Hermit); Looking For Logos?'s three
        /// (Suresh, the Refinery's door, Suresh); Mapping The Pools' thirteen (Timlin, the pack, the five beacons for each
        /// plant and again for the tracker, Timlin); Bane Investigation's three; Sabotage the Tower's two; Bane Jamming's
        /// seven; Elimination's one; A Cult Divided's one; Testing the Pools' six (each spot for its own and for 5); One Man's
        /// Trash's two; Retread Planning's four; Part II's one; The Supply Master's two (the beat's start for Nakrinx,
        /// Riggins); Eastern Listening Post's one; Smash and Grab's six; Spoils of War's one; Spy Networking's
        /// one; Poison Control's and Taking the Poison's three each. Building Materials' three (the pools nearest the Snake
        /// Pit), Death Patrol's 44 and More Clips and Tags' 48 (each body for tags and for clips). The Lamna Armory: Bane
        /// Battle Plans' six, Operation Overlord's four, Blow Them All Up's twenty. The Retread Caves: Stick Your Neck Out's
        /// one, Mox Clearing's five, Mox Shouldn't Smoke's two, A Mox On Both Our Houses' two, Deadly Moxins' four, Miasma
        /// Mutations' four, Miasma Testing's eleven, Total Slaughter's two, The Mad Bomber's one. Turpis Refinery: Things
        /// That Blow Up's six, Hard Core's six, Chemistry 101's fourteen, The Bad Doctor's one, Cost of Doing Business'
        /// 22; Turpis Transport's one, at the door on the Pools.
        /// </summary>
        internal const int Indicators = 271;

        private const string Before = "20270926000000_Plateau_retreads_to_retreads";
        private const string SnakePit = "20270927000000_Pools_snake_pit";
        private const string DeathPatrol = "20270928000000_Pools_death_patrol";
        private const string Contacts = "20270929000000_Pools_snake_pit_contacts";
        private const string BaneChain = "20270930000000_Pools_bane_chain";
        private const string RetreadChain = "20271001000000_Pools_retread_chain";
        private const string SupplyChain = "20271002000000_Pools_supply_chain";
        private const string SpyChain = "20271003000000_Pools_spy_chain";
        private const string LamnaArmory = "20271004000000_Pools_lamna_armory";
        private const string RetreadCaves = "20271005000000_Pools_retread_caves";
        private const string TurpisRefinery = "20271006000000_Pools_turpis_refinery";

        [TestMethod]
        public void TheMissionsAreEnabledOperationalAndGivenByTheirNpcs()
        {
            var expected = new (uint Mission, uint? Giver, uint? Receiver, uint Level)[]
            {
                (PoolsBuildingMaterialsV1.MissionId, PoolsSnakePitNpcsV1.Riggins, PoolsSnakePitNpcsV1.Riggins, 33),
                (PoolsMedicinalMeasuresV1.MissionId, PoolsSnakePitNpcsV1.Hester, PoolsSnakePitNpcsV1.Hester, 35),
                (PoolsDeathPatrolV1.MissionId, PoolsSnakePitNpcsV1.Riggins, PoolsSnakePitNpcsV1.Riggins, 33),
                (PoolsMoreClipsAndTagsV1.MissionId, PoolsSnakePitNpcsV1.Riggins, PoolsSnakePitNpcsV1.Riggins, 33),
                (PoolsThirstyHermitV1.MissionId, PoolsSnakePitNpcsV1.Riggins, PlateauBaileyNpcsV1.Hermit, 33),
                (PoolsLookingForLogosV1.MissionId, PoolsSnakePitContactsV1.Timlin, 510202, 33),
                (PoolsMappingThePoolsV1.MissionId, PoolsSnakePitContactsV1.Allen, PoolsSnakePitContactsV1.Allen, 35),
                (PoolsBaneInvestigationV1.MissionId, PoolsSnakePitContactsV1.Feldman, PoolsSnakePitContactsV1.Feldman, 35),
                (PoolsSabotageTheTowerV1.MissionId, PoolsSnakePitContactsV1.Feldman, PoolsSnakePitContactsV1.Feldman, 35),
                (PoolsBaneJammingV1.MissionId, PoolsSnakePitContactsV1.Feldman, PoolsSnakePitContactsV1.Feldman, 35),
                (PoolsEliminationV1.MissionId, PlateauSnakePitV1.Snake, PlateauSnakePitV1.Snake, 35),
                (PoolsCultDividedV1.MissionId, PlateauBaileyNpcsV1.Hermit, PoolsRetreadCampNpcsV1.McKormic, 33),
                (PoolsTestingThePoolsV1.MissionId, PoolsRetreadCampNpcsV1.Venma, PoolsRetreadCampNpcsV1.Venma, 33),
                (PoolsOneMansTrashV1.MissionId, PoolsRetreadCampNpcsV1.Buckley, PoolsRetreadCampNpcsV1.Buckley, 33),
                (PoolsRetreadPlanningV1.MissionId, null, PlateauBaileyNpcsV1.Hermit, 33),
                (PoolsRetreadPlanningPartTwoV1.MissionId, PlateauBaileyNpcsV1.Hermit, 510195, 33),
                (PoolsSupplyMasterV1.MissionId, PoolsSnakePitNpcsV1.Riggins, PoolsSnakePitNpcsV1.Riggins, 33),
                (PoolsEasternListeningPostV1.MissionId, PlateauSnakePitV1.Snake, 510198, 35),
                (PoolsSmashAndGrabV1.MissionId, 510198, 510198, 35),
                (PoolsSpoilsOfWarV1.MissionId, 510198, PlateauSnakePitV1.Snake, 35),
                (PoolsSpyNetworkingV1.MissionId, 510195, null, 32),
                (PoolsPoisonControlV1.MissionId, 510195, 510195, 32),
                (PoolsTakingThePoisonV1.MissionId, 510197, 510197, 33),
                (PoolsBaneBattlePlansV1.MissionId, PoolsLamnaArmoryNpcsV1.Arimoro, PoolsLamnaArmoryNpcsV1.Arimoro, 35),
                (PoolsOperationOverlordV1.MissionId, PoolsLamnaArmoryNpcsV1.Bromfield, PoolsLamnaArmoryNpcsV1.Bromfield, 35),
                (PoolsBlowThemAllUpV1.MissionId, PoolsLamnaArmoryNpcsV1.Herring, PoolsLamnaArmoryNpcsV1.Herring, 35),
                (PoolsStickYourNeckOutV1.MissionId, 510195, PoolsRetreadCavesNpcsV1.Kaethi, 33),
                (PoolsMoxClearingV1.MissionId, PoolsRetreadCavesNpcsV1.Ellen, PoolsRetreadCavesNpcsV1.Ellen, 35),
                (PoolsMoxShouldntSmokeV1.MissionId, PoolsRetreadCavesNpcsV1.Ellen, PoolsRetreadCavesNpcsV1.Ellen, 35),
                (PoolsMoxOnBothOurHousesV1.MissionId, PoolsRetreadCavesNpcsV1.Donius, PoolsRetreadCavesNpcsV1.Ellen, 35),
                (PoolsDeadlyMoxinsV1.MissionId, PoolsRetreadCavesNpcsV1.Donius, 510195, 35),
                (PoolsMiasmaMutationsV1.MissionId, PoolsRetreadCavesNpcsV1.Donius, PoolsRetreadCavesNpcsV1.Donius, 35),
                (PoolsMiasmaTestingV1.MissionId, PoolsRetreadCavesNpcsV1.Donius, PoolsRetreadCavesNpcsV1.Miles, 35),
                (PoolsTotalSlaughterV1.MissionId, PoolsRetreadCavesNpcsV1.Miles, PoolsRetreadCavesNpcsV1.Ellen, 35),
                (PoolsMadBomberV1.MissionId, PoolsRetreadCavesNpcsV1.Ellen, PoolsRetreadCavesNpcsV1.Ellen, 35),
                (PoolsThingsThatBlowUpV1.MissionId, PoolsTurpisRefineryNpcsV1.Niebolo, PoolsTurpisRefineryNpcsV1.Niebolo, 33),
                (PoolsHardCoreV1.MissionId, PoolsTurpisRefineryNpcsV1.Terentyeva, PoolsTurpisRefineryNpcsV1.Terentyeva, 33),
                (PoolsChemistry101V1.MissionId, PoolsTurpisRefineryNpcsV1.Larouche, PoolsTurpisRefineryNpcsV1.Larouche, 33),
                (PoolsTheBadDoctorV1.MissionId, PoolsTurpisRefineryNpcsV1.Lindloff, PoolsTurpisRefineryNpcsV1.Lindloff, 33),
                (PoolsCostOfDoingBusinessV1.MissionId, PoolsTurpisRefineryNpcsV1.Johansson, PoolsTurpisRefineryNpcsV1.Johansson, 33),
                (PoolsTurpisTransportV1.MissionId, PoolsTurpisRefineryNpcsV1.Hash, null, 33)
            };
            using var harness = WildernessRuntimeTestHarness.Create();
            var enabled = harness.World.MissionContentDefinitionEntries.AsNoTracking()
                .Where(entry => entry.Enabled && entry.ContentRevision == PoolsMissionDataV1.Revision).ToArray();
            CollectionAssert.AreEquivalent(MissionIds, enabled.Select(entry => entry.MissionId).ToArray());
            foreach (var (mission, giver, receiver, level) in expected)
            {
                var entry = enabled.Single(row => row.MissionId == mission);
                Assert.AreEqual<uint?>(giver, entry.GiverId, $"{mission} giver");
                Assert.AreEqual<uint?>(receiver, entry.ReceiverId, $"{mission} receiver");
                Assert.AreEqual<uint?>(level, entry.Level, $"{mission} level");
                Assert.AreEqual<uint?>(PoolsMissionDataV1.CategoryId, entry.CategoryId);
            }
            // Riggins, Hester, Timlin and Feldman stand at the Snake Pit, Allen in the field, McKormic, Venma, Buckley and
            // Rickert at the Retread Camp, on Valverde Pools, with their names, level and packages.
            foreach (var (npc, name, package) in new (uint, uint, uint)[]
            {
                (760200, 9385, 760200), (760201, 8865, 1129), (760202, 4385, 343), (760203, 4387, 760203), (760204, 4376, 760204),
                (760206, 4367, 326), (760207, 4368, 327), (760208, 4369, 328), (760209, 4374, 333)
            })
            {
                Assert.AreEqual(1304U, harness.World.SpawnPoolEntries.AsNoTracking().Single(row => row.Id == npc).MapContextId, $"{npc} map");
                var creature = harness.World.CreatureEntries.AsNoTracking().Single(row => row.Id == npc);
                Assert.AreEqual((name, 40U), (creature.NameId, creature.Level), $"{npc} name and level");
                Assert.AreEqual(package, harness.World.NpcPackageEntries.AsNoTracking().Single(row => row.Id == npc).PackageId, $"{npc} package");
            }
            // Nakrinx (level 33, neutral, no attack, package 1522) and his bodyguards (Thrax Soldier, level 30) as one NPC
            // squad of the Pools whose leader is a bystander, on a beat of 15 points walked there and back; Chester has a package.
            var nakrinx = harness.World.CreatureEntries.AsNoTracking().Single(row => row.Id == 760210);
            Assert.AreEqual((9403U, 33U, 3U, 0U, 11320U), (nakrinx.NameId, nakrinx.Level, nakrinx.Faction, nakrinx.Action1, nakrinx.ClassId), "Nakrinx");
            Assert.AreEqual(1522U, harness.World.NpcPackageEntries.AsNoTracking().Single(row => row.Id == 760210).PackageId);
            var bodyguard = harness.World.CreatureEntries.AsNoTracking().Single(row => row.Id == 760211);
            Assert.AreEqual((237U, 30U, 20757U), (bodyguard.NameId, bodyguard.Level, bodyguard.ClassId), "a bodyguard");
            Assert.IsFalse(harness.World.NpcPackageEntries.AsNoTracking().Any(row => row.Id == 760211), "the bodyguards do not talk");
            Assert.IsFalse(harness.World.SpawnPoolEntries.AsNoTracking().Any(row => row.Id == 760210), "no pool: a squad");
            var type = harness.World.NpcSquadTypeEntries.AsNoTracking().Single(row => row.Id == 760210);
            Assert.AreEqual(((byte)0, 3.0, 0.0, 30.0, (byte)1), (type.Formation, type.Spacing, type.EngageRange, type.Tether, type.LeaderBystander),
                "a wedge 3 m apart, its creatures' range, a 30 m tether, Nakrinx a bystander");
            CollectionAssert.AreEqual(new[] { (760210U, (byte)1, (byte)1), (760211U, (byte)4, (byte)4) },
                harness.World.NpcSquadMemberEntries.AsNoTracking().Where(row => row.TypeId == 760210).OrderBy(row => row.Slot)
                    .AsEnumerable().Select(row => (row.CreatureId, row.MinCount, row.MaxCount)).ToArray(), "Nakrinx leads his four bodyguards");
            var path = harness.World.NpcSquadPathEntries.AsNoTracking().Single(row => row.Id == 760210);
            Assert.AreEqual((1304U, (byte)1), (path.MapContextId, path.Mode), "on the Pools, there and back");
            Assert.AreEqual(15, harness.World.NpcSquadPathPointEntries.AsNoTracking().Count(row => row.PathId == 760210), "the beat");
            var spawn = harness.World.NpcSquadSpawnEntries.AsNoTracking().Single(row => row.Id == 760210);
            Assert.AreEqual((1304U, 760210U, 760210U, 300U, (byte)1), (spawn.MapContextId, spawn.TypeId, spawn.PathId, spawn.RespawnSeconds, spawn.Enabled));
            Assert.AreEqual(510198U, harness.World.NpcPackageEntries.AsNoTracking().Single(row => row.Id == 510198).PackageId, "Chester");
            // Sheen has her own package; her stand-in (a copy of her, no spawn pool) carries her client package 1524.
            Assert.AreEqual(510197U, harness.World.NpcPackageEntries.AsNoTracking().Single(row => row.Id == 510197).PackageId, "Sheen");
            var standIn = harness.World.CreatureEntries.AsNoTracking().Single(row => row.Id == 760212);
            var sheen = harness.World.CreatureEntries.AsNoTracking().Single(row => row.Id == 510197);
            Assert.AreEqual((sheen.NameId, sheen.ClassId, sheen.Level), (standIn.NameId, standIn.ClassId, standIn.Level), "Sheen's stand-in");
            Assert.AreEqual(1524U, harness.World.NpcPackageEntries.AsNoTracking().Single(row => row.Id == 760212).PackageId);
            Assert.IsFalse(harness.World.SpawnPoolEntries.AsNoTracking().Any(row => row.Id == 760212), "only her mission's scene sets her down");
            // The Lamna Armory: the givers and the key bosses in pools on its map; Vanduahl and his Kael in no pool.
            foreach (var (id, name) in new[]
                {
                    (PoolsLamnaArmoryNpcsV1.Arimoro, PoolsLamnaArmoryNpcsV1.ArimoroName), (PoolsLamnaArmoryNpcsV1.Bromfield, PoolsLamnaArmoryNpcsV1.BromfieldName),
                    (PoolsLamnaArmoryNpcsV1.Herring, PoolsLamnaArmoryNpcsV1.HerringName), (PoolsLamnaArmoryNpcsV1.Gaite, PoolsLamnaArmoryNpcsV1.GaiteName),
                    (PoolsLamnaArmoryNpcsV1.Kwel, PoolsLamnaArmoryNpcsV1.KwelName), (PoolsLamnaArmoryNpcsV1.Fearce, PoolsLamnaArmoryNpcsV1.FearceName),
                })
            {
                Assert.AreEqual(name, harness.World.CreatureEntries.AsNoTracking().Single(row => row.Id == id).NameId, $"{id}");
                Assert.AreEqual(PoolsLamnaArmoryNpcsV1.MapContextId,
                    harness.World.SpawnPoolEntries.AsNoTracking().Single(row => row.Id == id).MapContextId, $"{id} in the Armory");
            }
            foreach (var id in new[] { PoolsLamnaArmoryNpcsV1.Vanduahl, PoolsLamnaArmoryNpcsV1.Kael })
                Assert.IsFalse(harness.World.SpawnPoolEntries.AsNoTracking().Any(row => row.Id == id), $"{id}: only the switch sets him down");
            // The Retread Caves: the four Retreads (level 36, friendly) with their names and packages in pools on the caves'
            // map; the Mox and mutated miasma pools there; the mission creatures in no pool.
            foreach (var (npc, name, package) in new (uint, uint, uint)[]
            {
                (760221, 7083, 760221), (760222, 4377, 336), (760223, 7078, 849), (760224, 7080, 851)
            })
            {
                Assert.AreEqual(1694U, harness.World.SpawnPoolEntries.AsNoTracking().Single(row => row.Id == npc).MapContextId, $"{npc} map");
                var creature = harness.World.CreatureEntries.AsNoTracking().Single(row => row.Id == npc);
                Assert.AreEqual((name, 36U, 1U), (creature.NameId, creature.Level, creature.Faction), $"{npc} name, level and faction");
                Assert.AreEqual(package, harness.World.NpcPackageEntries.AsNoTracking().Single(row => row.Id == npc).PackageId, $"{npc} package");
            }
            foreach (var (pools, creature) in new[] { (Enumerable.Range(760230, 5), 760230U), (Enumerable.Range(760235, 4), 760226U) })
                foreach (var id in pools)
                {
                    var row = harness.World.SpawnPoolEntries.AsNoTracking().Single(entry => entry.Id == id);
                    Assert.AreEqual((1694U, creature, (byte)3, (byte)3), (row.MapContextId, row.Creature1Id, row.Creature1MinCount,
                        row.Creature1MaxCount), $"pool {id}");
                }
            foreach (var (id, name, level, faction) in new (uint, uint, uint, uint)[]
            {
                (760225, 9028, 36, 0), (760226, 9037, 33, 0), (760227, 9038, 33, 1), (760228, 7913, 34, 0), (760229, 7085, 36, 0),
                (760230, 8797, 33, 0)
            })
            {
                var creature = harness.World.CreatureEntries.AsNoTracking().Single(row => row.Id == id);
                Assert.AreEqual((name, level, faction), (creature.NameId, creature.Level, creature.Faction), $"{id}");
            }
            foreach (var id in new uint[] { 760225, 760226, 760227, 760228, 760229 })
                Assert.IsFalse(harness.World.SpawnPoolEntries.AsNoTracking().Any(row => row.Id == id), $"{id}: only its scene sets it down");
            // Turpis Refinery: the five givers (friendly; Larouche level 39, the others 36) with their own packages in pools on
            // its map, Hash on the Pools; the Toxicologist (hostile, level 36) in no pool.
            foreach (var (npc, name, level, map) in new (uint, uint, uint, uint)[]
            {
                (760239, 4379, 36, 1429), (760240, 9031, 36, 1429), (760241, 4373, 39, 1429), (760242, 8549, 36, 1429),
                (760243, 4380, 36, 1429), (760244, 9187, 36, 1304)
            })
            {
                Assert.AreEqual(map, harness.World.SpawnPoolEntries.AsNoTracking().Single(row => row.Id == npc).MapContextId, $"{npc} map");
                var creature = harness.World.CreatureEntries.AsNoTracking().Single(row => row.Id == npc);
                Assert.AreEqual((name, level, 1U), (creature.NameId, creature.Level, creature.Faction), $"{npc} name, level and faction");
                Assert.AreEqual(npc, harness.World.NpcPackageEntries.AsNoTracking().Single(row => row.Id == npc).PackageId, $"{npc} package");
            }
            var toxicologist = harness.World.CreatureEntries.AsNoTracking().Single(row => row.Id == 760245);
            Assert.AreEqual((10092U, 36U, 0U, 7043U), (toxicologist.NameId, toxicologist.Level, toxicologist.Faction, toxicologist.ClassId), "the Toxicologist");
            Assert.IsFalse(harness.World.SpawnPoolEntries.AsNoTracking().Any(row => row.Id == 760245), "only his scene sets him down");

            Assert.IsFalse(harness.Manager.LoadMissions().BlocksReadiness);
            foreach (var id in MissionIds)
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
            var expected = new (uint Mission, uint Title, (uint Objective, uint Name, uint Body, uint? Counter)[] Objectives)[]
            {
                (1314, 10666, new (uint, uint, uint, uint?)[] { (1, 10671, 10672, null), (2, 10673, 10674, null), (3, 10675, 10676, null) }),
                (1066, 6005, new (uint, uint, uint, uint?)[] { (1, 6019, 7644, null), (2, 6019, 7644, null) }),
                (1315, 10677, new (uint, uint, uint, uint?)[] { (1, 10717, 10718, null), (2, 10719, 10720, null), (4, 10717, 10718, null),
                    (5, 10719, 10720, null) }),
                (1321, 10721, new (uint, uint, uint, uint?)[] { (1, 10726, 10727, null), (2, 10728, 10729, null), (4, 10726, 10727, null),
                    (5, 10728, 10729, null) }),
                (1328, 10848, new (uint, uint, uint, uint?)[] { (1, 10853, 10854, null), (2, 10856, 10857, null), (3, 10858, 10859, null) }),
                (1308, 10610, new (uint, uint, uint, uint?)[] { (1, 10627, 10628, null), (2, 10630, 10631, null), (3, 10633, 10634, null) }),
                (1025, 5675, new (uint, uint, uint, uint?)[]
                {
                    (1, 5688, 7569, null), (2, 5690, 7570, null), (3, 5691, 7572, null), (4, 5692, 7573, null), (5, 5693, 7574, null),
                    (6, 5694, 7575, null), (7, 5695, 7576, null), (8, 5698, 7577, 5699), (9, 5700, 7578, null)
                }),
                (932, 5003, new (uint, uint, uint, uint?)[] { (1, 5020, 7974, null), (2, 5034, 7975, null), (3, 5036, 7976, null) }),
                (937, 5046, new (uint, uint, uint, uint?)[] { (1, 5050, 7606, 5059), (2, 5051, 7607, 5060) }),
                (938, 5066, new (uint, uint, uint, uint?)[]
                {
                    (1, 5093, 7978, 5097), (2, 5096, 7979, null), (8, 5263, 7980, null), (9, 5264, 7981, null), (10, 5265, 7982, null),
                    (11, 5266, 7983, null), (12, 5267, 7984, null)
                }),
                (941, 5115, new (uint, uint, uint, uint?)[] { (1, 5130, 7580, 5131) }),
                (560, 1623, new (uint, uint, uint, uint?)[] { (2, 9341, 9342, null) }),
                (561, 1634, new (uint, uint, uint, uint?)[] { (1, 1642, 8054, null), (3, 1643, 8055, null), (4, 1644, 8056, null),
                    (5, 1645, 8057, null) }),
                (562, 1638, new (uint, uint, uint, uint?)[] { (1, 1651, 7613, null), (2, 1652, 7614, null) }),
                (563, 1653, new (uint, uint, uint, uint?)[]
                {
                    (3, 1666, 7994, null), (4, 1668, 7995, null), (5, 1670, 7996, null), (6, 1672, 7997, null), (7, 1674, 7998, null),
                    (10, 1678, 8001, null), (11, 1680, 8002, null), (12, 1682, 8003, null), (13, 1684, 8004, null), (14, 1686, 8005, null),
                    (15, 10869, 10870, 10871), (16, 10872, 10873, 10874), (17, 10875, 10876, 10877),
                    (19, 10884, 10885, null), (20, 10887, 10888, null), (21, 10890, 10891, null), (22, 10893, 10894, null),
                    (23, 10896, 10897, null), (24, 10881, 10882, null),
                    // 25 and 26: the server's own hidden gate, on 24's texts.
                    (25, 10881, 10882, null), (26, 10881, 10882, null)
                }),
                (1330, 10899, new (uint, uint, uint, uint?)[] { (1, 10904, 10905, null) }),
                // 1322's 3: the server's own hidden kill count, on 1's texts (the client has no name for it, to-fix #6, #7).
                (1322, 10768, new (uint, uint, uint, uint?)[] { (1, 10773, 10774, null), (2, 10776, 10777, null), (3, 10773, 10774, null) }),
                (1323, 10778, new (uint, uint, uint, uint?)[] { (1, 10783, 10784, null) }),
                (1324, 10785, new (uint, uint, uint, uint?)[]
                {
                    (1, 10812, 10813, null), (2, 10814, 10815, null), (3, 10816, 10817, null), (4, 10818, 10819, 10820),
                    (5, 10821, 10822, 10823), (6, 10824, 10825, 10826)
                }),
                (1326, 10832, new (uint, uint, uint, uint?)[] { (1, 10837, 10838, null) }),
                (1331, 10907, new (uint, uint, uint, uint?)[] { (25, 11615, 11616, 11617) }),
                (1368, 11283, new (uint, uint, uint, uint?)[] { (1, 15000, 15001, null), (3, 15014, 15015, null), (5, 15026, 15027, null) }),
                (1590, 15038, new (uint, uint, uint, uint?)[] { (1, 15043, 15044, null), (3, 15055, 15056, null), (5, 15067, 15068, null) }),
                (896, 4716, new (uint, uint, uint, uint?)[]
                {
                    (2, 4720, 7339, null), (3, 5961, 7340, null), (4, 5962, 7341, null), (5, 5963, 7342, null), (6, 5963, 7342, null)
                }),
                (898, 4743, new (uint, uint, uint, uint?)[]
                {
                    (2, 6049, 7334, null), (3, 6050, 7335, null), (4, 6051, 7336, null), (5, 6052, 7337, null),
                    (6, 6052, 7337, null), (7, 6052, 7337, null), (8, 6052, 7337, null), (9, 6052, 7337, null)
                }),
                (903, 4782, new (uint, uint, uint, uint?)[] { (1, 4786, 7344, 4787) }),
                (1329, 10861, new (uint, uint, uint, uint?)[] { (1, 10866, 10867, null) }),
                (1132, 8013, new (uint, uint, uint, uint?)[] { (2, 8018, 8019, 8020) }),
                (1135, 8043, new (uint, uint, uint, uint?)[] { (1, 8051, 8052, null), (2, 8048, 8049, null) }),
                (1133, 8023, new (uint, uint, uint, uint?)[] { (1, 8028, 8029, null), (2, 8030, 8031, 8032) }),
                // 1134's 3: the server's own count of the last Mox, on 2's texts.
                (1134, 8033, new (uint, uint, uint, uint?)[] { (1, 8038, 8039, 8040), (2, 8041, 8042, null), (3, 8041, 8042, null) }),
                (1131, 8006, new (uint, uint, uint, uint?)[] { (1, 8011, 8012, null) }),
                (1136, 8064, new (uint, uint, uint, uint?)[]
                {
                    (1, 8100, 8101, null), (2, 8102, 8103, null), (3, 8104, 8105, null), (4, 8106, 8107, null), (5, 8108, 8109, null),
                    (6, 8110, 8111, null), (7, 8112, 8113, null), (8, 8114, 8115, null), (9, 8117, 8118, null), (10, 8121, 8122, null),
                    (11, 8124, 8125, null)
                }),
                // 1138's 3-5: the server's own wave counts, on 2's texts.
                (1138, 8127, new (uint, uint, uint, uint?)[]
                {
                    (1, 8185, 8186, null), (2, 8187, 8188, null), (3, 8187, 8188, null), (4, 8187, 8188, null), (5, 8187, 8188, null)
                }),
                (856, 4441, new (uint, uint, uint, uint?)[] { (1, 4445, 7359, null) }),
                (882, 4620, new (uint, uint, uint, uint?)[]
                {
                    (1, 4624, 7321, 6205), (3, 12581, 12582, null), (4, 12583, 12584, null), (5, 12585, 12586, null),
                    (6, 12587, 12588, null), (7, 12589, 12590, null)
                }),
                (884, 4629, new (uint, uint, uint, uint?)[] { (1, 4666, 7324, 4667), (2, 6210, 7325, null), (3, 6211, 7326, null), (4, 6212, 7327, null) }),
                (885, 4633, new (uint, uint, uint, uint?)[] { (1, 5910, 7316, null), (2, 5911, 7317, null) }),
                // 886's 2: the server's own approach, on 1's texts.
                (886, 4637, new (uint, uint, uint, uint?)[] { (1, 4668, 7329, null), (2, 4668, 7329, null) }),
                (1079, 6087, new (uint, uint, uint, uint?)[] { (2, 6091, 7319, 6092) }),
                (1305, 10581, new (uint, uint, uint, uint?)[] { (1, 10586, 10587, null) })
            };
            // 937's objective 1 shows its second count too: counter 1 "Bane Equipment Locker Destroyed".
            var secondCounters = new Dictionary<(uint, uint), uint> { [(937, 1)] = 5058 };
            using var harness = WildernessRuntimeTestHarness.Create();
            foreach (var (mission, title, objectives) in expected)
            {
                Assert.AreEqual(title, harness.World.MissionContentDefinitionEntries.AsNoTracking()
                    .Single(entry => entry.MissionId == mission && entry.ContentRevision == PoolsMissionDataV1.Revision)
                    .ClientNameTextId, $"{mission} title");
                var actual = harness.World.MissionObjectiveDefinitionEntries.AsNoTracking()
                    .Where(entry => entry.MissionId == mission && entry.ContentRevision == PoolsMissionDataV1.Revision).ToArray();
                CollectionAssert.AreEquivalent(objectives.Select(row => row.Objective).ToArray(),
                    actual.Select(row => row.ObjectiveId).ToArray(), $"{mission} objectives");
                foreach (var (objective, name, body, counter) in objectives)
                {
                    var row = actual.Single(entry => entry.ObjectiveId == objective);
                    Assert.AreEqual(name, row.ClientNameTextId, $"{mission}.{objective} name");
                    Assert.AreEqual(body, row.ClientBodyTextId, $"{mission}.{objective} body");
                    Assert.AreEqual(counter, row.ClientCounter0TextId, $"{mission}.{objective} counter0");
                    Assert.AreEqual(secondCounters.TryGetValue((mission, objective), out var second) ? second : null,
                        row.ClientCounter1TextId, $"{mission}.{objective} counter1");
                    Assert.IsNull(row.ClientCounter2TextId, $"{mission}.{objective} counter2");
                }
            }
        }

        /// <summary>
        /// Building Materials, Medicinal Measures, Death Patrol, Looking For Logos?, Mapping The Pools and Bane Investigation
        /// have no requirement (TaRapedia). More Clips and Tags follows
        /// Death Patrol and repeats at once (PlanetTR); The Thirsty Hermit follows both Death Patrol and Medicinal Measures;
        /// Sabotage the Tower follows Bane Investigation, and Bane Jamming and Elimination follow it; A Cult Divided follows The
        /// Thirsty Hermit, Testing the Pools and One Man's Trash follow it, Retread Planning follows One Man's Trash, and Part
        /// II follows Retread Planning; The Supply Master follows Death Patrol, and Eastern Listening Post, Smash and Grab and
        /// Spoils of War each the one before; Spy Networking follows Part II, and Poison Control and Taking the Poison follow
        /// it; Stick Your Neck Out follows Retread Planning, and the Retread Caves' chain runs Mox Clearing (no requirement),
        /// Mox Shouldn't Smoke, A Mox On Both Our Houses, Deadly Moxins, Miasma Mutations, Miasma Testing, Total Slaughter,
        /// The Mad Bomber, each the one before; Turpis Refinery's five and Turpis Transport have none (TaRapedia).
        /// </summary>
        [TestMethod]
        public void EachMissionRequiresTheMissionsBeforeIt()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var prerequisites = harness.World.MissionPrerequisiteEntries.AsNoTracking()
                .Where(entry => entry.ContentRevision == PoolsMissionDataV1.Revision).ToArray();
            Assert.HasCount(26, prerequisites);
            Assert.IsTrue(prerequisites.All(entry => entry.Kind == MissionPrerequisiteKind.MissionCompleted));
            foreach (var mission in new uint[] { 1314, 1066, 1315, 1308, 1025, 932, 896, 898, 903, 1132, 882, 884, 885, 886, 1079, 1305 })
                Assert.IsFalse(prerequisites.Any(entry => entry.MissionId == mission), $"{mission}");
            CollectionAssert.AreEquivalent(new uint?[] { 1315 },
                prerequisites.Where(entry => entry.MissionId == 1321).Select(entry => entry.RequiredMissionId).ToArray());
            CollectionAssert.AreEquivalent(new uint?[] { 1315, 1066 },
                prerequisites.Where(entry => entry.MissionId == 1328).Select(entry => entry.RequiredMissionId).ToArray());
            foreach (var (mission, required) in new (uint, uint)[]
            {
                (937, 932), (938, 937), (941, 937), (560, 1328), (561, 560), (562, 560), (563, 562), (1330, 563),
                (1322, 1315), (1323, 1322), (1324, 1323), (1326, 1324), (1331, 1330), (1368, 1331), (1590, 1331),
                (1329, 563), (1135, 1132), (1133, 1135), (1134, 1133), (1131, 1134), (1136, 1131), (1138, 1136), (856, 1138)
            })
                CollectionAssert.AreEquivalent(new uint?[] { required },
                    prerequisites.Where(entry => entry.MissionId == mission).Select(entry => entry.RequiredMissionId).ToArray(), $"{mission}");

            var repeats = harness.World.Set<MissionRepeatPolicyEntry>().AsNoTracking()
                .Where(entry => entry.ContentRevision == PoolsMissionDataV1.Revision).ToArray();
            Assert.AreEqual((1321U, Rasa.Missions.Runtime.MissionRepeatKind.Immediate), (repeats.Single().MissionId, repeats.Single().Kind));
        }

        /// <summary>The missions' sources and how far each is trusted: the client 1.0, TaRapedia 0.5, the reconstruction 0.3.</summary>
        [TestMethod]
        public void TheSourcesAreTrustedAsFarAsTheyGo()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var evidence = harness.World.MissionEvidenceEntries.AsNoTracking()
                .Where(entry => entry.ContentRevision == PoolsMissionDataV1.Revision).ToArray();
            foreach (var (mission, confidences) in new (uint, (MissionEvidenceSourceKind, double)[])[]
            {
                (1314, new[] { (MissionEvidenceSourceKind.Client, 1.0), (MissionEvidenceSourceKind.Documentation, 0.5),
                    (MissionEvidenceSourceKind.Reconstruction, 0.3) }),
                (1066, new[] { (MissionEvidenceSourceKind.Client, 1.0), (MissionEvidenceSourceKind.Documentation, 0.5),
                    (MissionEvidenceSourceKind.Reconstruction, 0.3) }),
                (1315, new[] { (MissionEvidenceSourceKind.Client, 1.0), (MissionEvidenceSourceKind.Documentation, 0.5),
                    (MissionEvidenceSourceKind.Reconstruction, 0.3) }),
                (1321, new[] { (MissionEvidenceSourceKind.Client, 1.0), (MissionEvidenceSourceKind.Documentation, 0.4),
                    (MissionEvidenceSourceKind.Reconstruction, 0.3) }),
                (1328, new[] { (MissionEvidenceSourceKind.Client, 1.0), (MissionEvidenceSourceKind.Documentation, 0.5),
                    (MissionEvidenceSourceKind.Reconstruction, 0.3) }),
                (1308, new[] { (MissionEvidenceSourceKind.Client, 1.0), (MissionEvidenceSourceKind.Documentation, 0.5),
                    (MissionEvidenceSourceKind.Reconstruction, 0.3) }),
                (1025, new[] { (MissionEvidenceSourceKind.Client, 1.0), (MissionEvidenceSourceKind.Documentation, 0.5),
                    (MissionEvidenceSourceKind.Reconstruction, 0.3) }),
                (932, new[] { (MissionEvidenceSourceKind.Client, 1.0), (MissionEvidenceSourceKind.Documentation, 0.5),
                    (MissionEvidenceSourceKind.Reconstruction, 0.3) }),
                (937, new[] { (MissionEvidenceSourceKind.Client, 1.0), (MissionEvidenceSourceKind.Documentation, 0.5),
                    (MissionEvidenceSourceKind.Reconstruction, 0.3) }),
                (938, new[] { (MissionEvidenceSourceKind.Client, 1.0), (MissionEvidenceSourceKind.Documentation, 0.5),
                    (MissionEvidenceSourceKind.Reconstruction, 0.3) }),
                (941, new[] { (MissionEvidenceSourceKind.Client, 1.0), (MissionEvidenceSourceKind.Documentation, 0.5),
                    (MissionEvidenceSourceKind.Reconstruction, 0.3) }),
                (560, new[] { (MissionEvidenceSourceKind.Client, 1.0), (MissionEvidenceSourceKind.Documentation, 0.5),
                    (MissionEvidenceSourceKind.Reconstruction, 0.3) }),
                (561, new[] { (MissionEvidenceSourceKind.Client, 1.0), (MissionEvidenceSourceKind.Documentation, 0.5),
                    (MissionEvidenceSourceKind.Reconstruction, 0.3) }),
                (562, new[] { (MissionEvidenceSourceKind.Client, 1.0), (MissionEvidenceSourceKind.Documentation, 0.5),
                    (MissionEvidenceSourceKind.Reconstruction, 0.3) }),
                (563, new[] { (MissionEvidenceSourceKind.Client, 1.0), (MissionEvidenceSourceKind.Documentation, 0.5),
                    (MissionEvidenceSourceKind.Reconstruction, 0.3) }),
                (1330, new[] { (MissionEvidenceSourceKind.Client, 1.0), (MissionEvidenceSourceKind.Documentation, 0.5),
                    (MissionEvidenceSourceKind.Reconstruction, 0.3) }),
                (1322, new[] { (MissionEvidenceSourceKind.Client, 1.0), (MissionEvidenceSourceKind.Documentation, 0.5),
                    (MissionEvidenceSourceKind.Reconstruction, 0.3) }),
                (1323, new[] { (MissionEvidenceSourceKind.Client, 1.0), (MissionEvidenceSourceKind.Documentation, 0.5),
                    (MissionEvidenceSourceKind.Reconstruction, 0.3) }),
                (1324, new[] { (MissionEvidenceSourceKind.Client, 1.0), (MissionEvidenceSourceKind.Documentation, 0.5),
                    (MissionEvidenceSourceKind.Reconstruction, 0.3) }),
                (1326, new[] { (MissionEvidenceSourceKind.Client, 1.0), (MissionEvidenceSourceKind.Documentation, 0.5),
                    (MissionEvidenceSourceKind.Reconstruction, 0.3) }),
                (1331, new[] { (MissionEvidenceSourceKind.Client, 1.0), (MissionEvidenceSourceKind.Documentation, 0.5),
                    (MissionEvidenceSourceKind.Reconstruction, 0.3) }),
                (1368, new[] { (MissionEvidenceSourceKind.Client, 1.0), (MissionEvidenceSourceKind.Documentation, 0.5),
                    (MissionEvidenceSourceKind.Reconstruction, 0.3) }),
                (1590, new[] { (MissionEvidenceSourceKind.Client, 1.0), (MissionEvidenceSourceKind.Documentation, 0.5),
                    (MissionEvidenceSourceKind.Reconstruction, 0.3) }),
                (896, new[] { (MissionEvidenceSourceKind.Client, 1.0), (MissionEvidenceSourceKind.Documentation, 0.5),
                    (MissionEvidenceSourceKind.Reconstruction, 0.3) }),
                (898, new[] { (MissionEvidenceSourceKind.Client, 1.0), (MissionEvidenceSourceKind.Documentation, 0.5),
                    (MissionEvidenceSourceKind.Reconstruction, 0.3) }),
                (903, new[] { (MissionEvidenceSourceKind.Client, 1.0), (MissionEvidenceSourceKind.Documentation, 0.5),
                    (MissionEvidenceSourceKind.Reconstruction, 0.3) })
            }.Concat(new uint[] { 1329, 1132, 1135, 1133, 1134, 1131, 1136, 1138, 856, 882, 885, 1079, 1305 }.Select(mission => (mission, new[]
            {
                (MissionEvidenceSourceKind.Client, 1.0), (MissionEvidenceSourceKind.Documentation, 0.5), (MissionEvidenceSourceKind.Reconstruction, 0.3)
            }))).Concat(new uint[] { 884, 886 }.Select(mission => (mission, new[]
            {
                (MissionEvidenceSourceKind.Client, 1.0), (MissionEvidenceSourceKind.Documentation, 0.5), (MissionEvidenceSourceKind.Reconstruction, 0.4)
            }))))
                CollectionAssert.AreEqual(confidences, evidence.Where(entry => entry.MissionId == mission)
                    .OrderBy(entry => entry.EvidenceId).Select(entry => (entry.SourceKind, entry.Confidence)).ToArray(),
                    $"{mission} evidence");
        }

        /// <summary>
        /// No scene object of the Pools or Plateau missions is of a class with mesh 776: the client draws that mesh as its
        /// "Default" pyramid and will not use it (to-fix Pools #3-#5: 7654, 7595, 7832, 7177).
        /// </summary>
        [TestMethod]
        public void NoSceneObjectIsOfAClassTheClientDrawsAsDefault()
        {
            const uint DefaultMesh = 776;
            using var harness = WildernessRuntimeTestHarness.Create();
            var meshes = harness.World.EntityClassEntries.AsNoTracking().ToDictionary(entry => entry.Id, entry => entry.MeshId);
            var objects = 0;
            var plateau = 0;

            foreach (var binding in harness.World.Set<MissionSceneBindingEntry>().AsNoTracking().ToList()
                .Where(entry => entry.ContentRevision is PoolsMissionDataV1.Revision or PlateauMissionDataV1.Revision))
            {
                var scene = System.Text.Json.JsonSerializer.Deserialize<Rasa.Missions.Content.MissionSceneDefinition>(binding.Bindings,
                    Rasa.Missions.Content.MissionContentCodec.Options);
                foreach (var actor in scene.Actors.Values.Where(actor => actor.Kind == Rasa.Missions.Scenes.SceneActorKind.Object))
                {
                    objects++;
                    if (binding.ContentRevision == PlateauMissionDataV1.Revision)
                        plateau++;
                    Assert.IsTrue(meshes.TryGetValue(actor.TemplateId, out var mesh), $"{binding.MissionId} {actor.Role}: class {actor.TemplateId}");
                    Assert.AreNotEqual(DefaultMesh, mesh, $"{binding.MissionId} {actor.Role}: class {actor.TemplateId} has the Default mesh");
                }
            }

            Assert.IsGreaterThan(100, objects, "the scene objects were read");
            Assert.IsGreaterThan(10, plateau, "the Plateau's too");
        }

        [TestMethod]
        public void IndicatorsCarryNoOtherMissionsClientName()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var indicators = harness.World.MissionIndicatorEntries.AsNoTracking()
                .Where(entry => entry.ContentRevision == PoolsMissionDataV1.Revision).ToArray();
            Assert.HasCount(Indicators, indicators);
            foreach (var row in indicators)
                Assert.IsTrue(row.IndicatorId > MissionIndicator.UnnamedFrom,
                    $"mission {row.MissionId} objective {row.ObjectiveId} indicator {row.IndicatorId}");
        }

        /// <summary>
        /// Every visible Pools objective has a marker, in the instances too (the server draws a marker on every map for
        /// now); hidden objectives have none.
        /// </summary>
        [TestMethod]
        public void EveryVisibleObjectiveHasAMarker()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var world = harness.World;
            var revision = PoolsMissionDataV1.Revision;
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
            Assert.IsTrue(hidden.Contains((PoolsTheBadDoctorV1.MissionId, PoolsTheBadDoctorV1.Approach)), "the hidden objectives are read");
            Assert.IsTrue(objectives.Count > 100, "the objectives are read");
            var missing = objectives.Select(entry => (entry.MissionId, entry.ObjectiveId))
                .Where(key => !marked.Contains(key) && !hidden.Contains(key)).OrderBy(key => key).ToArray();
            Assert.IsEmpty(missing, string.Join(", ", missing));
            Assert.IsFalse(marked.Any(hidden.Contains), "no marker on a hidden objective");
        }

        /// <summary>
        /// The markers of the marker pass stand at their objective's target: an NPC's or creature's place, a scene
        /// object, a spawn pool, an area or a door (radius as the data class gives it).
        /// </summary>
        [TestMethod]
        public void TheMarkersStandAtTheirObjectivesTargets()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var rows = harness.World.MissionIndicatorEntries.AsNoTracking()
                .Where(entry => entry.ContentRevision == PoolsMissionDataV1.Revision).ToList();
            var expected = new List<(uint Mission, uint Objective, double X, double Z, double Radius)>();
            void At(uint mission, uint objective, double x, double z, double radius = 8) => expected.Add((mission, objective, x, z, radius));

            foreach (var point in PoolsTestingThePoolsV1.Points)
                At(PoolsTestingThePoolsV1.MissionId, PoolsTestingThePoolsV1.Samples, point.At.X, point.At.Z);
            At(PoolsMadBomberV1.MissionId, PoolsMadBomberV1.Kill, PoolsMadBomberV1.BomberAt.X, PoolsMadBomberV1.BomberAt.Z,
                PoolsMadBomberV1.BomberRadius);
            At(PoolsThingsThatBlowUpV1.MissionId, PoolsThingsThatBlowUpV1.WayIn, PoolsThingsThatBlowUpV1.WallAt.X, PoolsThingsThatBlowUpV1.WallAt.Z);
            for (var index = 0; index < PoolsThingsThatBlowUpV1.BombsAt.Length; index++)
                At(PoolsThingsThatBlowUpV1.MissionId, PoolsThingsThatBlowUpV1.BombObjective(index), PoolsThingsThatBlowUpV1.BombsAt[index].X,
                    PoolsThingsThatBlowUpV1.BombsAt[index].Z);
            foreach (var (objective, at) in PoolsHardCoreV1.DrivesAt)
            {
                At(PoolsHardCoreV1.MissionId, PoolsHardCoreV1.Acquire, at.X, at.Z);
                At(PoolsHardCoreV1.MissionId, objective, at.X, at.Z);
            }
            Assert.HasCount(7, PoolsChemistry101V1.TechniciansAt, "a marker for each Thrax Technician pool");
            foreach (var at in PoolsChemistry101V1.TechniciansAt)
                foreach (var objective in new[] { PoolsChemistry101V1.Refined, PoolsChemistry101V1.Unrefined })
                    At(PoolsChemistry101V1.MissionId, objective, at.X, at.Z, PoolsChemistry101V1.TechnicianRadius);
            At(PoolsTheBadDoctorV1.MissionId, PoolsTheBadDoctorV1.Kill, PoolsTheBadDoctorV1.ToxicologistAt.X, PoolsTheBadDoctorV1.ToxicologistAt.Z,
                PoolsTheBadDoctorV1.LairRadius);
            foreach (var (objective, _, _, at) in PoolsBaneBattlePlansV1.Drives)
            {
                At(PoolsBaneBattlePlansV1.MissionId, objective, at.X, at.Z);
                At(PoolsBaneBattlePlansV1.MissionId, PoolsBaneBattlePlansV1.All, at.X, at.Z);
            }
            foreach (var (objective, boss) in new[]
                {
                    (2U, PoolsLamnaArmoryNpcsV1.GaiteAt), (3U, PoolsLamnaArmoryNpcsV1.FearceAt), (4U, PoolsLamnaArmoryNpcsV1.KwelAt)
                })
                At(PoolsOperationOverlordV1.MissionId, objective, boss.X, boss.Z, PoolsOperationOverlordV1.BossRadius);
            At(PoolsOperationOverlordV1.MissionId, PoolsOperationOverlordV1.KillOverlord, PoolsOperationOverlordV1.OverlordAt.X,
                PoolsOperationOverlordV1.OverlordAt.Z, PoolsOperationOverlordV1.BossRadius);
            foreach (var at in PoolsBlowThemAllUpV1.Lockers)
                At(PoolsBlowThemAllUpV1.MissionId, PoolsBlowThemAllUpV1.Destroy, at.X, at.Z);
            foreach (var (_, _, at) in PoolsCostOfDoingBusinessV1.Objects)
                At(PoolsCostOfDoingBusinessV1.MissionId, PoolsCostOfDoingBusinessV1.Destruction, at.X, at.Z);
            foreach (var (_, x, _, z) in PoolsRetreadCavesNpcsV1.MutatedPools)
                At(PoolsMiasmaMutationsV1.MissionId, PoolsMiasmaMutationsV1.Collect, x, z, PoolsMiasmaMutationsV1.PoolRadius);
            foreach (var (_, x, _, z) in PoolsRetreadCavesNpcsV1.MoxPools)
                At(PoolsMoxClearingV1.MissionId, PoolsMoxClearingV1.Kill, x, z, PoolsMoxClearingV1.PoolRadius);
            At(PoolsMoxOnBothOurHousesV1.MissionId, PoolsMoxOnBothOurHousesV1.Place, PoolsMoxOnBothOurHousesV1.BurnerAt.X,
                PoolsMoxOnBothOurHousesV1.BurnerAt.Z);
            At(PoolsMoxOnBothOurHousesV1.MissionId, PoolsMoxOnBothOurHousesV1.Kill, PoolsMoxOnBothOurHousesV1.AlphaAt.X,
                PoolsMoxOnBothOurHousesV1.AlphaAt.Z, PoolsMoxOnBothOurHousesV1.AlphaRadius);
            foreach (var at in PoolsDeadlyMoxinsV1.Spots)
                At(PoolsDeadlyMoxinsV1.MissionId, PoolsDeadlyMoxinsV1.PlaceFlares, at.X, at.Z);
            At(PoolsDeadlyMoxinsV1.MissionId, PoolsDeadlyMoxinsV1.KillMox, PoolsDeadlyMoxinsV1.MoxAt[0].X, PoolsDeadlyMoxinsV1.MoxAt[0].Z,
                PoolsDeadlyMoxinsV1.MoxRadius);
            At(PoolsMoxShouldntSmokeV1.MissionId, PoolsMoxShouldntSmokeV1.Talk, PoolsRetreadCavesNpcsV1.DoniusAt.X, PoolsRetreadCavesNpcsV1.DoniusAt.Z);
            At(PoolsMoxShouldntSmokeV1.MissionId, PoolsMoxShouldntSmokeV1.Carton, PoolsMoxShouldntSmokeV1.CrateAt.X,
                PoolsMoxShouldntSmokeV1.CrateAt.Z);
            var miles = PoolsRetreadCavesNpcsV1.MilesAt;
            foreach (var round in PoolsMiasmaTestingV1.Rounds)
            {
                At(PoolsMiasmaTestingV1.MissionId, round.Find, round.At.X, round.At.Z, PoolsMiasmaTestingV1.FindRadius);
                foreach (var objective in round.Next != 0 ? new[] { round.Return, round.Test, round.Next } : new[] { round.Return, round.Test })
                    At(PoolsMiasmaTestingV1.MissionId, objective, miles.X, miles.Z, PoolsMiasmaTestingV1.MilesRadius);
            }
            At(PoolsTotalSlaughterV1.MissionId, PoolsTotalSlaughterV1.Pump, PoolsTotalSlaughterV1.ComputerAt.X, PoolsTotalSlaughterV1.ComputerAt.Z);
            At(PoolsTotalSlaughterV1.MissionId, PoolsTotalSlaughterV1.Slaughter, PoolsTotalSlaughterV1.ComputerAt.X,
                PoolsTotalSlaughterV1.ComputerAt.Z, PoolsTotalSlaughterV1.WaveRadius);
            Assert.IsTrue(PoolsTotalSlaughterV1.Waves.SelectMany(wave => wave.At).All(at => Vector2.Distance(new(at.X, at.Z),
                new(PoolsTotalSlaughterV1.ComputerAt.X, PoolsTotalSlaughterV1.ComputerAt.Z)) < PoolsTotalSlaughterV1.WaveRadius),
                "every wave inside the slaughter's marker");
            At(PoolsStickYourNeckOutV1.MissionId, PoolsStickYourNeckOutV1.Speak, PoolsRetreadCavesNpcsV1.KaethiAt.X,
                PoolsRetreadCavesNpcsV1.KaethiAt.Z);
            At(PoolsTurpisTransportV1.MissionId, PoolsTurpisTransportV1.Complete, 147.8214, -607.3924, 6);   // map_link 26, written out
            foreach (var (objective, at, radius) in PoolsBuildingMaterialsV1.Markers)
                At(PoolsBuildingMaterialsV1.MissionId, objective, at.X, at.Z, radius);
            foreach (var (mission, bodies) in new[]
                {
                    (PoolsDeathPatrolV1.MissionId, PoolsDeathPatrolV1.Bodies), (PoolsMoreClipsAndTagsV1.MissionId, PoolsMoreClipsAndTagsV1.Bodies)
                })
                foreach (var (_, at) in bodies)
                {
                    At(mission, PoolsDeathPatrolV1.Tags, at.X, at.Z);
                    At(mission, PoolsDeathPatrolV1.Clips, at.X, at.Z);
                }
            var fort = PoolsSabotageTheTowerV1.FortAt;
            At(PoolsSabotageTheTowerV1.MissionId, PoolsSabotageTheTowerV1.Equipment, fort.X, fort.Z, PoolsSabotageTheTowerV1.FortRadius);
            Assert.IsTrue(PoolsSabotageTheTowerV1.Generators.Concat(PoolsSabotageTheTowerV1.Lockers).All(at =>
                Vector2.Distance(new(at.X, at.Z), new((float)fort.X, (float)fort.Z)) < PoolsSabotageTheTowerV1.FortRadius - 3),
                "every piece inside the fort's marker");
            var start = PoolsSupplyNpcsV1.Out[0];
            At(PoolsSupplyMasterV1.MissionId, PoolsSupplyMasterV1.Interrogate, start.X, start.Z, 60);
            foreach (var spot in PoolsMappingThePoolsV1.Spots)
                At(PoolsMappingThePoolsV1.MissionId, PoolsMappingThePoolsV1.Tracker, spot.X, spot.Z);

            foreach (var (mission, objective, x, z, radius) in expected)
                Assert.IsTrue(rows.Any(row => row.MissionId == mission && row.ObjectiveId == objective &&
                        Vector2.Distance(new((float)row.PosX, (float)row.PosZ), new((float)x, (float)z)) < 1f && row.Radius == radius),
                    $"{mission} objective {objective}'s marker at ({x}, {z}) radius {radius}");
            foreach (var mission in expected.Select(entry => entry.Mission).Distinct())
                foreach (var objective in expected.Where(entry => entry.Mission == mission).Select(entry => entry.Objective).Distinct())
                    Assert.HasCount(expected.Count(entry => entry.Mission == mission && entry.Objective == objective),
                        rows.Where(row => row.MissionId == mission && row.ObjectiveId == objective).ToList(), $"{mission} objective {objective}'s markers");
        }

        /// <summary>
        /// Pools_turpis_refinery takes out its six missions, the five givers and Hash with their pools and packages, and the
        /// Toxicologist; the refinery's older pools, its medical unit and the Retread Caves stay.
        /// </summary>
        [TestMethod]
        public void TurpisRefineryRollsBackWithItsMigration()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var world = harness.World;
            var missions = new uint[] { 882, 884, 885, 886, 1079, 1305 };
            bool Has(uint mission) => world.MissionContentDefinitionEntries.AsNoTracking()
                .Any(entry => entry.MissionId == mission && entry.ContentRevision == PoolsMissionDataV1.Revision);
            int Count(string sql) => world.Database.SqlQueryRaw<int>(sql).AsEnumerable().Single();
            (int, int, int, int, int, int) All() => (
                Count("SELECT COUNT(*) AS Value FROM creature WHERE id BETWEEN 760239 AND 760245"),
                Count("SELECT COUNT(*) AS Value FROM spawnpool WHERE id BETWEEN 760239 AND 760245"),
                Count("SELECT COUNT(*) AS Value FROM npc_package WHERE id BETWEEN 760239 AND 760245"),
                Count("SELECT COUNT(*) AS Value FROM creature_stat WHERE id BETWEEN 760239 AND 760245"),
                Count("SELECT COUNT(*) AS Value FROM mission_area WHERE mission_id IN (886, 1305)"),
                Count("SELECT COUNT(*) AS Value FROM mission_scene_binding WHERE mission_id IN (882, 884, 885, 886, 1079, 1305)"));
            int Stays() => Count("SELECT COUNT(*) AS Value FROM spawnpool WHERE map_context_id = 1429 AND id NOT BETWEEN 760239 AND 760245");

            Assert.IsTrue(missions.All(Has));
            Assert.AreEqual((7, 6, 6, 1, 2, 5), All(), "1305 has no scene; only the Toxicologist has a stat row");
            var stays = Stays();

            world.GetService<IMigrator>().Migrate(RetreadCaves);
            Assert.IsFalse(missions.Any(Has));
            Assert.AreEqual((0, 0, 0, 0, 0, 0), All());
            Assert.AreEqual(0, Count("SELECT COUNT(*) AS Value FROM creature_appearance WHERE id BETWEEN 760239 AND 760245"));
            Assert.IsFalse(world.MissionEvidenceEntries.Any(entry => missions.Contains(entry.MissionId)));
            Assert.AreEqual(stays, Stays(), "the refinery's older pools stay");
            Assert.IsTrue(Has(PoolsMadBomberV1.MissionId) && Has(PoolsLookingForLogosV1.MissionId));

            world.GetService<IMigrator>().Migrate(TurpisRefinery);
            Assert.IsTrue(missions.All(Has));
            Assert.AreEqual((7, 6, 6, 1, 2, 5), All());
        }

        /// <summary>
        /// Pools_retread_caves takes out its nine missions, the four Retreads, the mission creatures, the Mox and mutated
        /// miasma pools and the Mad Bomber's pistol; the caves' older pools, Amee and Retread Planning stay.
        /// </summary>
        [TestMethod]
        public void TheRetreadCavesRollBackWithTheirMigration()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var world = harness.World;
            var missions = new uint[] { 1329, 1132, 1135, 1133, 1134, 1131, 1136, 1138, 856 };
            bool Has(uint mission) => world.MissionContentDefinitionEntries.AsNoTracking()
                .Any(entry => entry.MissionId == mission && entry.ContentRevision == PoolsMissionDataV1.Revision);
            int Count(string sql) => world.Database.SqlQueryRaw<int>(sql).AsEnumerable().Single();
            (int, int, int, int, int, int) All() => (
                Count("SELECT COUNT(*) AS Value FROM creature WHERE id BETWEEN 760221 AND 760230"),
                Count("SELECT COUNT(*) AS Value FROM spawnpool WHERE id BETWEEN 760221 AND 760238"),
                Count("SELECT COUNT(*) AS Value FROM npc_package WHERE id BETWEEN 760221 AND 760230"),
                Count("SELECT COUNT(*) AS Value FROM creature_stat WHERE id BETWEEN 760221 AND 760230"),
                Count("SELECT COUNT(*) AS Value FROM creature_action WHERE id = 760040"),
                Count("SELECT COUNT(*) AS Value FROM mission_scene_binding WHERE mission_id IN (1329, 1132, 1135, 1133, 1134, 1131, 1136, 1138, 856)"));
            int Stays() => Count("SELECT COUNT(*) AS Value FROM spawnpool WHERE map_context_id = 1694 AND id NOT BETWEEN 760221 AND 760238");

            Assert.IsTrue(missions.All(Has));
            Assert.AreEqual((10, 13, 4, 5, 1, 7), All(), "1329 and 1132 have no scene; the docile miasma has no stat row");
            var stays = Stays();

            world.GetService<IMigrator>().Migrate(LamnaArmory);
            Assert.IsFalse(missions.Any(Has));
            Assert.AreEqual((0, 0, 0, 0, 0, 0), All());
            Assert.AreEqual(0, Count("SELECT COUNT(*) AS Value FROM creature_appearance WHERE id BETWEEN 760221 AND 760230"));
            Assert.IsFalse(world.MissionEvidenceEntries.Any(entry => missions.Contains(entry.MissionId)));
            Assert.AreEqual(stays, Stays(), "the caves' older pools stay");
            Assert.IsTrue(Has(PoolsRetreadPlanningV1.MissionId) && Has(PoolsBlowThemAllUpV1.MissionId));
            Assert.AreEqual(1, Count($"SELECT COUNT(*) AS Value FROM creature WHERE id = {PoolsRetreadCampNpcsV1.Amee}"));

            world.GetService<IMigrator>().Migrate(RetreadCaves);
            Assert.IsTrue(missions.All(Has));
            Assert.AreEqual((10, 13, 4, 5, 1, 7), All());
        }

        /// <summary>
        /// Pools_lamna_armory takes out its three missions, its givers, bosses and their pools, Fearce's attacks; the Armory's
        /// medics and Thrax pool and the spy chain stay.
        /// </summary>
        [TestMethod]
        public void TheLamnaArmoryRollsBackWithItsMigration()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var world = harness.World;
            var missions = new uint[] { 896, 898, 903 };
            bool Has(uint mission) => world.MissionContentDefinitionEntries.AsNoTracking()
                .Any(entry => entry.MissionId == mission && entry.ContentRevision == PoolsMissionDataV1.Revision);
            int Count(string sql) => world.Database.SqlQueryRaw<int>(sql).AsEnumerable().Single();
            (int, int, int, int, int) All() => (
                Count("SELECT COUNT(*) AS Value FROM creature WHERE id BETWEEN 760213 AND 760220"),
                Count("SELECT COUNT(*) AS Value FROM spawnpool WHERE id BETWEEN 760213 AND 760220"),
                Count("SELECT COUNT(*) AS Value FROM npc_package WHERE id BETWEEN 760213 AND 760220"),
                Count("SELECT COUNT(*) AS Value FROM creature_action WHERE id IN (760038, 760039)"),
                Count("SELECT COUNT(*) AS Value FROM mission_scene_binding WHERE mission_id IN (896, 898, 903)"));
            int Stays() => Count("SELECT COUNT(*) AS Value FROM spawnpool WHERE map_context_id = 1465 AND id NOT BETWEEN 760213 AND 760220");

            Assert.IsTrue(missions.All(Has));
            Assert.AreEqual((8, 6, 3, 2, 3), All());
            var stays = Stays();

            world.GetService<IMigrator>().Migrate(SpyChain);
            Assert.IsFalse(missions.Any(Has));
            Assert.AreEqual((0, 0, 0, 0, 0), All());
            Assert.AreEqual(0, Count("SELECT COUNT(*) AS Value FROM creature_appearance WHERE id BETWEEN 760213 AND 760220"));
            Assert.AreEqual(0, Count("SELECT COUNT(*) AS Value FROM creature_stat WHERE id BETWEEN 760213 AND 760220"));
            Assert.IsFalse(world.MissionEvidenceEntries.Any(entry => missions.Contains(entry.MissionId)));
            Assert.AreEqual(stays, Stays(), "the medics and the Thrax pool stay");
            Assert.IsTrue(Has(PoolsSpyNetworkingV1.MissionId));

            world.GetService<IMigrator>().Migrate(LamnaArmory);
            Assert.IsTrue(missions.All(Has));
            Assert.AreEqual((8, 6, 3, 2, 3), All());
        }

        /// <summary>
        /// Pools_spy_chain takes out its three missions, Sheen's package and her stand-in; Sheen, Amee and Part II stay.
        /// </summary>
        [TestMethod]
        public void TheSpyChainRollsBackWithItsMigration()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var world = harness.World;
            var missions = new uint[] { 1331, 1368, 1590 };
            bool Has(uint mission) => world.MissionContentDefinitionEntries.AsNoTracking()
                .Any(entry => entry.MissionId == mission && entry.ContentRevision == PoolsMissionDataV1.Revision);
            int Count(string sql) => world.Database.SqlQueryRaw<int>(sql).AsEnumerable().Single();
            (int, int, int, int) All() => (
                Count("SELECT COUNT(*) AS Value FROM creature WHERE id = 760212"),
                Count("SELECT COUNT(*) AS Value FROM creature_appearance WHERE id = 760212"),
                Count("SELECT COUNT(*) AS Value FROM npc_package WHERE id IN (760212, 510197)"),
                Count("SELECT COUNT(*) AS Value FROM mission_scene_binding WHERE mission_id IN (1331, 1368, 1590)"));

            Assert.IsTrue(missions.All(Has));
            Assert.AreEqual((1, Count("SELECT COUNT(*) AS Value FROM creature_appearance WHERE id = 510197"), 2, 3), All());

            world.GetService<IMigrator>().Migrate(SupplyChain);
            Assert.IsFalse(missions.Any(Has));
            Assert.AreEqual((0, 0, 0, 0), All());
            Assert.IsFalse(world.MissionEvidenceEntries.Any(entry => missions.Contains(entry.MissionId)));
            Assert.IsTrue(Has(PoolsRetreadPlanningPartTwoV1.MissionId) && Has(PoolsSpoilsOfWarV1.MissionId));
            Assert.AreEqual(1, Count("SELECT COUNT(*) AS Value FROM creature WHERE id = 510197"), "Sheen stays");

            world.GetService<IMigrator>().Migrate(SpyChain);
            Assert.IsTrue(missions.All(Has));
            Assert.AreEqual(2, All().Item3);
        }

        /// <summary>
        /// Pools_supply_chain takes out its four missions, Nakrinx, his bodyguards, their squad and beat, and Chester's package;
        /// Chester, Riggins and Death Patrol stay.
        /// </summary>
        [TestMethod]
        public void TheSupplyChainRollsBackWithItsMigration()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var world = harness.World;
            var missions = new uint[] { 1322, 1323, 1324, 1326 };
            bool Has(uint mission) => world.MissionContentDefinitionEntries.AsNoTracking()
                .Any(entry => entry.MissionId == mission && entry.ContentRevision == PoolsMissionDataV1.Revision);
            int Count(string sql) => world.Database.SqlQueryRaw<int>(sql).AsEnumerable().Single();
            (int, int, int, int, int, int) All() => (
                Count("SELECT COUNT(*) AS Value FROM creature WHERE id IN (760210, 760211)"),
                Count("SELECT COUNT(*) AS Value FROM creature_stat WHERE id IN (760210, 760211)"),
                Count("SELECT COUNT(*) AS Value FROM npc_package WHERE id IN (760210, 760211, 510198)"),
                Count("SELECT COUNT(*) AS Value FROM npc_squad_type WHERE id = 760210") +
                    Count("SELECT COUNT(*) AS Value FROM npc_squad_path WHERE id = 760210") +
                    Count("SELECT COUNT(*) AS Value FROM npc_squad_spawn WHERE id = 760210"),
                Count("SELECT COUNT(*) AS Value FROM npc_squad_path_point WHERE path_id = 760210") +
                    Count("SELECT COUNT(*) AS Value FROM npc_squad_member WHERE type_id = 760210"),
                Count("SELECT COUNT(*) AS Value FROM mission_scene_binding WHERE mission_id IN (1322, 1323, 1324, 1326)"));

            Assert.IsTrue(missions.All(Has));
            Assert.AreEqual((2, 2, 2, 3, 17, 3), All(), "1326 has no scene (1322 its hidden kill count's)");

            world.GetService<IMigrator>().Migrate(RetreadChain);
            Assert.IsFalse(missions.Any(Has));
            Assert.AreEqual((0, 0, 0, 0, 0, 0), All());
            Assert.IsFalse(world.MissionEvidenceEntries.Any(entry => missions.Contains(entry.MissionId)));
            Assert.IsTrue(Has(PoolsDeathPatrolV1.MissionId) && Has(PoolsRetreadPlanningPartTwoV1.MissionId));
            Assert.AreEqual(1, Count("SELECT COUNT(*) AS Value FROM creature WHERE id = 510198"), "Chester stays");
            Assert.AreEqual(1, Count($"SELECT COUNT(*) AS Value FROM creature WHERE id = {PoolsSnakePitNpcsV1.Riggins}"));

            world.GetService<IMigrator>().Migrate(SupplyChain);
            Assert.IsTrue(missions.All(Has));
            Assert.AreEqual((2, 2, 2, 3, 17, 3), All());
        }

        /// <summary>
        /// Pools_retread_chain takes out its five missions and the four Retreads; Amee, the Hermit and The Thirsty Hermit stay.
        /// </summary>
        [TestMethod]
        public void TheRetreadChainRollsBackWithItsMigration()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var world = harness.World;
            var missions = new uint[] { 560, 561, 562, 563, 1330 };
            bool Has(uint mission) => world.MissionContentDefinitionEntries.AsNoTracking()
                .Any(entry => entry.MissionId == mission && entry.ContentRevision == PoolsMissionDataV1.Revision);
            int Count(string sql) => world.Database.SqlQueryRaw<int>(sql).AsEnumerable().Single();
            int Rows(string table) => Count($"SELECT COUNT(*) AS Value FROM {table} WHERE id BETWEEN " +
                $"{PoolsRetreadCampNpcsV1.McKormic} AND {PoolsRetreadCampNpcsV1.Rickert}");
            (int, int, int, int, int) All() => (Rows("creature"), Rows("spawnpool"), Rows("npc_package"), Rows("creature_appearance"),
                Count("SELECT COUNT(*) AS Value FROM mission_scene_binding WHERE mission_id IN (560, 561, 562, 563, 1330)"));

            Assert.IsTrue(missions.All(Has));
            Assert.AreEqual((4, 4, 4, 24, 4), All(), "1330 has no scene");

            world.GetService<IMigrator>().Migrate(BaneChain);
            Assert.IsFalse(missions.Any(Has));
            Assert.AreEqual((0, 0, 0, 0, 0), All());
            Assert.IsFalse(world.MissionEvidenceEntries.Any(entry => missions.Contains(entry.MissionId)));
            Assert.IsFalse(world.Set<MissionChannelPolicyEntry>().AsNoTracking().Any(entry => entry.MissionId == 563 &&
                entry.ContentRevision == PoolsMissionDataV1.Revision), "563's radio offer goes");
            Assert.IsTrue(Has(PoolsThirstyHermitV1.MissionId) && Has(937));
            Assert.AreEqual(1, Count($"SELECT COUNT(*) AS Value FROM creature WHERE id = {PoolsRetreadCampNpcsV1.Amee}"));
            Assert.AreEqual(1, Count($"SELECT COUNT(*) AS Value FROM creature WHERE id = {PlateauBaileyNpcsV1.Hermit}"));

            world.GetService<IMigrator>().Migrate(RetreadChain);
            Assert.IsTrue(missions.All(Has));
            Assert.AreEqual((4, 4, 4, 24, 4), All());
        }

        /// <summary>
        /// Pools_bane_chain takes out its three missions, the obelisk and the Linker; Bane Investigation and Feldman stay.
        /// </summary>
        [TestMethod]
        public void TheBaneChainRollsBackWithItsMigration()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var world = harness.World;
            bool Has(uint mission) => world.MissionContentDefinitionEntries.AsNoTracking()
                .Any(entry => entry.MissionId == mission && entry.ContentRevision == PoolsMissionDataV1.Revision);
            int Count(string sql) => world.Database.SqlQueryRaw<int>(sql).AsEnumerable().Single();
            (int, int, int, int, int) Rows() => (
                Count($"SELECT COUNT(*) AS Value FROM world_destructible WHERE id = {PoolsObeliskV1.Id}"),
                Count($"SELECT COUNT(*) AS Value FROM creature WHERE id = {PoolsLinkerV1.Linker}"),
                Count($"SELECT COUNT(*) AS Value FROM creature_stat WHERE id = {PoolsLinkerV1.Linker}"),
                Count($"SELECT COUNT(*) AS Value FROM creature_action WHERE id BETWEEN {PoolsLinkerV1.FirstAction} AND {PoolsLinkerV1.LastAction}"),
                Count("SELECT COUNT(*) AS Value FROM mission_scene_binding WHERE mission_id IN (937, 938, 941)"));

            Assert.IsTrue(Has(937) && Has(938) && Has(941));
            Assert.AreEqual((1, 1, 1, 4, 2), Rows(), "941 has no scene");

            world.GetService<IMigrator>().Migrate(Contacts);
            Assert.IsFalse(Has(937) || Has(938) || Has(941));
            Assert.AreEqual((0, 0, 0, 0, 0), Rows());
            Assert.IsFalse(world.MissionEvidenceEntries.Any(entry => new uint[] { 937, 938, 941 }.Contains(entry.MissionId)));
            Assert.IsTrue(Has(932));
            Assert.AreEqual(1, Count($"SELECT COUNT(*) AS Value FROM creature WHERE id = {PoolsSnakePitContactsV1.Feldman}"));

            world.GetService<IMigrator>().Migrate(BaneChain);
            Assert.IsTrue(Has(937) && Has(938) && Has(941));
            Assert.AreEqual((1, 1, 1, 4, 2), Rows());
        }

        /// <summary>
        /// Pools_snake_pit_contacts takes out its three missions and Timlin, Allen and Feldman; Death Patrol's missions stay.
        /// </summary>
        [TestMethod]
        public void TheSnakePitContactsRollBackWithTheirMigration()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var world = harness.World;
            bool Has(uint mission) => world.MissionContentDefinitionEntries.AsNoTracking()
                .Any(entry => entry.MissionId == mission && entry.ContentRevision == PoolsMissionDataV1.Revision);
            int Rows(string table) => world.Database.SqlQueryRaw<int>(
                $"SELECT COUNT(*) AS Value FROM {table} WHERE id IN ({PoolsSnakePitContactsV1.Timlin}, {PoolsSnakePitContactsV1.Allen}, " +
                $"{PoolsSnakePitContactsV1.Feldman})").AsEnumerable().Single();
            int Scenes() => world.Database.SqlQueryRaw<int>(
                "SELECT COUNT(*) AS Value FROM mission_scene_binding WHERE mission_id IN (1308, 1025, 932)").AsEnumerable().Single();

            Assert.IsTrue(Has(1308) && Has(1025) && Has(932));
            Assert.AreEqual((3, 3, 3, 18), (Rows("creature"), Rows("spawnpool"), Rows("npc_package"), Rows("creature_appearance")));
            Assert.AreEqual(3, Scenes(), "one scene each; 1308's holds only its fact objective");

            world.GetService<IMigrator>().Migrate(DeathPatrol);
            Assert.IsFalse(Has(1308) || Has(1025) || Has(932));
            Assert.AreEqual((0, 0, 0, 0), (Rows("creature"), Rows("spawnpool"), Rows("npc_package"), Rows("creature_appearance")));
            Assert.AreEqual(0, Scenes());
            Assert.IsFalse(world.MissionEvidenceEntries.Any(entry => new uint[] { 1308, 1025, 932 }.Contains(entry.MissionId)));
            Assert.IsTrue(Has(1315) && Has(1321) && Has(1328));

            world.GetService<IMigrator>().Migrate(Contacts);
            Assert.IsTrue(Has(1308) && Has(1025) && Has(932));
        }

        /// <summary>
        /// Pools_death_patrol takes out its three missions and puts Snake back at his Plateau-PR place; the Snake Pit's two
        /// missions stay.
        /// </summary>
        [TestMethod]
        public void DeathPatrolRollsBackWithItsMigrationAndSnakeGoesBack()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var world = harness.World;
            bool Has(uint mission) => world.MissionContentDefinitionEntries.AsNoTracking()
                .Any(entry => entry.MissionId == mission && entry.ContentRevision == PoolsMissionDataV1.Revision);
            (double, double, double) Snake()
            {
                var row = world.SpawnPoolEntries.AsNoTracking().Single(entry => entry.Id == PlateauSnakePitV1.Snake);
                return (System.Math.Round(row.PosX, 2), System.Math.Round(row.PosY, 2), System.Math.Round(row.PosZ, 2));
            }
            // South Of The Border's marker at Snake goes with him.
            (double, double, double) Marker()
            {
                // Its place only: a world rolled back before Mission_indicator_maps has no map_context_id.
                var row = world.MissionIndicatorEntries.AsNoTracking().Where(entry => entry.MissionId == PlateauSouthOfTheBorderV1.MissionId)
                    .Select(entry => new { entry.PosX, entry.PosY, entry.PosZ }).Single();
                return (System.Math.Round(row.PosX, 2), System.Math.Round(row.PosY, 2), System.Math.Round(row.PosZ, 2));
            }

            Assert.IsTrue(Has(1315) && Has(1321) && Has(1328));
            Assert.AreEqual(PoolsWashingtonPlaceV1.At, Snake());
            Assert.AreEqual(PoolsWashingtonPlaceV1.At, Marker());

            world.GetService<IMigrator>().Migrate(SnakePit);
            Assert.IsFalse(Has(1315) || Has(1321) || Has(1328));
            Assert.IsFalse(world.Set<MissionRepeatPolicyEntry>().AsNoTracking().Any(entry => entry.ContentRevision == PoolsMissionDataV1.Revision));
            Assert.AreEqual(PlateauSnakePitV1.SnakeAt, Snake());
            Assert.IsTrue(Has(1314) && Has(1066));
            Assert.AreEqual(PlateauSnakePitV1.SnakeAt, Marker());

            world.GetService<IMigrator>().Migrate(DeathPatrol);
            Assert.IsTrue(Has(1315) && Has(1321) && Has(1328));
            Assert.AreEqual(PoolsWashingtonPlaceV1.At, Snake());
        }

        /// <summary>Pools_snake_pit takes out its missions and Riggins and Hester; Retreads to Retreads stays.</summary>
        [TestMethod]
        public void TheSnakePitRollsBackWithItsMigration()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var world = harness.World;
            bool Has(uint mission) => world.MissionContentDefinitionEntries.AsNoTracking()
                .Any(entry => entry.MissionId == mission && entry.ContentRevision == PoolsMissionDataV1.Revision);
            int Rows(string table) => world.Database.SqlQueryRaw<int>(
                $"SELECT COUNT(*) AS Value FROM {table} WHERE id IN ({PoolsSnakePitNpcsV1.Riggins}, {PoolsSnakePitNpcsV1.Hester})").AsEnumerable().Single();

            Assert.IsTrue(Has(PoolsBuildingMaterialsV1.MissionId) && Has(PoolsMedicinalMeasuresV1.MissionId));
            Assert.AreEqual((2, 2, 2, 12), (Rows("creature"), Rows("spawnpool"), Rows("npc_package"), Rows("creature_appearance")));

            world.GetService<IMigrator>().Migrate(Before);
            Assert.IsFalse(world.MissionContentDefinitionEntries.Any(entry => entry.ContentRevision == PoolsMissionDataV1.Revision));
            Assert.IsFalse(world.MissionRewardItemEntries.Any(entry => entry.ContentRevision == PoolsMissionDataV1.Revision));
            Assert.IsFalse(world.MissionEvidenceEntries.Any(entry => entry.ContentRevision == PoolsMissionDataV1.Revision));
            Assert.IsFalse(world.MissionScenarioEntries.Any(entry => entry.ContentRevision == PoolsMissionDataV1.Revision));
            Assert.AreEqual((0, 0, 0, 0), (Rows("creature"), Rows("spawnpool"), Rows("npc_package"), Rows("creature_appearance")));
            Assert.IsTrue(world.MissionContentDefinitionEntries.Any(entry => entry.MissionId == PlateauRetreadsToRetreadsV1.MissionId &&
                entry.ContentRevision == PlateauMissionDataV1.Revision));

            world.GetService<IMigrator>().Migrate(SnakePit);
            Assert.IsTrue(Has(PoolsBuildingMaterialsV1.MissionId) && Has(PoolsMedicinalMeasuresV1.MissionId));
        }
    }
}
