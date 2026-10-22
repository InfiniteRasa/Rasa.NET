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
            // And the battlefields' missions: their Targets of Opportunity (TargetsOfOpportunitySeed),
            // fourteen, the fifteenth being the Wilderness missions' own 1449; and the Plateau ones.
            var battlefields = Rasa.Services.Preloader.TargetsOfOpportunitySeed.Zones.Select(zone => zone.MissionId)
                .Concat(Plateau.PlateauMissionTests.MissionIds).ToArray();
            CollectionAssert.AreEquivalent(expected.Concat(WildernessMissionCases.ProtectedBootcampMissionIds)
                    .Concat(battlefields).ToArray(),
                enabled.Select(entry => entry.MissionId).ToArray());
            Assert.AreEqual(69 + battlefields.Length, enabled.Length);
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
                // A mission of another zone may be enabled with that zone's content (A Mystery Unearthed and
                // Symbolism with the Plateau's), never as a Wilderness quest.
                Assert.IsFalse(harness.World.MissionContentDefinitionEntries.Any(entry => entry.Enabled && entry.MissionId == id &&
                        !(disposition == WildernessDisposition.OtherZone &&
                          entry.ContentRevision == Rasa.Services.Preloader.Missions.Plateau.PlateauMissionDataV1.Revision)),
                    $"Excluded or conditional mission {id} must not become an ordinary enabled quest.");
            }
        }
    }
}
