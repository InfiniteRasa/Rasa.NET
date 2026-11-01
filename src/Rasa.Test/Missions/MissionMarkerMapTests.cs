using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Managers;
using Rasa.Missions.Content;
using Rasa.Missions.Runtime;
using Rasa.Structures;
using Rasa.Structures.World;

namespace Rasa.Test.Missions
{
    [TestClass]
    public class MissionMarkerMapTests
    {
        private static MissionIndicator Pin(uint id, uint map = 0) => new() {
            IndicatorId = id, MapContextId = map, Position = new(1, 2, 3), Radius = 8, Show3DEffect = true
        };
        private static Mission Definition(MissionIndicator pin = null)
        {
            var objective = new MissionObjectiveDefinition(1, 1, 1, null, 0, MissionObjectiveState.Incomplete,
                true, null, null, null, revealedObjectiveIds: System.Array.Empty<uint>(), activatedObjectiveIds: System.Array.Empty<uint>(), indicators: new[] { pin ?? Pin(42) });
            return new Mission(9, "map-scoped mission", 1, 10, 10, 1, 1, 1, false, false, new[] { objective });
        }
        private static MissionContentCatalog Catalog(Mission mission = null) =>
            new(null, mission == null ? new Dictionary<uint, Mission>() : new() { [9] = mission }, null);
        private static MissionInfo Info(params MissionIndicator[] pins)
        {
            var objective = new MissionObjective { ObjectiveId = 1, State = MissionObjectiveState.Incomplete, IsRequired = true };
            objective.Counters[3] = new MissionObjectiveCounter { CounterValue = 2, InitialValue = 0, TargetValue = 5 };
            objective.IndicatorList.AddRange(pins);
            return new MissionInfo { MissionState = MissionState.Active, ObjectivesList = new() { objective } };
        }

        [TestMethod]
        [DataRow(356U, 4U, 1000000004U, 1349U)]
        [DataRow(356U, 11U, 1000000011U, 1349U)]
        [DataRow(397U, 2U, 24U, 1349U)]
        [DataRow(399U, 5U, 10000001U, 1349U)]
        [DataRow(380U, 1U, 1000000001U, 1394U)]
        [DataRow(671U, 3U, 1000000003U, 1394U)]
        [DataRow(671U, 8U, 1000000008U, 1394U)]
        [DataRow(671U, 7U, 1000000007U, 1244U)]
        [DataRow(382U, 1U, 33U, 1148U)]
        [DataRow(384U, 1U, 33U, 1148U)]
        [DataRow(593U, 1U, 54U, 1220U)]
        [DataRow(1491U, 1U, 278U, 1761U)]
        [DataRow(1491U, 2U, 1000000002U, 1865U)]
        [DataRow(935U, 1U, 1000000002U, 2029U)]
        [DataRow(1329U, 1U, 1000000001U, 1694U)]
        [DataRow(2017U, 1U, 1000000001U, 1304U)]
        [DataRow(2017U, 6U, 1000000004U, 1220U)]
        public void LegacyCrossMapGoalsKeepTheirAuthoredIndoorOrOutdoorMap(uint mission, uint objective, uint indicator, uint map)
        {
            var catalog = Catalog();
            catalog.IndicatorMapDefaults[mission] = 9999; // Must not replace the specific marker scope.
            Assert.AreEqual(map, MissionMarkerProjection.ResolveMap(catalog, mission, objective, Pin(indicator)));
            catalog.SceneBindings[mission] = new MissionSceneDefinition {
                IndicatorMaps = new() { [objective] = new() { [indicator] = 8888 } }
            };
            Assert.AreEqual(8888U, MissionMarkerProjection.ResolveMap(catalog, mission, objective, Pin(indicator)));
            Assert.AreEqual(7777U, MissionMarkerProjection.ResolveMap(catalog, mission, objective, Pin(indicator, 7777)));
        }

        [TestMethod]
        [DataRow(1220U, 2)]
        [DataRow(2136U, 0)]
        [DataRow(0U, 0)]
        public void OnlyMatchingMapsReceiveNamedAndUnnamedMarkers(uint map, int count)
        {
            var catalog = Catalog(); catalog.IndicatorMapDefaults[9] = 1220;
            var info = Info(Pin(42), Pin(MissionIndicator.UnnamedFrom));
            MissionMarkerProjection.Apply(new Manifestation { MapContextId = map }, 9, info, catalog);
            Assert.HasCount(count, info.ObjectivesList[0].IndicatorList);
            Assert.AreEqual(MissionState.Active, info.MissionState);
            Assert.AreEqual(2U, info.ObjectivesList[0].Counters[3].CounterValue);
            if (count > 0)
            {
                Assert.IsTrue(info.ObjectivesList[0].IndicatorList.All(p => p.MapContextId == map && p.Show3DEffect));
                Assert.IsNull(info.ObjectivesList[0].IndicatorList[1].ClientNameId);
            }
        }

