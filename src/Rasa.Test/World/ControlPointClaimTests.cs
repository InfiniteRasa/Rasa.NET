using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Rasa.Data;
    using Rasa.Game;
    using Rasa.Managers;
    using Rasa.Packets;
    using Rasa.Packets.Communicator.Server;
    using Rasa.Packets.MapChannel.Client;
    using Rasa.Packets.MapChannel.Server;
    using Rasa.Structures;
    using Rasa.Structures.World;
    using Rasa.Test.Missions;

    // What a claim of a control point looks like to the claimant and to everyone else
    // (DynamicObjectManager, ControlPoints): the lock that puts the object's contested effect on,
    // the claimant's windup shown to the others, and how each ends - run to its end, interrupted,
    // or with the claimant dead.
    [TestClass]
    [DoNotParallelize]
    public class ControlPointClaimTests
    {
        private const uint MapId = 1220;    // WorldTestContext's map
        private const uint PointId = 7;
        private const uint BanePool = 9101;

        private System.Func<uint, bool> _known;

        [TestInitialize]
        public void Start()
        {
            _known = ControlPoints.Instance.KnownCreature;
            ControlPoints.Instance.KnownCreature = id => true;
        }

        [TestCleanup]
        public void Restore()
        {
            ControlPoints.Instance.Load(new List<ControlPointEntry>(), new List<ControlPointLinkEntry>(), null);
            ControlPoints.Instance.KnownCreature = _known;
        }

        [TestMethod]
        public void AClaimIsTheLockForEveryoneAndTheClaimantsWindupForTheOthers()
        {
            using var world = new WorldTestContext();
            var f = new Fixture(world);

            try
            {
                f.Request();

                var action = world.Map.PerformRecovery.Single();
                Assert.AreSame(f.Claimant.Player, action.Actor);
                Assert.AreEqual(f.Object.EntityId, action.SourceId);
                Assert.AreEqual(ControlPoints.CaptureMs, action.WaitTime);
                Assert.AreSame(f.Claimant.Player, f.Object.UsedBy);

                var own = Drain(f.Claimant);
                Assert.AreEqual(f.Claimant.Player.EntityId, own.OfType<LockToActorPacket>().Single().ActorId);
                Assert.AreEqual(1, own.OfType<UseInterruptiblePacket>().Count(), "the contested effect, aimed at them");
                Assert.AreEqual(PerformType.TwoArgs, own.OfType<PerformWindupPacket>().Single().PerformType, "their client plays its own");
                Assert.AreEqual(0, own.OfType<UsePacket>().Count(), "no transition from the state to itself");
                Assert.AreEqual(PlayerMessage.PmControlpointClaiming, own.OfType<DisplayClientMessagePacket>().Single().MsgId);

                var seen = Drain(f.Onlooker);
                Assert.AreEqual(f.Claimant.Player.EntityId, seen.OfType<LockToActorPacket>().Single().ActorId);
                Assert.AreEqual(1, seen.OfType<UseInterruptiblePacket>().Count());

                var windup = seen.OfType<PerformWindupPacket>().Single();
                Assert.AreEqual(PerformType.ThreeArgs, windup.PerformType);
                Assert.AreEqual(ActionId.UseObject, windup.ActionId);
                Assert.AreEqual(DynamicObjectManager.ControlPointUseArgId, windup.ActionArgId);
                Assert.AreEqual(f.Object.EntityId, windup.Arg, "the object the windup is aimed at");
                Assert.AreEqual(0, seen.OfType<UsePacket>().Count());
            }
            finally
            {
                f.Remove();
            }
        }

        [TestMethod]
        public void AClaimRunToItsEndIsRecoveredLetGoAndChangedOnce()
        {
            using var world = new WorldTestContext();
            var f = new Fixture(world);

            try
            {
                f.Request();
                f.Finish(interrupted: false);

                Assert.IsTrue(f.Point.HeldByAfs);
                Assert.IsNull(f.Object.UsedBy);

                foreach (var client in new[] { f.Claimant, f.Onlooker })
                {
                    var packets = Drain(client);

                    Assert.AreEqual(1, packets.OfType<PerformRecoveryPacket>().Count());
                    Assert.AreEqual(0UL, packets.OfType<LockToActorPacket>().Last().ActorId, "let go: the contested effect off");
                    Assert.IsFalse(packets.OfType<SetUsablePacket>().Single().IsEnabled);

                    var state = packets.OfType<ForceStatePacket>().Single();
                    Assert.AreEqual(UseObjectState.CpointStateFactionAOwned, state.State);
                    Assert.AreEqual((int)ControlPoints.CaptureMs, state.WindupTimeMs, "the time a capture takes is kept");

                    Assert.AreEqual(0, packets.OfType<UsableInfoPacket>().Count(), "the state is not started over");
                    Assert.AreEqual(0, packets.OfType<ActionInterruptPacket>().Count());
                    Assert.IsTrue(packets.IndexOf(packets.OfType<LockToActorPacket>().Last()) < packets.IndexOf(state), "let go before it changes");
                }
            }
            finally
            {
                f.Remove();
            }
        }

        [TestMethod]
        public void AnInterruptedClaimIsToldAsInterruptedAndNotAsDone()
        {
            using var world = new WorldTestContext();
            var f = new Fixture(world);

            try
            {
                f.Request();
                f.Finish(interrupted: true);

                Assert.IsFalse(f.Point.HeldByAfs);
                Assert.IsNull(f.Object.UsedBy);
                Assert.AreEqual(0, f.Object.TriggeredByPlayers.Count);

                var seen = Drain(f.Onlooker);
                var interrupt = seen.OfType<ActionInterruptPacket>().Single();
                Assert.AreEqual(f.Claimant.Player.EntityId, interrupt.SourceId);
                Assert.AreEqual(ActionId.UseObject, interrupt.ActionId);
                Assert.AreEqual(DynamicObjectManager.ControlPointUseArgId, interrupt.ActionArgId);
                Assert.AreEqual(0, seen.OfType<PerformRecoveryPacket>().Count(), "no recovery for a use that did not happen");
                Assert.AreEqual(f.Claimant.Player.EntityId, seen.OfType<UseInterruptedPacket>().Single().ActorId);
                Assert.AreEqual(0UL, seen.OfType<LockToActorPacket>().Single().ActorId);
                Assert.AreEqual(0, seen.OfType<ForceStatePacket>().Count());

                var own = Drain(f.Claimant);
                var closed = own.OfType<UserActionFailedPacket>().Single();
                Assert.AreEqual(ActionId.UseObject, closed.ActionId);
                Assert.IsNull(closed.MsgId, "their client stopped it itself: the request is only closed");
                Assert.AreEqual(0, own.OfType<PerformRecoveryPacket>().Count());
                Assert.AreEqual(0, own.OfType<ActionInterruptPacket>().Count());
            }
            finally
            {
                f.Remove();
            }
        }

        [TestMethod]
        public void AClaimantWhoDiesLetsThePointGo()
        {
            using var world = new WorldTestContext();
            var f = new Fixture(world);

            try
            {
                f.Request();
                Drain(f.Onlooker);

                Assert.IsTrue(PlayerDeath.AtZero(world.Map, f.Claimant.Player, null));

                Assert.AreEqual(0, world.Map.PerformRecovery.Count);
                Assert.IsNull(f.Object.UsedBy, "not left showing a dead player as its user");

                var seen = Drain(f.Onlooker);
                Assert.AreEqual(f.Claimant.Player.EntityId, seen.OfType<UseInterruptedPacket>().Single().ActorId);
                Assert.AreEqual(0UL, seen.OfType<LockToActorPacket>().Single().ActorId);
                Assert.IsFalse(f.Point.HeldByAfs);
            }
            finally
            {
                f.Remove();
            }
        }

        [TestMethod]
        public void AnInterruptedUseOfASceneObjectOfTheKindIsRecoveredAsItWas()
        {
            using var world = new WorldTestContext();
            var claimant = PlayerDeathTests.Player(world, 10, 10);
            var prop = new DynamicObject
            {
                EntityClassId = (EntityClasses)3147,
                DynamicObjectType = DynamicObjectType.ControlPoint,
                MapContextId = MapId,
                RuntimeMapChannel = world.Map,
                Position = claimant.Player.Position,
                StateId = UseObjectState.CpointStateFactionBOwned
            };

            world.Map.ControlPoints[900] = prop;
            CellManager.Instance.AddToWorld(world.Map, prop);
            prop.IsInWorld = true;

            try
            {
                DynamicObjectManager.Instance.RequestUseObjectPacket(claimant, new RequestUseObjectPacket
                {
                    ActionId = ActionId.UseObject,
                    ActionArgId = DynamicObjectManager.ControlPointUseArgId,
                    EntityId = prop.EntityId
                });

                Assert.AreEqual(1, Drain(claimant).OfType<UsePacket>().Count(), "a scene's object is used as it was");

                var action = world.Map.PerformRecovery.Single();
                world.Map.PerformRecovery.Clear();
                action.IsInrerrupted = true;

                Assert.IsFalse(DynamicObjectManager.Instance.IsInterruptedClaim(action));

                ActorActionManager.Instance.PerformRecovery(world.Map, action);

                var packets = Drain(claimant);
                Assert.AreEqual(1, packets.OfType<PerformRecoveryPacket>().Count());
                Assert.AreEqual(0, packets.OfType<UserActionFailedPacket>().Count());
                Assert.AreEqual(UseObjectState.CpointStateFactionBOwned, prop.StateId);
            }
            finally
            {
                CellManager.Instance.RemoveFromWorld(world.Map, prop);
            }
        }

        #region Fixture

        /// <summary>
        /// A Bane point with its garrison down and its object in the world, a claimant standing
        /// at it and an onlooker a few metres off.
        /// </summary>
        private sealed class Fixture
        {
            private readonly WorldTestContext _world;

            public ControlPoints.Point Point { get; }
            public DynamicObject Object => Point.Object;
            public Client Claimant { get; }
            public Client Onlooker { get; }

            public Fixture(WorldTestContext world)
            {
                _world = world;

                world.Map.SpawnPools.Add(new SpawnPool
                {
                    DbId = BanePool,
                    Mode = SpawnPoolManager.ModeControlPoint,
                    AnimType = 0,
                    MapContextId = MapId,
                    RuntimeMapChannel = world.Map,
                    Position = new Vector3(12, 0, 12),
                    RespawnTime = 90_000,
                    SpawnSlot = new List<SpawnPoolSlot> { new SpawnPoolSlot(9402, 1, 3) }
                });

                ControlPoints.Instance.Load(
                    new[]
                    {
                        new ControlPointEntry
                        {
                            Id = PointId, MapContextId = MapId, Name = "Retread Outpost", ClassId = 3814,
                            PosX = 10, PosY = 0, PosZ = 10, DefaultOwner = ControlPointEntry.OwnerBane
                        }
                    },
                    new[] { new ControlPointLinkEntry { ControlPointId = PointId, Kind = ControlPointLinkEntry.KindBanePool, ObjectId = BanePool } },
                    null);
                ControlPoints.Instance.Place(world.Map);

                Point = ControlPoints.Instance.ById(PointId);

                CellManager.Instance.AddToWorld(world.Map, Object);
                Object.IsInWorld = true;

                // Its garrison has stood and been killed: the point is in service.
                var pool = world.Map.SpawnPools.Single();
                pool.HasSpawned = true;
                pool.AliveCreatures = 0;
                ControlPoints.Instance.Worker(world.Map);
                Assert.IsTrue(Object.IsEnabled);

                Claimant = PlayerDeathTests.Player(world, 10, 10);
                Onlooker = PlayerDeathTests.Player(world, 14, 10);
            }

            public void Request()
            {
                DynamicObjectManager.Instance.RequestUseObjectPacket(Claimant, new RequestUseObjectPacket
                {
                    ActionId = ActionId.UseObject,
                    ActionArgId = DynamicObjectManager.ControlPointUseArgId,
                    EntityId = Object.EntityId
                });
            }

            /// <summary>The claim's time is up, as the map's worker has it: off the list, then recovered.</summary>
            public void Finish(bool interrupted)
            {
                var action = _world.Map.PerformRecovery.Single();

                Drain(Claimant);
                Drain(Onlooker);

                _world.Map.PerformRecovery.Clear();
                action.IsInrerrupted = interrupted;
                ActorActionManager.Instance.PerformRecovery(_world.Map, action);
            }

            public void Remove()
            {
                _world.Map.PerformRecovery.Clear();
                CellManager.Instance.RemoveFromWorld(_world.Map, Object);
            }
        }

        private static List<PythonPacket> Drain(Client client) => MissionTestContext.Drain(client).ToList();

        #endregion
    }
}
