using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Rasa.Data;
    using Rasa.Game;
    using Rasa.Managers;
    using Rasa.Packets.Communicator.Server;
    using Rasa.Packets.Protocol;
    using Rasa.Repositories.UnitOfWork;
    using Rasa.Structures.Char;
    using Rasa.Test.Missions;

    /// <summary>
    /// The console's "petition resolve" runs on the console's thread and looks for the petition's
    /// author among the connected clients (PetitionManager.Resolve). It walked Server.Clients
    /// without the lock every other walk of it takes, while the main loop adds to it and takes
    /// from it every tick: a "Collection was modified" there came after the petition had been
    /// marked resolved, so the console reported a failure and the player heard nothing.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class PetitionResolveTests
    {
        private readonly List<Client> _online = new List<Client>();

        [TestCleanup]
        public void Cleanup()
        {
            lock (Server.Clients)
            {
                foreach (var client in _online)
                    Server.Clients.Remove(client);

                Server.Clients.RemoveAll(client => client == null);
            }

            _online.Clear();
        }

        [TestMethod]
        public void ResolveWaitsForTheClientListLockAndThenTellsTheAuthor()
        {
            using var context = MissionTestContext.WithCompletableMission(429);
            var author = Online(context.Client);
            var petitions = Manager(context);
            var id = File(context, author);
            WorldTestContext.Drain(author);

            using var held = new ManualResetEventSlim();
            using var release = new ManualResetEventSlim();

            // The main loop, in the middle of its walk of the list.
            var loop = new Thread(() =>
            {
                lock (Server.Clients)
                {
                    held.Set();
                    release.Wait();
                }
            });

            loop.Start();
            Task<bool> resolve;

            try
            {
                Assert.IsTrue(held.Wait(TimeSpan.FromSeconds(5)));

                resolve = Task.Run(() => petitions.Resolve(id, "Moved you out of the rock."));

                Assert.IsFalse(resolve.Wait(TimeSpan.FromMilliseconds(500)), "it waits for the list instead of walking it under the main loop");
            }
            finally
            {
                release.Set();
                loop.Join();
            }

            Assert.IsTrue(resolve.Wait(TimeSpan.FromSeconds(5)));
            Assert.IsTrue(resolve.Result, "resolved");
            Assert.AreEqual("Your petition #" + id + " has been resolved: Moved you out of the rock.", Told(author).Single());

            using var unit = context.CreateChar();
            Assert.AreEqual((byte)PetitionStatus.Resolved, unit.Petitions.GetPetition(id).Status);
        }

        [TestMethod]
        public void AnEmptyOrAnonymousEntryInTheListDoesNotStopTheAuthorBeingFound()
        {
            using var context = MissionTestContext.WithCompletableMission(429);
            var author = context.Client;
            var petitions = Manager(context);
            var id = File(context, author);

            // A connection not logged in yet has no account; one being removed can leave a gap.
            var anonymous = context.CreateAdditionalClient(2);
            typeof(Client).GetProperty(nameof(Client.AccountEntry)).SetValue(anonymous, null);

            lock (Server.Clients)
                Server.Clients.Add(null);
            Online(anonymous);
            Online(author);
            WorldTestContext.Drain(author);

            Assert.IsTrue(petitions.Resolve(id, ""));
            Assert.AreEqual("Your petition #" + id + " has been resolved.", Told(author).Single());
        }

        private Client Online(Client client)
        {
            lock (Server.Clients)
                Server.Clients.Add(client);

            _online.Add(client);
            return client;
        }

        private static PetitionManager Manager(MissionTestContext context) =>
            (PetitionManager)typeof(PetitionManager).GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null,
                new[] { typeof(IGameUnitOfWorkFactory) }, null).Invoke(new object[] { context });

        private static uint File(MissionTestContext context, Client author)
        {
            using var unit = context.CreateChar();
            var id = unit.Petitions.AddPetition(new PetitionEntry(author.AccountEntry.Id, author.Player.Id, 1, "Stuck", "In a rock.", 1220, 0, 0, 0));

            Assert.AreNotEqual(0u, id);
            return id;
        }

        private static List<string> Told(Client client) =>
            WorldTestContext.Drain(client).Select(p => p.Message).OfType<CallMethodMessage>()
                .Select(m => m.Packet).OfType<SystemMessagePacket>().Select(p => p.TextMessage).ToList();
    }
}