        [TestMethod]
        public void PerIndicatorOverrideSupportsAQuestWithMarkersOnDifferentMaps()
        {
            var catalog = Catalog(); catalog.IndicatorMapDefaults[9] = 1220;
            catalog.SceneBindings[9] = new MissionSceneDefinition {
                IndicatorMaps = new() { [1] = new() { [42] = 2136 } }
            };
            var info = Info(Pin(42), Pin(43));
            MissionMarkerProjection.Apply(new Manifestation { MapContextId = 2136 }, 9, info, catalog);
            var pin = info.ObjectivesList[0].IndicatorList.Single();
            Assert.AreEqual(42U, pin.IndicatorId);
            Assert.AreEqual(2136U, pin.MapContextId);
            Assert.AreEqual(new System.Numerics.Vector3(1, 2, 3), pin.Position);
            Assert.AreEqual(8d, pin.Radius);
        }

        [TestMethod]
        public void ExplicitMapScopeSurvivesDefinitionAndObjectiveCloning()
        {
            var definition = Definition(Pin(42, 2136));
            var objective = definition.Objectives[1].CreateRuntime(MissionObjectiveState.Incomplete,
                new Dictionary<uint, uint>(), new Dictionary<uint, uint>());
            Assert.AreEqual(2136U, objective.IndicatorList.Single().MapContextId);
            var catalog = Catalog(definition); catalog.IndicatorMapDefaults[9] = 1220;
            var info = new MissionInfo { ObjectivesList = new() { objective } };
            MissionMarkerProjection.Apply(new Manifestation { MapContextId = 2136 }, 9, info, catalog);
            Assert.HasCount(1, objective.IndicatorList);
            objective.IndicatorList.Clear();
            Assert.HasCount(1, definition.Objectives[1].Indicators, "Projection never edits the shared authored definition.");
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void PlacedNpcContactsProvideAUniqueFallbackButAmbiguousMapsStayHidden(bool ambiguous)
        {
            var catalog = Catalog(Definition());
            var pools = new List<SpawnPoolEntry> { new() { Id = 1, Creature1Id = 10, MapContextId = 1220 } };
            if (ambiguous) pools.Add(new() { Id = 2, Creature1Id = 10, MapContextId = 2136 });
            MissionMarkerProjection.LoadDefaults(catalog, pools);
            var info = Info(Pin(42));
            MissionMarkerProjection.Apply(new Manifestation { MapContextId = 1220 }, 9, info, catalog);
            Assert.HasCount(ambiguous ? 0 : 1, info.ObjectivesList[0].IndicatorList);
        }

        // Logos: Feeling, Trap, Heal (923) is given in the Wilderness; Feeling's shrine (13) is in the Caves of Donn.
        private const uint WildernessMap = 1220, CavesOfDonn = 1506, Feeling = 13, Trap = 55;
        private static readonly System.Numerics.Vector3 FeelingAt = new(-160.24219f, 38.746094f, -325.59766f);

        private static LogosEntry Shrine(uint id, uint map, System.Numerics.Vector3 at) =>
            new() { Id = id, MapContextId = map, PosX = at.X, PosY = at.Y, PosZ = at.Z, Name = "shrine" };

        /// <summary>Mission 9, objective 1: acquire the Logos given, with the markers given (none by default).</summary>
        private static MissionContentCatalog LogosCatalog(MissionProgressRule rule, params MissionIndicator[] pins)
        {
            var objective = new MissionObjectiveDefinition(1, 1, 1, null, 0, MissionObjectiveState.Incomplete,
                true, null, null, null, revealedObjectiveIds: System.Array.Empty<uint>(), activatedObjectiveIds: System.Array.Empty<uint>(),
                indicators: pins, progressRule: rule);
            var catalog = Catalog(new Mission(9, "find the Logos", 1, 10, 10, 1, 1, 1, false, false, new[] { objective }));
            catalog.IndicatorMapDefaults[9] = WildernessMap;   // the giver's map
            MissionMarkerProjection.LoadShrines(catalog, new[]
            {
                Shrine(Feeling, CavesOfDonn, FeelingAt),
                Shrine(Trap, CavesOfDonn, new(-326.8789f, 24.050781f, -220.82031f)),
                Shrine(1, WildernessMap, new(363.872f, 214.211f, 619.304f))
            });
            return catalog;
        }

        private static MissionInfo Published(MissionContentCatalog catalog, uint map, uint missionId = 9, uint objectiveId = 1)
        {
            var objective = catalog.Missions[missionId].Objectives[objectiveId].CreateRuntime(MissionObjectiveState.Incomplete,
                new Dictionary<uint, uint>(), new Dictionary<uint, uint>());
            var info = new MissionInfo { MissionState = MissionState.Active, ObjectivesList = new() { objective } };
            return MissionMarkerProjection.Apply(new Manifestation { MapContextId = map }, missionId, info, catalog);
        }

        /// <summary>
        /// The Wilderness Logos dispatches have no authored marker: the shrine is marked, on its
        /// own map, as the authored ones on shrines are (radius 8, unnamed).
        /// </summary>
        [TestMethod]
        public void AFindTheLogosObjectiveWithNoMarkerIsMarkedOnItsShrine()
        {
            var catalog = LogosCatalog(MissionProgressRule.CompleteOnExactSubject(MissionProgressEventKind.LogosAcquired, Feeling));

            var pin = Published(catalog, CavesOfDonn).ObjectivesList[0].IndicatorList.Single();
            Assert.AreEqual(FeelingAt, pin.Position);
            Assert.AreEqual(CavesOfDonn, pin.MapContextId);
            Assert.AreEqual(MissionMarkerProjection.ShrineMarkerRadius, pin.Radius);
            Assert.AreEqual(MissionIndicator.UnnamedFrom + 1, pin.IndicatorId);
            Assert.IsFalse(pin.Show3DEffect);

            Assert.HasCount(0, Published(catalog, WildernessMap).ObjectivesList[0].IndicatorList, "not at Donn's coordinates on the giver's map");
            Assert.HasCount(0, catalog.Missions[9].Objectives[1].Indicators, "the definition is not changed");
        }

        /// <summary>An authored marker on a Logos objective is on the shrine's map, not the giver's (923's "Logos Puzzle" markers).</summary>
        [TestMethod]
        public void AnAuthoredLogosMarkerIsOnItsShrinesMap()
        {
            var catalog = LogosCatalog(MissionProgressRule.CompleteOnExactSubject(MissionProgressEventKind.LogosAcquired, Feeling), Pin(344));

            Assert.AreEqual(344U, Published(catalog, CavesOfDonn).ObjectivesList[0].IndicatorList.Single().IndicatorId);
            Assert.HasCount(0, Published(catalog, WildernessMap).ObjectivesList[0].IndicatorList);
        }

        /// <summary>A list of Logos gets no marker made up for it; its authored ones follow the shrines when they share a map, else the mission's.</summary>
        [TestMethod]
        [DataRow(Trap, CavesOfDonn)]
        [DataRow(1U, WildernessMap)]
        public void AListOfLogosIsNotMarkedButItsMarkersFollowOneMap(uint second, uint map)
        {
            var subjects = new HashSet<uint> { Feeling, second };
            var unmarked = LogosCatalog(MissionProgressRule.CompleteWhenAllDistinctSubjectsObserved(MissionProgressEventKind.LogosAcquired, subjects));
            Assert.HasCount(0, Published(unmarked, CavesOfDonn).ObjectivesList[0].IndicatorList);
            Assert.HasCount(0, Published(unmarked, WildernessMap).ObjectivesList[0].IndicatorList);

            var marked = LogosCatalog(MissionProgressRule.CompleteWhenAllDistinctSubjectsObserved(MissionProgressEventKind.LogosAcquired, subjects), Pin(344));
            Assert.HasCount(1, Published(marked, map).ObjectivesList[0].IndicatorList);
        }

        /// <summary>
        /// The shipped content: Logos: Area (1640) is marked on its Wilderness shrine; Logos:
        /// Feeling, Trap, Heal (923), given in the Wilderness, has its markers in the Caves of Donn.
        /// </summary>
        [TestMethod, DoNotParallelize]
        public void TheShippedLogosMissionsAreMarkedWhereTheirShrinesAre()
        {
            using var harness = Wilderness.WildernessRuntimeTestHarness.Create();
            var catalog = new MissionContentCatalog(harness, new Dictionary<uint, Mission>(), null);
            catalog.Load();

            var area = Published(catalog, WildernessMap, 1640, 4).ObjectivesList.Single().IndicatorList.Single();
            Assert.AreEqual(new System.Numerics.Vector3(363.872f, 214.211f, 619.304f), area.Position, "shrine 1, Area");

            Assert.HasCount(1, Published(catalog, CavesOfDonn, 923, 4).ObjectivesList.Single().IndicatorList);
            Assert.HasCount(0, Published(catalog, WildernessMap, 923, 4).ObjectivesList.Single().IndicatorList);

            using var world = harness.CreateWorld();
            var shrines = world.Logoses.GetLogos().Select(shrine => (uint?)shrine.Id).ToList();
            var asked = world.MissionContent.GetTriggers()
                .Where(trigger => trigger.EventKind == (byte)MissionProgressEventKind.LogosAcquired)
                .Select(trigger => trigger.SubjectId).Distinct().ToList();
            Assert.IsNotEmpty(asked);
            CollectionAssert.IsSubsetOf(asked, shrines, "every Logos a mission asks for has a shrine");
        }

        [TestMethod]
        [DataRow("objective")]
        [DataRow("indicator")]
        [DataRow("zero")]
        public void InvalidMapOverridesAreRejectedAtContentLoad(string invalid)
        {
            var scene = new MissionSceneDefinition { IndicatorMaps = new() {
                [invalid == "objective" ? 99U : 1U] = new() { [invalid == "indicator" ? 99U : 42U] = invalid == "zero" ? 0U : 2136U }
            } };
            Assert.ThrowsExactly<MissionRuleException>(() => MissionMarkerProjection.Validate(Definition(), scene));
        }
    }
}
