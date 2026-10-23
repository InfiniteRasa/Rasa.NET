using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Gameplay
{
    using Rasa.Data;
    using Rasa.Game;
    using Rasa.Managers;
    using Rasa.Models;
    using Rasa.Packets;
    using Rasa.Packets.MapChannel.Server;
    using Rasa.Packets.Protocol;
    using Rasa.Structures;
    using Rasa.Structures.World;
    using Rasa.Test.World;

    /// <summary>
    /// The armour skills' standing effects are sent at a level the client has visuals for
    /// (ManifestationManager.SkillPassive, GameEffectFx.LevelFor). Reflective Armor sent its
    /// damage back from pump 2 with no reflection to be seen: the hidden MEDIUM_ARMOR_SKILL, whose
    /// AnnounceReflect plays it, went on at the pump, and its FX are at level 1 alone.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class ReflectiveArmorFxTests
    {
        private const uint ArmorClass = 990611;

        [TestMethod]
        public void AtEveryPumpTheReflectionIsSentAtTheLevelItsFxAreAt()
        {
            for (var pump = 1; pump <= 5; pump++)
            {
                using var world = new WorldTestContext();
                var client = Watch(world);
                var player = client.Player;
                Learn(player, ArmorSkills.Reflective, pump);
                Wear(client, EquipmentData.Helmet, ArmorSkills.Reflective);
                Wear(client, EquipmentData.Torso, ArmorSkills.Reflective);

                ManifestationManager.Instance.SyncSkillPassives(client);

                var hidden = player.ActiveEffects.Values.Single(e => e.TypeId == ArmorSkills.ReflectiveHiddenTypeId);
                Assert.AreEqual(ArmorSkills.ReflectPercent(pump, 2), hidden.ReflectPercent, $"pump {pump}: what it reflects goes by the pump");
                Assert.IsTrue(GameEffectFx.HasRow(hidden.TypeId, hidden.EffectLevel), $"pump {pump}: sent at level {hidden.EffectLevel}");
                var shown = player.ActiveEffects.Values.Single(e => e.TypeId == ArmorSkills.ReflectiveVisibleTypeId);
                Assert.IsTrue(GameEffectFx.HasRow(shown.TypeId, shown.EffectLevel), $"pump {pump}: the icon's effect at level {shown.EffectLevel}");
                Assert.AreEqual(ArmorSkills.ReflectPercent(pump, 2), shown.Tooltip["reflectAmt"]);

                var attached = Calls(client).OfType<GameEffectAttachedPacket>().Single(p => p.EffectId == hidden.EffectId);
                Assert.AreEqual(hidden.EffectLevel, attached.EffectLevel);

                // A hit sent back: announced on that effect, to the wearer.
                var attacker = Spawn(world);
                Assert.IsTrue(Reflection.Reflect(world.Map, player, attacker, 100, DamageType.Physical) > 0);
                var announce = Calls(client).OfType<GameEffectAnnounceReflectPacket>().Single();
                Assert.AreEqual(hidden.EffectId, announce.EffectId);
                Assert.AreEqual(attacker.EntityId, announce.AttackerId);
            }
        }

        /// <summary>The Mech aura has its visual at level 1 alone too, and so do the copies it puts on the squad.</summary>
        [TestMethod]
        public void TheMechAuraIsSentAtTheLevelItsFxAreAt()
        {
            using var world = new WorldTestContext();
            var client = Watch(world);
            Learn(client.Player, ArmorSkills.Mech, 4);
            Wear(client, EquipmentData.Gloves, ArmorSkills.Mech);

            ManifestationManager.Instance.SyncSkillPassives(client);

            var aura = client.Player.ActiveEffects.Values.Single(e => e.TypeId == ArmorSkills.MechAuraTypeId);
            Assert.AreEqual(ArmorSkills.RegenPercent(4, 1), aura.PowerRegenPercent);
            Assert.AreEqual(ArmorSkills.AuraRadius(4), aura.AuraRadius);
            Assert.IsTrue(GameEffectFx.HasRow(aura.TypeId, aura.EffectLevel), $"sent at level {aura.EffectLevel}");
        }

        #region Fixture

        private static void Learn(Manifestation player, int skillId, int pump) =>
            player.Skills[(SkillId)skillId] = new SkillsData((SkillId)skillId, 0, pump);

        private static void Wear(Client client, EquipmentData slot, int skillId)
        {
            var template = new ItemTemplate(new ItemTemplateItemClassEntry { ItemTemplateId = ArmorClass, ItemClass = ArmorClass })
            {
                EquipableInfo = new EquipableInfo(skillId, 1)
            };
            var item = new Item { ItemTemplate = template, ItemTemplateId = ArmorClass, StackSize = 1, CurrentHitPoints = 100 };
            EntityManager.Instance.RegisterEntity(item.EntityId, EntityType.Item);
            EntityManager.Instance.RegisterItem(item.EntityId, item);
            client.Player.Inventory.EquippedInventory[(int)slot] = item.EntityId;
        }

        private static List<PythonPacket> Calls(Client client) =>
            WorldTestContext.Drain(client).Select(packet => packet.Message).OfType<CallMethodMessage>()
                .Select(message => message.Packet).ToList();

        private static Client Watch(WorldTestContext world)
        {
            var client = world.CreateClient(x: 10);
            var seed = CellManager.Instance.GetCellSeed(client.Player.Position);
            client.Player.Cells = CellManager.Instance.CreateCellMatrix(world.Map, seed & 0xFFFF, seed >> 16);
            CellManager.Instance.GetCell(world.Map, seed & 0xFFFF, seed >> 16).ClientList.Add(client);
            client.Player.State = CharacterState.Normal;
            client.Player.Inventory.EquippedInventory = Enumerable.Repeat(0UL, 22).ToList();
            foreach (var attribute in new[] { Attributes.Health, Attributes.Armor, Attributes.Power, Attributes.Regen })
                client.Player.Attributes[attribute] = new ActorAttributes(attribute, 100, 100, 100, 0, 0);
            WorldTestContext.Drain(client);
            return client;
        }

        private static Creature Spawn(WorldTestContext world)
        {
            var creature = new Creature
            {
                Name = "Fixture",
                TargetCategory = TargetCategory.Hostile,
                MapContextId = world.Map.MapInfo.MapContextId,
                RuntimeMapChannel = world.Map,
                Position = new Vector3(14, 0, 0),
                EntityClass = EntityClasses.HumanBaseMale,
                State = CharacterState.Idle,
                Level = 1,
                AppearanceData = new Dictionary<EquipmentData, AppearanceData>()
            };
            creature.Attributes[Attributes.Health] = new ActorAttributes(Attributes.Health, 10000, 10000, 10000, 0, 0);
            creature.Attributes[Attributes.Armor] = new ActorAttributes(Attributes.Armor, 0, 0, 0, 0, 0);
            EntityManager.Instance.RegisterEntity(creature.EntityId, EntityType.Creature);
            EntityManager.Instance.RegisterCreature(creature);
            EntityManager.Instance.RegisterActor(creature.EntityId, creature);
            var seed = CellManager.Instance.GetCellSeed(creature.Position);
            creature.Cells = CellManager.Instance.CreateCellMatrix(world.Map, seed & 0xFFFF, seed >> 16);
            CellManager.Instance.GetCell(world.Map, seed & 0xFFFF, seed >> 16).CreatureList.Add(creature);
            return creature;
        }

        #endregion
    }
}
