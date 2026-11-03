using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Game;
using Rasa.Managers;
using Rasa.Packets.MapChannel.Client;
using Rasa.Packets.MapChannel.Server;
using Rasa.Structures;
using Rasa.Structures.World;
using Rasa.Test.Missions;

namespace Rasa.Test.Missions.Wilderness
{
    /// <summary>
    /// Cache of the Day's fuel containers (665) take damage from every weapon and every damage
    /// ability, not only from what is aimed at one. A launcher's splash, a grenade's blast and an
    /// ability's area went to the creatures and players there alone (AbilityManager.VictimsWithin),
    /// a rocket that hit a container splashed nothing at all, and only the recruit's lightning
    /// and a grenade could be aimed at one: Force Blast, Rushing Blow and the rest were refused
    /// as having no target. They now find the objects there too (PracticeTargetManager.TargetsWithin,
    /// Blast), and may be aimed at an object a weapon can destroy (AbilityManager.ResolvePracticeTarget).
    ///
    /// The containers, 100 hit points each: fuel-1..4 along z 76 at x -269, -265, -261, -257;
    /// fuel-5..8 along z 98; fuel-9 at (-267, 81) and fuel-10 at (-259, 81).
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class CacheOfTheDayObjectDamageTests
    {
        private const uint CacheOfTheDay = 665;
        private const float Ground = 170.3f;

        /// <summary>A grenade (CONSUMABLE_GRENADE 420 at level 1: EFFECT_RADIUS 15, 100-300).</summary>
        private const uint GrenadeTemplateId = 45125;

        /// <summary>Six metres short of fuel-1 and fuel-2, between them, facing them (+z, rotation pi).</summary>
        private static readonly Vector3 Stand = new(-267f, Ground, 70f);

        private Func<int, bool> _toHit;

        [TestInitialize]
        public void Initialize()
        {
            _toHit = ToHit.Roll;
            ToHit.Roll = _ => true;
        }

        [TestCleanup]
        public void Cleanup() => ToHit.Roll = _toHit;

        #region Weapons

        /// <summary>A rocket at fuel-1 splashes fuel-2, four metres off, and the creature beside it; fuel-9, 5.4 m off, is out of a 5 m splash.</summary>
        [TestMethod]
        public void ARocketAtAContainerSplashesTheContainersAndCreaturesAroundIt()
        {
            using var harness = Create();
            var fuel = Fuel(harness);
            var beside = Spawn(harness, new Vector3(-270f, Ground, 76f));

            Launch(harness, fuel["fuel-1"].EntityId, 80, splashRadius: 5);

            Assert.IsTrue(fuel["fuel-1"].CurrentHitPoints < 100, "the container it hit");
            Assert.AreEqual(100U - (uint)Splash.DamageOf(80), fuel["fuel-2"].CurrentHitPoints, "splashed");
            Assert.IsTrue(beside.Attributes[Attributes.Health].Current < 5000, "the creature beside it, splashed");

            foreach (var role in new[] { "fuel-3", "fuel-9", "fuel-10" })
                Assert.AreEqual(100U, fuel[role].CurrentHitPoints, role);
        }

        /// <summary>A rocket at a creature between fuel-1 and fuel-2 splashes both, two metres off; fuel-9, five, is out of a 3 m splash.</summary>
        [TestMethod]
        public void ARocketAtACreatureSplashesTheContainersAroundIt()
        {
            using var harness = Create();
            var fuel = Fuel(harness);
            var creature = Spawn(harness, new Vector3(-267f, Ground, 76f));

            Launch(harness, creature.EntityId, 80, splashRadius: 3);

            Assert.IsTrue(creature.Attributes[Attributes.Health].Current < 5000, "the creature it hit");

            foreach (var role in new[] { "fuel-1", "fuel-2" })
                Assert.AreEqual(100U - (uint)Splash.DamageOf(80), fuel[role].CurrentHitPoints, role);

            foreach (var role in new[] { "fuel-3", "fuel-9" })
                Assert.AreEqual(100U, fuel[role].CurrentHitPoints, role);
        }

        /// <summary>The splash destroys them, the mission counts each, and a destroyed one is splashed no more.</summary>
        [TestMethod]
        public void ASplashDestroysTheContainersAndTheMissionCountsEach()
        {
            using var harness = Create();
            var fuel = Fuel(harness);

            Launch(harness, fuel["fuel-1"].EntityId, 1000, splashRadius: 5);

            foreach (var role in new[] { "fuel-1", "fuel-2" })
                Assert.AreEqual(UseObjectState.StateDestroyed, fuel[role].StateId, role);

            Assert.AreEqual(2U, Counter(harness));

            // Aimed at the wreck, the rocket goes off there again: nothing more is counted.
            Launch(harness, fuel["fuel-1"].EntityId, 1000, splashRadius: 5);
            Assert.AreEqual(2U, Counter(harness));
        }

        /// <summary>A propellant gun's cone burns every container in it, the one locked on to or not.</summary>
        [TestMethod]
        public void AFlamethrowerBurnsTheContainersInItsCone()
        {
            using var harness = Create();
            var fuel = Fuel(harness);
            harness.Client.SetWorldPosition(Stand, Math.PI);
            CellManager.Instance.UpdateVisibility(harness.Client);
            harness.Client.Player.Target = 0;

            try
            {
                ConstantFire.Pulse(harness.Map, harness.Client, Weapon(), new ActionData(harness.Client.Player, ActionId.WeaponFlamethrower, 1, 0), 40, DamageType.Fire, 0);
            }
            finally
            {
                ConstantFire.Stop(harness.Client, release: false);
            }

            foreach (var role in new[] { "fuel-1", "fuel-2" })
                Assert.AreEqual(60U, fuel[role].CurrentHitPoints, role);

            foreach (var role in new[] { "fuel-5", "fuel-10" })
                Assert.AreEqual(100U, fuel[role].CurrentHitPoints, $"{role}, out of reach");
        }

        #endregion

        #region Abilities

        /// <summary>Force Blast was refused at a container as having no target; it destroys one now.</summary>
        [TestMethod]
        public void ForceBlastAtAContainerDestroysIt()
        {
            using var harness = Create();
            var fuel = Fuel(harness);

            Perform(harness, ActionId.AaCommandoForceBlast, 1, Stand, targetId: fuel["fuel-1"].EntityId);

            Assert.AreEqual(UseObjectState.StateDestroyed, fuel["fuel-1"].StateId);
            Assert.AreEqual(1U, Counter(harness));
            Assert.AreEqual(100U, fuel["fuel-2"].CurrentHitPoints, "a single target's");
        }

        /// <summary>Rushing Blow charges a container as it does a creature, and hits it (90-120 at P1).</summary>
        [TestMethod]
        public void RushingBlowChargesAContainerAndHitsIt()
        {
            using var harness = Create();
            var fuel = Fuel(harness);

            Perform(harness, ActionId.AaCommandoRushingBlow, 1, Stand, targetId: fuel["fuel-1"].EntityId);

            Assert.IsTrue(fuel["fuel-1"].CurrentHitPoints <= 10, "90 or more taken off its 100");
            Assert.IsTrue(Vector3.Distance(harness.Client.Player.Position, fuel["fuel-1"].Position) < 3f, "carried to it");
        }

        /// <summary>The recruit's lightning destroyed a container before; at P2 its arc now jumps from it to the creature beside it.</summary>
        [TestMethod]
        public void LightningAtAContainerDestroysItAndArcsToTheCreatureBesideIt()
        {
            using var harness = Create();
            var fuel = Fuel(harness);
            var beside = Spawn(harness, new Vector3(-270f, Ground, 78f));

            Perform(harness, ActionId.AaRecruitLightning, 2, Stand, targetId: fuel["fuel-1"].EntityId);

            Assert.AreEqual(UseObjectState.StateDestroyed, fuel["fuel-1"].StateId);
            Assert.AreEqual(1U, Counter(harness));
            Assert.IsTrue(beside.Attributes[Attributes.Health].Current < 5000, "arced to");
        }

        /// <summary>Tectonic Strike's cone (CONE_RADIUS 10 degrees): fuel-9 straight ahead; fuel-1 and fuel-2, 18 degrees off, are not in it.</summary>
        [TestMethod]
        public void TectonicStrikesConeDestroysTheContainersInIt()
        {
            using var harness = Create();
            var fuel = Fuel(harness);

            Perform(harness, ActionId.AaGrenadierTectonicStrike, 1, Stand, reagent: 45108);

            Assert.AreEqual(UseObjectState.StateDestroyed, fuel["fuel-9"].StateId);
            Assert.AreEqual(1U, Counter(harness));

            foreach (var role in new[] { "fuel-1", "fuel-2" })
                Assert.AreEqual(100U, fuel[role].CurrentHitPoints, role);
        }

        /// <summary>Shrapnel's 6 m around the performer: fuel-1 and fuel-2, 2.8 m off; fuel-9 and fuel-3, 7 and 6.3 m, are not in it.</summary>
        [TestMethod]
        public void ShrapnelDestroysTheContainersAroundThePerformer()
        {
            using var harness = Create();
            var fuel = Fuel(harness);

            Perform(harness, ActionId.AaSoldierShrapnel, 1, new Vector3(-267f, Ground, 74f), reagent: 1765);

            foreach (var role in new[] { "fuel-1", "fuel-2" })
                Assert.AreEqual(UseObjectState.StateDestroyed, fuel[role].StateId, role);

            Assert.AreEqual(2U, Counter(harness));

            foreach (var role in new[] { "fuel-3", "fuel-9" })
                Assert.AreEqual(100U, fuel[role].CurrentHitPoints, role);
        }

        /// <summary>A grenade at fuel-1 destroys every container within its 15 m: fuel-1..4, 9 and 10. The row at z 98, 22 m off, stands.</summary>
        [TestMethod]
        public void AGrenadeAtAContainerDestroysTheContainersAroundIt()
        {
            using var harness = Create();
            var fuel = Fuel(harness);

            Perform(harness, ActionId.ConsumableGrenade, 1, Stand, targetId: fuel["fuel-1"].EntityId, grenade: true);

            foreach (var role in new[] { "fuel-1", "fuel-2", "fuel-3", "fuel-4", "fuel-9", "fuel-10" })
                Assert.AreEqual(UseObjectState.StateDestroyed, fuel[role].StateId, role);

            Assert.AreEqual(6U, Counter(harness));

            foreach (var role in new[] { "fuel-5", "fuel-6", "fuel-7", "fuel-8" })
                Assert.AreEqual(100U, fuel[role].CurrentHitPoints, role);
        }

        /// <summary>A grenade at a creature among them takes the containers around it as well.</summary>
        [TestMethod]
        public void AGrenadeAtACreatureDestroysTheContainersAroundIt()
        {
            using var harness = Create();
            var fuel = Fuel(harness);
            var creature = Spawn(harness, new Vector3(-267f, Ground, 76f));

            Perform(harness, ActionId.ConsumableGrenade, 1, Stand, targetId: creature.EntityId, grenade: true);

            Assert.IsTrue(creature.Attributes[Attributes.Health].Current < 5000);

            foreach (var role in new[] { "fuel-1", "fuel-2", "fuel-9" })
                Assert.AreEqual(UseObjectState.StateDestroyed, fuel[role].StateId, role);
        }

        #endregion

        #region What the client is shown

        /// <summary>
        /// Vortex flails each target its recovery lists (VortexAction.OnAbility, TransitionTo, an
        /// actor's), so the containers it caught are not listed there: their damage is floated
        /// through an ABILITY_DAMAGE_EFFECT put on the performer for the moment it takes.
        /// </summary>
        [TestMethod]
        public void VortexsContainersAreShownTheirDamageOutsideTheRecovery()
        {
            using var harness = Create();
            var fuel = Fuel(harness);

            var sent = Perform(harness, ActionId.AaGuardianVortex, 1, new Vector3(-267f, Ground, 74f));

            var caught = new[] { fuel["fuel-1"], fuel["fuel-2"] };

            foreach (var obj in caught)
                Assert.IsTrue(obj.CurrentHitPoints < 100, obj.SceneActorRole);

            var recovery = sent.OfType<AbilityRecoveryPacket>().Single();
            Assert.IsFalse(recovery.Hits.Any(hit => caught.Any(obj => obj.EntityId == hit.EntityId)), "not in the recovery");

            var attached = sent.OfType<GameEffectAttachedPacket>().Single(packet => packet.EffectTypeId == PracticeTargetManager.AnnouncerTypeId);
            var announced = sent.OfType<GameEffectAnnounceDamagePacket>().Single(packet => packet.EffectId == attached.EffectId);
            var detached = sent.OfType<GameEffectDetachedPacket>().Single(packet => packet.EffectId == attached.EffectId);

            Assert.AreEqual(harness.Client.Player.EntityId, attached.SourceId);
            Assert.IsFalse(attached.Announced, "nothing floated for the effect itself");
            CollectionAssert.AreEquivalent(caught.Select(obj => obj.EntityId).ToList(), announced.Hits.Select(hit => hit.EntityId).ToList());

            foreach (var hit in announced.Hits)
                Assert.AreEqual(100 - (int)caught.Single(obj => obj.EntityId == hit.EntityId).CurrentHitPoints, Math.Min(100, hit.Amount), "what it took");

            Assert.IsTrue(sent.IndexOf(attached) < sent.IndexOf(announced) && sent.IndexOf(announced) < sent.IndexOf(detached), "on, announced, off");
        }

        /// <summary>A bolt at a creature arcs (P2: ARC_RADIUS 12, ARC_DAMAGE 210) to the nearest of what is around it - here a container 1.6 m off.</summary>
        [TestMethod]
        public void LightningArcsFromACreatureToTheNearestContainer()
        {
            using var harness = Create();
            var fuel = Fuel(harness);
            var creature = Spawn(harness, new Vector3(-269.5f, Ground, 74.5f));
            var other = Spawn(harness, new Vector3(-269.5f, Ground, 70f));

            var sent = Perform(harness, ActionId.AaRecruitLightning, 2, Stand, targetId: creature.EntityId);

            var hit = sent.OfType<AbilityRecoveryPacket>().Single().Hits.Single(entry => entry.EntityId == creature.EntityId);
            Assert.AreEqual(fuel["fuel-1"].EntityId, hit.Arcs.Single(arc => arc.DamageType == DamageType.Electrical).EntityId, "drawn and floated on it");
            Assert.AreEqual(UseObjectState.StateDestroyed, fuel["fuel-1"].StateId);
            Assert.AreEqual(1U, Counter(harness));
            Assert.AreEqual(5000, (int)other.Attributes[Attributes.Health].Current, "one arc, to the nearest");
        }

        /// <summary>A bolt at a container arcs to the container beside it when that is nearest.</summary>
        [TestMethod]
        public void LightningArcsFromAContainerToTheNextContainer()
        {
            using var harness = Create();
            var fuel = Fuel(harness);

            var sent = Perform(harness, ActionId.AaRecruitLightning, 2, Stand, targetId: fuel["fuel-9"].EntityId);

            // fuel-9 (-267, 81): fuel-1 and fuel-2 5.4 m off, fuel-10 8 m.
            var hit = sent.OfType<AbilityRecoveryPacket>().Single().Hits.Single(entry => entry.EntityId == fuel["fuel-9"].EntityId);
            var arcedTo = hit.Arcs.Single().EntityId;

            CollectionAssert.Contains(new[] { fuel["fuel-1"].EntityId, fuel["fuel-2"].EntityId }, arcedTo);
            Assert.AreEqual(UseObjectState.StateDestroyed, fuel["fuel-9"].StateId);
            Assert.AreEqual(UseObjectState.StateDestroyed, fuel.Values.Single(obj => obj.EntityId == arcedTo).StateId);
            Assert.AreEqual(2U, Counter(harness));
        }

        #endregion

        #region The blast

        /// <summary>
        /// What every blast of a player's - the explosions, bombs, pools and storms - does to the
        /// objects in it (PracticeTargetManager.Blast): each within the radius but the one excepted
        /// takes its own roll, and one already destroyed is not there to take another.
        /// </summary>
        [TestMethod]
        public void ABlastDamagesEachContainerInItsRadiusButTheOneExcepted()
        {
            using var harness = Create();
            var fuel = Fuel(harness);
            harness.Client.SetWorldPosition(Stand, Math.PI);
            CellManager.Instance.UpdateVisibility(harness.Client);
            var rolls = 0;

            var hit = PracticeTargetManager.Blast(harness.Map, harness.Client.Player, fuel["fuel-1"].Position, 5f,
                () => 30 + rolls++, except: fuel["fuel-2"]);

            CollectionAssert.AreEquivalent(new[] { fuel["fuel-1"] }, hit.Select(h => h.Target).ToList());
            Assert.AreEqual(70U, fuel["fuel-1"].CurrentHitPoints);
            Assert.AreEqual(100U, fuel["fuel-2"].CurrentHitPoints, "excepted");

            hit = PracticeTargetManager.Blast(harness.Map, harness.Client.Player, fuel["fuel-1"].Position, 5f, () => 100);
            CollectionAssert.AreEquivalent(new[] { fuel["fuel-1"], fuel["fuel-2"] }, hit.Select(h => h.Target).ToList());
            Assert.AreEqual(2U, Counter(harness));

            hit = PracticeTargetManager.Blast(harness.Map, harness.Client.Player, fuel["fuel-1"].Position, 5f, () => 100);
            Assert.IsEmpty(hit, "both destroyed");
        }

        #endregion

        #region Fixture

        private static WildernessRuntimeTestHarness Create()
        {
            var harness = WildernessRuntimeTestHarness.Create();
            harness.SpawnWorld(172);
            var npc = harness.Npc(172);
            harness.MoveTo(npc.Position);
            var npcs = new NpcManager(harness, harness.Manager);
            npcs.RequestNpcConverse(harness.Client, new RequestNPCConversePacket { EntityId = npc.EntityId });
            npcs.AssignNPCMission(harness.Client, new AssignNPCMissionPacket { NpcEntityId = npc.EntityId, MissionId = CacheOfTheDay });
            Assert.AreEqual(MissionState.Active, harness.Client.Player.Missions[CacheOfTheDay].State);
            return harness;
        }

        private static Dictionary<string, DynamicObject> Fuel(WildernessRuntimeTestHarness harness) =>
            harness.Map.DynamicObjects.Where(obj => obj.SceneMissionId == CacheOfTheDay)
                .ToDictionary(obj => obj.SceneActorRole, obj => obj);

        private static uint Counter(WildernessRuntimeTestHarness harness) =>
            harness.Client.Player.Missions[CacheOfTheDay].Objectives[1].Counters[0];

        /// <summary>One launcher round from <see cref="Stand"/> at the target, landed.</summary>
        private static void Launch(WildernessRuntimeTestHarness harness, ulong targetId, int damage, float splashRadius)
        {
            harness.Client.SetWorldPosition(Stand, Math.PI);
            CellManager.Instance.UpdateVisibility(harness.Client);
            harness.Client.Player.Target = targetId;
            MissileManager.Instance.MissileLaunch(harness.Map,
                new ActionData(harness.Client.Player, ActionId.WeaponAttack, 133, targetId, 0), damage, splashRadius: splashRadius);
            MissileManager.Instance.DoWork(harness.Map, 5000);
        }

        /// <summary>
        /// The ability requested from where the player stands, as the client asks for it, and its
        /// recovery resolved: learned, with its logos, the power to pay for it, and - for a
        /// grenade - one in the bag.
        /// </summary>
        private static List<Rasa.Packets.PythonPacket> Perform(WildernessRuntimeTestHarness harness, ActionId actionId, uint level, Vector3 at,
            ulong targetId = 0, bool grenade = false, uint reagent = 0)
        {
            var abilities = harness.LoadAbilities();
            var player = harness.Client.Player;
            harness.Client.SetWorldPosition(at, Math.PI);
            CellManager.Instance.UpdateVisibility(harness.Client);
            player.Target = targetId;

            foreach (var attribute in new[] { Attributes.Power, Attributes.Chi })
                player.Attributes[attribute] = new ActorAttributes(attribute, 10_000, 10_000, 10_000, 0, 0);

            // The grenade it is, or what the ability uses up (action_item_requirement).
            var granted = grenade ? GrenadeTemplateId : reagent;

            if (granted != 0)
                using (var grant = new InventoryManager.InventoryGrant())
                {
                    using (var unit = harness.CreateChar())
                        unit.ExecuteTransaction(() => grant.PlanAndSave(harness.Client,
                            new[] { new InventoryManager.InventoryItemGrant(granted, 1) }, unit));
                    grant.Publish(harness.Client);
                }

            ulong itemId = 0;

            if (grenade)
                itemId = player.Inventory.PersonalInventory.Where(id => id != 0)
                    .Select(EntityManager.Instance.GetItem).Single(item => item.ItemTemplateId == GrenadeTemplateId).EntityId;
            else
            {
                player.Skills[(SkillId)(10_000 + (int)actionId)] = new SkillsData((SkillId)(10_000 + (int)actionId), (int)actionId, (int)level);

                foreach (var logos in AbilityLogos.Of(actionId))
                    if (!player.Logos.Contains(logos))
                        player.Logos.Add(logos);
            }

            lock (Server.Clients)
                Server.Clients.Add(harness.Client);

            try
            {
                abilities.RequestPerformAbility(harness.Client, ToyTests.Request((int)actionId, level, itemId, targetId: targetId));
                var pending = harness.Map.PerformRecovery.SingleOrDefault(entry => entry.ActionId == actionId);
                Assert.IsNotNull(pending, $"{actionId} {level} was refused: " + string.Join(", ",
                    Rasa.Test.World.WorldTestContext.Drain(harness.Client).Select(packet => packet.Message).OfType<Rasa.Packets.Protocol.CallMethodMessage>()
                        .Select(message => message.Packet).OfType<Rasa.Packets.MapChannel.Server.UserActionFailedPacket>().Select(failed => failed.MsgId?.ToString() ?? "none")));
                harness.Map.PerformRecovery.Remove(pending);

                // What the recovery sends the performer, and no more.
                Rasa.Test.World.WorldTestContext.Drain(harness.Client);
                abilities.PerformRecovery(harness.Map, pending);
                return Rasa.Test.World.WorldTestContext.Drain(harness.Client).Select(packet => packet.Message)
                    .OfType<Rasa.Packets.Protocol.CallMethodMessage>().Select(message => message.Packet).ToList();
            }
            finally
            {
                lock (Server.Clients)
                    Server.Clients.Remove(harness.Client);
            }
        }

        private static Item Weapon() => new()
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

        private static Creature Spawn(WildernessRuntimeTestHarness harness, Vector3 position)
        {
            var creature = new Creature
            {
                Name = "Fixture",
                TargetCategory = TargetCategory.Hostile,
                MapContextId = harness.Map.MapInfo.MapContextId,
                RuntimeMapChannel = harness.Map,
                Position = position,
                EntityClass = EntityClasses.HumanBaseMale,
                State = CharacterState.Idle,
                Level = 1,
                AppearanceData = new Dictionary<EquipmentData, AppearanceData>()
            };
            creature.Attributes[Attributes.Health] = new ActorAttributes(Attributes.Health, 5000, 5000, 5000, 0, 0);
            creature.Attributes[Attributes.Armor] = new ActorAttributes(Attributes.Armor, 0, 0, 0, 0, 0);
            EntityManager.Instance.RegisterEntity(creature.EntityId, EntityType.Creature);
            EntityManager.Instance.RegisterCreature(creature);
            EntityManager.Instance.RegisterActor(creature.EntityId, creature);
            var seed = CellManager.Instance.GetCellSeed(creature.Position);
            creature.Cells = CellManager.Instance.CreateCellMatrix(harness.Map, seed & 0xFFFF, seed >> 16);
            CellManager.Instance.GetCell(harness.Map, seed & 0xFFFF, seed >> 16).CreatureList.Add(creature);
            return creature;
        }

        #endregion
    }
}
