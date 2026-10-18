using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Api;
using Rasa.Context.World;
using Rasa.Loot;
using Rasa.Managers;
using Rasa.Repositories.Char;
using Rasa.Repositories.UnitOfWork;
using Rasa.Repositories.World;
using Rasa.Structures.World;
using Rasa.Test.Database;
using Rasa.Test.Gameplay;
using Rasa.Config;
using Rasa.Data;
using Rasa.Structures;
using System.Collections.Generic;
using System.Net;

namespace Rasa.Test.World
{
    [TestClass]
    [DoNotParallelize]
    public class WeightedLootStoreTests
    {
        private static object Store(IGameUnitOfWorkFactory factory)
        {
            var type = typeof(MonsterFlagStore).Assembly.GetType("Rasa.Managers.WeightedLootStore");
            Assert.IsNotNull(type, "The editor must use a canonical weighted store, not an archive writer.");
            return Activator.CreateInstance(type, factory);
        }

        private static OrdinaryLootContentSnapshot Read(object store) =>
            (OrdinaryLootContentSnapshot)store.GetType().GetMethod("Read")!.Invoke(store, null);

        private static StoreAnswer Replace(object store, OrdinaryLootContentSnapshot snapshot)
        {
            try { return (StoreAnswer)store.GetType().GetMethod("Replace")!.Invoke(store, new object[] { snapshot }); }
            catch (TargetInvocationException error) { throw error.InnerException!; }
        }

        [TestMethod]
        public void EditingWeightsChangesRealRollsOnlyAfterCommitAndSurvivesReload()
        {
            using var world = new Database();
            using var initial = world.CreateWorld();
            using var scope = new OrdinaryLootCatalogScope(initial);
            var store = Store(world);
            var original = Read(store);
            var edit = Candidate(original);
            var binding = edit.Bindings.Single();
            Assert.IsTrue(Replace(store, edit).Done);
            var captured = OrdinaryLootCatalog.Current.Resolve(binding.MapContextId, binding.CreatureId);
            var second = edit.Items[1].ItemTemplateId;
            Assert.AreEqual(second, captured.Roll(Draw).Single().TemplateId);

            edit.Items[0].Weight = 3;
            edit.Items[1].Weight = 1;
            world.BeforeSave = () => Assert.AreSame(captured,
                OrdinaryLootCatalog.Current.Resolve(binding.MapContextId, binding.CreatureId), "No pre-commit publication.");
            var done = Replace(store, edit);
            Assert.IsTrue(done.Done, string.Join("\n", done.Problems));
            var published = OrdinaryLootCatalog.Current.Resolve(binding.MapContextId, binding.CreatureId);
            Assert.AreEqual(edit.Items[0].ItemTemplateId, published.Roll(Draw).Single().TemplateId);
            Assert.AreEqual(second, captured.Roll(Draw).Single().TemplateId, "An already captured immutable profile stays intact.");
            edit.Items[0].Weight = 900;
            Assert.AreEqual(3, Read(store).Items.Single(row =>
                row.ProfileKey == "editor_test" && row.ItemTemplateId == edit.Items[0].ItemTemplateId).Weight);
            Assert.AreEqual(3, published.Groups.Single().Candidates[0].Weight);
            using var reload = world.CreateWorld();
            var loaded = OrdinaryLootCatalog.Load(reload).Resolve(binding.MapContextId, binding.CreatureId);
            Assert.AreEqual(published.Roll(Draw).Single(), loaded.Roll(Draw).Single());
            Assert.AreEqual(original.Bindings.Count, Read(store).Bindings.Count, "Unnamed managed bindings are preserved.");
            Assert.AreEqual(original.Profiles.Count + 1, Read(store).Profiles.Count);
            Assert.IsTrue(Read(store).MapPolicies.Single(row => row.MapContextId == 1220).RequireProfiles);
        }

        private static int Draw(int minimum, int maximum) =>
            minimum == 0 && maximum == 4 ? 1 : minimum;

