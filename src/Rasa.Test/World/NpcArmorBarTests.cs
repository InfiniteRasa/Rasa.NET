using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Managers;
using Rasa.Memory;
using Rasa.Packets.MapChannel.Server;
using Rasa.Packets.Game.Server;
using Rasa.Packets.Protocol;
using Rasa.Structures;

namespace Rasa.Test.World
{
    [TestClass, DoNotParallelize]
    public class NpcArmorBarTests
    {
        [TestMethod]
        [DataRow(3, 5, 0.6)]
        [DataRow(1, 5, 0.2)]
        [DataRow(0, 5, 0.0)]
        [DataRow(3, 1, 3.0)]
        [DataRow(3, 0, 0.0)]
        public void ArmorUpdateCarriesTheClientsPerSecondRate(int amount, int period, double expected)
        {
            var armor = new ActorAttributes(Attributes.Armor, 70, 70, 10, amount, period);
            Assert.AreEqual(expected, ReadRate(new UpdateArmorPacket(armor, 987)), 0.000001);
            Assert.AreEqual(amount, armor.RefreshAmount, "Serialization must not alter server regeneration.");
            Assert.AreEqual(period, armor.RefreshPeriod);
        }

        [TestMethod]
        [DataRow(0)]
        [DataRow(-100)]
        [DataRow(100)]
        public void PredictedArmorMatchesCombatTicksDamageEffectsAndReturnToRest(int modifier)
        {
            using var world = new WorldTestContext();
            var watcher = world.CreateClient(x: 10);
            watcher.Player.Attributes[Attributes.Health] = new ActorAttributes(Attributes.Health, 1000, 1000, 1000, 0, 1);
            var creature = new Creature {
                Position = Vector3.Zero, RuntimeMapChannel = world.Map, MapContextId = world.Map.MapInfo.MapContextId,
                TargetCategory = TargetCategory.Hostile, State = CharacterState.Idle
            };
            creature.Attributes[Attributes.Health] = CreatureHealth.NewAttribute(1000);
            var armor = new ActorAttributes(Attributes.Armor, 70, 70, 10, CreatureArmor.BaseRegen(70), 1);
            creature.Attributes[Attributes.Armor] = armor;
            if (modifier != 0)
                creature.ActiveEffects[1] = new GameEffect { EffectId = 1, ArmorRegenPercent = modifier };
            var seed = CellManager.Instance.GetCellSeed(creature.Position);
            creature.Cells = CellManager.Instance.CreateCellMatrix(world.Map, seed & 0xFFFF, seed >> 16);
            watcher.Player.Cells = creature.Cells;
            var cell = CellManager.Instance.GetCell(world.Map, seed & 0xFFFF, seed >> 16);
            cell.CreatureList.Add(creature);
            cell.ClientList.Add(watcher);

            Assert.IsTrue(BehaviorManager.Instance.TrySetActionFighting(creature, watcher.Player.EntityId));
            WorldTestContext.Drain(watcher);
            // First regeneration worker pass publishes the changed five-second period.
            CreatureArmor.Regenerate(world.Map);
            var update = Updates(watcher).Single();
            var rate = ReadRate(update);
            var effective = GameEffectManager.RegenAmount(creature, armor);
            Assert.AreEqual(effective / 5.0, rate, 0.000001);
            Assert.AreEqual(10, armor.Current);
            // Recv_UpdateArmor always sets refreshPeriod=1. Predict using the encoded rate.
            var predicted = (double)update.Armor.Current;
            for (var second = 2; second <= 20; second++)
            {
                predicted += rate;
                CreatureArmor.Regenerate(world.Map);
            }
            // The client interpolates between discrete server ticks; allow one tick of phase.
            Assert.AreEqual(10 + effective * 4, armor.Current);
            Assert.AreEqual((double)armor.Current, predicted, effective + 0.00001);

            ActorManager.Instance.Damage(world.Map, creature, 1, watcher.Player);
            update = Updates(watcher).Single();
            Assert.AreEqual(armor.Current, update.Armor.Current);
            Assert.AreEqual(rate, ReadRate(update), 0.000001, "Damage must not reset prediction to five times the rate.");

            creature.Controller.CurrentAction = BehaviorManager.BehaviorActionWander;
            CreatureArmor.Regenerate(world.Map);
            update = Updates(watcher).Single();
            Assert.AreEqual((double)effective, ReadRate(update), 0.000001, "Rest restores the one-second rate.");
        }

