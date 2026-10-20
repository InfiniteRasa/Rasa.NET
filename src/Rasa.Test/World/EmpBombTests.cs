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
    using Rasa.Packets.MapChannel.Server;
    using Rasa.Structures;
    using Rasa.Test.Missions;

    /// <summary>
    /// The EMP Bombs (CONSUMABLE_BOMB 10000004, abilities.placebomb) were refused as not
    /// implemented, bomb kept - and Class I is a mission reward. Used now, the bomb goes down in
    /// front of the player carrying PLACE_BOMB, and DELAY seconds later blows up on everything
    /// hostile within EFFECT_RADIUS of it, the thrower and their side spared.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class EmpBombTests
    {
        private const int BombAction = 10000004;
        private const uint ClassOne = 111247;   // Class I EMP Bomb: level 1, 2000-3000
        private const uint ClassTen = 111246;   // Class X EMP Bomb: level 10, 100000-150000
        private const int Full = 1_000_000;

        private readonly List<Creature> _creatures = new List<Creature>();

        [TestMethod]
        [DataRow(ClassOne, 1u, DisplayName = "Class I")]
        [DataRow(ClassTen, 10u, DisplayName = "Class X")]
        public void TheBombGoesDownInFrontOfThePlayerAndBlowsUpAfterItsDelay(uint template, uint level)
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var map = harness.BootcampMap;
            var player = harness.Client.Player;
            var bombs = ToyTests.Grant(harness, template, 2);
            var manager = ToyTests.CreateManager(harness, BombAction, level, template);

            Assert.IsTrue(manager.TryGetAction(ActionId.ConsumableBomb, level, out var module, out var info));
            Assert.AreEqual(AbilityManager.PlaceBombModule, module);
            Assert.IsTrue(AbilityManager.IsAttackAction(new ActionInfo { Module = module }, info), "an attack, for Mind Control's sake");

            var min = info.Get(AbilityProperty.DamageAmountMin);
            var max = info.Get(AbilityProperty.DamageAmountMax);
            var health = (int)player.Attributes[Attributes.Health].Current;

            try
            {
                harness.Drain();
                manager.RequestPerformAbility(harness.Client, ToyTests.Request(BombAction, level, bombs.EntityId));
                Assert.IsFalse(harness.Drain().OfType<UserActionFailedPacket>().Any(), "no target wanted");

                var landed = Environment.TickCount64;
                ToyTests.Land(harness, manager, map.PerformRecovery.Single(a => a.ActionId == ActionId.ConsumableBomb));

                var bomb = Bombs(map).Single();
                var spot = player.Position + AbilityManager.FacingOf(player) * 0.2f;
                Assert.AreEqual(0f, Vector2.Distance(new(bomb.Position.X, bomb.Position.Z), new(spot.X, spot.Z)), 0.1f, "20 cm in front");

                var packets = harness.Drain();
                var attached = packets.OfType<GameEffectAttachedPacket>().Single(p => p.EffectTypeId == AbilityManager.PlaceBombTypeId);
                Assert.AreEqual(level, attached.EffectLevel, "the blast's FX is by level");
                Assert.IsFalse(attached.Announced, "the recovery's hit announces it");
                CollectionAssert.AreEqual(new[] { bomb.EntityId },
                    packets.OfType<AbilityRecoveryPacket>().Single(p => p.ActionId == ActionId.ConsumableBomb).Hits.Select(h => h.EntityId).ToArray());
                Assert.AreEqual(1U, bombs.StackSize, "one used");

                var near = Spawn(map, bomb.Position + new Vector3(10, 0, 0), TargetCategory.Hostile);
                var edge = Spawn(map, bomb.Position + new Vector3(0, 0, 14.9f), TargetCategory.Hostile);
                var wildlife = Spawn(map, bomb.Position + new Vector3(-6, 0, 0), TargetCategory.Neutral);
                var outside = Spawn(map, bomb.Position + new Vector3(15.5f, 0, 0), TargetCategory.Hostile);
                var soldier = Spawn(map, bomb.Position + new Vector3(3, 0, 0), TargetCategory.Friendly);

                manager.PlacedBombWorker(map, landed + 4_500);
                Assert.IsFalse(harness.Drain().OfType<GameEffectAnnounceDamagePacket>().Any(), "not yet");
                Assert.AreEqual(Full, Health(near));

                manager.PlacedBombWorker(map, landed + 5_100);

                var blast = harness.Drain().OfType<GameEffectAnnounceDamagePacket>().Single();
                Assert.AreEqual("DoExplosion", blast.MethodName);
                Assert.AreEqual(attached.EffectId, blast.EffectId);
                CollectionAssert.AreEquivalent(new[] { near.EntityId, edge.EntityId, wildlife.EntityId }, blast.Hits.Select(h => h.EntityId).ToList());

                foreach (var hit in blast.Hits)
                {
                    var creature = _creatures.Single(c => c.EntityId == hit.EntityId);

                    Assert.AreEqual(DamageType.EMP, hit.DamageType);
                    Assert.AreEqual(Full - hit.Amount, Health(creature));
                    if (!hit.IsCritical)
                        Assert.IsTrue(hit.Amount >= min && hit.Amount <= max, $"{hit.Amount} in {min}-{max}, at any distance");
                }

                Assert.AreEqual(Full, Health(outside));
                Assert.AreEqual(Full, Health(soldier));
                Assert.AreEqual(health, (int)player.Attributes[Attributes.Health].Current, "the thrower, standing on it");

                manager.PlacedBombWorker(map, landed + 6_000);
                Assert.HasCount(1, Bombs(map).ToArray(), "kept a moment for the blast");
                Assert.IsFalse(harness.Drain().OfType<GameEffectAnnounceDamagePacket>().Any(), "once");

                manager.PlacedBombWorker(map, landed + 7_200);
                Assert.IsEmpty(Bombs(map).ToArray());
            }
            finally
            {
                Remove(map);
            }
        }

        [TestMethod]
        public void ABombWhoseThrowerHasLeftIsTakenAwayUnexploded()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var map = harness.BootcampMap;
            var player = harness.Client.Player;
            var bomb = ToyTests.Grant(harness, ClassOne);
            var manager = ToyTests.CreateManager(harness, BombAction, 1, ClassOne);

            manager.RequestPerformAbility(harness.Client, ToyTests.Request(BombAction, 1, bomb.EntityId));
            var landed = Environment.TickCount64;
            ToyTests.Land(harness, manager, map.PerformRecovery.Single(a => a.ActionId == ActionId.ConsumableBomb));
            var placed = Bombs(map).Single();
            var near = Spawn(map, placed.Position + new Vector3(5, 0, 0), TargetCategory.Hostile);
            var context = player.MapContextId;

            try
            {
                player.MapContextId = context + 1;
                harness.Drain();

                manager.PlacedBombWorker(map, landed + 5_100);

                Assert.IsFalse(harness.Drain().OfType<GameEffectAnnounceDamagePacket>().Any());
                Assert.AreEqual(Full, Health(near));
                Assert.IsEmpty(Bombs(map).ToArray());
            }
            finally
            {
                player.MapContextId = context;
                Remove(map);
            }
        }

        /// <summary>One who comes into view of a bomb still waiting is shown its effect, so the blast plays for them.</summary>
        [TestMethod]
        public void OneWhoComesIntoViewOfAWaitingBombIsShownIt()
        {
            using var harness = BootcampRuntimeTestHarness.Create();
            var map = harness.BootcampMap;
            var player = harness.Client.Player;
            var item = ToyTests.Grant(harness, ClassOne);
            var manager = ToyTests.CreateManager(harness, BombAction, 1, ClassOne);

            manager.RequestPerformAbility(harness.Client, ToyTests.Request(BombAction, 1, item.EntityId));
            var landed = Environment.TickCount64;
            ToyTests.Land(harness, manager, map.PerformRecovery.Single(a => a.ActionId == ActionId.ConsumableBomb));
            var bomb = Bombs(map).Single();
            var effectId = harness.Drain().OfType<GameEffectAttachedPacket>().Single(p => p.EffectTypeId == AbilityManager.PlaceBombTypeId).EffectId;

            // The server loads every class at start-up; the harness, what a test asks for.
            ToyTests.LoadClass(harness, (uint)AbilityManager.PlacedBombClass);

            DynamicObjectManager.Instance.CreateDynamicObjectOnClient(harness.Client, bomb);

            var shown = harness.Drain().OfType<GameEffectAttachedPacket>().Single();
            Assert.AreEqual((AbilityManager.PlaceBombTypeId, effectId, true, player.EntityId), (shown.EffectTypeId, shown.EffectId, shown.Announced, shown.SourceId));

            manager.PlacedBombWorker(map, landed + 5_100);
            harness.Drain();

            DynamicObjectManager.Instance.CreateDynamicObjectOnClient(harness.Client, bomb);
            Assert.IsFalse(harness.Drain().OfType<GameEffectAttachedPacket>().Any(), "spent");
        }

        private static IEnumerable<DynamicObject> Bombs(MapChannel map) =>
            map.MapCellInfo.Cells.Values.SelectMany(cell => cell.DynamicObjectList)
                .Where(obj => obj.EntityClassId == AbilityManager.PlacedBombClass).Distinct();

        private static int Health(Creature creature) => (int)creature.Attributes[Attributes.Health].Current;

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
    }
}