        [TestMethod]
        public void CandidateRowsArePrivatelyCopiedBeforeValidationAndPersistence()
        {
            using var world = new Database();
            using var initial = world.CreateWorld();
            using var scope = new OrdinaryLootCatalogScope(initial);
            var store = Store(world);
            var edit = Candidate(Read(store));
            world.BeforeSave = () =>
            {
                edit.Items[0].ItemTemplateId = uint.MaxValue;
                edit.Groups[0].ChanceBasisPoints = 0;
                edit.Profiles[0].Description = "mutated";
            };
            Assert.IsTrue(Replace(store, edit).Done);
            var saved = Read(store);
            Assert.AreEqual(10000, saved.Groups.Single(row => row.ProfileKey == "editor_test").ChanceBasisPoints);
            Assert.IsFalse(saved.Items.Any(row => row.ItemTemplateId == uint.MaxValue));
            Assert.AreEqual("Editor fixture", saved.Profiles.Single(row => row.ProfileKey == "editor_test").Description);
        }

        [TestMethod]
        [DataRow("key", "Loot keys")]
        [DataRow("native", "native template")]
        [DataRow("weight", "weight")]
        [DataRow("quantity", "quantity")]
        [DataRow("chance", "chance")]
        [DataRow("rolls", "roll")]
        [DataRow("metadata", "revision")]
        [DataRow("coverage", "binding")]
        [DataRow("required", "required")]
        public void InvalidEditsLeaveDatabaseAndRuntimeUnchanged(string fault, string diagnostic)
        {
            using var world = new Database();
            using var initial = world.CreateWorld();
            using var scope = new OrdinaryLootCatalogScope(initial);
            var before = OrdinaryLootCatalog.Current;
            var store = Store(world);
            var original = Read(store);
            var edit = Candidate(original);
            switch (fault)
            {
                case "key":
                    edit.Profiles[0].ProfileKey = "Upper Case";
                    edit.Groups[0].ProfileKey = "Upper Case";
                    foreach (var item in edit.Items) item.ProfileKey = "Upper Case";
                    edit.Bindings[0].ProfileKey = "Upper Case";
                    break;
                case "native": edit.Items[0].ItemTemplateId = uint.MaxValue; break;
                case "weight": edit.Items[0].Weight = 0; break;
                case "quantity": edit.Items[0].MaximumQuantity = int.MaxValue; break;
                case "chance": edit.Groups[0].ChanceBasisPoints = 10001; break;
                case "rolls": edit.Groups[0].MaximumRolls = 33; break;
                case "metadata": edit.Profiles[0].ContentRevision = new string('x', 33); break;
                case "coverage":
                    edit = new OrdinaryLootContentSnapshot(edit.Profiles, edit.Groups, edit.Items, edit.Bindings,
                        new[] { new LootMapPolicyEntry { MapContextId = 1148, RequireProfiles = true } });
                    break;
                case "required":
                    edit = new OrdinaryLootContentSnapshot(edit.Profiles, edit.Groups, edit.Items, edit.Bindings,
                        new[] { new LootMapPolicyEntry { MapContextId = 1220, RequireProfiles = false } });
                    break;
            }
            var answer = Replace(store, edit);
            Assert.IsFalse(answer.Done);
            StringAssert.Contains(string.Join("\n", answer.Problems).ToLowerInvariant(), diagnostic.ToLowerInvariant());
            Assert.AreEqual(JsonSerializer.Serialize(original), JsonSerializer.Serialize(Read(store)));
            Assert.AreSame(before, OrdinaryLootCatalog.Current);
        }

        [TestMethod]
        public void DatabaseFailureRollsBackEveryRowWithoutPublication()
        {
            using var world = new Database();
            using var initial = world.CreateWorld();
            using var scope = new OrdinaryLootCatalogScope(initial);
            var store = Store(world);
            var before = OrdinaryLootCatalog.Current;
            var original = Read(store);
            using (var context = world.Open())
                context.Database.ExecuteSqlRaw(
                    "CREATE TRIGGER fail_editor BEFORE INSERT ON loot_item WHEN NEW.profile_key = 'editor_test' " +
                    "BEGIN SELECT RAISE(ABORT, 'editor failure'); END;");
            var answer = Replace(store, Candidate(original));
            Assert.IsFalse(answer.Done, "Persistence errors must not report a successful edit.");
            StringAssert.Contains(string.Join("\n", answer.Problems), "database");
            Assert.AreEqual(JsonSerializer.Serialize(original), JsonSerializer.Serialize(Read(store)));
            Assert.AreSame(before, OrdinaryLootCatalog.Current);
        }

