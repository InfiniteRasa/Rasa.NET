using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Context.World;
using Rasa.Repositories.World;
using Rasa.Repositories.UnitOfWork;
using Rasa.Structures.World;

namespace Rasa.Test.Database
{
    [TestClass]
    public class OrdinaryLootRepositoryTests
    {
        [TestMethod]
        public void RepositoryRoundTripsAllRowsInStableOrderWithoutTrackingAndWithReadOnlyCollections()
        {
            using var database = new OrdinaryLootTestDatabase();
            var context = database.Context;
            context.Database.EnsureCreated();
            Seed(context);
            var snapshot = Read(context);
            CollectionAssert.AreEqual(new[] { "alpha", "empty", "zeta" },
                snapshot.Profiles.Select(entry => entry.ProfileKey).ToArray());
            CollectionAssert.AreEqual(new[] { "alpha:ammo", "alpha:equipment", "alpha:junk", "zeta:consumable" },
                snapshot.Groups.Select(entry => $"{entry.ProfileKey}:{entry.GroupKey}").ToArray());
            CollectionAssert.AreEqual(new uint[] { 3, 1, 2 },
                snapshot.Items.Select(entry => entry.ItemTemplateId).ToArray());
            CollectionAssert.AreEqual(new[] { "10:1", "10:2", "20:1" },
                snapshot.Bindings.Select(entry => $"{entry.MapContextId}:{entry.CreatureId}").ToArray());
            CollectionAssert.AreEqual(new uint[] { 10, 20 },
                snapshot.MapPolicies.Select(entry => entry.MapContextId).ToArray());

            var profile = snapshot.Profiles.First();
            Assert.AreEqual("A distribution", profile.Description);
            Assert.AreEqual("test_v1", profile.ContentRevision);
            Assert.AreEqual("Reconstructed", profile.Provenance);
            Assert.AreEqual("Test evidence", profile.EvidenceNotes);
            var group = snapshot.Groups.First();
            Assert.AreEqual(LootGroupKind.Ammunition, group.Kind);
            Assert.AreEqual(3000, group.ChanceBasisPoints);
            Assert.AreEqual(0, group.MinimumRolls);
            Assert.AreEqual(2, group.MaximumRolls);
            Assert.AreEqual(-1, group.SortOrder);
            var item = snapshot.Items.First();
            Assert.AreEqual("alpha", item.ProfileKey);
            Assert.AreEqual("ammo", item.GroupKey);
            Assert.AreEqual(7, item.Weight);
            Assert.AreEqual(8, item.MinimumQuantity);
            Assert.AreEqual(16, item.MaximumQuantity);
            Assert.AreEqual(-1, item.SortOrder);
            Assert.AreEqual("empty", snapshot.Bindings.Last().ProfileKey);
            Assert.IsTrue(snapshot.MapPolicies.First().RequireProfiles);
            Assert.IsFalse(snapshot.MapPolicies.Last().RequireProfiles);
            Assert.AreEqual(0, context.ChangeTracker.Entries().Count());

            AssertReadOnly((IList<LootProfileEntry>)snapshot.Profiles);
            AssertReadOnly((IList<OrdinaryLootGroupEntry>)snapshot.Groups);
            AssertReadOnly((IList<LootItemEntry>)snapshot.Items);
            AssertReadOnly((IList<CreatureLootBindingEntry>)snapshot.Bindings);
            AssertReadOnly((IList<LootMapPolicyEntry>)snapshot.MapPolicies);
            profile.Description = "Local edit";
            Assert.AreEqual("A distribution", context.LootProfileEntries.AsNoTracking()
                .Single(entry => entry.ProfileKey == "alpha").Description);
        }

        [TestMethod]
        public void EmptySchemaReturnsEmptyCollectionsButMissingSchemaFailsInsteadOfReturningDefaults()
        {
            using var database = new OrdinaryLootTestDatabase();
            database.Context.Database.EnsureCreated();
            var snapshot = Read(database.Context);
            Assert.AreEqual(0, snapshot.Profiles.Count);
            Assert.AreEqual(0, snapshot.Groups.Count);
            Assert.AreEqual(0, snapshot.Items.Count);
            Assert.AreEqual(0, snapshot.Bindings.Count);
            Assert.AreEqual(0, snapshot.MapPolicies.Count);
            database.Context.Database.ExecuteSqlRaw("DROP TABLE loot_item");
            Assert.ThrowsExactly<SqliteException>(() => Read(database.Context));
        }

