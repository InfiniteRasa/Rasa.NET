using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Rasa.Data;
    using Rasa.Game;
    using Rasa.Managers;
    using Rasa.Packets.MapChannel.Client;
    using Rasa.Packets.MapChannel.Server;
    using Rasa.Packets.Protocol;
    using Rasa.Repositories.UnitOfWork;
    using Rasa.Structures;
    using Rasa.Structures.World;

    /// <summary>
    /// The client's teleport effects for the abilities that send a player back (Tactical Retreat,
    /// Self Destruct; AbilityManager.Teleports) and for Bane come through a teleporter
    /// (BaneArrivals.ShowArrival): teleportertype TACTICAL_EVASION 7, SELF_DESTRUCT 5,
    /// BANE_TELEPORTER 4.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class AbilityTeleportTests
    {
        private const int SelfDestructBombTypeId = 10000032;
        private const int TacticalRetreatTypeId = 10000078;

        private static readonly Vector3 Home = new Vector3(0, 0, 20);

        [TestMethod]
        public void SelfDestructSendsItsHolderHomeAsTheSelfDestructTeleportForEveryone()
        {
            using var world = new WorldTestContext();
            var holder = Stand(world, new Vector3(0, 0, 0));
            var watcher = Stand(world, new Vector3(5, 0, 0));
            var bomb = new GameEffect
            {
                TypeId = SelfDestructBombTypeId,
                EffectId = GameEffectManager.Instance.NextEffectId(world.Map),
                EffectLevel = 1,
                SourceId = holder.Player.EntityId,
                Source = holder.Player,
                SourceLevel = 1,
                TickRadius = 10,
                ReturnTo = Home,
                ExpiresTick = Environment.TickCount64 + 30000
            };
            GameEffectManager.Instance.AddToList(holder.Player, bomb);
            Drain(holder);
            Drain(watcher);

            AbilityManager.OnPlayerDamaged(world.Map, holder.Player, 10);

            Assert.AreEqual(Home, holder.Player.Position);

            foreach (var client in new[] { holder, watcher })
            {
                var sent = Drain(client);
                Assert.AreEqual(TeleportType.SelfDestruct, Calls<PreTeleportPacket>(sent, holder).Single().TeleportType, "the type, for the departure effect");
                var teleport = Calls<TeleportPacket>(sent, holder).Single();
                Assert.AreEqual((TeleportType.SelfDestruct, Home, DynamicObjectManager.TeleportDelayMs), (teleport.TeleportType, teleport.Position, teleport.Delay));
                Assert.IsTrue(Moves(sent, holder).Any(), "the body moved, before the Teleport");
                Assert.IsLessThan(Index<TeleportPacket>(sent, holder), Index<PreTeleportPacket>(sent, holder));
            }
        }

        [TestMethod]
        public void TheRetreatVanishesFirstAndGoesWhenTheHoldIsUp()
        {
            using var world = new WorldTestContext();
            var ranger = Stand(world, new Vector3(0, 0, 0));
            var watcher = Stand(world, new Vector3(5, 0, 0));
            var abilities = Abilities();

            try
            {
                Retreat(world, ranger);

                // The vanishing effect, and nothing else yet.
                foreach (var client in new[] { ranger, watcher })
                {
                    var sent = Drain(client);
                    Assert.AreEqual(TeleportType.TacticalEvasion, Calls<PreTeleportPacket>(sent, ranger).Single().TeleportType);
                    Assert.IsEmpty(Calls<TeleportPacket>(sent, ranger));
                }
                Assert.AreEqual(Vector3.Zero, ranger.Player.Position);
                Assert.IsTrue(AbilityManager.IsReturning(ranger.Player));

                abilities.ReturnWorker(world.Map);
                Assert.AreEqual(Vector3.Zero, ranger.Player.Position, "not before the hold is up");

                HoldUp();
                abilities.ReturnWorker(world.Map);

                Assert.AreEqual(Home, ranger.Player.Position);
                Assert.IsFalse(AbilityManager.IsReturning(ranger.Player));
                foreach (var client in new[] { ranger, watcher })
                {
                    var sent = Drain(client);
                    var teleport = Calls<TeleportPacket>(sent, ranger).Single();
                    Assert.AreEqual((TeleportType.TacticalEvasion, Home), (teleport.TeleportType, teleport.Position));
                    Assert.IsTrue(Moves(sent, ranger).Any());
                }
            }
            finally
            {
                ClearReturns();
            }
        }

        [TestMethod]
        public void ARangerWhoDiesDuringTheHoldStaysAndTheEffectIsCalledOff()
        {
            using var world = new WorldTestContext();
            var ranger = Stand(world, new Vector3(0, 0, 0));
            var watcher = Stand(world, new Vector3(5, 0, 0));
            var abilities = Abilities();

            try
            {
                Retreat(world, ranger);
                Drain(ranger);
                Drain(watcher);

                ranger.Player.State = CharacterState.Dead;
                HoldUp();
                abilities.ReturnWorker(world.Map);

                Assert.AreEqual(Vector3.Zero, ranger.Player.Position);
                foreach (var client in new[] { ranger, watcher })
                {
                    var sent = Drain(client);
                    Assert.HasCount(1, Calls<TeleportFailedPacket>(sent, ranger));
                    Assert.IsEmpty(Calls<TeleportPacket>(sent, ranger));
                }
            }
            finally
            {
                ClearReturns();
            }
        }

        [TestMethod]
        public void ABaneThroughATeleporterArrivesInItsEffectAndOneOffADropshipDoesNot()
        {
            using var world = new WorldTestContext();
            var watcher = Stand(world, new Vector3(5, 0, 0));
            var creature = new Creature
            {
                Name = "Fixture",
                TargetCategory = TargetCategory.Hostile,
                MapContextId = world.Map.MapInfo.MapContextId,
                RuntimeMapChannel = world.Map,
                Position = new Vector3(2, 0, 2),
                Rotation = 1.5,
                EntityClass = EntityClasses.HumanBaseMale,
                State = CharacterState.Idle,
                Level = 1,
                AppearanceData = new Dictionary<EquipmentData, AppearanceData>()
            };
            var seed = CellManager.Instance.GetCellSeed(creature.Position);
            creature.Cells = CellManager.Instance.CreateCellMatrix(world.Map, seed & 0xFFFF, seed >> 16);
            Drain(watcher);

            BaneArrivals.ShowArrival(world.Map, creature, new SpawnPoolArrivalEntry { Kind = SpawnPoolArrivalEntry.KindDropship });
            Assert.IsEmpty(Calls<TeleportPacket>(Drain(watcher), creature.EntityId));

            BaneArrivals.ShowArrival(world.Map, creature, new SpawnPoolArrivalEntry { Kind = SpawnPoolArrivalEntry.KindTeleporter });
            var sent = Drain(watcher);
            Assert.AreEqual(TeleportType.BaneTeleporter, Calls<PreTeleportPacket>(sent, creature.EntityId).Single().TeleportType);
            var teleport = Calls<TeleportPacket>(sent, creature.EntityId).Single();
            Assert.AreEqual((TeleportType.BaneTeleporter, creature.Position, 1.5), (teleport.TeleportType, teleport.Position, teleport.Yaw));
        }

        /// <summary>The Tactical Retreat's time up: what GameEffectManager runs as its OnExpired.</summary>
        private static void Retreat(WorldTestContext world, Client ranger)
        {
            var retreat = new GameEffect { TypeId = TacticalRetreatTypeId, ReturnTo = Home };

            typeof(AbilityManager).GetMethod("Retreat", BindingFlags.Static | BindingFlags.NonPublic)!
                .Invoke(null, new object[] { world.Map, ranger.Player, retreat });
        }

        private static IList Returns() =>
            (IList)typeof(AbilityManager).GetField("PendingReturns", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null);

        /// <summary>Every waiting return's hold made up.</summary>
        private static void HoldUp()
        {
            foreach (var pending in Returns())
                pending.GetType().GetField("DepartAt")!.SetValue(pending, 0L);
        }

        private static void ClearReturns()
        {
            var returns = Returns();
            lock (returns)
                returns.Clear();
        }

        private static AbilityManager Abilities() =>
            (AbilityManager)typeof(AbilityManager)
                .GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null,
                    new[] { typeof(IGameUnitOfWorkFactory), typeof(MissionApplication) }, null)!
                .Invoke(new object[] { null, null });

        private static Client Stand(WorldTestContext world, Vector3 at)
        {
            var client = world.CreateClient(at.X, at.Z);
            client.Player.Position = at;
            client.Player.State = CharacterState.Normal;
            foreach (var attribute in new[] { Attributes.Health, Attributes.Armor, Attributes.Power, Attributes.Regen })
                client.Player.Attributes[attribute] = new ActorAttributes(attribute, 1000, 1000, 1000, 0, 0);
            CellManager.Instance.AddToWorld(client);
            return client;
        }

        private static List<object> Drain(Client client) =>
            WorldTestContext.Drain(client).Select(packet => packet.Message).Cast<object>().ToList();

        private static List<T> Calls<T>(List<object> sent, Client of) => Calls<T>(sent, of.Player.EntityId);

        private static List<T> Calls<T>(List<object> sent, ulong entityId) =>
            sent.OfType<CallMethodMessage>().Where(m => m.EntityId == entityId).Select(m => m.Packet).OfType<T>().ToList();

        private static int Index<T>(List<object> sent, Client of) =>
            sent.FindIndex(m => m is CallMethodMessage call && call.EntityId == of.Player.EntityId && call.Packet is T);

        private static List<MoveObjectMessage> Moves(List<object> sent, Client of) =>
            sent.OfType<MoveObjectMessage>().Where(m => m.EntityId == of.Player.EntityId).ToList();
    }
}
