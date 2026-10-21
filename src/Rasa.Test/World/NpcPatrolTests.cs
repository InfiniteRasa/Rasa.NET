using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Rasa.Context.World;
    using Rasa.Data;
    using Rasa.Game;
    using Rasa.Managers;
    using Rasa.Models;
    using Rasa.Packets;
    using Rasa.Packets.Protocol;
    using Rasa.Services.Preloader;
    using Rasa.Structures;
    using Rasa.Structures.World;
    using Rasa.Test.Database;
    using Rasa.Test.Missions;

    /// <summary>
    /// An NPC walking a beat (Patrols): that it walks its steps in straight lines at its walk
    /// speed and round again, stops, turns and stands where a step says to, turns before it
    /// walks, leaves the beat for a fight and comes back to it. And the pools' beats
    /// (spawnpool_patrol) with the Training Officer seeded with one at the Proving Grounds.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class NpcPatrolTests
    {
        /// <summary>The server's yaw of a walk along +x and along -x: atan2(-dx, -dz).</summary>
        private const float East = -MathF.PI / 2f;
        private const float West = MathF.PI / 2f;

        private const long Tick = 250;

        private const string Migration = "20261125000000_Add_npc_patrols";
        private const string Before = "20261124000000_Stand_npcs_on_their_floors";

        #region Walking the beat

        [TestMethod]
        public void ItWalksFromStepToStepAtItsWalkSpeedAndBackToTheFirst()
        {
            using var world = new WorldTestContext();
            var watcher = Watch(world, x: 40);
            var sentry = Spawn(world, x: 10, walkSpeed: 2, facing: East);

            sentry.Patrol = new[] { new PatrolStep(new Vector3(10, 0, 0)), new PatrolStep(new Vector3(14, 0, 0)) };

            var moves = Record(world, watcher, sentry, 40);

            Assert.AreEqual(BehaviorManager.BehaviorActionPatrol, sentry.Controller.CurrentAction);

            // Out: eight steps of half a metre, the last of them exactly onto the far step.
            var walking = moves.Where(move => move.Movement.Velocity > 0).ToList();
            var out_ = walking.Take(8).ToList();

            for (var index = 0; index < out_.Count; index++)
            {
                Assert.AreEqual(2f, out_[index].Movement.Velocity);
                Assert.AreEqual(East, out_[index].Movement.ViewDirection.X, 0.0001f);
                Assert.AreEqual(10f + 0.5f * (index + 1), out_[index].Movement.Position.X, 0.0001f);
                Assert.AreEqual(out_[0].Tick + index, out_[index].Tick, "A step a think.");
            }

            Assert.IsTrue(Near(new Vector3(14, 0, 0), out_[7].Movement.Position));

            // There: stopped where it is, exactly on the step, as the last step goes out.
            var stop = moves.First(move => move.Movement.Velocity == 0f);

            Assert.AreEqual(out_[7].Tick, stop.Tick);
            Assert.AreEqual(new Vector3(14, 0, 0), stop.Movement.Position);
            Assert.AreEqual(East, stop.Movement.ViewDirection.X, 0.0001f);

            // Turned about, and back the way it came.
            var back = moves.Where(move => move.Tick > out_[7].Tick && move.Movement.Velocity > 0).Take(8).ToList();

            Assert.HasCount(8, back);
            Assert.AreEqual(West, back[0].Movement.ViewDirection.X, 0.0001f);
            Assert.AreEqual(13.5f, back[0].Movement.Position.X, 0.0001f);
            Assert.IsTrue(Near(new Vector3(10, 0, 0), back[7].Movement.Position));

            // And out again: the first step follows the last.
            Assert.IsTrue(moves.Any(move => move.Tick > back[7].Tick && move.Movement.Velocity > 0 && move.Movement.ViewDirection.X < 0),
                "It sets off up the beat again.");
        }

        [TestMethod]
        public void ItTurnsAboutWhereItStandsBeforeItWalksBack()
        {
            using var world = new WorldTestContext();
            var watcher = Watch(world, x: 40);
            var sentry = Spawn(world, x: 10, walkSpeed: 2, facing: East);

            sentry.Patrol = new[] { new PatrolStep(new Vector3(10, 0, 0)), new PatrolStep(new Vector3(12, 0, 0)) };

            var moves = Record(world, watcher, sentry, 14);
            var there = moves.Where(move => Near(new Vector3(12, 0, 0), move.Movement.Position)).ToList();

            // The last step, the stop, and one stop more with the new facing: the turn.
            Assert.HasCount(3, there);
            Assert.AreEqual(0f, there[1].Movement.Velocity);
            Assert.AreEqual(East, there[1].Movement.ViewDirection.X, 0.0001f);
            Assert.AreEqual(0f, there[2].Movement.Velocity);
            Assert.AreEqual(West, there[2].Movement.ViewDirection.X, 0.0001f);
            Assert.AreEqual(there[1].Tick + 1, there[2].Tick);

            // Half a turn at a whole turn a second is half a second: it walks two thinks on.
            var off = moves.First(move => move.Tick > there[2].Tick);

            Assert.AreEqual(500, Patrols.TurnMs(MathF.PI));
            Assert.AreEqual(there[2].Tick + 2, off.Tick);
            Assert.AreEqual(2f, off.Movement.Velocity);
            Assert.AreEqual(West, off.Movement.ViewDirection.X, 0.0001f);
            Assert.AreEqual(11.5f, off.Movement.Position.X, 0.0001f);
        }

        [TestMethod]
        public void OnAStepWithAFacingItStopsTurnsToItStandsOutThePauseAndTurnsBackToWalkOn()
        {
            using var world = new WorldTestContext();
            var watcher = Watch(world, x: 40);
            var officer = Spawn(world, x: 10, walkSpeed: 2, facing: East);

            officer.Patrol = new[]
            {
                new PatrolStep(new Vector3(10, 0, 0)),
                new PatrolStep(new Vector3(12, 0, 0), facing: 0f, pauseMs: 2000),
                new PatrolStep(new Vector3(14, 0, 0))
            };

            // Long enough to see it off the step, and not so long that it comes back through it.
            var moves = Record(world, watcher, officer, 22);
            var there = moves.Where(move => Near(new Vector3(12, 0, 0), move.Movement.Position)).ToList();

            // The last step onto it, the stop, the turn to the facing, the turn back to the beat.
            Assert.HasCount(4, there);
            Assert.AreEqual(2f, there[0].Movement.Velocity);
            Assert.AreEqual(0f, there[1].Movement.Velocity);
            Assert.AreEqual(East, there[1].Movement.ViewDirection.X, 0.0001f);
            Assert.AreEqual(there[0].Tick, there[1].Tick, "It is stopped as it arrives.");
            Assert.AreEqual(0f, there[2].Movement.Velocity);
            Assert.AreEqual(0f, there[2].Movement.ViewDirection.X);
            Assert.AreEqual(there[1].Tick + 1, there[2].Tick);
            Assert.AreEqual(0f, there[3].Movement.Velocity);
            Assert.AreEqual(East, there[3].Movement.ViewDirection.X, 0.0001f);

            // A quarter turn is a think, and then the two seconds of the pause, facing that way.
            Assert.AreEqual(250, Patrols.TurnMs(MathF.PI / 2f));
            Assert.AreEqual(there[2].Tick + 9, there[3].Tick);

            // The quarter turn back, and on to the next step.
            var on = moves.First(move => move.Tick > there[3].Tick);

            Assert.AreEqual(there[3].Tick + 1, on.Tick);
            Assert.AreEqual(2f, on.Movement.Velocity);
            Assert.AreEqual(12.5f, on.Movement.Position.X, 0.0001f);
        }

        [TestMethod]
        public void WhileItStandsOnAStepItFacesTheStepsWayForWhoeverComesUponIt()
        {
            using var world = new WorldTestContext();
            var watcher = Watch(world, x: 40);
            var officer = Spawn(world, x: 10, walkSpeed: 2, facing: East);

            officer.Patrol = new[]
            {
                new PatrolStep(new Vector3(10, 0, 0)),
                new PatrolStep(new Vector3(11, 0, 0), facing: 1.25f, pauseMs: 60000)
            };

            Record(world, watcher, officer, 16);

            Assert.AreEqual(new Vector3(11, 0, 0), officer.Position);
            Assert.AreEqual(1.25f, officer.LastYaw);
            Assert.AreEqual(1.25f, (float)officer.Rotation, "What a client that comes upon it is told (ActorInfo, WorldLocationDescriptor).");
            Assert.IsTrue(officer.Controller.ActionPatrol.Arrived);

            // And for the whole of the pause nothing more is said of it.
            Assert.IsEmpty(Record(world, watcher, officer, 40));
        }

        [TestMethod]
        public void AStepStraightAheadWithNothingToDoOnItIsWalkedThroughWithoutAStop()
        {
            using var world = new WorldTestContext();
            var watcher = Watch(world, x: 40);
            var sentry = Spawn(world, x: 10, walkSpeed: 2, facing: East);

            sentry.Patrol = new[]
            {
                new PatrolStep(new Vector3(10, 0, 0)),
                new PatrolStep(new Vector3(12, 0, 0)),
                new PatrolStep(new Vector3(14, 0, 0))
            };

            var moves = Record(world, watcher, sentry, 14);
            var through = moves.Single(move => Near(new Vector3(12, 0, 0), move.Movement.Position));
            var out_ = moves.Where(move => move.Movement.Velocity > 0).Take(8).ToList();

            Assert.AreEqual(2f, through.Movement.Velocity, "It walks onto the step, and is not stopped on it.");
            Assert.IsTrue(Near(new Vector3(14, 0, 0), out_[7].Movement.Position));

            for (var index = 1; index < out_.Count; index++)
                Assert.AreEqual(out_[index - 1].Tick + 1, out_[index].Tick, "No think is lost on the step.");
        }

        [TestMethod]
        public void ACornerInTheBeatIsTurnedWhereItStands()
        {
            using var world = new WorldTestContext();
            var watcher = Watch(world, x: 40);
            var sentry = Spawn(world, x: 10, walkSpeed: 2, facing: East);

            // East, then along -z, which is yaw 0.
            sentry.Patrol = new[]
            {
                new PatrolStep(new Vector3(10, 0, 0)),
                new PatrolStep(new Vector3(12, 0, 0)),
                new PatrolStep(new Vector3(12, 0, -2))
            };

            var moves = Record(world, watcher, sentry, 16);
            var corner = moves.Where(move => Near(new Vector3(12, 0, 0), move.Movement.Position)).ToList();

            Assert.HasCount(3, corner);
            Assert.AreEqual(0f, corner[1].Movement.Velocity);
            Assert.AreEqual(0f, corner[2].Movement.Velocity);
            Assert.AreEqual(0f, corner[2].Movement.ViewDirection.X, 0.0001f);

            var on = moves.First(move => move.Tick > corner[2].Tick);

            Assert.AreEqual(2f, on.Movement.Velocity);
            Assert.IsTrue(Near(new Vector3(12, 0, -0.5f), on.Movement.Position));
        }

        [TestMethod]
        public void TheWalkIsAStraightLineBetweenTheStepsHeightsAndAll()
        {
            using var world = new WorldTestContext();
            var watcher = Watch(world, x: 40);
            var sentry = Spawn(world, x: 10, walkSpeed: 2, facing: East);

            // Four along and three up: five metres, ten thinks.
            sentry.Patrol = new[] { new PatrolStep(new Vector3(10, 0, 0)), new PatrolStep(new Vector3(14, 3, 0), pauseMs: 60000) };

            var walking = Record(world, watcher, sentry, 16).Where(move => move.Movement.Velocity > 0).ToList();

            Assert.HasCount(10, walking);

            for (var index = 0; index < walking.Count; index++)
            {
                Assert.AreEqual(10f + 0.4f * (index + 1), walking[index].Movement.Position.X, 0.0001f);
                Assert.AreEqual(0.3f * (index + 1), walking[index].Movement.Position.Y, 0.0001f);
            }

            Assert.AreEqual(new Vector3(14, 3, 0), sentry.Position);
        }

        #endregion

        #region Leaving the beat and coming back

        [TestMethod]
        public void AFightTakesItOffItsBeatAndItComesBackToTheStepItWasWalkingTo()
        {
            using var world = new WorldTestContext();
            var watcher = Watch(world, x: 40);
            var sentry = Spawn(world, x: 10, walkSpeed: 2, facing: East);

            sentry.Patrol = new[]
            {
                new PatrolStep(new Vector3(10, 0, 0)),
                new PatrolStep(new Vector3(20, 0, 0), pauseMs: 60000)
            };

            Record(world, watcher, sentry, 6);

            Assert.AreEqual(1, sentry.Controller.ActionPatrol.Step);
            Assert.IsGreaterThan(10f, sentry.Position.X);

            Assert.IsTrue(BehaviorManager.Instance.TrySetActionFighting(sentry, watcher.Player.EntityId));
            Assert.AreEqual(BehaviorManager.BehaviorActionFighting, sentry.Controller.CurrentAction);

            // The fight carried it well off the line, and is over.
            sentry.Position = new Vector3(15, 0, 6);
            BehaviorManager.Instance.StopFighting(sentry);
            sentry.TargetCategory = TargetCategory.Friendly;

            Assert.AreEqual(BehaviorManager.BehaviorActionWander, sentry.Controller.CurrentAction);
            Assert.AreEqual(1, sentry.Controller.ActionPatrol.Step, "Its place on the beat is kept.");

            Record(world, watcher, sentry, 2);

            Assert.AreEqual(BehaviorManager.BehaviorActionPatrol, sentry.Controller.CurrentAction);
            Assert.IsTrue(sentry.Controller.ActionPatrol.Rejoining);

            Record(world, watcher, sentry, 30);

            Assert.AreEqual(new Vector3(20, 0, 0), sentry.Position);
            Assert.IsFalse(sentry.Controller.ActionPatrol.Rejoining);
            Assert.IsTrue(sentry.Controller.ActionPatrol.Arrived);
        }

        [TestMethod]
        public void ItsHomeGoesAlongTheBeatSoAFightFarFromThePoolsPointIsNoChaseTooFar()
        {
            using var world = new WorldTestContext();
            var watcher = Watch(world, x: 80);
            var sentry = Spawn(world, x: 10, walkSpeed: 2, facing: East);

            sentry.Patrol = new[] { new PatrolStep(new Vector3(10, 0, 0)), new PatrolStep(new Vector3(75, 0, 0), pauseMs: 60000) };

            Record(world, watcher, sentry, 140);

            Assert.AreEqual(new Vector3(75, 0, 0), sentry.Position, $"at {sentry.Position}");
            Assert.AreEqual(sentry.Position, sentry.HomePos.Position);
            Assert.IsFalse(BehaviorManager.ChasedTooFar(sentry), "65 m from where it started, and at home.");
        }

        [TestMethod]
        public void AHitOnItsBeatStartsAFightAsOneOnAStrollDoes()
        {
            using var world = new WorldTestContext();
            var watcher = Watch(world, x: 40);
            var sentry = Spawn(world, x: 10, walkSpeed: 2, facing: East);

            sentry.Patrol = new[] { new PatrolStep(new Vector3(10, 0, 0)), new PatrolStep(new Vector3(20, 0, 0)) };
            Record(world, watcher, sentry, 6);

            Threat.FromDamage(sentry, watcher.Player, 10);

            Assert.AreEqual(BehaviorManager.BehaviorActionFighting, sentry.Controller.CurrentAction);
            Assert.AreEqual(watcher.Player.EntityId, sentry.Controller.ActionFighting.TargetEntityId);
        }

        [TestMethod]
        public void OneMovedWhileItStandsOnAStepGoesBackToItAndStandsItsPauseAgain()
        {
            using var world = new WorldTestContext();
            var watcher = Watch(world, x: 40);
            var officer = Spawn(world, x: 10, walkSpeed: 2, facing: East);

            officer.Patrol = new[]
            {
                new PatrolStep(new Vector3(10, 0, 0)),
                new PatrolStep(new Vector3(12, 0, 0), facing: 0f, pauseMs: 60000)
            };

            Record(world, watcher, officer, 12);

            Assert.IsTrue(officer.Controller.ActionPatrol.Faced);
            Assert.AreEqual(new Vector3(12, 0, 0), officer.Position);

            // Shoved half a metre down the line by something that is not its patrol.
            officer.Position = new Vector3(12.5f, 0, 0);

            var moves = Record(world, watcher, officer, 12);

            Assert.AreEqual(new Vector3(12, 0, 0), officer.Position);
            Assert.IsTrue(officer.Controller.ActionPatrol.Faced);
            Assert.AreEqual(0f, officer.LastYaw);
            Assert.AreEqual(1, officer.Controller.ActionPatrol.Step);
            Assert.AreEqual(0f, moves.Last().Movement.Velocity);
            Assert.AreEqual(0f, moves.Last().Movement.ViewDirection.X);
            Assert.IsGreaterThan(55000L, officer.Controller.ActionPatrol.WaitMs, "The pause is stood from the start.");
        }

        [TestMethod]
        public void OneWithNoWalkSpeedHasNoBeatAndOneWhoseBeatIsTakenAwayStrollsAgain()
        {
            using var world = new WorldTestContext();
            var watcher = Watch(world, x: 40);
            var legless = Spawn(world, x: 10, walkSpeed: 0, facing: East);
            var sentry = Spawn(world, x: 20, walkSpeed: 2, facing: East);
            var beat = new[] { new PatrolStep(new Vector3(10, 0, 0)), new PatrolStep(new Vector3(12, 0, 0)) };

            legless.Patrol = beat;
            sentry.Patrol = new[] { new PatrolStep(new Vector3(20, 0, 0)), new PatrolStep(new Vector3(22, 0, 0)) };

            Assert.IsFalse(Patrols.Has(legless));
            Assert.IsTrue(Patrols.Has(sentry));
            Assert.IsFalse(Patrols.Has(Spawn(world, x: 30, walkSpeed: 2, facing: East)), "One with no beat.");

            var moves = Moves(world, watcher, 12);

            Assert.AreEqual(BehaviorManager.BehaviorActionWander, legless.Controller.CurrentAction);
            Assert.AreEqual(0, moves.Count(move => move.EntityId == legless.EntityId));
            Assert.AreEqual(BehaviorManager.BehaviorActionPatrol, sentry.Controller.CurrentAction);

            sentry.Patrol = null;
            Moves(world, watcher, 2);

            Assert.AreEqual(BehaviorManager.BehaviorActionWander, sentry.Controller.CurrentAction);
        }

        [TestMethod]
        public void ABeatOfOneStepIsAPostWithAFacing()
        {
            using var world = new WorldTestContext();
            var watcher = Watch(world, x: 40);
            var sentry = Spawn(world, x: 13, walkSpeed: 2, facing: East);

            sentry.Patrol = new[] { new PatrolStep(new Vector3(10, 0, 0), facing: 2f, pauseMs: 5000) };

            var moves = Record(world, watcher, sentry, 60);

            Assert.AreEqual(new Vector3(10, 0, 0), sentry.Position);
            Assert.AreEqual(2f, sentry.LastYaw);

            // The walk to it, the stop, the turn to its facing - and then nothing, pause after pause.
            var last = moves.Last();

            Assert.AreEqual(2f, last.Movement.ViewDirection.X);
            Assert.AreEqual(0f, last.Movement.Velocity);
            Assert.IsLessThan(16, last.Tick);
        }

        #endregion

        #region Turns, and the rows

        [TestMethod]
        public void ATurnIsTheShortWayRound()
        {
            Assert.AreEqual(0f, Patrols.Turn(1f, 1f));
            Assert.AreEqual(-MathF.PI / 2f, Patrols.Turn(MathF.PI / 2f, 0f), 0.0001f);
            Assert.AreEqual(MathF.PI / 2f, Patrols.Turn(0f, MathF.PI / 2f), 0.0001f);

            // 6.2816 is just short of a whole turn: a hair to the left of 0, not nearly all the way round.
            Assert.AreEqual(-0.0016f, Patrols.Turn(0f, 6.2816f), 0.0001f);
            Assert.AreEqual(0.2f, Patrols.Turn(6.1832f, 0.1f), 0.001f);
            Assert.AreEqual(MathF.PI, MathF.Abs(Patrols.Turn(East, West)), 0.0001f);

            Assert.AreEqual(0, Patrols.TurnMs(0f));
            Assert.AreEqual(250, Patrols.TurnMs(-MathF.PI / 2f));
            Assert.AreEqual(1000, Patrols.TurnMs(2f * MathF.PI));
        }

        [TestMethod]
        public void AStepIsWalkedThroughOnlyWithNothingToDoOnItAndTheNextOneAhead()
        {
            var line = new[]
            {
                new PatrolStep(new Vector3(0, 0, 0)),
                new PatrolStep(new Vector3(4, 0, 0)),
                new PatrolStep(new Vector3(8, 0, 0), pauseMs: 1),
                new PatrolStep(new Vector3(12, 0, 0), facing: East),
                new PatrolStep(new Vector3(16, 0, 0))
            };

            Assert.IsTrue(Patrols.WalksOn(line, 1, East));
            Assert.IsFalse(Patrols.WalksOn(line, 1, West), "It is facing back the way it came.");
            Assert.IsFalse(Patrols.WalksOn(line, 2, East), "A pause.");
            Assert.IsFalse(Patrols.WalksOn(line, 3, East), "A facing, though it is the way it walks.");
            Assert.IsFalse(Patrols.WalksOn(line, 4, East), "The first step is behind it.");
            Assert.IsFalse(Patrols.WalksOn(new[] { line[0] }, 0, East), "A beat of one step.");
            Assert.IsFalse(Patrols.WalksOn(new[] { line[0], new PatrolStep(new Vector3(0, 2, 0)) }, 0, East), "The next step is this place.");

            // Fifteen degrees off is made on the move; twenty-five is turned first.
            var bend = new[] { line[0], new PatrolStep(new Vector3(4, 0, 0)), new PatrolStep(new Vector3(8, 0, 1.07f)) };
            var corner = new[] { line[0], new PatrolStep(new Vector3(4, 0, 0)), new PatrolStep(new Vector3(8, 0, 1.87f)) };

            Assert.IsTrue(Patrols.WalksOn(bend, 1, East));
            Assert.IsFalse(Patrols.WalksOn(corner, 1, East));
        }

        [TestMethod]
        public void TheRowsOfAPoolAreItsStepsInOrderAndARowThatIsNoPlaceLeavesThePoolWithNone()
        {
            var patrols = Patrols.FromRows(new[]
            {
                new SpawnPoolPatrolEntry { PoolId = 7, Step = 20, PosX = 3, PosY = 1, PosZ = 2 },
                new SpawnPoolPatrolEntry { PoolId = 9, Step = 0, PosX = 5, PosY = double.NaN, PosZ = 5 },
                new SpawnPoolPatrolEntry { PoolId = 7, Step = 10, PosX = 1, PosY = 1, PosZ = 2, Facing = 1.5, PauseMs = 4000 },
                new SpawnPoolPatrolEntry { PoolId = 9, Step = 1, PosX = 6, PosY = 5, PosZ = 5 },
                new SpawnPoolPatrolEntry { PoolId = 11, Step = 0, PosX = 6, PosY = 5, PosZ = 5, Facing = double.PositiveInfinity }
            });

            CollectionAssert.AreEqual(new uint[] { 7 }, patrols.Keys.ToArray());

            var steps = patrols[7];

            Assert.HasCount(2, steps);
            Assert.AreEqual(new Vector3(1, 1, 2), steps[0].Position);
            Assert.AreEqual(1.5f, steps[0].Facing);
            Assert.AreEqual(4000, steps[0].PauseMs);
            Assert.AreEqual(new Vector3(3, 1, 2), steps[1].Position);
            Assert.IsNull(steps[1].Facing);
            Assert.AreEqual(0, steps[1].PauseMs);
        }

        [TestMethod]
        public void AGameMasterIsToldWhereItIsOnItsBeat()
        {
            using var world = new WorldTestContext();
            var watcher = Watch(world, x: 40);
            var sentry = Spawn(world, x: 10, walkSpeed: 2, facing: East);

            Assert.AreEqual("none", Patrols.Describe(sentry));

            sentry.Patrol = new[] { new PatrolStep(new Vector3(10, 0, 0)), new PatrolStep(new Vector3(12, 0, 0), pauseMs: 60000) };

            Assert.AreEqual("off it, going to step 0 of 2 when it is idle", Patrols.Describe(sentry));

            Record(world, watcher, sentry, 4);
            Assert.AreEqual("walking to step 1 of 2", Patrols.Describe(sentry));

            Record(world, watcher, sentry, 8);
            Assert.AreEqual("standing on step 1 of 2", Patrols.Describe(sentry));

            sentry.Position = new Vector3(20, 0, 0);
            Record(world, watcher, sentry, 1);
            Assert.AreEqual("rejoining at step 1 of 2", Patrols.Describe(sentry));

            sentry.WalkSpeed = 0;
            Assert.AreEqual("2 steps, not walked: it has no walk speed", Patrols.Describe(sentry));
        }

        #endregion

        #region The Training Officer

        [TestMethod]
        public void TheSeedIsATrainingOfficerWithAFourStepBeatAtTheProvingGroundsAndNoOtherPoolHasOne()
        {
            var directory = Path.Combine(AppContext.BaseDirectory, "TestDatabases", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);

            try
            {
                var database = Path.Combine(directory, "world");

                using (var context = PersistenceIntegrationTests.CreateContext(typeof(SqliteWorldContext), database))
                {
                    MigratedDatabaseTemplates.Migrate(context, () => context.Database.Migrate());

                    // As they were seeded: Add_bootcamp_ambient_guards, after, put figures in their place.
                    context.GetService<IMigrator>().Migrate(BootcampAmbientGuardTests.Before);
                }

                using (var context = (WorldContext)PersistenceIntegrationTests.CreateContext(typeof(SqliteWorldContext), database))
                {
                    var rows = new Rasa.Repositories.World.SpawnpoolRepository(context).GetPatrols();

                    Assert.HasCount(4, rows);
                    Assert.IsTrue(rows.All(row => row.PoolId == BootcampTrainingOfficer.PoolId));
                    CollectionAssert.AreEqual(new uint[] { 0, 1, 2, 3 }, rows.Select(row => row.Step).ToArray());

                    // South end, the two stops, north end: a straight line along x on the floor.
                    CollectionAssert.AreEqual(new[] { 383.832, 380.4414, 376.8125, 375.7891 }, rows.Select(row => row.PosX).ToArray());
                    Assert.IsTrue(rows.All(row => row.PosY == 119.58 && row.PosZ == 148.59));

                    // He stops on the way north only, turned a quarter turn from the way he walks, for four seconds.
                    CollectionAssert.AreEqual(new double?[] { null, 0.0, 0.0, null }, rows.Select(row => row.Facing).ToArray());
                    CollectionAssert.AreEqual(new uint[] { 0, 4000, 4000, 0 }, rows.Select(row => row.PauseMs).ToArray());

                    var pool = context.SpawnPoolEntries.AsNoTracking().Single(entry => entry.Id == BootcampTrainingOfficer.PoolId);

                    Assert.AreEqual(1985u, pool.MapContextId);
                    Assert.AreEqual(rows[0].PosX, pool.PosX, "He is made on the first step.");
                    Assert.AreEqual(rows[0].PosY, pool.PosY);
                    Assert.AreEqual(rows[0].PosZ, pool.PosZ);
                    Assert.AreEqual(Math.PI / 2, pool.Rotation, 0.0001, "Facing up the beat.");
                    Assert.AreEqual(BootcampTrainingOfficer.CreatureId, pool.Creature1Id);
                    Assert.AreEqual((byte)1, pool.Creature1MinCount);
                    Assert.AreEqual((byte)1, pool.Creature1MaxCount);
                    Assert.AreEqual(0u, pool.Creature2Id);
                    Assert.AreEqual(0.0, pool.Radius);
                    Assert.AreEqual((byte)0, pool.Mode);

                    Assert.IsFalse(new Rasa.Repositories.World.SpawnpoolRepository(context).GetPoses().Any(row => row.Id == pool.Id),
                        "A pose is for standing at a post.");

                    var creature = context.CreatureEntries.AsNoTracking().Single(entry => entry.Id == BootcampTrainingOfficer.CreatureId);
                    var classes = context.EntityClassEntries.AsNoTracking().ToDictionary(entry => entry.Id, entry => entry.ClassName);

                    Assert.AreEqual("NPC_Corman_Swapset_Male", classes[creature.ClassId]);
                    Assert.AreEqual(10603u, creature.NameId, "creaturenamelanguage: Training Officer");
                    Assert.AreEqual(1u, creature.Faction, "friendly");
                    Assert.AreEqual(0u, creature.Action1, "no attack: safe ground");
                    Assert.AreEqual(1u, creature.WalkSpeed, "A slow march.");
                    Assert.AreEqual(0u, creature.RunSpeed);

                    // The officer's uniform and cap, as Lt Col Cimoch wears them.
                    var worn = context.CreatureAppearanceEntries.AsNoTracking().Where(row => row.Id == creature.Id).ToList();
                    var cimoch = context.CreatureAppearanceEntries.AsNoTracking().Where(row => row.Id == 118).ToList();

                    CollectionAssert.AreEquivalent(
                        cimoch.Select(row => (row.SlotId, row.ClassId, row.Color)).ToList(),
                        worn.Select(row => (row.SlotId, row.ClassId, row.Color)).ToList());
                    Assert.AreEqual("NPC_Clothing_Officer_3_Helmet", classes[worn.Single(row => row.SlotId == 1).ClassId]);

                    // Down takes the table and the officer away and leaves the rest; Up puts them back.
                    // Counted at this migration: what a later one adds goes with that one.
                    var migrator = context.GetService<IMigrator>();
                    migrator.Migrate(Migration);

                    var pools = context.SpawnPoolEntries.AsNoTracking().Count();
                    var creatures = context.CreatureEntries.AsNoTracking().Count();

                    migrator.Migrate(Before);

                    Assert.AreEqual(0, context.Database
                        .SqlQueryRaw<int>("SELECT COUNT(*) AS Value FROM sqlite_master WHERE type = 'table' AND name = 'spawnpool_patrol'")
                        .AsEnumerable().Single());
                    Assert.AreEqual(pools - 1, context.SpawnPoolEntries.AsNoTracking().Count());
                    Assert.AreEqual(creatures - 1, context.CreatureEntries.AsNoTracking().Count());
                    Assert.IsFalse(context.SpawnPoolEntries.AsNoTracking().Any(entry => entry.Id == BootcampTrainingOfficer.PoolId));
                    Assert.IsFalse(context.CreatureEntries.AsNoTracking().Any(entry => entry.Id == BootcampTrainingOfficer.CreatureId));
                    Assert.IsFalse(context.CreatureAppearanceEntries.AsNoTracking().Any(row => row.Id == BootcampTrainingOfficer.CreatureId));
                    Assert.IsFalse(context.CreatureStatEntries.AsNoTracking().Any(row => row.Id == BootcampTrainingOfficer.CreatureId));
                    Assert.HasCount(4, new Rasa.Repositories.World.SpawnpoolRepository(context).GetPoses(), "The posted NPCs are as they were.");

                    migrator.Migrate(Migration);

                    Assert.HasCount(4, new Rasa.Repositories.World.SpawnpoolRepository(context).GetPatrols());
                    Assert.AreEqual(pools, context.SpawnPoolEntries.AsNoTracking().Count());
                    Assert.AreEqual(creatures, context.CreatureEntries.AsNoTracking().Count());
                    Assert.AreEqual(6, context.CreatureAppearanceEntries.AsNoTracking().Count(row => row.Id == BootcampTrainingOfficer.CreatureId));
                }
            }
            finally
            {
                try { Directory.Delete(directory, true); } catch (IOException) { }
            }
        }

        [TestMethod]
        public void TheMigrationMakesTheSameTableAndRowsOnMySql()
        {
            using var context = PersistenceIntegrationTests.CreateContext(typeof(MySqlWorldContext), "unused");
            var migrator = context.GetService<IMigrator>();
            var up = migrator.GenerateScript(Before, Migration);
            var down = migrator.GenerateScript(Migration, Before);

            StringAssert.Contains(up, "CREATE TABLE `spawnpool_patrol`");
            StringAssert.Contains(up, "`pool_id` int unsigned NOT NULL");
            StringAssert.Contains(up, "`facing` double NULL");
            StringAssert.Contains(up, "`pause_ms` int unsigned NOT NULL");
            StringAssert.Contains(up, "PRIMARY KEY (`pool_id`, `step`)");

            // The second stop, whose x is a whole number of sixteenths, and an end, which has no facing.
            StringAssert.Contains(up, "(400005, 2, 376.8125, 119.58, 148.59, 0.0, 4000)");
            StringAssert.Contains(up, "119.58, 148.59, NULL, 0)");
            StringAssert.Contains(up, "'Training Officer on patrol', 6339, 1, 5, 555, 10603, 0, 1,");
            StringAssert.Contains(up, "(400005, 1, 26677, 4286886614)");
            StringAssert.Contains(up, $"'{Migration}'");

            StringAssert.Contains(down, "DROP TABLE `spawnpool_patrol`");

            foreach (var table in new[] { "spawnpool", "creature_stat", "creature_appearance", "creature" })
                StringAssert.Contains(down, $"delete from {table} where id = 400005;");
        }

        [TestMethod]
        public void TheTrainingOfficerWalksHisLineStoppingTwiceOnTheWayNorthAndNotOnTheWayBack()
        {
            using var harness = BootcampRuntimeTestHarness.Create(useWorldContent: true, worldMigration: BootcampAmbientGuardTests.Before);
            harness.Client.Player.GmFlagAlwaysFriendly = true;
            SpawnPoolManager.Instance.SpawnPoolWorker(harness.BootcampMap, 0);

            var officer = harness.BootcampMap.MapCellInfo.Cells.Values
                .SelectMany(cell => cell.CreatureList).Distinct()
                .Single(creature => creature.SpawnPool?.DbId == BootcampTrainingOfficer.PoolId);

            var south = new Vector3(383.832f, 119.58f, 148.59f);
            var firstStop = new Vector3(380.4414f, 119.58f, 148.59f);
            var secondStop = new Vector3(376.8125f, 119.58f, 148.59f);
            var north = new Vector3(375.7891f, 119.58f, 148.59f);

            // Made on the south end of his beat, on the floor and not on the navmesh, facing up it.
            Assert.AreEqual(south, officer.Position);
            Assert.AreEqual(West, (float)officer.Rotation, 0.0001f);
            Assert.AreEqual(TargetCategory.Friendly, officer.TargetCategory);
            Assert.AreEqual(1f, officer.WalkSpeed);
            Assert.AreEqual(10603u, officer.NameId);
            Assert.AreEqual(26677u, officer.AppearanceData[EquipmentData.Helmet].Class);
            Assert.HasCount(4, officer.Patrol);
            Assert.AreEqual(NpcPose.None, officer.Pose);

            harness.MovePlayerTo(officer);
            harness.Drain();
            CellManager.Instance.UpdateVisibility(harness.Client);
            harness.Drain();

            // Two circuits and a bit, a think at a time: where he is, which way he faces, what is sent.
            var seen = new List<(int Tick, Vector3 Position, float Yaw)>();
            var sent = new List<(int Tick, Movement Movement)>();

            for (var tick = 0; tick < 260; tick++)
            {
                BehaviorManager.Instance.MapChannelThink(harness.BootcampMap, Tick);
                seen.Add((tick, officer.Position, officer.LastYaw));

                foreach (var move in WorldTestContext.Drain(harness.Client).Select(packet => packet.Message).OfType<MoveObjectMessage>())
                    if (move.EntityId == officer.EntityId)
                        sent.Add((tick, move.Movement));
            }

            Assert.AreEqual(BehaviorManager.BehaviorActionPatrol, officer.Controller.CurrentAction);

            // Always on his line: the floor's height, never the navmesh's, which is over it.
            foreach (var (_, position, _) in seen)
            {
                Assert.AreEqual(119.58f, position.Y, 0.0001f);
                Assert.AreEqual(148.59f, position.Z, 0.0001f);
                Assert.IsTrue(position.X >= north.X - 0.0001f && position.X <= south.X + 0.0001f, position.ToString());
            }

            Assert.IsGreaterThan(0.05f, harness.BootcampMap.NavMesh.Nearest(firstStop).Value.Y - firstStop.Y, "The mesh there is not the floor.");

            // Where he stands still, and for how long.
            var halts = new List<(Vector3 Position, int Thinks, float Yaw)>();

            for (var index = 0; index < seen.Count;)
            {
                var end = index;

                while (end + 1 < seen.Count && seen[end + 1].Position == seen[index].Position)
                    end++;

                if (end > index)
                    halts.Add((seen[index].Position, end - index + 1, seen[end].Yaw));

                index = end + 1;
            }

            // Leaving out where the watch began and ended, his halts come round in this order.
            var order = halts.Skip(1).Take(8).Select(halt => halt.Position).ToList();

            CollectionAssert.AreEqual(new[] { firstStop, secondStop, north, south, firstStop, secondStop, north, south }, order);

            foreach (var halt in halts.Skip(1).Take(8))
            {
                if (halt.Position == firstStop || halt.Position == secondStop)
                {
                    // The stop, the quarter turn, four seconds, the quarter turn back.
                    Assert.IsGreaterThanOrEqualTo(18, halt.Thinks, halt.Position.ToString());
                    Assert.IsLessThanOrEqualTo(21, halt.Thinks, halt.Position.ToString());
                }
                else
                {
                    // An about-turn: no pause.
                    Assert.IsLessThanOrEqualTo(5, halt.Thinks, halt.Position.ToString());
                }
            }

            // At a stop he is turned to yaw 0 - a quarter turn from the way he walks - and that is sent.
            foreach (var stop in new[] { firstStop, secondStop })
            {
                var there = sent.Where(move => Near(stop, move.Movement.Position)).ToList();
                var turned = there.Where(move => move.Movement.Velocity == 0f && move.Movement.ViewDirection.X == 0f).ToList();

                Assert.IsGreaterThanOrEqualTo(2, turned.Count, "Once each time north.");
                Assert.AreEqual(MathF.PI / 2f, MathF.Abs(Patrols.Turn(West, turned[0].Movement.ViewDirection.X)), 0.0001f);

                // Sixteen thinks and more between the turn and the turn back.
                var back = there.First(move => move.Tick > turned[0].Tick);

                Assert.AreEqual(0f, back.Movement.Velocity);
                Assert.AreEqual(West, back.Movement.ViewDirection.X, 0.0001f);
                Assert.IsGreaterThanOrEqualTo(16, back.Tick - turned[0].Tick);
            }

            // He walks at a metre a second, north facing north and south facing south, and the
            // way south goes through both stops without one.
            var walking = sent.Where(move => move.Movement.Velocity > 0).ToList();

            Assert.IsTrue(walking.All(move => move.Movement.Velocity == 1f));
            Assert.IsTrue(walking.All(move => MathF.Abs(MathF.Abs(move.Movement.ViewDirection.X) - MathF.PI / 2f) < 0.0001f));

            var southbound = walking.Where(move => move.Movement.ViewDirection.X < 0).ToList();

            Assert.IsGreaterThanOrEqualTo(60, southbound.Count);
            Assert.IsFalse(sent.Any(move => move.Movement.Velocity == 0f && move.Movement.ViewDirection.X < 0
                                            && !Near(south, move.Movement.Position) && !Near(north, move.Movement.Position)),
                "Nothing stops him between the ends on the way south.");
        }

        [TestMethod]
        public void TheOfficersLineIsOpenWalkableGroundAndNobodyElseStandsOnIt()
        {
            using var harness = BootcampRuntimeTestHarness.Create(useWorldContent: true);
            SpawnPoolManager.Instance.SpawnPoolWorker(harness.BootcampMap, 0);

            var mesh = harness.BootcampMap.NavMesh;

            for (var x = 375.7891f; x <= 383.832f; x += 0.25f)
            {
                var point = new Vector3(x, 119.58f, 148.59f);
                var ground = mesh.Nearest(point);

                Assert.IsTrue(ground.HasValue, point.ToString());
                Assert.IsLessThan(0.3f, Vector2.Distance(new Vector2(ground.Value.X, ground.Value.Z), new Vector2(point.X, point.Z)), point.ToString());
                Assert.IsLessThan(0.5f, Math.Abs(ground.Value.Y - point.Y), point.ToString());
            }

            // Creatures within a metre of each other are pushed apart: nobody is that near his line.
            foreach (var other in harness.BootcampMap.MapCellInfo.Cells.Values.SelectMany(cell => cell.CreatureList).Distinct())
            {
                if (other.SpawnPool?.DbId == BootcampTrainingOfficer.PoolId)
                    continue;

                var beside = Math.Clamp(other.Position.X, 375.7891f, 383.832f);
                var away = Vector2.Distance(new Vector2(other.Position.X, other.Position.Z), new Vector2(beside, 148.59f));

                Assert.IsGreaterThan(2f, away, $"creature {other.DbId} at {other.Position}");
            }
        }

        #endregion

        #region A group on the beat

        [TestMethod]
        public void ItsGuardsWalkTheirPlacesBehindTheLeaderAndStandWhenItStands()
        {
            using var world = new WorldTestContext();
            var watcher = Watch(world, x: 40);
            var (leader, guards) = Group(world, new[]
            {
                new PatrolStep(new Vector3(10, 0, 0)),
                new PatrolStep(new Vector3(30, 0, 0), pauseMs: 60000)
            });

            Record(world, watcher, leader, 30);

            Assert.AreEqual(BehaviorManager.BehaviorActionPatrol, leader.Controller.CurrentAction);
            Assert.IsGreaterThan(20f, leader.Position.X);

            // Walking east: the two of the first rank 1.5 m behind it, 3 m to each side.
            AtPlace(leader, guards[0], new Vector3(-1.5f, 0, -3));
            AtPlace(leader, guards[1], new Vector3(-1.5f, 0, 3));
            Assert.AreEqual(leader.Position, leader.HomePos.Position, "Its home goes along the beat with it.");
            Assert.IsLessThan(0.51f, Vector3.Distance(leader.Position, guards[0].HomePos.Position), "A guard's home is its leader, as of its last think.");

            Record(world, watcher, leader, 30);

            Assert.AreEqual(new Vector3(30, 0, 0), leader.Position);
            Assert.IsTrue(leader.Controller.ActionPatrol.Arrived);

            foreach (var guard in guards)
            {
                Assert.AreEqual(BehaviorManager.BehaviorActionFollow, guard.Controller.CurrentAction);
                Assert.AreEqual(0f, guard.Controller.LastMovement.Velocity, "It stands when its leader does.");
            }

            AtPlace(leader, guards[0], new Vector3(-1.5f, 0, -3));
            AtPlace(leader, guards[1], new Vector3(-1.5f, 0, 3));
        }

        [TestMethod]
        public void ItsGuardsHoldTheirPlacesWhileTheLeaderTurnsAboutAndFallInBehindItWhenItWalksBack()
        {
            using var world = new WorldTestContext();
            var watcher = Watch(world, x: 40);
            var (leader, guards) = Group(world, new[]
            {
                new PatrolStep(new Vector3(10, 0, 0), pauseMs: 60000),
                new PatrolStep(new Vector3(20, 0, 0), pauseMs: 5000)
            });

            // Start it on its way to the far end.
            leader.Controller.ActionPatrol.Step = 1;

            Record(world, watcher, leader, 30);

            // On the far step, sitting out its pause: its guards where they were, behind it to the west.
            Assert.AreEqual(new Vector3(20, 0, 0), leader.Position);
            AtPlace(leader, guards[0], new Vector3(-1.5f, 0, -3));
            AtPlace(leader, guards[1], new Vector3(-1.5f, 0, 3));

            // Standing at their places, they face the way it does.
            foreach (var guard in guards)
                Assert.AreEqual(East, (float)guard.Rotation, 0.0001f);

            Record(world, watcher, leader, 25);

            // Turned about and walking back west: they are behind it again, on its east side,
            // each on the side of the beat it was on - not crossed over behind it.
            Assert.AreEqual(West, leader.LastYaw, 0.0001f);
            Assert.IsLessThan(20f, leader.Position.X);
            Assert.IsGreaterThan(10f, leader.Position.X);
            Assert.IsGreaterThan(0f, leader.Controller.LastMovement.Velocity);

            foreach (var guard in guards)
                Assert.IsGreaterThan(leader.Position.X, guard.Position.X);

            AtPlace(leader, guards[0], new Vector3(1.5f, 0, -3));
            AtPlace(leader, guards[1], new Vector3(1.5f, 0, 3));
        }

        [TestMethod]
        public void AHitOnAGuardTakesTheLeaderAndTheOtherGuardIntoTheFight()
        {
            using var world = new WorldTestContext();
            var watcher = Watch(world, x: 40);
            var (leader, guards) = Group(world, new[] { new PatrolStep(new Vector3(10, 0, 0)), new PatrolStep(new Vector3(30, 0, 0)) });

            Record(world, watcher, leader, 6);
            Assert.AreEqual(BehaviorManager.BehaviorActionFollow, guards[1].Controller.CurrentAction);

            Threat.FromDamage(guards[1], watcher.Player, 10);

            foreach (var member in new[] { leader, guards[0], guards[1] })
            {
                Assert.AreEqual(BehaviorManager.BehaviorActionFighting, member.Controller.CurrentAction);
                Assert.AreEqual(watcher.Player.EntityId, member.Controller.ActionFighting.TargetEntityId);
            }
        }

        [TestMethod]
        public void TheLeaderStartingAFightTakesItsGuardsIntoIt()
        {
            using var world = new WorldTestContext();
            var watcher = Watch(world, x: 40);
            var (leader, guards) = Group(world, new[] { new PatrolStep(new Vector3(10, 0, 0)), new PatrolStep(new Vector3(30, 0, 0)) });

            Record(world, watcher, leader, 6);

            Assert.IsTrue(BehaviorManager.Instance.TrySetActionFighting(leader, watcher.Player.EntityId));

            foreach (var guard in guards)
                Assert.AreEqual(BehaviorManager.BehaviorActionFighting, guard.Controller.CurrentAction);
        }

        [TestMethod]
        public void AGuardOutOfAFightItsLeaderIsStillInGoesBackIntoIt()
        {
            using var world = new WorldTestContext();
            var watcher = Watch(world, x: 40);
            var (leader, guards) = Group(world, new[] { new PatrolStep(new Vector3(10, 0, 0)), new PatrolStep(new Vector3(30, 0, 0)) });

            Record(world, watcher, leader, 6);
            Assert.IsTrue(BehaviorManager.Instance.TrySetActionFighting(leader, watcher.Player.EntityId));

            // Its own fight given up, the guard is back to following.
            BehaviorManager.Instance.GiveUp(guards[0]);
            Assert.AreEqual(BehaviorManager.BehaviorActionFollow, guards[0].Controller.CurrentAction);

            Record(world, watcher, leader, 1);

            Assert.AreEqual(BehaviorManager.BehaviorActionFighting, guards[0].Controller.CurrentAction);
            Assert.AreEqual(watcher.Player.EntityId, guards[0].Controller.ActionFighting.TargetEntityId);
        }

        [TestMethod]
        public void AfterTheFightTheLeaderRejoinsItsBeatAndItsGuardsFormUpBehindItAgain()
        {
            using var world = new WorldTestContext();
            var watcher = Watch(world, x: 40);
            var (leader, guards) = Group(world, new[]
            {
                new PatrolStep(new Vector3(10, 0, 0)),
                new PatrolStep(new Vector3(30, 0, 0), pauseMs: 60000)
            });

            Record(world, watcher, leader, 6);
            Assert.IsTrue(BehaviorManager.Instance.TrySetActionFighting(leader, watcher.Player.EntityId));

            // The fight carried them all off the line, and is over.
            leader.Position = new Vector3(15, 0, 6);
            guards[0].Position = new Vector3(12, 0, 12);
            guards[1].Position = new Vector3(18, 0, 10);
            BehaviorManager.Instance.GiveUp(leader);

            foreach (var guard in guards)
            {
                BehaviorManager.Instance.GiveUp(guard);
                Assert.AreEqual(BehaviorManager.BehaviorActionFollow, guard.Controller.CurrentAction, "Out of the fight, back to following.");
            }

            Record(world, watcher, leader, 2);

            Assert.AreEqual(BehaviorManager.BehaviorActionPatrol, leader.Controller.CurrentAction);
            Assert.AreEqual(1, leader.Controller.ActionPatrol.Step, "Its place on the beat is kept.");

            Record(world, watcher, leader, 60);

            Assert.AreEqual(new Vector3(30, 0, 0), leader.Position);
            Assert.IsTrue(leader.Controller.ActionPatrol.Arrived);

            foreach (var guard in guards)
                Assert.AreEqual(BehaviorManager.BehaviorActionFollow, guard.Controller.CurrentAction);

            // It came back onto the step from off the line, and stands facing the way it came:
            // its guards form up behind it as it faces.
            var facing = (float)leader.Rotation;

            Assert.AreNotEqual(East, facing, 0.1f);
            AtPlace(leader, guards[0], PatrolGroups.Offset(0, facing));
            AtPlace(leader, guards[1], PatrolGroups.Offset(1, facing));
        }

        [TestMethod]
        public void TheLeaderLeashedBackToItsBeatTakesItsGuardsOutOfTheFightWithIt()
        {
            using var world = new WorldTestContext();
            var watcher = Watch(world, x: 40);
            var (leader, guards) = Group(world, new[] { new PatrolStep(new Vector3(10, 0, 0)), new PatrolStep(new Vector3(30, 0, 0)) });

            Record(world, watcher, leader, 6);
            Assert.IsTrue(BehaviorManager.Instance.TrySetActionFighting(leader, watcher.Player.EntityId));

            BehaviorManager.Instance.Leash(world.Map, leader);

            Assert.IsTrue(BehaviorManager.IsReturning(leader));

            foreach (var guard in guards)
            {
                Assert.IsTrue(BehaviorManager.IsReturning(guard), "It gives the fight up with its leader.");
                Assert.IsFalse(BehaviorManager.Instance.TrySetActionFighting(guard, watcher.Player.EntityId), "Nothing pulls it back on the way.");
            }
        }

        [TestMethod]
        public void AGuardThatCannotGetToItsPlaceIsPutOnIt()
        {
            using var world = new WorldTestContext();
            var watcher = Watch(world, x: 40);
            var (leader, guards) = Group(world, new[] { new PatrolStep(new Vector3(10, 0, 0), pauseMs: 600000), new PatrolStep(new Vector3(30, 0, 0)) });

            // Cut off far from its leader, all but legless: it keeps up with its leader's walk at
            // least, and its leader barely walks.
            leader.WalkSpeed = 0.01f;
            guards[0].Position = new Vector3(10, 0, 60);
            guards[0].WalkSpeed = 0;
            guards[0].RunSpeed = 0;

            Record(world, watcher, leader, 4);
            Assert.IsGreaterThan(PatrolGroups.StrayDistance, Spacing.AcrossGround(guards[0].Position, leader.Position));

            // Twice a run of about 61 m at the 1 m/s it is taken to run at least: about 123 s.
            Record(world, watcher, leader, 520);

            AtPlace(leader, guards[0], new Vector3(-1.5f, 0, -3));
        }

        [TestMethod]
        public void GuardsOfADeadLeaderStrollWhereTheyAreAndFallInBehindTheNextOne()
        {
            using var world = new WorldTestContext();
            var watcher = Watch(world, x: 40);
            var beat = new[] { new PatrolStep(new Vector3(10, 0, 0)), new PatrolStep(new Vector3(30, 0, 0)) };
            var (leader, guards) = Group(world, beat);

            Record(world, watcher, leader, 4);

            leader.State = CharacterState.Dead;
            leader.Attributes[Attributes.Health].Current = 0;
            Record(world, watcher, leader, 1);

            foreach (var guard in guards)
            {
                Assert.AreEqual(BehaviorManager.BehaviorActionWander, guard.Controller.CurrentAction);
                Assert.AreEqual(0UL, guard.Controller.ActionFollow.FollowTargetId);
                Assert.AreEqual(guard.Position, guard.HomePos.Position, "It strolls where it stands.");
            }

            // The pool tops its leader back up.
            var next = Spawn(world, x: 10, walkSpeed: 2, facing: East);
            next.DbId = leader.DbId;
            next.SpawnPool = leader.SpawnPool;
            next.Patrol = beat;
            leader.SpawnPool.Leader = next;

            Record(world, watcher, next, 2);

            foreach (var guard in guards)
            {
                Assert.AreEqual(BehaviorManager.BehaviorActionFollow, guard.Controller.CurrentAction);
                Assert.AreEqual(next.EntityId, guard.Controller.ActionFollow.FollowTargetId);
            }

            CollectionAssert.AreEquivalent(new[] { 0, 1 }, guards.Select(guard => guard.Controller.ActionFollow.FormationSlot).ToArray());
        }

        [TestMethod]
        public void WithAnActingLeaderTheGuardOfTheFirstPlaceTakesTheBeatOnFromItsDeadLeader()
        {
            using var world = new WorldTestContext();
            var watcher = Watch(world, x: 40);
            var (leader, guards) = Group(world, new[] { new PatrolStep(new Vector3(10, 0, 0)), new PatrolStep(new Vector3(30, 0, 0)) });
            var pool = leader.SpawnPool;

            pool.ActingLeader = true;

            Record(world, watcher, leader, 8);
            Assert.AreEqual(1, leader.Controller.ActionPatrol.Step);
            pool.FormationMirrored = true;

            leader.State = CharacterState.Dead;
            leader.Attributes[Attributes.Health].Current = 0;
            Record(world, watcher, guards[0], 1);

            var first = guards.Single(guard => guard.Patrol != null);
            var other = guards.Single(guard => guard != first);

            Assert.AreSame(guards[0], first, "The guard of place 0 leads.");
            Assert.AreSame(first, pool.Leader);
            Assert.AreEqual(1, first.Controller.ActionPatrol.Step, "From the step its leader was walking to.");
            Assert.IsFalse(pool.FormationMirrored, "A new leader's formation starts afresh.");
            Assert.AreEqual(first.EntityId, other.Controller.ActionFollow.FollowTargetId);

            Record(world, watcher, first, 20);

            Assert.AreEqual(BehaviorManager.BehaviorActionPatrol, first.Controller.CurrentAction);
            Assert.IsGreaterThan(0f, first.Controller.LastMovement.Velocity);
            Assert.AreEqual(BehaviorManager.BehaviorActionFollow, other.Controller.CurrentAction);
            Assert.AreEqual(first.EntityId, other.Controller.ActionFollow.FollowTargetId);
        }

        [TestMethod]
        public void WithoutAnActingLeaderNoGuardTakesTheBeatOn()
        {
            using var world = new WorldTestContext();
            var watcher = Watch(world, x: 40);
            var (leader, guards) = Group(world, new[] { new PatrolStep(new Vector3(10, 0, 0)), new PatrolStep(new Vector3(30, 0, 0)) });

            Record(world, watcher, leader, 4);
            leader.State = CharacterState.Dead;
            leader.Attributes[Attributes.Health].Current = 0;
            Record(world, watcher, leader, 8);

            foreach (var guard in guards)
            {
                Assert.IsNull(guard.Patrol);
                Assert.AreEqual(BehaviorManager.BehaviorActionWander, guard.Controller.CurrentAction);
            }
        }

        [TestMethod]
        public void AGuardDropsAFightTooFarFromItsLeaderAsItIsAndIsNotDrawnBackIntoIt()
        {
            using var world = new WorldTestContext();
            var watcher = Watch(world, x: 40);
            var far = Watch(world, x: 80);
            var (leader, guards) = Group(world, new[] { new PatrolStep(new Vector3(10, 0, 0), pauseMs: 600000), new PatrolStep(new Vector3(30, 0, 0)) });

            Record(world, watcher, leader, 4);
            Assert.IsTrue(BehaviorManager.Instance.TrySetActionFighting(leader, far.Player.EntityId));
            Assert.AreEqual(BehaviorManager.BehaviorActionFighting, guards[0].Controller.CurrentAction);
            guards[0].Attributes[Attributes.Health].Current = 50;

            Record(world, watcher, leader, 1);

            // 70 m from its leader: dropped, as it is - not run home and made whole.
            Assert.AreEqual(BehaviorManager.BehaviorActionFollow, guards[0].Controller.CurrentAction);
            Assert.AreEqual(far.Player.EntityId, guards[0].Controller.ActionFollow.DroppedTargetId);
            Assert.AreEqual(50, guards[0].Attributes[Attributes.Health].Current);

            Record(world, watcher, leader, 4);

            Assert.AreEqual(BehaviorManager.BehaviorActionFighting, leader.Controller.CurrentAction, "Its leader fights on.");
            Assert.AreNotEqual(BehaviorManager.BehaviorActionFighting, guards[0].Controller.CurrentAction, "Not drawn back into it with that target.");
            Assert.IsFalse(BehaviorManager.IsReturning(guards[0]));
        }

        [TestMethod]
        public void TheGroupDoesNotTakeOneAMissionSceneMovesIntoItsFight()
        {
            using var world = new WorldTestContext();
            var watcher = Watch(world, x: 40);
            var (leader, guards) = Group(world, new[] { new PatrolStep(new Vector3(10, 0, 0)), new PatrolStep(new Vector3(30, 0, 0)) });

            Record(world, watcher, leader, 4);
            guards[1].Controller.CurrentAction = BehaviorManager.BehaviorActionScriptedMove;

            Assert.IsTrue(BehaviorManager.Instance.TrySetActionFighting(leader, watcher.Player.EntityId));

            Assert.AreEqual(BehaviorManager.BehaviorActionFighting, guards[0].Controller.CurrentAction);
            Assert.AreEqual(BehaviorManager.BehaviorActionScriptedMove, guards[1].Controller.CurrentAction);
        }

        [TestMethod]
        public void AGuardUnderMindControlFollowsThePlayerNotItsLeader()
        {
            using var world = new WorldTestContext();
            var watcher = Watch(world, x: 40);
            var (leader, guards) = Group(world, new[] { new PatrolStep(new Vector3(10, 0, 0)), new PatrolStep(new Vector3(30, 0, 0)) });

            Record(world, watcher, leader, 4);
            guards[0].ActiveEffects[1] = new GameEffect { TypeId = AbilityManager.MindControlTypeId, EffectId = 1, MindControlPump = AbilityManager.MindControlEnslavement };
            BehaviorManager.Instance.SetActionFollow(guards[0], watcher.Player.EntityId);

            Assert.IsFalse(PatrolGroups.Follows(guards[0]));

            Record(world, watcher, leader, 3);

            Assert.AreEqual(watcher.Player.EntityId, guards[0].Controller.ActionFollow.FollowTargetId);

            // Its leader's fight does not take it: it is the player's.
            Assert.IsTrue(BehaviorManager.Instance.TrySetActionFighting(leader, watcher.Player.EntityId));
            Assert.AreNotEqual(BehaviorManager.BehaviorActionFighting, guards[0].Controller.CurrentAction);
            Assert.AreEqual(BehaviorManager.BehaviorActionFighting, guards[1].Controller.CurrentAction);
        }

        [TestMethod]
        public void AGuardOutOfAFightWhenANewLeaderCameTakesAPlaceNoOtherHolds()
        {
            using var world = new WorldTestContext();
            var watcher = Watch(world, x: 40);
            var beat = new[] { new PatrolStep(new Vector3(10, 0, 0)), new PatrolStep(new Vector3(30, 0, 0)) };
            var (leader, guards) = Group(world, beat);

            Record(world, watcher, leader, 4);

            // One guard in a fight of its own as its leader dies.
            guards[0].Controller.CurrentAction = BehaviorManager.BehaviorActionFighting;
            leader.State = CharacterState.Dead;
            leader.Attributes[Attributes.Health].Current = 0;

            var next = Spawn(world, x: 10, walkSpeed: 2, facing: East);
            next.DbId = leader.DbId;
            next.SpawnPool = leader.SpawnPool;
            next.Patrol = beat;
            PatrolGroups.SetLeader(leader.SpawnPool, next);

            Record(world, watcher, next, 2);
            Assert.AreEqual(next.EntityId, guards[1].Controller.ActionFollow.FollowTargetId);

            // Its fight over, it comes back to following with its old place and its old leader.
            BehaviorManager.Instance.GiveUp(guards[0]);
            Record(world, watcher, next, 2);

            Assert.AreEqual(next.EntityId, guards[0].Controller.ActionFollow.FollowTargetId);
            CollectionAssert.AreEquivalent(new[] { 0, 1 }, guards.Select(guard => guard.Controller.ActionFollow.FormationSlot).ToArray());
        }

        [TestMethod]
        public void TheLeaderComingBackOntoItsBeatFromAFightDoesNotMirrorItsFormation()
        {
            using var world = new WorldTestContext();
            var watcher = Watch(world, x: 60);
            var (leader, guards) = Group(world, new[]
            {
                new PatrolStep(new Vector3(10, 0, 0)),
                new PatrolStep(new Vector3(30, 0, 0), pauseMs: 600000)
            });
            var pool = leader.SpawnPool;

            Record(world, watcher, leader, 10);
            Assert.IsFalse(pool.FormationMirrored);

            // A fight carried it east past the far step: it walks back onto the beat westwards.
            Assert.IsTrue(BehaviorManager.Instance.TrySetActionFighting(leader, watcher.Player.EntityId));
            leader.Position = new Vector3(45, 0, 0);
            BehaviorManager.Instance.GiveUp(leader);

            foreach (var guard in guards)
                BehaviorManager.Instance.GiveUp(guard);

            Record(world, watcher, leader, 50);

            Assert.AreEqual(new Vector3(30, 0, 0), leader.Position);
            Assert.IsFalse(pool.FormationMirrored, "The way back onto the beat is not the beat.");
        }

        [TestMethod]
        public void AHitOnOneOfTheGroupIsKnownToAllOfIt()
        {
            using var world = new WorldTestContext();
            var watcher = Watch(world, x: 40);
            var second = Watch(world, x: 45);
            var (leader, guards) = Group(world, new[] { new PatrolStep(new Vector3(10, 0, 0)), new PatrolStep(new Vector3(30, 0, 0)) });

            Record(world, watcher, leader, 4);

            Threat.FromDamage(guards[1], watcher.Player, 10);

            Assert.IsTrue(leader.Hate.Contains(watcher.Player.EntityId));
            Assert.IsTrue(guards[0].Hate.Contains(watcher.Player.EntityId));

            // A second attacker, on the leader while they all fight.
            Threat.FromDamage(leader, second.Player, 10);

            foreach (var guard in guards)
                Assert.IsTrue(guard.Hate.Contains(second.Player.EntityId), "They know of the second too.");
        }

        [TestMethod]
        public void TheLeaderHomeAfterALeashHasItsGuardsLeftFarBehindPutBackRoundIt()
        {
            using var world = new WorldTestContext();
            var watcher = Watch(world, x: 40);
            var (leader, guards) = Group(world, new[] { new PatrolStep(new Vector3(10, 0, 0), pauseMs: 600000), new PatrolStep(new Vector3(30, 0, 0)) });

            Record(world, watcher, leader, 4);

            // Left far behind, and in no state to come back on its own.
            guards[0].Position = new Vector3(10, 0, 70);
            guards[0].WalkSpeed = 0;
            guards[0].RunSpeed = 0;

            BehaviorManager.Instance.Leash(world.Map, leader);
            Record(world, watcher, leader, 1);

            Assert.IsFalse(BehaviorManager.IsReturning(leader));
            Assert.IsLessThan(PatrolGroups.StrayDistance, Spacing.AcrossGround(guards[0].Position, leader.Position));
        }

        [TestMethod]
        public void GuardsStandingAtTheirPlacesSendNoMovesAndWalkingAtTheirLeadersPaceAreNotShownRunning()
        {
            using var world = new WorldTestContext();
            var watcher = Watch(world, x: 60);
            var (leader, guards) = Group(world, new[]
            {
                new PatrolStep(new Vector3(10, 0, 0)),
                new PatrolStep(new Vector3(40, 0, 0), pauseMs: 600000)
            });

            // The leader walks faster than its guards do: they keep its pace, walking.
            leader.WalkSpeed = 3;

            Record(world, watcher, leader, 20);

            Assert.IsGreaterThan(0f, leader.Controller.LastMovement.Velocity);

            foreach (var guard in guards)
                Assert.IsFalse(guard.IsRunning, "In its place at its leader's walk, it is shown walking.");

            Record(world, watcher, leader, 30);
            Assert.AreEqual(new Vector3(40, 0, 0), leader.Position);

            foreach (var guard in guards)
                Assert.IsEmpty(Record(world, watcher, guard, 10), "Standing at its place, nothing is sent of it.");
        }

        /// <summary>A leader on <paramref name="beat"/> at x 10 facing east, walking 2 m/s, and two guards of its pool behind it, put in behind it as its pool does.</summary>
        private static (Creature Leader, Creature[] Guards) Group(WorldTestContext world, PatrolStep[] beat)
        {
            var pool = new SpawnPool
            {
                DbId = 77,
                MapContextId = world.Map.MapInfo.MapContextId,
                Patrol = beat,
                SpawnSlot = new List<SpawnPoolSlot> { new SpawnPoolSlot(1, 1, 1), new SpawnPoolSlot(2, 2, 2) }
            };

            var leader = Spawn(world, x: 10, walkSpeed: 2, facing: East);
            leader.DbId = 1;
            leader.SpawnPool = pool;
            leader.Patrol = beat;
            pool.Leader = leader;

            var guards = new[] { Spawn(world, x: 8, walkSpeed: 2, facing: East), Spawn(world, x: 6, walkSpeed: 2, facing: East) };

            foreach (var guard in guards)
            {
                guard.DbId = 2;
                guard.SpawnPool = pool;
                PatrolGroups.Attach(world.Map, guard, leader);
            }

            return (leader, guards);
        }

        /// <summary>The guard stands within a metre of its place: <paramref name="offset"/> from its leader.</summary>
        private static void AtPlace(Creature leader, Creature guard, Vector3 offset)
        {
            var place = leader.Position + offset;

            Assert.IsLessThan(1f, Spacing.AcrossGround(guard.Position, place), $"guard at {guard.Position}, place {place}");
        }

        #endregion

        #region Fixture

        /// <summary>The same place, to a millimetre: a step of a walk is summed in floats, a stop is put exactly on its step.</summary>
        private static bool Near(Vector3 expected, Vector3 actual) => Vector3.Distance(expected, actual) < 0.001f;

        /// <summary>Thinks the map a tick at a time and gives back every movement sent of the creature, with the tick it went out in.</summary>
        private static List<(int Tick, Movement Movement)> Record(WorldTestContext world, Client watcher, Creature creature, int ticks)
        {
            var moves = new List<(int, Movement)>();

            for (var tick = 0; tick < ticks; tick++)
            {
                BehaviorManager.Instance.MapChannelThink(world.Map, Tick);

                foreach (var move in WorldTestContext.Drain(watcher).Select(packet => packet.Message).OfType<MoveObjectMessage>())
                    if (move.EntityId == creature.EntityId)
                        moves.Add((tick, move.Movement));
            }

            return moves;
        }

        private static List<MoveObjectMessage> Moves(WorldTestContext world, Client watcher, int ticks)
        {
            for (var tick = 0; tick < ticks; tick++)
                BehaviorManager.Instance.MapChannelThink(world.Map, Tick);

            return WorldTestContext.Drain(watcher).Select(packet => packet.Message).OfType<MoveObjectMessage>().ToList();
        }

        /// <summary>A creature standing idle in the map's cells, its home where it stands, facing a way. Hostile, so it may fight a player; it scans for nobody.</summary>
        private static Creature Spawn(WorldTestContext world, float x, float walkSpeed, float facing)
        {
            var creature = new Creature
            {
                Name = "Sentry",
                TargetCategory = TargetCategory.Hostile,
                MapContextId = world.Map.MapInfo.MapContextId,
                RuntimeMapChannel = world.Map,
                Position = new Vector3(x, 0, 0),
                Rotation = facing,
                LastYaw = facing,
                EntityClass = EntityClasses.HumanBaseMale,
                State = CharacterState.Idle,
                Level = 1,
                AggroRange = 0,
                WalkSpeed = walkSpeed,
                RunSpeed = walkSpeed * 2,
                AppearanceData = new Dictionary<EquipmentData, AppearanceData>()
            };

            creature.HomePos.Position = creature.Position;
            creature.HomePos.MapContextid = creature.MapContextId;
            creature.Attributes[Attributes.Health] = new ActorAttributes(Attributes.Health, 100, 100, 100, 0, 0);
            BehaviorManager.StartWandering(creature, false);
            EntityManager.Instance.RegisterEntity(creature.EntityId, EntityType.Creature);
            EntityManager.Instance.RegisterCreature(creature);
            EntityManager.Instance.RegisterActor(creature.EntityId, creature);
            var seed = CellManager.Instance.GetCellSeed(creature.Position);
            creature.Cells = CellsAt(world, creature.Position);
            CellManager.Instance.GetCell(world.Map, seed & 0xFFFF, seed >> 16).CreatureList.Add(creature);
            return creature;
        }

        /// <summary>A client with health, standing in the map's cells, so what is sent about what is near it reaches it.</summary>
        private static Client Watch(WorldTestContext world, float x)
        {
            var client = world.CreateClient(x: x);
            var seed = CellManager.Instance.GetCellSeed(client.Player.Position);
            client.Player.Attributes[Attributes.Health] = new ActorAttributes(Attributes.Health, 1000, 1000, 1000, 0, 0);
            client.Player.Cells = CellsAt(world, client.Player.Position);
            CellManager.Instance.GetCell(world.Map, seed & 0xFFFF, seed >> 16).ClientList.Add(client);
            return client;
        }

        private static uint[,] CellsAt(WorldTestContext world, Vector3 position)
        {
            var seed = CellManager.Instance.GetCellSeed(position);
            return CellManager.Instance.CreateCellMatrix(world.Map, seed & 0xFFFF, seed >> 16);
        }

        #endregion
    }
}
