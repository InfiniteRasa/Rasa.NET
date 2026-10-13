using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Rasa.Data;
    using Rasa.Game;
    using Rasa.Managers;

    // A fall is worked out from the Moves the client sends (FallDamage). A descent is any run of
    // Moves each lower than the last, and the whole of it used to be charged once any part came
    // down at fall speed: a hill walked down to a ledge, or run on down after landing, was added
    // to the ledge. 16.5 m of hillside and a 12 m ledge cost 93 % of health where the ledge alone
    // costs 10 %; 12 m onto a hill run down for 20 m more killed. Only the fall is charged now.
    //
    // The Moves here are the client's as it would send them: one every 100 ms, a steady pace down
    // a hill, free fall under gravity off a ledge, carrying the speed it had at the lip.
    [TestClass]
    [DoNotParallelize]
    public class FallDamageTests
    {
        private const float Gravity = 9.8f;

        #region What a fall costs

        [TestMethod]
        public void AFallOffALedgeCostsItsHeight()
        {
            using var world = new WorldTestContext();
            var run = new Run(world);

            run.Level(1000).Ledge(15).Level(300);

            var fall = run.Falls.Single();
            Assert.AreEqual(15f, fall.Drop, 0.1f, "the last step of a fall may be within the level tolerance");
            Assert.AreEqual(1000 - 250, run.Health, 5, "5 % a metre past 10 m");
        }

        [TestMethod]
        public void AFallTooShortToHurtCostsNothing()
        {
            using var world = new WorldTestContext();
            var run = new Run(world);

            run.Level(1000).Ledge(9).Level(300);

            Assert.AreEqual(1000, run.Health);
        }

        #endregion

        #region The hill around it

        [TestMethod]
        public void AHillWalkedDownToTheLedgeIsNotCharged()
        {
            using var world = new WorldTestContext();
            var run = new Run(world);

            // The finding's case: 16.5 m of 25-degree hillside at a run, then a 12 m ledge.
            run.Level(1000).Hill(16.5f, 3f).Ledge(12).Level(300);

            var fall = run.Falls.Single();
            Assert.AreEqual(12f, fall.Drop, 1f, "the ledge, not the hill");
            Assert.IsTrue(run.Health >= 1000 - 150, $"{run.Health} left: 12 m costs 10 %, it used to be 93 %");
        }

        [TestMethod]
        public void AHillRunDownAfterLandingIsNotCharged()
        {
            using var world = new WorldTestContext();
            var run = new Run(world);

            run.Level(1000).Ledge(12);
            var touchdown = run.Tick;
            run.Hill(20f, 3f).Level(300);

            var fall = run.Falls.Single();
            Assert.AreEqual(12f, fall.Drop, 1f, "the ledge, not the hill below it");
            Assert.AreNotEqual(Rasa.Data.CharacterState.Dead, run.Client.Player.State, "it used to kill");
            Assert.IsTrue(run.Health >= 1000 - 150);
            Assert.IsTrue(run.FallTicks.Single() - touchdown <= 1000, "paid for on the way down the hill, not at its foot");
        }

        [TestMethod]
        public void TwoLedgesWithAHillBetweenAreTwoFalls()
        {
            using var world = new WorldTestContext();
            var run = new Run(world);

            run.Level(1000).Ledge(13).Hill(10f, 3f).Ledge(14).Level(300);

            CollectionAssert.AreEqual(new[] { 13f, 14f }, run.Falls.Select(f => (float)Math.Round(f.Drop)).ToArray());
            Assert.AreEqual(1000 - 150 - 200, run.Health, 60, "each on its own: 15 % and 20 %");
        }

        [TestMethod]
        public void AShortFallOntoAHillIsChargedForTheFall()
        {
            using var world = new WorldTestContext();
            var run = new Run(world);

            // Only just fast enough to be a fall, with the hill's slower speeds straight after it.
            run.Level(1000).Ledge(11).Hill(10f, 3f).Level(300);

            var fall = run.Falls.Single();
            Assert.AreEqual(11f, fall.Drop, 1f);
        }

        [TestMethod]
        public void TwoLedgesCloseTogetherAreTwoFalls()
        {
            using var world = new WorldTestContext();
            var run = new Run(world);

            // A few strides of steep ground between them: the second fall goes back no further
            // than where the first ended.
            run.Level(1000).Ledge(13).Hill(5f, 8f).Ledge(14).Level(300);

            Assert.AreEqual(2, run.Falls.Count);
            Assert.AreEqual(13f, run.Falls[0].Drop, 1.5f);
            Assert.AreEqual(14f, run.Falls[1].Drop, 3f, "the steep ground before it at most");
        }

        [TestMethod]
        public void SprintingDownTheSteepestHillIsNoFall()
        {
            using var world = new WorldTestContext();
            var run = new Run(world);

            run.Level(1000).Hill(40f, 8f).Level(300);

            Assert.AreEqual(0, run.Falls.Count);
            Assert.AreEqual(1000, run.Health);
        }

        [TestMethod]
        public void AJumpIsNoFall()
        {
            using var world = new WorldTestContext();
            var run = new Run(world);

            run.Level(1000).Jump(1.2f).Level(300);

            Assert.AreEqual(0, run.Falls.Count);
            Assert.AreEqual(1000, run.Health);
        }

        #endregion

        #region As the server gets them

        [TestMethod]
        public void PacketsArrivingUnevenlyChangeLittle()
        {
            using var world = new WorldTestContext();
            var jitter = new Random(1664);
            var run = new Run(world, () => jitter.Next(-40, 41));

            run.Level(1000).Hill(16.5f, 3f).Ledge(20).Hill(10f, 3f).Level(300);

            var fall = run.Falls.Single();
            Assert.AreEqual(20f, fall.Drop, 1.5f);
            Assert.AreEqual(1000 - 500, run.Health, 75);
        }

        [TestMethod]
        public void UnevenPacketsDoNotLoseTheStartOfTheFall()
        {
            // Near fall speed a late or early packet moves the measured speed by more than a tenth
            // of a second of falling adds to it; the fall must still be taken from its lip.
            for (var seed = 0; seed < 20; seed++)
            {
                using var world = new WorldTestContext();
                var jitter = new Random(seed);
                var run = new Run(world, () => jitter.Next(-40, 41));

                run.Level(1000).Hill(12f, 3f).Ledge(16).Level(300);

                Assert.AreEqual(1, run.Falls.Count, $"seed {seed}");
                Assert.AreEqual(16f, run.Falls[0].Drop, 2.5f, $"seed {seed}");
            }
        }

        [TestMethod]
        public void UnevenPacketsDoNotCarryTheFallDownTheHillBelow()
        {
            // Sprinting down the steepest slope after landing, with packets 15 ms out either way.
            for (var seed = 0; seed < 20; seed++)
            {
                using var world = new WorldTestContext();
                var jitter = new Random(seed);
                var run = new Run(world, () => jitter.Next(-15, 16));

                run.Level(1000).Ledge(13).Hill(30f, 8f).Level(300);

                Assert.AreEqual(1, run.Falls.Count, $"seed {seed}");
                Assert.AreEqual(13f, run.Falls[0].Drop, 2.5f, $"seed {seed}");
            }
        }

        [TestMethod]
        public void UnevenPacketsDoNotCarryTheFallBackUpASteepHill()
        {
            // Sprinting down the steepest slope to the ledge, with packets 15 ms out either way.
            for (var seed = 0; seed < 20; seed++)
            {
                using var world = new WorldTestContext();
                var jitter = new Random(seed);
                var run = new Run(world, () => jitter.Next(-15, 16));

                run.Level(1000).Hill(20f, 8f).Ledge(13).Level(300);

                Assert.AreEqual(1, run.Falls.Count, $"seed {seed}");
                Assert.AreEqual(13f, run.Falls[0].Drop, 2.5f, $"seed {seed}");
            }
        }

        [TestMethod]
        public void BadlyUnevenPacketsCarryTheFallLittleDownTheHillBelow()
        {
            // Packets 25 ms out either way, a quarter of a step: one that comes early must not make
            // a stride down the steepest slope look like falling.
            var errors = new List<float>();

            foreach (var ledge in new[] { 14f, 16f, 20f })
                for (var seed = 0; seed < 20; seed++)
                {
                    using var world = new WorldTestContext();
                    var jitter = new Random(seed);
                    var run = new Run(world, () => jitter.Next(-25, 26));

                    run.Level(1000).Ledge(ledge).Hill(30f, 8f).Level(300);

                    Assert.AreEqual(1, run.Falls.Count, $"{ledge} m, seed {seed}");
                    errors.Add(run.Falls[0].Drop - ledge);
                }

            Assert.IsTrue(errors.Max() <= 2.5f, $"at most {errors.Max():F2} m of the hill");
            Assert.IsTrue(errors.Average() <= 0.8f, $"{errors.Average():F2} m of it on average");
        }

        [TestMethod]
        public void ADescentLandedByTheWorkerIsPaidFor()
        {
            using var world = new WorldTestContext();
            var run = new Run(world);

            // Off a ledge, and then nothing: a player who stops dead sends no more Moves.
            run.Level(1000).Ledge(15);
            Assert.AreEqual(1000, run.Health, "not down yet");

            var tracker = run.Client.Player.Fall;
            tracker.LastMoveTick = Environment.TickCount64 - FallDamage.LandingTimeoutMs;
            FallDamage.Worker(world.Map);

            Assert.AreEqual(1000 - 250, run.Health, 5);
            Assert.IsFalse(tracker.Descending);
        }

        [TestMethod]
        public void AFallIntoWaterCostsNothing()
        {
            using var world = new WorldTestContext();
            world.Map.MapInfo.MapName = "adv_arieki_ligo_burningsteps";

            // Above the 64 m pond whose surface is at 196.
            var run = new Run(world, start: new Vector3(-216f, 230f, 400f), runSpeed: 0f);

            run.Level(500).Ledge(34).Level(300);

            var fall = run.Falls.Single();
            Assert.IsTrue(fall.Water);
            Assert.AreEqual(1000, run.Health);
        }

        #endregion

        #region Fixture

        /// <summary>A player and the Moves their client sends, 100 ms apart.</summary>
        private sealed class Run
        {
            private const long StepMs = 100;

            private readonly Func<int> _jitter;
            private readonly float _runSpeed;
            private Vector3 _at;
            private long _sentAt;
            private float _verticalSpeed;

            public Run(WorldTestContext world, Func<int> jitter = null, Vector3? start = null, float runSpeed = 6.5f)
            {
                Client = PlayerDeathTests.Player(world, 0, 0);
                Client.Player.Level = 20;
                _jitter = jitter ?? (() => 0);
                _runSpeed = runSpeed;
                _at = start ?? new Vector3(0f, 600f, 0f);
                Client.Player.Position = _at;
                _sentAt = Environment.TickCount64 - 60000;
            }

            public Client Client { get; }
            public List<(float Drop, bool Water)> Falls { get; } = new();
            public List<long> FallTicks { get; } = new();
            public long Tick => _sentAt;
            public int Health => Client.Player.Attributes[Attributes.Health].Current;

            /// <summary>Running on the level for that long.</summary>
            public Run Level(int ms)
            {
                _verticalSpeed = 0;

                for (var t = 0; t < ms; t += (int)StepMs)
                    Move(_at + new Vector3(_runSpeed * StepMs / 1000f, 0f, 0f));

                return this;
            }

            /// <summary>Down a hill at a steady pace until it has dropped that far.</summary>
            public Run Hill(float drop, float metresASecond)
            {
                var bottom = _at.Y - drop;
                _verticalSpeed = metresASecond;

                while (_at.Y > bottom + 0.001f)
                    Move(new Vector3(_at.X + _runSpeed * StepMs / 1000f, Math.Max(bottom, _at.Y - metresASecond * StepMs / 1000f), _at.Z));

                return this;
            }

            /// <summary>Off a ledge that high, falling from the speed they had at the lip, down to its foot.</summary>
            public Run Ledge(float height)
            {
                var bottom = _at.Y - height;
                var v = _verticalSpeed;
                var dt = StepMs / 1000f;

                while (_at.Y > bottom + 0.001f)
                {
                    var y = _at.Y - (v * dt + 0.5f * Gravity * dt * dt);
                    v += Gravity * dt;
                    Move(new Vector3(_at.X + _runSpeed * dt, Math.Max(bottom, y), _at.Z));
                }

                _verticalSpeed = 0;
                return this;
            }

            /// <summary>Up and down again, as a jump does.</summary>
            public Run Jump(float height)
            {
                var start = _at.Y;
                var v = (float)Math.Sqrt(2 * Gravity * height);
                var dt = StepMs / 1000f;
                var y = start;

                do
                {
                    y += v * dt - 0.5f * Gravity * dt * dt;
                    v -= Gravity * dt;
                    Move(new Vector3(_at.X + _runSpeed * dt, Math.Max(start, y), _at.Z));
                }
                while (y > start);

                return this;
            }

            private void Move(Vector3 to)
            {
                var from = _at;
                _at = to;
                _sentAt += StepMs;

                Client.Player.Position = to;

                var fall = FallDamage.OnMove(Client, from, to, _sentAt + _jitter());

                if (fall.HasValue)
                {
                    Falls.Add(fall.Value);
                    FallTicks.Add(_sentAt);

                    if (!fall.Value.Water)
                        Assert.IsTrue(fall.Value.Drop > 0);
                }
            }
        }

        #endregion
    }
}
