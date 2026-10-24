using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Rasa.Config;
    using Rasa.Data;
    using Rasa.Game;
    using Rasa.Managers;
    using Rasa.Structures;

    /// <summary>
    /// The creature think spread over the loop's passes (CreatureAi.StaggeredThink,
    /// CreatureThinkSchedule): each creature thinks every 300 ms - the whole-map think's cadence
    /// on 100 ms passes - over the time since its own last think, and only a third of a map's
    /// creatures on any one pass, where the whole map used to think on one pass in three and none
    /// on the other two.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class StaggeredThinkTests
    {
        /// <summary>A pass this long made the whole map think, and still thinks everyone (BehaviorManager.CreatureThinkInterval).</summary>
        private const long Interval = 250;

        /// <summary>CreatureAi.ThinkPeriodMs and ThinkSlots, as they ship.</summary>
        private const long Period = 300;
        private const int Slots = 9;

        private static CreatureThinkSchedule NewSchedule() => new CreatureThinkSchedule(Slots, Period, Interval);

        [TestMethod]
        public void EachGroupThinksOnItsOwnBeatOverAllTheTimeSinceItsLast()
        {
            var schedule = NewSchedule();
            var thought = new long[Slots];
            var thinks = new int[Slots];
            var perPass = new List<int>();

            for (var pass = 0; pass < 600; pass++)
            {
                schedule.Advance(100);
                perPass.Add(schedule.DueCount);

                for (var slot = 0; slot < Slots; slot++)
                {
                    var elapsed = schedule.ElapsedFor(slot);

                    if (elapsed <= 0)
                        continue;

                    thought[slot] += elapsed;
                    thinks[slot]++;
                    Assert.IsTrue(elapsed == Period || thinks[slot] == 1, $"group {slot} thought over {elapsed} ms");
                }
            }

            // A minute of 100 ms passes: every group thought every 300 ms, as the whole map did -
            // 200 thinks - and no time was lost.
            for (var slot = 0; slot < Slots; slot++)
            {
                Assert.AreEqual(60000 / Period, thinks[slot], 1, $"group {slot}");
                Assert.IsTrue(60000 - thought[slot] < Period, $"group {slot}: {thought[slot]} ms thought over");
            }

            // Three groups in nine on every pass - not all of them on one pass in three.
            Assert.IsTrue(perPass.All(count => count == 3), string.Join(",", perPass.Distinct()));
        }

        [TestMethod]
        public void UnevenPassesAndStallsDoNotBunchTheGroups()
        {
            var schedule = NewSchedule();
            var random = new Random(4);
            var thought = new long[Slots];
            var total = 0L;
            var afterStall = new List<int>();

            for (var pass = 0; pass < 3000; pass++)
            {
                // Passes of 85 to 115 ms, as Windows' sleep gives them, and now and then one of
                // a second - a stall - which brings every group due at once.
                var delta = pass % 500 == 250 ? 1000 : 85 + random.Next(31);
                schedule.Advance(delta);
                total += delta;

                if (delta == 1000)
                    Assert.AreEqual(Slots, schedule.DueCount, "a pass longer than the period thinks everyone");
                else if (pass % 500 > 250 && pass % 500 < 260)
                    afterStall.Add(schedule.DueCount);

                for (var slot = 0; slot < Slots; slot++)
                    thought[slot] += schedule.ElapsedFor(slot);
            }

            for (var slot = 0; slot < Slots; slot++)
                Assert.IsTrue(total - thought[slot] < Period + 115, $"group {slot}: {thought[slot]} of {total} ms");

            // Right after a stall the groups are as far apart as before it: two to four a pass.
            Assert.IsTrue(afterStall.All(count => count >= 2 && count <= 4), string.Join(",", afterStall));
        }

        [TestMethod]
        public void APassOfAWholeIntervalThinksEveryoneOverIt()
        {
            var schedule = NewSchedule();

            // 250 ms made the whole map think, and makes every group think - every pass of it.
            for (var pass = 0; pass < 5; pass++)
            {
                schedule.Advance(Interval);

                Assert.AreEqual(Slots, schedule.DueCount);

                for (var slot = 0; slot < Slots; slot++)
                    Assert.AreEqual(Interval, schedule.ElapsedFor(slot));
            }

            schedule.Advance(3500);

            for (var slot = 0; slot < Slots; slot++)
                Assert.AreEqual(3500, schedule.ElapsedFor(slot));

            // And 249 ms does not: only the groups whose beat it holds.
            schedule.Advance(Interval - 1);
            Assert.IsTrue(schedule.DueCount < Slots, $"{schedule.DueCount}");
        }

        [TestMethod]
        public void ACreatureThinksAsOftenAndOverAsMuchTimeStaggeredOrNot()
        {
            var whole = Run(staggered: false, out var wholePasses, out var wholeThinks);
            var staggered = Run(staggered: true, out var staggeredPasses, out var staggeredThinks);

            // Over 30 s of 100 ms passes each creature thought over all of it but its last
            // period, either way, and as many times.
            foreach (var (id, ms) in whole)
                Assert.IsTrue(30000 - ms < Period, $"whole-map think: creature {id} thought over {ms} ms");

            foreach (var (id, ms) in staggered)
                Assert.IsTrue(30000 - ms < Period, $"staggered: creature {id} thought over {ms} ms");

            // 30 s at 300 ms: a hundred thinks each, give or take the first.
            foreach (var (id, count) in wholeThinks.Concat(staggeredThinks))
                Assert.AreEqual(30000 / Period, count, 1, $"creature {id}: thinks");

            Assert.AreEqual(wholeThinks.Values.Sum(), staggeredThinks.Values.Sum(), whole.Count, "as many thinks in all");

            // The whole map on one pass in three, against a third of it on every pass.
            Console.WriteLine($"creatures thinking per pass: whole {string.Join(",", wholePasses.Distinct().OrderBy(n => n))}, " +
                              $"staggered {string.Join(",", staggeredPasses.Distinct().OrderBy(n => n))} of {whole.Count}; " +
                              $"thinks in all {wholeThinks.Values.Sum()} against {staggeredThinks.Values.Sum()}");
            Assert.AreEqual(whole.Count, wholePasses.Max());
            Assert.AreEqual(wholePasses.Count / 3, wholePasses.Count(n => n > 0), 1);
            Assert.IsTrue(staggeredPasses.Max() <= whole.Count / 2, $"at most {staggeredPasses.Max()} of {whole.Count} on a pass");
            Assert.IsTrue(staggeredPasses.Skip(3).All(n => n > 0), "someone thinks on every pass");
        }

        [TestMethod]
        public void OneThinkOfAWholeIntervalThinksEveryCreatureOnce()
        {
            var previous = BehaviorManager.Config;
            BehaviorManager.Config = new CreatureAiConfig { StaggeredThink = true };

            try
            {
                using var world = new WorldTestContext();
                var creatures = Enumerable.Range(0, 40).Select(i => Spawn(world, new Vector3(i * 7f, 0, 0))).ToList();

                for (var pass = 0; pass < 4; pass++)
                {
                    var before = creatures.Select(c => c.LastAgression).ToList();
                    BehaviorManager.Instance.MapChannelThink(world.Map, Interval);

                    for (var i = 0; i < creatures.Count; i++)
                        Assert.AreEqual(before[i] + Interval, creatures[i].LastAgression, $"creature {i}, think {pass}");
                }
            }
            finally
            {
                BehaviorManager.Config = previous;
            }
        }

        /// <summary>Forty idle creatures thinking for 30 s of 100 ms passes: how much time each thought over, and how many thought on each pass.</summary>
        private static Dictionary<ulong, long> Run(bool staggered, out List<int> perPass, out Dictionary<ulong, int> thinks)
        {
            var previous = BehaviorManager.Config;
            BehaviorManager.Config = new CreatureAiConfig { StaggeredThink = staggered };
            perPass = new List<int>();
            thinks = new Dictionary<ulong, int>();

            try
            {
                using var world = new WorldTestContext();
                var creatures = Enumerable.Range(0, 40).Select(i => Spawn(world, new Vector3(i * 7f, 0, (i % 5) * 7f))).ToList();

                foreach (var creature in creatures)
                    thinks[creature.EntityId] = 0;

                for (var ms = 0L; ms < 30000; ms += 100)
                {
                    var before = creatures.Select(c => c.LastAgression).ToList();
                    BehaviorManager.Instance.MapChannelThink(world.Map, 100);
                    var thought = creatures.Where((c, i) => c.LastAgression != before[i]).ToList();
                    perPass.Add(thought.Count);

                    foreach (var creature in thought)
                        thinks[creature.EntityId]++;
                }

                return creatures.ToDictionary(c => c.EntityId, c => c.LastAgression);
            }
            finally
            {
                BehaviorManager.Config = previous;
            }
        }

        private static Creature Spawn(WorldTestContext world, Vector3 at)
        {
            var creature = new Creature
            {
                Name = "Idler",
                TargetCategory = TargetCategory.Hostile,
                MapContextId = world.Map.MapInfo.MapContextId,
                RuntimeMapChannel = world.Map,
                Position = at,
                EntityClass = EntityClasses.HumanBaseMale,
                State = CharacterState.Idle,
                Level = 1,
                AggroRange = 0,
                WalkSpeed = 0,
                RunSpeed = 0,
                AppearanceData = new Dictionary<EquipmentData, AppearanceData>()
            };

            creature.HomePos.Position = creature.Position;
            creature.HomePos.MapContextid = creature.MapContextId;
            creature.Attributes[Attributes.Health] = CreatureHealth.NewAttribute(1000);
            BehaviorManager.StartWandering(creature, false);
            EntityManager.Instance.RegisterEntity(creature.EntityId, EntityType.Creature);
            EntityManager.Instance.RegisterCreature(creature);
            EntityManager.Instance.RegisterActor(creature.EntityId, creature);
            var seed = CellManager.Instance.GetCellSeed(creature.Position);
            creature.Cells = CellManager.Instance.CreateCellMatrix(world.Map, seed & 0xFFFF, seed >> 16);
            CellManager.Instance.GetCell(world.Map, seed & 0xFFFF, seed >> 16).CreatureList.Add(creature);
            creature.LastAgression = 0;
            return creature;
        }
    }
}
