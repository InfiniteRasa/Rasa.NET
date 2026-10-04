using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Managers;
using Rasa.Structures;
using Rasa.Structures.Char;

namespace Rasa.Test.Missions.Wilderness
{
    [TestClass]
    [DoNotParallelize]
    public class WildernessProgressionAcceptanceTests
    {
        [TestMethod]
        public void WaveAProviderBoundaryEnablesExactlyItsNativeOutdoorScopeAndBootcamp()
        {
            using var harness = WildernessRuntimeTestHarness.Create(
                targetWorldMigration: "20261104001100_WildernessLandingZone");
            var outdoor = new uint[]
            {
                1407, 1069, 479, 1449,
                1390, 1392, 1393, 428, 421, 427, 422, 1526, 2010, 2011,
                1638, 1640, 921, 907, 909, 1639, 1633, 911, 1741,
                429, 431, 433, 432, 434, 506, 436, 508,
                665, 666, 430, 549, 776, 795, 771, 441
            };
            CollectionAssert.AreEquivalent(outdoor, harness.World.MissionContentDefinitionEntries
                .Where(entry => entry.Enabled && entry.ContentRevision == "wilderness_1_16_5")
                .Select(entry => entry.MissionId).ToArray());
            CollectionAssert.AreEquivalent(new uint[] { 1990, 1992, 1994, 1995, 2005 },
                harness.World.MissionContentDefinitionEntries
                    .Where(entry => entry.Enabled && entry.ContentRevision == "deployment_11")
                    .Select(entry => entry.MissionId).ToArray());
            Assert.IsTrue(outdoor.All(id => harness.Manager.LoadedMissions[id].IsOperational));
        }

        [TestMethod]
        public void ForwardWorldUpgradePreservesTheActiveTargetsOfOpportunityAttemptAndCharacterState()
        {
            using var harness = WildernessRuntimeTestHarness.Create(
                targetWorldMigration: "20261104000200_WildernessAliaOpening");
            harness.SpawnWorld(196);
            var cimoch = harness.Npc(196);
            using (var unit = harness.CreateChar())
                unit.CharacterTeleporters.Add(new CharacterTeleporterEntry(1, 57, (byte)WaypointType.Waypoint));
            Assert.IsTrue(harness.Manager.AcceptOfferedMission(harness.Client, cimoch.EntityId, 1449));
            Assert.IsTrue(harness.Manager.RecordProgress(harness.Client, MissionProgressEvent.Creature(87)));
            Assert.IsTrue(harness.Manager.RecordProgress(harness.Client, MissionProgressEvent.Creature(87)));
            var original = harness.Client.Player.Missions[1449];
            var counters = original.Objectives.ToDictionary(entry => entry.Key,
                entry => entry.Value.Counters.ToDictionary(counter => counter.Key, counter => counter.Value));
            var states = original.Objectives.ToDictionary(entry => entry.Key, entry => entry.Value.State);
            using (var grant = new InventoryManager.InventoryGrant())
            {
                using (var unit = harness.CreateChar())
                    unit.ExecuteTransaction(() =>
                    {
                        unit.CharacterFlags.Set(1, 65099, 17);
                        grant.PlanAndSave(harness.Client,
                            new[] { new InventoryManager.InventoryItemGrant(56, 3) }, unit);
                    });
                grant.Publish(harness.Client);
            }
            var itemIds = harness.Client.Player.Inventory.PersonalInventory.Where(id => id != 0)
                .Select(EntityManager.Instance.GetItem).Select(item => item.Id).OrderBy(id => id).ToArray();
            var totals = harness.Context.ReadRewardTotals();
            var previousMigrations = harness.World.Database.GetAppliedMigrations().ToArray();

            harness.World.Initialize();
            Assert.IsFalse(harness.Manager.LoadMissions().BlocksReadiness);
            using (var unit = harness.CreateChar())
                harness.Manager.HydrateAndClearInvalid(harness.Client.Player, unit);

            Assert.IsTrue(harness.World.Database.GetAppliedMigrations().Count() > previousMigrations.Length);
            var retained = harness.Client.Player.Missions[1449];
            Assert.AreEqual(original.AssignmentId, retained.AssignmentId);
            Assert.AreEqual(original.Generation, retained.Generation);
            Assert.AreEqual(original.ContentRevision, retained.ContentRevision);
            foreach (var objective in counters)
            {
                Assert.AreEqual(states[objective.Key], retained.Objectives[objective.Key].State);
                CollectionAssert.AreEquivalent(objective.Value.ToArray(),
                    retained.Objectives[objective.Key].Counters.ToArray());
            }
            Assert.AreEqual(MissionState.Active, retained.State);
            Assert.IsFalse(retained.Completeable);
            Assert.IsFalse(harness.Manager.CompleteOfferedMission(harness.Client, cimoch.EntityId, 1449, null));
            Assert.AreEqual(totals, harness.Context.ReadRewardTotals());
            using var verify = harness.CreateChar();
            Assert.AreEqual(17U, verify.CharacterFlags.Get(1)[65099]);
            CollectionAssert.AreEqual(itemIds, verify.CharacterInventories.GetItems(harness.Client.AccountEntry.Id)
                .Where(item => item.CharacterId == 1 && item.InventoryType == (uint)InventoryType.Personal)
                .Select(item => item.ItemId).OrderBy(id => id).ToArray());
        }
    }
}
