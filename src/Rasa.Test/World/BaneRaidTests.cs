using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Rasa.Config;
    using Rasa.Data;
    using Rasa.Game;
    using Rasa.Managers;
    using Rasa.Packets.ClientMethod.Server;
    using Rasa.Packets.Communicator.Server;
    using Rasa.Structures;
    using Rasa.Structures.World;
    using Rasa.Test.Missions;

    // The raids on control points (BaneRaids): the Bane's on the AFS's points - when one is due and
    // when it begins, its waves of ships, a raider's claim and what interrupts it, the point taken
    // through ControlPoints' own change of hands, and the garrison a point with none of its own is
    // given and loses again - and the AFS's on the Bane's, less often and with less hope, told by
    // their countdown, healers first, called only by somebody playing.
    [TestClass]
    [DoNotParallelize]
    public class BaneRaidTests
    {
        private const uint MapId = 1220;    // WorldTestContext's map
        private const uint PointId = 4;
        private const uint BanePool = 9601;
        private const uint AfsPool = 9602;
        private const uint CampPool = 9603;
        private const EntityClasses ThraxClass = (EntityClasses)3762;
        private const EntityClasses SoldierClass = (EntityClasses)29423;
        private const EntityClasses CaretakerClass = (EntityClasses)3751;
        private const uint ThraxId = 4_100_000_011;
        private const uint SoldierId = 4_100_000_012;
        private static readonly Vector3 At = new Vector3(100, 0, 100);

        private Func<long> _raidNow;
        private Func<long> _pointNow;
        private Func<long> _utcNow;
        private Func<uint, bool> _known;
        private RaidConfig _config;
        private ReinforcementConfig _reinforcements;
        private long _tick;
        private readonly List<EntityClasses> _classes = new List<EntityClasses>();

        [TestInitialize]
        public void Start()
        {
            _raidNow = BaneRaids.Instance.Now;
            _pointNow = ControlPoints.Instance.Now;
            _utcNow = ControlPoints.Instance.UtcNow;
            _known = ControlPoints.Instance.KnownCreature;
            _config = BaneRaids.Instance.Config;
            _reinforcements = Reinforcements.Config;
            _tick = 10_000_000;

            BaneRaids.Instance.Reset();
            BaneRaids.Instance.Now = () => _tick;
            BaneRaids.Instance.Config = new RaidConfig();
            Reinforcements.Config = new ReinforcementConfig();
            ControlPoints.Instance.Now = () => _tick;
            ControlPoints.Instance.UtcNow = () => 1_700_000_000_000;
            ControlPoints.Instance.KnownCreature = id => true;

            Class(ThraxClass, "Bane_Thrax_Soldier_Pistol");
            Class(SoldierClass, "Redshirt_Human_Soldier_Light_Male");
            Class(CaretakerClass, "Bane_Caretaker");

            CreatureManager.Instance.LoadedCreatures[ThraxId] = new Creature
            {
                DbId = ThraxId, EntityClass = ThraxClass, TargetCategory = TargetCategory.Hostile, WalkSpeed = 5, RunSpeed = 9, Level = 11
            };
            CreatureManager.Instance.LoadedCreatures[SoldierId] = new Creature
            {
                DbId = SoldierId, EntityClass = SoldierClass, TargetCategory = TargetCategory.Friendly, WalkSpeed = 5, RunSpeed = 9, Level = 8
            };
        }

        [TestCleanup]
        public void Restore()
        {
            foreach (var point in ControlPoints.Instance.Points.ToList())
                BaneRaids.Instance.Stop(point.Id);

            BaneRaids.Instance.Reset();
            ControlPoints.Instance.Load(new List<ControlPointEntry>(), new List<ControlPointLinkEntry>(), null);
            BaneRaids.Instance.Now = _raidNow;
            BaneRaids.Instance.Config = _config;
            Reinforcements.Config = _reinforcements;
            ControlPoints.Instance.Now = _pointNow;
            ControlPoints.Instance.UtcNow = _utcNow;
            ControlPoints.Instance.KnownCreature = _known;
            CreatureManager.Instance.LoadedCreatures.Remove(ThraxId);
            CreatureManager.Instance.LoadedCreatures.Remove(SoldierId);

            foreach (var id in _classes)
                EntityClassManager.Instance.LoadedEntityClasses.Remove(id);

            _classes.Clear();

            foreach (var ship in DynamicObjectManager.Instance.Dropships.Values.Where(s => s.SpawnPool != null && Reinforcements.IsTransient(s.SpawnPool)).ToList())
            {
                DynamicObjectManager.Instance.Dropships.Remove(ship.EntityId);
                EntityManager.Instance.FreeEntity(ship.EntityId);
            }
        }

        [TestMethod]
        public void ARaidIsDueAfterTheServerStartsAndBeginsOnlyWithAPlayerNear()
        {
            using var world = new WorldTestContext();
            var point = Placed(world, ownGarrison: true);

            BaneRaids.Instance.Worker(world.Map);

            var due = BaneRaids.Instance.NextAt(point.Id);
            Assert.IsNotNull(due, "the first raid is set");
            Assert.IsTrue(due.Value >= _tick + 6 * 60_000 && due.Value <= _tick + 12 * 60_000, "six to twelve minutes after the server starts");

            _tick = due.Value + 1;
            BaneRaids.Instance.Worker(world.Map);
            Assert.IsNull(BaneRaids.Instance.Of(point.Id), "nobody near: it waits");

            var far = world.CreateClient(At.X + 500, At.Z);
            BaneRaids.Instance.Worker(world.Map);
            Assert.IsNull(BaneRaids.Instance.Of(point.Id), "nobody within range");

            var near = world.CreateClient(At.X + 100, At.Z);
            BaneRaids.Instance.Worker(world.Map);

            var raid = BaneRaids.Instance.Of(point.Id);
            Assert.IsNotNull(raid, "a player within 300 m: it begins");
            Assert.IsFalse(raid.ByAfs);
            CollectionAssert.AreEqual(new List<uint> { ThraxId }, raid.Troops, "the point's own Bane garrison's troops");
            Assert.IsTrue(raid.Waves >= 3 && raid.Waves <= 4, "three or four waves");
            Assert.IsTrue(world.Map.SpawnPools.Single(p => p.DbId == AfsPool).Besieged, "the point's AFS garrison is not restocked while the raid is on");

            BaneRaids.Instance.Worker(world.Map);
            Assert.AreEqual(1, raid.Wave, "the first wave's ships are sent on the next pass");
            Assert.IsTrue(raid.Pool.DropshipQueue > 0, "on their way");

            var ships = DynamicObjectManager.Instance.Dropships.Values.Where(s => s.SpawnPool == raid.Pool).ToList();
            Assert.IsTrue(ships.Count > 0 && ships.All(s => s.EntityClassId == EntityClasses.UsableCrSpawnerBaneDropshipV01), "by Bane dropship (9268)");
            Assert.IsTrue(ships.All(s => Vector3.Distance(s.Position, point.Object.Position) <= Reinforcements.RaidLandingFurthest), "no further out than 120 m");

            Assert.IsTrue(MissionTestContext.Drain(near).OfType<DisplaySystemMessagePacket>().Any(m => m.MsgId == PlayerMessage.PmBRifWelcome),
                "\"Thrax are gathering to attack a friendly outpost\"");
            Assert.IsFalse(MissionTestContext.Drain(far).OfType<DisplaySystemMessagePacket>().Any(m => m.MsgId == PlayerMessage.PmBRifWelcome),
                "not told 600 m off");
        }

        [TestMethod]
        public void ARaiderStandingAtThePointTakesItUnlessItIsHurt()
        {
            using var world = new WorldTestContext();
            var point = Placed(world, ownGarrison: true);
            var client = world.CreateClient(At.X + 50, At.Z);
            var raid = BaneRaids.Instance.Begin(world.Map, point);
            var raider = Raider(world, raid, point.Object.Position);

            BaneRaids.Instance.Worker(world.Map);

            Assert.AreSame(raider, raid.Claimer, "it stands at the point, not fighting: it claims it");
            Assert.AreSame(raider, point.Object.UsedBy, "the object is locked to it");
            Assert.IsTrue(MissionTestContext.Drain(client).OfType<DisplayClientMessagePacket>().Any(m => m.MsgId == PlayerMessage.PmControlpointClaiming),
                "\"Bane claiming Control Point\"");

            _tick += 5_000;
            raider.Attributes[Attributes.Health].Current -= 10;
            BaneRaids.Instance.Worker(world.Map);

            Assert.IsNull(raid.Claimer, "hurt: interrupted");
            Assert.IsNull(point.Object.UsedBy);
            Assert.IsTrue(point.HeldByAfs);

            BaneRaids.Instance.Worker(world.Map);
            Assert.IsNull(raid.Claimer, "not while it is still under fire");

            _tick += BaneRaids.UnhurtMs;
            BaneRaids.Instance.Worker(world.Map);
            Assert.AreSame(raider, raid.Claimer, "left alone, it starts again");
            Assert.AreEqual(0, MissionTestContext.Drain(client).OfType<DisplayClientMessagePacket>().Count(m => m.MsgId == PlayerMessage.PmControlpointClaiming),
                "the map is not told again within half a minute");

            _tick += 9_000;
            BaneRaids.Instance.Worker(world.Map);
            Assert.IsTrue(point.HeldByAfs, "not ten seconds yet");

            _tick += 1_000;
            BaneRaids.Instance.Worker(world.Map);

            Assert.IsFalse(point.HeldByAfs, "ten seconds at it: the Bane's");
            Assert.IsNull(point.Object.UsedBy, "let go");
            Assert.IsNull(BaneRaids.Instance.Of(point.Id), "the raid is over");
            Assert.AreEqual(1, BaneRaids.Instance.Holders(world.Map, point, ControlPoints.Bane), "its raider holds the point with the garrison");
            Assert.AreEqual(ControlPoints.Garrison.Standing, ControlPoints.Instance.GarrisonOf(world.Map, point, ControlPoints.Bane));
            Assert.IsTrue(world.Map.SpawnPools.Single(p => p.DbId == BanePool).ShipNextArrival, "the garrison that takes it over is flown in");
            Assert.IsFalse(world.Map.SpawnPools.Single(p => p.DbId == AfsPool).Besieged, "the siege is lifted");
            Assert.IsTrue(MissionTestContext.Drain(client).OfType<DisplayClientMessagePacket>().Any(m => m.MsgId == PlayerMessage.PmControlpointOwned),
                "\"Bane took Control Point\", by ControlPoints' own change of hands");

            BaneRaids.Instance.Worker(world.Map);
            Assert.AreEqual(_tick + 30 * 60_000, BaneRaids.Instance.NextAt(point.Id), "the AFS come back for it half an hour later");
        }

        [TestMethod]
        public void TheClaimIsNotAnnouncedOverAndOver()
        {
            using var world = new WorldTestContext();
            var point = Placed(world, ownGarrison: true);
            var client = world.CreateClient(At.X + 50, At.Z);
            var raid = BaneRaids.Instance.Begin(world.Map, point);
            var raider = Raider(world, raid, point.Object.Position);
            var told = 0;

            // Hurt and left alone by turns for a minute: a claim begun six times.
            for (var i = 0; i < 6; i++)
            {
                BaneRaids.Instance.Worker(world.Map);
                Assert.AreSame(raider, raid.Claimer, $"claim {i + 1}");
                raider.Attributes[Attributes.Health].Current -= 1;
                _tick += 1_000;
                BaneRaids.Instance.Worker(world.Map);
                _tick += BaneRaids.UnhurtMs + 6_000;
                told += MissionTestContext.Drain(client).OfType<DisplayClientMessagePacket>().Count(m => m.MsgId == PlayerMessage.PmControlpointClaiming);
            }

            Assert.AreEqual(2, told, "once in half a minute");
        }

        [TestMethod]
        public void APointWithNoGarrisonIsGivenTheRaidsUntilTheAfsTakeItBack()
        {
            using var world = new WorldTestContext();
            var point = Placed(world, ownGarrison: false);
            world.CreateClient(At.X + 50, At.Z);
            var raid = BaneRaids.Instance.Begin(world.Map, point);

            Assert.IsNotNull(raid);
            CollectionAssert.AreEqual(new List<uint> { ThraxId }, raid.Troops, "the troops of the camp nearest it");

            Raider(world, raid, point.Object.Position);
            BaneRaids.Instance.Worker(world.Map);
            _tick += 10_000;
            BaneRaids.Instance.Worker(world.Map);

            Assert.IsFalse(point.HeldByAfs);
            Assert.IsTrue(point.BanePools.Contains(raid.Pool.DbId), "the raid's own is its garrison now");
            Assert.IsTrue(world.Map.SpawnPools.Contains(raid.Pool), "and on the map's list, for the spawn worker");
            Assert.AreEqual(SpawnPoolManager.ModeControlPoint, raid.Pool.Mode);
            Assert.IsTrue(raid.Pool.IsGarrison);
            Assert.IsTrue(raid.Pool.SpawnSlot.Any(s => s.CreatureId == ThraxId && s.CountMax > 0), "it comes back");
            Assert.AreEqual(ControlPoints.Garrison.Standing, ControlPoints.Instance.GarrisonOf(world.Map, point, ControlPoints.Bane));
            Assert.IsTrue(raid.Pool.DropshipQueue > 0, "a number of Bane are dropshipped in");

            ControlPoints.Instance.SetOwner(point, ControlPoints.Afs, null);
            BaneRaids.Instance.Worker(world.Map);

            Assert.IsFalse(point.BanePools.Contains(raid.Pool.DbId), "the AFS took it back: it is the point's no more");
            Assert.IsFalse(world.Map.SpawnPools.Contains(raid.Pool));
            Assert.AreEqual(_tick + 8 * 60_000, BaneRaids.Instance.NextAt(point.Id), "the Bane come back for it eight minutes later");
        }

        [TestMethod]
        public void ThePointsDefendersTakeTheRaidOnAndTheClaimerFirst()
        {
            using var world = new WorldTestContext();
            var point = Placed(world, ownGarrison: true);
            var afs = world.Map.SpawnPools.Single(p => p.DbId == AfsPool);
            world.CreateClient(At.X + 50, At.Z);
            BaneRaids.Instance.Config.RaidDefendRange = 50;
            BaneRaids.Instance.Config.RaidDefendersOnClaimer = 3;
            var raid = BaneRaids.Instance.Begin(world.Map, point);
            var at = point.Object.Position;

            var claimer = Raider(world, raid, at);
            var other = Raider(world, raid, at + new Vector3(-30, 0, 5));
            var near = Enumerable.Range(0, 4).Select(i => Armed(Standing(world, afs, at + new Vector3(-8 - i * 6, 0, 0), SoldierId, SoldierClass, TargetCategory.Friendly))).ToList();
            var far = Armed(Standing(world, afs, at + new Vector3(0, 0, 80), SoldierId, SoldierClass, TargetCategory.Friendly));

            BaneRaids.Instance.Worker(world.Map);

            Assert.AreSame(claimer, raid.Claimer);
            foreach (var defender in near.Take(3))
                Assert.AreEqual(claimer.EntityId, Target(defender), "the three nearest go for the raider at the point");
            Assert.AreEqual(other.EntityId, Target(near[3]), "the next for the raider nearest it");
            Assert.AreEqual(0UL, Target(far), "one 80 m off stays at its post");

            BaneRaids.Instance.Config.RaidDefendRange = 0;
            var idle = Armed(Standing(world, afs, at + new Vector3(-10, 0, 5), SoldierId, SoldierClass, TargetCategory.Friendly));
            BaneRaids.Instance.Worker(world.Map);
            Assert.AreEqual(0UL, Target(idle), "off: they fight what they see");
        }

        [TestMethod]
        public void NoRaidWithRaidsOff()
        {
            using var world = new WorldTestContext();
            var point = Placed(world, ownGarrison: true);
            world.CreateClient(At.X, At.Z);

            BaneRaids.Instance.Config.Raids = false;
            BaneRaids.Instance.Worker(world.Map);
            _tick += 60 * 60_000;
            BaneRaids.Instance.Worker(world.Map);
            Assert.IsNull(BaneRaids.Instance.Of(point.Id), "the Bane's off");

            BaneRaids.Instance.Config.AfsRaids = false;
            ControlPoints.Instance.SetOwner(point, ControlPoints.Bane, null);
            BaneRaids.Instance.Worker(world.Map);
            _tick += 60 * 60_000;
            BaneRaids.Instance.Worker(world.Map);
            Assert.IsNull(BaneRaids.Instance.Of(point.Id), "the AFS's off");
        }

        [TestMethod]
        public void TheAfsComeBackForAPointTheBaneTookLaterAndOnlyForSomebodyPlaying()
        {
            using var world = new WorldTestContext();
            var point = Placed(world, ownGarrison: true);

            BaneRaids.Instance.Worker(world.Map);
            ControlPoints.Instance.SetOwner(point, ControlPoints.Bane, null);
            BaneRaids.Instance.Worker(world.Map);

            Assert.AreEqual(_tick + 30 * 60_000, BaneRaids.Instance.NextAt(point.Id), "half an hour after the Bane took it");

            _tick += 30 * 60_000;
            var immune = world.CreateClient(At.X + 50, At.Z);
            immune.Player.ImmuneToAllDamage = true;
            var away = world.CreateClient(At.X + 60, At.Z);
            away.Player.IsAFK = true;
            BaneRaids.Instance.Worker(world.Map);
            Assert.IsNull(BaneRaids.Instance.Of(point.Id), "nobody playing near: under .immune, or AFK");

            var player = world.CreateClient(At.X + 100, At.Z);
            BaneRaids.Instance.Worker(world.Map);

            var raid = BaneRaids.Instance.Of(point.Id);
            Assert.IsNotNull(raid, "somebody playing within 300 m: it begins");
            Assert.IsTrue(raid.ByAfs, "the AFS's");
            CollectionAssert.AreEqual(new List<uint> { SoldierId }, raid.Troops, "the point's own AFS soldiers");
            Assert.AreEqual(3, raid.Waves, "three waves");
            Assert.AreEqual(8, raid.Squad, "of eight a ship");
            Assert.AreEqual(14u, raid.Level, "three over the middle level of the point's Bane");
            Assert.AreEqual(14u, raid.Pool.Level, "and brought to it as they are set down");
            Assert.IsTrue(world.Map.SpawnPools.Single(p => p.DbId == BanePool).Besieged, "the point's garrison is not restocked while the raid is on");
            Assert.IsTrue(MissionTestContext.Drain(player).OfType<DisplayPlayerNotificationPacket>().Any(m => m.MsgId == PlayerMessage.ARakshaAfsArrive2Min),
                "\"AFS Reinforcements En Route - 2 minutes remaining\"");

            BaneRaids.Instance.Worker(world.Map);
            Assert.AreEqual(0, raid.Wave, "no ships yet");

            _tick += 60_000;
            BaneRaids.Instance.Worker(world.Map);
            Assert.IsTrue(MissionTestContext.Drain(player).OfType<DisplayPlayerNotificationPacket>().Any(m => m.MsgId == PlayerMessage.ARakshaAfsArrive1Min),
                "\"1 minute remaining\"");

            _tick += 60_000;
            BaneRaids.Instance.Worker(world.Map);
            Assert.AreEqual(1, raid.Wave, "two minutes on: the first wave");

            var ships = DynamicObjectManager.Instance.Dropships.Values.Where(s => s.SpawnPool == raid.Pool).ToList();
            Assert.IsTrue(ships.Count == 1 && ships.All(s => s.EntityClassId == EntityClasses.UsableCrSpawnerHumDropshipV01), "one AFS dropship (9269)");
            Assert.AreEqual(8, raid.Pool.QueuedCreatures, "of eight");

            _tick += 40_000;
            BaneRaids.Instance.Worker(world.Map);
            Assert.AreEqual(2, raid.Wave, "the next wave 40 s on, whatever is left of the first");

            BaneRaids.Instance.Stop(point.Id);
            Assert.IsTrue(MissionTestContext.Drain(player).OfType<DisplayPlayerNotificationPacket>().Any(m => m.MsgId == PlayerMessage.ARakshaAfsCalledBack),
                "\"AFS Reinforcements Called Back\"");
            Assert.IsFalse(world.Map.SpawnPools.Single(p => p.DbId == BanePool).Besieged);
        }

        [TestMethod]
        public void TheAfsSoldiersGoForTheBanesHealersFirst()
        {
            using var world = new WorldTestContext();
            var point = Placed(world, ownGarrison: true);
            var garrison = world.Map.SpawnPools.Single(p => p.DbId == BanePool);
            world.CreateClient(At.X + 50, At.Z);

            ControlPoints.Instance.SetOwner(point, ControlPoints.Bane, null);
            BaneRaids.Instance.Worker(world.Map);

            var raid = BaneRaids.Instance.Begin(world.Map, point);
            var at = point.Object.Position;
            var soldier = Standing(world, raid.Pool, at + new Vector3(20, 0, 0), SoldierId, SoldierClass, TargetCategory.Friendly);
            var thrax = Armed(Standing(world, garrison, at + new Vector3(18, 0, 0), ThraxId, ThraxClass, TargetCategory.Hostile));
            var caretaker = Armed(Standing(world, garrison, at + new Vector3(-10, 0, 0), ThraxId, CaretakerClass, TargetCategory.Hostile));
            caretaker.Actions.Add(new CreatureAction { ActionId = ActionId.CrCaretakerRevive });

            BaneRaids.Instance.Worker(world.Map);

            Assert.AreEqual(caretaker.EntityId, Target(soldier), "past the Thrax beside it, to the Caretaker who raises them");
            Assert.AreNotEqual(0UL, Target(thrax), "the point's Bane take the raid on");
        }

        [TestMethod]
        public void AnAfsSoldierClaimsThePointOnlyOnceNoBaneStandsAtIt()
        {
            using var world = new WorldTestContext();
            var point = Placed(world, ownGarrison: true);
            var client = world.CreateClient(At.X + 50, At.Z);
            var garrison = world.Map.SpawnPools.Single(p => p.DbId == BanePool);

            // The ground cleared first: the rule's own setting (off by default).
            BaneRaids.Instance.Config.AfsRaidClearRange = 20;
            ControlPoints.Instance.SetOwner(point, ControlPoints.Bane, null);
            BaneRaids.Instance.Worker(world.Map);

            var raid = BaneRaids.Instance.Begin(world.Map, point);
            Assert.IsTrue(raid.ByAfs);

            var soldier = Standing(world, raid.Pool, point.Object.Position, SoldierId, SoldierClass, TargetCategory.Friendly);
            var thrax = Standing(world, garrison, point.Object.Position + new Vector3(20, 0, 0), ThraxId, ThraxClass, TargetCategory.Hostile);

            BaneRaids.Instance.Worker(world.Map);
            Assert.IsNull(raid.Claimer, "a Bane stands 20 m from the point: the AFS clear its ground first");

            thrax.State = CharacterState.Dead;
            BaneRaids.Instance.Worker(world.Map);
            Assert.AreSame(soldier, raid.Claimer, "the ground clear: it claims the point");
            Assert.IsTrue(MissionTestContext.Drain(client).OfType<DisplayClientMessagePacket>().Any(m => m.MsgId == PlayerMessage.PmControlpointClaiming),
                "\"AFS claiming Control Point\"");

            var back = Standing(world, garrison, point.Object.Position + new Vector3(15, 0, 0), ThraxId, ThraxClass, TargetCategory.Hostile);
            _tick += 5_000;
            BaneRaids.Instance.Worker(world.Map);
            Assert.IsNull(raid.Claimer, "a Bane at the point again: interrupted");

            back.State = CharacterState.Dead;
            BaneRaids.Instance.Worker(world.Map);
            Assert.AreSame(soldier, raid.Claimer, "clear again: it starts again");

            _tick += 14_000;
            BaneRaids.Instance.Worker(world.Map);
            Assert.IsFalse(point.HeldByAfs, "not fifteen seconds yet");

            _tick += 1_000;
            BaneRaids.Instance.Worker(world.Map);
            Assert.IsTrue(point.HeldByAfs, "fifteen seconds at it: the AFS's again");
            Assert.IsNull(BaneRaids.Instance.Of(point.Id), "the raid is over");
            Assert.IsFalse(garrison.Besieged, "the siege lifted with it");
            Assert.AreEqual(1, BaneRaids.Instance.Holders(world.Map, point, ControlPoints.Afs), "its soldier holds the point");
            Assert.AreEqual(0, BaneRaids.Instance.Holders(world.Map, point, ControlPoints.Bane), "no Bane of it");
            Assert.IsTrue(world.Map.SpawnPools.Single(p => p.DbId == AfsPool).ShipNextArrival, "the AFS garrison is flown in");

            BaneRaids.Instance.Worker(world.Map);
            Assert.AreEqual(_tick + 8 * 60_000, BaneRaids.Instance.NextAt(point.Id), "the Bane come back for it eight minutes later");

            ControlPoints.Instance.SetOwner(point, ControlPoints.Bane, null);
            BaneRaids.Instance.Worker(world.Map);
            Assert.AreEqual(0, BaneRaids.Instance.Holders(world.Map, point, ControlPoints.Afs), "the Bane took it again: the soldiers who held it are gone");
            Assert.AreEqual(_tick + 30 * 60_000, BaneRaids.Instance.NextAt(point.Id));
        }

        [TestMethod]
        public void ARaidsKillsAreThePointsMinions()
        {
            using var world = new WorldTestContext();
            var point = Placed(world, ownGarrison: true);
            var client = world.CreateClient(At.X + 50, At.Z);
            var raid = BaneRaids.Instance.Begin(world.Map, point);
            var raider = Raider(world, raid, point.Object.Position + new Vector3(30, 0, 0));
            var change = PvpPrestige.Change;
            var sharers = ControlPoints.Instance.SharersOf;
            var given = 0;

            PvpPrestige.Change = (c, amount) => { given += amount; return true; };
            ControlPoints.Instance.SharersOf = (killer, corpse) => new List<Client> { killer };

            try
            {
                Assert.AreEqual(ControlPoints.MinionPrestige, ControlPoints.Instance.CreatureKilled(raider, client), "a raider is worth a minion's prestige");
                Assert.AreEqual(ControlPoints.MinionPrestige, given);
            }
            finally
            {
                PvpPrestige.Change = change;
                ControlPoints.Instance.SharersOf = sharers;
            }
        }

        [TestMethod]
        public void AShipWithNoRoomWaitsAndTheRaidIsCalledOffOnlyIfNoneCame()
        {
            using var world = new WorldTestContext();
            var point = Placed(world, ownGarrison: true);
            world.CreateClient(At.X + 50, At.Z);

            // Every spot round the point taken by a ship of somebody else's that stays: no room anywhere.
            var blockers = new List<Dropship>();

            for (var x = -140; x <= 140; x += 30)
                for (var z = -140; z <= 140; z += 30)
                {
                    var ship = new Dropship(TargetCategory.Hostile, DropshipType.Spawner, world.Map.SpawnPools.Single(p => p.DbId == CampPool))
                    {
                        Position = At + new Vector3(x, 0, z), RuntimeMapChannel = world.Map
                    };

                    DynamicObjectManager.Instance.Dropships.Add(ship.EntityId, ship);
                    blockers.Add(ship);
                }

            try
            {
                var raid = BaneRaids.Instance.Begin(world.Map, point);

                BaneRaids.Instance.Worker(world.Map);
                Assert.AreEqual(1, raid.Wave);
                Assert.AreEqual(1, raid.ShipsOwed, "its one ship waits for room");
                Assert.AreEqual(0, raid.Pool.DropshipQueue, "rather than being set down far off");

                _tick += BaneRaids.ShipOwedMs + 1;
                BaneRaids.Instance.Worker(world.Map);
                Assert.AreEqual(0, raid.ShipsOwed, "given up");
                Assert.AreEqual(raid.Waves, raid.Wave, "no ship of the wave came down: the raid is called off");

                BaneRaids.Instance.Worker(world.Map);
                Assert.IsNull(BaneRaids.Instance.Of(point.Id), "and over");
                Assert.IsTrue(point.HeldByAfs);
            }
            finally
            {
                foreach (var ship in blockers)
                {
                    DynamicObjectManager.Instance.Dropships.Remove(ship.EntityId);
                    EntityManager.Instance.FreeEntity(ship.EntityId);
                }
            }
        }

        [TestMethod]
        public void ARaidsShipsComeDownBeforeTheGatesAndItsRaidersStrikeTheShields()
        {
            using var world = new WorldTestContext();
            var point = Placed(world, ownGarrison: true);
            world.CreateClient(At.X + 50, At.Z);

            // The outpost's two gates, 40 m south and 40 m east of the point, each closed by an AFS shield.
            var south = ForceFields.Place(world.Map, ForceFields.ClassOf("humgate"), ForceFields.Side.A, At + new Vector3(0, 0, -40), 0f, 100);
            var east = ForceFields.Place(world.Map, ForceFields.ClassOf("humgate"), ForceFields.Side.A, At + new Vector3(40, 0, 0), (float)(Math.PI / 2), 100);
            var struck = new List<(Creature, ForceFields.Field)>();
            var strike = BaneRaids.Instance.StrikeShield;

            BaneRaids.Instance.StrikeShield = (map, creature, field) => { struck.Add((creature, field)); return true; };

            try
            {
                Assert.AreEqual(2, OutpostWalls.GatesOf(point).Count, "the shields within 120 m are its gates");

                var raid = BaneRaids.Instance.Begin(world.Map, point);

                BaneRaids.Instance.Worker(world.Map);

                var ship = DynamicObjectManager.Instance.Dropships.Values.Single(s => s.SpawnPool == raid.Pool);
                var outFromSouth = Vector3.Distance(ship.Position, At + new Vector3(0, 0, -66));
                var outFromEast = Vector3.Distance(ship.Position, At + new Vector3(66, 0, 0));

                Assert.IsTrue(Math.Min(outFromSouth, outFromEast) < 0.5f, $"26 m straight out before a gate: {ship.Position}");

                // The next ship of the raid at the other gate.
                Assert.AreEqual(outFromSouth < 0.5f ? new Vector3(166, 0, 100) : new Vector3(100, 0, 34),
                    BaneRaids.GateLanding(raid), "the next gate round");

                var atTheGate = Raider(world, raid, At + new Vector3(0, 0, -46));
                var faraway = Raider(world, raid, At + new Vector3(-60, 0, -60));

                BaneRaids.Instance.Worker(world.Map);

                CollectionAssert.AreEqual(new List<(Creature, ForceFields.Field)> { (atTheGate, south) }, struck,
                    "the raider held back at the south gate strikes its shield; the one far off does not");

                struck.Clear();
                ForceFields.Damage(south, 100, 0);
                BaneRaids.Instance.Worker(world.Map);
                Assert.AreEqual(0, struck.Count, "the shield is down: nothing to strike");
            }
            finally
            {
                BaneRaids.Instance.StrikeShield = strike;
                ForceFields.Remove(south);
                ForceFields.Remove(east);
            }
        }

        [TestMethod]
        public void AGameMasterBeginsWatchesAndStopsARaid()
        {
            using var world = new WorldTestContext();
            var point = Placed(world, ownGarrison: true);
            var client = world.CreateClient(At.X + 500, At.Z);
            typeof(Client).GetProperty(nameof(Client.AccountEntry))!.SetValue(client,
                new Rasa.Structures.Char.GameAccountEntry { Id = 1, Level = (byte)GmLevel.GameMaster });

            var commands = new ChatCommandsManager(null);
            commands.RegisterChatCommands();

            List<string> Say(string command)
            {
                MissionTestContext.Drain(client);
                commands.ProcessCommand(client, command);
                return MissionTestContext.Drain(client).OfType<SystemMessagePacket>().Select(m => m.TextMessage).ToList();
            }

            BaneRaids.Instance.Worker(world.Map);
            Assert.IsTrue(Say(".cp raids").Any(line => line.StartsWith("#4 Landing Zone: AFS, next Bane raid in ")), "when the next is due");

            Assert.IsTrue(Say(".cp 4 raid").Single().StartsWith("The Bane raid control point #4 Landing Zone: "), "begun by hand, nobody near");
            Assert.IsNotNull(BaneRaids.Instance.Of(point.Id));

            BaneRaids.Instance.Worker(world.Map);
            Raider(world, BaneRaids.Instance.Of(point.Id), At + new Vector3(20, 0, 0));
            Assert.IsTrue(Say(".raids").Any(line => line.Contains("Bane raid, wave 1/") && line.Contains(" 1 alive")), "its wave and its raiders");
            Assert.AreEqual(2, Say(".cp 4 raid who").Count, "a heading and the one raider");

            Say(".cp 4 raid in 2");
            Assert.AreEqual(_tick + 120_000, BaneRaids.Instance.NextAt(point.Id));

            Assert.IsTrue(Say(".cp 4 raid stop").Single().Contains("is over"));
            Assert.IsNull(BaneRaids.Instance.Of(point.Id));
            Assert.IsTrue(Say(".cp 4 raid stop").Single().Contains("is not being raided"));

            var said = Say($".dropship {SoldierId} 2");
            Assert.IsTrue(said.Single().StartsWith("An AFS dropship"), string.Join(" | ", said));
            var ship = DynamicObjectManager.Instance.Dropships.Values.Single(s => s.SpawnPool != null && s.SpawnPool.SpawnSlot.Any(slot => slot.CreatureId == SoldierId) && Reinforcements.IsTransient(s.SpawnPool));
            Assert.AreEqual(EntityClasses.UsableCrSpawnerHumDropshipV01, ship.EntityClassId, "the soldier's side's ship");
            Assert.AreEqual(2, ship.SpawnPool.QueuedCreatures);
            Assert.IsTrue(Vector3.Distance(ship.Position, client.Player.Position) <= GmRaidCommands.DropAhead + GmRaidCommands.DropSearch + 1);
        }

        #region Fixtures

        private void Class(EntityClasses id, string name)
        {
            var classes = EntityClassManager.Instance.LoadedEntityClasses;

            if (classes.ContainsKey(id))
                return;

            classes.Add(id, new EntityClass((uint)id, name, 0, 0, new List<AugmentationType> { AugmentationType.Creature }, true));
            _classes.Add(id);
        }

        /// <summary>The point, the AFS's, set down on the world's map at <see cref="At"/>, with a Bane garrison of Thrax of its own or none, and a Thrax camp 60 m off.</summary>
        private static ControlPoints.Point Placed(WorldTestContext world, bool ownGarrison)
        {
            if (ownGarrison)
                world.Map.SpawnPools.Add(Pool(world, BanePool, SpawnPoolManager.ModeControlPoint, At));

            world.Map.SpawnPools.Add(Pool(world, AfsPool, SpawnPoolManager.ModeAutomatic, At + new Vector3(5, 0, 0)));
            world.Map.SpawnPools.Add(Pool(world, CampPool, SpawnPoolManager.ModeAutomatic, At + new Vector3(60, 0, 0)));

            var links = new List<ControlPointLinkEntry>
            {
                new ControlPointLinkEntry { ControlPointId = PointId, Kind = ControlPointLinkEntry.KindAfsPool, ObjectId = AfsPool }
            };

            if (ownGarrison)
                links.Add(new ControlPointLinkEntry { ControlPointId = PointId, Kind = ControlPointLinkEntry.KindBanePool, ObjectId = BanePool });

            ControlPoints.Instance.Load(new[]
            {
                new ControlPointEntry
                {
                    Id = PointId, MapContextId = MapId, Name = "Landing Zone", ClassId = 3814,
                    PosX = At.X, PosY = At.Y, PosZ = At.Z, Rotation = 0, MarkerEntityId = 0, DefaultOwner = ControlPointEntry.OwnerAfs
                }
            }, links, null);
            ControlPoints.Instance.Place(world.Map);

            var point = ControlPoints.Instance.ById(PointId);

            // As the object worker would have it: on the map.
            point.Object.IsInWorld = true;

            return point;
        }

        private static SpawnPool Pool(WorldTestContext world, uint id, short mode, Vector3 at) => new SpawnPool
        {
            DbId = id,
            Mode = mode,
            MapContextId = MapId,
            RuntimeMapChannel = world.Map,
            Position = at,
            Radius = 10,
            RespawnTime = 90_000,
            SpawnSlot = new List<SpawnPoolSlot> { new SpawnPoolSlot(id == AfsPool ? SoldierId : ThraxId, 1, 3) }
        };

        /// <summary>A creature with something to fight with.</summary>
        private static Creature Armed(Creature creature)
        {
            creature.Actions.Add(new CreatureAction());
            return creature;
        }

        /// <summary>Whom a creature fights; 0 for nobody.</summary>
        private static ulong Target(Creature creature) =>
            creature.Controller?.CurrentAction == BehaviorManager.BehaviorActionFighting ? creature.Controller.ActionFighting.TargetEntityId : 0;

        /// <summary>One of the raid, standing at a spot, on the map.</summary>
        private static Creature Raider(WorldTestContext world, BaneRaids.Raid raid, Vector3 at) =>
            Standing(world, raid.Pool, at, ThraxId, ThraxClass, TargetCategory.Hostile);

        /// <summary>One of a pool's, standing at a spot, on the map.</summary>
        private static Creature Standing(WorldTestContext world, SpawnPool pool, Vector3 at, uint dbId, EntityClasses entityClass, TargetCategory side)
        {
            var creature = new Creature
            {
                DbId = dbId,
                EntityClass = entityClass,
                TargetCategory = side,
                SpawnPool = pool,
                Position = at,
                State = CharacterState.Idle,
                WalkSpeed = 5,
                RunSpeed = 9
            };

            creature.Attributes.Add(Attributes.Health, new ActorAttributes(Attributes.Health, 100, 100, 100, 0, 0));
            creature.Attributes.Add(Attributes.Armor, new ActorAttributes(Attributes.Armor, 50, 50, 50, 0, 0));
            BehaviorManager.StartWandering(creature, false);
            pool.AliveCreatures++;
            CellManager.Instance.AddToWorld(world.Map, creature);

            return creature;
        }

        #endregion
    }
}
