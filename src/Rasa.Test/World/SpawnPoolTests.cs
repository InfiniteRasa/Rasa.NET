using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Managers;
    using Structures;

    [TestClass]
    [DoNotParallelize]
    public class SpawnPoolTests
    {
        [ClassInitialize]
        public static void Initialize(TestContext context)
        {
            if (Logger.Config == null)
                Logger.UpdateConfig(new Logger.LoggerConfig());
        }

        [TestMethod]
        public void APoolCannotSelectMoreThan64CreaturesAcrossAllSixSlots()
        {
            var pool = new SpawnPool
            {
                SpawnSlot = new List<SpawnPoolSlot>
                {
                    new(1, 60, 60),
                    new(2, 10, 10),
                    new(3, 10, 10),
                    new(4, 10, 10),
                    new(5, 10, 10),
                    new(6, 10, 10)
                }
            };
            var definitions = Enumerable.Range(1, 6)
                .ToDictionary(id => (uint)id, id => new Creature { DbId = (uint)id });

            var creatures = SpawnPoolManager.CreateListOfCreatures(pool, definitions, new Random(1));

            Assert.HasCount(64, creatures);
            Assert.AreEqual(60, creatures.Count(creature => creature.DbId == 1));
            Assert.AreEqual(4, creatures.Count(creature => creature.DbId == 2));
        }

        [TestMethod]
        [DataRow((short)-1, (short)1)]
        [DataRow((short)2, (short)1)]
        [DataRow((short)3, (short)1)]
        public void InvalidCountRangesDoNotPreventOtherSlotsFromSpawning(short minimum, short maximum)
        {
            var pool = new SpawnPool
            {
                SpawnSlot = new List<SpawnPoolSlot> { new(1, minimum, maximum), new(2, 2, 2) }
            };
            var definitions = new Dictionary<uint, Creature>
            {
                [1] = new Creature { DbId = 1 },
                [2] = new Creature { DbId = 2 }
            };

            var creatures = SpawnPoolManager.CreateListOfCreatures(pool, definitions, new EndpointRandom(true));

            CollectionAssert.AreEqual(new uint[] { 2, 2 }, creatures.Select(creature => creature.DbId).ToArray());
        }

        [TestMethod]
        public void MissingAndUnconfiguredCreatureDefinitionsDoNotPreventValidSlotsFromSpawning()
        {
            var pool = new SpawnPool
            {
                SpawnSlot = new List<SpawnPoolSlot>
                {
                    new(99, 1, 1), new(0, 1, 1), null, new(2, 1, 1)
                }
            };
            var definitions = new Dictionary<uint, Creature> { [2] = new Creature { DbId = 2 } };

            var creatures = SpawnPoolManager.CreateListOfCreatures(pool, definitions, new Random(1));

            Assert.HasCount(1, creatures);
            Assert.AreEqual(2U, creatures[0].DbId);
        }

        [TestMethod]
        public void EmptyAndZeroCountPoolsStayEmpty()
        {
            var definitions = new Dictionary<uint, Creature> { [1] = new Creature { DbId = 1 } };
            var pool = new SpawnPool();

            Assert.IsEmpty(SpawnPoolManager.CreateListOfCreatures(pool, definitions, new Random(1)));
            pool.SpawnSlot = new List<SpawnPoolSlot>();
            Assert.IsEmpty(SpawnPoolManager.CreateListOfCreatures(pool, definitions, new Random(1)));
            pool.SpawnSlot.Add(new SpawnPoolSlot(1, 0, 0));
            Assert.IsEmpty(SpawnPoolManager.CreateListOfCreatures(pool, definitions, new Random(1)));
        }

        [TestMethod]
        [DataRow(false, 3)]
        [DataRow(true, 6)]
        public void SelectionPreservesBothConfiguredInclusiveEndpoints(bool chooseMaximum, int expected)
        {
            var pool = new SpawnPool { SpawnSlot = new List<SpawnPoolSlot> { new(47, 3, 6) } };
            var template = new Creature { DbId = 47, Level = 5 };
            var definitions = new Dictionary<uint, Creature> { [47] = template };

            var creatures = SpawnPoolManager.CreateListOfCreatures(pool, definitions, new EndpointRandom(chooseMaximum));

            Assert.HasCount(expected, creatures);
            Assert.IsTrue(creatures.All(creature => creature.DbId == 47 && creature.Level == 5));
            Assert.AreNotSame(template, creatures[0]);
        }

        private sealed class EndpointRandom : Random
        {
            private readonly bool _chooseMaximum;

            public EndpointRandom(bool chooseMaximum)
            {
                _chooseMaximum = chooseMaximum;
            }

            public override int Next(int minValue, int maxValue)
            {
                return _chooseMaximum ? maxValue - 1 : minValue;
            }
        }
    }
}
