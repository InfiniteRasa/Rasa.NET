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
    using Rasa.Packets.MapChannel.Server;
    using Rasa.Packets.Protocol;
    using Rasa.Repositories.UnitOfWork;
    using Rasa.Structures;
    using Rasa.Structures.Char;
    using Rasa.Test.Missions;

    // A Hortimonculus attacked (AbilityManager.Hortimonculus): an enemy of its owner's across a
    // wargame may shoot it down, and a creature fights it by its hate table, which the plant's
    // healing fills.
    [TestClass]
    [DoNotParallelize]
    public class HortimonculusAttackTests
    {
        private const uint RedClan = 900041;
        private const uint BlueClan = 900042;

        [TestMethod]
        public void AnEnemysShotLandsOnItsHitPointsAndNobodyElsesDoes()
        {
            using var f = new Fixture();
            var mate = f.Fighter(RedClan, 2, 0);
            var enemy = f.Fighter(BlueClan, 20, 0);
            var stranger = f.Fighter(0, 22, 0);

            f.WithFeud(() =>
            {
                var plant = f.Grow();
                var max = AbilityManager.PlantHealthOf(plant.EntityId);
                f.DrainAll();

                Assert.AreEqual(1000, max, "the fallen's health, at the first pump");
                Assert.IsTrue(AbilityManager.PlantMayBeAttackedBy(enemy.Player, plant.EntityId));
                Assert.IsFalse(AbilityManager.PlantMayBeAttackedBy(f.Owner.Player, plant.EntityId));
                Assert.IsFalse(AbilityManager.PlantMayBeAttackedBy(mate.Player, plant.EntityId));
                Assert.IsFalse(AbilityManager.PlantMayBeAttackedBy(stranger.Player, plant.EntityId));
                Assert.IsFalse(AbilityManager.PlantMayBeAttackedBy(enemy.Player, f.Owner.Player.EntityId), "a player is no plant");

                // What it took is what the hit shows, and everyone around is told what is left.
                Assert.AreEqual(300, f.Shoot(enemy.Player, plant, 300).DamageA);
                Assert.AreEqual(max - 300, AbilityManager.PlantHealthOf(plant.EntityId));
                Assert.AreEqual(max - 300, Told<UpdateHitPointsPacket>(f.Owner, plant).Single().CurrentHitPoints);

                foreach (var harmless in new[] { f.Owner, mate, stranger })
                    Assert.AreEqual(0, f.Shoot(harmless.Player, plant, 300).DamageA);

                Assert.AreEqual(max - 300, AbilityManager.PlantHealthOf(plant.EntityId));

                // A shot is let go at it only by who may harm it.
                var before = f.World.Map.QueuedMissiles.Count;

                MissileManager.Instance.MissileLaunch(f.World.Map, new ActionData(stranger.Player, ActionId.WeaponAttack, 1, plant.EntityId, 0), 55);
                Assert.AreEqual(before, f.World.Map.QueuedMissiles.Count);

                MissileManager.Instance.MissileLaunch(f.World.Map, new ActionData(enemy.Player, ActionId.WeaponAttack, 1, plant.EntityId, 0), 55);
                Assert.AreEqual(before + 1, f.World.Map.QueuedMissiles.Count);
                Assert.AreEqual(plant.EntityId, f.World.Map.QueuedMissiles.Last().TargetEntityId);
                f.World.Map.QueuedMissiles.Clear();

                // A constant-fire weapon's pulse.
                Assert.AreEqual(120, AbilityManager.PlantTakeDamage(enemy.Player, plant.EntityId, 120));
                Assert.IsNull(AbilityManager.PlantTakeDamage(stranger.Player, plant.EntityId, 120));
                Assert.IsNull(AbilityManager.PlantTakeDamage(enemy.Player, f.Owner.Player.EntityId, 120));
                Assert.AreEqual(max - 420, AbilityManager.PlantHealthOf(plant.EntityId));
            });
        }

        [TestMethod]
        public void AConstantFireWeaponReachesItAsFarAsAMissile()
        {
            using var f = new Fixture();
            var enemy = f.Fighter(BlueClan, 20, 0);

            f.WithFeud(() =>
            {
                var plant = f.Grow();
                var max = AbilityManager.PlantHealthOf(plant.EntityId);

                f.Move(enemy, new Vector3(300, 0, 0));
                ConstantFireTargetTests.Fire(f.World, enemy, plant.EntityId, ActionId.WeaponMachinegun, () =>
                    Assert.AreEqual(max, AbilityManager.PlantHealthOf(plant.EntityId), "300 m away"));

                f.Move(enemy, new Vector3(20, 0, 0));
                ConstantFireTargetTests.Fire(f.World, enemy, plant.EntityId, ActionId.WeaponMachinegun, () =>
                    Assert.IsTrue(AbilityManager.PlantHealthOf(plant.EntityId) < max, "20 m away"));
            });
        }

        [TestMethod]
        public void AnAttackOnItEndsTheAttackersSafety()
        {
            using var f = new Fixture();
            var enemy = f.Fighter(BlueClan, 20, 0);

            f.WithFeud(() =>
            {
                var plant = f.Grow();

                enemy.Player.ActiveEffects[900] = new GameEffect { EffectId = 900, TypeId = Pvp.SafetyTypeId };
                Assert.IsTrue(Pvp.IsSafe(enemy.Player));

                f.Shoot(enemy.Player, plant, 10);

                Assert.IsFalse(Pvp.IsSafe(enemy.Player));
            });
        }

        [TestMethod]
        public void AtNoHitPointsItDiesAsWhenItHasDecayed()
        {
            using var f = new Fixture();
            var enemy = f.Fighter(BlueClan, 20, 0);

            f.WithFeud(() =>
            {
                var plant = f.Grow();
                var max = AbilityManager.PlantHealthOf(plant.EntityId);
                f.DrainAll();

                Assert.AreEqual(max - 1, f.Shoot(enemy.Player, plant, max - 1).DamageA);
                Assert.AreEqual(UseObjectState.StatePowerUp, plant.StateId);

                // More than it has left: it takes what it has.
                Assert.AreEqual(1, f.Shoot(enemy.Player, plant, 500).DamageA);

                Assert.AreEqual(0, AbilityManager.PlantHealthOf(plant.EntityId));
                Assert.AreEqual(UseObjectState.StateDestroyed, plant.StateId);
                var told = Told<object>(f.Owner, plant).ToList();
                Assert.AreEqual(0, told.OfType<UpdateHitPointsPacket>().Last().CurrentHitPoints);
                Assert.AreEqual(UseObjectState.StateDestroyed, told.OfType<UsePacket>().Single().CurState, "its death plays");

                // Dying, it is hit by nobody - but it is still there to be walked away from.
                Assert.IsFalse(AbilityManager.PlantMayBeAttackedBy(enemy.Player, plant.EntityId));
                Assert.AreEqual(0, f.Shoot(enemy.Player, plant, 10).DamageA);
                Assert.IsTrue(AbilityManager.TryGetPlantPosition(plant.EntityId, out var position));
                Assert.AreEqual(plant.Position, position);
            });
        }

        [TestMethod]
        public void ItsHealingDrawsTheHateOfThoseThatHateTheHealedToItAndNotItsOwner()
        {
            using var f = new Fixture();
            var mate = f.Fighter(RedClan, 2, 0);

            f.WithFeud(() =>
            {
                var plant = f.Grow();
                f.Party(f.Owner, mate);
                f.Owner.Player.Attributes[Attributes.Health].Current = 500;
                mate.Player.Attributes[Attributes.Health].Current = 500;

                var angry = f.Creature(TargetCategory.Hostile, 10, 0);
                var calm = f.Creature(TargetCategory.Hostile, 12, 0);
                angry.Hate.Ensure(mate.Player.EntityId, 50);

                f.Tick(plant);

                Assert.IsGreaterThan(500, mate.Player.Attributes[Attributes.Health].Current, "it healed");
                Assert.IsGreaterThan(0, angry.Hate.Of(plant.EntityId), "the plant healed who it hates");
                Assert.IsFalse(angry.Hate.Contains(f.Owner.Player.EntityId), "not its owner");
                Assert.IsFalse(calm.Hate.Contains(plant.EntityId), "it hates nobody it healed");

                // A direct use heals as well, and draws the same.
                var drawn = angry.Hate.Of(plant.EntityId);
                mate.Player.Attributes[Attributes.Health].Current = 500;
                f.Use(mate, plant);

                Assert.IsGreaterThan(drawn, angry.Hate.Of(plant.EntityId));
            });
        }

        [TestMethod]
        public void ACreatureThatHatesItMostWalksUpToItAndBringsItDown()
        {
            using var f = new Fixture();

            f.WithFeud(() =>
            {
                var plant = f.Grow();
                var bane = f.Creature(TargetCategory.Hostile, 30, 0);
                bane.Actions.Add(new CreatureAction { ActionId = ActionId.WeaponMelee, ActionArgId = 1, RangeMin = 0, RangeMax = 5, MinDamage = 400, MaxDamage = 400, Cooldown = 1000 });
                bane.HomePos.Position = bane.Position;
                bane.RunSpeed = 4;
                bane.WalkSpeed = 2;
                BehaviorManager.StartWandering(bane, false);

                // It is fighting the owner, and has come to hate the plant more.
                bane.Hate.Ensure(f.Owner.Player.EntityId, 10);
                bane.Hate.Ensure(plant.EntityId, 100);
                BehaviorManager.Instance.SetActionFighting(bane, f.Owner.Player.EntityId);
                bane.Controller.ActionFighting.Opened = true;

                f.Think(1);

                Assert.AreEqual(BehaviorManager.BehaviorActionFighting, bane.Controller.CurrentAction);
                Assert.AreEqual(plant.EntityId, bane.Controller.ActionFighting.TargetEntityId, "the most hated");

                for (var tick = 0; tick < 400 && AbilityManager.PlantHealthOf(plant.EntityId) > 0; tick++)
                    f.Think(1);

                Assert.AreEqual(0, AbilityManager.PlantHealthOf(plant.EntityId));
                Assert.AreEqual(UseObjectState.StateDestroyed, plant.StateId);
                Assert.IsLessThan(6f, Vector3.Distance(bane.Position, plant.Position), "it walked up to within its reach of it");

                // Down: it turns to whoever it hates next.
                f.Think(1);
                Assert.AreNotEqual(plant.EntityId, bane.Controller.ActionFighting.TargetEntityId);
            });
        }

        [TestMethod]
        public void WhoMayFightItIsWhoMayFightItsOwnerAndNobodyGoesLookingForIt()
        {
            using var f = new Fixture();
            var enemy = f.Fighter(BlueClan, 60, 0);

            var plant = (DynamicObject)null;

            f.WithFeud(() =>
            {
                plant = f.Grow();

                var bane = f.Creature(TargetCategory.Hostile, 10, 0);
                var neutral = f.Creature(TargetCategory.Neutral, 11, 0);
                var soldier = f.Creature(TargetCategory.Friendly, 12, 0);
                var turret = f.Creature(TargetCategory.Friendly, 13, 0, master: enemy);
                var ours = f.Creature(TargetCategory.Friendly, 14, 0, master: f.Owner);

                Assert.IsTrue(BehaviorManager.MayFight(bane, plant.EntityId));
                Assert.IsTrue(Threat.CanFight(bane, plant.EntityId));
                Assert.IsTrue(Threat.CanFight(neutral, plant.EntityId));
                Assert.IsFalse(BehaviorManager.MayFight(soldier, plant.EntityId), "a FRIENDLY creature never fights a FRIENDLY player's");
                Assert.IsTrue(Threat.CanFight(turret, plant.EntityId), "an enemy's, for its master's wargame");
                Assert.IsFalse(Threat.CanFight(ours, plant.EntityId));

                // Its hit lands as a player's does.
                Assert.AreEqual(250, f.Shoot(bane, plant, 250).DamageA);
                Assert.AreEqual(0, f.Shoot(soldier, plant, 250).DamageA);

                // A wandering creature that hates nobody passes it by.
                var passer = f.Creature(TargetCategory.Hostile, 8, 0);
                passer.HomePos.Position = passer.Position;
                BehaviorManager.StartWandering(passer, false);
                passer.LastAgression = 3000;
                f.Move(f.Owner, new Vector3(300, 0, 300));

                f.Think(2);

                Assert.AreNotEqual(plant.EntityId, passer.Controller.ActionFighting.TargetEntityId);
                Assert.IsFalse(passer.Hate.Contains(plant.EntityId));
            });
        }

        private static IEnumerable<T> Told<T>(Client client, DynamicObject obj) =>
            WorldTestContext.Drain(client).Select(packet => packet.Message).OfType<CallMethodMessage>()
                .Where(message => message.EntityId == obj.EntityId).Select(message => message.Packet).OfType<T>().ToList();

        private sealed class Fixture : IDisposable
        {
            private readonly List<Creature> _creatures = new List<Creature>();

            internal WorldTestContext World { get; } = new WorldTestContext();
            internal Client Owner { get; }
            private Client Fallen { get; }

            internal Fixture()
            {
                World.AddClass(AbilityManager.HortimonculusClass);
                Owner = Fighter(RedClan, 0, 0);
                Fallen = Fighter(BlueClan, 6, 0);
            }

            internal Client Fighter(uint clan, float x, float z)
            {
                var client = PlayerDeathTests.Player(World, x, z);
                client.Player.ClanId = clan;
                return client;
            }

            internal void DrainAll()
            {
                foreach (var client in World.Map.ClientList)
                    WorldTestContext.Drain(client);
            }

            internal void Party(params Client[] members)
            {
                foreach (var member in members)
                    member.Player.PartyId = 77;
            }

            internal void Move(Client client, Vector3 position)
            {
                client.SetWorldPosition(position, client.Player.Rotation);
                CellManager.Instance.UpdateVisibility(client);
            }

            /// <summary>The Red clan and the Blue at feud for as long as the body runs.</summary>
            internal void WithFeud(Action body)
            {
                var online = World.Map.ClientList.ToList();

                lock (Server.Clients)
                    Server.Clients.AddRange(online);

                ClanFeuds.Feud feud = null;

                try
                {
                    feud = ClanFeuds.Instance.Start(
                        new ClanEntry { Id = RedClan, Name = "Red", IsPvP = true },
                        new ClanEntry { Id = BlueClan, Name = "Blue", IsPvP = true });
                    Assert.IsNotNull(feud);

                    DrainAll();
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

            /// <summary>The owner grows a plant from the fallen enemy, at the first pump.</summary>
            internal DynamicObject Grow()
            {
                Fallen.Player.Attributes[Attributes.Health].Current = 0;
                Assert.IsTrue(PlayerDeath.AtZero(World.Map, Fallen.Player, null));

                var info = new ActionLevelInfo { ActionId = ActionId.AaExobiologistHortimunculus, Level = 1, MaxRange = 40 };
                Invoke("GrowHortimonculus", World.Map, Owner.Player, info, new ActionData(Owner.Player, ActionId.AaExobiologistHortimunculus, 1, Fallen.Player.EntityId, 0));

                return World.Map.MapCellInfo.Cells.Values.SelectMany(c => c.DynamicObjectList).Distinct()
                    .Single(o => o.DynamicObjectType == DynamicObjectType.Hortimonculus);
            }

            /// <summary>Its interval comes round now.</summary>
            internal void Tick(DynamicObject plant)
            {
                plant.ObjectData.GetType().GetField("NextTickAt").SetValue(plant.ObjectData, 0L);
                Invoke("HortimonculusWorker", World.Map);
            }

            /// <summary>A use of it lands, and its heal runs through.</summary>
            internal void Use(Client client, DynamicObject plant)
            {
                Invoke("UseHortimonculusRecovery", World.Map, new ActionData(client.Player, ActionId.UseObject, 1, 0) { SourceId = plant.EntityId });

                var heal = client.Player.ActiveEffects.Values.Single(e => e.TypeId == AbilityManager.HortimonculusUseTypeId);

                for (var tick = 0; tick < 10 && client.Player.ActiveEffects.ContainsKey(heal.EffectId); tick++)
                {
                    heal.OnTick(World.Map, client.Player, heal);

                    if (tick >= 4)
                        break;
                }
            }

            internal Creature Creature(TargetCategory category, float x, float z, Client master = null)
            {
                var creature = new Creature
                {
                    Name = "Fixture",
                    MasterEntityId = master?.Player.EntityId ?? 0,
                    TargetCategory = category,
                    MapContextId = World.Map.MapInfo.MapContextId,
                    Position = new Vector3(x, 0, z),
                    EntityClass = EntityClasses.HumanBaseMale,
                    State = CharacterState.Idle,
                    Level = 1,
                    AppearanceData = new Dictionary<EquipmentData, AppearanceData>()
                };

                creature.Attributes[Attributes.Health] = new ActorAttributes(Attributes.Health, 500, 500, 500, 0, 0);
                creature.Attributes[Attributes.Armor] = new ActorAttributes(Attributes.Armor, 0, 0, 0, 0, 0);
                CellManager.Instance.AddToWorld(World.Map, creature);
                _creatures.Add(creature);

                DrainAll();

                return creature;
            }

            internal Missile Shoot(Actor source, DynamicObject obj, int damage)
            {
                var missile = new Missile
                {
                    Source = source,
                    TargetEntityId = obj.EntityId,
                    DamageA = damage,
                    DamageType = DamageType.Physical,
                    ActionId = ActionId.WeaponAttack,
                    ActionArgId = 1,
                    CritChance = 0
                };

                MissileManager.Instance.MissileTrigger(World.Map, missile);

                return missile;
            }

            internal void Think(int ticks)
            {
                for (var tick = 0; tick < ticks; tick++)
                {
                    BehaviorManager.Instance.MapChannelThink(World.Map, 250);
                    MissileManager.Instance.DoWork(World.Map, 250);
                }
            }

            public void Dispose()
            {
                foreach (var creature in _creatures)
                    CellManager.Instance.RemoveCreatureFromWorld(World.Map, creature);

                World.Map.QueuedMissiles.Clear();

                // The plants of this map are taken away with it.
                var plants = (IList)typeof(AbilityManager).GetField("Plants", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;
                var remove = typeof(AbilityManager).GetMethod("RemovePlant", BindingFlags.Static | BindingFlags.NonPublic)!;

                foreach (var plant in plants.Cast<object>().ToList())
                    if (plant.GetType().GetField("MapChannel")!.GetValue(plant) == World.Map)
                        remove.Invoke(null, new[] { plant });

                World.Dispose();
            }

            private static object Invoke(string name, params object[] args)
            {
                var method = typeof(AbilityManager).GetMethod(name, BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic)!;
                return method.Invoke(method.IsStatic ? null : Abilities(), args);
            }

            private static AbilityManager Abilities() =>
                (AbilityManager)typeof(AbilityManager)
                    .GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null,
                        new[] { typeof(IGameUnitOfWorkFactory), typeof(MissionApplication) }, null)!
                    .Invoke(new object[] { null, null });
        }
    }
}
