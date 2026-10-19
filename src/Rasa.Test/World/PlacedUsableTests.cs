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
    using Rasa.Test.Missions.Wilderness;

    /// <summary>
    /// The usable world objects the server puts down (PlacedUsables): rows of world_usable on their
    /// map and on each private copy; which classes are placed and with what defaults; switches,
    /// doors, item dispensers and acceptors, bombs and random explosions; the GM's .worldusable;
    /// and the table.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class PlacedUsableTests
    {
        private const uint Wilderness = 1220;

        private const EntityClasses Beam = (EntityClasses)4196;         // UsableTwoStateBaneIndustrialCircuitBeam128MV01
        private const EntityClasses Door = (EntityClasses)4412;         // UsableDoorBaneV01
        private const EntityClasses Dispenser = (EntityClasses)7007;    // UsableItemDispCrystalspecimenCONVERT2TD
        private const EntityClasses Acceptor = (EntityClasses)7131;     // UsableItemAccQueenlure
        private const EntityClasses Bomb = (EntityClasses)7870;         // UsableBombHumV01
        private const EntityClasses FallingBomb = (EntityClasses)3913;  // FALLINGBOMB
        private const EntityClasses Geyser = (EntityClasses)8090;       // RandomExplosionAriekiGeyser
        private const EntityClasses ThreeState = (EntityClasses)24670;  // RCH_TestREcordSomeMoreStillWoo
        private const EntityClasses FiveState = (EntityClasses)29042;   // UsableFiveStateBaneDoorAccessLevel
        private const EntityClasses Mechpad = (EntityClasses)30421;     // TEST_KGS_Mechpad
        private const EntityClasses ControlPoint = (EntityClasses)3814; // UsableControlPointElohV01
        private const EntityClasses Barrel = (EntityClasses)9260;       // UsableInertDestBaneBarrelV01
        private const EntityClasses Rock = (EntityClasses)4321;         // no augmentation

        /// <summary>Ammo_Cartridge_1_Standard_Grade: template 28, class 3147, in the seeded world.</summary>
        private const uint Cartridges = 28;
        private const EntityClasses CartridgeClass = (EntityClasses)3147;

        private long _now;
        private readonly List<(EntityClasses Id, bool Had, EntityClass Was)> _classes = new();

        [TestInitialize]
        public void Start()
        {
            _now = 5_000_000;
            PlacedUsables.Now = () => _now;
            PlacedUsables.Roll = (min, max) => min + (max - min) / 2;
        }

        [TestCleanup]
        public void Restore()
        {
            PlacedUsables.Load(null);
            PlacedUsables.Reset();

            var classes = EntityClassManager.Instance.LoadedEntityClasses;

            for (var i = _classes.Count - 1; i >= 0; i--)
            {
                if (_classes[i].Had)
                    classes[_classes[i].Id] = _classes[i].Was;
                else
                    classes.Remove(_classes[i].Id);
            }

            _classes.Clear();
        }

        private void AddClass(EntityClasses id, string name, bool targetable, params AugmentationType[] augmentations)
        {
            var classes = EntityClassManager.Instance.LoadedEntityClasses;

            _classes.Add((id, classes.TryGetValue(id, out var was), was));
            classes[id] = new EntityClass((uint)id, name, 0, 1, augmentations.ToList(), targetable);
        }

        private void AddFixtureClasses()
        {
            AddClass(Beam, "UsableTwoStateBaneIndustrialCircuitBeam128MV01", false, AugmentationType.TwoStateSwitch);
            AddClass(Door, "UsableDoorBaneV01", false, AugmentationType.Door);
            AddClass(Dispenser, "UsableItemDispCrystalspecimenCONVERT2TD", true, AugmentationType.ItemDispenser);
            AddClass(Acceptor, "UsableItemAccQueenlure", true, AugmentationType.ItemAcceptor);
            AddClass(Bomb, "UsableBombHumV01", true, AugmentationType.Bomb);
            AddClass(FallingBomb, "FALLINGBOMB", false, AugmentationType.RandomExplosion);
            AddClass(Geyser, "RandomExplosionAriekiGeyser", false, AugmentationType.RandomExplosion);
            AddClass(ThreeState, "RCH_TestREcordSomeMoreStillWoo", false, AugmentationType.ThreeStateSwitch);
            AddClass(FiveState, "UsableFiveStateBaneDoorAccessLevel", false, AugmentationType.FiveStateSwitch);
            AddClass(Mechpad, "TEST_KGS_Mechpad", false, AugmentationType.MechPad);
            AddClass(ControlPoint, "UsableControlPointElohV01", true, AugmentationType.ControlPoint);
            AddClass(Barrel, "UsableInertDestBaneBarrelV01", true, AugmentationType.InertDestroyable);
            AddClass(Rock, "PropRock", false);
        }

        private static WorldUsableEntry Row(uint id, EntityClasses classId, float x, float z, uint map = Wilderness, uint state = 0, bool usable = false,
            uint itemTemplate = 0, uint itemClass = 0, uint count = 0, uint reset = 0, uint min = 0, uint max = 0, uint damageMin = 0, uint damageMax = 0,
            double radius = 0, uint arg = 0) => new()
        {
            Id = id, MapContextId = map, ClassId = (uint)classId, PosX = x, PosY = 2, PosZ = z, Rotation = 1.5, State = state, Usable = usable ? 1u : 0u,
            ItemTemplateId = itemTemplate, ItemClassId = itemClass, ItemCount = count, ResetSeconds = reset, IntervalMinSeconds = min, IntervalMaxSeconds = max,
            DamageMin = damageMin, DamageMax = damageMax, Radius = radius, ActionArg = arg, Comment = "fixture"
        };

        private sealed class Placed : IDisposable
        {
            private readonly MapChannel _map;
            public Placed(MapChannel map) => _map = map;
            public void Dispose() => PlacedUsables.Forget(_map);
        }

        private static Placed Put(MapChannel map, params WorldUsableEntry[] rows)
        {
            Assert.AreEqual(rows.Length, PlacedUsables.Load(rows), "every row kept");
            PlacedUsables.Place(map);
            return new Placed(map);
        }

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

        private static RequestUseObjectPacket Use(DynamicObject obj) =>
            new RequestUseObjectPacket { ActionId = ActionId.UseObject, ActionArgId = 1, EntityId = obj.EntityId };

        /// <summary>A use, through the server's request handling, run past its windup.</summary>
        private static void UseAndRecover(MapChannel map, Client client, DynamicObject obj)
        {
            DynamicObjectManager.Instance.RequestUseObjectPacket(client, Use(obj));
            ActorActionManager.Instance.DoWork(map, PlacedUsables.UseWindupMs);
        }

        private static DynamicObject ObjectOf(MapChannel map, uint row) => PlacedUsables.LiveOf(map, row).Object;

        [TestMethod]
        public void EachKindIsPlacedWithItsDefaultsAndTheRestAreSentToTheirTables()
        {
            AddFixtureClasses();

            var kept = PlacedUsables.Load(new[]
            {
                Row(1, Beam, 0, 0), Row(2, Door, 0, 0), Row(3, Dispenser, 0, 0, usable: true, itemTemplate: 0),
                Row(4, Acceptor, 0, 0), Row(5, Bomb, 0, 0), Row(6, FallingBomb, 0, 0), Row(7, Geyser, 0, 0),
                Row(8, ThreeState, 0, 0), Row(9, FiveState, 0, 0), Row(10, Mechpad, 0, 0),
                Row(11, ControlPoint, 0, 0), Row(12, Barrel, 0, 0), Row(13, Rock, 0, 0), Row(14, (EntityClasses)999999, 0, 0),
                Row(15, Door, 0, 0, state: (uint)UseObjectState.TsState1)
            });

            Assert.AreEqual(10, kept);

            var byId = PlacedUsables.All.ToDictionary(p => p.Id);

            Assert.AreEqual((PlacedUsables.Kind.TwoState, UseObjectState.TsState0), (byId[1].Kind, byId[1].State));
            Assert.AreEqual((PlacedUsables.Kind.Door, UseObjectState.DoorStateClosed, 0L), (byId[2].Kind, byId[2].State, byId[2].ResetMs), "a door stays as it is left");
            Assert.AreEqual((PlacedUsables.Kind.Dispenser, UseObjectState.IdStateActive, 60_000L), (byId[3].Kind, byId[3].State, byId[3].ResetMs));
            Assert.AreEqual((PlacedUsables.Kind.Acceptor, UseObjectState.IaStateActive, 10_000L, 1u), (byId[4].Kind, byId[4].State, byId[4].ResetMs, byId[4].ItemCount));
            Assert.AreEqual((PlacedUsables.Kind.Bomb, UseObjectState.BombStateDisarmed, 5_000L, 30_000L, PlacedUsables.BombBlast),
                (byId[5].Kind, byId[5].State, byId[5].IntervalMinMs, byId[5].ResetMs, byId[5].ActionArg));
            Assert.AreEqual((PlacedUsables.Kind.Explosion, UseObjectState.ReState0, 20_000L, 40_000L, 100u, 200u, 6f, PlacedUsables.BombBlast),
                (byId[6].Kind, byId[6].State, byId[6].IntervalMinMs, byId[6].IntervalMaxMs, byId[6].DamageMin, byId[6].DamageMax, byId[6].Radius, byId[6].ActionArg));
            Assert.AreEqual(PlacedUsables.Geyser, byId[7].ActionArg, "by the class's name");
            Assert.AreEqual((100u, 100u), (PlacedUsables.PlacementOf(Row(16, Geyser, 0, 0, damageMax: 50), out _).DamageMin,
                PlacedUsables.PlacementOf(Row(16, Geyser, 0, 0, damageMax: 50), out _).DamageMax), "a most below the least is the least");
            Assert.AreEqual(UseObjectState.ThreeStateSwithState0, byId[8].State);
            Assert.AreEqual(UseObjectState.FiveStateSwithState0, byId[9].State);
            Assert.AreEqual((PlacedUsables.Kind.Display, UseObjectState.MechpadOnReady), (byId[10].Kind, byId[10].State));
            Assert.IsFalse(byId[6].Usable || byId[10].Usable);

            StringAssert.Contains(PlacedUsables.WhyNot(ControlPoint, out _, out _), "control_point");
            StringAssert.Contains(PlacedUsables.WhyNot(Barrel, out _, out _), "world_destructible");
            StringAssert.Contains(PlacedUsables.WhyNot(Rock, out _, out _), "no usable");
            StringAssert.Contains(PlacedUsables.WhyNot((EntityClasses)999999, out _, out _), "not loaded");
            PlacedUsables.PlacementOf(Row(15, Door, 0, 0, state: (uint)UseObjectState.TsState1), out var why);
            StringAssert.Contains(why, "is not one of its class's");

            Assert.AreEqual(PlacedUsables.Lightning, PlacedUsables.ArgOf("RandomExplosionAriekiLightning"));
            Assert.AreEqual(PlacedUsables.Hornito, PlacedUsables.ArgOf("RandomExplosionAriekiHornitoEruption"));
            Assert.AreEqual(PlacedUsables.ExplosionLarge, PlacedUsables.ArgOf("RandomExplosionHumanToriodCannon"));
            Assert.AreEqual(PlacedUsables.ExplosionSmall, PlacedUsables.ArgOf("RandomExplosionPravusReactorFlamingChunks"));
            Assert.AreEqual(DamageType.Electrical, PlacedUsables.DamageTypeOf(PlacedUsables.Lightning));
            Assert.AreEqual(DamageType.Fire, PlacedUsables.DamageTypeOf(PlacedUsables.Geyser));
        }

        [TestMethod]
        public void AUsableSwitchIsShownInItsStateAndAUseTogglesItForEveryoneInRange()
        {
            AddFixtureClasses();
            using var world = new WorldTestContext();
            var user = Stand(world, new Vector3(12, 2, 20));
            var watcher = Stand(world, new Vector3(30, 2, 20));
            using var placed = Put(world.Map, Row(1, Beam, 10, 20, state: (uint)UseObjectState.TsState1, usable: true), Row(2, Beam, 14, 20));

            var beam = ObjectOf(world.Map, 1);
            var made = Sent(user).Select(entry => entry.Packet).OfType<CreatePhysicalEntityPacket>().Single(packet => packet.EntityId == beam.EntityId);
            var info = made.EntityData.OfType<UsableInfoPacket>().Single();

            Assert.AreEqual((UseObjectState.TsState1, true), (info.CurState, info.Enabled));
            Assert.AreEqual(DynamicObjectType.PlacedUsable, beam.DynamicObjectType);
            Assert.AreEqual(new Vector3(10, 2, 20), beam.Position);
            WorldTestContext.Drain(watcher);

            UseAndRecover(world.Map, user, beam);
            Assert.AreEqual(UseObjectState.TsState0, Uses(Sent(watcher), beam.EntityId).Single(), "toggled, to everyone in range");

            UseAndRecover(world.Map, user, beam);
            Assert.AreEqual(UseObjectState.TsState1, beam.StateId);

            // The other is no usable: refused as the client would put it, and left as it is.
            var other = ObjectOf(world.Map, 2);
            WorldTestContext.Drain(user);
            UseAndRecover(world.Map, user, other);
            Assert.AreEqual(UseObjectState.TsState0, other.StateId);
            Assert.AreEqual(PlayerMessage.PmUseObjectNotUsable, Sent(user).Select(entry => entry.Packet).OfType<UserActionFailedPacket>().Single().MsgId);

            // Too far away.
            user.Player.Position = new Vector3(200, 2, 20);
            UseAndRecover(world.Map, user, beam);
            Assert.AreEqual(UseObjectState.TsState1, beam.StateId);
        }

        [TestMethod]
        public void ThreeAndFiveStateSwitchesStepRoundAndADoorClosesAfterItsReset()
        {
            AddFixtureClasses();
            using var world = new WorldTestContext();
            var user = Stand(world, new Vector3(10, 2, 21));
            using var placed = Put(world.Map,
                Row(1, ThreeState, 10, 20, usable: true),
                Row(2, FiveState, 11, 20, state: (uint)UseObjectState.FiveStateSwithState3, usable: true),
                Row(3, Door, 12, 20, usable: true, reset: 8));

            var three = ObjectOf(world.Map, 1);
            var five = ObjectOf(world.Map, 2);
            var door = ObjectOf(world.Map, 3);

            foreach (var expected in new[] { UseObjectState.ThreeStateSwithState1, UseObjectState.ThreeStateSwithState2, UseObjectState.ThreeStateSwithState0 })
            {
                UseAndRecover(world.Map, user, three);
                Assert.AreEqual(expected, three.StateId);
            }

            UseAndRecover(world.Map, user, five);
            Assert.AreEqual(UseObjectState.FiveStateSwithState4, five.StateId);
            UseAndRecover(world.Map, user, five);
            Assert.AreEqual(UseObjectState.FiveStateSwithState0, five.StateId, "4 to 0, a way the client has");

            WorldTestContext.Drain(user);
            UseAndRecover(world.Map, user, door);
            Assert.AreEqual(UseObjectState.DoorStateOpen, Uses(Sent(user), door.EntityId).Single());

            _now += 7_999;
            PlacedUsables.Worker(world.Map);
            Assert.AreEqual(UseObjectState.DoorStateOpen, door.StateId);
            _now += 1;
            PlacedUsables.Worker(world.Map);
            Assert.AreEqual(UseObjectState.DoorStateClosed, Uses(Sent(user), door.EntityId).Single(), "closed again");
        }

        [TestMethod]
        public void AnArmedBombGoesOffAfterItsFuseHurtingThoseNearAndIsDisarmedAgain()
        {
            AddFixtureClasses();
            using var world = new WorldTestContext();
            var user = Stand(world, new Vector3(10, 2, 22));
            var bystander = Stand(world, new Vector3(16, 2, 20));
            var faraway = Stand(world, new Vector3(30, 2, 20));
            using var placed = Put(world.Map, Row(1, Bomb, 10, 20, usable: true, damageMin: 400, damageMax: 400));
            var bomb = ObjectOf(world.Map, 1);

            UseAndRecover(world.Map, user, bomb);
            Assert.AreEqual(UseObjectState.BombStateArmed, bomb.StateId);

            // Disarmed again, and armed again.
            UseAndRecover(world.Map, user, bomb);
            Assert.AreEqual(UseObjectState.BombStateDisarmed, bomb.StateId);
            UseAndRecover(world.Map, user, bomb);
            Assert.AreEqual(UseObjectState.BombStateArmed, bomb.StateId);
            user.Player.Position = new Vector3(40, 2, 20);
            WorldTestContext.Drain(bystander);

            _now += 4_999;
            PlacedUsables.Worker(world.Map);
            Assert.AreEqual(UseObjectState.BombStateArmed, bomb.StateId);

            _now += 1;
            PlacedUsables.Worker(world.Map);
            var sent = Sent(bystander);
            Assert.AreEqual(UseObjectState.BombStateDetonated, Uses(sent, bomb.EntityId).Single());
            var blast = sent.Where(entry => entry.EntityId == bomb.EntityId).Select(entry => entry.Packet).OfType<PerformObjectDamagePacket>().Single();
            Assert.AreEqual(PlacedUsables.BombBlast, blast.ActionArgId);
            CollectionAssert.AreEqual(new[] { bystander.Player.EntityId }, blast.Hits.Select(hit => hit.TargetId).ToArray(), "6 m away, inside 8; the user ran 30 m");
            Assert.AreEqual(1600, bystander.Player.Attributes[Attributes.Health].Current);
            Assert.AreEqual(2000, faraway.Player.Attributes[Attributes.Health].Current);

            // Detonated: no use.
            UseAndRecover(world.Map, bystander, bomb);
            Assert.AreEqual(UseObjectState.BombStateDetonated, bomb.StateId);

            _now += 30_000;
            PlacedUsables.Worker(world.Map);
            Assert.AreEqual(UseObjectState.BombStateDisarmed, bomb.StateId);
        }

        [TestMethod]
        public void ARandomExplosionGoesOffWithinItsIntervalsAndItsEffectIsPutBack()
        {
            AddFixtureClasses();
            using var world = new WorldTestContext();
            var near = Stand(world, new Vector3(12, 2, 20));
            var far = Stand(world, new Vector3(25, 2, 20));
            using var placed = Put(world.Map, Row(1, Geyser, 10, 20, min: 10, max: 30, damageMin: 150, damageMax: 150, radius: 5));
            var geyser = ObjectOf(world.Map, 1);
            var live = PlacedUsables.LiveOf(world.Map, 1);

            Assert.AreEqual(_now + 20_000, live.NextBlastAt, "the roll's middle");
            WorldTestContext.Drain(near);

            _now += 19_999;
            PlacedUsables.Worker(world.Map);
            Assert.IsEmpty(Sent(near).Where(entry => entry.Packet is PerformObjectDamagePacket).ToList());

            _now += 1;
            PlacedUsables.Worker(world.Map);
            var sent = Sent(near);
            Assert.AreEqual(UseObjectState.ReState0, Uses(sent, geyser.EntityId).Single(), "Use to its one state: the transition's effect, where it has one");
            var blast = sent.Select(entry => entry.Packet).OfType<PerformObjectDamagePacket>().Single();
            Assert.AreEqual((PlacedUsables.Geyser, geyser.EntityId), (blast.ActionArgId, blast.SourceId));
            Assert.AreEqual((near.Player.EntityId, DamageType.Fire, 150L), (blast.Hits.Single().TargetId, blast.Hits.Single().DamageType, blast.Hits.Single().Amount));
            Assert.AreEqual(1850, near.Player.Attributes[Attributes.Health].Current);
            Assert.AreEqual(2000, far.Player.Attributes[Attributes.Health].Current);
            Assert.AreEqual(_now + 20_000, live.NextBlastAt);

            _now += PlacedUsables.RearmMs;
            PlacedUsables.Worker(world.Map);
            Assert.AreEqual(UseObjectState.ReState0, Sent(near).Select(entry => entry.Packet).OfType<ForceStatePacket>().Single().State, "the state's effect put back");

            // Nobody can use one.
            UseAndRecover(world.Map, near, geyser);
            Assert.IsFalse(Sent(near).Any(entry => entry.Packet is UsePacket));
        }

        [TestMethod]
        public void RandomExplosionsMissedWhileTheirMapStoodEmptyTakeNewPlacesInTheirCyclesRatherThanAllGoingOff()
        {
            AddFixtureClasses();
            using var world = new WorldTestContext();
            var near = Stand(world, new Vector3(12, 2, 20));
            using var placed = Put(world.Map,
                Row(1, Geyser, 10, 20, min: 10, max: 30, damageMin: 150, damageMax: 150, radius: 5),
                Row(2, Geyser, 14, 20, min: 10, max: 30, damageMin: 150, damageMax: 150, radius: 5));
            var first = PlacedUsables.LiveOf(world.Map, 1);
            var second = PlacedUsables.LiveOf(world.Map, 2);
            Assert.AreEqual((_now + 20_000, _now + 20_000), (first.NextBlastAt, second.NextBlastAt), "both at the roll's middle");
            WorldTestContext.Drain(near);

            // Nobody worked the map: both are past due by more than a map is late by.
            _now += 20_000 + PlacedUsables.MissedMs + 1;
            var rolls = new Queue<long>(new long[] { 4_000, 25_000 });
            var asked = new List<(long, long)>();
            PlacedUsables.Roll = (min, max) => { asked.Add((min, max)); return rolls.Dequeue(); };
            PlacedUsables.Worker(world.Map);

            Assert.IsFalse(Sent(near).Any(entry => entry.Packet is PerformObjectDamagePacket), "neither goes off as the map is worked again");
            Assert.AreEqual(2000, near.Player.Attributes[Attributes.Health].Current);
            CollectionAssert.AreEqual(new[] { (0L, 30_000L), (0L, 30_000L) }, asked, "each somewhere in its longest wait");
            Assert.AreEqual((_now + 4_000, _now + 25_000), (first.NextBlastAt, second.NextBlastAt));

            // Late by no more than that, one goes off; the other is not yet due.
            PlacedUsables.Roll = (min, max) => min + (max - min) / 2;
            _now += 4_000 + PlacedUsables.MissedMs;
            PlacedUsables.Worker(world.Map);
            var blasts = Sent(near).Select(entry => entry.Packet).OfType<PerformObjectDamagePacket>().ToList();
            Assert.AreEqual(ObjectOf(world.Map, 1).EntityId, blasts.Single().SourceId);
            Assert.AreEqual(_now + 20_000, first.NextBlastAt);
            Assert.AreEqual(_now - 9_000 + 25_000, second.NextBlastAt);
        }

        [TestMethod]
        public void ADispenserGivesItsItemAndIsActiveAgainAfterItsReset()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var map = harness.Map;
            var at = harness.Client.Player.Position + new Vector3(1.5f, 0, 0);
            var row = Row(1, Dispenser, at.X, at.Z, map: map.MapInfo.MapContextId, usable: true, itemTemplate: Cartridges, count: 25, reset: 90);
            row.PosY = at.Y;

            using var placed = Put(map, row);
            var dispenser = ObjectOf(map, 1);
            var before = InventoryManager.Instance.CountItemsByClass(harness.Client, CartridgeClass);

            harness.Drain();
            UseAndRecover(map, harness.Client, dispenser);

            Assert.AreEqual(before + 25, InventoryManager.Instance.CountItemsByClass(harness.Client, CartridgeClass));
            Assert.AreEqual(UseObjectState.IdStateInactive, dispenser.StateId);
            Assert.AreEqual(UseObjectState.IdStateInactive, harness.Drain().OfType<UsePacket>().Single().CurState);

            // Inactive: refused.
            UseAndRecover(map, harness.Client, dispenser);
            Assert.AreEqual(before + 25, InventoryManager.Instance.CountItemsByClass(harness.Client, CartridgeClass));
            Assert.AreEqual(PlayerMessage.PmUseObjectNotUsable, harness.Drain().OfType<UserActionFailedPacket>().Single().MsgId);

            _now += 90_000;
            PlacedUsables.Worker(map);
            Assert.AreEqual(UseObjectState.IdStateActive, dispenser.StateId);
        }

        [TestMethod]
        public void AnAcceptorTakesItsItemsOrIsRefusedWithoutThem()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var map = harness.Map;
            var at = harness.Client.Player.Position + new Vector3(1.5f, 0, 0);
            var row = Row(1, Acceptor, at.X, at.Z, map: map.MapInfo.MapContextId, usable: true, itemClass: (uint)CartridgeClass, count: 10);
            row.PosY = at.Y;

            using var placed = Put(map, row);
            var acceptor = ObjectOf(map, 1);
            var held = InventoryManager.Instance.CountItemsByClass(harness.Client, CartridgeClass);

            if (held > 0)
                InventoryManager.Instance.RemoveItemsByClass(harness.Client, CartridgeClass, held);

            harness.Drain();
            UseAndRecover(map, harness.Client, acceptor);
            Assert.AreEqual(PlayerMessage.PmMissingReqItem, harness.Drain().OfType<UserActionFailedPacket>().Single().MsgId);
            Assert.AreEqual(UseObjectState.IaStateActive, acceptor.StateId);

            Assert.IsNotNull(InventoryManager.Instance.GrantItemToInventory(harness.Client, ItemManager.Instance.CreateFromTemplateId(Cartridges, 25)));
            UseAndRecover(map, harness.Client, acceptor);

            Assert.AreEqual(15u, InventoryManager.Instance.CountItemsByClass(harness.Client, CartridgeClass));
            Assert.AreEqual(UseObjectState.IaStateAccepted, acceptor.StateId);

            _now += 10_000;
            PlacedUsables.Worker(map);
            Assert.AreEqual(UseObjectState.IaStateActive, acceptor.StateId);
        }

        [TestMethod]
        public void EachPrivateCopyHasItsOwnAndForgetTakesThemAway()
        {
            AddFixtureClasses();
            using var world = new WorldTestContext();
            using var copy = new WorldTestContext();
            var user = Stand(world, new Vector3(12, 2, 20));
            using var placed = Put(world.Map, Row(1, Beam, 10, 20, usable: true), Row(2, Beam, 10, 20, map: 9999));

            Assert.HasCount(1, PlacedUsables.On(world.Map));
            Assert.AreEqual(0, PlacedUsables.Place(world.Map), "one the channel has is left alone");
            Assert.AreEqual(1, PlacedUsables.Place(copy.Map));

            UseAndRecover(world.Map, user, ObjectOf(world.Map, 1));
            Assert.AreEqual(UseObjectState.TsState1, ObjectOf(world.Map, 1).StateId);
            Assert.AreEqual(UseObjectState.TsState0, ObjectOf(copy.Map, 1).StateId, "the copy's own");

            var gone = ObjectOf(copy.Map, 1).EntityId;
            Assert.AreEqual(1, PlacedUsables.Forget(copy.Map));
            Assert.IsFalse(EntityManager.Instance.TryGetObject(gone, out _));
        }

        [TestMethod]
        public void AGameMasterPutsOneDownSetsItsStateSetsOneOffAndClears()
        {
            AddFixtureClasses();
            using var world = new WorldTestContext();
            var client = world.CreateClient();
            typeof(Client).GetProperty(nameof(Client.AccountEntry)).SetValue(client, new GameAccountEntry { Level = (byte)GmLevel.Admin });
            client.Player.PlaceAt(new Vector3(30f, 4f, 40f));
            client.Player.Rotation = 1.25f;
            client.Player.State = CharacterState.Normal;
            client.Player.Attributes[Attributes.Health] = new ActorAttributes(Attributes.Health, 2000, 2000, 2000, 0, 0);
            client.Player.Attributes[Attributes.Armor] = new ActorAttributes(Attributes.Armor, 0, 0, 0, 0, 0);
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

            using var placed = Put(world.Map, Row(3, Door, 31, 40));

            var classes = Say(".worldusable classes door");
            StringAssert.Contains(classes[0], "class(es)");
            Assert.IsTrue(classes.Any(line => line.StartsWith("4412 UsableDoorBaneV01 (Door: 31, 91)")));

            StringAssert.Contains(Say(".worldusable put 9260").Single(), "world_destructible");
            StringAssert.Contains(Say(".worldusable put UsableDoorBaneV01 state=55").Single(), "is not one of its class's");
            StringAssert.Contains(Say(".worldusable put UsableDoorBaneV01 colour=3").Single(), "no key");

            var put = Say(".worldusable put FALLINGBOMB min=4 max=6 radius=3.5");
            StringAssert.Contains(put[0], "put down: 3913 FALLINGBOMB (Explosion)");
            Assert.AreEqual("Row(id, 1220, 3913, 30, 4, 40, 1.25, \"FALLINGBOMB\", state: 0, usable: false, itemTemplateId: 0, itemClassId: 0, itemCount: 0, "
                + "resetSeconds: 0, intervalMinSeconds: 4, intervalMaxSeconds: 6, damageMin: 0, damageMax: 0, radius: 3.5, actionArg: 0)", put[1]);

            var bomb = PlacedUsables.On(world.Map).Single(live => live.Placement.IsPutDown);
            Assert.AreEqual((4_000L, 6_000L, 3.5f), (bomb.Placement.IntervalMinMs, bomb.Placement.IntervalMaxMs, bomb.Placement.Radius));

            StringAssert.Contains(Say(".worldusable near")[0], "put down: 3913");
            StringAssert.Contains(Say(".worldusable state 91 3").Single(), "Now row 3");
            Assert.AreEqual(UseObjectState.DoorStateOpen, ObjectOf(world.Map, 3).StateId);
            StringAssert.Contains(Say(".worldusable state 55 3").Single(), "not one of its states");
            StringAssert.Contains(Say(".worldusable blast 3").Single(), "Only a random explosion or a bomb");
            StringAssert.Contains(Say(".worldusable blast").Single(), "Boom: 1 player(s) hit");

            StringAssert.Contains(Say(".worldusable clear").Single(), "Took away 1 object(s)");
            Assert.HasCount(1, PlacedUsables.On(world.Map), "the row's stays");
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
                    Assert.IsTrue(new WorldUsableRepository(context).Get().All(Seeded), "only the Arieki vents' eruptions, the Lightning Fields' strikes and the Wilderness terminals are seeded");
                    Assert.IsFalse(context.Database.HasPendingModelChanges());
                }

                using (var context = (WorldContext)PersistenceIntegrationTests.CreateContext(typeof(SqliteWorldContext), database))
                {
                    var builder = new MigrationBuilder(context.Database.ProviderName);
                    var lot = new FixtureLot();

                    lot.Preload(builder);
                    Run(context, builder);

                    var rows = new WorldUsableRepository(context).Get().Where(row => !Seeded(row)).OrderBy(row => row.Id).ToList();
                    Assert.HasCount(4, rows);
                    Assert.AreEqual((1u, 1220u, 4412u, 10.5, 2.25, 20.75, 0.5, 0u, 1u, 0u, 15u, "Door, it's by the gate"),
                        (rows[0].Id, rows[0].MapContextId, rows[0].ClassId, rows[0].PosX, rows[0].PosY, rows[0].PosZ, rows[0].Rotation,
                         rows[0].State, rows[0].Usable, rows[0].ItemTemplateId, rows[0].ResetSeconds, rows[0].Comment));
                    Assert.AreEqual((7007u, 1u, 28u, 25u, 90u), (rows[1].ClassId, rows[1].Usable, rows[1].ItemTemplateId, rows[1].ItemCount, rows[1].ResetSeconds));
                    Assert.AreEqual((7131u, 3147u, 10u), (rows[2].ClassId, rows[2].ItemClassId, rows[2].ItemCount));
                    Assert.AreEqual((3913u, 0u, 4u, 9u, 50u, 80u, 3.5, 2u), (rows[3].ClassId, rows[3].Usable, rows[3].IntervalMinSeconds, rows[3].IntervalMaxSeconds,
                        rows[3].DamageMin, rows[3].DamageMax, rows[3].Radius, rows[3].ActionArg));

                    builder = new MigrationBuilder(context.Database.ProviderName);
                    lot.Remove(builder);
                    Run(context, builder);
                    Assert.IsTrue(new WorldUsableRepository(context).Get().All(Seeded), "the lot's rows gone, the seeded ones left");
                }

                using (var context = (WorldContext)PersistenceIntegrationTests.CreateContext(typeof(SqliteWorldContext), database))
                {
                    context.GetService<IMigrator>().Migrate("20261213000000_Add_world_destructibles");
                    Assert.ThrowsExactly<Microsoft.Data.Sqlite.SqliteException>(() => context.WorldUsableEntries.AsNoTracking().ToList());
                    Assert.IsEmpty(context.WorldDestructibleEntries.AsNoTracking().ToList(), "the table before it is left");
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
            var up = migrator.GenerateScript("20261213000000_Add_world_destructibles", "20261214000000_Add_world_usables");
            var down = migrator.GenerateScript("20261214000000_Add_world_usables", "20261213000000_Add_world_destructibles");

            StringAssert.Contains(up, "CREATE TABLE `world_usable`");
            foreach (var column in new[] { "`id` int unsigned NOT NULL AUTO_INCREMENT", "`state` int unsigned NOT NULL", "`usable` int unsigned NOT NULL",
                "`item_template_id` int unsigned NOT NULL", "`radius` double NOT NULL", "`action_arg` int unsigned NOT NULL",
                "`comment` varchar(128) NOT NULL", "CONSTRAINT `PK_world_usable` PRIMARY KEY (`id`)" })
                StringAssert.Contains(up, column);
            StringAssert.Contains(down, "DROP TABLE `world_usable`");
        }

        private static bool Vent(WorldUsableEntry row) => row.Id is >= AriekiVentEruptions.FirstId and <= AriekiVentEruptions.LastId;

        /// <summary>A row a migration seeds: an Arieki vent's eruption, or a Wilderness terminal of Wilderness_post_npcs.</summary>
        private static bool Seeded(WorldUsableEntry row) => Vent(row) ||
            row.Id is >= LightningFieldsStrikes.FirstId and <= LightningFieldsStrikes.LastId ||
            row.Id >= WildernessPostNpcs.FirstUsable && row.Id < WildernessPostNpcs.FirstUsable + WildernessPostNpcs.Usables.Length;

        private static void Run(WorldContext context, MigrationBuilder builder)
        {
            var sql = context.GetService<IMigrationsSqlGenerator>().Generate(builder.Operations, context.Model);

            foreach (var command in sql)
                context.Database.ExecuteSqlRaw(command.CommandText);
        }

        private sealed class FixtureLot : WorldUsablePreloader
        {
            protected override IEnumerable<object[]> GetRows()
            {
                yield return Switch(1, 1220, 4412, 10.5, 2.25, 20.75, 0.5, "Door, it's by the gate", resetSeconds: 15);
                yield return Dispenser(2, 1220, 7007, 40, 2, 20, 1, "Crystal specimen", itemTemplateId: 28, count: 25, resetSeconds: 90);
                yield return Acceptor(3, 1220, 7131, 41, 2, 20, 1, "Queen lure", itemClassId: 3147, count: 10);
                yield return Explosion(4, 1220, 3913, 42, 2, 20, "Falling bombs", minSeconds: 4, maxSeconds: 9, damageMin: 50, damageMax: 80, radius: 3.5, actionArg: 2);
            }
        }
    }
}
