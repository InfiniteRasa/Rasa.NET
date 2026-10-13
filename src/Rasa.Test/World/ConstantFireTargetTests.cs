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
    using Rasa.Structures.Char;
    using Rasa.Structures.World;

    // What a constant-fire weapon's pulse may land on: a machine gun, a laser chaingun, the leech
    // gun, the polarity gun - every weapon that fires for as long as the trigger is held
    // (ConstantFire). A missile from any other weapon lands only on what the player may attack:
    // a HOSTILE or NEUTRAL creature, an enemy's creature or an enemy player across a wargame
    // (MissileManager, AbilityManager.IsAttackable).
    [TestClass]
    [DoNotParallelize]
    public class ConstantFireTargetTests
    {
        private const int Full = 10000;

        #region The target's category

        [TestMethod]
        [DataRow(TargetCategory.Friendly, false, DisplayName = "a FRIENDLY creature: a vendor, a mission NPC, a friend's turret")]
        [DataRow(TargetCategory.Neutral, true, DisplayName = "a NEUTRAL creature")]
        [DataRow(TargetCategory.Hostile, true, DisplayName = "a HOSTILE creature")]
        public void APulseLandsOnlyOnACreatureThePlayerMayAttack(TargetCategory category, bool hit)
        {
            using var world = new WorldTestContext();
            world.AddClass((EntityClasses)6048);
            var shooter = Watch(world, 0);
            var creature = Spawn(world, new Vector3(0, 0, -5), category);

            Fire(world, shooter, creature, ActionId.WeaponMachinegun, () =>
            {
                var shots = Sent(shooter).OfType<ConstantFireTickPacket>().Single().Pulses.Single();

                Assert.AreEqual(hit, creature.Attributes[Attributes.Health].Current < Full);
                Assert.AreEqual(hit ? 1 : 0, shots.Count(shot => shot.EntityId == creature.EntityId), "and is shown hitting it");
            });
        }

        [TestMethod]
        public void APulseLandsOnAnEnemyPlayerAcrossAWargameAndOnNoOtherPlayer()
        {
            const uint Red = 900051, Blue = 900052;
            using var world = new WorldTestContext();
            var shooter = Watch(world, 0);
            var enemy = Watch(world, 3);
            var stranger = Watch(world, -3);
            shooter.Player.ClanId = Red;
            enemy.Player.ClanId = Blue;

            // No armour to take the pulse first.
            foreach (var target in new[] { enemy, stranger })
                target.Player.Attributes[Attributes.Armor] = new ActorAttributes(Attributes.Armor, 0, 0, 0, 0, 0);
            ClanFeuds.Feud feud = null;

            lock (Server.Clients)
                Server.Clients.AddRange(new[] { shooter, enemy, stranger });

            try
            {
                feud = ClanFeuds.Instance.Start(
                    new ClanEntry { Id = Red, Name = "Red", IsPvP = true },
                    new ClanEntry { Id = Blue, Name = "Blue", IsPvP = true });
                Assert.IsNotNull(feud);

                Fire(world, shooter, stranger.Player, ActionId.WeaponMachinegun, () =>
                    Assert.AreEqual(100, stranger.Player.Attributes[Attributes.Health].Current, "nobody's enemy"));

                Fire(world, shooter, enemy.Player, ActionId.WeaponMachinegun, () =>
                    Assert.IsTrue(enemy.Player.Attributes[Attributes.Health].Current < 100, "the other clan's, at feud"));
            }
            finally
            {
                if (feud != null)
                    ClanFeuds.Instance.End(feud, ClanFeuds.Outcome.Cancelled);

                lock (Server.Clients)
                    foreach (var client in new[] { shooter, enemy, stranger })
                        Server.Clients.Remove(client);
            }
        }

        [TestMethod]
        public void APolarityChargeIsNotLetGoIntoACreatureThatIsNoLongerOneToAttack()
        {
            using var world = new WorldTestContext();
            world.AddClass((EntityClasses)6048);
            var shooter = Watch(world, 0);
            var creature = Spawn(world, new Vector3(0, 0, -5), TargetCategory.Hostile);

            Fire(world, shooter, creature, ActionId.WeaponPolaritygun, () =>
            {
                Assert.IsTrue(creature.Attributes[Attributes.Health].Current < Full, "charged by the beam");

                // A creature that changed sides while the beam was on it.
                creature.TargetCategory = TargetCategory.Friendly;
                var before = creature.Attributes[Attributes.Health].Current;

                ConstantFire.Stop(shooter);

                Assert.AreEqual(before, creature.Attributes[Attributes.Health].Current, "nothing let go into it");
            }, pulses: 3);
        }

        [TestMethod]
        public void APolarityChargeIsLetGoIntoACreatureStillToAttack()
        {
            using var world = new WorldTestContext();
            world.AddClass((EntityClasses)6048);
            var shooter = Watch(world, 0);
            var creature = Spawn(world, new Vector3(0, 0, -5), TargetCategory.Hostile);

            Fire(world, shooter, creature, ActionId.WeaponPolaritygun, () =>
            {
                var before = creature.Attributes[Attributes.Health].Current;

                ConstantFire.Stop(shooter);

                Assert.IsTrue(creature.Attributes[Attributes.Health].Current < before, "the charge let go");
            }, pulses: 3);
        }

        #endregion

        #region Where the target is

        [TestMethod]
        public void ACreatureInAnotherInstanceOfTheMapIsNotHit()
        {
            using var world = new WorldTestContext();
            var shooter = Watch(world, 0);

            // The same map in another channel: a squad's private instance of it.
            var instance = new MapChannel { MapInfo = world.Map.MapInfo, ClientList = new List<Client>(), PlayerLimit = 128 };
            var creature = Spawn(world, new Vector3(0, 0, -5), TargetCategory.Hostile, instance);

            Fire(world, shooter, creature, ActionId.WeaponMachinegun, () =>
                Assert.AreEqual(Full, creature.Attributes[Attributes.Health].Current));
        }

        [TestMethod]
        [DataRow(120f, true, DisplayName = "120 m: in reach")]
        [DataRow(500f, false, DisplayName = "500 m: out of reach")]
        public void APulseReachesAsFarAsAMissile(float distance, bool hit)
        {
            using var world = new WorldTestContext();
            var shooter = Watch(world, 0);
            var creature = Spawn(world, new Vector3(0, 0, -distance), TargetCategory.Hostile);

            Assert.AreEqual(128f, MissileManager.MaxTargetDistance);
            Fire(world, shooter, creature, ActionId.WeaponMachinegun, () =>
                Assert.AreEqual(hit, creature.Attributes[Attributes.Health].Current < Full));
        }

        [TestMethod]
        public void APolarityChargeIsNotLetGoIntoATargetThatHasGoneOutOfReach()
        {
            using var world = new WorldTestContext();
            var shooter = Watch(world, 0);
            var creature = Spawn(world, new Vector3(0, 0, -5), TargetCategory.Hostile);

            Fire(world, shooter, creature, ActionId.WeaponPolaritygun, () =>
            {
                creature.Position = new Vector3(0, 0, -500);
                var before = creature.Attributes[Attributes.Health].Current;

                ConstantFire.Stop(shooter);

                Assert.AreEqual(before, creature.Attributes[Attributes.Health].Current);
            }, pulses: 3);
        }

        [TestMethod]
        public void APolarityChargeIsNotLetGoIntoATargetInAnotherInstance()
        {
            using var world = new WorldTestContext();
            var shooter = Watch(world, 0);
            var creature = Spawn(world, new Vector3(0, 0, -5), TargetCategory.Hostile);

            Fire(world, shooter, creature, ActionId.WeaponPolaritygun, () =>
            {
                // Taken into a private instance of the map while the beam was on it.
                foreach (var cell in world.Map.MapCellInfo.Cells.Values)
                    cell.CreatureList.Remove(creature);
                creature.RuntimeMapChannel = new MapChannel { MapInfo = world.Map.MapInfo, ClientList = new List<Client>(), PlayerLimit = 128 };
                var before = creature.Attributes[Attributes.Health].Current;

                ConstantFire.Stop(shooter);

                Assert.AreEqual(before, creature.Attributes[Attributes.Health].Current);
            }, pulses: 3);
        }

        #endregion

        #region Fixture

        /// <summary>Holds the trigger on <paramref name="target"/> for some pulses, then checks, then lets go without a release.</summary>
        private static void Fire(WorldTestContext world, Client shooter, Actor target, ActionId actionId, Action check, int pulses = 1) =>
            Fire(world, shooter, target.EntityId, actionId, check, pulses);

        /// <summary>The same at whatever entity the shooter has selected: a Personal Waypoint, a Hortimonculus.</summary>
        internal static void Fire(WorldTestContext world, Client shooter, ulong targetId, ActionId actionId, Action check, int pulses = 1)
        {
            world.AddClass((EntityClasses)6048);
            var weapon = new Item
            {
                ItemTemplate = new ItemTemplate(new ItemTemplateItemClassEntry { ItemTemplateId = 145, ItemClass = 6048 })
                {
                    WeaponInfo = new WeaponInfo(new ItemTemplateWeaponEntry
                    {
                        Id = 145, AmmoPerShot = 1, Refire = 200, ReloadTime = 1500,
                        Windup = 0, Recovery = 1, Range = 80, ToolType = 15, AttackType = 2
                    })
                },
                ItemTemplateId = 145, StackSize = 1, Crafter = ""
            };
            var action = new ActionData(shooter.Player, actionId, 1, 0) { TargetId = targetId };
            shooter.Player.Target = targetId;

            try
            {
                for (var i = 0; i < pulses; i++)
                    ConstantFire.Pulse(world.Map, shooter, weapon, action, 40, DamageType.Physical, 0);

                check();
            }
            finally
            {
                ConstantFire.Stop(shooter, release: false);
            }
        }

        private static List<PythonPacket> Sent(Client client) =>
            WorldTestContext.Drain(client)
                .Select(packet => packet.Message)
                .OfType<CallMethodMessage>()
                .Select(message => message.Packet)
                .ToList();

        /// <summary>A creature of the category with ten thousand health and no armour, in the map's cells.</summary>
        internal static Creature Spawn(WorldTestContext world, Vector3 position, TargetCategory category, MapChannel channel = null)
        {
            var creature = new Creature
            {
                Name = "Fixture",
                TargetCategory = category,
                MapContextId = world.Map.MapInfo.MapContextId,
                RuntimeMapChannel = channel ?? world.Map,
                Position = position,
                EntityClass = EntityClasses.HumanBaseMale,
                State = CharacterState.Idle,
                Level = 1,
                AppearanceData = new Dictionary<EquipmentData, AppearanceData>()
            };
            creature.Attributes[Attributes.Health] = new ActorAttributes(Attributes.Health, Full, Full, Full, 0, 0);
            creature.Attributes[Attributes.Armor] = new ActorAttributes(Attributes.Armor, 0, 0, 0, 0, 0);
            creature.Controller.CurrentAction = BehaviorManager.BehaviorActionWander;
            EntityManager.Instance.RegisterEntity(creature.EntityId, EntityType.Creature);
            EntityManager.Instance.RegisterCreature(creature);
            EntityManager.Instance.RegisterActor(creature.EntityId, creature);

            if (channel == null)
            {
                var seed = CellManager.Instance.GetCellSeed(creature.Position);
                creature.Cells = CellManager.Instance.CreateCellMatrix(world.Map, seed & 0xFFFF, seed >> 16);
                CellManager.Instance.GetCell(world.Map, seed & 0xFFFF, seed >> 16).CreatureList.Add(creature);
            }

            return creature;
        }

        /// <summary>A client standing in the map's cells, so what is sent near it reaches it.</summary>
        internal static Client Watch(WorldTestContext world, float x)
        {
            var client = world.CreateClient(x: x);
            var seed = CellManager.Instance.GetCellSeed(client.Player.Position);
            client.Player.Cells = CellManager.Instance.CreateCellMatrix(world.Map, seed & 0xFFFF, seed >> 16);
            CellManager.Instance.GetCell(world.Map, seed & 0xFFFF, seed >> 16).ClientList.Add(client);
            client.Player.State = CharacterState.Normal;
            foreach (var attribute in new[] { Attributes.Health, Attributes.Armor, Attributes.Power, Attributes.Regen })
                client.Player.Attributes[attribute] = new ActorAttributes(attribute, 100, 100, 100, 0, 0);
            WorldTestContext.Drain(client);
            return client;
        }

        #endregion
    }
}
