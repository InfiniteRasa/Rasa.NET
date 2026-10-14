using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Networking
{
    using Rasa.Api;
    using Rasa.Api.Ingame;
    using Rasa.Structures.World;

    [TestClass]
    public class IngameMissionsEndpointTests
    {
        [TestMethod]
        public void MissionCatalogAndDetailsAreReadOnlyAndRequireLiveGameSession()
        {
            var sessions = new IngameSessionService();
            var catalog = new IngameMissionsEndpoint(sessions);
            var details = new IngameMissionDetailsEndpoint(sessions);
            Assert.AreEqual("ingame/missions", catalog.Name);
            Assert.AreEqual("ingame/missions/{id}", details.Name);
            Assert.AreEqual("GET", catalog.Method);
            Assert.AreEqual("GET", details.Method);
            Assert.IsFalse(catalog.RequiresApiKey);
            Assert.IsFalse(details.RequiresApiKey);
            Assert.IsTrue(details.Matches("ingame/missions/1449"));
            Assert.IsFalse(details.Matches("ingame/missions/1449/extra"));
            Assert.AreEqual(401, catalog.Handle(new ApiRequest { Method = "GET", Remote = IPAddress.Loopback }).Status);
            Assert.AreEqual(401, details.Handle(new ApiRequest { Method = "GET", Remote = IPAddress.Loopback }).Status);
        }

        [TestMethod]
        public void MissionMapAssociationsUseStoredEvidenceAndLeaveUnknownMissionsUnknown()
        {
            var definitions = new[] {
                new MissionContentDefinitionEntry { MissionId = 11, GiverId = 500 },
                new MissionContentDefinitionEntry { MissionId = 12 },
                new MissionContentDefinitionEntry { MissionId = 13 }
            };
            var areas = new[] { new MissionAreaEntry { MissionId = 11, MapContextId = 1220 } };
            var groups = new[] { new MissionSpawnGroupEntry { MissionId = 11, MapContextId = 1721 } };
            var steps = new[] { new MissionScenarioStepEntry { MissionId = 13, MapContextId = 2000 } };
            var pools = new[] { new SpawnPoolEntry { Creature1Id = 500, MapContextId = 1220 } };
            var maps = IngameMissionCatalog.FindMissionMaps(definitions, Array.Empty<NpcMissionEntry>(),
                areas, groups, steps, pools);
            CollectionAssert.AreEqual(new uint[] { 1220, 1721 }, maps[11].ToArray());
            Assert.IsFalse(maps.ContainsKey(12));
            CollectionAssert.AreEqual(new uint[] { 2000 }, maps[13].ToArray());
            // Historical content from another revision must not move the live mission.
            var staleAreas = areas.Concat(new[] { new MissionAreaEntry {
                MissionId = 11, MapContextId = 9000, ContentRevision = "stale" } });
            maps = IngameMissionCatalog.FindMissionMaps(definitions, Array.Empty<NpcMissionEntry>(),
                staleAreas, groups, steps, pools);
            CollectionAssert.AreEqual(new uint[] { 1220, 1721 }, maps[11].ToArray());
        }

        [TestMethod]
        public void DetailProjectionNeverTraversesNavigationProperties()
        {
            var definition = new MissionContentDefinitionEntry { MissionId = 21, Comment = "An example" };
            definition.Objectives.Add(new MissionObjectiveDefinitionEntry { MissionId = 21, Content = definition });
            var row = IngameMissionCatalog.Rows(new[] { definition }).Single();
            Assert.AreEqual((uint)21, row["missionId"]);
            Assert.AreEqual("An example", row["comment"]);
            Assert.IsFalse(row.ContainsKey("objectives"));
        }
    }
}
