using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Rasa.Config;
    using Rasa.Data;
    using Rasa.Managers;
    using Rasa.Missions.Scenes;
    using Rasa.Structures;
    using Rasa.Structures.Char;
    using Rasa.Structures.World;
    using Rasa.Test.Missions;
    using Rasa.Test.Missions.Wilderness;

    [TestClass]
    [DoNotParallelize]
    public class ControlPointServiceTests
    {
        private const uint Vendor = 990001, MissionNpc = 990002, Guard = 990003, Enemy = 990004;

        [TestMethod]
        public void NearestPointUsesTheSameMapAnInclusiveRadiusAndStableTies()
        {
            using var fixture = new Fixture();
            var pool = fixture.Pool(701, Vendor, new Vector3(130, 0, 0));
            fixture.Load(ControlPoints.Bane);
            Assert.AreEqual(1u, pool.ServiceControlPointId);
            Assert.IsTrue(pool.ServiceSuspended);
            pool.Position = new Vector3(130.01f, 0, 0);
            fixture.Points.RefreshServices(fixture.Map);
            Assert.AreEqual(0u, pool.ServiceControlPointId);
            Assert.IsFalse(pool.ServiceSuspended);
            CellManager.Instance.RemoveFromWorld(fixture.Map, fixture.Points.ById(1).Object);
            fixture.Points.Load(new[]
            {
                Entry(ControlPoints.Afs, 2, new Vector3(200, 0, 0)),
                Entry(ControlPoints.Bane, 1, Vector3.Zero),
                Entry(ControlPoints.Afs, 3, new Vector3(100, 0, 0), 9999)
            }, Array.Empty<ControlPointLinkEntry>(), null);
            pool.Position = new Vector3(100, 0, 0);
            fixture.Points.RefreshServices(fixture.Map);
            Assert.AreEqual(1u, pool.ServiceControlPointId, "same-map ties use the smaller point id");
        }

        [TestMethod]
        public void ExplicitServiceLinksOverrideDistanceAndAnOlderGarrisonLink()
        {
            using var fixture = new Fixture();
            var pool = fixture.Pool(701, Vendor, Vector3.Zero);
            fixture.Points.Load(new[] { Entry(ControlPoints.Bane), Entry(ControlPoints.Afs, 2, new Vector3(1000, 0, 0)) },
                new[] { Link(1, ControlPointLinkEntry.KindAfsPool, pool.DbId), Link(2, ControlPointLinkEntry.KindServicePool, pool.DbId) }, null);
            fixture.Points.Place(fixture.Map);
            Assert.AreEqual(2u, pool.ServiceControlPointId);
            Assert.IsFalse(pool.ServiceSuspended);
            var vendor = fixture.Stand(pool, Vendor);
            fixture.Points.SetOwner(fixture.Points.ById(1), ControlPoints.Afs, null);
            fixture.Points.SetOwner(fixture.Points.ById(1), ControlPoints.Bane, null);
            Assert.IsTrue(EntityManager.Instance.Creatures.ContainsKey(vendor.EntityId), "the overridden point cannot remove this service");
            fixture.Points.SetOwner(fixture.Points.ById(2), ControlPoints.Bane, null);
            Assert.IsFalse(EntityManager.Instance.Creatures.ContainsKey(vendor.EntityId));
        }

        [TestMethod]
        public void StartupUsesPersistedOwnershipBeforeAnyNpcCanSpawn()
        {
            using var fixture = new Fixture();
            var pool = fixture.Pool(701, MissionNpc, new Vector3(10, 0, 0));
            var store = new MemoryStore();
            store.Rows.Add(new ControlPointStateEntry { ControlPointId = 1, Owner = ControlPoints.Bane });
            fixture.Points.Load(new[] { Entry(ControlPoints.Afs) }, Array.Empty<ControlPointLinkEntry>(), store);
            fixture.Points.Place(fixture.Map);
            Assert.IsTrue(pool.ServiceSuspended);
            Assert.IsNull(CreatureManager.Instance.CreateCreature(MissionNpc, pool), "blocked before database work or entity allocation");
            Assert.IsNull(CreatureManager.Instance.CreateScenarioCreature(pool, MissionNpc, pool.Position, 0));
        }

        [TestMethod]
        public void BaneTakeoverRemovesServicesAndInvalidatesAnAlreadyOpenVendor()
        {
            using var fixture = new Fixture();
            var pool = fixture.Pool(701, Vendor, new Vector3(5, 0, 0));
            fixture.Load(ControlPoints.Afs);
            var vendor = fixture.Stand(pool, Vendor);
            fixture.Context.Client.Player.Position = vendor.Position;
            EntityManager.Instance.RegisterVendorItem(vendor.EntityId, new List<ulong>());
            Assert.IsNotNull(NpcManager.NpcInReach(fixture.Context.Client, vendor.EntityId, NpcManager.IsVendorNpc, "a vendor"));
            var experience = fixture.Context.Client.Player.Experience;
            fixture.Points.SetOwner(fixture.Points.ById(1), ControlPoints.Bane, null);
            Assert.AreEqual(0, pool.AliveCreatures);
            Assert.IsFalse(EntityManager.Instance.Creatures.ContainsKey(vendor.EntityId));
            Assert.IsFalse(EntityManager.Instance.VendorItems.ContainsKey(vendor.EntityId));
            Assert.IsNull(NpcManager.NpcInReach(fixture.Context.Client, vendor.EntityId, NpcManager.IsVendorNpc, "a vendor"));
            Assert.AreEqual(experience, fixture.Context.Client.Player.Experience, "despawning is not a rewarded kill");
        }

        [TestMethod]
        public void AServiceCannotKeepTheCombatGarrisonStanding()
        {
            using var fixture = new Fixture();
            var service = fixture.Pool(701, MissionNpc, Vector3.Zero);
            var defender = fixture.Pool(702, Guard, new Vector3(2, 0, 0));
            defender.HasSpawned = true;
            fixture.Load(ControlPoints.Afs, Link(1, ControlPointLinkEntry.KindAfsPool, service.DbId),
                Link(1, ControlPointLinkEntry.KindAfsPool, defender.DbId));
            fixture.Stand(service, MissionNpc);
            defender.HasSpawned = true;
            Assert.AreEqual(ControlPoints.Garrison.Down, fixture.Points.GarrisonOf(fixture.Map, fixture.Points.ById(1), ControlPoints.Afs));
            fixture.Points.Worker(fixture.Map);
            Assert.IsFalse(fixture.Points.ById(1).HeldByAfs);
            Assert.AreEqual(0, service.AliveCreatures);
        }

        [TestMethod]
        public void ABaseWithOnlyCivilianServicesHasNoCombatGarrison()
        {
            using var fixture = new Fixture();
            var pool = fixture.Pool(701, Vendor, Vector3.Zero);
            fixture.Load(ControlPoints.Afs, Link(1, ControlPointLinkEntry.KindAfsPool, pool.DbId));
            Assert.AreEqual(ControlPoints.Garrison.None, fixture.Points.GarrisonOf(fixture.Map, fixture.Points.ById(1), ControlPoints.Afs));
        }

        [TestMethod]
        public void PendingArrivalsRecheckOwnershipWhenTheyTryToCreateTheirNpcs()
        {
            using var fixture = new Fixture();
            var pool = fixture.Pool(701, Vendor, Vector3.Zero);
            fixture.Load(ControlPoints.Afs);
            pool.QueuedCreatures = 1;
            pool.DropshipQueue = 1;
            fixture.Points.SetOwner(fixture.Points.ById(1), ControlPoints.Bane, null);
            Assert.IsNull(CreatureManager.Instance.CreateCreature(Vendor, pool));
            Assert.IsNull(CreatureManager.Instance.CreateScenarioCreature(pool, Vendor, pool.Position, 0));
            Assert.AreEqual(0, pool.AliveCreatures);
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void CopiesOfTheMapDoNotInheritOpenWorldOwnership(bool shared)
        {
            using var fixture = new Fixture();
            fixture.Map.IsSharedInstance = shared;
            fixture.Map.IsPrivateInstance = !shared;
            var pool = fixture.Pool(701, Vendor, Vector3.Zero);
            fixture.Load(ControlPoints.Bane, Link(1, ControlPointLinkEntry.KindServicePool, pool.DbId));
            Assert.AreEqual(0u, pool.ServiceControlPointId);
            Assert.IsFalse(pool.ServiceSuspended);
        }

        [TestMethod]
        public void HostileNpcsGuardsAndMixedCombatPoolsAreNotAutomaticallyHidden()
        {
            using var fixture = new Fixture();
            var enemy = fixture.Pool(701, Enemy, Vector3.Zero);
            var guard = fixture.Pool(702, Guard, Vector3.Zero);
            var mixed = fixture.Pool(703, Vendor, Vector3.Zero);
            mixed.SpawnSlot.Add(new SpawnPoolSlot(Guard, 1, 1));
            fixture.Load(ControlPoints.Bane);
            Assert.IsFalse(enemy.ServiceSuspended || guard.ServiceSuspended || mixed.ServiceSuspended);
        }

        [TestMethod]
        public void RadiusChangesDoNotEraseIndependentSpawnRestrictions()
        {
            using var fixture = new Fixture();
            var near = fixture.Pool(701, Vendor, new Vector3(100, 0, 0));
            near.Suspended = true;
            var linked = fixture.Pool(702, MissionNpc, new Vector3(1000, 0, 0));
            fixture.Load(ControlPoints.Bane, Link(1, ControlPointLinkEntry.KindServicePool, linked.DbId));
            Assert.IsTrue(near.ServiceSuspended);
            fixture.Points.Config.ServiceNpcRadius = 0;
            fixture.Points.RefreshServices(fixture.Map);
            Assert.IsFalse(near.ServiceSuspended);
            Assert.IsTrue(near.Suspended, "the independent suspension remains");
            Assert.IsTrue(linked.ServiceSuspended, "explicit links still apply with proximity disabled");
            fixture.Points.Config.ServiceNpcRadius = 130;
            fixture.Points.RefreshServices(fixture.Map);
            Assert.IsTrue(near.ServiceSuspended);
        }

        [TestMethod]
        public void ClanChangesKeepServicesOpenWhileAfsStillOwnsThePoint()
        {
            using var fixture = new Fixture();
            var pool = fixture.Pool(701, Vendor, Vector3.Zero);
            fixture.Load(ControlPoints.Afs);
            var vendor = fixture.Stand(pool, Vendor);
            var previousName = fixture.Points.ClanName;
            try
            {
                fixture.Points.ClanName = _ => "Service test clan";
                fixture.Points.SetHolder(fixture.Points.ById(1), ControlPoints.Afs, 12, null);
                Assert.IsFalse(pool.ServiceSuspended);
                Assert.IsTrue(EntityManager.Instance.Creatures.ContainsKey(vendor.EntityId));
            }
            finally { fixture.Points.ClanName = previousName; }
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void RealWorldVendorSpawnsDespawnAndReturnWithoutDuplicates(bool controlPointMode)
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var points = ControlPoints.Instance;
            var previous = points.Config;
            try
            {
                points.Config = new ControlPointConfig();
                var pool = harness.Map.SpawnPools.First(candidate => candidate.SpawnSlot?.Count == 1 &&
                    candidate.SpawnSlot[0].CountMin == 1 && candidate.SpawnSlot[0].CountMax == 1 &&
                    harness.Creatures.LoadedCreatures.TryGetValue(candidate.SpawnSlot[0].CreatureId, out var creature) &&
                    creature.TargetCategory == TargetCategory.Friendly && creature.Npc?.Vendor != null);
                if (controlPointMode)
                    pool.Mode = SpawnPoolManager.ModeControlPoint;
                points.Load(new[] { Entry(ControlPoints.Bane, position: pool.Position) }, Array.Empty<ControlPointLinkEntry>(), null);
                points.Place(harness.Map);
                for (var capture = 0; capture < 3; capture++)
                {
                    harness.SpawnWorldAfter(60000, pool.DbId);
                    Assert.AreEqual(0, Standing(harness.Map, pool));
                    points.SetOwner(points.ById(1), ControlPoints.Afs, null);
                    harness.SpawnWorld(pool.DbId);
                    Assert.AreEqual(1, Standing(harness.Map, pool));
                    harness.SpawnWorld(pool.DbId);
                    Assert.AreEqual(1, Standing(harness.Map, pool), "no duplicate on another pass");
                    points.SetOwner(points.ById(1), ControlPoints.Bane, null);
                    Assert.AreEqual(0, Standing(harness.Map, pool), "taken off during the capture itself");
                }
            }
            finally
            {
                points.Load(Array.Empty<ControlPointEntry>(), Array.Empty<ControlPointLinkEntry>(), null);
                points.Config = previous;
            }
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void MissionScenesWaitForServicesAndRestoreOnlyThoseActorsOnRecapture(bool startBane)
        {
            using var fixture = new Fixture();
            fixture.Load(startBane ? ControlPoints.Bane : ControlPoints.Afs);
            var bindings = new SceneBindings("unversioned", new Dictionary<string, SceneActorDefinition>
            {
                ["service"] = new("service", SceneActorKind.Creature, MissionNpc, new ScenePosition(5, 0, 0)),
                ["unrelated"] = new("unrelated", SceneActorKind.Creature, Guard, new ScenePosition(300, 0, 0))
            }, new Dictionary<string, SceneRoute>(), new Dictionary<uint, SceneSequence>
            {
                [0] = new(worldIntents: new WorldIntent[]
                {
                    new EnsureActorIntent("service-present", "service"),
                    new EnsureActorIntent("unrelated-present", "unrelated")
                })
            });
            var run = fixture.Context.Manager.Scenes.Start(fixture.Context.Client, "data.sequence", bindings);
            var unrelated = Actors(fixture.Map).Single(creature => creature.DbId == Guard);
            if (startBane)
            {
                Assert.IsFalse(Actors(fixture.Map).Any(creature => creature.DbId == MissionNpc));
                fixture.Points.SetOwner(fixture.Points.ById(1), ControlPoints.Afs, null);
            }
            for (var capture = 0; capture < 3; capture++)
            {
                Assert.AreEqual(1, Actors(fixture.Map).Count(creature => creature.DbId == MissionNpc));
                Assert.AreSame(unrelated, Actors(fixture.Map).Single(creature => creature.DbId == Guard));
                Assert.AreEqual(2, fixture.Map.SpawnPools.Count(pool => pool.SceneRunId == run));
                fixture.Points.SetOwner(fixture.Points.ById(1), ControlPoints.Bane, null);
                Assert.IsFalse(Actors(fixture.Map).Any(creature => creature.DbId == MissionNpc));
                fixture.Points.SetOwner(fixture.Points.ById(1), ControlPoints.Afs, null);
            }
        }

        [TestMethod]
        public void AnActiveMissionKeepsItsPublicNpcLeaseAcrossOccupationAndRecapture()
        {
            using var fixture = new Fixture(leasedMission: true);
            var giver = fixture.Context.AddNpc(77);
            giver.TargetCategory = TargetCategory.Friendly;
            var pool = fixture.Pool(77, 77, giver.Position);
            pool.AliveCreatures = 1;
            giver.SpawnPool = pool;
            CreatureManager.Instance.LoadedCreatures[77] = new Creature
            {
                DbId = 77, EntityClass = EntityClasses.HumanBaseMale, TargetCategory = TargetCategory.Friendly,
                Npc = new Npc { NpcPackageId = 77, NpcMissionIds = new List<uint> { 321 } }, AppearanceData = new()
            };
            fixture.Load(ControlPoints.Afs);
            var bindings = new SceneBindings("unversioned", new Dictionary<string, SceneActorDefinition>
            {
                ["guide"] = new("guide", SceneActorKind.PublicSpawn, pool.DbId)
            }, new Dictionary<string, SceneRoute>(), new Dictionary<uint, SceneSequence>
            {
                [0] = new(worldIntents: new WorldIntent[]
                {
                    new EnsureActorIntent("guide-present", "guide"),
                    new SetInteractionIntent("guide-talks", "guide", true)
                })
            });
            fixture.Context.Manager.Scenes.Bind(321, "data.sequence", bindings);
            fixture.Context.Manager.PublicActors.Bind(new PublicEncounterBinding(321, pool.DbId, "guide", "data.sequence", "Wait"));
            Assert.IsTrue(fixture.Context.Manager.AcceptOfferedMission(fixture.Context.Client, giver.EntityId, 321));
            var handle = fixture.Context.Manager.PublicActors.Handle(fixture.Map, pool.DbId);
            Assert.IsNotNull(handle);
            fixture.Points.SetOwner(fixture.Points.ById(1), ControlPoints.Bane, null);
            fixture.Context.Manager.Scenes.Tick(fixture.Map);
            Assert.AreEqual(MissionState.Active, fixture.Context.Client.Player.Missions[321].State);
            fixture.Points.SetOwner(fixture.Points.ById(1), ControlPoints.Afs, null);
            var replacement = fixture.Stand(pool, 77);
            fixture.Context.Manager.Scenes.ActorAvailable(fixture.Map, pool.DbId);
            Assert.IsTrue(fixture.Context.Manager.PublicActors.TryResolve(fixture.Map, handle, out var leased));
            Assert.AreSame(replacement, leased);
            Assert.IsTrue(replacement.IsInteractable);
            Assert.AreEqual(MissionState.Active, fixture.Context.Client.Player.Missions[321].State);
        }

        private static Creature[] Actors(MapChannel map) => map.MapCellInfo.Cells.Values
            .SelectMany(cell => cell.CreatureList).Distinct().ToArray();
        private static int Standing(MapChannel map, SpawnPool pool) => Actors(map)
            .Count(creature => ReferenceEquals(creature.SpawnPool, pool) && creature.State != CharacterState.Dead);
        private static ControlPointEntry Entry(byte owner, uint id = 1, Vector3? position = null, uint map = 1220)
        {
            var at = position ?? Vector3.Zero;
            return new ControlPointEntry { Id = id, MapContextId = map, Name = "Service test", ClassId = 3814,
                DefaultOwner = owner, PosX = at.X, PosY = at.Y, PosZ = at.Z };
        }
        private static ControlPointLinkEntry Link(uint point, byte kind, uint pool) =>
            new() { ControlPointId = point, Kind = kind, ObjectId = pool };

        private sealed class MemoryStore : ControlPoints.IStore
        {
            internal List<ControlPointStateEntry> Rows { get; } = new();
            public List<ControlPointStateEntry> Load() => Rows;
            public void Save(ControlPointStateEntry state) { }
        }

        private sealed class Fixture : IDisposable
        {
            private readonly FieldInfo _missions = typeof(MissionApplication).GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic);
            private readonly FieldInfo _creatures = typeof(CreatureManager).GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic);
            private readonly object _previousMissions, _previousCreatures;
            private readonly EntityClass _previousClass;
            private readonly ControlPointConfig _previousConfig;
            internal MissionTestContext Context { get; }
            internal MapChannel Map => Context.Map;
            internal ControlPoints Points => ControlPoints.Instance;
            internal Fixture(bool leasedMission = false)
            {
                _previousConfig = Points.Config;
                Points.Load(Array.Empty<ControlPointEntry>(), Array.Empty<ControlPointLinkEntry>(), null);
                Points.Config = new ControlPointConfig();
                Context = leasedMission ? MissionTestContext.WithProgressMission(MissionProgressRule.CompleteOnScenarioEvent(321, 1, 1))
                    : MissionTestContext.WithCustomDefinitions(new Dictionary<uint, Mission>());
                _previousMissions = _missions.GetValue(null);
                _previousCreatures = _creatures.GetValue(null);
                _missions.SetValue(null, Context.Manager);
                _previousClass = EntityClassManager.Instance.LoadedEntityClasses[EntityClasses.HumanBaseMale];
                EntityClassManager.Instance.LoadedEntityClasses[EntityClasses.HumanBaseMale] =
                    new EntityClass((uint)EntityClasses.HumanBaseMale, "service-fixture", 0, 0, new() { AugmentationType.Creature }, true);
                var creatures = new CreatureManager(Context, new ManifestationManager(Context), Context.Manager);
                foreach (var (id, npc, side) in new[]
                {
                    (Vendor, new Npc { Vendor = new Vendor(1) }, TargetCategory.Friendly),
                    (MissionNpc, new Npc { NpcMissionIds = new List<uint> { 321 } }, TargetCategory.Friendly),
                    (Guard, (Npc)null, TargetCategory.Friendly),
                    (Enemy, new Npc { Vendor = new Vendor(1) }, TargetCategory.Hostile)
                })
                    creatures.LoadedCreatures[id] = new Creature { DbId = id, EntityClass = EntityClasses.HumanBaseMale,
                        TargetCategory = side, Npc = npc, AppearanceData = new(), State = CharacterState.Idle };
                _creatures.SetValue(null, creatures);
            }
            internal SpawnPool Pool(uint id, uint creature, Vector3 at)
            {
                var pool = new SpawnPool { DbId = id, MapContextId = Map.MapInfo.MapContextId,
                    RuntimeMapChannel = Map, Position = at, Mode = SpawnPoolManager.ModeAutomatic,
                    SpawnSlot = new List<SpawnPoolSlot> { new(creature, 1, 1) }, RespawnTime = 1000 };
                Map.SpawnPools.Add(pool);
                return pool;
            }
            internal void Load(byte owner, params ControlPointLinkEntry[] links)
            {
                Points.Load(new[] { Entry(owner) }, links, null);
                Points.Place(Map);
            }
            internal Creature Stand(SpawnPool pool, uint id)
            {
                var creature = CreatureManager.Instance.CreateScenarioCreature(pool, id, pool.Position, 0);
                Assert.IsNotNull(creature);
                CellManager.Instance.AddToWorld(Map, creature);
                pool.HasSpawned = true;
                return creature;
            }
            public void Dispose()
            {
                foreach (var creature in Actors(Map))
                    CellManager.Instance.RemoveCreatureFromWorld(Map, creature);
                foreach (var point in Points.Points)
                    if (point.Object?.IsInWorld == true && ReferenceEquals(point.Map, Map))
                        CellManager.Instance.RemoveFromWorld(Map, point.Object);
                Points.Load(Array.Empty<ControlPointEntry>(), Array.Empty<ControlPointLinkEntry>(), null);
                Points.Config = _previousConfig;
                EntityClassManager.Instance.LoadedEntityClasses[EntityClasses.HumanBaseMale] = _previousClass;
                _missions.SetValue(null, _previousMissions);
                _creatures.SetValue(null, _previousCreatures);
                Context.Dispose();
            }
        }
    }
}