        [TestMethod]
        public void WorldUnitOfWorkContractRequiresAnOrdinaryLootRepository()
        {
            var property = typeof(IWorldUnitOfWork).GetProperty("OrdinaryLoot");
            Assert.IsNotNull(property, "IWorldUnitOfWork must expose OrdinaryLoot.");
            Assert.AreEqual("Rasa.Repositories.World.IOrdinaryLootRepository", property.PropertyType.FullName);
            Assert.IsTrue(property.GetMethod!.IsAbstract, "No silent default repository is permitted.");
        }

        [TestMethod]
        public void SnapshotCopiesCollectionMembershipAndRemainsReadableAfterContextDisposal()
        {
            var profiles = new List<LootProfileEntry> { new() { ProfileKey = "empty" } };
            var groups = new List<OrdinaryLootGroupEntry> { new() { ProfileKey = "empty", GroupKey = "ammo" } };
            var items = new List<LootItemEntry> { new() { ProfileKey = "empty", GroupKey = "ammo", ItemTemplateId = 1 } };
            var bindings = new List<CreatureLootBindingEntry> { new() { MapContextId = 10, CreatureId = 1, ProfileKey = "empty" } };
            var policies = new List<LootMapPolicyEntry> { new() { MapContextId = 10, RequireProfiles = true } };
            var snapshot = new OrdinaryLootContentSnapshot(profiles, groups, items, bindings, policies);
            profiles.Clear();
            groups.Clear();
            items.Clear();
            bindings.Clear();
            policies.Clear();
            Assert.AreEqual(1, snapshot.Profiles.Count);
            Assert.AreEqual(1, snapshot.Groups.Count);
            Assert.AreEqual(1, snapshot.Items.Count);
            Assert.AreEqual(1, snapshot.Bindings.Count);
            Assert.AreEqual(1, snapshot.MapPolicies.Count);

            using (var database = new OrdinaryLootTestDatabase())
            {
                database.Context.Database.EnsureCreated();
                Seed(database.Context);
                snapshot = Read(database.Context);
            }
            Assert.AreEqual("alpha", snapshot.Profiles[0].ProfileKey);
            Assert.AreEqual(3U, snapshot.Items[0].ItemTemplateId);
        }

        [TestMethod]
        public void GameDiScopesAndDelegatingUnitOfWorkForwardTheSameOrdinaryLootRepository()
        {
            using var database = new OrdinaryLootTestDatabase();
            database.Context.Database.EnsureCreated();
            Seed(database.Context);
            var path = new SqliteConnectionStringBuilder(database.Context.Database.GetConnectionString()).DataSource;
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string>
            {
                ["Databases:Provider"] = "Sqlite",
                ["Databases:World:Database"] = path[..^3]
            }).Build();
            var hostContext = new HostBuilderContext(new Dictionary<object, object>()) { Configuration = configuration };
            var services = new ServiceCollection();
            typeof(GameProgram).GetMethod("ConfigureServices", BindingFlags.NonPublic | BindingFlags.Static)!
                .Invoke(null, new object[] { hostContext, services });
            var registration = services.Single(service => service.ServiceType == typeof(IOrdinaryLootRepository));
            Assert.AreEqual(ServiceLifetime.Scoped, registration.Lifetime);
            Assert.AreEqual(typeof(OrdinaryLootRepository), registration.ImplementationType);
            using var provider = services.BuildServiceProvider();
            using (var scope = provider.CreateScope())
            {
                var repository = scope.ServiceProvider.GetRequiredService<IOrdinaryLootRepository>();
                var world = scope.ServiceProvider.GetRequiredService<IWorldUnitOfWork>();
                Assert.AreSame(repository, world.OrdinaryLoot);
                using var wrapper = new DelegatingWorldUnitOfWork(world, provider.CreateScope());
                Assert.AreSame(repository, wrapper.OrdinaryLoot);
                Assert.AreEqual(3, wrapper.OrdinaryLoot.Get().Profiles.Count);
            }
            using var delegated = provider.GetRequiredService<IGameUnitOfWorkFactory>().CreateWorld();
            Assert.IsInstanceOfType<DelegatingWorldUnitOfWork>(delegated);
            Assert.AreEqual(3, delegated.OrdinaryLoot.Get().Profiles.Count);
        }

