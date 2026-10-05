using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;

using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Rasa.Config;
    using Rasa.Context.Char;
    using Rasa.Context.World;
    using Rasa.Data;
    using Rasa.Game;
    using Rasa.Managers;
    using Rasa.Packets;
    using Rasa.Packets.Clan.Server;
    using Rasa.Packets.Communicator.Server;
    using Rasa.Packets.Game.Server;
    using Rasa.Packets.MapChannel.Client;
    using Rasa.Packets.MapChannel.Server;
    using Rasa.Repositories.Char.ControlPointState;
    using Rasa.Repositories.Char;
    using Rasa.Repositories.UnitOfWork;
    using Rasa.Repositories.World;
    using Rasa.Structures;
    using Rasa.Structures.Char;
    using Rasa.Structures.World;
    using Rasa.Test.Database;
    using Rasa.Test.Missions;

    // Clan-owned control points (ControlPoints): a point taken from the Bane by a clan's member
    // is the clan's; what its object is while a clan holds it and what each client is told of
    // it; a clan at feud taking it; the Bane, a disbanding, the weekly reset and the setting
    // taking it away; its pay; its lockbox; and what is kept.
    [TestClass]
    [DoNotParallelize]
    public class ClanOwnedControlPointTests
    {
        private const uint MapId = 1220;    // WorldTestContext's map
        private const uint PointId = 7;
        private const uint BanePool = 9101;
        private const uint AfsPool = 9201;
        private const uint LockboxRow = 9501;
        private const uint Red = 11, Blue = 12, Green = 13;
        private const ulong MarkerId = 133079561962676UL;
        private const long Minute = 60_000L, Hour = 60 * Minute, Day = 24 * Hour;

        private static readonly EntityClasses[] Classes =
        {
            (EntityClasses)3814, ControlPoints.ClanPointClass, EntityClasses.UsableClanLockboxV01
        };

        private readonly Dictionary<uint, string> _clans = new Dictionary<uint, string>();
        private readonly HashSet<(uint, uint)> _feuds = new HashSet<(uint, uint)>();
        private readonly List<(uint Clan, int Amount, string Point)> _paid = new List<(uint, int, string)>();
        private readonly Dictionary<EntityClasses, EntityClass> _classesBefore = new Dictionary<EntityClasses, EntityClass>();
        private readonly List<MapChannel> _maps = new List<MapChannel>();

        private Func<uint, bool> _known;
        private Func<long> _now;
        private Func<long> _utcNow;
        private Func<uint, string> _clanName;
        private Func<uint, uint, bool> _atFeud;
        private Func<uint, int, string, bool> _payClan;
        private Func<DateTime> _wallClock;
        private Func<SquadInstanceConfig> _schedule;
        private ControlPointConfig _config;

        private long _utc;
        private DateTime _wall;

        [TestInitialize]
        public void Start()
        {
            var points = ControlPoints.Instance;

            _known = points.KnownCreature;
            _now = points.Now;
            _utcNow = points.UtcNow;
            _clanName = points.ClanName;
            _atFeud = points.AtFeud;
            _payClan = points.PayClan;
            _wallClock = points.WallClock;
            _schedule = points.ResetSchedule;
            _config = points.Config;

            _utc = 1_800_000_000_000;
            _wall = new DateTime(2026, 10, 5, 12, 0, 0);    // a Monday: the reset is Tuesday at 03:00

            _clans[Red] = "Red Company";
            _clans[Blue] = "Blue Company";
            _clans[Green] = "Green Company";

            points.KnownCreature = id => true;
            points.Now = () => 1_000_000;
            points.UtcNow = () => _utc;
            points.ClanName = id => _clans.TryGetValue(id, out var name) ? name : null;
            points.AtFeud = (a, b) => _feuds.Contains((Math.Min(a, b), Math.Max(a, b)));
            points.PayClan = (clan, amount, point) =>
            {
                _paid.Add((clan, amount, point));
                return true;
            };
            points.WallClock = () => _wall;
            points.ResetSchedule = () => new SquadInstanceConfig();
            points.Config = new ControlPointConfig();

            // The classes as the client has them: the clan control point by its augmentation.
            var loaded = EntityClassManager.Instance.LoadedEntityClasses;

            foreach (var id in Classes)
            {
                _classesBefore[id] = loaded.TryGetValue(id, out var was) ? was : null;
                loaded[id] = new EntityClass((uint)id, "fixture", 0, 0,
                    id == ControlPoints.ClanPointClass ? new List<AugmentationType> { AugmentationType.ClanControlPoint } : new List<AugmentationType>(), true);
            }
        }

        [TestCleanup]
        public void Restore()
        {
            var points = ControlPoints.Instance;

            foreach (var map in _maps)
            {
                map.PerformRecovery.Clear();

                foreach (var point in points.OnMap(MapId))
                    foreach (var obj in new[] { point.Object, point.Lockbox })
                        if (obj != null && obj.IsInWorld)
                            CellManager.Instance.RemoveFromWorld(map, obj);

                map.MapCellInfo.Cells.Clear();
            }

            points.Load(new List<ControlPointEntry>(), new List<ControlPointLinkEntry>(), null);
            points.KnownCreature = _known;
            points.Now = _now;
            points.UtcNow = _utcNow;
            points.ClanName = _clanName;
            points.AtFeud = _atFeud;
            points.PayClan = _payClan;
            points.WallClock = _wallClock;
            points.ResetSchedule = _schedule;
            points.Config = _config;

            foreach (var (id, was) in _classesBefore)
                if (was == null)
                    EntityClassManager.Instance.LoadedEntityClasses.Remove(id);
                else
                    EntityClassManager.Instance.LoadedEntityClasses[id] = was;
        }

        #region Taking a point

        [TestMethod]
        public void APointTakenFromTheBaneByAClansMemberIsTheClans()
        {
            using var world = new WorldTestContext();
            var store = new MemoryStore();
            var point = Placed(world, ControlPointEntry.OwnerBane, store);
            var before = point.Object;
            var member = Member(world, 10, 10, Red);

            Assert.AreEqual((EntityClasses)3814, before.EntityClassId);

            BaneDown(world);
            Drain(member);

            Assert.IsTrue(ControlPoints.Instance.Captured(world.Map, member, point));

            Assert.IsTrue(point.HeldByAfs, "it is the AFS's point, held for them");
            Assert.IsTrue(point.HeldByClan);
            Assert.AreEqual(Red, point.ClanId);
            Assert.AreEqual(_utc, point.ChangedAt);
            Assert.AreEqual(_utc, point.PaidAt, "its pay counts from now");

            var saved = store.Saved.Single();
            Assert.AreEqual((PointId, ControlPoints.Afs, _utc, Red, _utc), (saved.ControlPointId, saved.Owner, saved.ChangedAt, saved.ClanId, saved.ClanPaidAt));

            // The object is another: of the clan class, where the first stood.
            Assert.AreNotSame(before, point.Object);
            Assert.AreNotEqual(before.EntityId, point.Object.EntityId);
            Assert.AreEqual(ControlPoints.ClanPointClass, point.Object.EntityClassId);
            Assert.AreEqual(UseObjectState.CcpStateClanControlled, point.Object.StateId);
            Assert.AreEqual(ControlPoints.ClanPointNameOverride, point.Object.NameOverrideId);
            Assert.AreEqual(30_000u, point.Object.WindupTime);
            Assert.AreEqual(before.Position, point.Object.Position);
            Assert.IsTrue(point.Object.IsInWorld);
            Assert.AreSame(point.Object, world.Map.ControlPoints[PointId]);
            Assert.AreSame(point, ControlPoints.Instance.PointOf(point.Object));
            Assert.IsNull(ControlPoints.Instance.PointOf(before));
            Assert.IsFalse(EntityManager.Instance.TryGetObject(before.EntityId, out _), "the first is gone");

            // The AFS's garrison, and the Bane's sent away.
            Assert.IsTrue(Pool(world, BanePool).Suspended);
            Assert.IsFalse(Pool(world, AfsPool).Suspended);

            var packets = Drain(member);
            Assert.AreEqual(before.EntityId, packets.OfType<DestroyPhysicalEntityPacket>().Single().EntityId);

            var made = packets.OfType<CreatePhysicalEntityPacket>().Single();
            Assert.AreEqual(point.Object.EntityId, made.EntityId);
            Assert.AreEqual(ControlPoints.ClanPointClass, made.ClassId);

            var owned = packets.OfType<DisplayClientMessagePacket>().Single(m => m.MsgId == PlayerMessage.PmControlpointOwned);
            Assert.AreEqual("Red Company", owned.Args["faction"]);
            Assert.AreEqual("Retread Outpost", owned.Args["cpName"]);

            var marker = packets.OfType<UpdateMapMarkerPacket>().Single(m => m.MarkerEntityId == MarkerId);
            CollectionAssert.AreEqual(new byte[] { 0x82, 0x16, 0x01 }, Encode(marker.State), "(FACTION_OWNED, True): the map has no text for a clan's");

            Assert.AreEqual(0, packets.OfType<ForceStatePacket>().Count(), "the new object comes in its state");
        }

        [TestMethod]
        public void APointTakenByAPlayerInNoClanIsTheAfssAsBefore()
        {
            using var world = new WorldTestContext();
            var store = new MemoryStore();
            var point = Placed(world, ControlPointEntry.OwnerBane, store);
            var before = point.Object;
            var loner = Member(world, 10, 10, 0);

            BaneDown(world);
            Drain(loner);

            Assert.IsTrue(ControlPoints.Instance.Captured(world.Map, loner, point));

            Assert.IsTrue(point.HeldByAfs);
            Assert.IsFalse(point.HeldByClan);
            Assert.AreSame(before, point.Object, "the object it always was");
            Assert.AreEqual(UseObjectState.CpointStateFactionAOwned, point.Object.StateId);
            Assert.AreEqual(0u, point.Object.NameOverrideId);
            Assert.AreEqual(0u, store.Saved.Single().ClanId);

            var packets = Drain(loner);
            Assert.AreEqual("AFS", packets.OfType<DisplayClientMessagePacket>().Single(m => m.MsgId == PlayerMessage.PmControlpointOwned).Args["faction"]);
            Assert.AreEqual(UseObjectState.CpointStateFactionAOwned, packets.OfType<ForceStatePacket>().Single().State);
            Assert.AreEqual(0, packets.OfType<CreatePhysicalEntityPacket>().Count());
        }

        [TestMethod]
        public void WithClanOwnershipOffEveryCaptureIsTheAfssAndAClansPointGoesBack()
        {
            using var world = new WorldTestContext();
            var point = Placed(world, ControlPointEntry.OwnerBane);
            var member = Member(world, 10, 10, Red);

            BaneDown(world);
            ControlPoints.Instance.Config = new ControlPointConfig { ClanOwnership = false };

            Assert.IsTrue(ControlPoints.Instance.Captured(world.Map, member, point));
            Assert.IsTrue(point.HeldByAfs);
            Assert.IsFalse(point.HeldByClan);

            // One a clan had from before the setting was changed.
            ControlPoints.Instance.Config = new ControlPointConfig();
            Assert.IsTrue(ControlPoints.Instance.SetHolder(point, ControlPoints.Afs, Red, null));
            Assert.IsTrue(point.HeldByClan);

            ControlPoints.Instance.Config = new ControlPointConfig { ClanOwnership = false };
            ControlPoints.Instance.ClanWorker();

            Assert.IsFalse(point.HeldByClan);
            Assert.IsTrue(point.HeldByAfs);
            Assert.AreEqual((EntityClasses)3814, point.Object.EntityClassId);
            Assert.AreEqual(0, _paid.Count);
        }

        [TestMethod]
        public void AClansPointIsMadeOnAClientWithItsOwnerAheadOfItsState()
        {
            using var world = new WorldTestContext();
            var point = Held(world, Red);

            _feuds.Add((Red, Blue));

            var owner = Arrive(world, 11, 10, Red);
            var enemy = Arrive(world, 12, 10, Blue);
            var other = Arrive(world, 13, 10, Green);
            var loner = Arrive(world, 14, 10, 0);

            foreach (var (client, usable) in new[] { (owner, false), (enemy, true), (other, false), (loner, false) })
            {
                var made = Drain(client).OfType<CreatePhysicalEntityPacket>().Single(p => p.EntityId == point.Object.EntityId);
                var data = made.EntityData;

                Assert.AreEqual(ControlPoints.ClanPointClass, made.ClassId);

                var association = data.OfType<ClanAssociationPacket>().Single();
                var info = data.OfType<UsableInfoPacket>().Single();

                Assert.AreEqual((int)Red, association.OwningClanId);
                Assert.IsTrue(data.IndexOf(association) < data.IndexOf(info), "the owner first: the state takes its effect");
                Assert.AreEqual(UseObjectState.CcpStateClanControlled, info.CurState);
                Assert.AreEqual(ControlPoints.ClanPointNameOverride, info.NameOverrideId, "Central Dispatch Unit - Control Point");
                Assert.AreEqual(30_000u, info.WindupTime);
                Assert.AreEqual(usable, info.Enabled, "in service to a clan at feud with the one that holds it, and nobody else");
                Assert.IsFalse(data.OfType<PvPEnabledPacket>().Single().IsPvPEnabled);
            }
        }

        [TestMethod]
        public void AnAfsOrBanePointIsToldNoClan()
        {
            using var world = new WorldTestContext();
            var point = Placed(world, ControlPointEntry.OwnerBane);
            var client = Arrive(world, 11, 10, Red);

            var made = Drain(client).OfType<CreatePhysicalEntityPacket>().Single(p => p.EntityId == point.Object.EntityId);

            Assert.AreEqual((EntityClasses)3814, made.ClassId);
            Assert.AreEqual(0, made.EntityData.OfType<ClanAssociationPacket>().Count());
            Assert.AreEqual(0u, made.EntityData.OfType<UsableInfoPacket>().Single().NameOverrideId);
            Assert.IsNull(ControlPoints.Instance.ClanShownBy(point.Object));
            Assert.IsNull(ControlPoints.Instance.ShownUsable(client, point.Object));
        }

        #endregion

        #region A clan at feud

        [TestMethod]
        public void OnlyAClanAtFeudWithTheHolderMayTakeAClansPoint()
        {
            using var world = new WorldTestContext();
            var point = Held(world, Red);
            var owner = Member(world, 10, 10, Red);
            var enemy = Member(world, 11, 10, Blue);
            var other = Member(world, 12, 10, Green);
            var loner = Member(world, 13, 10, 0);

            _feuds.Add((Red, Blue));

            Assert.IsFalse(ControlPoints.Instance.MayCapture(world.Map, point, owner), "it is theirs");
            Assert.IsTrue(ControlPoints.Instance.MayCapture(world.Map, point, enemy));
            Assert.IsFalse(ControlPoints.Instance.MayCapture(world.Map, point, other), "a clan at feud with nobody");
            Assert.IsFalse(ControlPoints.Instance.MayCapture(world.Map, point, loner));
            Assert.IsFalse(ControlPoints.Instance.MayCapture(world.Map, point), "the Bane do not hold it");

            foreach (var client in new[] { owner, other, loner })
            {
                Drain(client);
                Request(client, point.Object);

                Assert.AreEqual(PlayerMessage.PmUseObjectNotUsable, Drain(client).OfType<UserActionFailedPacket>().Single().MsgId);
                Assert.AreEqual(0, world.Map.PerformRecovery.Count);
                Assert.IsNull(point.Object.UsedBy);
            }

            Assert.IsFalse(ControlPoints.Instance.Captured(world.Map, other, point));
            Assert.AreEqual(Red, point.ClanId);
        }

        [TestMethod]
        public void AClaimOfAClansPointIsTheLockWithTheClaimantsClanAndTakesTheClansTime()
        {
            using var world = new WorldTestContext();
            var point = Held(world, Red);
            var owner = Member(world, 10, 10, Red);
            var enemy = Member(world, 11, 10, Blue);

            _feuds.Add((Red, Blue));
            Drain(owner);
            Drain(enemy);

            Request(enemy, point.Object);

            var action = world.Map.PerformRecovery.Single();
            Assert.AreSame(enemy.Player, action.Actor);
            Assert.AreEqual(point.Object.EntityId, action.SourceId);
            Assert.AreEqual(30_000L, action.WaitTime, "ClanCaptureSeconds, which the object has told the client");
            Assert.AreSame(enemy.Player, point.Object.UsedBy);

            foreach (var client in new[] { owner, enemy })
            {
                var packets = Drain(client);

                Assert.AreEqual(enemy.Player.EntityId, packets.OfType<LockToActorPacket>().Single().ActorId);

                var use = packets.OfType<UseInterruptiblePacket>().Single();
                Assert.AreEqual(enemy.Player.EntityId, use.ActorId);
                Assert.AreEqual((int)Blue, use.UsingClanId, "the clan class takes the clan that is at it, or fails");

                var claiming = packets.OfType<DisplayClientMessagePacket>().Single(m => m.MsgId == PlayerMessage.PmControlpointClaiming);
                Assert.AreEqual("Blue Company", claiming.Args["faction"]);
            }

            // Somebody who comes into view partway through is told the same.
            var late = Arrive(world, 12, 10, Green);
            var shown = Drain(late);

            Assert.AreEqual((int)Blue, shown.OfType<UseInterruptiblePacket>().Single().UsingClanId);
        }

        [TestMethod]
        public void AClaimRunToItsEndGivesTheOtherClanThePointOnTheObjectItIs()
        {
            using var world = new WorldTestContext();
            var store = new MemoryStore();
            var point = Held(world, Red, store);
            var obj = point.Object;
            var owner = Member(world, 10, 10, Red);
            var enemy = Member(world, 11, 10, Blue);

            _feuds.Add((Red, Blue));
            ControlPoints.Instance.Worker(world.Map);

            Request(enemy, obj);
            _utc += 30_000;
            store.Saved.Clear();
            Finish(world, owner, enemy);

            Assert.AreEqual(Blue, point.ClanId);
            Assert.IsTrue(point.HeldByClan);
            Assert.AreSame(obj, point.Object, "the class is the same: the object stays");
            Assert.IsNull(obj.UsedBy);
            Assert.AreEqual(_utc, point.ChangedAt);
            Assert.AreEqual(_utc, point.PaidAt);
            Assert.AreEqual((Blue, _utc), (store.Saved.Single().ClanId, store.Saved.Single().ChangedAt));

            foreach (var client in new[] { owner, enemy })
            {
                var packets = Drain(client);
                var use = packets.OfType<UsePacket>().Single();

                Assert.AreEqual(enemy.Player.EntityId, use.PlayerEntityId);
                Assert.AreEqual(UseObjectState.CcpStateClanControlled, use.CurState, "to the state it is in: made over with the new owner's effect");
                Assert.AreEqual((int)Blue, use.OwningClanId);
                Assert.AreEqual(30_000, use.WindupTimeMs);
                Assert.AreEqual(0UL, packets.OfType<LockToActorPacket>().Last().ActorId);
                Assert.IsTrue(packets.IndexOf(packets.OfType<LockToActorPacket>().Last()) < packets.IndexOf(use), "let go before it changes");
                Assert.AreEqual("Blue Company", packets.OfType<DisplayClientMessagePacket>().Single(m => m.MsgId == PlayerMessage.PmControlpointOwned).Args["faction"]);
                Assert.AreEqual(0, packets.OfType<UpdateMapMarkerPacket>().Count(), "the AFS's still");
                Assert.AreEqual(0, packets.OfType<CreatePhysicalEntityPacket>().Count(p => p.EntityId == obj.EntityId));

                // Out of service to the clan that has it now, and in service to the one that lost it.
                Assert.AreEqual(client == owner, packets.OfType<SetUsablePacket>().Single().IsEnabled);
            }

            Assert.IsTrue(ControlPoints.Instance.MayCapture(world.Map, point, owner));
            Assert.IsFalse(ControlPoints.Instance.MayCapture(world.Map, point, enemy));

            ControlPoints.Instance.Worker(world.Map);
            Assert.AreEqual(0, Drain(owner).Concat(Drain(enemy)).OfType<SetUsablePacket>().Count(), "told once");
        }

        [TestMethod]
        public void AClaimWhoseFeudEndedBeforeItsTimeWasUpComesToNothing()
        {
            using var world = new WorldTestContext();
            var point = Held(world, Red);
            var owner = Member(world, 10, 10, Red);
            var enemy = Member(world, 11, 10, Blue);

            _feuds.Add((Red, Blue));
            Request(enemy, point.Object);
            _feuds.Clear();
            Finish(world, owner, enemy);

            Assert.AreEqual(Red, point.ClanId);
            Assert.IsNull(point.Object.UsedBy);
            Assert.AreEqual(0, Drain(owner).OfType<UsePacket>().Count());
        }

        [TestMethod]
        public void EachClientIsToldWhenTheServiceChangesForItAndNotOtherwise()
        {
            using var world = new WorldTestContext();
            var point = Held(world, Red);
            var owner = Member(world, 10, 10, Red);
            var enemy = Member(world, 11, 10, Blue);

            ControlPoints.Instance.Worker(world.Map);
            Assert.AreEqual(0, Drain(enemy).OfType<SetUsablePacket>().Count(), "out of service, as it was made");

            _feuds.Add((Red, Blue));
            ControlPoints.Instance.Worker(world.Map);
            Assert.IsTrue(Drain(enemy).OfType<SetUsablePacket>().Single().IsEnabled, "the feud has begun");
            Assert.AreEqual(0, Drain(owner).OfType<SetUsablePacket>().Count());

            ControlPoints.Instance.Worker(world.Map);
            Assert.AreEqual(0, Drain(enemy).OfType<SetUsablePacket>().Count(), "told once");

            // They leave their clan for one at feud with nobody.
            enemy.Player.ClanId = Green;
            ControlPoints.Instance.Worker(world.Map);
            Assert.IsFalse(Drain(enemy).OfType<SetUsablePacket>().Single().IsEnabled);

            // The owner's member changes sides.
            owner.Player.ClanId = Blue;
            ControlPoints.Instance.Worker(world.Map);
            Assert.IsTrue(Drain(owner).OfType<SetUsablePacket>().Single().IsEnabled);

            _feuds.Clear();
            ControlPoints.Instance.Worker(world.Map);
            Assert.IsFalse(Drain(owner).OfType<SetUsablePacket>().Single().IsEnabled, "the feud is over");
        }

        #endregion

        #region Losing a point

        [TestMethod]
        public void TheBaneTakeAClansPointAsTheyTakeAnyOfTheAfss()
        {
            using var world = new WorldTestContext();
            var store = new MemoryStore();
            var point = Held(world, Red, store);
            var clans = point.Object;
            var member = Member(world, 10, 10, Red);

            Spawned(world, AfsPool, alive: 2);
            ControlPoints.Instance.Worker(world.Map);
            Assert.IsTrue(point.HeldByClan);

            Spawned(world, AfsPool, alive: 0);
            store.Saved.Clear();
            Drain(member);
            ControlPoints.Instance.Worker(world.Map);

            Assert.IsFalse(point.HeldByAfs);
            Assert.IsFalse(point.HeldByClan);
            Assert.AreEqual(0u, point.ClanId);
            Assert.AreEqual(0, point.PaidAt);
            Assert.AreEqual((ControlPoints.Bane, 0u, 0L), (store.Saved.Single().Owner, store.Saved.Single().ClanId, store.Saved.Single().ClanPaidAt));

            Assert.AreNotSame(clans, point.Object);
            Assert.AreEqual((EntityClasses)3814, point.Object.EntityClassId);
            Assert.AreEqual(UseObjectState.CpointStateFactionBOwned, point.Object.StateId);
            Assert.AreEqual(0u, point.Object.NameOverrideId);
            Assert.AreEqual(ControlPoints.CaptureMs, point.Object.WindupTime);
            Assert.IsFalse(point.Object.IsEnabled, "until its garrison is down");
            Assert.IsTrue(point.Object.IsInWorld);

            var packets = Drain(member);
            Assert.AreEqual("Bane", packets.OfType<DisplayClientMessagePacket>().Single(m => m.MsgId == PlayerMessage.PmControlpointOwned).Args["faction"]);
            Assert.AreEqual(clans.EntityId, packets.OfType<DestroyPhysicalEntityPacket>().Single().EntityId);
            Assert.AreEqual((EntityClasses)3814, packets.OfType<CreatePhysicalEntityPacket>().Single().ClassId);
            CollectionAssert.AreEqual(new byte[] { 0x82, 0x16, 0x02 }, Encode(packets.OfType<UpdateMapMarkerPacket>().Single(m => m.MarkerEntityId == MarkerId).State), "(FACTION_OWNED, False)");

            // And a clan's member may take it back from them, for the clan.
            BaneDown(world);
            Assert.IsTrue(ControlPoints.Instance.Captured(world.Map, member, point));
            Assert.AreEqual(Red, point.ClanId);
        }

        [TestMethod]
        public void AClanThatDisbandsOrIsNoMoreLosesItsPointsToTheAfs()
        {
            using var world = new WorldTestContext();
            var store = new MemoryStore();
            var point = Held(world, Red, store);
            var member = Member(world, 10, 10, Red);

            Assert.AreEqual(0, ControlPoints.Instance.ClanDisbanded(Blue), "another clan's");
            Assert.AreEqual(Red, point.ClanId);

            Drain(member);
            store.Saved.Clear();
            Assert.AreEqual(1, ControlPoints.Instance.ClanDisbanded(Red));

            Assert.IsTrue(point.HeldByAfs);
            Assert.IsFalse(point.HeldByClan);
            Assert.AreEqual((EntityClasses)3814, point.Object.EntityClassId);
            Assert.AreEqual(UseObjectState.CpointStateFactionAOwned, point.Object.StateId);
            Assert.IsFalse(point.Object.IsEnabled);
            Assert.AreEqual((ControlPoints.Afs, 0u), (store.Saved.Single().Owner, store.Saved.Single().ClanId));
            Assert.IsFalse(Pool(world, AfsPool).Suspended, "the garrison is left as it stands");

            var packets = Drain(member);
            Assert.AreEqual("AFS", packets.OfType<DisplayClientMessagePacket>().Single(m => m.MsgId == PlayerMessage.PmControlpointOwned).Args["faction"]);
            Assert.AreEqual(0, packets.OfType<UpdateMapMarkerPacket>().Count(), "the AFS's before and after");

            // A clan the server has no row of any more: the worker's pass takes its point.
            Assert.IsTrue(ControlPoints.Instance.SetHolder(point, ControlPoints.Afs, Green, null));
            _clans.Remove(Green);
            ControlPoints.Instance.ClanWorker();

            Assert.IsFalse(point.HeldByClan);
            Assert.AreEqual(0, _paid.Count);
        }

        [TestMethod]
        public void TheWeeklyResetGivesEveryClansPointBackToTheAfs()
        {
            using var world = new WorldTestContext();
            var point = Held(world, Red);

            // The first pass: the next reset is tomorrow at three.
            ControlPoints.Instance.ClanWorker();
            Assert.AreEqual(new DateTime(2026, 10, 6, 3, 0, 0), ControlPoints.Instance.NextClanReset);
            Assert.IsTrue(point.HeldByClan, "taken since the last one");

            _wall = new DateTime(2026, 10, 6, 2, 59, 59);
            ControlPoints.Instance.ClanWorker();
            Assert.IsTrue(point.HeldByClan);

            _wall = new DateTime(2026, 10, 6, 3, 0, 0);
            ControlPoints.Instance.ClanWorker();

            Assert.IsFalse(point.HeldByClan);
            Assert.IsTrue(point.HeldByAfs);
            Assert.AreEqual((EntityClasses)3814, point.Object.EntityClassId);
            Assert.AreEqual(new DateTime(2026, 10, 13, 3, 0, 0), ControlPoints.Instance.NextClanReset);

            // One taken after it is the clan's until the next.
            Assert.IsTrue(ControlPoints.Instance.SetHolder(point, ControlPoints.Afs, Blue, null));
            _wall = new DateTime(2026, 10, 12, 23, 0, 0);
            ControlPoints.Instance.ClanWorker();
            Assert.IsTrue(point.HeldByClan);

            // With the reset off there is none.
            ControlPoints.Instance.Config = new ControlPointConfig { ClanWeeklyReset = false };
            _wall = new DateTime(2026, 10, 14, 0, 0, 0);
            ControlPoints.Instance.ClanWorker();

            Assert.IsNull(ControlPoints.Instance.NextClanReset);
            Assert.IsTrue(point.HeldByClan);
        }

        [TestMethod]
        public void AResetThatCameRoundWhileTheServerWasDownIsCaughtUpWithOnTheFirstPass()
        {
            using var world = new WorldTestContext();

            // Monday noon: the last reset was six days and nine hours ago.
            var before = new MemoryStore();
            before.Rows.Add(new ControlPointStateEntry { ControlPointId = PointId, Owner = ControlPointEntry.OwnerAfs, ChangedAt = _utc - 7 * Day, ClanId = Red, ClanPaidAt = _utc - 7 * Day });

            var point = Placed(world, ControlPointEntry.OwnerBane, before);
            Assert.IsTrue(point.HeldByClan);

            ControlPoints.Instance.ClanWorker();

            Assert.IsFalse(point.HeldByClan, "held from before the reset the server missed");
            Assert.IsTrue(point.HeldByAfs);
            Assert.AreEqual(0, _paid.Count);

            // One taken after that reset is kept.
            Unplace(world);

            var after = new MemoryStore();
            after.Rows.Add(new ControlPointStateEntry { ControlPointId = PointId, Owner = ControlPointEntry.OwnerAfs, ChangedAt = _utc - 6 * Day, ClanId = Red, ClanPaidAt = _utc });

            point = Placed(world, ControlPointEntry.OwnerBane, after);
            ControlPoints.Instance.ClanWorker();

            Assert.IsTrue(point.HeldByClan);
        }

        #endregion

        #region Pay

        [TestMethod]
        public void AHeldPointPaysItsClanOnceForEveryIntervalItIsHeld()
        {
            using var world = new WorldTestContext();
            var store = new MemoryStore();
            var point = Held(world, Red, store);

            ControlPoints.Instance.Config = new ControlPointConfig { ClanPrestige = 250, ClanPrestigeMinutes = 30, ClanWeeklyReset = false };
            store.Saved.Clear();

            _utc += 30 * Minute - 1;
            ControlPoints.Instance.ClanWorker();
            Assert.AreEqual(0, _paid.Count);
            Assert.AreEqual(0, store.Saved.Count);

            _utc += 1;
            ControlPoints.Instance.ClanWorker();
            Assert.AreEqual((Red, 250, "Retread Outpost"), _paid.Single());
            Assert.AreEqual(_utc, point.PaidAt);
            Assert.AreEqual(_utc, store.Saved.Single().ClanPaidAt, "kept: a restart pays nothing twice");
            Assert.AreEqual(point.ChangedAt, store.Saved.Single().ChangedAt, "its pay is not a change of hands");

            ControlPoints.Instance.ClanWorker();
            Assert.AreEqual(1, _paid.Count, "once");

            _utc += 45 * Minute;
            ControlPoints.Instance.ClanWorker();
            Assert.AreEqual(2, _paid.Count);
            Assert.AreEqual(_utc - 15 * Minute, point.PaidAt, "the next counts from when this one was due");

            // Hours behind - the server was down - is one payment, and counts from now.
            _utc += 10 * Hour;
            ControlPoints.Instance.ClanWorker();
            ControlPoints.Instance.ClanWorker();
            Assert.AreEqual(3, _paid.Count);
            Assert.AreEqual(_utc, point.PaidAt);

            // Another clan's pay counts from when it took the point.
            _utc += 20 * Minute;
            Assert.IsTrue(ControlPoints.Instance.SetHolder(point, ControlPoints.Afs, Blue, null));
            _utc += 29 * Minute;
            ControlPoints.Instance.ClanWorker();
            Assert.AreEqual(3, _paid.Count);

            _utc += Minute;
            ControlPoints.Instance.ClanWorker();
            Assert.AreEqual((Blue, 250, "Retread Outpost"), _paid.Last());
        }

        [TestMethod]
        public void NoPayWithTheAmountOrTheIntervalAtNothing()
        {
            using var world = new WorldTestContext();
            Held(world, Red);

            foreach (var config in new[]
                     {
                         new ControlPointConfig { ClanPrestige = 0, ClanWeeklyReset = false },
                         new ControlPointConfig { ClanPrestigeMinutes = 0, ClanWeeklyReset = false }
                     })
            {
                ControlPoints.Instance.Config = config;
                _utc += 5 * Hour;
                ControlPoints.Instance.ClanWorker();
            }

            Assert.AreEqual(0, _paid.Count);
        }

        [TestMethod]
        public void APaymentGoesIntoTheClansLockboxWithALineOfItsHistory()
        {
            using var context = MissionTestContext.WithCompletableMission(429);
            var client = context.Client;
            var inventory = new InventoryManager(context);
            var clanInstance = typeof(ClanManager).GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic);
            var previous = clanInstance.GetValue(null);
            var lockbox = new DynamicObject { EntityClassId = EntityClasses.UsableClanLockboxV01, DynamicObjectType = DynamicObjectType.Lockbox };
            ClanEntry clan;

            using (var unit = context.CreateChar())
            {
                clan = unit.Clans.CreateClan("Red Company", true);
                Assert.IsTrue(unit.ClanMembers.InsertClanMemberData(clan.Id, client.Player.Id, 3, ""));
                unit.Clans.UpdatePrestige(clan.Id, 40);
                unit.Clans.UpdateCredits(clan.Id, 7000);
            }

            try
            {
                var clans = (ClanManager)typeof(ClanManager).GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null,
                    new[] { typeof(IGameUnitOfWorkFactory) }, null).Invoke(new object[] { context });

                clanInstance.SetValue(null, clans);
                client.Player.ClanId = clan.Id;

                lock (Server.Clients)
                    Server.Clients.Add(client);

                EntityManager.Instance.RegisterDynamicObject(lockbox);
                Drain(client);

                Assert.AreEqual("Red Company", clans.ClanNameOf(clan.Id));
                Assert.IsNull(clans.ClanNameOf(clan.Id + 50));
                Assert.AreEqual(clan.Id, clans.FindClan(clan.Id.ToString()).Id);
                Assert.IsNull(clans.FindClan("Nobody"));

                Assert.IsTrue(inventory.PayClanPrestige(clan.Id, 100, ControlPoints.PayerName, "Retread Outpost"));
                Assert.IsFalse(inventory.PayClanPrestige(clan.Id + 50, 100, ControlPoints.PayerName, "Retread Outpost"), "no such clan");
                Assert.IsFalse(inventory.PayClanPrestige(clan.Id, 0, ControlPoints.PayerName, "Retread Outpost"));

                using (var unit = context.CreateChar())
                {
                    var row = unit.Clans.GetClanById(clan.Id);

                    Assert.AreEqual(140u, row.Prestige);
                    Assert.AreEqual(7000u, row.Credits);

                    var line = unit.ClanLockboxLogs.Get(clan.Id, 10).Single();

                    Assert.AreEqual(InventoryTransactionType.Deposit, line.TransactionType);
                    Assert.AreEqual((byte)CurencyType.Prestige, line.CreditType);
                    Assert.AreEqual(100, line.Amount);
                    Assert.AreEqual(0u, line.CharacterId);
                    Assert.AreEqual("Control Point Retread Outpost", line.CharacterName + " " + line.UserName, "as the client prints who it was");
                }

                var packets = Drain(client);
                var funds = packets.OfType<UpdateClanLockboxCreditsPacket>().First();

                CollectionAssert.AreEqual(new uint[] { 7000, 140 }, funds.ListCredits, "to each lockbox there is: the client has the balance from whichever it uses");
                Assert.AreEqual(1, packets.OfType<ClanLockboxLogsPacket>().Single().Logs.Count);
            }
            finally
            {
                EntityManager.Instance.UnregisterDynamicObject(lockbox.EntityId);
                EntityManager.Instance.FreeEntity(lockbox.EntityId);

                lock (Server.Clients)
                    Server.Clients.Remove(client);

                clanInstance.SetValue(null, previous);
            }
        }

        #endregion

        #region The lockbox

        [TestMethod]
        public void ThePointsLockboxIsOnTheMapWhileAClanHoldsItAndThatClansAloneToOpen()
        {
            using var world = new WorldTestContext();

            // The map has the row as a footlocker, as it has every row of that table.
            world.Map.FootLockers[LockboxRow] = new DynamicObject { EntityClassId = EntityClasses.UsableClanLockboxV01, DynamicObjectType = DynamicObjectType.Lockbox };
            world.Map.FootLockers[77] = new DynamicObject { EntityClassId = EntityClasses.UsableClanLockboxV01, DynamicObjectType = DynamicObjectType.Lockbox };

            var point = Placed(world, ControlPointEntry.OwnerBane, lockbox: true);
            var city = world.Map.FootLockers[77];

            Assert.AreEqual(LockboxRow, point.LockboxId);
            Assert.AreEqual(new Vector3(14, 0, 10), point.LockboxPosition);
            Assert.IsFalse(world.Map.FootLockers.ContainsKey(LockboxRow), "the point's to set down, not the map's");
            Assert.IsTrue(world.Map.FootLockers.ContainsKey(77));
            Assert.IsNull(point.Lockbox, "the Bane hold the point");

            var red = Member(world, 10, 10, Red);
            var blue = Member(world, 11, 10, Blue);

            BaneDown(world);
            Drain(red);
            Drain(blue);
            Assert.IsTrue(ControlPoints.Instance.Captured(world.Map, red, point));

            var box = point.Lockbox;
            Assert.IsNotNull(box);
            Assert.IsTrue(box.IsInWorld);
            Assert.AreEqual(EntityClasses.UsableClanLockboxV01, box.EntityClassId);
            Assert.AreEqual(DynamicObjectType.Lockbox, box.DynamicObjectType);
            Assert.AreEqual(UseObjectState.ClanlockboxState0, box.StateId);
            Assert.AreEqual(new Vector3(14, 0, 10), box.Position);
            Assert.AreSame(point, ControlPoints.Instance.LockboxOf(box));

            // In service to the clan's members.
            Assert.IsTrue(Drain(red).OfType<CreatePhysicalEntityPacket>().Single(p => p.EntityId == box.EntityId).EntityData.OfType<UsableInfoPacket>().Single().Enabled);
            Assert.IsFalse(Drain(blue).OfType<CreatePhysicalEntityPacket>().Single(p => p.EntityId == box.EntityId).EntityData.OfType<UsableInfoPacket>().Single().Enabled);

            Assert.IsTrue(ControlPoints.Instance.MayOpenLockbox(red, box));
            Assert.IsFalse(ControlPoints.Instance.MayOpenLockbox(blue, box));
            Assert.IsTrue(ControlPoints.Instance.MayOpenLockbox(blue, city), "a lockbox that is no point's is anyone's");

            Request(blue, box, DynamicObjectManager.FootlockerUseArgId);
            Assert.AreEqual(PlayerMessage.PmUseObjectNotUsable, Drain(blue).OfType<UserActionFailedPacket>().Single().MsgId);
            Assert.AreEqual(0, world.Map.PerformRecovery.Count);

            Request(red, box, DynamicObjectManager.FootlockerUseArgId);
            Assert.AreEqual(red.Player.EntityId, Drain(red).OfType<UsePacket>().Single().PlayerEntityId, "the client opens its lockbox window on this");
            world.Map.PerformRecovery.Clear();

            // A member who leaves the clan, and one who joins it, are told.
            red.Player.ClanId = 0;
            blue.Player.ClanId = Red;
            ControlPoints.Instance.Worker(world.Map);

            Assert.IsFalse(Drain(red).OfType<SetUsablePacket>().Single().IsEnabled);
            Assert.IsTrue(Drain(blue).OfType<SetUsablePacket>().Single().IsEnabled);

            // Another clan's point: another lockbox, so nobody is left with the first one open.
            Assert.IsTrue(ControlPoints.Instance.SetHolder(point, ControlPoints.Afs, Green, null));
            Assert.AreNotSame(box, point.Lockbox);
            Assert.IsNull(ControlPoints.Instance.LockboxOf(box));
            Assert.IsFalse(EntityManager.Instance.TryGetObject(box.EntityId, out _));
            Assert.IsFalse(ControlPoints.Instance.MayOpenLockbox(blue, point.Lockbox));

            // With the AFS it is gone.
            var last = point.Lockbox;
            Drain(red);
            Assert.AreEqual(1, ControlPoints.Instance.ReturnClanPoints());

            Assert.IsNull(point.Lockbox);
            Assert.IsTrue(Drain(red).OfType<DestroyPhysicalEntityPacket>().Any(p => p.EntityId == last.EntityId));
        }

        [TestMethod]
        public void AGameMasterSetsTheLockboxDownMovesItAndTakesItAway()
        {
            var directory = Path.Combine(AppContext.BaseDirectory, "TestDatabases", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);

            try
            {
                var database = Path.Combine(directory, "world");

                using (var context = PersistenceIntegrationTests.CreateContext(typeof(SqliteWorldContext), database))
                    context.Database.EnsureCreated();

                var factory = new WorldFactory(database);

                using var world = new WorldTestContext();
                var point = Held(world, Red);

                Assert.AreEqual(0u, point.LockboxId);
                Assert.IsNull(point.Lockbox);
                Assert.IsFalse(ControlPoints.Instance.RemoveLockbox(point, factory), "it has none");

                Assert.IsTrue(ControlPoints.Instance.SetLockbox(point, new Vector3(15, 0, 11), 1.5, factory));

                Assert.AreNotEqual(0u, point.LockboxId);
                Assert.IsNotNull(point.Lockbox, "on the map at once: a clan holds the point");
                Assert.AreEqual(new Vector3(15, 0, 11), point.Lockbox.Position);

                var first = point.Lockbox;

                using (var context = (WorldContext)PersistenceIntegrationTests.CreateContext(typeof(SqliteWorldContext), database))
                {
                    var row = new FootlockerRepository(context).GetFootlockers().Single();
                    var link = new ControlPointRepository(context).GetLinks().Single();

                    Assert.AreEqual(point.LockboxId, row.Id);
                    Assert.AreEqual(((uint)EntityClasses.UsableClanLockboxV01, MapId, 15d, 11d, 1.5), (row.ClassId, row.MapContextId, row.PosX, row.PosZ, row.Rotation));
                    Assert.AreEqual("Clan Lockbox Retread Outpost", row.Comment);
                    Assert.AreEqual((PointId, ControlPointLinkEntry.KindClanLockbox, row.Id), (link.ControlPointId, link.Kind, link.ObjectId));
                }

                // Again: the same row, moved.
                var id = point.LockboxId;
                Assert.IsTrue(ControlPoints.Instance.SetLockbox(point, new Vector3(16, 0, 12), 2, factory));

                Assert.AreEqual(id, point.LockboxId);
                Assert.AreNotSame(first, point.Lockbox);
                Assert.AreEqual(new Vector3(16, 0, 12), point.Lockbox.Position);

                using (var context = (WorldContext)PersistenceIntegrationTests.CreateContext(typeof(SqliteWorldContext), database))
                {
                    var row = new FootlockerRepository(context).GetFootlockers().Single();

                    Assert.AreEqual((16d, 12d, 2d), (row.PosX, row.PosZ, row.Rotation));
                    Assert.AreEqual(1, new ControlPointRepository(context).GetLinks().Count);

                    // What a restart reads back.
                    Unplace(world);
                    var store = new MemoryStore();
                    store.Rows.Add(new ControlPointStateEntry { ControlPointId = PointId, Owner = ControlPointEntry.OwnerAfs, ChangedAt = _utc, ClanId = Red, ClanPaidAt = _utc });

                    ControlPoints.Instance.Load(new[] { Entry(ControlPointEntry.OwnerBane) }, new ControlPointRepository(context).GetLinks(), store, new FootlockerRepository(context).GetFootlockers());
                    ControlPoints.Instance.Place(world.Map);
                    point = ControlPoints.Instance.ById(PointId);
                }

                Assert.AreEqual(id, point.LockboxId);
                Assert.IsNotNull(point.Lockbox, "a clan's point from the start has its lockbox from the start");
                Assert.AreEqual(new Vector3(16, 0, 12), point.Lockbox.Position);

                Assert.IsTrue(ControlPoints.Instance.RemoveLockbox(point, factory));

                Assert.AreEqual(0u, point.LockboxId);
                Assert.IsNull(point.Lockbox);

                using (var context = (WorldContext)PersistenceIntegrationTests.CreateContext(typeof(SqliteWorldContext), database))
                {
                    Assert.AreEqual(0, new FootlockerRepository(context).GetFootlockers().Count);
                    Assert.AreEqual(0, new ControlPointRepository(context).GetLinks().Count);
                }
            }
            finally
            {
                Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
                Directory.Delete(directory, true);
            }
        }

        #endregion

        #region Kept

        [TestMethod]
        public void TheClanAndItsPayAreReadBackAndOnlyWithTheAfs()
        {
            using var world = new WorldTestContext();
            var store = new MemoryStore();

            store.Rows.Add(new ControlPointStateEntry { ControlPointId = PointId, Owner = ControlPointEntry.OwnerAfs, ChangedAt = _utc - Hour, ClanId = Red, ClanPaidAt = _utc - 10 * Minute });

            var point = Placed(world, ControlPointEntry.OwnerBane, store);

            Assert.IsTrue(point.HeldByClan);
            Assert.AreEqual((Red, _utc - Hour, _utc - 10 * Minute), (point.ClanId, point.ChangedAt, point.PaidAt));
            Assert.AreEqual(ControlPoints.ClanPointClass, point.Object.EntityClassId, "set down as the clan's from the start");
            Assert.AreEqual(UseObjectState.CcpStateClanControlled, point.Object.StateId);
            Assert.AreEqual(ControlPoints.ClanPointNameOverride, point.Object.NameOverrideId);
            Assert.IsTrue(point.Object.IsEnabled);
            Assert.AreEqual("Red Company", ControlPoints.Instance.HolderName(point));
            Assert.AreEqual((int)Red, ControlPoints.Instance.ClanShownBy(point.Object));

            // A row that names a clan with the Bane as owner names nobody.
            Unplace(world);
            store.Rows[0] = new ControlPointStateEntry { ControlPointId = PointId, Owner = ControlPointEntry.OwnerBane, ChangedAt = _utc, ClanId = Red, ClanPaidAt = _utc };
            point = Placed(world, ControlPointEntry.OwnerAfs, store);

            Assert.IsFalse(point.HeldByAfs);
            Assert.IsFalse(point.HeldByClan);
            Assert.AreEqual(0u, point.ClanId);
            Assert.AreEqual(0, point.PaidAt);
            Assert.AreEqual("Bane", ControlPoints.Instance.HolderName(point));
        }

        [TestMethod]
        public void TheStateRowKeepsTheClanAndItsPay()
        {
            var directory = Path.Combine(AppContext.BaseDirectory, "TestDatabases", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);

            try
            {
                var database = Path.Combine(directory, "database");

                using (var context = PersistenceIntegrationTests.CreateContext(typeof(SqliteCharContext), database))
                    context.Database.Migrate();

                using (var context = (CharContext)PersistenceIntegrationTests.CreateContext(typeof(SqliteCharContext), database))
                {
                    var repository = new ControlPointStateRepository(context);

                    repository.SaveState(new ControlPointStateEntry { ControlPointId = 7, Owner = 1, ChangedAt = 100 });
                    repository.SaveState(new ControlPointStateEntry { ControlPointId = 8, Owner = 1, ChangedAt = 200, ClanId = 12, ClanPaidAt = 250 });
                }

                using (var context = (CharContext)PersistenceIntegrationTests.CreateContext(typeof(SqliteCharContext), database))
                {
                    var repository = new ControlPointStateRepository(context);
                    var rows = repository.GetStates();

                    Assert.AreEqual((7u, (byte)1, 100L, 0u, 0L), (rows[0].ControlPointId, rows[0].Owner, rows[0].ChangedAt, rows[0].ClanId, rows[0].ClanPaidAt));
                    Assert.AreEqual((8u, (byte)1, 200L, 12u, 250L), (rows[1].ControlPointId, rows[1].Owner, rows[1].ChangedAt, rows[1].ClanId, rows[1].ClanPaidAt));

                    // The same row again: every column of it.
                    repository.SaveState(new ControlPointStateEntry { ControlPointId = 8, Owner = 0, ChangedAt = 300 });
                }

                using (var context = (CharContext)PersistenceIntegrationTests.CreateContext(typeof(SqliteCharContext), database))
                {
                    var row = new ControlPointStateRepository(context).GetStates().Single(r => r.ControlPointId == 8);

                    Assert.AreEqual(((byte)0, 300L, 0u, 0L), (row.Owner, row.ChangedAt, row.ClanId, row.ClanPaidAt));
                }
            }
            finally
            {
                Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
                Directory.Delete(directory, true);
            }
        }

        #endregion

        #region The packets

        [TestMethod]
        public void TheClanArgumentsAreSentToTheClanClassAndToNoOther()
        {
            // UseInterruptible: (actorId) to any usable, (actorId, usingClanId) to the clan class.
            CollectionAssert.AreEqual(Written(pw => { pw.WriteTuple(1); pw.WriteULong(55); }), MissionTestContext.Encode(new UseInterruptiblePacket(55)));
            CollectionAssert.AreEqual(Written(pw => { pw.WriteTuple(2); pw.WriteULong(55); pw.WriteInt(12); }), MissionTestContext.Encode(new UseInterruptiblePacket(55, 12)));

            // Use: (actorId, state, windup) and, to the clan class, the owning clan after them.
            CollectionAssert.AreEqual(Written(pw => { pw.WriteTuple(3); pw.WriteULong(55); pw.WriteUInt(209); pw.WriteInt(100); }),
                MissionTestContext.Encode(new UsePacket(55, UseObjectState.CcpStateClanControlled, 100)));
            CollectionAssert.AreEqual(Written(pw => { pw.WriteTuple(4); pw.WriteULong(55); pw.WriteUInt(209); pw.WriteInt(100); pw.WriteInt(12); }),
                MissionTestContext.Encode(new UsePacket(55, UseObjectState.CcpStateClanControlled, 100, 12)));

            // ClanAssociation: (owningClanId), the AFS and the Bane as clans below zero.
            CollectionAssert.AreEqual(Written(pw => { pw.WriteTuple(1); pw.WriteInt(-1); }), MissionTestContext.Encode(new ClanAssociationPacket(ControlPoints.VirtualClanAfs)));
            Assert.AreEqual(GameOpcode.ClanAssociation, new ClanAssociationPacket(12).Opcode);
            Assert.AreEqual(-2, ControlPoints.VirtualClanBane);

            // By the class of the object, and the clan of whoever is at it.
            var clans = new DynamicObject { EntityClassId = ControlPoints.ClanPointClass };
            var plain = new DynamicObject { EntityClassId = (EntityClasses)3814 };

            try
            {
                Assert.AreEqual(12, DynamicObjectManager.UseInterruptibleOf(clans, new Manifestation { ClanId = 12 }).UsingClanId);
                Assert.AreEqual(ControlPoints.VirtualClanAfs, DynamicObjectManager.UseInterruptibleOf(clans, new Manifestation()).UsingClanId, "in no clan: the AFS's");
                Assert.IsNull(DynamicObjectManager.UseInterruptibleOf(plain, new Manifestation { ClanId = 12 }).UsingClanId);
            }
            finally
            {
                EntityManager.Instance.FreeEntity(clans.EntityId);
                EntityManager.Instance.FreeEntity(plain.EntityId);
            }
        }

        #endregion

        #region Fixture

        private sealed class MemoryStore : ControlPoints.IStore
        {
            public List<ControlPointStateEntry> Rows { get; } = new List<ControlPointStateEntry>();
            public List<ControlPointStateEntry> Saved { get; } = new List<ControlPointStateEntry>();

            public List<ControlPointStateEntry> Load() => Rows;
            public void Save(ControlPointStateEntry state) => Saved.Add(state);
        }

        /// <summary>A unit of work factory over a world database of the schema alone: the footlockers and the control points.</summary>
        private sealed class WorldFactory : IGameUnitOfWorkFactory
        {
            private readonly string _database;

            public WorldFactory(string database)
            {
                _database = database;
            }

            public ICharUnitOfWork CreateChar() => throw new InvalidOperationException("Unexpected character database access.");

            public IWorldUnitOfWork CreateWorld()
            {
                var context = (WorldContext)PersistenceIntegrationTests.CreateContext(typeof(SqliteWorldContext), _database);

                return DepartureFailureTests.StrictProxy.Create<IWorldUnitOfWork>(new Dictionary<string, object>
                {
                    ["get_Footlockers"] = new FootlockerRepository(context),
                    ["get_ControlPoints"] = new ControlPointRepository(context)
                }, "Dispose");
            }
        }

        private static ControlPointEntry Entry(byte defaultOwner) => new ControlPointEntry
        {
            Id = PointId,
            MapContextId = MapId,
            Name = "Retread Outpost",
            ClassId = 3814,
            PosX = 10,
            PosY = 0,
            PosZ = 10,
            Rotation = 0,
            MarkerEntityId = MarkerId,
            DefaultOwner = defaultOwner
        };

        private static ControlPointLinkEntry Link(byte kind, uint objectId) =>
            new ControlPointLinkEntry { ControlPointId = PointId, Kind = kind, ObjectId = objectId };

        /// <summary>The point loaded and set down on the world's map and in its cells, with a Bane pool and one of the AFS's.</summary>
        private ControlPoints.Point Placed(WorldTestContext world, byte owner, ControlPoints.IStore store = null, bool lockbox = false)
        {
            if (!_maps.Contains(world.Map))
                _maps.Add(world.Map);

            world.Map.SpawnPools.Add(NewPool(world, BanePool, SpawnPoolManager.ModeControlPoint, 90_000));
            world.Map.SpawnPools.Add(NewPool(world, AfsPool, SpawnPoolManager.ModeAutomatic, 2_000));

            var links = new List<ControlPointLinkEntry> { Link(ControlPointLinkEntry.KindBanePool, BanePool), Link(ControlPointLinkEntry.KindAfsPool, AfsPool) };
            var rows = new List<FootlockerEntry>();

            if (lockbox)
            {
                links.Add(Link(ControlPointLinkEntry.KindClanLockbox, LockboxRow));
                rows.Add(new FootlockerEntry
                {
                    Id = LockboxRow, ClassId = (uint)EntityClasses.UsableClanLockboxV01, MapContextId = MapId,
                    PosX = 14, PosY = 0, PosZ = 10, Rotation = 1, Comment = "Clan Lockbox Retread Outpost"
                });
            }

            ControlPoints.Instance.Load(new[] { Entry(owner) }, links, store, rows);
            ControlPoints.Instance.Place(world.Map);

            var point = ControlPoints.Instance.ById(PointId);

            CellManager.Instance.AddToWorld(world.Map, point.Object);
            point.Object.IsInWorld = true;

            return point;
        }

        /// <summary>The point as a clan has it: taken from the Bane and given to the clan, on the map.</summary>
        private ControlPoints.Point Held(WorldTestContext world, uint clanId, ControlPoints.IStore store = null)
        {
            var point = Placed(world, ControlPointEntry.OwnerBane, store);

            Assert.IsTrue(ControlPoints.Instance.SetHolder(point, ControlPoints.Afs, clanId, null));

            return point;
        }

        /// <summary>The point's objects off the map and its pools gone, for another <see cref="Placed"/>.</summary>
        private void Unplace(WorldTestContext world)
        {
            foreach (var point in ControlPoints.Instance.OnMap(MapId))
                foreach (var obj in new[] { point.Object, point.Lockbox })
                    if (obj != null && obj.IsInWorld)
                        CellManager.Instance.RemoveFromWorld(world.Map, obj);

            world.Map.ControlPoints.Clear();
            world.Map.SpawnPools.Clear();
        }

        private static SpawnPool NewPool(WorldTestContext world, uint id, short mode, long respawnMs) => new SpawnPool
        {
            DbId = id,
            Mode = mode,
            AnimType = 0,
            MapContextId = MapId,
            RuntimeMapChannel = world.Map,
            Position = new Vector3(12, 0, 12),
            RespawnTime = respawnMs,
            SpawnSlot = new List<SpawnPoolSlot> { new SpawnPoolSlot(9402, 1, 3) }
        };

        private static SpawnPool Pool(WorldTestContext world, uint id) => world.Map.SpawnPools.Single(p => p.DbId == id);

        private static void Spawned(WorldTestContext world, uint id, int alive)
        {
            var pool = Pool(world, id);

            pool.HasSpawned = true;
            pool.AliveCreatures = alive;
            pool.QueuedCreatures = 0;
            pool.DropshipQueue = 0;
        }

        /// <summary>The Bane garrison has stood and been killed: the point is in service.</summary>
        private static void BaneDown(WorldTestContext world)
        {
            Spawned(world, BanePool, alive: 0);
            ControlPoints.Instance.Worker(world.Map);
        }

        /// <summary>A player of a clan set down on the map, with whatever that shows them still to be read.</summary>
        private static Client Arrive(WorldTestContext world, float x, float z, uint clanId)
        {
            var client = world.CreateClient(x, z);

            client.Player.ClanId = clanId;
            client.Player.Attributes[Attributes.Health] = new ActorAttributes(Attributes.Health, 1000, 1000, 1000, 0, 0);
            client.Player.Attributes[Attributes.Armor] = new ActorAttributes(Attributes.Armor, 0, 0, 0, 0, 0);
            CellManager.Instance.AddToWorld(client);

            return client;
        }

        private static Client Member(WorldTestContext world, float x, float z, uint clanId)
        {
            var client = Arrive(world, x, z, clanId);

            Drain(client);
            return client;
        }

        private static void Request(Client client, DynamicObject obj, uint argId = DynamicObjectManager.ControlPointUseArgId)
        {
            DynamicObjectManager.Instance.RequestUseObjectPacket(client, new RequestUseObjectPacket
            {
                ActionId = ActionId.UseObject,
                ActionArgId = argId,
                EntityId = obj.EntityId
            });
        }

        /// <summary>The claim's time is up, as the map's worker has it: off the list, then recovered.</summary>
        private static void Finish(WorldTestContext world, params Client[] clients)
        {
            var action = world.Map.PerformRecovery.Single();

            foreach (var client in clients)
                Drain(client);

            world.Map.PerformRecovery.Clear();
            ActorActionManager.Instance.PerformRecovery(world.Map, action);
        }

        private static List<PythonPacket> Drain(Client client) => MissionTestContext.Drain(client).ToList();

        private static byte[] Encode(MapMarkerState state) => Written(state.Write);

        private static byte[] Written(Action<Rasa.Memory.PythonWriter> write)
        {
            using var stream = new MemoryStream();
            using var binary = new BinaryWriter(stream);
            using var writer = new Rasa.Memory.PythonWriter(binary);
            write(writer);
            return stream.ToArray();
        }

        #endregion
    }
}