        [TestMethod]
        [DataRow(0)]
        [DataRow(-100)]
        [DataRow(100)]
        public void ANewObserverReceivesTheSameArmorRateInBothIntroductionPackets(int modifier)
        {
            using var world = new WorldTestContext();
            var watcher = world.CreateClient();
            var creature = new Creature {
                EntityClass = EntityClasses.HumanBaseMale, RuntimeMapChannel = world.Map,
                MapContextId = world.Map.MapInfo.MapContextId, TargetCategory = TargetCategory.Hostile,
                State = CharacterState.Idle, AppearanceData = new Dictionary<EquipmentData, AppearanceData>()
            };
            creature.Attributes[Attributes.Health] = CreatureHealth.NewAttribute(1000);
            creature.Attributes[Attributes.Armor] = new ActorAttributes(Attributes.Armor, 70, 70, 10, 3, 5);
            creature.ActiveEffects[1] = new GameEffect { EffectId = 1, ArmorRegenPercent = modifier };
            EntityManager.Instance.RegisterEntity(creature.EntityId, EntityType.Creature);
            EntityManager.Instance.RegisterCreature(creature);
            try
            {
                CreatureManager.Instance.CreateCreatureOnClient(watcher, creature);
                var made = WorldTestContext.Drain(watcher).Select(p => p.Message).OfType<CallMethodMessage>()
                    .Select(p => p.Packet).OfType<CreatePhysicalEntityPacket>().Single();
                var initial = made.EntityData.OfType<AttributeInfoPacket>().Single().ActorAttributes[Attributes.Armor];
                var expected = initial.RefreshAmount / (double)initial.RefreshPeriod;
                var followup = made.EntityData.OfType<UpdateAttributesPacket>().Single();
                using var stream = new MemoryStream();
                using (var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, true))
                    followup.Write(new PythonWriter(writer));
                stream.Position = 0;
                using var reader = new PythonReader(new BinaryReader(stream));
                Assert.AreEqual(2, reader.ReadTuple());
                var found = false;
                for (var count = reader.ReadList(); count > 0; count--)
                {
                    Assert.AreEqual(5, reader.ReadTuple());
                    var id = (Attributes)reader.ReadInt();
                    reader.ReadInt(); reader.ReadInt(); reader.ReadInt();
                    var rate = reader.ReadNumber();
                    if (id == Attributes.Armor) { found = true; Assert.AreEqual(expected, rate, 0.000001); }
                }
                Assert.IsTrue(found);
                Assert.AreEqual(0UL, reader.ReadULong());
                Assert.AreEqual(stream.Length, stream.Position);
                Assert.AreEqual(3, creature.Attributes[Attributes.Armor].RefreshAmount);
                Assert.AreEqual(5, creature.Attributes[Attributes.Armor].RefreshPeriod);
            }
            finally
            {
                EntityManager.Instance.UnregisterCreature(creature.EntityId);
                EntityManager.Instance.UnregisterEntity(creature.EntityId);
                EntityManager.Instance.FreeEntity(creature.EntityId);
            }
        }

        private static List<UpdateArmorPacket> Updates(Rasa.Game.Client client) => WorldTestContext.Drain(client)
            .Select(p => p.Message).OfType<CallMethodMessage>().Select(p => p.Packet).OfType<UpdateArmorPacket>().ToList();

        private static double ReadRate(UpdateArmorPacket packet)
        {
            using var stream = new MemoryStream();
            using (var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, true))
                packet.Write(new PythonWriter(writer));
            stream.Position = 0;
            using var reader = new PythonReader(new BinaryReader(stream));
            Assert.AreEqual(4, reader.ReadTuple());
            Assert.AreEqual(packet.Armor.Current, reader.ReadInt());
            Assert.AreEqual(packet.Armor.CurrentMax, reader.ReadInt());
            var rate = reader.ReadNumber();
            Assert.AreEqual(packet.WhoId, reader.ReadULong());
            Assert.AreEqual(stream.Length, stream.Position);
            return rate;
        }
    }
}
