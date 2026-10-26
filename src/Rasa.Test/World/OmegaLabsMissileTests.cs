using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Rasa.Data;
    using Rasa.Game;
    using Rasa.Managers;
    using Rasa.Packets;
    using Rasa.Packets.Game.Server;
    using Rasa.Packets.MapChannel.Server;
    using Rasa.Packets.Protocol;
    using Rasa.Services.Preloader;
    using Rasa.Structures;
    using Rasa.Structures.World;
    using Rasa.Test.Database;

    /// <summary>The Omega Missile on its pad in Omega Labs' silo, idle (Omega_labs_missile, OmegaLabsMissile).</summary>
    [TestClass]
    [DoNotParallelize]
    public class OmegaLabsMissileTests
    {
        private const string Migration = "20270202000000_Omega_labs_missile";
        private const string Previous = "20270201000000_Omega_labs_regions";

        private const EntityClasses Missile = (EntityClasses)OmegaLabsMissile.MissileClass;

        private bool _had;
        private EntityClass _was;

        [TestCleanup]
        public void Restore()
        {
            PlacedUsables.Load(null);
            PlacedUsables.Reset();

            var classes = EntityClassManager.Instance.LoadedEntityClasses;

            if (_had)
                classes[Missile] = _was;
            else
                classes.Remove(Missile);
        }

        [TestMethod]
        public void TheRowGoesInOnceAndTheDownTakesItOut()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();

            migrator.Migrate(Previous);
            Assert.IsNull(Row(database));

            migrator.Migrate(Migration);
            var row = Row(database);
            Assert.IsNotNull(row, "the client's class is in entityclass");
            Assert.AreEqual((OmegaLabsMissile.MapContextId, OmegaLabsMissile.MissileClass, (uint)UseObjectState.TsState0, 0u),
                (row.MapContextId, row.ClassId, row.State, row.Usable));
            Assert.AreEqual((0.0, 180.0, 60.0), (row.PosX, row.PosY, row.PosZ));

            foreach (var sql in OmegaLabsMissile.Up())
                Assert.AreEqual(0, database.Context.Database.ExecuteSqlRaw(sql), "twice");

            migrator.Migrate(Previous);
            Assert.IsNull(Row(database));
        }

        [TestMethod]
        public void OnMySqlTheRowGoesInAndComesOut()
        {
            using var context = PersistenceIntegrationTests.CreateContext(typeof(Rasa.Context.World.MySqlWorldContext), "unused");
            var migrator = context.GetService<IMigrator>();

            foreach (var sql in OmegaLabsMissile.Up())
                StringAssert.Contains(migrator.GenerateScript(Previous, Migration), sql);
            foreach (var sql in OmegaLabsMissile.Down())
                StringAssert.Contains(migrator.GenerateScript(Migration, Previous), sql);
        }

        [TestMethod]
        public void ItStandsOnThePadReadyAndNobodyCanUseIt()
        {
            var classes = EntityClassManager.Instance.LoadedEntityClasses;
            _had = classes.TryGetValue(Missile, out _was);
            classes[Missile] = new EntityClass((uint)Missile, "UsableTwoStateMissileOmegalab", 0, 1, new List<AugmentationType> { AugmentationType.TwoStateSwitch }, false);

            using var world = new WorldTestContext();
            world.Map.MapInfo = new MapInfo(OmegaLabsMissile.MapContextId, "adv_arieki_torden_abyss_omegalabs", 162, 0);
            var watcher = world.CreateClient(x: 20, z: 60);
            watcher.Player.Position = new Vector3(20, 181, 60);
            watcher.Player.State = CharacterState.Normal;
            CellManager.Instance.AddToWorld(watcher);

            Assert.AreEqual(1, PlacedUsables.Load(new[]
            {
                new WorldUsableEntry
                {
                    Id = OmegaLabsMissile.Id, MapContextId = OmegaLabsMissile.MapContextId, ClassId = OmegaLabsMissile.MissileClass,
                    PosX = OmegaLabsMissile.X, PosY = OmegaLabsMissile.Y, PosZ = OmegaLabsMissile.Z, Rotation = OmegaLabsMissile.Rotation,
                    State = OmegaLabsMissile.IdleState, Usable = 0, Comment = OmegaLabsMissile.Comment
                }
            }));

            try
            {
                PlacedUsables.Place(world.Map);
                var missile = PlacedUsables.LiveOf(world.Map, OmegaLabsMissile.Id).Object;

                var made = WorldTestContext.Drain(watcher).Select(packet => packet.Message).OfType<CallMethodMessage>().Select(message => message.Packet)
                    .OfType<CreatePhysicalEntityPacket>().Single(packet => packet.EntityId == missile.EntityId);
                var info = made.EntityData.OfType<UsableInfoPacket>().Single();

                Assert.AreEqual((UseObjectState.TsState0, false), (info.CurState, info.Enabled), "idle, and not to be used");
                Assert.AreEqual(new Vector3(0, 180, 60), missile.Position);
            }
            finally
            {
                PlacedUsables.Forget(world.Map);
            }
        }

        private static WorldUsableEntry Row(OrdinaryLootTestDatabase database) =>
            ((Rasa.Context.World.WorldContext)database.Context).WorldUsableEntries.AsNoTracking().SingleOrDefault(entry => entry.Id == OmegaLabsMissile.Id);
    }
}
