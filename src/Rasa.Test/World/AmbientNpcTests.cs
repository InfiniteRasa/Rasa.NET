using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
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
    using Rasa.Packets.MapChannel.Server;
    using Rasa.Packets.Protocol;
    using Rasa.Repositories.World;
    using Rasa.Services.Preloader;
    using Rasa.Structures;
    using Rasa.Structures.Char;
    using Rasa.Structures.World;
    using Rasa.Test.Database;
    using Rasa.Test.Missions;

    /// <summary>
    /// The client's ambient figures (AmbientNpcs): that a row's figure stands on its map and on
    /// each private copy of it, what a client is sent to make one, which rows are left out, the
    /// GM's .ambients and .ambient, and the figures seeded at the Proving Grounds with the six
    /// riflemen and the Captain's move that came with them.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class AmbientNpcTests
    {
        private const EntityClasses FiringRange = (EntityClasses)29425;
        private const EntityClasses SittingTalking = (EntityClasses)25583;
        private const EntityClasses Sitting = (EntityClasses)25272;

        /// <summary>The map of WorldTestContext.</summary>
        private const uint Wilderness = 1220;

        /// <summary>Classes as the world data has them: stateless switches, not targetable. Put back as they were on Dispose.</summary>
        private sealed class Figures : IDisposable
        {
            private readonly List<(EntityClasses Id, bool Had, EntityClass Was)> _changed = new();

            public Figures Add(EntityClasses id, string name, params AugmentationType[] augmentations)
            {
                var classes = EntityClassManager.Instance.LoadedEntityClasses;

                _changed.Add((id, classes.TryGetValue(id, out var was), was));
                classes[id] = new EntityClass((uint)id, name, 0, 1, augmentations.ToList(), false);

                return this;
            }

            public Figures AddFigure(EntityClasses id, string name) => Add(id, name, AugmentationType.StatelessSwitch);

            /// <summary>From a migrated world's entityclass rows, augmentations read as EntityClassManager reads them.</summary>
            public Figures AddFrom(WorldContext world, IEnumerable<uint> ids)
            {
                foreach (var id in ids.Distinct())
                {
                    var row = world.EntityClassEntries.AsNoTracking().Single(entry => entry.Id == id);
                    var augmentations = Regex.Split(row.AugList ?? "", @"\D+")
                        .Where(value => int.TryParse(value, out _))
                        .Select(value => (AugmentationType)int.Parse(value))
                        .ToArray();

                    Add((EntityClasses)id, row.ClassName, augmentations);
                    EntityClassManager.Instance.LoadedEntityClasses[(EntityClasses)id].TargetFlag = row.TargetFlag != 0;
                }

                return this;
            }

            public void Dispose()
            {
                AmbientNpcs.Load(null);

                var classes = EntityClassManager.Instance.LoadedEntityClasses;

                for (var i = _changed.Count - 1; i >= 0; i--)
                {
                    if (_changed[i].Had)
                        classes[_changed[i].Id] = _changed[i].Was;
                    else
                        classes.Remove(_changed[i].Id);
                }
            }
        }

        private static AmbientNpcEntry Row(uint id, uint map, EntityClasses classId, float x, float y, float z, double rotation, string comment = "fixture") => new()
        {
            Id = id, MapContextId = map, ClassId = (uint)classId, PosX = x, PosY = y, PosZ = z, Rotation = rotation, Comment = comment
        };

        private static List<PythonPacket> Methods(Client client) =>
            WorldTestContext.Drain(client).Select(packet => packet.Message).OfType<CallMethodMessage>().Select(message => message.Packet).ToList();

        private static void Clear(MapChannel map)
        {
            foreach (var figure in AmbientNpcs.OnChannel(map))
                CellManager.Instance.RemoveFromWorld(map, figure);
        }

        [TestMethod]
        public void ARowsFigureStandsOnItsMapOnce()
        {
            using var world = new WorldTestContext();
            using var figures = new Figures().AddFigure(FiringRange, "UsableStatelessNPCMaleFiringRange").AddFigure(SittingTalking, "UsableStatelessNPCMaleSittingTalkingV01");

            try
            {
                Assert.AreEqual(3, AmbientNpcs.Load(new[]
                {
                    Row(1, Wilderness, FiringRange, 10f, 2f, 20f, 3.1, "at the range"),
                    Row(2, Wilderness, SittingTalking, 12f, 2f, 22f, 0.5),
                    Row(3, 1985, FiringRange, 0f, 0f, 0f, 0)
                }));

                Assert.AreEqual(0, AmbientNpcs.Place(null));
                Assert.AreEqual(2, AmbientNpcs.Place(world.Map), "the two rows of this map");
                Assert.AreEqual(0, AmbientNpcs.Place(world.Map), "placed a second time");
                Assert.AreEqual(2, AmbientNpcs.OnChannel(world.Map).Count);

                var shooter = AmbientNpcs.ObjectOf(world.Map, AmbientNpcs.All[0]);

                Assert.AreEqual(DynamicObjectType.AmbientNpc, shooter.DynamicObjectType);
                Assert.AreEqual(FiringRange, shooter.EntityClassId);
                Assert.AreEqual(new Vector3(10f, 2f, 20f), shooter.Position);
                Assert.AreEqual(3.1, shooter.Rotation, 1e-9);
                Assert.AreEqual(Wilderness, shooter.MapContextId);
                Assert.AreEqual(UseObjectState.SsState0, shooter.StateId);
                Assert.IsFalse(shooter.IsEnabled, "nothing to use");
                Assert.AreEqual("at the range", shooter.Comment);
                Assert.AreSame(world.Map, shooter.RuntimeMapChannel);
                Assert.IsTrue(EntityManager.Instance.TryGetObject(shooter.EntityId, out var registered));
                Assert.AreSame(shooter, registered);

                Assert.AreEqual(SittingTalking, AmbientNpcs.ObjectOf(world.Map, AmbientNpcs.All[1]).EntityClassId);
                Assert.IsNull(AmbientNpcs.ObjectOf(world.Map, AmbientNpcs.All[2]), "another map's");
            }
            finally
            {
                Clear(world.Map);
            }
        }

        [TestMethod]
        public void EachPrivateCopyOfAMapHasItsOwnFigures()
        {
            using var world = new WorldTestContext();
            using var figures = new Figures().AddFigure(FiringRange, "UsableStatelessNPCMaleFiringRange");
            var maps = new MapChannelManager(null, privateInstances: new PrivateMapInstanceService());
            maps.MapChannelArray.Add(Wilderness, world.Map);

            AmbientNpcs.Load(new[] { Row(1, Wilderness, FiringRange, 10f, 2f, 20f, 3.1), Row(2, Wilderness, FiringRange, 15f, 2f, 20f, 3.1) });

            var first = maps.GetOrCreatePrivateInstance(Wilderness, 7);
            var second = maps.GetOrCreatePrivateInstance(Wilderness, 8);

            try
            {
                Assert.AreEqual(0, AmbientNpcs.OnChannel(world.Map).Count, "the open map's are put there at start-up, not by a copy being made");

                var ofFirst = AmbientNpcs.OnChannel(first);
                var ofSecond = AmbientNpcs.OnChannel(second);

                Assert.AreEqual(2, ofFirst.Count);
                Assert.AreEqual(2, ofSecond.Count);
                Assert.IsFalse(ofFirst.Select(figure => figure.EntityId).Intersect(ofSecond.Select(figure => figure.EntityId)).Any(), "each copy has entities of its own");
                Assert.IsTrue(ofFirst.All(figure => ReferenceEquals(figure.RuntimeMapChannel, first)));
                Assert.AreEqual(0, AmbientNpcs.Place(first), "already there");

                // The copy closes: its figures go with it.
                var gone = ofFirst.Select(figure => figure.EntityId).ToList();
                maps.ReleaseOwnedPrivateInstances(7);

                Assert.IsFalse(gone.Any(entityId => EntityManager.Instance.TryGetObject(entityId, out _)));
                Assert.AreEqual(2, AmbientNpcs.OnChannel(second).Count);
            }
            finally
            {
                maps.ReleaseOwnedPrivateInstances(7);
                maps.ReleaseOwnedPrivateInstances(8);
            }
        }

        [TestMethod]
        public void APlayerIsShownTheFigureInItsOneStateOutOfServiceAndNoTarget()
        {
            using var world = new WorldTestContext();

            // Were the class marked targetable, the figure still would not be.
            using var figures = new Figures().AddFigure(FiringRange, "UsableStatelessNPCMaleFiringRange");
            EntityClassManager.Instance.LoadedEntityClasses[FiringRange].TargetFlag = true;

            try
            {
                AmbientNpcs.Load(new[] { Row(1, Wilderness, FiringRange, 10f, 2f, 20f, MathF.PI) });
                Assert.AreEqual(1, AmbientNpcs.Place(world.Map));

                var shooter = AmbientNpcs.OnChannel(world.Map).Single();
                var client = world.CreateClient();

                client.Player.PlaceAt(new Vector3(12f, 2f, 18f));
                CellManager.Instance.AddToWorld(client);

                var seen = Methods(client);
                var made = seen.OfType<CreatePhysicalEntityPacket>().Single(packet => packet.EntityId == shooter.EntityId);

                Assert.AreEqual(FiringRange, made.ClassId);
                Assert.AreEqual(3, made.EntityData.Count);
                Assert.IsFalse(((IsTargetablePacket)made.EntityData[0]).IsTargetable);

                var where = (WorldLocationDescriptorPacket)made.EntityData[1];

                Assert.AreEqual(new Vector3(10f, 2f, 20f), where.Position);

                // Facing +Z, as a player with rotation pi does: the model's front is its -Z.
                var front = Vector3.Transform(-Vector3.UnitZ, where.Rotation);
                Assert.IsTrue(Vector3.Distance(front, Vector3.UnitZ) < 1e-5f, $"faces {front}");

                // The state is what starts the animation; out of service, with no mission's mark.
                var usable = (UsableInfoPacket)made.EntityData[2];

                Assert.IsFalse(usable.Enabled);
                Assert.AreEqual(UseObjectState.SsState0, usable.CurState);
                Assert.AreEqual(44u, (uint)usable.CurState, "USE_SS_STATE_0");
                Assert.AreEqual(0u, usable.NameOverrideId);
                Assert.AreEqual(0u, usable.WindupTime);
                Assert.AreEqual(0u, usable.MissionActivated);

                // A change to the player's missions tells the figure nothing.
                MissionObjects.Refresh(client);
                Assert.IsFalse(Methods(client).Any());
            }
            finally
            {
                Clear(world.Map);
            }
        }

        [TestMethod]
        public void ARowWhoseClassIsNotLoadedOrIsNoStatelessSwitchIsLeftOut()
        {
            using var world = new WorldTestContext();
            using var figures = new Figures()
                .AddFigure(FiringRange, "UsableStatelessNPCMaleFiringRange")
                .Add((EntityClasses)29365, "UsableStatelessHumPracticeDummyV01", AugmentationType.DestroyableStatelessSwitch)
                .Add((EntityClasses)6977, "TerraForeasCavernInstance");

            EntityClassManager.Instance.LoadedEntityClasses.Remove(SittingTalking, out var kept);

            try
            {
                Assert.AreEqual(1, AmbientNpcs.Load(new[]
                {
                    Row(1, Wilderness, SittingTalking, 1f, 0f, 1f, 0),      // not loaded
                    Row(2, Wilderness, (EntityClasses)29365, 2f, 0f, 2f, 0), // a usable of another kind: no state 44
                    Row(3, Wilderness, (EntityClasses)6977, 3f, 0f, 3f, 0),  // no usable at all
                    Row(4, Wilderness, FiringRange, 4f, 0f, 4f, 0)
                }));

                CollectionAssert.AreEqual(new uint[] { 4 }, AmbientNpcs.All.Select(placement => placement.Id).ToArray());
                Assert.AreEqual(1, AmbientNpcs.Place(world.Map));
                Assert.AreEqual(FiringRange, AmbientNpcs.OnChannel(world.Map).Single().EntityClassId);

                Assert.IsFalse(AmbientNpcs.IsFigure(null));
                Assert.IsNull(AmbientNpcs.PutDown(world.Map, (EntityClasses)29365, Vector3.Zero, 0));
                Assert.IsNull(AmbientNpcs.PutDown(null, FiringRange, Vector3.Zero, 0));

                Assert.AreEqual(0, AmbientNpcs.Load(null));
                Assert.AreEqual(0, AmbientNpcs.All.Count);
            }
            finally
            {
                Clear(world.Map);

                if (kept != null)
                    EntityClassManager.Instance.LoadedEntityClasses[SittingTalking] = kept;
            }
        }

        [TestMethod]
        public void AGameMasterListsTheFiguresStandsOneUpAndTakesItAway()
        {
            using var world = new WorldTestContext();
            using var figures = new Figures()
                .AddFigure(FiringRange, "UsableStatelessNPCMaleFiringRange")
                .AddFigure(SittingTalking, "UsableStatelessNPCMaleSittingTalkingV01")
                .AddFigure(Sitting, "UsableStatelessNPCMaleSitting")
                // A stateless switch that is no ambient person: not listed, but it can be stood up by id.
                .AddFigure((EntityClasses)919, "UsableStatelessSwitchV01")
                .Add((EntityClasses)29365, "UsableStatelessHumPracticeDummyV01", AugmentationType.DestroyableStatelessSwitch);

            var client = world.CreateClient();
            typeof(Client).GetProperty(nameof(Client.AccountEntry)).SetValue(client, new GameAccountEntry { Level = (byte)GmLevel.Admin });
            client.Player.PlaceAt(new Vector3(30f, 4f, 40f));
            client.Player.Rotation = 1.25f;
            CellManager.Instance.AddToWorld(client);

            var commands = new ChatCommandsManager(null);
            commands.RegisterChatCommands();

            List<string> Say(string command)
            {
                WorldTestContext.Drain(client);
                commands.ProcessCommand(client, command);
                return Methods(client).OfType<SystemMessagePacket>().Select(message => message.TextMessage).ToList();
            }

            try
            {
                // A row's figure, to see that clear leaves it.
                AmbientNpcs.Load(new[] { Row(1, Wilderness, FiringRange, 10f, 2f, 20f, 0) });
                AmbientNpcs.Place(world.Map);

                var listed = Say(".ambients");

                StringAssert.Contains(listed[0], "3 ambient figure(s)");
                StringAssert.Contains(listed[0], "1 stand on this map");
                CollectionAssert.AreEqual(new[] { "25272 MaleSitting", "25583 MaleSittingTalkingV01", "29425 MaleFiringRange" }, listed.Skip(1).ToArray());
                CollectionAssert.AreEqual(new[] { "25583 MaleSittingTalkingV01" }, Say(".ambients sitting talk").Skip(1).ToArray());
                StringAssert.Contains(Say(".ambients welding").Single(), "No ambient figure matches");

                StringAssert.Contains(Say(".ambient").Single(), "usage: .ambient class | clear");
                StringAssert.Contains(Say(".ambient 29365").Single(), "Class 29365 is not an ambient figure");
                StringAssert.Contains(Say(".ambient 123456").Single(), "is not an ambient figure");
                StringAssert.Contains(Say(".ambient welding").Single(), "No ambient figure is named 'welding'");
                StringAssert.Contains(Say(".ambient male").Single(), "3 ambient figures match 'male'");
                Assert.AreEqual(1, AmbientNpcs.OnChannel(world.Map).Count, "nothing was put down by any of those");

                // By id: where the GM stands, facing the GM's way, and the GM is shown it.
                WorldTestContext.Drain(client);
                commands.ProcessCommand(client, ".ambient 29425");

                var sent = Methods(client);
                var put = AmbientNpcs.OnChannel(world.Map).Single(figure => figure.ObjectData == null);

                StringAssert.Contains(sent.OfType<SystemMessagePacket>().Single().TextMessage, "MaleFiringRange [29425] stands where you were");
                Assert.AreEqual(FiringRange, put.EntityClassId);
                Assert.AreEqual(new Vector3(30f, 4f, 40f), put.Position);
                Assert.AreEqual(1.25, put.Rotation, 1e-6);
                Assert.AreEqual(Wilderness, put.MapContextId);

                var made = sent.OfType<CreatePhysicalEntityPacket>().Single(packet => packet.EntityId == put.EntityId);
                Assert.AreEqual(UseObjectState.SsState0, made.EntityData.OfType<UsableInfoPacket>().Single().CurState);

                // By name: the whole name wins over the longer names it begins.
                StringAssert.Contains(Say(".ambient malesitting").Single(), "MaleSitting [25272]");
                StringAssert.Contains(Say(".ambient talking").Single(), "MaleSittingTalkingV01 [25583]");
                StringAssert.Contains(Say(".ambient 919").Single(), "UsableStatelessSwitchV01 [919]");
                Assert.AreEqual(5, AmbientNpcs.OnChannel(world.Map).Count);
                StringAssert.Contains(Say(".ambients")[0], "5 stand on this map");

                // Clear takes the four away and leaves the row's.
                var goneId = put.EntityId;

                StringAssert.Contains(Say(".ambient clear").Single(), "Took away 4 figure(s)");
                Assert.IsFalse(EntityManager.Instance.TryGetObject(goneId, out _));
                Assert.AreSame(AmbientNpcs.ObjectOf(world.Map, AmbientNpcs.All[0]), AmbientNpcs.OnChannel(world.Map).Single());
                StringAssert.Contains(Say(".ambient clear").Single(), "Took away 0 figure(s)");
            }
            finally
            {
                Clear(world.Map);
            }
        }

        [TestMethod]
        public void AnObserverCanListTheFiguresButNotStandOneUp()
        {
            using var world = new WorldTestContext();
            using var figures = new Figures().AddFigure(FiringRange, "UsableStatelessNPCMaleFiringRange");

            var client = world.CreateClient();
            typeof(Client).GetProperty(nameof(Client.AccountEntry)).SetValue(client, new GameAccountEntry { Level = (byte)GmLevel.Observer });
            CellManager.Instance.AddToWorld(client);

            var commands = new ChatCommandsManager(null);
            commands.RegisterChatCommands();

            try
            {
                WorldTestContext.Drain(client);
                commands.ProcessCommand(client, ".ambients");
                Assert.IsTrue(Methods(client).OfType<SystemMessagePacket>().Any(message => message.TextMessage == "29425 MaleFiringRange"));

                commands.ProcessCommand(client, ".ambient 29425");
                Assert.AreEqual(0, AmbientNpcs.OnChannel(world.Map).Count);
            }
            finally
            {
                Clear(world.Map);
            }
        }

        [TestMethod]
        public void TheSeedIsSixFiguresAndSixRiflemenAtTheProvingGroundsAndTheCaptainMoved()
        {
            var directory = Path.Combine(AppContext.BaseDirectory, "TestDatabases", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);

            try
            {
                var database = Path.Combine(directory, "world");

                using (var context = PersistenceIntegrationTests.CreateContext(typeof(SqliteWorldContext), database))
                    MigratedDatabaseTemplates.Migrate(context, () => context.Database.Migrate());

                using (var context = (WorldContext)PersistenceIntegrationTests.CreateContext(typeof(SqliteWorldContext), database))
                {
                    var rows = new AmbientNpcRepository(context).Get();
                    var classes = context.EntityClassEntries.AsNoTracking().ToDictionary(entry => entry.Id);

                    CollectionAssert.AreEqual(new uint[] { 1, 2, 3, 4, 5, 6 }, rows.Select(row => row.Id).ToArray());
                    CollectionAssert.AreEqual(
                        new[]
                        {
                            "UsableStatelessNPCMaleFiringRange", "UsableStatelessNPCMaleFiringRange",
                            "UsableStatelessNPCMaleSittingTalkingV01", "UsableStatelessNPCMaleCleanWeaponSittingV01",
                            "UsableStatelessNPCMaleStandingTabletV01", "UsableStatelessNPCMaleStandingV01"
                        },
                        rows.Select(row => classes[row.ClassId].ClassName).ToArray());

                    foreach (var row in rows)
                    {
                        Assert.AreEqual(BootcampPostedNpcs.BootcampMapContextId, row.MapContextId);
                        Assert.AreEqual("8", classes[row.ClassId].AugList, "a stateless switch and nothing else");
                        Assert.AreEqual((byte)0, classes[row.ClassId].TargetFlag);
                        StringAssert.StartsWith(row.Comment, "Proving Grounds: ");
                    }

                    // The two at the range: on the firing step of the middle and the west lane
                    // (the map's catwalks at x 380.6 and 375.2, z 170.28 to 174.28, top y 119.59),
                    // each where the game master stood and faced, down the lane within two degrees.
                    foreach (var shooter in rows.Take(2))
                    {
                        Assert.AreEqual(119.5938, shooter.PosY, 0.0001);
                        Assert.IsTrue(shooter.PosZ > 170.28 && shooter.PosZ < 174.28);
                        Assert.IsTrue(Math.Abs(shooter.Rotation - Math.PI) < 0.04);

                        // His target, 14.25 m ahead: on the lane's far platform (z 183.84 to 187.84), short of the sandbags behind it.
                        var targetZ = shooter.PosZ - Math.Cos(shooter.Rotation) * 14.25;
                        var targetX = shooter.PosX - Math.Sin(shooter.Rotation) * 14.25;

                        Assert.IsTrue(targetZ > 183.84 && targetZ < 187.84, $"target at z {targetZ}");
                        Assert.IsTrue(Math.Abs(targetX - (shooter.PosX > 378 ? 380.65 : 375.31)) < 2.0, $"target at x {targetX}");
                    }

                    Assert.AreEqual(380.7695, rows[0].PosX, 0.0001);
                    Assert.AreEqual(173.1484, rows[0].PosZ, 0.0001);
                    Assert.AreEqual(3.1054, rows[0].Rotation, 0.0001);
                    Assert.AreEqual(375.418, rows[1].PosX, 0.0001);
                    Assert.AreEqual(173.2656, rows[1].PosZ, 0.0001);
                    Assert.AreEqual(3.1541, rows[1].Rotation, 0.0001);

                    // The two seated: at the map's chairs (ArchHumGenObjChairV02 at 379.5813, 167.9491
                    // facing 3.1816 and at 379.0926, 169.427 facing 5.1738), on the floor the chairs stand on.
                    Assert.AreEqual(379.5813, rows[2].PosX, 0.0001);
                    Assert.AreEqual(119.5292, rows[2].PosY, 0.0001);
                    Assert.AreEqual(167.9491, rows[2].PosZ, 0.0001);
                    Assert.AreEqual(3.1816, rows[2].Rotation, 0.0001);
                    Assert.AreEqual(379.0926, rows[3].PosX, 0.0001);
                    Assert.AreEqual(119.5292, rows[3].PosY, 0.0001);
                    Assert.AreEqual(169.427, rows[3].PosZ, 0.0001);
                    Assert.AreEqual(5.1738, rows[3].Rotation, 0.0001);

                    // Within a step of where the game master stood for them, on the chairs.
                    Assert.IsTrue(Vector2.Distance(new Vector2(379.5859f, 168.0664f), new Vector2((float)rows[2].PosX, (float)rows[2].PosZ)) < 0.3f);
                    Assert.IsTrue(Vector2.Distance(new Vector2(378.8516f, 169.5f), new Vector2((float)rows[3].PosX, (float)rows[3].PosZ)) < 0.3f);

                    // The officer with the tablet and the man standing: where the game master stood and faced.
                    Assert.AreEqual(393.7969, rows[4].PosX, 0.0001);
                    Assert.AreEqual(122.0, rows[4].PosY, 0.0001);
                    Assert.AreEqual(166.1016, rows[4].PosZ, 0.0001);
                    Assert.AreEqual(2.4299, rows[4].Rotation, 0.0001);
                    Assert.AreEqual(376.1602, rows[5].PosX, 0.0001);
                    Assert.AreEqual(119.5273, rows[5].PosY, 0.0001);
                    Assert.AreEqual(169.9258, rows[5].PosZ, 0.0001);
                    Assert.AreEqual(3.0802, rows[5].Rotation, 0.0001);

                    // Six more riflemen, as the two of the first lot.
                    var pools = context.SpawnPoolEntries.AsNoTracking()
                        .Where(pool => pool.Id >= BootcampGarrisonNpcs.FirstPoolId && pool.Id <= BootcampGarrisonNpcs.LastPoolId)
                        .OrderBy(pool => pool.Id).ToList();
                    var poses = new SpawnpoolRepository(context).GetPoses().ToDictionary(row => row.Id, row => row.Pose);

                    CollectionAssert.AreEqual(new uint[] { 400101, 400102, 400103, 400104, 400105, 400106 }, pools.Select(pool => pool.Id).ToArray());

                    foreach (var pool in pools)
                    {
                        Assert.AreEqual(BootcampPostedNpcs.BootcampMapContextId, pool.MapContextId);
                        Assert.AreEqual(BootcampPostedNpcs.InfantrymanWithRifleId, pool.Creature1Id);
                        Assert.AreEqual((byte)1, pool.Creature1MinCount);
                        Assert.AreEqual((byte)1, pool.Creature1MaxCount);
                        Assert.AreEqual(0u, pool.Creature2Id);
                        Assert.AreEqual(0.0, pool.Radius);
                        Assert.AreEqual(BootcampPostedNpcs.WeaponOut, poses[pool.Id]);
                    }

                    Assert.AreEqual(402.3125, pools[0].PosX, 0.0001);
                    Assert.AreEqual(169.4883, pools[0].PosZ, 0.0001);
                    Assert.AreEqual(1.4922, pools[0].Rotation, 0.0001);
                    Assert.AreEqual(1.5864, pools[1].Rotation, 0.0001);
                    Assert.AreEqual(5.4176, pools[2].Rotation, 0.0001);
                    Assert.AreEqual(0.6487, pools[3].Rotation, 0.0001);
                    Assert.AreEqual(3.2609, pools[4].Rotation, 0.0001);
                    Assert.AreEqual(382.457, pools[5].PosX, 0.0001);
                    Assert.AreEqual(126.3633, pools[5].PosY, 0.0001);
                    Assert.AreEqual(73.3242, pools[5].PosZ, 0.0001);
                    Assert.AreEqual(2.8477, pools[5].Rotation, 0.0001);

                    // The four in the south stand round the Eloh hologram platform (389, 65), one to a corner.
                    CollectionAssert.AreEquivalent(
                        new[] { (1, -1), (-1, -1), (1, 1), (-1, 1) },
                        pools.Skip(2).Select(pool => (Math.Sign(pool.PosX - 389.0), Math.Sign(pool.PosZ - 65.0))).ToList());

                    // Captain Delessio: 1.3 m from where he stood, the same deck, facing the way the game master faced.
                    var captain = context.SpawnPoolEntries.AsNoTracking().Single(pool => pool.Id == BootcampGarrisonNpcs.DelessioPoolId);

                    Assert.AreEqual(BootcampRuntimeTestHarness.CaptainDelessioCreatureId, captain.Creature1Id);
                    Assert.AreEqual(400.4883, captain.PosX, 0.0001);
                    Assert.AreEqual(122.0, captain.PosY, 0.0001);
                    Assert.AreEqual(171.1992, captain.PosZ, 0.0001);
                    Assert.AreEqual(0.4381, captain.Rotation, 0.0001);
                    Assert.AreEqual(BootcampPostedNpcs.BootcampMapContextId, captain.MapContextId);
                }
            }
            finally
            {
                try { Directory.Delete(directory, true); } catch (IOException) { }
            }
        }

        [TestMethod]
        public void TheSixStandAtTheProvingGroundsAndARecruitAtTheRangeIsShownThem()
        {
            using var harness = BootcampRuntimeTestHarness.Create(useWorldContent: true);

            var rows = new AmbientNpcRepository(harness.WorldContext).Get();

            using var figures = new Figures().AddFrom(harness.WorldContext, rows.Select(row => row.ClassId));

            try
            {
                Assert.AreEqual(6, AmbientNpcs.Load(rows), "every seeded row is a figure the world data has");
                Assert.AreEqual(6, AmbientNpcs.Place(harness.BootcampMap));

                var standing = AmbientNpcs.OnChannel(harness.BootcampMap);

                Assert.AreEqual(6, standing.Count);

                // A recruit on the east lane's firing step.
                harness.MovePlayerTo(new Vector3(386f, 119.6f, 172.3f));
                harness.Drain();
                CellManager.Instance.UpdateVisibility(harness.Client);

                var made = harness.Drain().OfType<CreatePhysicalEntityPacket>()
                    .Where(packet => standing.Any(figure => figure.EntityId == packet.EntityId))
                    .ToDictionary(packet => packet.EntityId);

                Assert.AreEqual(6, made.Count, "all six are in sight of the range");

                foreach (var figure in standing)
                {
                    var data = made[figure.EntityId].EntityData;

                    Assert.AreEqual(figure.EntityClassId, made[figure.EntityId].ClassId);
                    Assert.AreEqual(3, data.Count);
                    Assert.IsFalse(data.OfType<IsTargetablePacket>().Single().IsTargetable);
                    Assert.AreEqual(figure.Position, data.OfType<WorldLocationDescriptorPacket>().Single().Position);
                    Assert.AreEqual(UseObjectState.SsState0, data.OfType<UsableInfoPacket>().Single().CurState);
                    Assert.IsFalse(data.OfType<UsableInfoPacket>().Single().Enabled);
                }

                CollectionAssert.AreEquivalent(
                    new uint[] { 29425, 29425, 25583, 25622, 25634, 25600 },
                    made.Values.Select(packet => (uint)packet.ClassId).ToList());

                // They are no creatures: the map's creatures are as they were, and its thinking leaves them be.
                var places = standing.ToDictionary(figure => figure.EntityId, figure => figure.Position);

                for (var tick = 0; tick < 40; tick++)
                    BehaviorManager.Instance.MapChannelThink(harness.BootcampMap, 250);

                foreach (var figure in AmbientNpcs.OnChannel(harness.BootcampMap))
                    Assert.AreEqual(places[figure.EntityId], figure.Position);

                Assert.IsFalse(harness.Drain().OfType<UsableInfoPacket>().Any(), "nothing more is said of them");
            }
            finally
            {
                Clear(harness.BootcampMap);
            }
        }
    }
}
