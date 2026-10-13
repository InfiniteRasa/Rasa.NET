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
    using Rasa.Packets;
    using Rasa.Packets.MapChannel.Server;
    using Rasa.Packets.Protocol;
    using Rasa.Structures;

    /// <summary>
    /// A Maw of a spawn pool lies under the ground, comes up at whoever comes near - its
    /// CREATURE_BIRTH - and goes back down when the fight is over - CR_DESPAWN 424/1 - to lie at
    /// its spawn point again, whole (MawLurk).
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class MawLurkTests
    {
        private const EntityClasses Maw = (EntityClasses)6332;
        private const EntityClasses MawBoss = (EntityClasses)22980;

        private readonly List<Creature> _creatures = new List<Creature>();
        private long _now = 1_000_000;

        [TestInitialize]
        public void Initialize()
        {
            MawLurk.Clock = () => _now;
        }

        [TestCleanup]
        public void Cleanup()
        {
            MawLurk.Clock = () => Environment.TickCount64;

            foreach (var creature in _creatures)
            {
                MawLurk.Forget(creature);
                EntityManager.Instance.UnregisterCreature(creature.EntityId);
                EntityManager.Instance.UnregisterActor(creature.EntityId);
                EntityManager.Instance.UnregisterEntity(creature.EntityId);
            }

            _creatures.Clear();
        }

        [TestMethod]
        public void OnlyAPoolsMawLurks()
        {
            var pool = new SpawnPool { Mode = SpawnPoolManager.ModeAutomatic };

            Assert.IsTrue(MawLurk.Lurks(new Creature { EntityClass = Maw, SpawnPool = pool }));
            Assert.IsTrue(MawLurk.Lurks(new Creature { EntityClass = MawBoss, SpawnPool = pool }), "Seymour");
            Assert.IsFalse(MawLurk.Lurks(new Creature { EntityClass = Maw }), "a GM's");
            Assert.IsFalse(MawLurk.Lurks(new Creature { EntityClass = Maw, SpawnPool = new SpawnPool { Mode = 1 } }), "a scripted pool's");
            Assert.IsFalse(MawLurk.Lurks(new Creature { EntityClass = Maw, SpawnPool = pool, MasterEntityId = 7 }), "a minion");
            Assert.IsFalse(MawLurk.Lurks(new Creature { EntityClass = EntityClasses.HumanBaseMale, SpawnPool = pool }));
        }

        [TestMethod]
        public void ItSpawnsUnderTheGroundInNoCellAndOnNoClient()
        {
            using var world = new WorldTestContext();
            var watcher = Watch(world, new Vector3(60, 0, 0));
            var maw = Bury(world);

            Assert.IsTrue(MawLurk.IsBuried(maw));
            Assert.IsFalse(InAnyCell(world, maw));
            Assert.IsTrue(EntityManager.Instance.Creatures.ContainsKey(maw.EntityId), "registered, for its pool");
            Assert.AreEqual(0, WorldTestContext.Drain(watcher).Count);

            MawLurk.Worker(world.Map);

            Assert.IsTrue(MawLurk.IsBuried(maw), "nobody within its aggro range");
        }

        [TestMethod]
        public void SomeoneComingNearBringsItUpAtThem()
        {
            using var world = new WorldTestContext();
            var player = Watch(world, new Vector3(10, 0, 0));
            var maw = Bury(world);

            MawLurk.Worker(world.Map);

            Assert.IsFalse(MawLurk.IsBuried(maw));
            Assert.IsTrue(InAnyCell(world, maw));
            Assert.AreEqual(BehaviorManager.BehaviorActionFighting, maw.Controller.CurrentAction);
            Assert.AreEqual(player.Player.EntityId, maw.Controller.ActionFighting.TargetEntityId);

            var sent = Sent(player, maw);
            var windup = sent.OfType<PerformWindupPacket>().Single();
            var birth = sent.OfType<PerformRecoveryPacket>().Single();
            Assert.AreEqual((ActionId.CreatureBirth, 6332u), (windup.ActionId, windup.ActionArgId));
            Assert.AreEqual((ActionId.CreatureBirth, 6332u), (birth.ActionId, birth.ActionArgId));

            Assert.IsTrue(MawLurk.IsBusy(maw), "coming up");
            _now += MawLurk.EmergeMs;
            Assert.IsFalse(MawLurk.IsBusy(maw));
        }

        [TestMethod]
        public void WithNothingToFightItGoesDownAndLiesAtItsSpawnPointWhole()
        {
            using var world = new WorldTestContext();
            var player = Watch(world, new Vector3(10, 0, 0));
            var maw = Bury(world);
            MawLurk.Worker(world.Map);
            _now += MawLurk.EmergeMs;

            // Fought, hurt and walked off; the fight is over.
            maw.Attributes[Attributes.Health].Current = 1234;
            maw.Position = new Vector3(4, 0, 4);
            maw.Hate.Clear();
            BehaviorManager.StartWandering(maw, false);
            maw.LastAgression = MawLurk.LingerMs - 1;
            Assert.IsFalse(MawLurk.Sink(world.Map, maw), "not yet");

            maw.LastAgression = MawLurk.LingerMs;
            maw.Hate.Ensure(player.Player.EntityId, 1);
            Assert.IsFalse(MawLurk.Sink(world.Map, maw), "something to fight still");

            maw.Hate.Clear();
            Sent(player, maw);
            Assert.IsTrue(MawLurk.Sink(world.Map, maw));
            Assert.IsTrue(MawLurk.IsBusy(maw));

            var sent = Sent(player, maw);
            Assert.AreEqual((ActionId.CrDespawn, 1u), (sent.OfType<PerformWindupPacket>().Single().ActionId, sent.OfType<PerformWindupPacket>().Single().ActionArgId));
            var despawn = sent.OfType<AbilityRecoveryPacket>().Single();
            Assert.AreEqual((ActionId.CrDespawn, 1u), (despawn.ActionId, despawn.ActionArgId));

            _now += MawLurk.SinkMs - 1;
            MawLurk.Worker(world.Map);
            Assert.IsTrue(InAnyCell(world, maw), "still going down");

            // The player gone off; someone still in view, out of its reach, sees it go.
            Leave(world, player);
            var onlooker = Watch(world, new Vector3(40, 0, 0));
            _now += 1;
            MawLurk.Worker(world.Map);

            Assert.IsTrue(MawLurk.IsBuried(maw));
            Assert.IsFalse(InAnyCell(world, maw));
            Assert.IsFalse(MawLurk.IsBusy(maw));
            Assert.IsTrue(WorldTestContext.Drain(onlooker).Select(p => p.Message).OfType<CallMethodMessage>()
                .Any(m => m.Packet is DestroyPhysicalEntityPacket));
            Assert.AreEqual(10000, maw.Attributes[Attributes.Health].Current);
            Assert.AreEqual(maw.HomePos.Position, maw.Position);
        }

        [TestMethod]
        public void TheCreaturesThinkSendsItDown()
        {
            using var world = new WorldTestContext();
            var maw = Bury(world);
            var player = Watch(world, new Vector3(10, 0, 0));
            MawLurk.Worker(world.Map);
            _now += MawLurk.EmergeMs;

            // Its target gone: it gives the fight up, and stands about until LingerMs is up.
            Leave(world, player);
            maw.Hate.Clear();
            BehaviorManager.Instance.StopFighting(maw);
            maw.LastAgression = MawLurk.LingerMs;

            BehaviorManager.Instance.MapChannelThink(world.Map, 250);

            Assert.IsTrue(MawLurk.IsBusy(maw), "going down");
        }

        [TestMethod]
        public void OneKilledOnTheWayDownStaysWhereItFell()
        {
            using var world = new WorldTestContext();
            var player = Watch(world, new Vector3(10, 0, 0));
            var maw = Bury(world);
            MawLurk.Worker(world.Map);
            _now += MawLurk.EmergeMs;
            maw.Hate.Clear();
            BehaviorManager.StartWandering(maw, false);
            maw.LastAgression = MawLurk.LingerMs;
            Leave(world, player);
            Assert.IsTrue(MawLurk.Sink(world.Map, maw));

            maw.Attributes[Attributes.Health].Current = 0;
            maw.State = CharacterState.Dead;
            _now += MawLurk.SinkMs;
            MawLurk.Worker(world.Map);

            Assert.IsFalse(MawLurk.IsBuried(maw));
            Assert.IsTrue(InAnyCell(world, maw));
        }

        [TestMethod]
        public void AMapResetTakesTheBuriedToo()
        {
            using var world = new WorldTestContext();
            var maw = Bury(world);

            var going = MapReset.CreaturesToRemove(world.Map.MapInfo.MapContextId,
                world.Map.MapCellInfo.Cells.Values.SelectMany(cell => cell.CreatureList).Concat(MawLurk.BuriedOn(world.Map)));
            CollectionAssert.Contains(going, maw);

            MapReset.Remove(world.Map, maw);

            Assert.IsFalse(MawLurk.IsBuried(maw));
            Assert.IsFalse(EntityManager.Instance.Creatures.ContainsKey(maw.EntityId));
        }

        private Creature Bury(WorldTestContext world)
        {
            var creature = new Creature
            {
                Name = "Maw",
                TargetCategory = TargetCategory.Hostile,
                MapContextId = world.Map.MapInfo.MapContextId,
                Position = Vector3.Zero,
                EntityClass = Maw,
                State = CharacterState.Idle,
                Level = 34,
                RunSpeed = 9,
                WalkSpeed = 5,
                SpawnPool = new SpawnPool { Mode = SpawnPoolManager.ModeAutomatic, MapContextId = world.Map.MapInfo.MapContextId },
                AppearanceData = new Dictionary<EquipmentData, AppearanceData>()
            };
            creature.HomePos.Position = Vector3.Zero;
            creature.Attributes[Attributes.Health] = new ActorAttributes(Attributes.Health, 10000, 10000, 10000, 0, 0);
            creature.Attributes[Attributes.Armor] = new ActorAttributes(Attributes.Armor, 0, 0, 0, 0, 0);
            BehaviorManager.StartWandering(creature, false);
            EntityManager.Instance.RegisterActor(creature.EntityId, creature);
            world.AddClass(Maw);
            MawLurk.Bury(world.Map, creature);
            _creatures.Add(creature);
            return creature;
        }

        [TestMethod]
        public void SomeoneComingNearAsItGoesDownBringsItStraightBackUp()
        {
            using var world = new WorldTestContext();
            var player = Watch(world, new Vector3(10, 0, 0));
            var maw = Bury(world);
            MawLurk.Worker(world.Map);
            _now += MawLurk.EmergeMs;
            maw.Hate.Clear();
            BehaviorManager.StartWandering(maw, false);
            maw.LastAgression = MawLurk.LingerMs;
            Assert.IsTrue(MawLurk.Sink(world.Map, maw));

            _now += MawLurk.SinkMs;
            Sent(player, maw);
            MawLurk.Worker(world.Map);

            Assert.IsFalse(MawLurk.IsBuried(maw));
            Assert.IsTrue(InAnyCell(world, maw));
            Assert.AreEqual(player.Player.EntityId, maw.Controller.ActionFighting.TargetEntityId);
            Assert.AreEqual(ActionId.CreatureBirth, Sent(player, maw).OfType<PerformRecoveryPacket>().Single().ActionId);
        }

        private static void Leave(WorldTestContext world, Client client)
        {
            foreach (var cell in world.Map.MapCellInfo.Cells.Values)
                cell.ClientList.Remove(client);
        }

        private static bool InAnyCell(WorldTestContext world, Creature creature) =>
            world.Map.MapCellInfo.Cells.Values.Any(cell => cell.CreatureList.Contains(creature));

        private static Client Watch(WorldTestContext world, Vector3 position)
        {
            var client = world.CreateClient(x: position.X, z: position.Z);
            var seed = CellManager.Instance.GetCellSeed(client.Player.Position);
            client.Player.Cells = CellManager.Instance.CreateCellMatrix(world.Map, seed & 0xFFFF, seed >> 16);
            CellManager.Instance.GetCell(world.Map, seed & 0xFFFF, seed >> 16).ClientList.Add(client);
            client.Player.State = CharacterState.Normal;
            foreach (var attribute in new[] { Attributes.Health, Attributes.Armor, Attributes.Power, Attributes.Regen })
                client.Player.Attributes[attribute] = new ActorAttributes(attribute, 1000, 1000, 1000, 0, 0);
            WorldTestContext.Drain(client);
            return client;
        }

        private static List<PythonPacket> Sent(Client client, Creature about) =>
            WorldTestContext.Drain(client)
                .Select(packet => packet.Message)
                .OfType<CallMethodMessage>()
                .Where(message => message.EntityId == about.EntityId)
                .Select(message => message.Packet)
                .ToList();
    }
}
