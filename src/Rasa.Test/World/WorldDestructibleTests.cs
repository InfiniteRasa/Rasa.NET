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

    /// <summary>
    /// The .map's dissection tables, stasis chambers, articulated drills and tesla coils can be
    /// shot down by players and come back (WorldDestructibles), and the coils zap players near
    /// them while they stand (TeslaCoils).
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class WorldDestructibleTests
    {
        private const ulong Table = 134419591466928;        // Penal Research dissection table 1
        private const ulong Coil = 132770324036699;         // Concordia Divide tesla coil 1
        private const ulong Drill = 132770324750504;        // Concordia Divide articulated drill

        private long _now;
        private readonly List<EntityClasses> _added = new();

        [TestInitialize]
        public void Start()
        {
            _now = 7_000_000;
            WorldDestructibles.Now = () => _now;
            WorldDestructibles.Roll = (min, max) => 50_000;
            TeslaCoils.Now = () => _now;
            TeslaCoils.Roll = (min, max) => 200;

            // The classes as the client's data has them: their augmentations decide their states.
            AddClass(MapUsables.BrannDissectionTable, AugmentationType.InertDestroyable);
            AddClass(MapUsables.BaneArticulatedDrill, AugmentationType.InertDestroyable);
            AddClass(MapUsables.BaneStasisChamber, AugmentationType.InertDestroyable);
            AddClass(MapUsables.BaneTeslaCoil, AugmentationType.TeslaCoil);
        }

        [TestCleanup]
        public void Restore()
        {
            WorldDestructibles.Reset();
            TeslaCoils.Reset();
            foreach (var id in _added)
                EntityClassManager.Instance.LoadedEntityClasses.Remove(id);
        }

        private void AddClass(uint classId, AugmentationType augmentation)
        {
            var classes = EntityClassManager.Instance.LoadedEntityClasses;
            if (classes.ContainsKey((EntityClasses)classId))
                return;
            classes.Add((EntityClasses)classId, new EntityClass(classId, "fixture", 0, 0, new List<AugmentationType> { augmentation }, true));
            _added.Add((EntityClasses)classId);
        }

        private static WorldTestContext On(uint mapContextId, string name)
        {
            var world = new WorldTestContext();
            world.Map.MapInfo = new MapInfo(mapContextId, name, 1, 0);
            WorldDestructibles.Forget(world.Map);
            TeslaCoils.Forget(world.Map);
            return world;
        }

        /// <summary>A living player at a place, in the map's cells.</summary>
        private static Client Stand(WorldTestContext world, Vector3 at, int health = 2000)
        {
            var client = world.CreateClient(at.X, at.Z);
            client.Player.Position = at;
            client.Player.State = CharacterState.Normal;
            client.Player.Attributes[Attributes.Health] = new ActorAttributes(Attributes.Health, health, health, health, 0, 0);
            client.Player.Attributes[Attributes.Armor] = new ActorAttributes(Attributes.Armor, 0, 0, 0, 0, 0);
            CellManager.Instance.AddToWorld(client);
            WorldTestContext.Drain(client);
            return client;
        }

        private static List<(ulong EntityId, PythonPacket Packet)> Sent(Client client) =>
            WorldTestContext.Drain(client).Select(packet => packet.Message).OfType<CallMethodMessage>()
                .Select(message => (message.EntityId, message.Packet)).ToList();

        private static List<UseObjectState> Uses(List<(ulong EntityId, PythonPacket Packet)> sent, ulong entityId) =>
            sent.Where(entry => entry.EntityId == entityId).Select(entry => entry.Packet).OfType<UsePacket>().Select(use => use.CurState).ToList();

        private static void Shoot(WorldTestContext world, Client shooter, ulong entityId, int damage)
        {
            Assert.IsTrue(PracticeTargetManager.TryGetTarget(world.Map, entityId, out var target), "a target");
            PracticeTargetManager.RecordHit(world.Map, shooter.Player, target, ActionId.WeaponAttack, damage: damage);
        }

        [TestMethod]
        public void TheTargetableDestructiblesHaveHitPointsBySizeAndTheUntargetableAreLeftOut()
        {
            Assert.HasCount(29, WorldDestructibles.All);

            foreach (var (classId, count, hitPoints) in new[]
            {
                (MapUsables.BrannDissectionTable, 19, WorldDestructibles.SmallHitPoints),
                (MapUsables.BaneStasisChamber, 2, WorldDestructibles.SmallHitPoints),
                (MapUsables.BaneArticulatedDrill, 3, WorldDestructibles.LargeHitPoints),
                (MapUsables.BaneTeslaCoil, 5, WorldDestructibles.LargeHitPoints),
            })
            {
                var some = WorldDestructibles.All.Where(d => d.Usable.ClassId == classId).ToList();
                Assert.HasCount(count, some, classId.ToString());
                Assert.IsTrue(some.All(d => d.HitPoints == hitPoints), classId.ToString());
            }

            // The Bane outpost and Brann generators, the foundries and the sonic towers: untargetable classes.
            foreach (var classId in new uint[] { 3857, 10776, 23091, 21964 })
                Assert.IsFalse(WorldDestructibles.All.Any(d => d.Usable.ClassId == classId), classId.ToString());

            Assert.AreEqual(UseObjectState.StatePowerDown, WorldDestructibles.Find(Coil).UpState, "the coil's arcing is 171's effect");
        }

        [TestMethod]
        public void TheStandInIsTheMapsEntityAndNoObjectOfTheServers()
        {
            using var world = On(MapUsables.PenalResearch, "adv_arieki_torden_plains_penalresearch");

            Assert.IsTrue(PracticeTargetManager.TryGetTarget(world.Map, Table, out var target));
            Assert.IsTrue(PracticeTargetManager.TryGetTarget(world.Map, Table, out var again));
            Assert.AreSame(target, again, "one stand-in per channel");
            Assert.AreEqual(Table, target.EntityId);
            Assert.AreEqual((EntityClasses)MapUsables.BrannDissectionTable, target.EntityClassId);
            Assert.AreEqual(WorldDestructibles.SmallHitPoints, target.CurrentHitPoints);
            Assert.IsTrue(WorldDestructibles.IsOne(target));
            Assert.IsFalse(world.Map.DynamicObjects.Contains(target), "never sent to a client: it has the entity already");
            Assert.AreEqual((EntityType)0, EntityManager.Instance.GetEntityType(Table), "in no registry");

            // Another map's is not this one's.
            Assert.IsFalse(PracticeTargetManager.TryGetTarget(world.Map, Coil, out _));
        }

        [TestMethod]
        public void ArrivingIsToldTheyCanBeDamaged()
        {
            using var world = On(MapUsables.ConcordiaDivide, "adv_foreas_concordia_divide");
            var client = Stand(world, new Vector3(0, 0, 0));

            Assert.AreEqual(6, WorldDestructibles.PlayerEnteredMap(client), "the drill and the five coils");

            var sent = Sent(client);
            foreach (var destructible in WorldDestructibles.OnMap(MapUsables.ConcordiaDivide))
            {
                var info = sent.Where(entry => entry.EntityId == destructible.EntityId).Select(entry => entry.Packet).OfType<DamageInfoPacket>().Single();
                Assert.IsTrue(info.CanBeDamaged, destructible.ToString());
                Assert.AreEqual((destructible.HitPoints, destructible.HitPoints), (info.TotalHitPoints, info.CurrentHitPoints));
                Assert.AreEqual(TargetCategory.Object, sent.Where(entry => entry.EntityId == destructible.EntityId)
                    .Select(entry => entry.Packet).OfType<TargetCategoryPacket>().Single().TargetCategory);
            }
        }

        [TestMethod]
        public void ATableShowsItsDamageBlowsUpAndComesBackAboutAMinuteLater()
        {
            using var world = On(MapUsables.PenalResearch, "adv_arieki_torden_plains_penalresearch");
            var position = WorldDestructibles.Find(Table).Usable.Position;
            var shooter = Stand(world, position + new Vector3(5, 0, 0));
            var faraway = Stand(world, position + new Vector3(400, 0, 0));

            Shoot(world, shooter, Table, 149);
            var sent = Sent(shooter);
            Assert.AreEqual(151, sent.Select(entry => entry.Packet).OfType<UpdateHitPointsPacket>().Single().CurrentHitPoints);
            Assert.IsEmpty(Uses(sent, Table), "151 of 300: intact");

            Shoot(world, shooter, Table, 1);
            Assert.AreEqual(UseObjectState.IdesState50pHealth, Uses(Sent(shooter), Table).Single(), "half");
            Assert.AreEqual(UseObjectState.IdesState50pHealth, Uses(Sent(faraway), Table).Single(), "to everyone on the map");

            Shoot(world, shooter, Table, 150);
            sent = Sent(shooter);
            CollectionAssert.AreEqual(new[] { UseObjectState.IdesState25pHealth, UseObjectState.StateDestroyed }, Uses(sent, Table),
                "through 25% to destroyed: the explosion, then the wreck");
            Assert.IsFalse(sent.Select(entry => entry.Packet).OfType<DamageInfoPacket>().Single().CanBeDamaged);
            CollectionAssert.AreEqual(new[] { UseObjectState.IdesState25pHealth, UseObjectState.StateDestroyed }, Uses(Sent(faraway), Table));
            Assert.IsFalse(PracticeTargetManager.TryGetTarget(world.Map, Table, out _), "a wreck is no target");
            Assert.AreEqual(UseObjectState.StateDestroyed, MapUsables.StateOf(world.Map, MapUsables.Find(Table)), "whoever arrives sees the wreck");
            Assert.AreEqual(_now + 50_000, WorldDestructibles.RespawnAtOf(world.Map, Table));

            // Shown so on arrival.
            var late = Stand(world, position + new Vector3(10, 0, 0));
            MapUsables.PlayerEnteredMap(late);
            WorldDestructibles.PlayerEnteredMap(late);
            sent = Sent(late);
            Assert.AreEqual(UseObjectState.StateDestroyed, sent.Where(entry => entry.EntityId == Table).Select(entry => entry.Packet).OfType<ForceStatePacket>().Single().State);
            Assert.IsFalse(sent.Where(entry => entry.EntityId == Table).Select(entry => entry.Packet).OfType<DamageInfoPacket>().Single().CanBeDamaged);

            _now += 49_999;
            WorldDestructibles.Worker(world.Map);
            Assert.IsEmpty(Uses(Sent(shooter), Table));

            _now += 1;
            WorldDestructibles.Worker(world.Map);
            sent = Sent(faraway);
            Assert.AreEqual(UseObjectState.IdesStateIntact, Uses(sent, Table).Single(), "back");
            var info = sent.Select(entry => entry.Packet).OfType<DamageInfoPacket>().Single();
            Assert.AreEqual((true, 300u, 300u), (info.CanBeDamaged, info.TotalHitPoints, info.CurrentHitPoints));
            Assert.IsTrue(PracticeTargetManager.TryGetTarget(world.Map, Table, out var back));
            Assert.AreEqual(300u, back.CurrentHitPoints);
            Assert.AreEqual(UseObjectState.IdesStateIntact, MapUsables.StateOf(world.Map, MapUsables.Find(Table)));
        }

        [TestMethod]
        public void OnlyAPlayerCanDamageOne()
        {
            using var world = On(MapUsables.ConcordiaDivide, "adv_foreas_concordia_divide");
            Assert.IsTrue(PracticeTargetManager.TryGetTarget(world.Map, Drill, out var target));

            var creature = new Creature { Position = target.Position };
            creature.Attributes[Attributes.Health] = new ActorAttributes(Attributes.Health, 100, 100, 100, 0, 0);
            PracticeTargetManager.RecordHit(world.Map, creature, target, ActionId.WeaponAttack, damage: 500);

            Assert.AreEqual(WorldDestructibles.LargeHitPoints, WorldDestructibles.HitPointsOf(world.Map, Drill));
        }

        [TestMethod]
        public void ACoilGoesStraightToItsWreckAndComesBackArcing()
        {
            using var world = On(MapUsables.ConcordiaDivide, "adv_foreas_concordia_divide");
            var position = WorldDestructibles.Find(Coil).Usable.Position;
            var shooter = Stand(world, position + new Vector3(40, 0, 0));

            Shoot(world, shooter, Coil, 599);
            Assert.IsEmpty(Uses(Sent(shooter), Coil), "a coil has no damaged states");

            Shoot(world, shooter, Coil, 1);
            Assert.AreEqual(UseObjectState.StateDestroyed, Uses(Sent(shooter), Coil).Single(), "171 to destroyed: state_death_arch_bane_teslacoil_v01.pkg");
            Assert.IsFalse(WorldDestructibles.IsUp(world.Map, Coil));

            _now += 50_000;
            WorldDestructibles.Worker(world.Map);
            Assert.AreEqual(UseObjectState.StatePowerDown, Uses(Sent(shooter), Coil).Single(), "destroyed to 171: arcing again");
            Assert.IsTrue(WorldDestructibles.IsUp(world.Map, Coil));
        }

        [TestMethod]
        public void AShotFiredAtOneLandsOnIt()
        {
            using var world = On(MapUsables.ConcordiaDivide, "adv_foreas_concordia_divide");
            var position = WorldDestructibles.Find(Drill).Usable.Position;
            var shooter = Stand(world, position + new Vector3(0, 0, 10));

            var action = new ActionData(shooter.Player, ActionId.WeaponAttack, 133, 0) { TargetId = Drill };
            MissileManager.Instance.MissileLaunch(world.Map, action, 100);
            Assert.HasCount(1, world.Map.QueuedMissiles, "the drill is taken for an object");
            MissileManager.Instance.DoWork(world.Map, 1000);

            Assert.AreEqual(WorldDestructibles.LargeHitPoints - 100, WorldDestructibles.HitPointsOf(world.Map, Drill));
        }

        [TestMethod]
        public void AStandingCoilZapsAPlayerNearItAndOneShotDownZapsNobody()
        {
            using var world = On(MapUsables.ConcordiaDivide, "adv_foreas_concordia_divide");
            var foot = WorldDestructibles.Find(Coil).Usable.Position;
            var near = Stand(world, foot + new Vector3(8, 0, 0));
            var far = Stand(world, foot + new Vector3(TeslaCoils.Radius + 3, 0, 0));

            TeslaCoils.Worker(world.Map);

            var sent = Sent(near);
            var attached = sent.Select(entry => entry.Packet).OfType<GameEffectAttachedPacket>().Single();
            Assert.AreEqual((TeslaCoils.EffectTypeId, 1u, Coil), (attached.EffectTypeId, attached.EffectLevel, attached.SourceId), "TESLA_COIL_ZAP, from the coil");
            var hit = sent.Select(entry => entry.Packet).OfType<GameEffectTickPacket>().Single().Entries.Single();
            Assert.AreEqual((200, DamageType.Electrical), (hit.Amount, hit.DamageType));
            Assert.AreEqual(1800, near.Player.Attributes[Attributes.Health].Current);
            Assert.IsNull(TeslaCoils.ZapOn(far.Player), "out of reach");

            // Once every two seconds.
            _now += 1999;
            TeslaCoils.Worker(world.Map);
            Assert.AreEqual(1800, near.Player.Attributes[Attributes.Health].Current);
            _now += 1;
            TeslaCoils.Worker(world.Map);
            Assert.AreEqual(1600, near.Player.Attributes[Attributes.Health].Current);

            // Shot down, it zaps nobody, and the zap comes off.
            Shoot(world, far, Coil, (int)WorldDestructibles.LargeHitPoints);
            _now += TeslaCoils.LeaveGraceMs;
            TeslaCoils.Worker(world.Map);
            Assert.IsNull(TeslaCoils.ZapOn(near.Player));
            Assert.AreEqual(1600, near.Player.Attributes[Attributes.Health].Current);

            // Back, and zapping again.
            _now += 50_000;
            WorldDestructibles.Worker(world.Map);
            TeslaCoils.Worker(world.Map);
            Assert.IsNotNull(TeslaCoils.ZapOn(near.Player));
            Assert.AreEqual(1400, near.Player.Attributes[Attributes.Health].Current);

            // High above it on a ledge, out of its reach.
            Assert.IsFalse(TeslaCoils.InReach(WorldDestructibles.Find(Coil), foot + new Vector3(0, TeslaCoils.Above + 1, 0)));
        }
    }
}
