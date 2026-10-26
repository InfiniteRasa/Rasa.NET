using System.Linq;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Missions
{
    using Rasa.Data;
    using Rasa.Managers;
    using Rasa.Missions.Runtime;
    using Rasa.Packets.Game.Server;
    using Rasa.Packets.MapChannel.Server;
    using Rasa.Structures;

    /// <summary>
    /// A usable lock's mission requirement, resolved by the server for each player (MissionLocks):
    /// the client's own check reads its mission log, which carries no turned-in missions after a
    /// relog, so a lock whose mission the player has completed is sent to them as open.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class MissionLockTests
    {
        private const uint Mission = 429;

        private static readonly EntityClasses DoorClass = (EntityClasses)29877;     // a footlocker class, usable
        private bool _addedClass;
        private DynamicObject _door;

        [TestCleanup]
        public void Cleanup()
        {
            if (_door != null)
                EntityManager.Instance.UnregisterDynamicObject(_door.EntityId);

            if (_addedClass)
                EntityClassManager.Instance.LoadedEntityClasses.Remove(DoorClass);
        }

        [TestMethod]
        public void ALockIsOpenOnlyForAPlayerWhoHasCompletedItsMission()
        {
            var usableLock = new UsableLock { LockStateId = UseObjectState.TdStateClosed, MissionId = Mission };
            var player = new Manifestation();

            Assert.IsFalse(MissionLocks.OpensFor(usableLock, player), "not on the mission");

            player.Missions[Mission] = new MissionLog(Mission, MissionState.Active, true);
            Assert.IsFalse(MissionLocks.OpensFor(usableLock, player), "on it, not handed in");

            player.Missions[Mission] = new MissionLog(Mission, MissionState.Success, false);
            Assert.IsFalse(MissionLocks.OpensFor(usableLock, player), "done, not handed in");

            player.Missions[Mission] = new MissionLog(Mission, MissionState.Completed, false);
            Assert.IsTrue(MissionLocks.OpensFor(usableLock, player), "handed in");

            Assert.IsFalse(MissionLocks.OpensFor(new UsableLock { LockStateId = UseObjectState.TdStateClosed, KeyItemTemplateId = 28 }, player),
                "a lock with no mission is not the server's to open");
            Assert.IsFalse(MissionLocks.OpensFor(new UsableLock { Unlocked = true, MissionId = Mission }, player),
                "and one open for everyone needs nothing more");
        }

        [TestMethod]
        public void AnObjectIsIntroducedOpenToAPlayerWhoHasCompletedItsMissionAndLockedToOthers()
        {
            using var context = MissionTestContext.WithCompletableMission(Mission);
            var door = Door(context);

            DynamicObjectManager.Instance.CreateDynamicObjectOnClient(context.Client, door);
            var locked = Introduced(context, door);
            Assert.IsFalse(locked.Unlocked, "on the mission, not handed in");
            Assert.AreEqual(Mission, locked.MissionId);
            Assert.DoesNotContain(door.EntityId, context.Client.MissionLocks);

            context.Client.Player.Missions[Mission] = new MissionLog(Mission, MissionState.Completed, false);

            DynamicObjectManager.Instance.CreateDynamicObjectOnClient(context.Client, door);
            Assert.IsTrue(Introduced(context, door).Unlocked, "met again after handing it in: open for them");
            Assert.Contains(door.EntityId, context.Client.MissionLocks);
            Assert.IsFalse(door.Lock.Unlocked, "and only for them");
        }

        /// <summary>The way a relog's knowledge was lost: handed in while the object is in view, it opens there and then.</summary>
        [TestMethod]
        public void HandingTheMissionInOpensTheLockInViewAndNothingIsResentAfter()
        {
            using var context = MissionTestContext.WithCompletableMission(Mission);
            var door = Door(context);

            DynamicObjectManager.Instance.CreateDynamicObjectOnClient(context.Client, door);
            Assert.IsFalse(Introduced(context, door).Unlocked);

            Assert.IsTrue(context.Manager.CompleteOfferedMission(context.Client, context.Receiver.EntityId, Mission, 0, null));

            var opened = context.Drain().OfType<LockInfoPacket>().Single();
            Assert.IsTrue(opened.Unlocked, "the turn-in opens it for them");
            Assert.Contains(door.EntityId, context.Client.MissionLocks);

            MissionLocks.Refresh(context.Client);
            Assert.IsEmpty(context.Drain().OfType<LockInfoPacket>().ToList(), "told once");

            // A mission given again (a repeatable) is no longer handed in: shut again.
            context.Client.Player.Missions[Mission] = new MissionLog(Mission, MissionState.Active, false);
            MissionLocks.Refresh(context.Client);
            Assert.IsFalse(context.Drain().OfType<LockInfoPacket>().Single().Unlocked);
            Assert.DoesNotContain(door.EntityId, context.Client.MissionLocks);
        }

        private DynamicObject Door(MissionTestContext context)
        {
            var door = new DynamicObject
            {
                EntityClassId = DoorClass,
                MapContextId = context.Map.MapInfo.MapContextId,
                RuntimeMapChannel = context.Map,
                Position = context.Client.Player.Position + new Vector3(2, 0, 0),
                StateId = UseObjectState.TdStateClosed,
                Lock = new UsableLock { LockStateId = UseObjectState.TdStateClosed, MissionId = Mission }
            };

            // Known to the server as an object of a usable class, as one the map places is.
            var classes = EntityClassManager.Instance.LoadedEntityClasses;
            if (!classes.ContainsKey(DoorClass))
            {
                classes.Add(DoorClass, new EntityClass((uint)DoorClass, "fixture", 0, 0, new System.Collections.Generic.List<AugmentationType>(), true));
                _addedClass = true;
            }
            EntityManager.Instance.RegisterEntity(door.EntityId, EntityType.Object);
            EntityManager.Instance.RegisterDynamicObject(door);
            _door = door;

            var seed = CellManager.Instance.GetCellSeed(door.Position);
            CellManager.Instance.GetCell(context.Map, seed & 0xFFFF, seed >> 16).DynamicObjectList.Add(door);
            context.Client.Player.Cells ??= CellManager.Instance.CreateCellMatrix(context.Map, seed & 0xFFFF, seed >> 16);
            context.Drain();

            return door;
        }

        private static LockInfoPacket Introduced(MissionTestContext context, DynamicObject door) =>
            context.Drain().OfType<CreatePhysicalEntityPacket>().Single(packet => packet.EntityId == door.EntityId)
                .EntityData.OfType<LockInfoPacket>().Single();
    }
}
