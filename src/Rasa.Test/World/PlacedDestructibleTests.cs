using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Rasa.Context.World;
    using Rasa.Data;
    using Rasa.Game;
    using Rasa.Managers;
    using Rasa.Packets;
    using Rasa.Packets.Communicator.Server;
    using Rasa.Packets.Game.Server;
    using Rasa.Packets.MapChannel.Client;
    using Rasa.Packets.MapChannel.Server;
    using Rasa.Packets.Protocol;
    using Rasa.Repositories.World;
    using Rasa.Services.Preloader;
    using Rasa.Structures;
    using Rasa.Structures.Char;
    using Rasa.Structures.World;
    using Rasa.Test.Database;

    /// <summary>
    /// The destructible world objects the server puts down (PlacedDestructibles): rows of
    /// world_destructible on their map and on each private copy, shot down by players and back
    /// 45 to 60 seconds later; what each kind of class does on the way; the spawners' creatures;
    /// placed tesla coils zapping (TeslaCoils); placed force fields (ForceFields); the GM's
    /// .destructible; and the table.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class PlacedDestructibleTests
    {
        /// <summary>The map of WorldTestContext.</summary>
        private const uint Wilderness = 1220;

        private const EntityClasses Barrel = (EntityClasses)9260;          // UsableInertDestBaneBarrelV01: small
        private const EntityClasses SleepPod = (EntityClasses)20621;       // UsableInertDestBaneSleepPodV01: medium
        private const EntityClasses Crane = (EntityClasses)10193;          // UsableInertDestBaneSupplyCraneV01: large
        private const EntityClasses AttaEggs = (EntityClasses)24996;       // UsableCrSpawnerDestAttaEggClusterV01DONOTUSE: 68
        private const EntityClasses WarnetHive = (EntityClasses)21804;     // UsableCrSpawnerDestWarnetHiveV01: 61
        private const EntityClasses Unnamed = (EntityClasses)21455;        // UsableCrSpawnerDestDELETEDUPE: 68, named for nothing
        private const EntityClasses Coil = (EntityClasses)3899;            // UsableTeslaCoilBaneV01
        private const EntityClasses Plant = (EntityClasses)10000053;       // UsableAbilityHortimonculus: 69
        private const EntityClasses BaneGate = (EntityClasses)9572;        // UsableForceFieldBaneOutpostWallMajorV01
        private const EntityClasses Shrine = (EntityClasses)919;           // UsableShrinePlayer: 13
        private const EntityClasses Dummy = (EntityClasses)29365;          // the Practice Dummy
        private const EntityClasses Rock = (EntityClasses)4321;            // no augmentation at all

        private const uint AttaGrubClass = 29089;
        private const uint WarnetSoldierClass = 6262;

        private long _now;
        private readonly List<(EntityClasses Id, bool Had, EntityClass Was)> _classes = new();
        private readonly List<uint> _creatures = new();
        private readonly List<uint> _pools = new();
        private readonly List<Creature> _registered = new();
        private readonly List<(MapChannel Map, DynamicObject From, uint CreatureId, Manifestation Target)> _hatched = new();

        [TestInitialize]
        public void Start()
        {
            _now = 9_000_000;
            PlacedDestructibles.Now = () => _now;
            PlacedDestructibles.Roll = (min, max) => 50_000;
            TeslaCoils.Now = () => _now;
            TeslaCoils.Roll = (min, max) => 200;

            // What comes out of a spawner: a creature in the world, alive until a test kills it.
            PlacedDestructibles.Hatch = (map, from, creatureId, target) =>
            {
                _hatched.Add((map, from, creatureId, target));
                var creature = new Creature { DbId = creatureId, State = CharacterState.Idle, Position = from.Position };
                EntityManager.Instance.RegisterCreature(creature);
                _registered.Add(creature);
                return creature;
            };

            AddClass(Barrel, "UsableInertDestBaneBarrelV01", true, AugmentationType.InertDestroyable);
            AddClass(SleepPod, "UsableInertDestBaneSleepPodV01", true, AugmentationType.InertDestroyable);
            AddClass(Crane, "UsableInertDestBaneSupplyCraneV01", true, AugmentationType.InertDestroyable);
            AddClass(AttaEggs, "UsableCrSpawnerDestAttaEggClusterV01DONOTUSE", true, AugmentationType.DestroyableCreatureSpawner);
            AddClass(WarnetHive, "UsableCrSpawnerDestWarnetHiveV01", true, AugmentationType.CreatureSpawner);
            AddClass(Unnamed, "UsableCrSpawnerDestDELETEDUPE", true, AugmentationType.DestroyableCreatureSpawner);
            AddClass(Coil, "UsableTeslaCoilBaneV01", true, AugmentationType.TeslaCoil);
            AddClass(Plant, "UsableAbilityHortimonculus", true, AugmentationType.DestroyableStatelessSwitch);
            AddClass(BaneGate, "UsableForceFieldBaneOutpostWallMajorV01", true, AugmentationType.ForceField);
            AddClass(Shrine, "UsableShrinePlayer", true, AugmentationType.Shrine);
            AddClass(Dummy, "UsableStatelessHumPracticeDummyV01", true, AugmentationType.InertDestroyable);
            AddClass(Rock, "PropRock", false);

            // The kinds of creature the spawners are named for: two grubs, a soldier on the map.
            AddCreature(531081, AttaGrubClass);
            AddCreature(531017, AttaGrubClass);
            AddCreature(540005, WarnetSoldierClass);
            AddCreature(540003, WarnetSoldierClass);
        }

        [TestCleanup]
        public void Restore()
        {
            PlacedDestructibles.Load(null);
            PlacedDestructibles.Reset();
            TeslaCoils.Reset();

            foreach (var creature in _registered)
            {
                EntityManager.Instance.UnregisterCreature(creature.EntityId);
                EntityManager.Instance.FreeEntity(creature.EntityId);
            }

            foreach (var id in _creatures)
                CreatureManager.Instance.LoadedCreatures.Remove(id);
            foreach (var id in _pools)
                SpawnPoolManager.Instance.LoadedSpawnPools.Remove(id);

            var classes = EntityClassManager.Instance.LoadedEntityClasses;

            for (var i = _classes.Count - 1; i >= 0; i--)
            {
                if (_classes[i].Had)
                    classes[_classes[i].Id] = _classes[i].Was;
                else
                    classes.Remove(_classes[i].Id);
            }
        }

        private void AddClass(EntityClasses id, string name, bool targetable, params AugmentationType[] augmentations)
        {
            var classes = EntityClassManager.Instance.LoadedEntityClasses;

            _classes.Add((id, classes.TryGetValue(id, out var was), was));
            classes[id] = new EntityClass((uint)id, name, 0, 1, augmentations.ToList(), targetable);
        }

        private void AddCreature(uint id, uint classId)
        {
            CreatureManager.Instance.LoadedCreatures[id] = new Creature { DbId = id, EntityClass = (EntityClasses)classId };
            _creatures.Add(id);
        }

        private void AddPool(uint id, uint map, params uint[] creatures)
        {
            SpawnPoolManager.Instance.LoadedSpawnPools[id] = new SpawnPool
            {
                DbId = id,
                MapContextId = map,
                SpawnSlot = creatures.Select(creature => new SpawnPoolSlot(creature, 1, 1)).ToList()
            };
            _pools.Add(id);
        }

        private static WorldDestructibleEntry Row(uint id, EntityClasses classId, float x, float z, uint map = Wilderness,
            uint hitPoints = 0, uint creatureId = 0, uint count = 0, uint side = 0) => new()
        {
            Id = id, MapContextId = map, ClassId = (uint)classId, PosX = x, PosY = 2, PosZ = z, Rotation = 0.5,
            HitPoints = hitPoints, CreatureId = creatureId, CreatureCount = count, Side = side, Comment = "fixture"
        };

        /// <summary>The rows, put down on the map; everything taken away again when the test is done.</summary>
        private static Placed Put(WorldTestContext world, params WorldDestructibleEntry[] rows)
        {
            PlacedDestructibles.Load(rows);
            PlacedDestructibles.Place(world.Map);
            return new Placed(world.Map);
        }

        private sealed class Placed : IDisposable
        {
            private readonly MapChannel _map;
            public Placed(MapChannel map) => _map = map;
            public void Dispose() => PlacedDestructibles.Forget(_map);
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
            return client;
        }

        private static List<(ulong EntityId, PythonPacket Packet)> Sent(Client client) =>
            WorldTestContext.Drain(client).Select(packet => packet.Message).OfType<CallMethodMessage>()
                .Select(message => (message.EntityId, message.Packet)).ToList();

        private static List<UseObjectState> Uses(List<(ulong EntityId, PythonPacket Packet)> sent, ulong entityId) =>
            sent.Where(entry => entry.EntityId == entityId).Select(entry => entry.Packet).OfType<UsePacket>().Select(use => use.CurState).ToList();

        private static List<UseObjectState> Forced(List<(ulong EntityId, PythonPacket Packet)> sent, ulong entityId) =>
            sent.Where(entry => entry.EntityId == entityId).Select(entry => entry.Packet).OfType<ForceStatePacket>().Select(force => force.State).ToList();

        private static DynamicObject ObjectOf(WorldTestContext world, uint row) => PlacedDestructibles.LiveOf(world.Map, row).Object;

        private static void Shoot(WorldTestContext world, Client shooter, DynamicObject target, int damage)
        {
            Assert.IsTrue(PracticeTargetManager.TryGetTarget(world.Map, target.EntityId, out var found), "a target");
            Assert.AreSame(target, found);
            PracticeTargetManager.RecordHit(world.Map, shooter.Player, target, ActionId.WeaponAttack, damage: damage);
        }

        [TestMethod]
        public void EveryClassWithADestroyedStateHasASizeAndTheHitPointsGoBySize()
        {
            // The client's 133 classes with destroyed content, less the 12 force fields (ForceFields'
            // health), the shrine and the Machina factory base (no destroyables to the client) and
            // the Practice Dummy (PracticeTargetManager's).
            Assert.HasCount(118, PlacedDestructibles.Sizes);
            Assert.IsFalse(PlacedDestructibles.Sizes.ContainsKey((uint)Dummy));

            Assert.AreEqual(300u, PlacedDestructibles.HitPointsOf(new PlacedDestructibles.Placement { ClassId = Barrel, Kind = PlacedDestructibles.Kind.Inert }));
            Assert.AreEqual(600u, PlacedDestructibles.HitPointsOf(new PlacedDestructibles.Placement { ClassId = SleepPod, Kind = PlacedDestructibles.Kind.Inert }));
            Assert.AreEqual(1500u, PlacedDestructibles.HitPointsOf(new PlacedDestructibles.Placement { ClassId = Crane, Kind = PlacedDestructibles.Kind.Inert }));
            Assert.AreEqual(250u, PlacedDestructibles.HitPointsOf(new PlacedDestructibles.Placement { ClassId = Crane, Kind = PlacedDestructibles.Kind.Inert, HitPoints = 250 }), "the row's");
            Assert.AreEqual((uint)ForceFields.DefaultHealth, PlacedDestructibles.HitPointsOf(new PlacedDestructibles.Placement { ClassId = BaneGate, Kind = PlacedDestructibles.Kind.ForceField }));
            Assert.AreEqual(PlacedDestructibles.Size.Medium, PlacedDestructibles.SizeOf((EntityClasses)987654), "one not listed");

            // The .map's own keep the hit points they have there.
            Assert.AreEqual(WorldDestructibles.SmallHitPoints, PlacedDestructibles.HitPointsOf(PlacedDestructibles.Size.Small));
            Assert.AreEqual(WorldDestructibles.LargeHitPoints, PlacedDestructibles.HitPointsOf(PlacedDestructibles.SizeOf((EntityClasses)MapUsables.BaneArticulatedDrill)));
            Assert.AreEqual(WorldDestructibles.LargeHitPoints, PlacedDestructibles.HitPointsOf(PlacedDestructibles.SizeOf((EntityClasses)MapUsables.BaneTeslaCoil)));

            // Both force fields the client marks for deletion have extents, on their meshes' twins.
            Assert.IsNotNull(ForceFields.ClassOf("9473"));
            Assert.IsNotNull(ForceFields.ClassOf("9747"));
        }

        [TestMethod]
        public void ARowIsKeptForEachKindAndLeftOutForAnUnknownClassOrThePracticeDummy()
        {
            var kept = PlacedDestructibles.Load(new[]
            {
                Row(1, Barrel, 0, 0), Row(2, AttaEggs, 0, 0), Row(3, WarnetHive, 0, 0), Row(4, Coil, 0, 0),
                Row(5, Plant, 0, 0), Row(6, BaneGate, 0, 0), Row(7, Shrine, 0, 0),
                Row(8, Dummy, 0, 0), Row(9, Rock, 0, 0), Row(10, (EntityClasses)999999, 0, 0)
            });

            Assert.AreEqual(7, kept);
            CollectionAssert.AreEqual(
                new[]
                {
                    PlacedDestructibles.Kind.Inert, PlacedDestructibles.Kind.DestroyedSpawner, PlacedDestructibles.Kind.Spawner,
                    PlacedDestructibles.Kind.TeslaCoil, PlacedDestructibles.Kind.Switch, PlacedDestructibles.Kind.ForceField,
                    PlacedDestructibles.Kind.Scenery
                },
                PlacedDestructibles.All.Select(placement => placement.Kind).ToArray());

            StringAssert.Contains(PlacedDestructibles.WhyNot(Dummy, out _), "Practice Dummy");
            StringAssert.Contains(PlacedDestructibles.WhyNot(Rock, out _), "no destroyed state");
            StringAssert.Contains(PlacedDestructibles.WhyNot((EntityClasses)999999, out _), "not loaded");
        }

        [TestMethod]
        public void ABarrelIsShownStandingShowsItsDamageBlowsUpAndComesBackAboutAMinuteLater()
        {
            using var world = new WorldTestContext();
            var shooter = Stand(world, new Vector3(15, 2, 20));
            var faraway = Stand(world, new Vector3(900, 2, 900));
            using var placed = Put(world, Row(1, Barrel, 10, 20));

            var barrel = ObjectOf(world, 1);
            var made = Sent(shooter).Select(entry => entry.Packet).OfType<CreatePhysicalEntityPacket>().Single(packet => packet.EntityId == barrel.EntityId);

            Assert.AreEqual(Barrel, made.ClassId);
            Assert.AreEqual(UseObjectState.IdesStateIntact, made.EntityData.OfType<UsableInfoPacket>().Single().CurState);
            Assert.AreEqual(TargetCategory.Object, made.EntityData.OfType<TargetCategoryPacket>().Single().TargetCategory);
            var info = made.EntityData.OfType<DamageInfoPacket>().Single();
            Assert.AreEqual((true, 300u, 300u), (info.CanBeDamaged, info.TotalHitPoints, info.CurrentHitPoints));
            Assert.AreEqual(DynamicObjectType.Destructible, barrel.DynamicObjectType);
            Assert.AreEqual(new Vector3(10, 2, 20), barrel.Position);
            Assert.AreEqual(0.5, barrel.Rotation, 1e-9);
            Assert.IsFalse(world.Map.DynamicObjects.Contains(barrel), "no template object: a private copy places its own");
            Assert.IsFalse(Sent(faraway).Any(entry => entry.Packet is CreatePhysicalEntityPacket made && made.EntityId == barrel.EntityId), "out of range");

            Shoot(world, shooter, barrel, 149);
            var sent = Sent(shooter);
            Assert.AreEqual(151, sent.Select(entry => entry.Packet).OfType<UpdateHitPointsPacket>().Single().CurrentHitPoints);
            Assert.IsEmpty(Uses(sent, barrel.EntityId), "151 of 300: intact");

            Shoot(world, shooter, barrel, 1);
            Assert.AreEqual(UseObjectState.IdesState50pHealth, Uses(Sent(shooter), barrel.EntityId).Single());

            Shoot(world, shooter, barrel, 1000);
            sent = Sent(shooter);
            CollectionAssert.AreEqual(new[] { UseObjectState.IdesState25pHealth, UseObjectState.StateDestroyed }, Uses(sent, barrel.EntityId),
                "through 25% to destroyed: the explosion, then the wreck");
            Assert.IsFalse(sent.Select(entry => entry.Packet).OfType<DamageInfoPacket>().Single().CanBeDamaged);
            Assert.IsFalse(PracticeTargetManager.TryGetTarget(world.Map, barrel.EntityId, out _), "a wreck is no target");
            Assert.AreEqual(_now + 50_000, PlacedDestructibles.LiveOf(world.Map, 1).RespawnAt);

            // Whoever comes along now is shown the wreck.
            var late = Stand(world, new Vector3(12, 2, 22));
            made = Sent(late).Select(entry => entry.Packet).OfType<CreatePhysicalEntityPacket>().Single(packet => packet.EntityId == barrel.EntityId);
            Assert.AreEqual(UseObjectState.StateDestroyed, made.EntityData.OfType<UsableInfoPacket>().Single().CurState);
            Assert.IsFalse(made.EntityData.OfType<DamageInfoPacket>().Single().CanBeDamaged);

            _now += 49_999;
            PlacedDestructibles.Worker(world.Map);
            Assert.IsEmpty(Uses(Sent(shooter), barrel.EntityId));

            _now += 1;
            PlacedDestructibles.Worker(world.Map);
            sent = Sent(shooter);
            Assert.AreEqual(UseObjectState.IdesStateIntact, Uses(sent, barrel.EntityId).Single(), "destroyed to intact");
            info = sent.Select(entry => entry.Packet).OfType<DamageInfoPacket>().Single();
            Assert.AreEqual((true, 300u, 300u), (info.CanBeDamaged, info.TotalHitPoints, info.CurrentHitPoints));
            Assert.IsTrue(PracticeTargetManager.TryGetTarget(world.Map, barrel.EntityId, out _));
            Assert.AreEqual(0, PlacedDestructibles.LiveOf(world.Map, 1).RespawnAt);
        }

        [TestMethod]
        public void OnlyAPlayerCanDamageOneAndNobodyCanUseIt()
        {
            using var world = new WorldTestContext();
            var client = Stand(world, new Vector3(12, 2, 20));
            using var placed = Put(world, Row(1, SleepPod, 10, 20));
            var pod = ObjectOf(world, 1);

            var creature = new Creature { Position = pod.Position };
            creature.Attributes[Attributes.Health] = new ActorAttributes(Attributes.Health, 100, 100, 100, 0, 0);
            PracticeTargetManager.RecordHit(world.Map, creature, pod, ActionId.WeaponAttack, damage: 500);
            Assert.AreEqual(600u, pod.CurrentHitPoints);

            WorldTestContext.Drain(client);
            DynamicObjectManager.Instance.RequestUseObjectPacket(client, new RequestUseObjectPacket { EntityId = pod.EntityId, ActionId = ActionId.UseObject, ActionArgId = 1 });
            Assert.AreEqual(UseObjectState.IdesStateIntact, pod.StateId);
            Assert.IsFalse(Sent(client).Any(entry => entry.Packet is UsePacket));
        }

        [TestMethod]
        public void AShotFiredAtOneLandsOnIt()
        {
            using var world = new WorldTestContext();
            var shooter = Stand(world, new Vector3(10, 2, 30));
            using var placed = Put(world, Row(1, Crane, 10, 20));
            var crane = ObjectOf(world, 1);

            var action = new ActionData(shooter.Player, ActionId.WeaponAttack, 133, 0) { TargetId = crane.EntityId };
            MissileManager.Instance.MissileLaunch(world.Map, action, 100);
            Assert.HasCount(1, world.Map.QueuedMissiles, "taken for an object");
            MissileManager.Instance.DoWork(world.Map, 1000);

            Assert.AreEqual(1400u, crane.CurrentHitPoints);
        }

        [TestMethod]
        public void AnEggClusterThatSpawnsWhenDestroyedLetsOutTheMapsGrubsAfterWhoeverDidIt()
        {
            using var world = new WorldTestContext();
            var shooter = Stand(world, new Vector3(14, 2, 20));

            // The map's pools have the Ashen Desert grub, twice over the Plains': it is the one.
            AddPool(990001, Wilderness, 531017, 531017);
            AddPool(990002, Wilderness, 531081);
            AddPool(990003, 9999, 531081, 531081, 531081);

            using var placed = Put(world, Row(1, AttaEggs, 10, 20));
            var eggs = ObjectOf(world, 1);
            var live = PlacedDestructibles.LiveOf(world.Map, 1);

            Assert.AreEqual((531017u, 3u), (live.CreatureId, live.CreatureCount));
            Assert.AreEqual(UseObjectState.IdesStateIntact, eggs.StateId);

            Shoot(world, shooter, eggs, 599);
            Assert.IsEmpty(_hatched, "nothing while it stands");

            Shoot(world, shooter, eggs, 1);
            Assert.HasCount(3, _hatched);
            Assert.IsTrue(_hatched.All(h => h.CreatureId == 531017 && h.From == eggs && h.Target == shooter.Player && h.Map == world.Map));
            Assert.AreEqual(UseObjectState.StateDestroyed, eggs.StateId);
            CollectionAssert.AreEqual(new[] { UseObjectState.IdesState50pHealth, UseObjectState.IdesState25pHealth, UseObjectState.StateDestroyed },
                Uses(Sent(shooter), eggs.EntityId));

            // Back intact a minute later; what came out stays.
            _now += 50_000;
            PlacedDestructibles.Worker(world.Map);
            Assert.AreEqual(UseObjectState.IdesStateIntact, eggs.StateId);
            Assert.HasCount(3, live.Brood);
            Assert.HasCount(3, _hatched);
        }

        [TestMethod]
        public void ASpawnersCreatureIsTheRowsOrTheMapsOrItsClasssOrNone()
        {
            AddPool(990001, Wilderness, 540003);

            PlacedDestructibles.Load(new[]
            {
                Row(1, WarnetHive, 0, 0),
                Row(2, WarnetHive, 0, 0, map: 9999),
                Row(3, WarnetHive, 0, 0, creatureId: 531081, count: 5),
                Row(4, Unnamed, 0, 0),
                Row(5, Barrel, 0, 0, creatureId: 531081)
            });

            var all = PlacedDestructibles.All;

            Assert.AreEqual(540003u, PlacedDestructibles.CreatureFor(all[0]), "the soldier the map's pools have");
            Assert.AreEqual(540005u, PlacedDestructibles.CreatureFor(all[1]), "the class's own, on a map whose pools have none");
            Assert.AreEqual((531081u, 5u), (PlacedDestructibles.CreatureFor(all[2]), PlacedDestructibles.CountFor(all[2])), "the row's");
            Assert.AreEqual(3u, PlacedDestructibles.CountFor(all[0]));
            Assert.AreEqual(0u, PlacedDestructibles.CreatureFor(all[3]), "named for nothing: it just blows up");
            Assert.AreEqual(0u, PlacedDestructibles.CreatureFor(all[4]), "a barrel spawns nothing");
        }

        [TestMethod]
        public void AHiveSpawnsWhenAPlayerComesNearAndAgainOnlyOnceThoseAreDeadAndAMinuteHasPassed()
        {
            using var world = new WorldTestContext();
            using var placed = Put(world, Row(1, WarnetHive, 10, 20));
            var hive = ObjectOf(world, 1);

            Assert.AreEqual(UseObjectState.CsStateIdle, hive.StateId);

            var watcher = Stand(world, new Vector3(10 + PlacedDestructibles.SpawnerTriggerRadius + 5, 2, 20));
            PlacedDestructibles.Worker(world.Map);
            Assert.AreEqual(UseObjectState.CsStateIdle, hive.StateId, "nobody near");
            WorldTestContext.Drain(watcher);

            var player = Stand(world, new Vector3(20, 2, 25));
            PlacedDestructibles.Worker(world.Map);
            Assert.AreEqual(UseObjectState.CsStateBegin, Uses(Sent(player), hive.EntityId).Single());

            _now += PlacedDestructibles.SpawnerBeginMs;
            PlacedDestructibles.Worker(world.Map);
            Assert.AreEqual(UseObjectState.CsStateSpawn, Uses(Sent(player), hive.EntityId).Single());
            Assert.HasCount(3, _hatched);
            Assert.IsTrue(_hatched.All(h => h.CreatureId == 540005 && h.Target == player.Player));

            _now += PlacedDestructibles.SpawnerSpawnMs;
            PlacedDestructibles.Worker(world.Map);
            Assert.AreEqual(UseObjectState.CsStateEnd, Uses(Sent(player), hive.EntityId).Single());

            _now += PlacedDestructibles.SpawnerEndMs;
            PlacedDestructibles.Worker(world.Map);
            Assert.AreEqual(UseObjectState.CsStateIdle, Uses(Sent(player), hive.EntityId).Single());

            // A minute on, with its soldiers alive: nothing.
            _now += PlacedDestructibles.SpawnerRearmMs;
            PlacedDestructibles.Worker(world.Map);
            PlacedDestructibles.Worker(world.Map);
            Assert.IsEmpty(Uses(Sent(player), hive.EntityId));

            foreach (var creature in _registered)
                creature.State = CharacterState.Dead;

            PlacedDestructibles.Worker(world.Map);
            PlacedDestructibles.Worker(world.Map);
            Assert.AreEqual(UseObjectState.CsStateBegin, Uses(Sent(player), hive.EntityId).Single(), "again");

            // Shot down partway: straight to destroyed, and it comes back idle.
            Shoot(world, player, hive, 600);
            Assert.AreEqual(UseObjectState.StateDestroyed, Uses(Sent(player), hive.EntityId).Single());
            _now += PlacedDestructibles.SpawnerBeginMs;
            PlacedDestructibles.Worker(world.Map);
            Assert.HasCount(3, _hatched, "a hive that is down spawns nothing");

            _now += 50_000;
            PlacedDestructibles.Worker(world.Map);
            Assert.AreEqual(UseObjectState.CsStateIdle, Uses(Sent(player), hive.EntityId).First(), "destroyed to idle");
        }

        [TestMethod]
        public void APlacedCoilZapsWhileItStandsAndGoesStraightToItsWreck()
        {
            using var world = new WorldTestContext();
            using var placed = Put(world, Row(1, Coil, 10, 20));
            var coil = ObjectOf(world, 1);
            var near = Stand(world, coil.Position + new Vector3(6, 0, 0));
            var shooter = Stand(world, coil.Position + new Vector3(30, 0, 0));

            Assert.AreEqual(UseObjectState.StatePowerDown, coil.StateId);

            TeslaCoils.Worker(world.Map);
            Assert.AreEqual(coil.EntityId, TeslaCoils.ZapOn(near.Player)?.SourceId);
            Assert.AreEqual(1800, near.Player.Attributes[Attributes.Health].Current);
            Assert.IsNull(TeslaCoils.ZapOn(shooter.Player));

            WorldTestContext.Drain(shooter);
            Shoot(world, shooter, coil, 600);
            Assert.AreEqual(UseObjectState.StateDestroyed, Uses(Sent(shooter), coil.EntityId).Single(), "171 to destroyed");

            _now += TeslaCoils.LeaveGraceMs + TeslaCoils.IntervalMs;
            TeslaCoils.Worker(world.Map);
            Assert.IsNull(TeslaCoils.ZapOn(near.Player), "one shot down zaps nobody");
            Assert.AreEqual(1800, near.Player.Attributes[Attributes.Health].Current);

            _now += 50_000;
            PlacedDestructibles.Worker(world.Map);
            Assert.AreEqual(UseObjectState.StatePowerDown, Uses(Sent(shooter), coil.EntityId).Single(), "destroyed to 171");
            TeslaCoils.Worker(world.Map);
            Assert.IsNotNull(TeslaCoils.ZapOn(near.Player));
        }

        [TestMethod]
        public void AHortimonculusIsDestroyedGrownAndComesBackAsASproutThatGrows()
        {
            using var world = new WorldTestContext();
            var shooter = Stand(world, new Vector3(14, 2, 20));
            using var placed = Put(world, Row(1, Plant, 10, 20));
            var plant = ObjectOf(world, 1);

            Assert.AreEqual(UseObjectState.StatePowerUp, plant.StateId);
            WorldTestContext.Drain(shooter);

            Shoot(world, shooter, plant, 600);
            Assert.AreEqual(UseObjectState.StateDestroyed, Uses(Sent(shooter), plant.EntityId).Single(), "172 to destroyed");

            _now += 50_000;
            PlacedDestructibles.Worker(world.Map);
            var sent = Sent(shooter);
            Assert.AreEqual(UseObjectState.StatePowerDown, Forced(sent, plant.EntityId).Single(), "no way from destroyed: forced");
            Assert.AreEqual(UseObjectState.StatePowerUp, Uses(sent, plant.EntityId).Single(), "and it grows");
        }

        [TestMethod]
        public void AForceFieldRowIsTheBanesAndComesBackRepaired()
        {
            using var world = new WorldTestContext();
            using var placed = Put(world, Row(1, BaneGate, 10, 20), Row(2, BaneGate, 60, 20, side: WorldDestructibleEntry.SideAfs, hitPoints: 900));

            var bane = PlacedDestructibles.LiveOf(world.Map, 1);
            var afs = PlacedDestructibles.LiveOf(world.Map, 2);

            Assert.AreEqual((ForceFields.Side.B, ForceFields.DefaultHealth), (bane.Field.Side, bane.Field.MaxHealth));
            Assert.AreEqual((ForceFields.Side.A, 900), (afs.Field.Side, afs.Field.MaxHealth));
            Assert.AreSame(bane.Field.Object, bane.Object);
            Assert.HasCount(2, ForceFields.OnMap(world.Map));
            Assert.IsFalse(PlacedDestructibles.TryGetTarget(world.Map, bane.Object.EntityId, out _), "ForceFields has its hits");

            ForceFields.Damage(bane.Field, ForceFields.DefaultHealth, 0);
            Assert.IsTrue(bane.IsDown);

            PlacedDestructibles.Worker(world.Map);
            Assert.AreEqual(_now + 50_000, bane.RespawnAt);

            _now += 50_000;
            PlacedDestructibles.Worker(world.Map);
            Assert.IsFalse(bane.IsDown);
            Assert.AreEqual(ForceFields.DefaultHealth, bane.Field.Health);
            Assert.AreEqual(UseObjectState.FfStateFactionBIntact, bane.Object.StateId);

            // The channel going away takes its fields out of ForceFields.
            Assert.AreEqual(2, PlacedDestructibles.Forget(world.Map));
            Assert.IsEmpty(ForceFields.OnMap(world.Map));
        }

        [TestMethod]
        public void AShrineIsSceneryInItsOneStateAndNoTarget()
        {
            using var world = new WorldTestContext();
            var client = Stand(world, new Vector3(14, 2, 20));
            using var placed = Put(world, Row(1, Shrine, 10, 20));
            var shrine = ObjectOf(world, 1);

            Assert.AreEqual(DynamicObjectType.Scenery, shrine.DynamicObjectType);
            var made = Sent(client).Select(entry => entry.Packet).OfType<CreatePhysicalEntityPacket>().Single(packet => packet.EntityId == shrine.EntityId);
            Assert.AreEqual(UseObjectState.ShrineState0, made.EntityData.OfType<UsableInfoPacket>().Single().CurState);
            Assert.IsFalse(made.EntityData.OfType<IsTargetablePacket>().Single().IsTargetable);
            Assert.IsFalse(PracticeTargetManager.TryGetTarget(world.Map, shrine.EntityId, out _));
            Assert.IsFalse(PlacedDestructibles.LiveOf(world.Map, 1).IsDown);
        }

        [TestMethod]
        public void EachPrivateCopyOfAMapHasItsOwnAndTheOpenWorldsAreLeftAlone()
        {
            using var world = new WorldTestContext();
            using var copy = new WorldTestContext();
            var shooter = Stand(world, new Vector3(14, 2, 20));
            using var placed = Put(world, Row(1, Barrel, 10, 20), Row(2, Barrel, 30, 20, map: 9999));

            Assert.HasCount(1, PlacedDestructibles.On(world.Map), "the other map's row is not this one's");
            Assert.AreEqual(0, PlacedDestructibles.Place(world.Map), "one the channel has is left alone");

            Assert.AreEqual(1, PlacedDestructibles.Place(copy.Map));
            using var copied = new Placed(copy.Map);

            Assert.AreNotSame(ObjectOf(world, 1), ObjectOf(copy, 1));
            Shoot(world, shooter, ObjectOf(world, 1), 1000);
            Assert.IsTrue(PlacedDestructibles.LiveOf(world.Map, 1).IsDown);
            Assert.IsFalse(PlacedDestructibles.LiveOf(copy.Map, 1).IsDown, "the copy's own");
            Assert.IsFalse(PracticeTargetManager.TryGetTarget(world.Map, ObjectOf(copy, 1).EntityId, out _), "the copy's is not the open world's to hit");

            var goneId = ObjectOf(copy, 1).EntityId;
            Assert.AreEqual(1, PlacedDestructibles.Forget(copy.Map));
            Assert.IsFalse(EntityManager.Instance.TryGetObject(goneId, out _));
        }

        [TestMethod]
        public void AGameMasterListsPutsDownKillsBringsBackAndClears()
        {
            using var world = new WorldTestContext();
            var client = world.CreateClient();
            typeof(Client).GetProperty(nameof(Client.AccountEntry)).SetValue(client, new GameAccountEntry { Level = (byte)GmLevel.Admin });
            client.Player.PlaceAt(new Vector3(30f, 4f, 40f));
            client.Player.Rotation = 1.25f;
            client.Player.State = CharacterState.Normal;
            CellManager.Instance.AddToWorld(client);

            var commands = new ChatCommandsManager(null);
            commands.RegisterChatCommands();

            List<string> Say(string command)
            {
                WorldTestContext.Drain(client);
                commands.ProcessCommand(client, command);
                return WorldTestContext.Drain(client).Select(packet => packet.Message).OfType<CallMethodMessage>()
                    .Select(message => message.Packet).OfType<SystemMessagePacket>().Select(message => message.TextMessage).ToList();
            }

            using var placed = Put(world, Row(7, Barrel, 32, 40));

            var classes = Say(".destructible classes barrel");
            StringAssert.Contains(classes[0], "1 class(es)");
            StringAssert.Contains(classes[1], "9260 UsableInertDestBaneBarrelV01 (Inert, 300 hp)");

            StringAssert.Contains(Say(".destructible put").Single(), "usage: .destructible put");
            StringAssert.Contains(Say(".destructible put 29365").Single(), "Practice Dummy");
            StringAssert.Contains(Say(".destructible put hive 0 123").Single(), "Creature 123 is not in the database");

            var put = Say(".destructible put UsableCrSpawnerDestWarnetHiveV01 900 540003 2");
            StringAssert.Contains(put[0], "put down: 21804 UsableCrSpawnerDestWarnetHiveV01 (Spawner), 900/900 hp");
            StringAssert.Contains(put[0], "spawns 2 x creature 540003");
            Assert.AreEqual("Row(id, 1220, 21804, 30, 4, 40, 1.25, \"UsableCrSpawnerDestWarnetHiveV01\", 900, 540003, 2, 0)", put[1]);

            var hive = PlacedDestructibles.On(world.Map).Single(live => live.Placement.IsPutDown);
            Assert.AreEqual(new Vector3(30, 4, 40), hive.Object.Position);

            StringAssert.Contains(Say(".destructible near")[0], "put down: 21804");
            StringAssert.Contains(Say(".destructible kill 7").Single(), "Down: row 7");
            Assert.IsTrue(PlacedDestructibles.LiveOf(world.Map, 7).IsDown);
            StringAssert.Contains(Say(".destructible kill 7").Single(), "down already");
            StringAssert.Contains(Say(".destructible back 7").Single(), "Back: row 7");
            Assert.IsFalse(PlacedDestructibles.LiveOf(world.Map, 7).IsDown);

            StringAssert.Contains(Say(".destructible clear").Single(), "Took away 1 object(s)");
            Assert.HasCount(1, PlacedDestructibles.On(world.Map), "the row's stays");
        }

        [TestMethod]
        public void TheTableIsMadeEmptyAndAPreloadersRowsGoInAndComeOut()
        {
            var directory = Path.Combine(AppContext.BaseDirectory, "TestDatabases", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);

            try
            {
                var database = Path.Combine(directory, "world");

                using (var context = (WorldContext)PersistenceIntegrationTests.CreateContext(typeof(SqliteWorldContext), database))
                {
                    MigratedDatabaseTemplates.Migrate(context, () => context.Database.Migrate());

                    Assert.IsEmpty(new WorldDestructibleRepository(context).Get(), "no rows of its own");
                    Assert.IsFalse(context.Database.HasPendingModelChanges());
                }

                // A later migration's lot, put in and taken out as one would.
                using (var context = (WorldContext)PersistenceIntegrationTests.CreateContext(typeof(SqliteWorldContext), database))
                {
                    var builder = new MigrationBuilder(context.Database.ProviderName);
                    var lot = new FixtureLot();

                    lot.Preload(builder);
                    Run(context, builder);

                    var rows = new WorldDestructibleRepository(context).Get();
                    Assert.HasCount(2, rows);
                    Assert.AreEqual((1u, 1220u, 9260u, 10.5, 2.25, 20.75, 0.5, 0u, 0u, 0u, 0u, "Bane barrel, it's by the gate"),
                        (rows[0].Id, rows[0].MapContextId, rows[0].ClassId, rows[0].PosX, rows[0].PosY, rows[0].PosZ, rows[0].Rotation,
                         rows[0].HitPoints, rows[0].CreatureId, rows[0].CreatureCount, rows[0].Side, rows[0].Comment));
                    Assert.AreEqual((21804u, 900u, 540003u, 2u, WorldDestructibleEntry.SideBane),
                        (rows[1].ClassId, rows[1].HitPoints, rows[1].CreatureId, rows[1].CreatureCount, rows[1].Side));

                    builder = new MigrationBuilder(context.Database.ProviderName);
                    lot.Remove(builder);
                    Run(context, builder);
                    Assert.IsEmpty(new WorldDestructibleRepository(context).Get());
                }

                // Down drops it.
                using (var context = (WorldContext)PersistenceIntegrationTests.CreateContext(typeof(SqliteWorldContext), database))
                {
                    context.GetService<IMigrator>().Migrate("20261212000000_Thunderhead_pool_height");
                    Assert.ThrowsExactly<Microsoft.Data.Sqlite.SqliteException>(() => context.WorldDestructibleEntries.AsNoTracking().ToList());
                }
            }
            finally
            {
                Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
                try { Directory.Delete(directory, true); } catch (IOException) { }
            }
        }

        [TestMethod]
        public void OnMySqlTheTableIsMadeAndDropped()
        {
            using var context = PersistenceIntegrationTests.CreateContext(typeof(MySqlWorldContext), "unused");
            var migrator = context.GetService<IMigrator>();
            var up = migrator.GenerateScript("20261212000000_Thunderhead_pool_height", "20261213000000_Add_world_destructibles");
            var down = migrator.GenerateScript("20261213000000_Add_world_destructibles", "20261212000000_Thunderhead_pool_height");

            StringAssert.Contains(up, "CREATE TABLE `world_destructible`");
            foreach (var column in new[] { "`id` int unsigned NOT NULL AUTO_INCREMENT", "`hit_points` int unsigned NOT NULL", "`side` int unsigned NOT NULL",
                "`pos_x` double NOT NULL", "`comment` varchar(128) NOT NULL", "CONSTRAINT `PK_world_destructible` PRIMARY KEY (`id`)" })
                StringAssert.Contains(up, column);
            StringAssert.Contains(down, "DROP TABLE `world_destructible`");
        }

        private static void Run(WorldContext context, MigrationBuilder builder)
        {
            var sql = context.GetService<IMigrationsSqlGenerator>().Generate(builder.Operations, context.Model);

            foreach (var command in sql)
                context.Database.ExecuteSqlRaw(command.CommandText);
        }

        private sealed class FixtureLot : WorldDestructiblePreloader
        {
            protected override IEnumerable<object[]> GetRows()
            {
                yield return Row(1, 1220, 9260, 10.5, 2.25, 20.75, 0.5, "Bane barrel, it's by the gate");
                yield return Row(2, 1220, 21804, 40, 2, 20, 1, "Warnet hive", hitPoints: 900, creatureId: 540003, creatureCount: 2, side: WorldDestructibleEntry.SideBane);
            }
        }
    }
}
