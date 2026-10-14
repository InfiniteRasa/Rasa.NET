using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Gameplay
{
    using Rasa.Data;
    using Rasa.Game;
    using Rasa.Managers;
    using Rasa.Memory;
    using Rasa.Packets;
    using Rasa.Packets.Game.Server;
    using Rasa.Packets.MapChannel.Server;
    using Rasa.Packets.Protocol;
    using Rasa.Structures;
    using Rasa.Structures.World;
    using Rasa.Test.Missions;
    using Rasa.Test.World;

    // With nothing in hand the client fights with the item in its HANDTOHAND slot - and nothing
    // gave it one, and the server fired nothing with an empty hand, so an unarmed attack did
    // nothing at all. Every player now has fists (Unarmed): on their own client, and on the server.
    [TestClass]
    [DoNotParallelize]
    public class UnarmedTests
    {
        [TestMethod]
        public void AnEmptyHandPunches()
        {
            using var context = Context(out var victim);
            EmptyHand(context);

            var punch = Fire(context);

            Assert.AreEqual(ActionId.WeaponMelee, punch.ActionId);
            Assert.AreEqual(1u, punch.ActionArgId);
            Assert.IsTrue(punch.IsMelee, "a swing: no bead, no to-hit roll, no cover");
            Assert.AreEqual(30, punch.DamageA, "Weapon_Avatar_Unarmed's 30");
            Assert.AreEqual(DamageType.Physical, punch.DamageType);
            Assert.AreSame(victim, punch.TargetActor);
        }

        [TestMethod]
        public void ABrokenWeaponIsPutAsideForFists()
        {
            using var context = Context(out _);
            EntityClassManager.Instance.LoadedEntityClasses[(EntityClasses)6048].ItemClassInfo.MaxHitPoints = 100;
            context.Weapon.CurrentHitPoints = 0;
            var clip = context.Weapon.CurrentAmmo;

            var punch = Fire(context);

            Assert.AreEqual(30, punch.DamageA, "the fists, not the gun's 55");
            Assert.AreEqual(clip, context.Weapon.CurrentAmmo, "and the gun was not fired");
        }

        [TestMethod]
        public void AWholeWeaponIsStillFired()
        {
            using var context = Context(out _);

            var shot = Fire(context);

            Assert.AreEqual(ActionId.WeaponAttack, shot.ActionId);
            Assert.IsFalse(shot.IsMelee);
        }

        [TestMethod]
        [DataRow(0, 0, 0)]
        [DataRow(3, 25, 0)]
        [DataRow(4, 50, 1000)]
        [DataRow(5, 75, 2000)]
        public void HandToHandKnocksBackWithAPunchAsWithASwing(int pump, int chance, int stunMs)
        {
            using var context = Context(out _);
            EmptyHand(context);
            context.Client.Player.Skills[SkillId.HandToHand] = new SkillsData(SkillId.HandToHand, -1, pump);

            var punch = Fire(context);

            Assert.AreEqual(chance, punch.KnockbackChance);
            Assert.AreEqual(stunMs, punch.KnockbackStunMs);
        }

        [TestMethod]
        public void FistsDoNotHeatOrWear()
        {
            using var context = Context(out _);
            EmptyHand(context);
            var fists = Unarmed.For(context.Client.Player);

            Fire(context);

            Assert.AreEqual(0.0, fists.Heat);
            Assert.AreEqual(1, fists.CurrentHitPoints);
        }

        [TestMethod]
        public void PunchesComeNoFasterThanTheSwing()
        {
            using var context = Context(out _);
            EmptyHand(context);
            var manager = new ManifestationManager(context);

            // The shot clock's tolerance lets one through early (ShotTolerance), and no more.
            Assert.IsTrue(manager.PlayerTryFireWeapon(context.Client));
            Assert.IsTrue(manager.PlayerTryFireWeapon(context.Client));
            Assert.IsFalse(manager.PlayerTryFireWeapon(context.Client), "500 ms between punches");
            Assert.AreEqual(Unarmed.DefaultSwingMs, Unarmed.For(context.Client.Player).ItemTemplate.WeaponInfo.Refire);
        }

        [TestMethod]
        public void AnEmptyHandIsDrawnAsFistsAndHeldFireKeepsPunching()
        {
            using var context = Context(out _);
            EmptyHand(context);
            context.Client.Player.WeaponReady = false;
            var manager = new ManifestationManager(context);

            manager.RequestWeaponDraw(context.Client);

            var draw = context.World.Map.PerformRecovery.Single(action => action.ActionId == ActionId.WeaponDraw);
            Assert.AreEqual(8u, draw.ActionArgId, "Weapon_Avatar_Unarmed's draw");
            Assert.IsTrue(context.Client.Player.WeaponReady);

            Assert.IsTrue(manager.RegisterAutoFire(context.Client), "held fire has fists to fire");
        }

        [TestMethod]
        public void HoldingFireWithAnEmptyHandKeepsPunching()
        {
            using var context = Context(out _);
            EmptyHand(context);
            var manager = new ManifestationManager(context);

            manager.StartAutoFire(context.Client, 0);

            Assert.IsTrue(context.Client.Player.AutoFireCombatMode);
            Assert.AreEqual(ActionId.WeaponMelee, context.World.Map.QueuedMissiles.Single().ActionId);
        }

        [TestMethod]
        public void TheirOwnClientIsGivenTheFistsAndTheirSlot()
        {
            using var context = Context(out _);
            var player = context.Client.Player;
            WorldTestContext.Drain(context.Client);

            // Arriving on a map: the fists, then the manifestation whose equipment names them.
            new ManifestationManager(context).CellIntroduceClientToSefl(context.Client);

            var fists = Unarmed.For(player);
            var sent = WorldTestContext.Drain(context.Client).Select(packet => packet.Message).OfType<CallMethodMessage>().ToList();
            var created = sent.Select(message => message.Packet).OfType<CreatePhysicalEntityPacket>().ToList();
            Assert.AreEqual(2, created.Count);
            Assert.AreEqual(fists.EntityId, created[0].EntityId);
            Assert.AreEqual(player.EntityId, created[1].EntityId);
            Assert.IsTrue(sent.Any(message => message.EntityId == fists.EntityId && message.Packet is WeaponInfoPacket), "a weapon, as the client reads one");
            Assert.IsTrue(sent.Any(message => message.EntityId == fists.EntityId && message.Packet is ItemInfoPacket));

            var own = new ManifestationManager(context).CreatePlayerEntityData(context.Client, context.Client).OfType<EquipmentInfoPacket>().Single();
            CollectionAssert.Contains(Slots(own), ((long)EquipmentData.HandToHand, (long)fists.EntityId));

            var other = context.World.CreateClient(x: 5);
            var theirs = new ManifestationManager(context).CreatePlayerEntityData(context.Client, other).OfType<EquipmentInfoPacket>().Single();
            Assert.IsFalse(Slots(theirs).Any(slot => slot.Slot == (long)EquipmentData.HandToHand), "nobody else's client holds them");

            Assert.AreSame(fists, Unarmed.For(player), "one pair for good");
        }

        [TestMethod]
        public void TheEquipmentResentAfterAPolymorphKeepsTheFists()
        {
            using var context = Context(out _);
            var player = context.Client.Player;
            var fists = Unarmed.For(player);
            player.MorphWeapon = new MorphWeaponItem();
            WorldTestContext.Drain(context.Client);

            typeof(AbilityManager).GetMethod("EndPolymorph", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
                .Invoke(null, new object[] { context.World.Map, player, null });

            var equipment = WorldTestContext.Drain(context.Client).Select(packet => packet.Message).OfType<CallMethodMessage>()
                .Select(message => message.Packet).OfType<EquipmentInfoPacket>().Single();
            CollectionAssert.Contains(Slots(equipment), ((long)EquipmentData.HandToHand, (long)fists.EntityId),
                "an EquipmentInfo replaces all the client holds equipped");
        }

        #region Fixture

        private static WeaponAmmoContext Context(out Creature victim)
        {
            var context = new WeaponAmmoContext(clip: 20);
            context.World.AddClass(Unarmed.ClassId);

            var fists = EntityClassManager.Instance.LoadedEntityClasses[Unarmed.ClassId];
            fists.WeaponClassInfo = new WeaponClassInfo(new WeaponClassEntry
            {
                // weaponclass 6082.
                Id = 6082, WeaponTemplatId = 8, AttackActionId = 174, AttackActionArgId = 1,
                DrawActionId = 8, StowActionId = 8, ReloadActionId = 0, AmmoClassId = 0,
                ClipSize = 0, MinDamage = 30, MaxDamage = 30, DamageType = 1, WeaponAnimConditionCode = 13
            });
            fists.EquipableClassInfo = new EquipableClassInfo(EquipmentData.HandToHand);
            fists.ItemClassInfo = new ItemClassInfo(new ItemClassEntry { StackSize = 1, MaxHitPoints = 0 });

            victim = Spawn(context.World, new Vector3(0, 0, -2));
            context.Client.Player.Target = victim.EntityId;

            return context;
        }

        private static void EmptyHand(WeaponAmmoContext context)
        {
            context.Client.Player.Inventory.EquippedInventory[13] = 0;
        }

        private static Missile Fire(WeaponAmmoContext context)
        {
            Assert.IsTrue(new ManifestationManager(context).PlayerTryFireWeapon(context.Client), "fired");

            return context.World.Map.QueuedMissiles.Single();
        }

        /// <summary>(slot, entity) for every entry an EquipmentInfo writes.</summary>
        private static List<(long Slot, long Entity)> Slots(EquipmentInfoPacket packet)
        {
            var reader = new PythonReader(new BinaryReader(new MemoryStream(MissionTestContext.Encode(packet))));
            Assert.AreEqual(1, reader.ReadTuple());

            var slots = new List<(long, long)>();
            var count = reader.ReadList();

            for (var i = 0; i < count; i++)
            {
                Assert.AreEqual(2, reader.ReadTuple());
                slots.Add((Number(reader), Number(reader)));
            }

            return slots;
        }

        private static long Number(PythonReader reader) =>
            reader.PeekType() == PythonType.Long ? reader.ReadLong() : reader.ReadInt();

        private static Creature Spawn(WorldTestContext world, Vector3 position)
        {
            var creature = new Creature
            {
                Name = "Fixture",
                TargetCategory = TargetCategory.Hostile,
                MapContextId = world.Map.MapInfo.MapContextId,
                RuntimeMapChannel = world.Map,
                Position = position,
                EntityClass = EntityClasses.HumanBaseMale,
                State = CharacterState.Idle,
                Level = 1,
                AppearanceData = new Dictionary<EquipmentData, AppearanceData>()
            };
            creature.Attributes[Attributes.Health] = new ActorAttributes(Attributes.Health, 5000, 5000, 5000, 0, 0);
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