        internal static void Seed(WorldContext context)
        {
            context.MapInfoEntries.AddRange(
                new MapInfoEntry { Id = 20, MapName = "Second map" },
                new MapInfoEntry { Id = 10, MapName = "First map" });
            context.CreatureEntries.AddRange(
                new CreatureEntry { Id = 2, Comment = "Second creature" },
                new CreatureEntry { Id = 1, Comment = "First creature" });
            context.ItemTemplateEntries.AddRange(new ItemTemplateEntry { Id = 3 },
                new ItemTemplateEntry { Id = 2 }, new ItemTemplateEntry { Id = 1 });
            context.LootProfileEntries.AddRange(
                new LootProfileEntry { ProfileKey = "zeta" },
                new LootProfileEntry { ProfileKey = "empty" },
                new LootProfileEntry
                {
                    ProfileKey = "alpha", Description = "A distribution", ContentRevision = "test_v1",
                    Provenance = "Reconstructed", EvidenceNotes = "Test evidence"
                });
            context.OrdinaryLootGroupEntries.AddRange(
                new OrdinaryLootGroupEntry { ProfileKey = "zeta", GroupKey = "consumable", Kind = LootGroupKind.Consumable },
                new OrdinaryLootGroupEntry { ProfileKey = "alpha", GroupKey = "junk", Kind = LootGroupKind.Junk },
                new OrdinaryLootGroupEntry { ProfileKey = "alpha", GroupKey = "equipment", Kind = LootGroupKind.Equipment },
                new OrdinaryLootGroupEntry
                {
                    ProfileKey = "alpha", GroupKey = "ammo", Kind = LootGroupKind.Ammunition,
                    ChanceBasisPoints = 3000, MinimumRolls = 0, MaximumRolls = 2, SortOrder = -1
                });
            context.LootItemEntries.AddRange(
                new LootItemEntry
                {
                    ProfileKey = "alpha", GroupKey = "ammo", ItemTemplateId = 2, Weight = 1,
                    MinimumQuantity = 1, MaximumQuantity = 1
                },
                new LootItemEntry
                {
                    ProfileKey = "alpha", GroupKey = "ammo", ItemTemplateId = 1, Weight = 2,
                    MinimumQuantity = 1, MaximumQuantity = 1
                },
                new LootItemEntry
                {
                    ProfileKey = "alpha", GroupKey = "ammo", ItemTemplateId = 3, Weight = 7,
                    MinimumQuantity = 8, MaximumQuantity = 16, SortOrder = -1
                });
            context.CreatureLootBindingEntries.AddRange(
                new CreatureLootBindingEntry { MapContextId = 20, CreatureId = 1, ProfileKey = "empty" },
                new CreatureLootBindingEntry { MapContextId = 10, CreatureId = 2, ProfileKey = "zeta" },
                new CreatureLootBindingEntry { MapContextId = 10, CreatureId = 1, ProfileKey = "alpha" });
            context.LootMapPolicyEntries.AddRange(
                new LootMapPolicyEntry { MapContextId = 20, RequireProfiles = false },
                new LootMapPolicyEntry { MapContextId = 10, RequireProfiles = true });
            context.SaveChanges();
            context.ChangeTracker.Clear();
        }

        private static OrdinaryLootContentSnapshot Read(WorldContext context) => new OrdinaryLootRepository(context).Get();

        private static void AssertReadOnly<T>(IList<T> rows)
        {
            Assert.IsTrue(rows.IsReadOnly);
            Assert.ThrowsExactly<NotSupportedException>(() => rows.Clear());
        }
    }

    internal sealed class OrdinaryLootTestDatabase : IDisposable
    {
        private readonly string _directory = Path.GetFullPath(
            ".ordinary-loot-tests-" + Guid.NewGuid().ToString("N"));
        internal SqliteWorldContext Context { get; }

        internal OrdinaryLootTestDatabase()
        {
            Directory.CreateDirectory(_directory);
            Context = (SqliteWorldContext)PersistenceIntegrationTests.CreateContext(
                typeof(SqliteWorldContext), Path.Combine(_directory, "world"));
        }

        public void Dispose()
        {
            var connection = (SqliteConnection)Context.Database.GetDbConnection();
            Context.Dispose();
            SqliteConnection.ClearPool(connection);
            Directory.Delete(_directory, true);
        }
    }
}
