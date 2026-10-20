using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Missions.Plateau
{
    using Rasa.Services.Preloader.Missions.Plateau;
    using Rasa.Structures;
    using Rasa.Test.Missions.Wilderness;

    /// <summary>
    /// The Plateau missions at Colonel Bosley, Camp Resistance (PlateauMissionDataV1): Giant Killer
    /// (939, Plateau_giant_killer) and Incommunicado (1040, Plateau_incommunicado) with its three
    /// recon squads (PlateauReconSquadsV1).
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class PlateauMissionTests
    {
        /// <summary>The Plateau missions the World migrations enable, for the tests that pin the enabled set.</summary>
        internal static readonly uint[] MissionIds = { PlateauGiantKillerV1.MissionId, PlateauIncommunicadoV1.MissionId };

        /// <summary>Giant Killer's two indicators and Incommunicado's three.</summary>
        internal const int Indicators = 5;

        private const string Before = "20270123000000_Thunderhead_base_teleporters";
        private const string GiantKiller = "20270125000000_Plateau_giant_killer";

        [TestMethod]
        public void BothMissionsAreEnabledAndOperationalAtBosley()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var enabled = harness.World.MissionContentDefinitionEntries.AsNoTracking()
                .Where(entry => entry.Enabled && entry.ContentRevision == PlateauMissionDataV1.Revision).ToArray();
            CollectionAssert.AreEquivalent(MissionIds, enabled.Select(entry => entry.MissionId).ToArray());
            foreach (var entry in enabled)
            {
                Assert.AreEqual<uint?>(PlateauGiantKillerV1.Bosley, entry.GiverId);
                Assert.AreEqual<uint?>(PlateauGiantKillerV1.Bosley, entry.ReceiverId);
                Assert.AreEqual<uint?>(30, entry.Level);
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
        /// not taken from the data classes, so a wrong id in those fails.
        /// </summary>
        [TestMethod]
        public void TitlesAndObjectivesUseTheClientsTexts()
        {
            var expected = new (uint Mission, uint Title, (uint Objective, uint Name, uint Body)[] Objectives)[]
            {
                (939, 5104, new[] { (1U, 5108U, 6354U), (2U, 5109U, 6355U) }),
                (1040, 5823, new[] { (1U, 5832U, 6368U), (2U, 5833U, 6369U), (3U, 5834U, 6370U) })
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
                foreach (var (objective, name, body) in objectives)
                {
                    var row = actual.Single(entry => entry.ObjectiveId == objective);
                    Assert.AreEqual(name, row.ClientNameTextId, $"{mission}.{objective} name");
                    Assert.AreEqual(body, row.ClientBodyTextId, $"{mission}.{objective} body");
                    Assert.IsNull(row.ClientCounter0TextId, $"{mission}.{objective} counter0: the client has none");
                    Assert.IsNull(row.ClientCounter1TextId, $"{mission}.{objective} counter1");
                    Assert.IsNull(row.ClientCounter2TextId, $"{mission}.{objective} counter2");
                }
            }
        }

        [TestMethod]
        public void IncommunicadoRequiresGiantKiller()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var prerequisites = harness.World.MissionPrerequisiteEntries.AsNoTracking()
                .Where(entry => entry.ContentRevision == PlateauMissionDataV1.Revision).ToArray();
            Assert.HasCount(1, prerequisites);
            Assert.AreEqual(PlateauIncommunicadoV1.MissionId, prerequisites[0].MissionId);
            Assert.AreEqual<uint?>(PlateauGiantKillerV1.MissionId, prerequisites[0].RequiredMissionId);
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
    }
}
