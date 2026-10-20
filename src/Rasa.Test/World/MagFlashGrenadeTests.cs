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
    using Rasa.Packets.MapChannel.Server;
    using Rasa.Packets.Protocol;
    using Rasa.Repositories.UnitOfWork;
    using Rasa.Structures;
    using Rasa.Structures.Char;
    using Rasa.Test.Missions;

    /// <summary>
    /// The Mag Flash Grenade (CONSUMABLE_FLASH_BANG 498, abilities.flashbang) was refused as not
    /// implemented, grenade kept. Thrown now, it lands at the aimed spot carrying
    /// FLASH_BANG_DELAY_EFFECT, and DELAY_TIME_MS later blinds everything hostile within
    /// RADIUS_AROUND_TARGET for EFFECT_DURATION_MS - FLASH_BANG_BLIND_EFFECT, announced by the
    /// delay effect's tick - the thrower and their side spared.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class MagFlashGrenadeTests
    {
        private const int FlashAction = 498;
        private const uint FlashTemplate = 121683;     // Mag Flash Grenade: 498 level 1
        private const uint RedClanId = 900301;
        private const uint BlueClanId = 900302;

        private readonly List<Creature> _creatures = new List<Creature>();

        [TestMethod]
        public void ItLandsWhereItWasAimedAndBlindsWhatIsHostileAroundIt()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var map = harness.BootcampMap;
            var player = harness.Client.Player;
            var grenades = ToyTests.Grant(harness, FlashTemplate, 2);
            var manager = ToyTests.CreateManager(harness, FlashAction, 1, FlashTemplate);

            Assert.IsTrue(manager.TryGetAction(ActionId.ConsumableFlashBang, 1, out var module, out var info));
            Assert.AreEqual(AbilityManager.FlashGrenadeModule, module);
            Assert.AreEqual((15, 3000, 5000), (info.Get(AbilityProperty.RadiusAroundTarget), info.Get(AbilityProperty.DelayTimeMs), info.Get(AbilityProperty.EffectDurationMs)));
            Assert.IsTrue(AbilityManager.IsAttackAction(new ActionInfo { Module = module }, info), "an attack, for Mind Control's sake");

            var aimed = player.Position + AbilityManager.FacingOf(player) * 6f;

            try
            {
                harness.Drain();
                manager.RequestPerformAbility(harness.Client, ToyTests.Request(FlashAction, 1, grenades.EntityId, aimed));
                Assert.IsFalse(harness.Drain().OfType<UserActionFailedPacket>().Any());

                var landed = Environment.TickCount64;
                ToyTests.Land(harness, manager, map.PerformRecovery.Single(a => a.ActionId == ActionId.ConsumableFlashBang));

                var proxy = Proxies(map).Single();
                Assert.AreEqual(0f, Vector2.Distance(new(proxy.Position.X, proxy.Position.Z), new(aimed.X, aimed.Z)), 0.5f);

                var packets = harness.Drain();
                var delay = packets.OfType<GameEffectAttachedPacket>().Single(p => p.EffectTypeId == AbilityManager.FlashGrenadeDelayTypeId);
                Assert.IsFalse(delay.Announced, "the recovery's hit announces it");
                CollectionAssert.AreEqual(new[] { proxy.EntityId },
                    packets.OfType<AbilityRecoveryPacket>().Single(p => p.ActionId == ActionId.ConsumableFlashBang).Hits.Select(h => h.EntityId).ToArray());
                Assert.AreEqual(1U, grenades.StackSize, "one used");
                Assert.IsTrue(player.ActionReuseUntil.ContainsKey(ActionId.ConsumableFlashBang), "15 s reuse");

                var near = Spawn(map, proxy.Position + new Vector3(10, 0, 0), TargetCategory.Hostile);
                var edge = Spawn(map, proxy.Position + new Vector3(0, 0, -14.9f), TargetCategory.Hostile);
                var wildlife = Spawn(map, proxy.Position + new Vector3(-4, 0, 0), TargetCategory.Neutral);
                var outside = Spawn(map, proxy.Position + new Vector3(15.5f, 0, 0), TargetCategory.Hostile);
                var soldier = Spawn(map, proxy.Position + new Vector3(2, 0, 0), TargetCategory.Friendly);

                manager.FlashGrenadeWorker(map, landed + 2_500);
                Assert.IsFalse(Detection.IsBlind(near), "not yet");
                Assert.IsFalse(harness.Drain().OfType<GameEffectTickPacket>().Any());

                manager.FlashGrenadeWorker(map, landed + 3_100);

                foreach (var blinded in new[] { near, edge, wildlife })
                {
                    Assert.IsTrue(Detection.IsBlind(blinded), $"{Vector3.Distance(proxy.Position, blinded.Position)} m");
                    var blind = blinded.ActiveEffects.Values.Single(e => e.TypeId == AbilityManager.FlashGrenadeBlindTypeId);
                    Assert.IsTrue(blind.ExpiresTick - Environment.TickCount64 is > 4_500 and <= 5_000, "EFFECT_DURATION_MS");
                }

                Assert.IsFalse(Detection.IsBlind(outside), "15.5 m out");
                Assert.IsFalse(Detection.IsBlind(soldier), "a friendly creature");
                Assert.IsFalse(Detection.IsBlind(player), "the thrower, 6 m from it");

                packets = harness.Drain();
                Assert.IsTrue(packets.OfType<GameEffectAttachedPacket>().Where(p => p.EffectTypeId == AbilityManager.FlashGrenadeBlindTypeId).All(p => !p.Announced),
                    "put on quietly: the tick announces it");
                var tick = packets.OfType<GameEffectTickPacket>().Single();
                Assert.AreEqual((delay.EffectId, GameEffectTickPacket.TickKind.EntityIds), (tick.EffectId, tick.Kind));
                CollectionAssert.AreEquivalent(new[] { near.EntityId, edge.EntityId, wildlife.EntityId }, tick.Entries.Select(e => e.EntityId).ToList());

                manager.FlashGrenadeWorker(map, landed + 4_000);
                Assert.HasCount(1, Proxies(map).ToArray(), "kept while the flash plays");
                Assert.IsFalse(harness.Drain().OfType<GameEffectTickPacket>().Any(), "once");

                manager.FlashGrenadeWorker(map, landed + 5_200);
                Assert.AreEqual(delay.EffectId, harness.Drain().OfType<GameEffectDetachedPacket>().Single(p => p.EffectId == delay.EffectId).EffectId);
                Assert.IsEmpty(Proxies(map).ToArray());
            }
            finally
            {
                Remove(map);
            }
        }

        [TestMethod]
        public void AnEnemyPlayerIsBlindedForHalfAsLongAndLetsGoOfTheirTargetAndAFriendIsNot()
        {
            using var world = new WorldTestContext();
            var red = Player(world, 0, RedClanId);
            var redMate = Player(world, 12, RedClanId);
            var loner = Player(world, 9);
            var blue = Player(world, 14, BlueClanId);
            blue.Player.Target = red.Player.EntityId;

            WithFeud(world, () =>
            {
                var landed = Throw(world, red, new Vector3(10, 0, 0));
                Abilities().FlashGrenadeWorker(world.Map, landed + 3_100);

                Assert.IsTrue(Detection.IsBlind(blue.Player), "the enemy, 4 m from it");
                Assert.IsTrue(blue.Player.ActiveEffects.Values.Single(e => e.TypeId == AbilityManager.FlashGrenadeBlindTypeId).ExpiresTick - Environment.TickCount64 is > 2_000 and <= 2_500,
                    "half of EFFECT_DURATION_MS, as any control between players");
                Assert.AreEqual(0UL, blue.Player.Target, "let go of on the server too");

                Assert.IsFalse(Detection.IsBlind(red.Player), "the thrower");
                Assert.IsFalse(Detection.IsBlind(redMate.Player), "a clan mate");
                Assert.IsFalse(Detection.IsBlind(loner.Player), "not in the feud");
            });
        }

        [TestMethod]
        public void AFlashWhoseThrowerHasLeftIsTakenAwayWithoutGoingOff()
        {
            using var world = new WorldTestContext();
            var thrower = Player(world, 0);
            var near = Spawn(world.Map, new Vector3(12, 0, 0), TargetCategory.Hostile);
            var landed = Throw(world, thrower, new Vector3(10, 0, 0));

            thrower.Player.MapChannel = new MapChannel();
            try
            {
                Abilities().FlashGrenadeWorker(world.Map, landed + 3_100);

                Assert.IsFalse(Detection.IsBlind(near));
                Assert.IsEmpty(Proxies(world.Map).ToArray());
            }
            finally
            {
                thrower.Player.MapChannel = world.Map;
            }
        }

        /// <summary>One who comes into view of a flash still waiting is shown it, so the flash plays for them.</summary>
        [TestMethod]
        public void OneWhoComesIntoViewOfAWaitingFlashIsShownIt()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var map = harness.BootcampMap;
            var player = harness.Client.Player;
            var item = ToyTests.Grant(harness, FlashTemplate);
            var manager = ToyTests.CreateManager(harness, FlashAction, 1, FlashTemplate);

            manager.RequestPerformAbility(harness.Client, ToyTests.Request(FlashAction, 1, item.EntityId, player.Position + AbilityManager.FacingOf(player) * 20f));
            var landed = Environment.TickCount64;
            ToyTests.Land(harness, manager, map.PerformRecovery.Single(a => a.ActionId == ActionId.ConsumableFlashBang));
            var proxy = Proxies(map).Single();
            var effectId = harness.Drain().OfType<GameEffectAttachedPacket>().Single(p => p.EffectTypeId == AbilityManager.FlashGrenadeDelayTypeId).EffectId;

            // The server loads every class at start-up; the harness, what a test asks for.
            ToyTests.LoadClass(harness, (uint)AbilityManager.LocationProxyClass);

            DynamicObjectManager.Instance.CreateDynamicObjectOnClient(harness.Client, proxy);

            var shown = harness.Drain().OfType<GameEffectAttachedPacket>().Single();
            Assert.AreEqual((AbilityManager.FlashGrenadeDelayTypeId, effectId, true, player.EntityId), (shown.EffectTypeId, shown.EffectId, shown.Announced, shown.SourceId));

            manager.FlashGrenadeWorker(map, landed + 3_100);
            harness.Drain();

            DynamicObjectManager.Instance.CreateDynamicObjectOnClient(harness.Client, proxy);
            Assert.IsFalse(harness.Drain().OfType<GameEffectAttachedPacket>().Any(), "spent");
        }

        #region Fixture

        /// <summary>The thrower's grenade lands at the point (ThrowFlashGrenade); returns when.</summary>
        private static long Throw(WorldTestContext world, Client thrower, Vector3 at)
        {
            var info = new ActionLevelInfo { ActionId = ActionId.ConsumableFlashBang, Level = 1, MaxRange = 30 };
            info.Properties[AbilityProperty.RadiusAroundTarget] = 15;
            info.Properties[AbilityProperty.DelayTimeMs] = 3000;
            info.Properties[AbilityProperty.EffectDurationMs] = 5000;

            var action = new ActionData(thrower.Player, ActionId.ConsumableFlashBang, 1, 0, 0) { TargetLocation = at };
            var recovery = new AbilityRecoveryPacket(ActionId.ConsumableFlashBang, 1, AbilityRecoveryPacket.HitDataKind.None);
            var landed = Environment.TickCount64;

            typeof(AbilityManager).GetMethod("ThrowFlashGrenade", BindingFlags.Static | BindingFlags.NonPublic)!
                .Invoke(null, new object[] { world.Map, thrower.Player, info, action, recovery });

            return landed;
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
            CellManager.Instance.AddToWorld(client);
            WorldTestContext.Drain(client);
            return client;
        }

        private static IEnumerable<DynamicObject> Proxies(MapChannel map) =>
            map.MapCellInfo.Cells.Values.SelectMany(cell => cell.DynamicObjectList)
                .Where(obj => obj.EntityClassId == AbilityManager.LocationProxyClass).Distinct();

        /// <summary>A living creature of the category, in the map's cells.</summary>
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
            creature.Attributes[Attributes.Health] = new ActorAttributes(Attributes.Health, 1000, 1000, 1000, 0, 0);
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
