using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Rasa.Configuration;
    using Rasa.Configuration.ConnectionStrings;
    using Rasa.Configuration.ContextSetup;
    using Rasa.Context.Char;
    using Rasa.Data;
    using Rasa.Managers;
    using Rasa.Repositories.Char.CharacterTeleporter;
    using Rasa.Services.DbContext;
    using Rasa.Structures;
    using Rasa.Structures.Char;

    [TestClass]
    [DoNotParallelize]
    public class WaypointPersistenceTests
    {
        [TestMethod]
        public void DiscoveredWaypointsSurviveReopenAndRemainCharacterScoped()
        {
            var directory = Path.Combine(AppContext.BaseDirectory, "TestDatabases", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            var database = Path.Combine(directory, "travel");
            try
            {
                using var world = new WorldTestContext();
                var client = world.CreateClient();
                using (var context = Open(database))
                {
                    context.Database.Migrate();
                    var repository = new CharacterTeleporterRepository(context);
                    var manager = new DynamicObjectManager(null, updateCharacter: (_, update, value) =>
                    {
                        Assert.AreEqual(CharacterUpdate.Teleporter, update);
                        repository.Add((CharacterTeleporterEntry)value);
                    });

                    manager.CheckPlayerWaypoint(client, new WaypointInfo(20, false, WaypointType.Waypoint));
                    manager.CheckPlayerWaypoint(client, new WaypointInfo(20, false, WaypointType.Waypoint));

                    Assert.AreEqual(1, repository.Get(client.Player.Id).Count);
                    Assert.AreEqual(0, repository.Get(client.Player.Id + 1).Count);
                }
                using (var reopened = Open(database))
                {
                    var saved = new CharacterTeleporterRepository(reopened).Get(client.Player.Id);
                    Assert.AreEqual(1, saved.Count);
                    Assert.AreEqual(20U, saved[0].WaypointId);
                    Assert.AreEqual((byte)WaypointType.Waypoint, saved[0].WaypointType);
                    Assert.AreEqual(client.Player.Id, saved[0].CharacterId);
                }
            }
            finally
            {
                Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
                Directory.Delete(directory, true);
            }
        }

        internal static SqliteCharContext Open(string database)
        {
            return new SqliteCharContext(
                Options.Create(new DatabaseConfiguration
                {
                    Provider = "Sqlite",
                    Char = new DatabaseConnectionConfiguration { Database = database }
                }),
                new SqliteDbContextConfigurationService(new SqliteConnectionStringFactory()),
                new SqliteDbContextPropertyModifier());
        }
    }
}
