using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Networking
{
    using Rasa.Api;
    using Rasa.Api.Ingame;
    using Rasa.Structures.World;
    using Rasa.Structures.Char;

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
        public void CompletionIncludesArchivedRewardedMissionsAndNotOnlyTheLiveJournal()
        {
            var assignments = new[] {
                new CharacterMissionEntry(42, 101, 0),  // active
                new CharacterMissionEntry(42, 102, 1),  // turn-in pending
                new CharacterMissionEntry(42, 103, 4)   // rewarded, not yet archived
            };
            var history = new[] {
                new CharacterMissionHistoryEntry { CharacterId = 42, MissionId = 104, Rewarded = true, Outcome = 4 },
                new CharacterMissionHistoryEntry { CharacterId = 42, MissionId = 105, Rewarded = false, Outcome = 2 },
                new CharacterMissionHistoryEntry { CharacterId = 42, MissionId = 106, Rewarded = false, Outcome = 1 }
            };
            var completed = IngameMissionCatalog.CompletedMissionIds(assignments, history);
            CollectionAssert.AreEquivalent(new uint[] { 103, 104 }, completed.ToArray());
        }

        [TestMethod]
        public void LinkedPrerequisitesUseOnlyTheSelectedRevisionAndMissionRequirements()
        {
            var prerequisites = new[] {
                new MissionPrerequisiteEntry { MissionId = 400, ContentRevision = "current", PrerequisiteId = 1,
                    Kind = MissionPrerequisiteKind.MissionCompleted, RequiredMissionId = 200, Comment = "Finish first" },
                new MissionPrerequisiteEntry { MissionId = 400, ContentRevision = "old", PrerequisiteId = 2,
                    Kind = MissionPrerequisiteKind.MissionCompleted, RequiredMissionId = 201 },
                new MissionPrerequisiteEntry { MissionId = 400, ContentRevision = "current", PrerequisiteId = 3,
                    Kind = MissionPrerequisiteKind.MissionAccepted, RequiredMissionId = 202 },
                new MissionPrerequisiteEntry { MissionId = 400, ContentRevision = "current", PrerequisiteId = 4,
                    Kind = MissionPrerequisiteKind.PlayerLevelAtLeast, RequiredLevel = 10 },
                new MissionPrerequisiteEntry { MissionId = 401, ContentRevision = "current", PrerequisiteId = 5,
                    Kind = MissionPrerequisiteKind.MissionCompleted, RequiredMissionId = 203 }
            };
            var links = IngameMissionCatalog.LinkedPrerequisites(prerequisites, 400, "current");
            using var parsed = JsonDocument.Parse(JsonSerializer.Serialize(links));
            var array = parsed.RootElement;
            Assert.AreEqual(2, array.GetArrayLength());
            Assert.AreEqual((uint)200, array[0].GetProperty("missionId").GetUInt32());
            Assert.AreEqual("MissionCompleted", array[0].GetProperty("kind").GetString());
            Assert.AreEqual((uint)202, array[1].GetProperty("missionId").GetUInt32());
            Assert.AreEqual("MissionAccepted", array[1].GetProperty("kind").GetString());
        }

        [TestMethod]
        public void MissionGiverFallsBackToLegacyNpcAssignmentWhenNewDefinitionOmitsIt()
        {
            var legacy = new NpcMissionEntry { Id = 444, GiverId = 88 };
            Assert.AreEqual(88u, IngameMissionCatalog.MissionGiverId(
                new MissionContentDefinitionEntry { MissionId = 444, GiverId = null }, legacy));
            Assert.AreEqual(88u, IngameMissionCatalog.MissionGiverId(
                new MissionContentDefinitionEntry { MissionId = 444, GiverId = 0 }, legacy));
            Assert.AreEqual(99u, IngameMissionCatalog.MissionGiverId(
                new MissionContentDefinitionEntry { MissionId = 444, GiverId = 99 }, legacy));
            Assert.AreEqual(0u, IngameMissionCatalog.MissionGiverId(
                new MissionContentDefinitionEntry { MissionId = 444, GiverId = null }, null));
        }

        [TestMethod]
        public void GiverLocationsAreRealSpawnpoolPositionsAndNeverReceiverLocations()
        {
            var pools = new[] {
                new SpawnPoolEntry { Id = 3, MapContextId = 1220, Creature1Id = 80,
                    PosX = 14.5, PosY = 120, PosZ = -91.25 },
                new SpawnPoolEntry { Id = 1, MapContextId = 1721, Creature6Id = 80,
                    PosX = 3, PosY = 7, PosZ = 9 },
                new SpawnPoolEntry { Id = 4, MapContextId = 1220, Creature1Id = 81,
                    PosX = 200 },
                new SpawnPoolEntry { Id = 2, MapContextId = 1220, Creature2Id = 80,
                    PosX = 11 },
            };
            var locations = IngameMissionCatalog.GiverLocations(80, pools);
            Assert.AreEqual(3, locations.Count);
            Assert.AreEqual((uint)2, locations[0].spawnPoolId);
            Assert.AreEqual((uint)3, locations[1].spawnPoolId);
            Assert.AreEqual(-91.25, locations[1].z);
            Assert.AreEqual((uint)1721, locations[2].mapContextId);
            Assert.AreEqual(0, IngameMissionCatalog.GiverLocations(0, pools).Count);
            using var json = JsonDocument.Parse(JsonSerializer.Serialize(locations));
            Assert.AreEqual(14.5, json.RootElement[1].GetProperty("x").GetDouble());
            Assert.AreEqual((uint)1220, json.RootElement[1].GetProperty("mapContextId").GetUInt32());
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