        [TestMethod]
        public void NonemptyArchivesBlockReadAndSaveWithoutActivationOrArchiveWrites()
        {
            using var world = new Database();
            using var initial = world.CreateWorld();
            using var scope = new OrdinaryLootCatalogScope(initial);
            var store = Store(world);
            var edit = Candidate(Read(store));
            var before = OrdinaryLootCatalog.Current;
            using (var context = world.Open())
                context.Database.ExecuteSqlRaw(
                    "INSERT INTO legacy_flat_loot_group (id, name, comment) VALUES (91, 'retained', 'export me');");
            var answer = Replace(store, edit);
            Assert.IsFalse(answer.Done);
            StringAssert.Contains(string.Join("\n", answer.Problems), "legacy_flat_loot_group");
            Assert.AreSame(before, OrdinaryLootCatalog.Current);
            using var check = world.Open();
            Assert.AreEqual("retained", check.LootGroupEntries.Single().Name);
            Assert.IsFalse(check.LootProfileEntries.Any(row => row.ProfileKey == "editor_test"));
            try { Read(store); Assert.Fail("Archive guard must also protect editor GET."); }
            catch (TargetInvocationException error) { Assert.IsInstanceOfType<LegacyFlatLootArchiveException>(error.InnerException); }
        }

        [TestMethod]
        public void AuthenticatedApiRoundTripsTheRealCanonicalStoreAndReportsSaveFailures()
        {
            using var world = new Database();
            using var initial = world.CreateWorld();
            using var scope = new OrdinaryLootCatalogScope(initial);
            var store = (IWeightedLootStore)Store(world);
            var api = new ApiServer();
            api.Register(new WeightedLootEndpoint { Store = store });
            api.Register(new UpdateWeightedLootEndpoint { Store = store });
            var config = new RestApiConfig { ApiKey = "owned-test-only" };
            config.Endpoints["lootpools"] = new ApiEndpointConfig();
            config.Endpoints["updatelootpools"] = new ApiEndpointConfig();
            api.Apply(config);
            ApiResponse Call(string method, string body = null, string key = "owned-test-only")
            {
                var request = new ApiRequest { Method = method, Path = method == "POST" ? "/updatelootpools" : "/lootpools",
                    Body = body, Remote = IPAddress.Loopback };
                request.Headers["X-API-Key"] = key;
                request.Headers["Content-Type"] = "application/json";
                return api.Respond(request);
            }
            Assert.AreEqual(401, Call("GET", key: "wrong").Status);
            Assert.AreEqual(401, Call("POST", "{}", "wrong").Status);
            var read = Call("GET");
            Assert.AreEqual(200, read.Status, read.Json);
            var before = OrdinaryLootCatalog.Current;
            Assert.AreEqual(200, Call("POST", read.Json).Status, "GET is an exact canonical round trip.");
            var durable = Call("GET").Json;
            Assert.AreEqual(read.Json, durable);
            var privateCatalog = OrdinaryLootCatalog.Current;
            Assert.AreNotSame(before, privateCatalog);
            Assert.AreEqual(400, Call("POST", "{\"format\":\"rasa-loot-tables\",\"version\":1,\"groups\":[]}").Status);
            var candidate = Candidate(store.Read());
            var body = JsonSerializer.Serialize(new
            {
                format = WeightedLootEndpoint.Format, version = WeightedLootEndpoint.Version,
                profiles = candidate.Profiles, groups = candidate.Groups, items = candidate.Items,
                bindings = candidate.Bindings, mapPolicies = candidate.MapPolicies
            }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
            using (var context = world.Open())
                context.Database.ExecuteSqlRaw("CREATE TRIGGER fail_editor BEFORE INSERT ON loot_item " +
                    "WHEN NEW.profile_key = 'editor_test' BEGIN SELECT RAISE(ABORT, 'API editor failure'); END;");
            var failed = Call("POST", body);
            Assert.AreEqual(500, failed.Status, failed.Json);
            StringAssert.Contains(failed.Json, "World database");
            Assert.AreSame(privateCatalog, OrdinaryLootCatalog.Current);
            Assert.AreEqual(durable, Call("GET").Json);
            using (var context = world.Open())
                context.Database.ExecuteSqlRaw("INSERT INTO legacy_flat_loot_group (id, name, comment) VALUES (92, 'retained', 'API guard');");
            var blocked = Call("GET");
            Assert.AreEqual(503, blocked.Status);
            StringAssert.Contains(blocked.Json, "legacy_flat_loot_group");
            Assert.AreSame(privateCatalog, OrdinaryLootCatalog.Current);
            api.Stop();
        }

        [TestMethod]
        [DataRow("groups", "chanceBasisPoints", "omitted")]
        [DataRow("groups", "minimumRolls", "omitted")]
        [DataRow("groups", "maximumRolls", "omitted")]
        [DataRow("groups", "sortOrder", "omitted")]
        [DataRow("groups", "kind", "omitted")]
        [DataRow("groups", "admissionAndRolls", "omitted")]
        [DataRow("mapPolicies", "requireProfiles", "omitted")]
        [DataRow("groups", "chanceBasisPoints", "null")]
        [DataRow("mapPolicies", "requireProfiles", "null")]
        [DataRow("groups", "kind", "\"4\"")]
        [DataRow("groups", "sortOrder", "1.5")]
        [DataRow("mapPolicies", "requireProfiles", "\"false\"")]
        [DataRow("groups", "duplicateAdmission", "omitted")]
        public void OmittedFieldsCannotChangeTheRealStoreOrPublishedCatalog(string collection, string field, string fault)
        {
            using var world = new Database();
            using var initial = world.CreateWorld();
            using var scope = new OrdinaryLootCatalogScope(initial);
            var store = (IWeightedLootStore)Store(world);
            var endpoint = new UpdateWeightedLootEndpoint { Store = store };
            var get = new WeightedLootEndpoint { Store = store };
            var before = get.Handle(new ApiRequest()).Json;
            var catalog = OrdinaryLootCatalog.Current;
            var body = JsonNode.Parse(before);
            var row = body[collection][0].AsObject();
            foreach (var name in field == "admissionAndRolls" ?
                new[] { "chanceBasisPoints", "minimumRolls", "maximumRolls" } :
                field == "duplicateAdmission" ? new[] { "chanceBasisPoints" } : new[] { field })
                if (fault == "omitted") row.Remove(name);
                else row[name] = fault == "null" ? null : JsonNode.Parse(fault);
            var reads = world.WorldUnitsCreated;
            var json = body.ToJsonString();
            if (field == "duplicateAdmission")
            {
                var original = JsonNode.Parse(before).ToJsonString();
                json = original[..^1] + ",\"GROUPS\":" + body[collection].ToJsonString() + "}";
            }
            var request = new ApiRequest { Body = json };
            request.Headers["Content-Type"] = "application/json";

            var answer = endpoint.Handle(request);

            Assert.AreEqual(400, answer.Status, answer.Json);
            Assert.AreEqual(reads, world.WorldUnitsCreated, "Reject before the real store opens a World unit.");
            Assert.AreEqual(before, get.Handle(new ApiRequest()).Json, "Durable readback is unchanged.");
            Assert.AreSame(catalog, OrdinaryLootCatalog.Current, "No runtime publication.");
        }

        [TestMethod]
        public void ExplicitZeroGroupAndOptionalPolicyRoundTripThroughTheRealApiStore()
        {
            using var world = new Database();
            using var initial = world.CreateWorld();
            using var scope = new OrdinaryLootCatalogScope(initial);
            var store = (IWeightedLootStore)Store(world);
            var candidate = Candidate(store.Read());
            candidate.Groups[0].ChanceBasisPoints = 0;
            candidate.Groups[0].MinimumRolls = 0;
            candidate.Groups[0].MaximumRolls = 0;
            var body = JsonSerializer.Serialize(new
            {
                format = WeightedLootEndpoint.Format, version = WeightedLootEndpoint.Version,
                profiles = candidate.Profiles, groups = candidate.Groups, items = candidate.Items,
                bindings = candidate.Bindings,
                mapPolicies = new[] { new LootMapPolicyEntry { MapContextId = 1148, RequireProfiles = false } }
            }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
            var request = new ApiRequest { Body = body, Remote = IPAddress.Loopback };
            request.Headers["Content-Type"] = "application/json";
            var answer = new UpdateWeightedLootEndpoint { Store = store }.Handle(request);
            Assert.AreEqual(200, answer.Status, answer.Json);
            var saved = store.Read();
            var group = saved.Groups.Single(row => row.ProfileKey == "editor_test");
            Assert.AreEqual(0, group.ChanceBasisPoints);
            Assert.AreEqual(0, group.MinimumRolls);
            Assert.AreEqual(0, group.MaximumRolls);
            Assert.IsFalse(saved.MapPolicies.Single(row => row.MapContextId == 1148).RequireProfiles);
            Assert.IsTrue(saved.MapPolicies.Single(row => row.MapContextId == 1220).RequireProfiles);
            var binding = candidate.Bindings.Single();
            Assert.IsEmpty(OrdinaryLootCatalog.Current.Resolve(binding.MapContextId, binding.CreatureId).Roll(Draw));
        }

        [TestMethod]
        public void MonsterFlagStoreStillPreservesUnnamedClassesAndCapturedFlagLists()
        {
            using var world = new Database();
            var amoeboid = new EntityClass(6032, "Bane_Amoeboid_v1", 0, 0, new List<AugmentationType> { AugmentationType.Creature }, true);
            var creepa = new EntityClass(10215, "Ambient_Cavern_Creepa", 0, 0, new List<AugmentationType> { AugmentationType.Creature }, true);
            var classes = new Dictionary<EntityClasses, EntityClass>
            {
                [(EntityClasses)6032] = amoeboid, [(EntityClasses)10215] = creepa
            };
            var store = new MonsterFlagStore(world, () => classes);
            var read = store.Read();
            // The seed's BIOLOGICAL and SPECIES_AMOEBOID, and the monster flag editor's (CreatureFlagEdits).
            CollectionAssert.AreEqual(new uint[] { 1, 4, 5, 14, 15, 23, 30, 40, 71, 128, 142, 143 },
                read.Single(row => row.ClassId == 6032).Flags.OrderBy(flag => flag).ToArray());
            amoeboid.CreatureFlags = new List<CreatureFlag> { CreatureFlag.Biological, (CreatureFlag)71 };
            var captured = amoeboid.CreatureFlags;
            using var before = world.Open();
            var untouched = before.CreatureClassFlagEntries.AsNoTracking().First(row => row.ClassId != 6032 && row.ClassId != 10215);
            Assert.IsFalse(store.Replace(new[] { new MonsterFlags(6032, "", new uint[] { 999 }) }).Done);
            Assert.AreSame(captured, amoeboid.CreatureFlags);
            Assert.IsTrue(store.Replace(new[]
            {
                new MonsterFlags(6032, "", new uint[] { 142, 5, 71 }),
                new MonsterFlags(10215, "", new uint[] { 1 })
            }).Done);
            Assert.AreNotSame(captured, amoeboid.CreatureFlags);
            Assert.HasCount(2, captured);
            CollectionAssert.AreEqual(new[] { CreatureFlag.Biological, (CreatureFlag)71, CreatureFlag.VulnerableFire }, amoeboid.CreatureFlags);
            CollectionAssert.AreEqual(new[] { CreatureFlag.KingdomAnimal }, creepa.CreatureFlags);
            using var check = world.Open();
            Assert.IsTrue(check.CreatureClassFlagEntries.Any(row => row.ClassId == untouched.ClassId && row.FlagId == untouched.FlagId));
            Assert.IsTrue(store.Replace(new[] { new MonsterFlags(10215, "", null) }).Done);
            Assert.IsEmpty(creepa.CreatureFlags);
            CollectionAssert.AreEqual(new uint[] { 5, 71, 142 }, store.Read().Single(row => row.ClassId == 6032).Flags.ToArray());
        }

        private static OrdinaryLootContentSnapshot Candidate(OrdinaryLootContentSnapshot content)
        {
            var equipment = content.Groups.Where(row => row.Kind == LootGroupKind.Equipment)
                .Select(row => (row.ProfileKey, row.GroupKey)).ToHashSet();
            var templates = content.Items.Where(row => equipment.Contains((row.ProfileKey, row.GroupKey)))
                .Select(row => row.ItemTemplateId).Distinct().Take(2).ToArray();
            var binding = content.Bindings.First();
            return new OrdinaryLootContentSnapshot(
                new[] { new LootProfileEntry { ProfileKey = "editor_test", Description = "Editor fixture",
                    ContentRevision = "editor_v1", Provenance = "Test", EvidenceNotes = "Not gameplay balance" } },
                new[] { new OrdinaryLootGroupEntry { ProfileKey = "editor_test", GroupKey = "gear",
                    Kind = LootGroupKind.Equipment, ChanceBasisPoints = 10000, MinimumRolls = 1, MaximumRolls = 1 } },
                templates.Select((id, index) => new LootItemEntry { ProfileKey = "editor_test", GroupKey = "gear",
                    ItemTemplateId = id, Weight = index == 0 ? 1 : 3, MinimumQuantity = 1, MaximumQuantity = 1, SortOrder = index }),
                new[] { new CreatureLootBindingEntry { MapContextId = binding.MapContextId,
                    CreatureId = binding.CreatureId, ProfileKey = "editor_test" } },
                Array.Empty<LootMapPolicyEntry>());
        }

        private sealed class Database : IGameUnitOfWorkFactory, IDisposable
        {
            private readonly string _directory = Path.GetFullPath(".weighted-editor-tests-" + Guid.NewGuid().ToString("N"));
            internal Action BeforeSave { get; set; }
            internal int WorldUnitsCreated { get; private set; }
            internal Database()
            {
                if (Logger.Config == null) Logger.UpdateConfig(new Logger.LoggerConfig());
                Directory.CreateDirectory(_directory);
                using var context = Open();
                MigratedDatabaseTemplates.Migrate(context, () => context.Database.Migrate());
            }
            internal SqliteWorldContext Open() => (SqliteWorldContext)PersistenceIntegrationTests.CreateContext(
                typeof(SqliteWorldContext), Path.Combine(_directory, "world"));
            public ICharUnitOfWork CreateChar() => throw new NotSupportedException();
            public IWorldUnitOfWork CreateWorld()
            {
                WorldUnitsCreated++;
                var c = Open();
                c.SavingChanges += (_, _) => BeforeSave?.Invoke();
                return new WorldUnitOfWork(c, new ActionRepository(c), new EquipmentRepository(c),
                    new CreatureRepository(c), new EntityClassRepository(c), new FootlockerRepository(c),
                    new LogosRepository(c), new MapInfoRepository(c), new MapLinkRepository(c),
                    new KraftwerksRepository(c), new MapRegionRepository(c), new MapMarkerRepository(c),
                    new MapEmitterRepository(c), new SpawnPoolArrivalRepository(c), new RecipeRepository(c),
                    new NpcMissionRepository(c), new NpcMissionRewardRepository(c), new MissionContentRepository(c),
                    new OrdinaryLootRepository(c), new NpcPackageRepository(c), new PlayerRandomNameRepository(c),
                    new SpawnpoolRepository(c), new TeleporterRepository(c));
            }
            public void Dispose()
            {
                Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
                Directory.Delete(_directory, true);
            }
        }
    }
}
