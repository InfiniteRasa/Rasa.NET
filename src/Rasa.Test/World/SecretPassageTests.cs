extern alias RasaGame;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using ClientState = RasaGame::Rasa.Data.ClientState;
    using Rasa.Data;
    using Rasa.Managers;
    using Rasa.Models;
    using Rasa.Navigation;
    using Rasa.Packets;
    using Rasa.Packets.ClientMethod.Server;
    using Rasa.Packets.Game.Server;
    using Rasa.Packets.MapChannel.Client;
    using Rasa.Packets.MapChannel.Server;
    using Rasa.Packets.Protocol;
    using Rasa.Structures;

    [TestClass]
    [DoNotParallelize]
    public class SecretPassageTests
    {
        private const uint Wilderness = SecretPassages.ConcordiaWilderness;

        /// <summary>The Enhance logos (world data, logos 10), which Receptive Reception sends the player to.</summary>
        private static readonly Vector3 EnhanceLogos = new Vector3(832f, 161.838f, 960f);

        private static NavMeshQuery WildernessNavMesh()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);

            while (directory != null && !File.Exists(Path.Combine(directory.FullName, "Rasa.NET.sln")))
                directory = directory.Parent;

            Assert.IsNotNull(directory, "Repository root not found.");

            return TestNavMeshes.Query(
                NavMeshFile.PathFor(Path.Combine(directory.FullName, "navmesh"), "adv_foreas_concordia_wilderness"));
        }

        [TestCleanup]
        public void Cleanup()
        {
            SecretPassages.Travel = null;
        }

        /// <summary>The object manager the Alia Caverns passages travel by, saving nothing but counting what it would.</summary>
        private static DynamicObjectManager Travel(WorldTestContext world, Action saved = null, Func<long> clock = null)
        {
            var manager = WaypointTravelTests.CreateManager(world, (_, update, _) =>
            {
                if (update == CharacterUpdate.Position)
                    saved?.Invoke();
            }, clock);

            SecretPassages.Travel = manager;

            return manager;
        }

        private static uint CenterCell(Vector3 position)
        {
            Assert.IsTrue(CellManager.TryGetCellCoordinates(position, out var x, out var z));

            return (x & 0xFFFF) | (z << 16);
        }

        [TestMethod]
        public void TheGroundBeforeTheAliaCavernsAlcoveIsAPassageAndTheCorridorBeforeItIsNot()
        {
            // Walking up the corridor, short of the alcove; and along either wall right to the corridor's end.
            Assert.IsNull(SecretPassages.Crossed(Wilderness, new Vector3(835f, 286f, 724f), new Vector3(835f, 286f, 731f)));
            Assert.IsNull(SecretPassages.Crossed(Wilderness, new Vector3(829f, 286f, 733f), new Vector3(841f, 286f, 736f)));
            Assert.IsNull(SecretPassages.Crossed(Wilderness, new Vector3(827.5f, 286f, 734f), new Vector3(827.5f, 286f, 738.8f)));
            Assert.IsNull(SecretPassages.Crossed(Wilderness, new Vector3(842.5f, 286f, 734f), new Vector3(842.5f, 286f, 738.8f)));

            // Up to the alcove: at a walk, off to one side of the niche, into the niche, and in one long jump at it.
            Assert.AreSame(SecretPassages.AliaCavernsDoor,
                SecretPassages.Crossed(Wilderness, new Vector3(835f, 286f, 735f), new Vector3(835f, 286f, 737f)));
            Assert.AreSame(SecretPassages.AliaCavernsDoor,
                SecretPassages.Crossed(Wilderness, new Vector3(832.5f, 286f, 736f), new Vector3(832.5f, 286f, 737.5f)));
            Assert.AreSame(SecretPassages.AliaCavernsDoor,
                SecretPassages.Crossed(Wilderness, new Vector3(835f, 286f, 738.5f), new Vector3(835f, 286f, 740f)));
            Assert.AreSame(SecretPassages.AliaCavernsDoor,
                SecretPassages.Crossed(Wilderness, new Vector3(835f, 288f, 733f), new Vector3(835f, 284f, 747f)));

            // The same step on another map is nothing.
            Assert.IsNull(SecretPassages.Crossed(SecretPassages.TordenAbyss, new Vector3(835f, 286f, 735f), new Vector3(835f, 286f, 737f)));
        }

        [TestMethod]
        public void TheGroundBeforeTheEnhanceShrineAlcoveIsAPassageAndTheShrineIsNot()
        {
            // Around the room and down the corridor, short of the alcove; and along a wall to the corridor's end.
            Assert.IsNull(SecretPassages.Crossed(Wilderness, new Vector3(832f, 160f, 958f), new Vector3(832f, 160f, 940f)));
            Assert.IsNull(SecretPassages.Crossed(Wilderness, new Vector3(826f, 160f, 934f), new Vector3(838f, 160f, 923f)));
            Assert.IsNull(SecretPassages.Crossed(Wilderness, new Vector3(824.5f, 160f, 925f), new Vector3(824.5f, 160f, 920.2f)));

            Assert.AreSame(SecretPassages.EnhanceShrineExit,
                SecretPassages.Crossed(Wilderness, new Vector3(832f, 160f, 924f), new Vector3(832f, 160f, 922f)));
            Assert.AreSame(SecretPassages.EnhanceShrineExit,
                SecretPassages.Crossed(Wilderness, new Vector3(834.5f, 160f, 923f), new Vector3(834.5f, 160f, 921.5f)));
            Assert.AreSame(SecretPassages.EnhanceShrineExit,
                SecretPassages.Crossed(Wilderness, new Vector3(832f, 160f, 920.5f), new Vector3(832f, 160f, 919f)));

            // The identical rooms above and below it, 64 m apart, are not this one.
            Assert.IsNull(SecretPassages.Crossed(Wilderness, new Vector3(832f, 224f, 924f), new Vector3(832f, 224f, 920f)));
            Assert.IsNull(SecretPassages.Crossed(Wilderness, new Vector3(832f, 96f, 924f), new Vector3(832f, 96f, 920f)));

            // Every passage with an alcove shows itself; the leap into the Growth chamber does not.
            Assert.IsTrue(SecretPassages.AliaCavernsDoor.WithEffect);
            Assert.IsTrue(SecretPassages.EnhanceShrineExit.WithEffect);
            Assert.IsTrue(SecretPassages.GrowthHallEnd.WithEffect);
            Assert.IsFalse(SecretPassages.JumpAndBelieve.WithEffect);
        }

        [TestMethod]
        public void TheGroundBeforeTheGrowthHallAlcoveIsAPassageAndTheHallBeforeItIsNot()
        {
            const uint torden = SecretPassages.TordenAbyss;

            // Up the hall (its floor is at y 199, from z -355 to z -339), short of the alcove; and along a wall to the hall's end.
            Assert.IsNull(SecretPassages.Crossed(torden, new Vector3(-315f, 199.2f, -354f), new Vector3(-315f, 199.2f, -342f)));
            Assert.IsNull(SecretPassages.Crossed(torden, new Vector3(-322f, 199.2f, -350f), new Vector3(-322f, 199.2f, -339.5f)));

            Assert.AreSame(SecretPassages.GrowthHallEnd,
                SecretPassages.Crossed(torden, new Vector3(-315f, 199.2f, -343f), new Vector3(-315f, 199.2f, -341f)));
            Assert.AreSame(SecretPassages.GrowthHallEnd,
                SecretPassages.Crossed(torden, new Vector3(-317.5f, 199.2f, -342f), new Vector3(-317.5f, 199.2f, -340f)));
            Assert.AreSame(SecretPassages.GrowthHallEnd,
                SecretPassages.Crossed(torden, new Vector3(-315f, 199.2f, -339.5f), new Vector3(-315f, 199.2f, -338f)));

            // The chamber below, where the leap ends, and the shelf it leads back to, are clear of it.
            Assert.IsFalse(SecretPassages.GrowthHallEnd.Contains(SecretPassages.JumpAndBelieve.Destination));
            Assert.IsFalse(SecretPassages.JumpAndBelieve.Contains(SecretPassages.GrowthHallEnd.Destination));
            Assert.AreEqual(new Vector3(-347.8f, 523.5f, -531f), SecretPassages.GrowthHallEnd.Destination);
        }

        [TestMethod]
        public void WalkingUpToTheGrowthHallAlcoveTeleportsThePlayerBackToTheObelisks()
        {
            using var world = new WorldTestContext();

            var torden = new MapChannel
            {
                MapInfo = new MapInfo(SecretPassages.TordenAbyss, "adv_arieki_torden_abyss", 1556, 0),
                ClientList = new List<Rasa.Game.Client>(),
                PlayerLimit = 128
            };
            var saved = 0;
            var now = 1000L;
            var manager = Travel(world, () => saved++, () => now);
            var client = world.CreateClient();
            var player = client.Player;

            torden.ClientList.Add(client);
            player.MapChannel = torden;
            player.MapContextId = SecretPassages.TordenAbyss;
            player.PlaceAt(new Vector3(-315f, 199.2f, -348f));
            CellManager.Instance.AddToWorld(client);
            player.MoveBudget = 60;
            WorldTestContext.Drain(client);

            try
            {
                // Up the hall: an ordinary step.
                Assert.IsTrue(client.HandleMovement(new Movement(new Vector3(-315f, 199.2f, -342.5f), Vector2.Zero)));
                Assert.AreEqual(new Vector3(-315f, 199.2f, -342.5f), player.Position);
                Assert.AreEqual(ClientState.Ingame, client.State);

                // Up to the niche: the teleport-out effect where they stand, held there while it plays.
                Assert.IsTrue(client.HandleMovement(new Movement(new Vector3(-315f, 199.2f, -340.5f), Vector2.Zero)));
                Assert.AreEqual(new Vector3(-315f, 199.2f, -340.5f), player.Position);
                Assert.IsNotNull(client.PendingTransfer);

                var held = Methods(client);
                Assert.IsTrue(held.Any(packet => packet is PreTeleportPacket), "no teleport effect where they stood");
                Assert.IsFalse(held.Any(packet => packet is TeleportPacket), "moved before the effect has played");

                // Then taken back to the obelisks, and held until the client answers.
                now += DynamicObjectManager.PassageHoldMs;
                manager.PassagesWorker(torden);
                Assert.AreEqual(SecretPassages.GrowthHallEnd.Destination, player.Position);
                Assert.AreEqual(0f, (float)player.Rotation, 1e-4f);
                Assert.AreEqual(ClientState.Teleporting, client.State);
                Assert.AreEqual(SecretPassages.GrowthHallEnd.Destination, Methods(client).OfType<TeleportPacket>().Single().Position);

                manager.TeleportAcknowledge(client);

                Assert.AreEqual(ClientState.Ingame, client.State);
                Assert.AreEqual(1, saved);
            }
            finally
            {
                if (CellManager.Instance.IsInWorld(client))
                    CellManager.Instance.RemoveFromWorld(client);
            }
        }

        [TestMethod]
        public void NoPassageEndsInsideAnotherOrWithinAStepOfOne()
        {
            foreach (var passage in SecretPassages.All)
            {
                foreach (var other in SecretPassages.All.Where(other => other.MapContextId == passage.MapContextId))
                {
                    Assert.IsFalse(other.Contains(passage.Destination), $"{passage.Name} ends inside {other.Name}");

                    // Arriving at a run: three metres further on in any direction is still clear.
                    for (var i = 0; i < 8; i++)
                    {
                        var angle = i * MathF.PI / 4f;
                        var step = passage.Destination + new Vector3(MathF.Sin(angle), 0f, MathF.Cos(angle)) * 3f;

                        Assert.IsFalse(other.Touches(passage.Destination, step), $"{passage.Name} ends a step from {other.Name}");
                    }
                }
            }
        }

        [TestMethod]
        public void TheAliaCavernsPassagesEndOnTheFloorFacingTheWayOn()
        {
            var nav = WildernessNavMesh();
            var door = SecretPassages.AliaCavernsDoor;
            var exit = SecretPassages.EnhanceShrineExit;

            foreach (var passage in new[] { door, exit })
            {
                Assert.IsTrue(nav.IsOnMesh(passage.Destination), passage.Name);

                var ground = nav.GroundHeight(passage.Destination);

                Assert.IsNotNull(ground, passage.Name);
                Assert.AreEqual(ground.Value, passage.Destination.Y, 0.35f, passage.Name);
            }

            // In the shrine: the floor runs unbroken to the logos, which is straight ahead (+Z; 0 faces -Z).
            nav.FindPath(door.Destination, new Vector3(EnhanceLogos.X, door.Destination.Y, EnhanceLogos.Z - 5f), out var toLogos);
            Assert.IsTrue(toLogos, "no floor from the shrine's corridor to the logos");
            Assert.AreEqual(EnhanceLogos.X, door.Destination.X, 0.01f);
            Assert.IsTrue(EnhanceLogos.Z > door.Destination.Z);
            Assert.AreEqual(MathF.PI, door.Rotation, 1e-4f);
            Assert.AreEqual(EnhanceLogos.Y, door.Destination.Y, 2f);

            // Back in the cave: the floor runs unbroken down the tunnels (-Z), which is straight ahead.
            nav.FindPath(exit.Destination, new Vector3(835f, 285.2f, 650f), out var toTunnels);
            Assert.IsTrue(toTunnels, "no floor from the cave corridor back down the tunnels");
            Assert.AreEqual(0f, exit.Rotation, 1e-4f);

            // And nothing but the passages joins the two: the reason they exist.
            nav.FindPath(exit.Destination, door.Destination, out var onFoot);
            Assert.IsFalse(onFoot, "the cave corridor and the shrine are joined on foot");
        }

        [TestMethod]
        public void TheFloorRunsFromEachArrivalIntoTheOtherPassage()
        {
            var nav = WildernessNavMesh();

            foreach (var doorway in new[] { SecretPassages.AliaCavernsDoorway, SecretPassages.EnhanceShrineDoorway })
            {
                // Where the corridor's floor still is, a metre and a half out from the alcove's wall: inside the passage.
                var before = doorway.Position + doorway.Out * 1.5f + new Vector3(0f, 0.2f, 0f);

                Assert.IsTrue(nav.IsOnMesh(before), $"no floor before {doorway.Name}");
                Assert.IsTrue(doorway.Passage.Contains(before), $"the floor before {doorway.Name} is not in its passage");

                // And whoever arrives in this corridor can walk there.
                var arriving = SecretPassages.All.Single(passage => passage != doorway.Passage && passage.MapContextId == doorway.MapContextId &&
                    Vector3.Distance(passage.Destination, doorway.Position) < 20f);

                nav.FindPath(arriving.Destination, before, out var reached);
                Assert.IsTrue(reached, $"no floor from where {arriving.Name} ends to {doorway.Name}");
            }
        }

        [TestMethod]
        public void WalkingUpToTheAliaCavernsAlcoveTeleportsThePlayerIntoTheShrineAndBackAgain()
        {
            using var world = new WorldTestContext();

            world.Map.NavMesh = WildernessNavMesh();

            var saved = 0;
            var now = 1000L;
            var manager = Travel(world, () => saved++, () => now);
            var client = world.CreateClient();
            var player = client.Player;

            CellManager.Instance.AddToWorld(client);
            player.PlaceAt(new Vector3(835f, 286.2f, 732f));
            player.MoveBudget = 60;
            WorldTestContext.Drain(client);

            // Up the corridor and onto the disc: ordinary steps.
            Assert.IsTrue(client.HandleMovement(new Movement(new Vector3(835f, 286.2f, 734.5f), Vector2.Zero)));
            Assert.IsTrue(client.HandleMovement(new Movement(new Vector3(835f, 286.2f, 736f), Vector2.Zero)));
            Assert.AreEqual(new Vector3(835f, 286.2f, 736f), player.Position);
            Assert.AreEqual(ClientState.Ingame, client.State);

            // Up to the niche: the teleport-out effect, where they stand, held there while it plays.
            Assert.IsTrue(client.HandleMovement(new Movement(new Vector3(835f, 286.2f, 737.5f), Vector2.Zero)));
            Assert.AreEqual(new Vector3(835f, 286.2f, 737.5f), player.Position);
            Assert.AreEqual(ClientState.Ingame, client.State);
            Assert.IsNotNull(client.PendingTransfer);

            var held = Methods(client);
            Assert.IsTrue(held.Any(packet => packet is RequestMovementBlockPacket), "not held where they stand");
            Assert.IsTrue(held.Any(packet => packet is PreTeleportPacket), "no teleport effect where they stood");
            Assert.IsFalse(held.Any(packet => packet is TeleportPacket || packet is BeginTeleportPacket), "moved before the effect has played");

            // A step while it plays is not taken, and nor is the passage again.
            Assert.IsFalse(client.HandleMovement(new Movement(new Vector3(835f, 286.2f, 737f), Vector2.Zero)));
            manager.PassagesWorker(world.Map);
            now += DynamicObjectManager.PassageHoldMs - 1;
            manager.PassagesWorker(world.Map);
            Assert.AreEqual(new Vector3(835f, 286.2f, 737.5f), player.Position, "moved before the effect has played");

            // Then taken, as a waypoint takes them.
            now++;
            manager.PassagesWorker(world.Map);
            Assert.AreEqual(SecretPassages.AliaCavernsDoor.Destination, player.Position);
            Assert.AreEqual(MathF.PI, (float)player.Rotation, 1e-4f);
            Assert.AreEqual(ClientState.Teleporting, client.State);
            Assert.IsNotNull(client.PendingTransfer);
            Assert.AreEqual(CenterCell(SecretPassages.AliaCavernsDoor.Destination), player.Cells[2, 2], "the player still sees the cave's cells");

            var sent = Methods(client);
            var begin = sent.FindIndex(packet => packet is BeginTeleportPacket);
            var teleport = sent.FindIndex(packet => packet is TeleportPacket);

            Assert.IsTrue(begin >= 0 && teleport > begin, "BeginTeleport, Teleport: the order the client answers");
            Assert.AreEqual(SecretPassages.AliaCavernsDoor.Destination, ((TeleportPacket)sent[teleport]).Position);
            Assert.IsFalse(sent.Any(packet => packet is PreTeleportPacket), "the effect played twice");
            Assert.IsFalse(sent.Any(packet => packet is TeleportArrivalPacket), "arrived before the client answered");

            // Held until the client answers: a step sent meanwhile is not taken.
            Assert.IsFalse(client.HandleMovement(new Movement(new Vector3(832f, 160.1f, 930f), Vector2.Zero)));
            Assert.AreEqual(SecretPassages.AliaCavernsDoor.Destination, player.Position);
            Assert.AreEqual(0, saved);

            manager.TeleportAcknowledge(client);

            Assert.AreEqual(ClientState.Ingame, client.State);
            Assert.IsNull(client.PendingTransfer);
            Assert.AreEqual(1, saved);
            Assert.IsFalse(Methods(client).Any(packet => packet is TeleportArrivalPacket),
                "their own client plays the arrival when the Teleport's delay is up; one sent back can beat it and stop it resetting");

            // To the logos and back down the corridor, as ordinary steps.
            player.MoveBudget = 60;
            Assert.IsTrue(client.HandleMovement(new Movement(new Vector3(832f, 160.1f, 933f), Vector2.Zero)));
            Assert.AreEqual(new Vector3(832f, 160.1f, 933f), player.Position);
            Assert.IsTrue(client.HandleMovement(new Movement(new Vector3(832f, 160.1f, 923.5f), Vector2.Zero)));
            Assert.AreEqual(new Vector3(832f, 160.1f, 923.5f), player.Position);

            // Up to the shrine's own alcove: back to the cave, once its effect has played.
            Assert.IsTrue(client.HandleMovement(new Movement(new Vector3(832f, 160.1f, 921.5f), Vector2.Zero)));
            Assert.IsTrue(Methods(client).Any(packet => packet is PreTeleportPacket));
            now += DynamicObjectManager.PassageHoldMs;
            manager.PassagesWorker(world.Map);
            Assert.AreEqual(SecretPassages.EnhanceShrineExit.Destination, player.Position);
            Assert.AreEqual(0f, (float)player.Rotation, 1e-4f);
            Assert.AreEqual(ClientState.Teleporting, client.State);
            Assert.AreEqual(CenterCell(SecretPassages.EnhanceShrineExit.Destination), player.Cells[2, 2], "the player still sees the shrine's cells");
            Assert.IsTrue(Methods(client).Any(packet => packet is TeleportPacket));

            manager.TeleportAcknowledge(client);

            Assert.AreEqual(ClientState.Ingame, client.State);
            Assert.AreEqual(2, saved);

            // And on down the tunnels.
            player.MoveBudget = 60;
            Assert.IsTrue(client.HandleMovement(new Movement(new Vector3(835f, 286.3f, 729f), Vector2.Zero)));
            Assert.AreEqual(new Vector3(835f, 286.3f, 729f), player.Position);
        }

        [TestMethod]
        public void APlayerWhoCannotTravelIsLeftBeforeTheAlcove()
        {
            using var world = new WorldTestContext();

            var manager = Travel(world);
            var client = world.CreateClient();
            var player = client.Player;

            CellManager.Instance.AddToWorld(client);
            player.PlaceAt(new Vector3(835f, 286.2f, 736f));
            player.MoveBudget = 60;
            player.State = CharacterState.Dead;
            WorldTestContext.Drain(client);

            Assert.IsFalse(manager.TakePassage(client, SecretPassages.AliaCavernsDoor.Destination, SecretPassages.AliaCavernsDoor.Rotation));
            Assert.AreEqual(new Vector3(835f, 286.2f, 736f), player.Position);
            Assert.AreEqual(ClientState.Ingame, client.State);
            Assert.IsNull(client.PendingTransfer);
            Assert.AreEqual(0, WorldTestContext.Drain(client).Count);
        }

        [TestMethod]
        public void TheTeleportOutIsSeenWhereTheyStoodAndTheArrivalWhereTheyArrive()
        {
            using var world = new WorldTestContext();

            var now = 1000L;
            var manager = Travel(world, clock: () => now);
            var client = world.CreateClient();
            var player = client.Player;
            var atTheCave = world.CreateClient();
            var atTheShrine = world.CreateClient();

            player.PlaceAt(new Vector3(835f, 286.2f, 736f));
            atTheCave.Player.PlaceAt(new Vector3(835f, 286.2f, 730f));
            atTheShrine.Player.PlaceAt(new Vector3(832f, 160.1f, 935f));

            foreach (var each in new[] { client, atTheCave, atTheShrine })
                CellManager.Instance.AddToWorld(each);
            foreach (var each in new[] { client, atTheCave, atTheShrine })
                WorldTestContext.Drain(each);

            Assert.IsTrue(manager.TakePassage(client, SecretPassages.AliaCavernsDoor.Destination, SecretPassages.AliaCavernsDoor.Rotation));

            Assert.IsTrue(Methods(atTheCave).Any(packet => packet is PreTeleportPacket), "the cave does not see them go");
            Assert.IsFalse(Methods(atTheShrine).Any(packet => packet is PreTeleportPacket), "the shrine sees them go");

            now += DynamicObjectManager.PassageHoldMs;
            manager.PassagesWorker(world.Map);
            WorldTestContext.Drain(atTheShrine);
            manager.TeleportAcknowledge(client);

            Assert.IsTrue(Methods(atTheShrine).Any(packet => packet is TeleportArrivalPacket), "the shrine does not see them arrive");
            Assert.IsFalse(Methods(client).Any(packet => packet is TeleportArrivalPacket));
            Assert.AreEqual(ClientState.Ingame, client.State);
        }

        [TestMethod]
        public void APlayerWhoDiesInTheEffectIsLetGoWhereTheyStand()
        {
            using var world = new WorldTestContext();

            var now = 1000L;
            var manager = Travel(world, clock: () => now);
            var client = world.CreateClient();
            var player = client.Player;
            var watching = world.CreateClient();

            player.PlaceAt(new Vector3(835f, 286.2f, 736f));
            watching.Player.PlaceAt(new Vector3(835f, 286.2f, 730f));
            CellManager.Instance.AddToWorld(client);
            CellManager.Instance.AddToWorld(watching);

            Assert.IsTrue(manager.TakePassage(client, SecretPassages.AliaCavernsDoor.Destination, SecretPassages.AliaCavernsDoor.Rotation));
            Assert.IsFalse(manager.TakePassage(client, SecretPassages.AliaCavernsDoor.Destination, SecretPassages.AliaCavernsDoor.Rotation), "taken twice");
            WorldTestContext.Drain(client);
            WorldTestContext.Drain(watching);

            player.State = CharacterState.Dead;
            now += DynamicObjectManager.PassageHoldMs;
            manager.PassagesWorker(world.Map);

            Assert.AreEqual(new Vector3(835f, 286.2f, 736f), player.Position);
            Assert.IsNull(client.PendingTransfer);
            Assert.AreEqual(ClientState.Ingame, client.State);

            var sent = Methods(client);
            Assert.IsTrue(sent.Any(packet => packet is TeleportFailedPacket), "the effect is not stopped");
            Assert.IsTrue(sent.Any(packet => packet is UnrequestMovementBlockPacket), "still held");
            Assert.IsFalse(sent.Any(packet => packet is TeleportPacket));
            Assert.IsTrue(Methods(watching).Any(packet => packet is TeleportFailedPacket), "the effect is not stopped for those who saw it start");
        }

        // The client finishes a Teleport by where the body is when the Teleport's delay is up
        // (actor.py _TelportMovementCompleted: the camera put behind the body, the area round it
        // loaded, the arrival played), and only the MoveObject moves the body. Sent after the
        // Teleport with 5 ms, the teleport finished where the player had stood: their view stayed
        // in the cave, the alcove went as their cells changed, and the shrine came with their
        // next step.
        [TestMethod]
        public void TheBodyIsMovedBeforeTheTeleportAndTheTeleportWaitsForIt()
        {
            using var world = new WorldTestContext();

            world.Map.NavMesh = WildernessNavMesh();

            var now = 1000L;
            var manager = Travel(world, () => { }, () => now);
            var client = world.CreateClient();
            var player = client.Player;

            CellManager.Instance.AddToWorld(client);
            player.PlaceAt(new Vector3(835f, 286.2f, 736f));
            player.MoveBudget = 60;
            WorldTestContext.Drain(client);

            Assert.IsTrue(client.HandleMovement(new Movement(new Vector3(835f, 286.2f, 737.5f), Vector2.Zero)));
            Assert.IsNotNull(client.PendingTransfer);
            WorldTestContext.Drain(client);

            now += DynamicObjectManager.PassageHoldMs;
            manager.PassagesWorker(world.Map);
            Assert.AreEqual(ClientState.Teleporting, client.State);

            var sent = WorldTestContext.Drain(client).Select(packet => packet.Message).ToList();
            var moved = sent.FindIndex(message => message is MoveObjectMessage move && move.EntityId == player.EntityId);
            var teleport = sent.FindIndex(message => message is CallMethodMessage call && call.Packet is TeleportPacket);
            var begin = sent.FindIndex(message => message is CallMethodMessage call && call.Packet is BeginTeleportPacket);

            Assert.IsTrue(moved >= 0, "the body is moved");
            Assert.IsTrue(teleport >= 0 && begin >= 0, "the teleport is sent");
            Assert.IsTrue(moved < begin && begin < teleport, "the body first, then BeginTeleport, then the Teleport");
            Assert.AreEqual(SecretPassages.AliaCavernsDoor.Destination, ((MoveObjectMessage)sent[moved]).Movement.Position);

            var packet = (TeleportPacket)((CallMethodMessage)sent[teleport]).Packet;
            Assert.AreEqual(DynamicObjectManager.TeleportDelayMs, packet.Delay, "the client is given time to move the body first");
            Assert.IsTrue(packet.Delay >= 200, "a few frames on a slow client");
        }

        private static void AssertNear(Vector3 expected, Vector3 actual, float tolerance, string what)
        {
            Assert.IsTrue(Vector3.Distance(expected, actual) <= tolerance, $"{what}: expected {expected}, was {actual}");
        }

        private static List<PythonPacket> Methods(Rasa.Game.Client client) =>
            WorldTestContext.Drain(client).Select(packet => packet.Message).OfType<CallMethodMessage>().Select(message => message.Packet).ToList();

        private static void Remove(MapChannel map)
        {
            foreach (var doorway in SecretPassages.Doorways)
                if (SecretPassages.DoorwayObject(map, doorway) is { } obj)
                    CellManager.Instance.RemoveFromWorld(map, obj);
        }

        [TestMethod]
        public void EachAlcoveStandsAtItsCorridorsEndOpeningDownTheCorridor()
        {
            // Where the client's maps have the end of each corridor's piece, and which way the corridor runs from it.
            var expected = new (SecretPassages.Doorway Doorway, uint Map, Vector3 Position, Vector3 Out)[]
            {
                (SecretPassages.AliaCavernsDoorway, Wilderness, new Vector3(835f, 286f, 739f), new Vector3(0f, 0f, -1f)),
                (SecretPassages.EnhanceShrineDoorway, Wilderness, new Vector3(832f, 159.93f, 920f), new Vector3(0f, 0f, 1f)),
                (SecretPassages.GrowthHallDoorway, SecretPassages.TordenAbyss, new Vector3(-315f, 199f, -339f), new Vector3(0f, 0f, -1f)),
                (SecretPassages.EarthCaveDoorway, Wilderness, new Vector3(-837.03f, 141f, -684.48f), Vector3.Normalize(new Vector3(-848.34f + 837.03f, 0f, -695.79f + 684.48f))),
                (SecretPassages.FewRoomDoorway, SecretPassages.HowlingMaw, new Vector3(-482f, 123f, -557f), new Vector3(0f, 0f, 1f)),
                (SecretPassages.DonnDoorway, SecretPassages.CavesOfDonn, new Vector3(-328f, 25f, -359f), new Vector3(1f, 0f, 0f)),
                (SecretPassages.DonnEpicDoorway, SecretPassages.CavesOfDonnEpic, new Vector3(-328f, 25f, -359f), new Vector3(1f, 0f, 0f))
            };

            CollectionAssert.AreEquivalent(expected.Select(row => row.Doorway).ToList(), SecretPassages.Doorways);

            foreach (var (doorway, map, position, away) in expected)
            {
                Assert.AreEqual(map, doorway.MapContextId, doorway.Name);
                Assert.AreEqual(position, doorway.Position, doorway.Name);
                AssertNear(away, doorway.Out, 1e-4f, doorway.Name);
                Assert.AreEqual(SecretPassages.ElohAlcove, doorway.ClassId, doorway.Name);
            }

            // The rotation a client is sent is the one its own map gives the corridor's piece.
            AssertNear(new Vector3(0f, 0.3827f, 0.9239f), YawAsSent(SecretPassages.EarthCaveDoorway), 1e-4f, "the Earth cave alcove's turn");
            AssertNear(new Vector3(0f, -0.7071f, 0.7071f), YawAsSent(SecretPassages.DonnDoorway), 1e-4f, "the Caves of Donn alcove's turn");
        }

        /// <summary>The (x, y, w) of the rotation a doorway is sent with; z is 0 for a turn about the vertical.</summary>
        private static Vector3 YawAsSent(SecretPassages.Doorway doorway)
        {
            var rotation = new WorldLocationDescriptorPacket(doorway.Position, doorway.Yaw).Rotation;

            Assert.AreEqual(0f, rotation.Z, 1e-6f);

            return new Vector3(rotation.X, rotation.Y, rotation.W);
        }

        [TestMethod]
        public void ALitAlcoveHasItsPassageBeforeItAndAnUnlitOneHasNone()
        {
            foreach (var doorway in SecretPassages.Doorways)
            {
                var away = doorway.Out;
                var across = new Vector3(away.Z, 0f, -away.X);
                var head = new Vector3(0f, 0.3f, 0f);

                if (doorway.Passage == null)
                {
                    // Unlit, and walking into its niche from down the corridor is a walk and nothing more.
                    Assert.AreEqual(UseObjectState.TsState0, doorway.State, doorway.Name);

                    for (var side = -6f; side <= 6f; side += 1.5f)
                        Assert.IsNull(SecretPassages.Crossed(doorway.MapContextId,
                            doorway.Position + across * side + head + away * 10f, doorway.Position + across * side + head - away * 1.4f), $"{doorway.Name}, {side} m across");

                    continue;
                }

                Assert.AreEqual(UseObjectState.TsState1, doorway.State, doorway.Name);
                Assert.AreEqual(doorway.MapContextId, doorway.Passage.MapContextId, doorway.Name);
                Assert.IsTrue(doorway.Passage.WithEffect, doorway.Name);

                for (var side = -2.9f; side <= 2.9f; side += 0.58f)
                {
                    var at = doorway.Position + across * side + head;

                    // Walking at the alcove: taken 2.5 m out from its wall, and anywhere on to the back of the niche.
                    Assert.AreSame(doorway.Passage,
                        SecretPassages.Crossed(doorway.MapContextId, at + away * 2.6f, at + away * 2.4f), $"{doorway.Name}, {side} m across");
                    Assert.IsNull(SecretPassages.Crossed(doorway.MapContextId, at + away * 5.8f, at + away * 2.6f), $"{doorway.Name}, {side} m across");
                    Assert.IsTrue(doorway.Passage.Contains(at - away * 1.4f), $"{doorway.Name}, the back of the niche {side} m across");
                }

                // Beside the niche's arms, against the alcove's wall: not taken.
                foreach (var side in new[] { -3.2f, 3.2f, -6f, 6f })
                    Assert.IsNull(SecretPassages.Crossed(doorway.MapContextId,
                        doorway.Position + across * side + head + away * 4f, doorway.Position + across * side + head + away * 0.6f), $"{doorway.Name}, {side} m across");

                // Whoever arrives in this corridor by another passage comes out clear of this one, a run's step and more.
                foreach (var arriving in SecretPassages.All.Where(passage => passage != doorway.Passage && passage.MapContextId == doorway.MapContextId &&
                    Vector3.Distance(passage.Destination, doorway.Position) < 20f))
                    Assert.IsTrue(Vector3.Dot(arriving.Destination - doorway.Position, away) > SecretPassages.AlcoveReach + 3f, $"{arriving.Name} ends too near {doorway.Name}");
            }

            Assert.AreSame(SecretPassages.AliaCavernsDoor, SecretPassages.AliaCavernsDoorway.Passage);
            Assert.AreSame(SecretPassages.EnhanceShrineExit, SecretPassages.EnhanceShrineDoorway.Passage);
            Assert.AreSame(SecretPassages.GrowthHallEnd, SecretPassages.GrowthHallDoorway.Passage);
        }

        private static MapChannel Channel(uint mapContextId, string name) => new MapChannel
        {
            MapInfo = new MapInfo(mapContextId, name, 1556, 0),
            ClientList = new List<Rasa.Game.Client>(),
            PlayerLimit = 128
        };

        [TestMethod]
        public void TheDoorwaysArePlacedOnceOnTheirOwnMapAndNowhereElse()
        {
            using var world = new WorldTestContext();

            world.AddClass(SecretPassages.ElohAlcove);

            var divide = Channel(1148, "adv_foreas_concordia_divide");
            var others = new[]
            {
                Channel(SecretPassages.TordenAbyss, "adv_arieki_torden_abyss"),
                Channel(SecretPassages.HowlingMaw, "adv_foreas_howlingmaw1"),
                Channel(SecretPassages.CavesOfDonn, "adv_foreas_concordia_wilderness_cavesofdonn02"),
                Channel(SecretPassages.CavesOfDonnEpic, "adv_foreas_concordia_wilderness_cavesofdonn_epic")
            };

            try
            {
                Assert.AreEqual(0, SecretPassages.PlaceDoorways(null));
                Assert.AreEqual(0, SecretPassages.PlaceDoorways(divide));
                Assert.AreEqual(0, divide.MapCellInfo.Cells.Count);

                Assert.AreEqual(3, SecretPassages.PlaceDoorways(world.Map));
                Assert.AreEqual(0, SecretPassages.PlaceDoorways(world.Map), "placed a second time");

                foreach (var map in others)
                {
                    Assert.AreEqual(1, SecretPassages.PlaceDoorways(map), map.MapInfo.MapName);
                    Assert.AreEqual(0, SecretPassages.PlaceDoorways(map), "placed a second time");
                }

                foreach (var doorway in SecretPassages.Doorways)
                {
                    var map = doorway.MapContextId == Wilderness ? world.Map : others.Single(other => other.MapInfo.MapContextId == doorway.MapContextId);
                    var obj = SecretPassages.DoorwayObject(map, doorway);

                    Assert.IsNotNull(obj, doorway.Name);
                    Assert.AreEqual(DynamicObjectType.Scenery, obj.DynamicObjectType);
                    Assert.AreEqual(SecretPassages.ElohAlcove, obj.EntityClassId);
                    Assert.AreEqual(doorway.Position, obj.Position);
                    Assert.AreEqual(doorway.Yaw, obj.Rotation, 1e-6);
                    Assert.AreEqual(doorway.State, obj.StateId);
                    Assert.AreEqual(doorway.SeenFrom, obj.CellPosition);
                    Assert.AreEqual(doorway.MapContextId, obj.MapContextId);
                    Assert.AreSame(map, obj.RuntimeMapChannel);
                    Assert.AreEqual(1, map.MapCellInfo.Cells.Values.Sum(cell => cell.DynamicObjectList.Count(o => ReferenceEquals(o, obj))));

                    // A doorway of another map is not on this one.
                    foreach (var other in others.Append(world.Map).Where(other => other != map))
                        Assert.IsNull(SecretPassages.DoorwayObject(other, doorway), doorway.Name);
                }
            }
            finally
            {
                Remove(world.Map);

                foreach (var map in others)
                    Remove(map);
            }

            Assert.IsNull(SecretPassages.DoorwayObject(world.Map, SecretPassages.AliaCavernsDoorway));
        }

        [TestMethod]
        public void ADoorwayWhoseClassIsNotLoadedIsLeftOut()
        {
            using var world = new WorldTestContext();

            var classes = EntityClassManager.Instance.LoadedEntityClasses;
            var had = classes.Remove(SecretPassages.ElohAlcove, out var kept);

            try
            {
                Assert.AreEqual(0, SecretPassages.PlaceDoorways(world.Map));
                Assert.IsNull(SecretPassages.DoorwayObject(world.Map, SecretPassages.AliaCavernsDoorway));
                Assert.IsNull(SecretPassages.DoorwayObject(world.Map, SecretPassages.EnhanceShrineDoorway));
            }
            finally
            {
                Remove(world.Map);

                if (had)
                    classes[SecretPassages.ElohAlcove] = kept;
            }
        }

        [TestMethod]
        public void APlayerIsShownTheAlcoveInViewAsLitUntargetableSceneryAndTheOtherOnArrival()
        {
            using var world = new WorldTestContext();

            // A class the world data marks targetable would still be sent as not: scenery is never a target.
            world.AddClass(SecretPassages.ElohAlcove);
            var now = 1000L;
            var manager = Travel(world, clock: () => now);

            DynamicObject plain = null;

            try
            {
                Assert.AreEqual(3, SecretPassages.PlaceDoorways(world.Map));

                // A piece of scenery with no state to stand in, beside it.
                plain = new DynamicObject
                {
                    EntityClassId = SecretPassages.ElohAlcove,
                    DynamicObjectType = DynamicObjectType.Scenery,
                    Position = new Vector3(835f, 286f, 730f),
                    MapContextId = Wilderness,
                    IsInWorld = true
                };
                CellManager.Instance.AddToWorld(world.Map, plain);

                var cave = SecretPassages.DoorwayObject(world.Map, SecretPassages.AliaCavernsDoorway);
                var shrine = SecretPassages.DoorwayObject(world.Map, SecretPassages.EnhanceShrineDoorway);
                var client = world.CreateClient();
                var player = client.Player;

                player.PlaceAt(new Vector3(835f, 286.2f, 734f));
                CellManager.Instance.AddToWorld(client);

                var seen = Methods(client);
                var made = seen.OfType<CreatePhysicalEntityPacket>().Single(packet => packet.EntityId == cave.EntityId);

                Assert.AreEqual(SecretPassages.ElohAlcove, made.ClassId);
                Assert.AreEqual(3, made.EntityData.Count, "the alcove is sent a target flag, a place and its state, and nothing else a usable is");
                Assert.IsFalse(((IsTargetablePacket)made.EntityData[0]).IsTargetable);

                var where = (WorldLocationDescriptorPacket)made.EntityData[1];

                Assert.AreEqual(new Vector3(835f, 286f, 739f), where.Position);
                Assert.IsTrue(MathF.Abs(Quaternion.Dot(where.Rotation, Quaternion.Identity)) > 0.99999f, $"the cave alcove is turned: {where.Rotation}");

                var state = (UsableInfoPacket)made.EntityData[2];

                Assert.IsFalse(state.Enabled, "the alcove can be used");
                Assert.AreEqual(UseObjectState.TsState1, state.CurState, "the alcove is not lit");
                Assert.AreEqual(0u, state.MissionActivated);

                // Scenery with no state is sent none.
                Assert.AreEqual(2, seen.OfType<CreatePhysicalEntityPacket>().Single(packet => packet.EntityId == plain.EntityId).EntityData.Count);

                // 181 m off: not in view from the cave.
                Assert.IsFalse(seen.OfType<CreatePhysicalEntityPacket>().Any(packet => packet.EntityId == shrine.EntityId));

                // Up to the alcove: the shrine's is there on arrival, turned about, and the cave's is gone.
                player.MoveBudget = 60;
                Assert.IsTrue(client.HandleMovement(new Movement(new Vector3(835f, 286.2f, 737f), Vector2.Zero)));
                now += DynamicObjectManager.PassageHoldMs;
                manager.PassagesWorker(world.Map);
                Assert.AreEqual(SecretPassages.AliaCavernsDoor.Destination, player.Position);

                seen = Methods(client);
                made = seen.OfType<CreatePhysicalEntityPacket>().Single(packet => packet.EntityId == shrine.EntityId);
                where = (WorldLocationDescriptorPacket)made.EntityData[1];

                Assert.AreEqual(SecretPassages.ElohAlcove, made.ClassId);
                Assert.AreEqual(new Vector3(832f, 159.93f, 920f), where.Position);
                Assert.IsTrue(MathF.Abs(Quaternion.Dot(where.Rotation, new Quaternion(0f, 1f, 0f, 0f))) > 0.99999f, $"the shrine alcove is not turned about: {where.Rotation}");
                Assert.AreEqual(UseObjectState.TsState1, ((UsableInfoPacket)made.EntityData[2]).CurState);
                Assert.IsTrue(seen.OfType<DestroyPhysicalEntityPacket>().Any(packet => packet.EntityId == cave.EntityId), "the cave alcove was left on the client");

                // The alcove off the Earth logos cave: shown unlit, and from the cave's junction before its corridor.
                var earth = SecretPassages.DoorwayObject(world.Map, SecretPassages.EarthCaveDoorway);
                var other = world.CreateClient();

                other.Player.PlaceAt(new Vector3(-862f, 145.3f, -700f));
                CellManager.Instance.AddToWorld(other);

                made = Methods(other).OfType<CreatePhysicalEntityPacket>().Single(packet => packet.EntityId == earth.EntityId);

                Assert.AreEqual(3, made.EntityData.Count);
                Assert.AreEqual(new Vector3(-837.03f, 141f, -684.48f), ((WorldLocationDescriptorPacket)made.EntityData[1]).Position);
                Assert.IsFalse(((UsableInfoPacket)made.EntityData[2]).Enabled);
                Assert.AreEqual(UseObjectState.TsState0, ((UsableInfoPacket)made.EntityData[2]).CurState, "the Earth cave alcove is lit");
            }
            finally
            {
                if (plain != null)
                    CellManager.Instance.RemoveFromWorld(world.Map, plain);

                Remove(world.Map);
            }
        }

        [TestMethod]
        public void ADoorwayIsShownFromTheWholeLengthOfTheWayToIt()
        {
            using var world = new WorldTestContext();

            world.AddClass(SecretPassages.ElohAlcove);

            try
            {
                Assert.AreEqual(3, SecretPassages.PlaceDoorways(world.Map));

                var cave = SecretPassages.DoorwayObject(world.Map, SecretPassages.AliaCavernsDoorway);
                var shrine = SecretPassages.DoorwayObject(world.Map, SecretPassages.EnhanceShrineDoorway);

                // Filed by where it is seen from, sent as standing where it stands.
                Assert.AreEqual(new Vector3(835f, 286f, 705f), cave.CellPosition);
                Assert.AreEqual(new Vector3(835f, 286f, 739f), cave.Position);
                Assert.AreEqual(new Vector3(832f, 160f, 960f), shrine.CellPosition);

                bool Shown(Vector3 at, DynamicObject obj)
                {
                    var client = world.CreateClient();

                    client.Player.PlaceAt(at);
                    CellManager.Instance.AddToWorld(client);

                    var shown = Methods(client).OfType<CreatePhysicalEntityPacket>().Any(packet => packet.EntityId == obj.EntityId);

                    Assert.AreEqual(shown, CellManager.Instance.ClientsSeeing(world.Map, obj).Contains(client));

                    return shown;
                }

                // The tunnel runs straight at the cave's doorway from z 647, 92 m off; its own cell would give it at 48 m.
                Assert.IsTrue(Shown(new Vector3(835f, 285.2f, 648f), cave), "at the far end of the straight tunnel");
                Assert.IsTrue(Shown(new Vector3(835f, 286f, 695f), cave), "at the corridor's coupler");
                Assert.IsTrue(Shown(new Vector3(835f, 286.2f, 736f), cave), "before the alcove");
                Assert.IsFalse(Shown(new Vector3(835f, 280f, 600f), cave), "outside the cave");

                // The shrine: from its corridor's end to the back wall of the room, 65 m.
                Assert.IsTrue(Shown(new Vector3(832f, 160.1f, 922f), shrine), "at the shrine corridor's end");
                Assert.IsTrue(Shown(new Vector3(832f, 160.1f, 984f), shrine), "at the back of the shrine");
                Assert.IsFalse(Shown(new Vector3(832f, 160.1f, 984f), cave));

                // Taken away again through the same cell it was filed in.
                CellManager.Instance.RemoveFromWorld(world.Map, cave);
                Assert.IsNull(SecretPassages.DoorwayObject(world.Map, SecretPassages.AliaCavernsDoorway));
                Assert.IsFalse(world.Map.MapCellInfo.Cells.Values.Any(cell => cell.DynamicObjectList.Contains(cave)));
            }
            finally
            {
                Remove(world.Map);
            }
        }

        [TestMethod]
        public void APrivateCopyOfTheMapHasItsOwnDoorways()
        {
            using var world = new WorldTestContext();

            world.AddClass(SecretPassages.ElohAlcove);

            var maps = new MapChannelManager(null, privateInstances: new PrivateMapInstanceService());

            maps.MapChannelArray.Add(Wilderness, world.Map);

            MapChannel copy = null;

            try
            {
                Assert.AreEqual(3, SecretPassages.PlaceDoorways(world.Map));

                copy = maps.GetOrCreatePrivateInstance(Wilderness, 7);

                Assert.IsNotNull(copy);
                Assert.AreNotSame(world.Map, copy);

                foreach (var doorway in SecretPassages.Doorways.Where(doorway => doorway.MapContextId == Wilderness))
                {
                    var open = SecretPassages.DoorwayObject(world.Map, doorway);
                    var own = SecretPassages.DoorwayObject(copy, doorway);

                    Assert.IsNotNull(own, doorway.Name);
                    Assert.AreNotSame(open, own);
                    Assert.AreNotEqual(open.EntityId, own.EntityId);
                    Assert.AreSame(copy, own.RuntimeMapChannel);
                    Assert.AreEqual(doorway.Position, own.Position);
                }
            }
            finally
            {
                Remove(copy);
                Remove(world.Map);
            }
        }
    }
}
