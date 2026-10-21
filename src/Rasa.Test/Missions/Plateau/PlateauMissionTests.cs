using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Missions.Plateau
{
    using Rasa.Services.Preloader.Missions.Plateau;
    using Rasa.Structures;
    using Rasa.Structures.World;
    using Rasa.Test.Missions.Wilderness;

    /// <summary>
    /// The Plateau missions (PlateauMissionDataV1): Colonel Bosley's Giant Killer (939,
    /// Plateau_giant_killer) and Incommunicado (1040, Plateau_incommunicado) with its three recon
    /// squads (PlateauReconSquadsV1); the commanders' Canyon Sweep (1045, Plateau_canyon_sweep, with
    /// the Maligo Elite Guards of PlateauMaligoGuardsV1 and the flags of PlateauIncommunicadoFlagsV1),
    /// Blueprint For Victory (1046, Plateau_blueprint_for_victory) and Toxic Detox (1053,
    /// Plateau_toxic_detox); the Scrounger's chain (1251, 1015, 1017); New Velon Village's Graveyard Shift
    /// (933, Plateau_graveyard_shift), Village Of The Damned (1981, Plateau_village_of_the_damned), The
    /// Book Of Velonar (930, Plateau_book_of_velonar) and Going Postal (1034, Plateau_going_postal).
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class PlateauMissionTests
    {
        /// <summary>The Plateau missions the World migrations enable, for the tests that pin the enabled set.</summary>
        internal static readonly uint[] MissionIds =
        {
            PlateauGiantKillerV1.MissionId, PlateauIncommunicadoV1.MissionId, PlateauCanyonSweepV1.MissionId,
            PlateauBlueprintForVictoryV1.MissionId, PlateauToxicDetoxV1.MissionId, PlateauComicReliefV1.MissionId,
            PlateauScavengerHuntV1.MissionId, PlateauStalkMarketV1.MissionId, PlateauGraveyardShiftV1.MissionId,
            PlateauVillageOfTheDamnedV1.MissionId, PlateauBookOfVelonarV1.MissionId, PlateauGoingPostalV1.MissionId
        };

        /// <summary>
        /// Giant Killer's two indicators, Incommunicado's three, Canyon Sweep's one, Blueprint For Victory's two,
        /// Toxic Detox's one, Comic Relief's three, The Stalk Market's three; Scavenger Hunt has none.
        /// Graveyard Shift's two, Village Of The Damned's, The Book Of Velonar's and Going Postal's one each.
        /// </summary>
        internal const int Indicators = 20;

        private const string Before = "20270123000000_Thunderhead_base_teleporters";
        private const string GiantKiller = "20270125000000_Plateau_giant_killer";
        private const string Incommunicado = "20270126000000_Plateau_incommunicado";
        private const string CanyonSweep = "20270127000000_Plateau_canyon_sweep";
        private const string BlueprintForVictory = "20270128000000_Plateau_blueprint_for_victory";
        private const string ToxicDetox = "20270129000000_Plateau_toxic_detox";
        private const string ComicRelief = "20270130000000_Plateau_comic_relief";
        private const string ScavengerHunt = "20270131000000_Plateau_scavenger_hunt";
        private const string StalkMarket = "20270201000000_Plateau_stalk_market";
        private const string GraveyardShift = "20270202000000_Plateau_graveyard_shift";
        private const string VillageOfTheDamned = "20270203000000_Plateau_village_of_the_damned";
        private const string BookOfVelonar = "20270204000000_Plateau_book_of_velonar";

        [TestMethod]
        public void TheMissionsAreEnabledAndOperationalAtTheirGiversAndReceiversAndLevels()
        {
            var expected = new (uint Mission, uint Giver, uint Receiver)[]
            {
                (PlateauGiantKillerV1.MissionId, PlateauGiantKillerV1.Bosley, PlateauGiantKillerV1.Bosley),
                (PlateauIncommunicadoV1.MissionId, PlateauGiantKillerV1.Bosley, PlateauGiantKillerV1.Bosley),
                (PlateauCanyonSweepV1.MissionId, PlateauReconSquadsV1.Carvelle, PlateauReconSquadsV1.Carvelle),
                (PlateauBlueprintForVictoryV1.MissionId, PlateauReconSquadsV1.Petrie, PlateauGiantKillerV1.Bosley),
                (PlateauToxicDetoxV1.MissionId, PlateauReconSquadsV1.Locke, PlateauReconSquadsV1.Locke),
                (PlateauComicReliefV1.MissionId, PlateauScroungerNpcsV1.OBrien, PlateauScroungerNpcsV1.OBrien),
                (PlateauScavengerHuntV1.MissionId, PlateauScroungerNpcsV1.Scarpelo, PlateauScroungerNpcsV1.Scarpelo),
                (PlateauStalkMarketV1.MissionId, PlateauScroungerNpcsV1.Scarpelo, PlateauScroungerNpcsV1.Scarpelo),
                (PlateauGraveyardShiftV1.MissionId, PlateauGraveyardShiftV1.Talisen, PlateauGraveyardShiftV1.Talisen),
                (PlateauVillageOfTheDamnedV1.MissionId, PlateauVillageOfTheDamnedV1.Sampei, PlateauVillageOfTheDamnedV1.Sampei),
                (PlateauBookOfVelonarV1.MissionId, PlateauVillageOfTheDamnedV1.Sampei, PlateauVillageOfTheDamnedV1.Sampei),
                (PlateauGoingPostalV1.MissionId, PlateauGoingPostalV1.Kaely, PlateauGoingPostalV1.Kaely)
            };
            using var harness = WildernessRuntimeTestHarness.Create();
            var enabled = harness.World.MissionContentDefinitionEntries.AsNoTracking()
                .Where(entry => entry.Enabled && entry.ContentRevision == PlateauMissionDataV1.Revision).ToArray();
            CollectionAssert.AreEquivalent(MissionIds, enabled.Select(entry => entry.MissionId).ToArray());
            foreach (var (mission, giver, receiver) in expected)
            {
                var entry = enabled.Single(row => row.MissionId == mission);
                Assert.AreEqual<uint?>(giver, entry.GiverId, $"{mission} giver");
                Assert.AreEqual<uint?>(receiver, entry.ReceiverId, $"{mission} receiver");
                Assert.AreEqual<uint?>(mission switch { 1251 or 1015 or 1017 => 31U, 930 => 32U, _ => 30U }, entry.Level,
                    $"{mission} level");
                Assert.AreEqual<uint?>(PlateauMissionDataV1.CategoryId, entry.CategoryId);
            }

            Assert.IsFalse(harness.Manager.LoadMissions().BlocksReadiness);
            foreach (var id in MissionIds)
                Assert.IsTrue(harness.Manager.LoadedMissions[id].IsOperational,
                    $"{id}: {harness.Manager.LoadedMissions[id].OperationalDiagnostic}");
        }

        /// <summary>
        /// The client's own texts, as WildernessContentInventoryTests checks the Wilderness ones: the
        /// ids are written out here from the client tables (missionconversation, missionobjective),
        /// not taken from the data classes, so a wrong id in those fails. Only Canyon Sweep, Toxic
        /// Detox, Graveyard Shift and Village Of The Damned have a counter text in the client.
        /// </summary>
        [TestMethod]
        public void TitlesAndObjectivesUseTheClientsTexts()
        {
            var expected = new (uint Mission, uint Title, (uint Objective, uint Name, uint Body, uint? Counter)[] Objectives)[]
            {
                (939, 5104, new (uint, uint, uint, uint?)[] { (1, 5108, 6354, null), (2, 5109, 6355, null) }),
                (1040, 5823, new (uint, uint, uint, uint?)[] { (1, 5832, 6368, null), (2, 5833, 6369, null), (3, 5834, 6370, null) }),
                (1045, 5875, new (uint, uint, uint, uint?)[] { (1, 5879, 6333, 5880) }),
                (1046, 5881, new (uint, uint, uint, uint?)[] { (1, 5922, 6311, null), (3, 6024, 6312, null) }),
                (1053, 5916, new (uint, uint, uint, uint?)[] { (1, 5920, 6474, 5921) }),
                (1251, 10008, new (uint, uint, uint, uint?)[]
                {
                    (1, 10015, 10016, null), (7, 10033, 10034, null), (8, 10013, 10014, null), (9, 10018, 10019, null),
                    (10, 10020, 10021, null)
                }),
                (1015, 5555, new (uint, uint, uint, uint?)[] { (3, 5563, 6445, null), (4, 5564, 6446, null) }),
                (1017, 5587, new (uint, uint, uint, uint?)[] { (1, 5605, 6458, null), (2, 5606, 6459, null), (3, 5607, 6460, null) }),
                (933, 5016, new (uint, uint, uint, uint?)[] { (1, 5021, 6365, 5022), (2, 5023, 6366, 5024) }),
                (1981, 20858, new (uint, uint, uint, uint?)[] { (1, 20863, 20864, 20865) }),
                (930, 4983, new (uint, uint, uint, uint?)[] { (1, 4987, 6330, null) }),
                (1034, 5776, new (uint, uint, uint, uint?)[] { (1, 5785, 6363, null), (2, 8093, 8094, null) })
            };
            using var harness = WildernessRuntimeTestHarness.Create();
            foreach (var (mission, title, objectives) in expected)
            {
                Assert.AreEqual(title, harness.World.MissionContentDefinitionEntries.AsNoTracking()
                    .Single(entry => entry.MissionId == mission && entry.ContentRevision == PlateauMissionDataV1.Revision)
                    .ClientNameTextId, $"{mission} title");
                var actual = harness.World.MissionObjectiveDefinitionEntries.AsNoTracking()
                    .Where(entry => entry.MissionId == mission && entry.ContentRevision == PlateauMissionDataV1.Revision).ToArray();
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
        }

        /// <summary>
        /// Incommunicado follows Giant Killer; each commander's mission follows his own delivery: the
        /// flag that Incommunicado's delivery objective sets (action 3, SetPlayerFlag to 1). Scavenger
        /// Hunt follows the Scrounger's talk in Comic Relief (objective 10, action 1, the same way);
        /// The Stalk Market follows Scavenger Hunt. New Velon Village's four have no requirement.
        /// </summary>
        [TestMethod]
        public void EachMissionRequiresTheMissionOrDeliveryBeforeIt()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var prerequisites = harness.World.MissionPrerequisiteEntries.AsNoTracking()
                .Where(entry => entry.ContentRevision == PlateauMissionDataV1.Revision).ToArray();
            Assert.HasCount(6, prerequisites);
            var incommunicado = prerequisites.Single(entry => entry.MissionId == PlateauIncommunicadoV1.MissionId);
            Assert.AreEqual(MissionPrerequisiteKind.MissionCompleted, incommunicado.Kind);
            Assert.AreEqual<uint?>(PlateauGiantKillerV1.MissionId, incommunicado.RequiredMissionId);

            var deliveries = harness.World.MissionActionEntries.AsNoTracking()
                .Where(entry => entry.ContentRevision == PlateauMissionDataV1.Revision &&
                    entry.MissionId == PlateauIncommunicadoV1.MissionId && entry.Kind == MissionActionKind.SetPlayerFlag)
                .ToArray();
            foreach (var (mission, objective, flag) in new (uint, uint, uint)[]
            {
                (1045, 1, 1040001), (1046, 2, 1040002), (1053, 3, 1040003)
            })
            {
                var prerequisite = prerequisites.Single(entry => entry.MissionId == mission);
                Assert.AreEqual(MissionPrerequisiteKind.PlayerFlagValue, prerequisite.Kind, $"{mission}");
                Assert.AreEqual<uint?>(flag, prerequisite.PlayerFlagId, $"{mission} flag");
                Assert.AreEqual<uint?>(1, prerequisite.PlayerFlagValue, $"{mission} flag value");
                var action = deliveries.Single(entry => entry.ObjectiveId == objective);
                Assert.AreEqual((3U, (uint?)flag, (uint?)1), (action.ActionId, action.PlayerFlagId, action.PlayerFlagValue),
                    $"Incommunicado delivery {objective}");
            }
            Assert.HasCount(3, deliveries);

            var scavengerHunt = prerequisites.Single(entry => entry.MissionId == PlateauScavengerHuntV1.MissionId);
            Assert.AreEqual((MissionPrerequisiteKind.PlayerFlagValue, (uint?)1251001, (uint?)1),
                (scavengerHunt.Kind, scavengerHunt.PlayerFlagId, scavengerHunt.PlayerFlagValue));
            var scrounger = harness.World.MissionActionEntries.AsNoTracking().Single(entry =>
                entry.ContentRevision == PlateauMissionDataV1.Revision && entry.MissionId == PlateauComicReliefV1.MissionId &&
                entry.Kind == MissionActionKind.SetPlayerFlag);
            Assert.AreEqual((10U, 1U, (uint?)1251001, (uint?)1),
                (scrounger.ObjectiveId, scrounger.ActionId, scrounger.PlayerFlagId, scrounger.PlayerFlagValue));
            var stalkMarket = prerequisites.Single(entry => entry.MissionId == PlateauStalkMarketV1.MissionId);
            Assert.AreEqual((MissionPrerequisiteKind.MissionCompleted, (uint?)1015),
                (stalkMarket.Kind, stalkMarket.RequiredMissionId));
            foreach (var mission in new uint[] { 933, 1981, 930, 1034 })
                Assert.IsFalse(prerequisites.Any(entry => entry.MissionId == mission), $"{mission}");
        }

        /// <summary>
        /// The missions' sources and how far each is trusted: the client's tables 1.0, TaRapedia 0.5
        /// (0.4 for Blueprint For Victory, whose page gives no drop rate, and for Scavenger Hunt and Going
        /// Postal, stub pages), and what this repository reconstructed 0.3 (the guards, the drop rates,
        /// the pumps, the NPCs' looks and Lonigan's place, the Stalkers, the miasmas, the Infected
        /// Foreans, the book's place, Porthus's place).
        /// </summary>
        [TestMethod]
        public void TheCommandersMissionsTrustWhatIsReconstructedLeast()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var evidence = harness.World.MissionEvidenceEntries.AsNoTracking()
                .Where(entry => entry.ContentRevision == PlateauMissionDataV1.Revision).ToArray();
            foreach (var (mission, confidences) in new (uint, (MissionEvidenceSourceKind, double)[])[]
            {
                (1045, new[] { (MissionEvidenceSourceKind.Client, 1.0), (MissionEvidenceSourceKind.Documentation, 0.5),
                    (MissionEvidenceSourceKind.Reconstruction, 0.3) }),
                (1046, new[] { (MissionEvidenceSourceKind.Client, 1.0), (MissionEvidenceSourceKind.Documentation, 0.4),
                    (MissionEvidenceSourceKind.Reconstruction, 0.3) }),
                (1053, new[] { (MissionEvidenceSourceKind.Client, 1.0), (MissionEvidenceSourceKind.Documentation, 0.5),
                    (MissionEvidenceSourceKind.Reconstruction, 0.3) }),
                (1251, new[] { (MissionEvidenceSourceKind.Client, 1.0), (MissionEvidenceSourceKind.Documentation, 0.5),
                    (MissionEvidenceSourceKind.Reconstruction, 0.3) }),
                (1015, new[] { (MissionEvidenceSourceKind.Client, 1.0), (MissionEvidenceSourceKind.Documentation, 0.4),
                    (MissionEvidenceSourceKind.Reconstruction, 0.3) }),
                (1017, new[] { (MissionEvidenceSourceKind.Client, 1.0), (MissionEvidenceSourceKind.Documentation, 0.5),
                    (MissionEvidenceSourceKind.Reconstruction, 0.3) }),
                (933, new[] { (MissionEvidenceSourceKind.Client, 1.0), (MissionEvidenceSourceKind.Documentation, 0.5),
                    (MissionEvidenceSourceKind.Reconstruction, 0.3) }),
                (1981, new[] { (MissionEvidenceSourceKind.Client, 1.0), (MissionEvidenceSourceKind.Documentation, 0.5),
                    (MissionEvidenceSourceKind.Reconstruction, 0.3) }),
                (930, new[] { (MissionEvidenceSourceKind.Client, 1.0), (MissionEvidenceSourceKind.Documentation, 0.5),
                    (MissionEvidenceSourceKind.Reconstruction, 0.3) }),
                (1034, new[] { (MissionEvidenceSourceKind.Client, 1.0), (MissionEvidenceSourceKind.Documentation, 0.4),
                    (MissionEvidenceSourceKind.Reconstruction, 0.3) })
            })
                CollectionAssert.AreEqual(confidences, evidence.Where(entry => entry.MissionId == mission)
                    .OrderBy(entry => entry.EvidenceId).Select(entry => (entry.SourceKind, entry.Confidence)).ToArray(),
                    $"{mission} evidence");
        }

        [TestMethod]
        public void IndicatorsCarryNoOtherMissionsClientName()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var indicators = harness.World.MissionIndicatorEntries.AsNoTracking()
                .Where(entry => entry.ContentRevision == PlateauMissionDataV1.Revision).ToArray();
            Assert.HasCount(Indicators, indicators);
            foreach (var row in indicators)
                Assert.IsTrue(row.IndicatorId > MissionIndicator.UnnamedFrom,
                    $"mission {row.MissionId} objective {row.ObjectiveId} indicator {row.IndicatorId}");
        }

        [TestMethod]
        public void TheReconSquadsAndGoliathsGuardsStandAndRollBack()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var world = harness.World;
            int Count(string sql) => world.Database.SqlQueryRaw<int>(sql).AsEnumerable().Single();
            string Squads(string table) =>
                $"SELECT COUNT(*) AS Value FROM {table} WHERE id BETWEEN {PlateauReconSquadsV1.First} AND {PlateauReconSquadsV1.Last}";
            (uint Creature, byte Min, byte Max) Guards()
            {
                var pool = world.SpawnPoolEntries.AsNoTracking().Single(row => row.Id == PlateauGiantKillerV1.GoliathPool);
                return (pool.Creature2Id, pool.Creature2MinCount, pool.Creature2MaxCount);
            }

            Assert.AreEqual(11, Count(Squads("creature")));
            Assert.AreEqual(11, Count(Squads("spawnpool")));
            Assert.AreEqual(3, Count(Squads("npc_package")));
            Assert.AreEqual(((uint)PlateauGiantKillerV1.KaelGuard, (byte)PlateauGiantKillerV1.GuardsMin,
                (byte)PlateauGiantKillerV1.GuardsMax), Guards());

            world.GetService<IMigrator>().Migrate(GiantKiller);
            Assert.AreEqual(0, Count(Squads("creature")));
            Assert.AreEqual(0, Count(Squads("creature_appearance")));
            Assert.AreEqual(0, Count(Squads("spawnpool")));
            Assert.AreEqual(0, Count(Squads("npc_package")));
            Assert.IsFalse(world.MissionContentDefinitionEntries.Any(entry => entry.MissionId == PlateauIncommunicadoV1.MissionId));

            world.GetService<IMigrator>().Migrate(Before);
            Assert.AreEqual((0U, (byte)0, (byte)0), Guards());
            Assert.IsFalse(world.MissionContentDefinitionEntries.Any(entry => entry.ContentRevision == PlateauMissionDataV1.Revision));
        }

        /// <summary>
        /// Each migration takes out only its own rows: Toxic Detox and Blueprint For Victory their
        /// missions; Canyon Sweep its mission, the Maligo Elite Guards and guardmasters (creatures,
        /// stats, actions, six pools) and Incommunicado's flag actions, which leaves Incommunicado.
        /// </summary>
        [TestMethod]
        public void TheMaligoGuardsTheFlagsAndTheCommandersMissionsRollBack()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var world = harness.World;
            int Count(string sql) => world.Database.SqlQueryRaw<int>(sql).AsEnumerable().Single();
            var creatures = $"SELECT COUNT(*) AS Value FROM creature WHERE id IN ({PlateauMaligoGuardsV1.Guard}, {PlateauMaligoGuardsV1.Guardmaster})";
            var stats = $"SELECT COUNT(*) AS Value FROM creature_stat WHERE id IN ({PlateauMaligoGuardsV1.Guard}, {PlateauMaligoGuardsV1.Guardmaster})";
            var actions = $"SELECT COUNT(*) AS Value FROM creature_action WHERE id BETWEEN {PlateauMaligoGuardsV1.FirstAction} AND {PlateauMaligoGuardsV1.LastAction}";
            var pools = $"SELECT COUNT(*) AS Value FROM spawnpool WHERE id BETWEEN {PlateauMaligoGuardsV1.FirstPool} AND {PlateauMaligoGuardsV1.LastPool} " +
                $"AND map_context_id = {PlateauMissionDataV1.MapContextId}";
            bool Has(uint mission) => world.MissionContentDefinitionEntries.AsNoTracking().Any(entry => entry.MissionId == mission);
            int Flags() => world.MissionActionEntries.AsNoTracking().Count(entry =>
                entry.MissionId == PlateauIncommunicadoV1.MissionId && entry.Kind == MissionActionKind.SetPlayerFlag);

            Assert.AreEqual(2, Count(creatures));
            Assert.AreEqual(2, Count(stats));
            Assert.AreEqual(7, Count(actions));
            Assert.AreEqual(6, Count(pools));
            Assert.AreEqual(3, Flags());

            world.GetService<IMigrator>().Migrate(BlueprintForVictory);
            Assert.IsFalse(Has(PlateauToxicDetoxV1.MissionId));
            Assert.IsTrue(Has(PlateauBlueprintForVictoryV1.MissionId));

            world.GetService<IMigrator>().Migrate(CanyonSweep);
            Assert.IsFalse(Has(PlateauBlueprintForVictoryV1.MissionId));
            Assert.IsTrue(Has(PlateauCanyonSweepV1.MissionId));
            Assert.AreEqual(6, Count(pools), "Canyon Sweep's guards stay with it");

            world.GetService<IMigrator>().Migrate(Incommunicado);
            Assert.IsFalse(Has(PlateauCanyonSweepV1.MissionId));
            Assert.AreEqual(0, Count(creatures));
            Assert.AreEqual(0, Count(stats));
            Assert.AreEqual(0, Count(actions));
            Assert.AreEqual(0, Count(pools));
            Assert.AreEqual(0, Flags());
            Assert.IsTrue(Has(PlateauIncommunicadoV1.MissionId));
            Assert.AreEqual(2, world.MissionActionEntries.AsNoTracking().Count(entry =>
                entry.MissionId == PlateauIncommunicadoV1.MissionId && entry.ObjectiveId == 1), "Incommunicado's own actions stay");
        }

        /// <summary>
        /// The Scrounger's chain rolls back a migration at a time: The Stalk Market with its Stalker
        /// (creature, stats, three actions, four pools); Scavenger Hunt; Comic Relief with O'Brien,
        /// Lonigan and the Scrounger (creatures, looks, pools, packages 999 and 1000). Toxic Detox stays.
        /// </summary>
        [TestMethod]
        public void TheScroungersChainItsNpcsAndTheStalkersRollBack()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var world = harness.World;
            int Count(string sql) => world.Database.SqlQueryRaw<int>(sql).AsEnumerable().Single();
            string Ids(string table, uint first, uint last) =>
                $"SELECT COUNT(*) AS Value FROM {table} WHERE id BETWEEN {first} AND {last}";
            var npcs = Ids("creature", PlateauScroungerNpcsV1.First, PlateauScroungerNpcsV1.Last);
            var looks = Ids("creature_appearance", PlateauScroungerNpcsV1.First, PlateauScroungerNpcsV1.Last);
            var npcPools = Ids("spawnpool", PlateauScroungerNpcsV1.First, PlateauScroungerNpcsV1.Last);
            var packages = $"SELECT COUNT(*) AS Value FROM npc_package WHERE package_id IN (999, 1000)";
            var stalker = Ids("creature", PlateauBaneStalkersV1.Stalker, PlateauBaneStalkersV1.Stalker);
            var stalkerStat = Ids("creature_stat", PlateauBaneStalkersV1.Stalker, PlateauBaneStalkersV1.Stalker);
            var stalkerActions = Ids("creature_action", PlateauBaneStalkersV1.FirstAction, PlateauBaneStalkersV1.LastAction);
            var stalkerPools = Ids("spawnpool", PlateauBaneStalkersV1.FirstPool, PlateauBaneStalkersV1.LastPool) +
                $" AND map_context_id = {PlateauMissionDataV1.MapContextId} AND creature_1_Id = {PlateauBaneStalkersV1.Stalker}";
            bool Has(uint mission) => world.MissionContentDefinitionEntries.AsNoTracking().Any(entry => entry.MissionId == mission);

            Assert.AreEqual((3, 19, 3, 2), (Count(npcs), Count(looks), Count(npcPools), Count(packages)));
            Assert.AreEqual((1, 1, 3, 4), (Count(stalker), Count(stalkerStat), Count(stalkerActions), Count(stalkerPools)));

            world.GetService<IMigrator>().Migrate(ScavengerHunt);
            Assert.IsFalse(Has(PlateauStalkMarketV1.MissionId));
            Assert.AreEqual((0, 0, 0, 0), (Count(stalker), Count(stalkerStat), Count(stalkerActions), Count(stalkerPools)));
            Assert.IsTrue(Has(PlateauScavengerHuntV1.MissionId));

            world.GetService<IMigrator>().Migrate(ComicRelief);
            Assert.IsFalse(Has(PlateauScavengerHuntV1.MissionId));
            Assert.IsTrue(Has(PlateauComicReliefV1.MissionId));
            Assert.AreEqual(3, Count(npcs), "the NPCs stay with Comic Relief");

            world.GetService<IMigrator>().Migrate(ToxicDetox);
            Assert.IsFalse(Has(PlateauComicReliefV1.MissionId));
            Assert.AreEqual((0, 0, 0, 0), (Count(npcs), Count(looks), Count(npcPools), Count(packages)));
            Assert.IsTrue(Has(PlateauToxicDetoxV1.MissionId));
        }

        /// <summary>
        /// New Velon Village's missions roll back a migration at a time: Going Postal puts Porthus back on
        /// the chasm's floor; The Book Of Velonar; Village Of The Damned with its Infected Forean
        /// (creature, stats, look, action, four pools); Graveyard Shift with the Tomb Miasma and the Bull
        /// (creatures, stats, four actions, five pools). The Stalk Market stays.
        /// </summary>
        [TestMethod]
        public void NewVelonVillagesMissionsTheirCreaturesAndPorthusRollBack()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var world = harness.World;
            int Count(string sql) => world.Database.SqlQueryRaw<int>(sql).AsEnumerable().Single();
            string Ids(string table, uint first, uint last) =>
                $"SELECT COUNT(*) AS Value FROM {table} WHERE id BETWEEN {first} AND {last}";
            var miasmas = (Ids("creature", PlateauTombMiasmasV1.TombMiasma, PlateauTombMiasmasV1.Bull),
                Ids("creature_stat", PlateauTombMiasmasV1.TombMiasma, PlateauTombMiasmasV1.Bull),
                Ids("creature_action", PlateauTombMiasmasV1.FirstAction, PlateauTombMiasmasV1.LastAction),
                Ids("spawnpool", PlateauTombMiasmasV1.FirstPool, PlateauTombMiasmasV1.LastPool) +
                    $" AND map_context_id = {PlateauMissionDataV1.MapContextId}");
            var infected = (Ids("creature", PlateauInfectedForeansV1.Infected, PlateauInfectedForeansV1.Infected),
                Ids("creature_stat", PlateauInfectedForeansV1.Infected, PlateauInfectedForeansV1.Infected),
                Ids("creature_appearance", PlateauInfectedForeansV1.Infected, PlateauInfectedForeansV1.Infected),
                Ids("creature_action", PlateauInfectedForeansV1.Weapon, PlateauInfectedForeansV1.Weapon),
                Ids("spawnpool", PlateauInfectedForeansV1.FirstPool, PlateauInfectedForeansV1.LastPool) +
                    $" AND creature_1_Id = {PlateauInfectedForeansV1.Infected}");
            (int, int, int, int) Miasmas() => (Count(miasmas.Item1), Count(miasmas.Item2), Count(miasmas.Item3), Count(miasmas.Item4));
            (int, int, int, int, int) Infected() => (Count(infected.Item1), Count(infected.Item2), Count(infected.Item3),
                Count(infected.Item4), Count(infected.Item5));
            (double, double, double) Porthus()
            {
                var pool = world.SpawnPoolEntries.AsNoTracking().Single(row => row.Id == PlateauGoingPostalV1.Porthus);
                return (Math.Round(pool.PosX, 2), Math.Round(pool.PosY, 2), Math.Round(pool.PosZ, 2));
            }
            bool Has(uint mission) => world.MissionContentDefinitionEntries.AsNoTracking().Any(entry => entry.MissionId == mission);

            Assert.AreEqual((2, 2, 4, 5), Miasmas());
            Assert.AreEqual((1, 1, 1, 1, 4), Infected());
            Assert.AreEqual(PlateauGoingPostalV1.PorthusAt, Porthus());

            world.GetService<IMigrator>().Migrate(BookOfVelonar);
            Assert.IsFalse(Has(PlateauGoingPostalV1.MissionId));
            Assert.AreEqual(PlateauGoingPostalV1.PorthusBefore, Porthus());
            Assert.IsTrue(Has(PlateauBookOfVelonarV1.MissionId));

            world.GetService<IMigrator>().Migrate(VillageOfTheDamned);
            Assert.IsFalse(Has(PlateauBookOfVelonarV1.MissionId));
            Assert.IsTrue(Has(PlateauVillageOfTheDamnedV1.MissionId));
            Assert.AreEqual((1, 1, 1, 1, 4), Infected(), "the Infected Foreans stay with Village Of The Damned");

            world.GetService<IMigrator>().Migrate(GraveyardShift);
            Assert.IsFalse(Has(PlateauVillageOfTheDamnedV1.MissionId));
            Assert.AreEqual((0, 0, 0, 0, 0), Infected());
            Assert.IsTrue(Has(PlateauGraveyardShiftV1.MissionId));

            world.GetService<IMigrator>().Migrate(StalkMarket);
            Assert.IsFalse(Has(PlateauGraveyardShiftV1.MissionId));
            Assert.AreEqual((0, 0, 0, 0), Miasmas());
            Assert.IsTrue(Has(PlateauStalkMarketV1.MissionId));
        }
    }
}
