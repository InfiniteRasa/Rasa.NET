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
    using Rasa.Packets.ClientMethod.Server;
    using Rasa.Packets.Protocol;
    using Rasa.Structures;
    using Rasa.Structures.World;

    /// <summary>The tutorials the server raises (Tutorials): each once a session, when the thing it explains first happens.</summary>
    [TestClass]
    [DoNotParallelize]
    public class TutorialsTests
    {
        private Func<long> _now;
        private long _tick;

        [TestInitialize]
        public void Start()
        {
            _now = Tutorials.Now;
            _tick = 1_000_000;
            Tutorials.Now = () => _tick;
        }

        [TestCleanup]
        public void Restore()
        {
            Tutorials.Now = _now;
            ControlPoints.Instance.Load(new List<ControlPointEntry>(), new List<ControlPointLinkEntry>(), null);
        }

        [TestMethod]
        public void ATutorialIsRaisedOnceASessionAndAnAlwaysDisplayOneEveryTime()
        {
            using var world = new WorldTestContext();
            var client = world.CreateClient();

            Assert.IsTrue(Tutorials.Raise(client, TutorialId.Tutfire));
            Assert.IsFalse(Tutorials.Raise(client, TutorialId.Tutfire), "once");
            Assert.IsTrue(Tutorials.Raise(client, TutorialId.BattlegroundRulesIntro));
            Assert.IsTrue(Tutorials.Raise(client, TutorialId.BattlegroundRulesIntro), "every time");

            CollectionAssert.AreEqual(new[] { TutorialId.Tutfire, TutorialId.BattlegroundRulesIntro, TutorialId.BattlegroundRulesIntro }, Raised(client));
        }

        [TestMethod]
        public void TheHudsTutorialsComeOneAtATimeAfterTheSessionsFirstArrival()
        {
            using var world = new WorldTestContext();
            var client = world.CreateClient();

            Tutorials.PlayerEntered(client);
            Tutorials.PlayerEntered(client);    // another arrival: nothing more lined up

            var seen = new List<(long At, TutorialId Tutorial)>();
            for (var second = 0; second <= 180; second++)
            {
                _tick = 1_000_000 + second * 1000L;
                Tutorials.Worker(world.Map);
                seen.AddRange(Raised(client).Select(tutorial => ((long)second, tutorial)));
            }

            CollectionAssert.AreEqual(
                new[] { (5L, TutorialId.Tutmovement), (45L, TutorialId.Tutminimap), (90L, TutorialId.Trays), (135L, TutorialId.Tutctrl) },
                seen);
        }

        [TestMethod]
        public void ComingNearAControlPointRaisesItsTutorialOnce()
        {
            using var world = new WorldTestContext();
            ControlPoints.Instance.Load(new[]
            {
                new ControlPointEntry
                {
                    Id = 9001, MapContextId = world.Map.MapInfo.MapContextId, Name = "Retread Outpost", ClassId = 3814,
                    PosX = 100, PosY = 0, PosZ = 0, DefaultOwner = ControlPointEntry.OwnerBane
                }
            }, new List<ControlPointLinkEntry>(), null);
            ControlPoints.Instance.Place(world.Map);
            var client = world.CreateClient(x: 100 - Tutorials.ControlPointMetres - 5);

            Tutorials.Worker(world.Map);
            Assert.IsEmpty(Raised(client), "still too far");

            client.Player.Position = new Vector3(100 - Tutorials.ControlPointMetres + 1, 0, 0);
            Tutorials.Worker(world.Map);
            Tutorials.Worker(world.Map);
            CollectionAssert.AreEqual(new[] { TutorialId.ControlPoint }, Raised(client));
        }

        [TestMethod]
        public void TheFirstHitAPlayerTakesRaisesTheBars()
        {
            using var world = new WorldTestContext();
            var client = world.CreateClient();
            client.Player.Attributes[Attributes.Health] = new ActorAttributes(Attributes.Health, 100, 100, 100, 0, 0);
            client.Player.Attributes[Attributes.Armor] = new ActorAttributes(Attributes.Armor, 0, 0, 0, 0, 0);
            var creature = new Creature { TargetCategory = TargetCategory.Hostile, MapContextId = world.Map.MapInfo.MapContextId, RuntimeMapChannel = world.Map };

            ActorManager.Instance.Damage(world.Map, client.Player, 10, creature);
            ActorManager.Instance.Damage(world.Map, client.Player, 10, creature);

            CollectionAssert.AreEqual(new[] { TutorialId.Tuthealthpower }, Raised(client));
        }

        [TestMethod]
        public void TargetingAHostileCreatureRaisesTargeting()
        {
            using var world = new WorldTestContext();
            var client = world.CreateClient();
            var friend = new Creature { TargetCategory = TargetCategory.Friendly };
            var foe = new Creature { TargetCategory = TargetCategory.Hostile };
            EntityManager.Instance.RegisterCreature(friend);
            EntityManager.Instance.RegisterCreature(foe);
            try
            {
                ManifestationManager.Instance.SetTargetId(client, friend.EntityId);
                Assert.IsEmpty(Raised(client), "not a friend");

                ManifestationManager.Instance.SetTargetId(client, foe.EntityId);
                CollectionAssert.AreEqual(new[] { TutorialId.Tuttarget }, Raised(client));
            }
            finally
            {
                EntityManager.Instance.UnregisterCreature(friend.EntityId);
                EntityManager.Instance.UnregisterCreature(foe.EntityId);
            }
        }

        /// <summary>The tutorials the client has been told to raise since it was last read.</summary>
        private static List<TutorialId> Raised(Client client) =>
            WorldTestContext.Drain(client).Select(packet => packet.Message).OfType<CallMethodMessage>()
                .Select(message => message.Packet).OfType<DisplayPlayerTutorialNotificationPacket>()
                .Select(packet => packet.TutorialId).ToList();
    }
}
