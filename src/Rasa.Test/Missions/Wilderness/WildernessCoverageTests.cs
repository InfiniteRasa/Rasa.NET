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
            // And the battlefields' Targets of Opportunity (TargetsOfOpportunitySeed): fourteen, the
            // fifteenth being the Wilderness missions' own 1449. And the Ojasa Atta Hive's five
            // (OjasaHiveContent), the Caves of Donn's six (CavesOfDonnContent), the Devil's Den's
            // eight (DevilsDenContent), Pravus Research's six (PravusResearchContent), Torcastra
            // Prison's five (TorcastraMissionsV1), Minos Caverns' four (MinosCavernsMissions), Crater Lake's ten
            // (CraterLakeContent), the Energy Weapon Center's five (EnergyWeaponCenterContent), Timora Mines' six and
            // Stolen History (TimoraMinesMissionsV1), Warnet Caverns' eight (WarnetCavernsMissions), the Kardash Atta
            // Colony's ten (KardashAttaColonyContent) and the Phanin Research Facility's seven (PhaninResearchContent),
            // which are instances'. And the Plateau, Valverde Pools and Valverde Marshes ones.
            var battlefields = Rasa.Services.Preloader.TargetsOfOpportunitySeed.Zones.Select(zone => zone.MissionId)
                .Concat(Plateau.PlateauMissionTests.MissionIds).Concat(Pools.PoolsMissionTests.MissionIds)
                .Concat(Marshes.MarshesMissionTests.MissionIds).ToArray();
            var ojasa = Rasa.Services.Preloader.OjasaHiveContent.Missions;
            var donn = Rasa.Services.Preloader.CavesOfDonnContent.Missions;
            var den = Rasa.Services.Preloader.DevilsDenContent.Missions;
            var pravus = Rasa.Services.Preloader.PravusResearchContent.Missions
                .Concat(Rasa.Services.Preloader.Missions.Torcastra.TorcastraMissionsV1.Enabled).ToArray();
            var minos = Rasa.Services.Preloader.MinosCavernsMissions.Missions
                .Concat(Rasa.Services.Preloader.Missions.Timora.TimoraMinesMissionsV1.Enabled)
                .Concat(Rasa.Services.Preloader.WarnetCavernsMissions.Missions).ToArray();
            var craterLake = Rasa.Services.Preloader.CraterLakeContent.Missions
                .Concat(Rasa.Services.Preloader.MagmaCavernsContent.Missions).ToArray();
            var ewc = Rasa.Services.Preloader.EnergyWeaponCenterContent.Missions;
            var kardash = Rasa.Services.Preloader.KardashAttaColonyContent.Missions;
            var phanin = Rasa.Services.Preloader.PhaninResearchContent.Missions;
            CollectionAssert.AreEquivalent(expected.Concat(WildernessMissionCases.ProtectedBootcampMissionIds)
                    .Concat(battlefields).Concat(ojasa).Concat(donn).Concat(den).Concat(pravus).Concat(minos).Concat(craterLake).Concat(ewc).Concat(kardash).Concat(phanin).ToArray(),
                enabled.Select(entry => entry.MissionId).ToArray());
            Assert.AreEqual(69 + battlefields.Length + ojasa.Length + donn.Length + den.Length + pravus.Length + minos.Length + craterLake.Length +
                ewc.Length + kardash.Length + phanin.Length, enabled.Length);
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
                // Symbolism with the Plateau's), never as a Wilderness quest. The Final Assault (1167) is enabled
                // with the Plateau's Temporal Chamber instance content.
                Assert.IsFalse(harness.World.MissionContentDefinitionEntries.Any(entry => entry.Enabled && entry.MissionId == id &&
                        !((disposition == WildernessDisposition.OtherZone || id == 1167U) &&
                          entry.ContentRevision == Rasa.Services.Preloader.Missions.Plateau.PlateauMissionDataV1.Revision)),
                    $"Excluded or conditional mission {id} must not become an ordinary enabled quest.");
            }
        }
    }
}
