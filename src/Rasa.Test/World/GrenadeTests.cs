using System;
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
    using Rasa.Packets;
    using Rasa.Packets.MapChannel.Server;
    using Rasa.Packets.Protocol;
    using Rasa.Repositories.UnitOfWork;
    using Rasa.Structures;
    using Rasa.Structures.Char;
    using Rasa.Test.Missions;

    /// <summary>
    /// The grenades (CONSUMABLE_GRENADE, action 420, abilities.grenade) were refused as not
    /// implemented, item kept: a player could not throw one. Thrown now, one goes off on what it
    /// was thrown at and hurts everything hostile within EFFECT_RADIUS (15 m) of it - the full roll
    /// at any distance, no roll to hit, and never the thrower or anyone on their side. Each throw
    /// uses up one grenade and starts the 5 s reuse.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class GrenadeTests
    {
        private const int GrenadeAction = 420;
        private const int Roll = 200;
        private const int Full = 100_000;
        private const uint RedClanId = 900201;
        private const uint BlueClanId = 900202;

        private readonly List<Creature> _creatures = new List<Creature>();

        #region Who it hits

        [TestMethod]
        public void EverythingHostileInTheBlastTakesTheFullRollWhereverItStands()
        {
            using var world = new WorldTestContext();
            var thrower = Player(world, 0);
            var target = Spawn(world, new Vector3(20, 0, 0), TargetCategory.Hostile);
            var beside = Spawn(world, new Vector3(20.5f, 0, 0), TargetCategory.Hostile);
            var atTheEdge = Spawn(world, new Vector3(34.9f, 0, 0), TargetCategory.Hostile);
            var above = Spawn(world, new Vector3(20, 10, 10), TargetCategory.Hostile);   // 14.1 m away
            var wildlife = Spawn(world, new Vector3(20, 0, -8), TargetCategory.Neutral);
            var outside = Spawn(world, new Vector3(35.6f, 0, 0), TargetCategory.Hostile);
            var farAbove = Spawn(world, new Vector3(20, 12, 10), TargetCategory.Hostile); // 15.6 m away

            var recovery = Throw(world, thrower, target.EntityId);

            Assert.AreEqual(AbilityRecoveryPacket.HitDataKind.Splash, recovery.Kind);
            Assert.AreEqual(target.EntityId, recovery.SplashOn, "the blast is drawn on what it was thrown at");
            Assert.IsEmpty(recovery.Misses, "no roll to hit");
            CollectionAssert.AreEquivalent(new[] { target, beside, atTheEdge, above, wildlife }.Select(c => c.EntityId).ToList(),
                recovery.Hits.Select(h => h.EntityId).ToList());

            foreach (var hit in recovery.Hits)
            {
                var creature = _creatures.Single(c => c.EntityId == hit.EntityId);

                if (!hit.IsCritical)
                    Assert.AreEqual(Roll, hit.Amount, $"the full roll at {Vector3.Distance(target.Position, creature.Position)} m");

                Assert.AreEqual(Full - hit.Amount, (int)creature.Attributes[Attributes.Health].Current, "what it was shown is what it took");
                Assert.AreEqual(DamageType.Physical, hit.DamageType);
            }

            Assert.AreEqual(Full, (int)outside.Attributes[Attributes.Health].Current, "15.6 m out");
            Assert.AreEqual(Full, (int)farAbove.Attributes[Attributes.Health].Current, "15.6 m out, above");
        }

        [TestMethod]
        public void TheThrowerAndEveryoneOnTheirSideAreNeverCaught()
        {
            using var world = new WorldTestContext();
            var red = Player(world, 0, RedClanId);
            var redMate = Player(world, 7, RedClanId);
            var loner = Player(world, 6);
            var blue = Player(world, 8, BlueClanId);
            var target = Spawn(world, new Vector3(5, 0, 0), TargetCategory.Hostile);
            var soldier = Spawn(world, new Vector3(4, 0, 1), TargetCategory.Friendly);
            var theirTurret = Spawn(world, new Vector3(6, 0, -1), TargetCategory.Friendly);

            // In a squad together too.
            red.Player.PartyId = loner.Player.PartyId = 77;

            WithFeud(world, () =>
            {
                var recovery = Throw(world, red, target.EntityId);

                CollectionAssert.AreEquivalent(new[] { target.EntityId, blue.Player.EntityId }, recovery.Hits.Select(h => h.EntityId).ToList(),
                    "the creature and the enemy player");
                Assert.IsLessThan(1000, Health(blue));
            });

            Assert.AreEqual(1000, Health(red), "the thrower, 5 m from the blast");
            Assert.AreEqual(1000, Health(redMate), "a clan mate");
            Assert.AreEqual(1000, Health(loner), "a squad mate, not in the feud");
            Assert.AreEqual(Full, (int)soldier.Attributes[Attributes.Health].Current, "a friendly creature");
            Assert.AreEqual(Full, (int)theirTurret.Attributes[Attributes.Health].Current, "a summon of their side");
        }

        [TestMethod]
        public void WithNoFeudNoPlayerIsCaught()
        {
            using var world = new WorldTestContext();
            var red = Player(world, 0, RedClanId);
            var blue = Player(world, 8, BlueClanId);
            var target = Spawn(world, new Vector3(6, 0, 0), TargetCategory.Hostile);

            var recovery = Throw(world, red, target.EntityId);

            Assert.AreEqual(target.EntityId, recovery.Hits.Single().EntityId);
            Assert.AreEqual(1000, Health(blue));
            Assert.AreEqual(1000, Health(red));
        }

        [TestMethod]
        public void ItNeverMisses()
        {
            using var world = new WorldTestContext();
            var thrower = Player(world, 0);
            var target = Spawn(world, new Vector3(25, 0, 0), TargetCategory.Hostile);

            for (var i = 0; i < 40; i++)
            {
                var recovery = Throw(world, thrower, target.EntityId);

                Assert.IsEmpty(recovery.Misses);
                Assert.AreEqual(target.EntityId, recovery.Hits.Single().EntityId, $"throw {i}");
                Assert.IsGreaterThanOrEqualTo(Roll, recovery.Hits.Single().Amount);
            }
        }

        [TestMethod]
        public void ATargetKilledDuringTheThrowStillHasTheBlastGoOffOnIt()
        {
            using var world = new WorldTestContext();
            var thrower = Player(world, 0);
            var target = Spawn(world, new Vector3(20, 0, 0), TargetCategory.Hostile);
            var beside = Spawn(world, new Vector3(22, 0, 0), TargetCategory.Hostile);

            target.State = CharacterState.Dead;
            target.Attributes[Attributes.Health].Current = 0;

            var recovery = Throw(world, thrower, target.EntityId);

            Assert.AreEqual(target.EntityId, recovery.SplashOn);
            Assert.AreEqual(beside.EntityId, recovery.Hits.Single().EntityId, "the body takes nothing more");
            Assert.AreEqual(0, (int)target.Attributes[Attributes.Health].Current);
        }

        [TestMethod]
        public void ATargetGoneFromTheMapLeavesNothingToHitAndTheThrowStillFinishes()
        {
            using var world = new WorldTestContext();
            var thrower = Player(world, 0);
            var nearby = Spawn(world, new Vector3(3, 0, 0), TargetCategory.Hostile);

            var recovery = Throw(world, thrower, 0x7FFF_0000_0001UL);

            Assert.AreEqual(0UL, recovery.SplashOn);
            Assert.IsEmpty(recovery.Hits);
            Assert.AreEqual(Full, (int)nearby.Attributes[Attributes.Health].Current);
        }

        #endregion

        #region What the client is sent

        /// <summary>
        /// GrenadeAbility.DoAbility: for each hit, hitdata[i] is a list of (splashId, rawInfo), and
        /// AnnounceDamage goes on each splashId. So hits is the one it landed on, and every entity
        /// hurt - that one included - is in that one's list.
        /// </summary>
        [TestMethod]
        public void EveryHitGoesOutAsASplashOfTheOneItLandedOn()
        {
            using var world = new WorldTestContext();
            var thrower = Player(world, 0);
            var target = Spawn(world, new Vector3(20, 0, 0), TargetCategory.Hostile);
            var beside = Spawn(world, new Vector3(24, 0, 0), TargetCategory.Hostile);

            var recovery = Throw(world, thrower, target.EntityId, DamageType.Fire);
            var sent = HitResultTests.Decode(recovery);

            Assert.HasCount(6, sent);
            Assert.AreEqual((long)GrenadeAction, sent[0]);
            CollectionAssert.AreEqual(new List<object> { (long)target.EntityId }, (List<object>)sent[2], "hits");
            Assert.IsEmpty((List<object>)sent[3], "misses");
            Assert.IsEmpty((List<object>)sent[4], "missdata");

            var hitdata = (List<object>)sent[5];
            Assert.HasCount(1, hitdata);

            var splash = ((List<object>)hitdata[0]).Cast<List<object>>().ToList();
            CollectionAssert.AreEquivalent(new List<object> { (long)target.EntityId, (long)beside.EntityId }, splash.Select(s => s[0]).ToList());

            foreach (var entry in splash)
            {
                var rawInfo = (List<object>)entry[1];
                var hit = recovery.Hits.Single(h => (long)h.EntityId == (long)entry[0]);

                Assert.HasCount(12, rawInfo);
                Assert.AreEqual((long)DamageType.Fire, rawInfo[0]);
                Assert.AreEqual((long)hit.Amount, rawInfo[5]);
            }
        }

        [TestMethod]
        public void NothingLandedOnIsSentAsNoHits()
        {
            var recovery = new AbilityRecoveryPacket(ActionId.ConsumableGrenade, 3, AbilityRecoveryPacket.HitDataKind.Splash);

            var sent = HitResultTests.Decode(recovery);

            Assert.AreEqual(3L, sent[1]);
            Assert.IsEmpty((List<object>)sent[2]);
            Assert.IsEmpty((List<object>)sent[5], "hits and hitdata the same length");
        }

        #endregion

        #region Thrown from the pack

        [TestMethod]
        [DataRow(1u, 45125u, DamageType.Physical, DisplayName = "level 1, physical")]
        [DataRow(11u, 111022u, DamageType.EMP, DisplayName = "level 11, EMP")]
        [DataRow(21u, 111032u, DamageType.Fire, DisplayName = "level 21, fire")]
        [DataRow(31u, 111042u, DamageType.Ice, DisplayName = "level 31, ice")]
        [DataRow(41u, 111012u, DamageType.Sonic, DisplayName = "level 41, sonic")]
        public void AGrenadeFromThePackIsThrownUsedUpAndCoolsDown(uint level, uint template, DamageType damageType)
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var map = harness.BootcampMap;
            var player = harness.Client.Player;
            var grenades = ToyTests.Grant(harness, template, 2);
            var manager = ToyTests.CreateManager(harness, GrenadeAction, level, template);

            Assert.IsTrue(manager.TryGetAction(ActionId.ConsumableGrenade, level, out var module, out var info));
            Assert.AreEqual(AbilityManager.GrenadeModule, module);
            Assert.AreEqual(15, info.Get(AbilityProperty.EffectRadius));
            Assert.AreEqual((int)damageType, info.Get(AbilityProperty.DamageType));

            var min = info.Get(AbilityProperty.DamageAmountMin);
            var max = info.Get(AbilityProperty.DamageAmountMax);
            var target = Spawn(map, player.Position + new Vector3(12, 0, 0), TargetCategory.Hostile);
            var beside = Spawn(map, player.Position + new Vector3(20, 0, 0), TargetCategory.Hostile);

            try
            {
                harness.Drain();
                manager.RequestPerformAbility(harness.Client, ToyTests.Request(GrenadeAction, level, grenades.EntityId, targetId: target.EntityId));
                Assert.IsFalse(harness.Drain().OfType<UserActionFailedPacket>().Any(), "accepted");

                ToyTests.Land(harness, manager, map.PerformRecovery.Single(a => a.ActionId == ActionId.ConsumableGrenade));

                var packets = harness.Drain();
                Assert.IsFalse(packets.OfType<UserActionFailedPacket>().Any());

                var recovery = packets.OfType<AbilityRecoveryPacket>().Single(p => p.ActionId == ActionId.ConsumableGrenade);
                Assert.AreEqual(target.EntityId, recovery.SplashOn);
                CollectionAssert.AreEquivalent(new[] { target.EntityId, beside.EntityId }, recovery.Hits.Select(h => h.EntityId).ToList());

                foreach (var hit in recovery.Hits)
                {
                    Assert.AreEqual(damageType, hit.DamageType);
                    if (!hit.IsCritical)
                        Assert.IsTrue(hit.Amount >= min && hit.Amount <= max, $"{hit.Amount} in {min}-{max}");
                }

                Assert.AreEqual(1U, grenades.StackSize, "one used");
                Assert.IsTrue(player.ActionReuseUntil.TryGetValue(ActionId.ConsumableGrenade, out var readyAt));
                Assert.IsTrue(readyAt - Environment.TickCount64 is > 5_500 and <= 5_850, "recovery and reuse, as the client counts it");

                // Straight away again: still cooling down, and the last grenade is kept.
                manager.RequestPerformAbility(harness.Client, ToyTests.Request(GrenadeAction, level, grenades.EntityId, targetId: target.EntityId));
                Assert.AreEqual(PlayerMessage.PmCannotPerformActionNow, harness.Drain().OfType<UserActionFailedPacket>().Single().MsgId);
                Assert.IsFalse(map.PerformRecovery.Any(a => a.ActionId == ActionId.ConsumableGrenade));
                Assert.AreEqual(1U, grenades.StackSize);
            }
            finally
            {
                Remove(map);
            }
        }

        [TestMethod]
        public void ThrownAtAFriendItIsRefusedAndKept()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var map = harness.BootcampMap;
            var grenade = ToyTests.Grant(harness, 45125);
            var manager = ToyTests.CreateManager(harness, GrenadeAction, 1, 45125);
            var soldier = Spawn(map, harness.Client.Player.Position + new Vector3(5, 0, 0), TargetCategory.Friendly);

            try
            {
                harness.Drain();
                manager.RequestPerformAbility(harness.Client, ToyTests.Request(GrenadeAction, 1, grenade.EntityId, targetId: soldier.EntityId));

                Assert.AreEqual(PlayerMessage.PmActionFailedActorFriendly, harness.Drain().OfType<UserActionFailedPacket>().Single().MsgId);
                Assert.IsFalse(map.PerformRecovery.Any(a => a.ActionId == ActionId.ConsumableGrenade));
                Assert.AreEqual(1U, grenade.StackSize);
            }
            finally
            {
                Remove(map);
            }
        }

        #endregion

        #region Fixture

        private AbilityRecoveryPacket Throw(WorldTestContext world, Client thrower, ulong targetId, DamageType damageType = DamageType.Physical)
        {
            var info = new ActionLevelInfo
            {
                ActionId = ActionId.ConsumableGrenade, Level = 1, MaxRange = 30, WindupMs = 1000, RecoveryMs = 1000, ReuseMs = 5000, StartReuseOnPerform = true
            };
            info.Properties[AbilityProperty.DamageAmountMin] = Roll;
            info.Properties[AbilityProperty.DamageAmountMax] = Roll;
            info.Properties[AbilityProperty.DamageType] = (int)damageType;
            info.Properties[AbilityProperty.EffectRadius] = 15;

            var action = new ActionInfo { ActionId = ActionId.ConsumableGrenade, Name = "CONSUMABLE_GRENADE", Module = AbilityManager.GrenadeModule };
            action.Levels[1] = info;

            WorldTestContext.Drain(thrower);
            BootcampRuntimeTestHarness.InvokeResolveDirectDamage(Abilities(), world.Map, thrower, action, info,
                new ActionData(thrower.Player, ActionId.ConsumableGrenade, 1, targetId, 0));

            return WorldTestContext.Drain(thrower).Select(p => p.Message).OfType<CallMethodMessage>()
                .Select(m => m.Packet).OfType<AbilityRecoveryPacket>().Single();
        }

        private static AbilityManager Abilities() =>
            (AbilityManager)typeof(AbilityManager)
                .GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null,
                    new[] { typeof(IGameUnitOfWorkFactory), typeof(MissionApplication) }, null)!
                .Invoke(new object[] { null, null });

        private static Client Player(WorldTestContext world, float x, uint clanId = 0)
        {
            var client = world.CreateClient(x, 0);
            client.Player.ClanId = clanId;
            client.Player.Attributes[Attributes.Health] = new ActorAttributes(Attributes.Health, 1000, 1000, 1000, 0, 0);
            client.Player.Attributes[Attributes.Armor] = new ActorAttributes(Attributes.Armor, 0, 0, 0, 0, 0);
            CellManager.Instance.AddToWorld(client);
            WorldTestContext.Drain(client);
            return client;
        }

        private static int Health(Client client) => (int)client.Player.Attributes[Attributes.Health].Current;

        private Creature Spawn(WorldTestContext world, Vector3 position, TargetCategory category) => Spawn(world.Map, position, category);

        /// <summary>A living creature of the category with plenty of health and no armour, in the map's cells.</summary>
        private Creature Spawn(MapChannel map, Vector3 position, TargetCategory category)
        {
            var creature = new Creature
            {
                Name = "Fixture",
                TargetCategory = category,
                MapContextId = map.MapInfo.MapContextId,
                RuntimeMapChannel = map,
                Position = position,
                EntityClass = EntityClasses.HumanBaseMale,
                State = CharacterState.Idle,
                Level = 1,
                AppearanceData = new Dictionary<EquipmentData, AppearanceData>()
            };
            creature.Attributes[Attributes.Health] = new ActorAttributes(Attributes.Health, Full, Full, Full, 0, 0);
            creature.Attributes[Attributes.Armor] = new ActorAttributes(Attributes.Armor, 0, 0, 0, 0, 0);
            EntityManager.Instance.RegisterEntity(creature.EntityId, EntityType.Creature);
            EntityManager.Instance.RegisterCreature(creature);
            EntityManager.Instance.RegisterActor(creature.EntityId, creature);

            var seed = CellManager.Instance.GetCellSeed(creature.Position);
            creature.Cells = CellManager.Instance.CreateCellMatrix(map, seed & 0xFFFF, seed >> 16);
            CellManager.Instance.GetCell(map, seed & 0xFFFF, seed >> 16).CreatureList.Add(creature);

            _creatures.Add(creature);
            return creature;
        }

        private void Remove(MapChannel map)
        {
            foreach (var cell in map.MapCellInfo.Cells.Values)
                cell.CreatureList.RemoveAll(_creatures.Contains);
        }

        /// <summary>Red challenges Blue and Blue accepts; the players are online for the feud's messages.</summary>
        private static void WithFeud(WorldTestContext world, Action body)
        {
            var online = world.Map.ClientList.ToList();

            lock (Server.Clients)
                Server.Clients.AddRange(online);

            ClanFeuds.Feud feud = null;

            try
            {
                feud = ClanFeuds.Instance.Start(
                    new ClanEntry { Id = RedClanId, Name = "Red", IsPvP = true },
                    new ClanEntry { Id = BlueClanId, Name = "Blue", IsPvP = true });
                Assert.IsNotNull(feud);

                body();
            }
            finally
            {
                if (feud != null)
                    ClanFeuds.Instance.End(feud, ClanFeuds.Outcome.Cancelled);

                lock (Server.Clients)
                    foreach (var client in online)
                        Server.Clients.Remove(client);
            }
        }

        #endregion
    }
}
