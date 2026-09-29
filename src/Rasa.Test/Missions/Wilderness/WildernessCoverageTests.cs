using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Missions.Wilderness
{
    [TestClass]
    [DoNotParallelize]
    public class WildernessCoverageTests
    {
        [TestMethod]
        public void LatestProvidersEnableExactlyTheReconciledOutdoorInventory()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var expected = WildernessMissionCases.All.Where(entry =>
                    entry.Disposition == WildernessDisposition.OutdoorRelease)
                .Select(entry => entry.MissionId).ToArray();
            Assert.AreEqual(64, expected.Length);
            Assert.AreEqual(64, expected.Distinct().Count());
            var enabled = harness.World.MissionContentDefinitionEntries.AsNoTracking()
                .Where(entry => entry.Enabled).ToArray();
            CollectionAssert.AreEquivalent(expected,
                enabled.Where(entry => entry.ContentRevision == WildernessMissionCases.ContentRevision)
                    .Select(entry => entry.MissionId).ToArray());
            CollectionAssert.AreEquivalent(expected.Concat(WildernessMissionCases.ProtectedBootcampMissionIds).ToArray(),
                enabled.Select(entry => entry.MissionId).ToArray());
            Assert.AreEqual(69, enabled.Length);
            Assert.IsTrue(expected.All(id => harness.Manager.LoadedMissions[id].IsOperational),
                "An enabled but nonoperational definition is not outdoor coverage.");
        }

        [TestMethod]
        public void ConditionalInstanceAndUnmatchedCandidatesRemainOutsideNormalOutdoorOffers()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            foreach (var (id, disposition) in new[]
            {
                (706U, WildernessDisposition.InstanceDependent),
                (1167U, WildernessDisposition.SourceOnlyUnmatched),
                (1769U, WildernessDisposition.ConditionalEvent),
                (1998U, WildernessDisposition.OtherZone),
                (1999U, WildernessDisposition.ConditionalEvent),
                (2012U, WildernessDisposition.InstanceDependent),
                (2016U, WildernessDisposition.OtherZone),
                (2017U, WildernessDisposition.OtherZone),
                (751U, WildernessDisposition.Retired),
                (780U, WildernessDisposition.Retired),
                (767U, WildernessDisposition.Retired)
            })
            {
                Assert.AreEqual(disposition, WildernessMissionCases.All.Single(entry => entry.MissionId == id).Disposition,
                    $"Native mission {id} must keep its evidence-backed boundary disposition.");
                Assert.IsFalse(harness.World.MissionContentDefinitionEntries.Any(entry => entry.Enabled && entry.MissionId == id),
                    $"Excluded or conditional mission {id} must not become an ordinary enabled quest.");
            }
        }
    }
}
