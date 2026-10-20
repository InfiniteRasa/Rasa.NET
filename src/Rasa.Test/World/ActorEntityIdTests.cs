using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Rasa.Managers;
    using Rasa.Structures;

    /// <summary>
    /// An actor takes its entity id from the pool the first time it is read, not when it is built:
    /// a creature definition, a clone's source or a throwaway takes none, and one given an id
    /// takes none of its own.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class ActorEntityIdTests
    {
        [TestMethod]
        public void ABuiltActorHasTakenNoId()
        {
            var definition = new Creature { Name = "definition" };
            var clones = Enumerable.Range(0, 100).Select(_ => (Creature)definition.Clone()).ToList();

            Assert.IsFalse(new Actor().HasEntityId);
            Assert.IsFalse(definition.HasEntityId);
            Assert.IsTrue(clones.All(clone => !clone.HasEntityId));
        }

        [TestMethod]
        public void TheFirstReadTakesOneAndItStays()
        {
            var creature = new Creature();

            var first = creature.EntityId;

            Assert.IsTrue(creature.HasEntityId);
            Assert.AreNotEqual(0UL, first);
            Assert.AreEqual(first, creature.EntityId);
            Assert.AreEqual(first, creature.EntityId);
        }

        [TestMethod]
        public void OneGivenIsKeptAndNoneIsTaken()
        {
            var given = EntityManager.Instance.GetEntityId;
            var proxy = new Creature { EntityId = given };

            Assert.IsTrue(proxy.HasEntityId);
            Assert.AreEqual(given, proxy.EntityId);

            // Given after a read: the given one wins.
            var read = new Creature();
            _ = read.EntityId;
            read.EntityId = given;
            Assert.AreEqual(given, read.EntityId);

            EntityManager.Instance.FreeEntity(given);
        }

        [TestMethod]
        public void ClonesTakeIdsOfTheirOwn()
        {
            var definition = new Creature { Name = "definition" };
            var a = (Creature)definition.Clone();
            var b = (Creature)definition.Clone();

            Assert.AreNotEqual(a.EntityId, b.EntityId);
            Assert.IsFalse(definition.HasEntityId, "reading a clone's takes nothing for its source");
        }

        [TestMethod]
        public void FirstReadsAtOnceAllSeeTheSameId()
        {
            for (var round = 0; round < 50; round++)
            {
                var actor = new Actor();
                var seen = new ConcurrentBag<ulong>();
                using var start = new ManualResetEventSlim();
                var readers = Enumerable.Range(0, 8).Select(_ => Task.Run(() =>
                {
                    start.Wait();
                    seen.Add(actor.EntityId);
                })).ToArray();

                start.Set();
                Task.WaitAll(readers);

                Assert.AreEqual(1, seen.Distinct().Count());
                Assert.AreEqual(actor.EntityId, seen.First());
            }
        }
    }
}
