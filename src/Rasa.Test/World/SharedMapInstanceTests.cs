extern alias RasaGame;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Text;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using ClientState = RasaGame::Rasa.Data.ClientState;
    using Rasa.Config;
    using Rasa.Context.World;
    using Rasa.Data;
    using Rasa.Game;
    using Rasa.Managers;
    using Rasa.Memory;
    using Rasa.Packets;
    using Rasa.Packets.Communicator.Server;
    using Rasa.Packets.Game.Server;
    using Rasa.Packets.MapChannel.Client;
    using Rasa.Packets.MapChannel.Server;
    using Rasa.Packets.Protocol;
    using Rasa.Services.Preloader;
    using Rasa.Structures;
    using Rasa.Structures.Char;
    using Rasa.Test.Database;

    /// <summary>
    /// Shared copies of a map and the instance picker (MapChannelManager.Instances): when a copy
    /// opens, who is shown the picker and what it lists, where a pick leads, and when a copy closes.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class SharedMapInstanceTests
    {
        private const uint Range = 2374;
        private const uint RangeTemplate = 2377;
        private static readonly Vector3 Arrival = new Vector3(400, 5, 0);

        [TestMethod]
        public void AMapWithNoEntryIsEnteredAsBefore()
        {
            using var world = new WorldTestContext();
            var fixture = new Fixture(world, policy: null);
            var client = fixture.Player();

            Assert.IsTrue(fixture.Maps.EnterMap(client, Range, Arrival, 0));

            Assert.AreSame(fixture.Own, client.PendingTransfer.DestinationMap);
            Assert.IsNull(client.PendingInstanceChoice);
            Assert.AreEqual(0, fixture.Maps.CopiesOf(Range).Count(copy => copy.IsSharedInstance));
            Assert.IsFalse(Sent<ChooseInstanceListPacket>(client).Any());
        }

        [TestMethod]
        public void AnEntryOfOneCopyIsNoEntry()
        {
            using var world = new WorldTestContext();
            var fixture = new Fixture(world, new MapInstanceConfig { Capacity = 1, MaxCopies = 1 });

            fixture.Fill(fixture.Own, 3);

            Assert.IsNull(fixture.Maps.SharedPolicyOf(Range));
            Assert.IsNull(fixture.Maps.OpenSharedCopy(Range));
            Assert.IsTrue(fixture.Maps.EnterMap(fixture.Player(), Range, Arrival, 0), "over its capacity, which counts for nothing");
        }

        [TestMethod]
        public void TheDoorLeadsStraightInWhileThereIsOneCopy()
        {
            using var world = new WorldTestContext();
            var fixture = new Fixture(world, new MapInstanceConfig { Capacity = 2, MaxCopies = 3 });
            var client = fixture.Player();

            Assert.IsTrue(fixture.Maps.EnterMap(client, Range, Arrival, 1.5f));

            Assert.AreSame(fixture.Own, client.PendingTransfer.DestinationMap);
            Assert.AreEqual(Arrival, client.PendingTransfer.DestinationPosition);
            Assert.IsNull(client.PendingInstanceChoice);
            Assert.IsFalse(Sent<ChooseInstanceListPacket>(client).Any());
            Assert.AreEqual(1, fixture.Maps.PopulationOf(fixture.Own), "counted there before the map has loaded");

            fixture.Maps.MapLoaded(client);

            Assert.IsTrue(fixture.Own.ClientList.Contains(client));
            Assert.AreEqual(1, fixture.Maps.PopulationOf(fixture.Own), "and once");
            Assert.AreEqual(0, fixture.Own.Arriving.Count);
        }

        [TestMethod]
        public void PlayersOnTheWayCountSoTheCrowdAtTheDoorDoesNotOverfillACopy()
        {
            using var world = new WorldTestContext();
            var fixture = new Fixture(world, new MapInstanceConfig { Capacity = 2, MaxCopies = 3 });
            var first = fixture.Player();
            var second = fixture.Player();
            var third = fixture.Player();

            Assert.IsTrue(fixture.Maps.EnterMap(first, Range, Arrival, 0));
            Assert.IsTrue(fixture.Maps.EnterMap(second, Range, Arrival, 0));
            Assert.AreEqual(1, fixture.Maps.CopiesOf(Range).Count, "nobody has loaded and the map's own channel is spoken for");

            Assert.IsTrue(fixture.Maps.EnterMap(third, Range, Arrival, 0));

            var copies = fixture.Maps.CopiesOf(Range);
            Assert.AreEqual(2, copies.Count);
            Assert.IsNull(third.PendingTransfer, "asked, not sent");
            CollectionAssert.AreEqual(new[] { copies[1].InstanceId, fixture.Own.InstanceId },
                Sent<ChooseInstanceListPacket>(third).Single().Rows.Select(row => row.InstanceId).ToArray());
        }

        [TestMethod]
        public void ACopyOpensWhenEveryCopyIsFullAndThePickerListsThemAll()
        {
            using var world = new WorldTestContext();
            var fixture = new Fixture(world, new MapInstanceConfig { Capacity = 2, MaxCopies = 3 });
            var client = fixture.Player();

            fixture.Fill(fixture.Own, 2);

            Assert.IsTrue(fixture.Maps.EnterMap(client, Range, Arrival, 0));

            var copies = fixture.Maps.CopiesOf(Range);
            Assert.AreEqual(2, copies.Count);
            Assert.AreSame(fixture.Own, copies[0]);

            var opened = copies[1];
            Assert.IsTrue(opened.IsSharedInstance);
            Assert.IsTrue(opened.IsCopy);
            Assert.IsFalse(opened.IsPrivateInstance);
            Assert.AreEqual(0u, opened.OwnerCharacterId);
            Assert.AreNotEqual(fixture.Own.InstanceId, opened.InstanceId);
            Assert.AreSame(fixture.Own.MapInfo, opened.MapInfo);
            Assert.AreSame(opened, fixture.Maps.FindByContextAndInstance(Range, opened.InstanceId));
            Assert.AreSame(fixture.Own, fixture.Maps.FindByContextId(Range), "the map's own channel is still the map's");

            Assert.IsNull(client.PendingTransfer);
            Assert.AreEqual(ClientState.Ingame, client.State);
            Assert.AreSame(world.Map, client.Player.MapChannel);

            var rows = Sent<ChooseInstanceListPacket>(client).Single().Rows;
            Assert.AreEqual(2, rows.Count);

            Assert.AreEqual(opened.InstanceId, rows[0].InstanceId, "the one with room is first, which is the row the client selects");
            Assert.AreEqual(2, rows[0].Ordinal);
            Assert.AreEqual(MapInstanceStatus.Low, rows[0].Status);
            Assert.AreEqual(RangeTemplate, rows[0].MapTemplateId);
            Assert.AreEqual(0, rows[0].StartGroup);

            Assert.AreEqual(fixture.Own.InstanceId, rows[1].InstanceId);
            Assert.AreEqual(1, rows[1].Ordinal);
            Assert.AreEqual(MapInstanceStatus.Full, rows[1].Status);

            var choice = client.PendingInstanceChoice;
            Assert.AreEqual(Range, choice.MapContextId);
            Assert.AreEqual(Arrival, choice.Position);
            CollectionAssert.AreEquivalent(new[] { fixture.Own.InstanceId, opened.InstanceId }, choice.Offered.ToArray());
        }

        [TestMethod]
        public void NoCopyOpensWhileOneHasRoom()
        {
            using var world = new WorldTestContext();
            var fixture = new Fixture(world, new MapInstanceConfig { Capacity = 2, MaxCopies = 5 });

            fixture.Fill(fixture.Own, 2);
            var second = fixture.Maps.OpenSharedCopy(Range);
            fixture.Fill(second, 1);

            Assert.IsTrue(fixture.Maps.EnterMap(fixture.Player(), Range, Arrival, 0));

            Assert.AreEqual(2, fixture.Maps.CopiesOf(Range).Count);
        }

        [TestMethod]
        public void ThePickerListsTheFullestCopyWithRoomFirstAndTheFullOnesLast()
        {
            using var world = new WorldTestContext();
            var fixture = new Fixture(world, new MapInstanceConfig { Capacity = 4, MaxCopies = 5 });
            var client = fixture.Player();

            fixture.Fill(fixture.Own, 4);
            var second = fixture.Maps.OpenSharedCopy(Range);
            var third = fixture.Maps.OpenSharedCopy(Range);
            var fourth = fixture.Maps.OpenSharedCopy(Range);
            fixture.Fill(second, 1);
            fixture.Fill(third, 3);
            fixture.Fill(fourth, 2);

            Assert.IsTrue(fixture.Maps.EnterMap(client, Range, Arrival, 0));

            var rows = Sent<ChooseInstanceListPacket>(client).Single().Rows;

            CollectionAssert.AreEqual(new[] { third.InstanceId, fourth.InstanceId, second.InstanceId, fixture.Own.InstanceId },
                rows.Select(row => row.InstanceId).ToArray());
            CollectionAssert.AreEqual(new[] { 3, 4, 2, 1 }, rows.Select(row => row.Ordinal).ToArray());
            CollectionAssert.AreEqual(
                new[] { MapInstanceStatus.High, MapInstanceStatus.Medium, MapInstanceStatus.Low, MapInstanceStatus.Full },
                rows.Select(row => row.Status).ToArray());
            Assert.AreEqual(4, fixture.Maps.CopiesOf(Range).Count, "there was room: nothing opened");
        }

        [TestMethod]
        public void ThePopulationWordGoesByHowFullTheCopyIs()
        {
            Assert.AreEqual(MapInstanceStatus.Low, MapChannelManager.StatusOf(0, 32));
            Assert.AreEqual(MapInstanceStatus.Low, MapChannelManager.StatusOf(15, 32));
            Assert.AreEqual(MapInstanceStatus.Medium, MapChannelManager.StatusOf(16, 32));
            Assert.AreEqual(MapInstanceStatus.Medium, MapChannelManager.StatusOf(23, 32));
            Assert.AreEqual(MapInstanceStatus.High, MapChannelManager.StatusOf(24, 32));
            Assert.AreEqual(MapInstanceStatus.High, MapChannelManager.StatusOf(31, 32));
            Assert.AreEqual(MapInstanceStatus.Full, MapChannelManager.StatusOf(32, 32));
            Assert.AreEqual(MapInstanceStatus.Full, MapChannelManager.StatusOf(40, 32));
            Assert.AreEqual(MapInstanceStatus.Full, MapChannelManager.StatusOf(0, 0));
        }

        [TestMethod]
        public void ThePickedCopyIsWhereThePlayerGoes()
        {
            using var world = new WorldTestContext();
            var fixture = new Fixture(world, new MapInstanceConfig { Capacity = 2, MaxCopies = 3 });
            var client = fixture.Player();

            fixture.Fill(fixture.Own, 1);
            var second = fixture.Maps.OpenSharedCopy(Range);

            Assert.IsTrue(fixture.Maps.EnterMap(client, Range, Arrival, 1.5f));
            Assert.IsNull(client.PendingTransfer);

            fixture.Maps.SelectInstance(client, new SelectInstancePacket { InstanceId = second.InstanceId, StartGroup = 0 });

            Assert.IsNull(client.PendingInstanceChoice);
            Assert.AreSame(second, client.PendingTransfer.DestinationMap);
            Assert.AreEqual(Arrival, client.PendingTransfer.DestinationPosition);
            Assert.AreEqual(1.5f, client.PendingTransfer.DestinationRotation);
            Assert.AreEqual(second.InstanceId, Sent<WonkavatePacket>(client).Single().MapInstanceId);
            Assert.AreEqual(1, fixture.Maps.PopulationOf(second));

            fixture.Maps.MapLoaded(client);

            Assert.IsTrue(second.ClientList.Contains(client));
            Assert.IsFalse(fixture.Own.ClientList.Contains(client));
            Assert.AreSame(second, client.Player.MapChannel);
            Assert.AreEqual(1, fixture.Maps.PopulationOf(second));
        }

        [TestMethod]
        public void TheMapsOwnChannelCanBePickedToo()
        {
            using var world = new WorldTestContext();
            var fixture = new Fixture(world, new MapInstanceConfig { Capacity = 2, MaxCopies = 3 });
            var client = fixture.Player();

            fixture.Maps.OpenSharedCopy(Range);
            Assert.IsTrue(fixture.Maps.EnterMap(client, Range, Arrival, 0));

            fixture.Maps.SelectInstance(client, new SelectInstancePacket { InstanceId = fixture.Own.InstanceId });

            Assert.AreSame(fixture.Own, client.PendingTransfer.DestinationMap);
        }

        [TestMethod]
        public void APickThatWasNotOfferedGoesNowhere()
        {
            using var world = new WorldTestContext();
            var fixture = new Fixture(world, new MapInstanceConfig { Capacity = 2, MaxCopies = 3 });
            var client = fixture.Player();
            var stranger = fixture.Player();

            // Nobody asked them anything.
            fixture.Maps.SelectInstance(stranger, new SelectInstancePacket { InstanceId = fixture.Own.InstanceId });
            Assert.IsNull(stranger.PendingTransfer);

            fixture.Maps.OpenSharedCopy(Range);
            Assert.IsTrue(fixture.Maps.EnterMap(client, Range, Arrival, 0));

            // A copy that opened after the list was sent.
            var later = fixture.Maps.OpenSharedCopy(Range);
            fixture.Maps.SelectInstance(client, new SelectInstancePacket { InstanceId = later.InstanceId });

            Assert.IsNull(client.PendingTransfer);
            Assert.IsNull(client.PendingInstanceChoice, "and the picker's answer is spent");

            fixture.Maps.SelectInstance(client, new SelectInstancePacket { InstanceId = fixture.Own.InstanceId });
            Assert.IsNull(client.PendingTransfer);
        }

        [TestMethod]
        public void ClosingThePickerSpendsIt()
        {
            using var world = new WorldTestContext();
            var fixture = new Fixture(world, new MapInstanceConfig { Capacity = 2, MaxCopies = 3 });
            var client = fixture.Player();

            var second = fixture.Maps.OpenSharedCopy(Range);
            Assert.IsTrue(fixture.Maps.EnterMap(client, Range, Arrival, 0));

            fixture.Maps.SelectInstanceCancel(client);
            fixture.Maps.SelectInstance(client, new SelectInstancePacket { InstanceId = second.InstanceId });

            Assert.IsNull(client.PendingInstanceChoice);
            Assert.IsNull(client.PendingTransfer);
            Assert.AreEqual(ClientState.Ingame, client.State);
        }

        [TestMethod]
        public void APickMadeTooLateOrFromSomewhereElseGoesNowhere()
        {
            using var world = new WorldTestContext();
            var fixture = new Fixture(world, new MapInstanceConfig { Capacity = 2, MaxCopies = 3 });
            var second = fixture.Maps.OpenSharedCopy(Range);

            // Too late.
            var late = fixture.Player();
            Assert.IsTrue(fixture.Maps.EnterMap(late, Range, Arrival, 0));
            fixture.Now += MapChannelManager.InstanceChoiceSeconds * 1000L + 1;
            fixture.Maps.SelectInstance(late, new SelectInstancePacket { InstanceId = second.InstanceId });
            Assert.IsNull(late.PendingTransfer);

            // Walked off.
            var walker = fixture.Player();
            Assert.IsTrue(fixture.Maps.EnterMap(walker, Range, Arrival, 0));
            walker.Player.Position += new Vector3(MapChannelManager.InstanceChoiceReach + 1, 0, 0);
            fixture.Maps.SelectInstance(walker, new SelectInstancePacket { InstanceId = second.InstanceId });
            Assert.IsNull(walker.PendingTransfer);

            // Dead.
            var dead = fixture.Player();
            Assert.IsTrue(fixture.Maps.EnterMap(dead, Range, Arrival, 0));
            dead.Player.State = CharacterState.Dead;
            fixture.Maps.SelectInstance(dead, new SelectInstancePacket { InstanceId = second.InstanceId });
            Assert.IsNull(dead.PendingTransfer);

            // Within the time and the reach: on their way.
            var prompt = fixture.Player();
            Assert.IsTrue(fixture.Maps.EnterMap(prompt, Range, Arrival, 0));
            fixture.Now += MapChannelManager.InstanceChoiceSeconds * 1000L;
            prompt.Player.Position += new Vector3(MapChannelManager.InstanceChoiceReach - 1, 0, 0);
            fixture.Maps.SelectInstance(prompt, new SelectInstancePacket { InstanceId = second.InstanceId });
            Assert.AreSame(second, prompt.PendingTransfer.DestinationMap);
        }

        [TestMethod]
        public void ACopyThatFilledMeanwhileIsRefusedAndThePickerShownAfresh()
        {
            using var world = new WorldTestContext();
            var fixture = new Fixture(world, new MapInstanceConfig { Capacity = 2, MaxCopies = 3 });
            var client = fixture.Player();

            var second = fixture.Maps.OpenSharedCopy(Range);
            Assert.IsTrue(fixture.Maps.EnterMap(client, Range, Arrival, 0));
            WorldTestContext.Drain(client);

            fixture.Fill(second, 2);
            fixture.Maps.SelectInstance(client, new SelectInstancePacket { InstanceId = second.InstanceId });

            Assert.IsNull(client.PendingTransfer);

            var packets = Packets(client);
            Assert.AreEqual("That instance is full.", packets.OfType<SystemMessagePacket>().Single().TextMessage);

            var rows = packets.OfType<ChooseInstanceListPacket>().Single().Rows;
            Assert.AreEqual(fixture.Own.InstanceId, rows[0].InstanceId);
            Assert.AreEqual(MapInstanceStatus.Full, rows.Single(row => row.InstanceId == second.InstanceId).Status);
            Assert.IsNotNull(client.PendingInstanceChoice);
        }

        [TestMethod]
        public void ACopyThatClosedMeanwhileIsRefusedAndThePlayerSentOn()
        {
            using var world = new WorldTestContext();
            var fixture = new Fixture(world, new MapInstanceConfig { Capacity = 2, MaxCopies = 3 });
            var client = fixture.Player();

            var second = fixture.Maps.OpenSharedCopy(Range);
            Assert.IsTrue(fixture.Maps.EnterMap(client, Range, Arrival, 0));
            Assert.IsTrue(fixture.Maps.CloseSharedCopy(second));
            WorldTestContext.Drain(client);

            fixture.Maps.SelectInstance(client, new SelectInstancePacket { InstanceId = second.InstanceId });

            // One copy is left, so there is nothing to pick between.
            Assert.AreSame(fixture.Own, client.PendingTransfer.DestinationMap);
            Assert.AreEqual("That instance has closed.",
                Sent<SystemMessagePacket>(client).Single().TextMessage);
        }

        [TestMethod]
        public void APlayerWhoLeftAMatchIsLedBackToTheCopyTheyLeftWithNoPicker()
        {
            using var world = new WorldTestContext();
            var fixture = new Fixture(world, new MapInstanceConfig { Capacity = 2, MaxCopies = 3 });
            using var grounds = new Grounds(fixture);
            var leaver = fixture.Player();
            var other = fixture.Player();

            var second = fixture.Maps.OpenSharedCopy(Range);
            fixture.Fill(fixture.Own, 1);
            grounds.Of.Lock(leaver.Player.Id, Range, second.InstanceId);

            Assert.IsTrue(fixture.Maps.EnterMap(leaver, Range, Arrival, 1.5f));

            Assert.AreSame(second, leaver.PendingTransfer.DestinationMap, "not the fuller one, which anybody else is offered first");
            Assert.AreEqual(Arrival, leaver.PendingTransfer.DestinationPosition);
            Assert.IsNull(leaver.PendingInstanceChoice);
            Assert.AreEqual(0, Packets(leaver).Count(packet => packet is ChooseInstanceListPacket || packet is SystemMessagePacket));

            // Anybody else is asked.
            Assert.IsTrue(fixture.Maps.EnterMap(other, Range, Arrival, 0));
            Assert.IsNull(other.PendingTransfer);
            Assert.AreEqual(1, Sent<ChooseInstanceListPacket>(other).Count);

            // The map's own channel is a copy like any other.
            var first = fixture.Player();
            grounds.Of.Lock(first.Player.Id, Range, fixture.Own.InstanceId);
            Assert.IsTrue(fixture.Maps.EnterMap(first, Range, Arrival, 0));
            Assert.AreSame(fixture.Own, first.PendingTransfer.DestinationMap);
        }

        [TestMethod]
        public void TheDoorLeadsALockedOutPlayerNowhereWhenTheCopyTheyLeftIsFullOrClosed()
        {
            using var world = new WorldTestContext();
            var fixture = new Fixture(world, new MapInstanceConfig { Capacity = 2, MaxCopies = 3 });
            using var grounds = new Grounds(fixture);
            var leaver = fixture.Player();

            var second = fixture.Maps.OpenSharedCopy(Range);
            grounds.Of.Lock(leaver.Player.Id, Range, second.InstanceId);
            fixture.Fill(second, 2);

            Assert.IsTrue(fixture.Maps.EnterMap(leaver, Range, Arrival, 0), "told so: nothing went wrong");
            Assert.IsNull(leaver.PendingTransfer, "the map's own channel has room, and is not theirs");
            Assert.IsNull(leaver.PendingInstanceChoice);

            var packets = Packets(leaver);
            Assert.IsFalse(packets.OfType<ChooseInstanceListPacket>().Any());
            Assert.AreEqual("You left a match in progress and that instance is full: no other is open to you for 15 more minutes.",
                packets.OfType<SystemMessagePacket>().Single().TextMessage);

            // Every copy full: no copy is opened for somebody who could not go into it.
            fixture.Fill(fixture.Own, 2);
            Assert.IsTrue(fixture.Maps.EnterMap(leaver, Range, Arrival, 0));
            Assert.AreEqual(2, fixture.Maps.CopiesOf(Range).Count);
            WorldTestContext.Drain(leaver);

            // Closed since: nowhere is theirs.
            grounds.Of.Lock(leaver.Player.Id, Range, 4040);
            fixture.Now += 5 * 60000L;

            Assert.IsTrue(fixture.Maps.EnterMap(leaver, Range, Arrival, 0));
            Assert.IsNull(leaver.PendingTransfer);
            Assert.AreEqual("You left a match in progress and that instance has closed: no other is open to you for 10 more minutes.",
                Sent<SystemMessagePacket>(leaver).Single().TextMessage);

            // Their time up, the door is anybody's door again.
            fixture.Now += 10 * 60000L;

            Assert.IsTrue(fixture.Maps.EnterMap(leaver, Range, Arrival, 0));
            Assert.AreEqual(3, fixture.Maps.CopiesOf(Range).Count, "a copy opened for them");
            Assert.AreEqual(1, Sent<ChooseInstanceListPacket>(leaver).Count);
        }

        [TestMethod]
        public void ALockoutOfAnotherMapOrOfAGameMasterDoesNotChangeTheDoor()
        {
            using var world = new WorldTestContext();
            var fixture = new Fixture(world, new MapInstanceConfig { Capacity = 2, MaxCopies = 3 });
            using var grounds = new Grounds(fixture);
            var elsewhere = fixture.Player();
            var master = fixture.Player();

            var second = fixture.Maps.OpenSharedCopy(Range);
            grounds.Of.Lock(elsewhere.Player.Id, 999, second.InstanceId);
            grounds.Of.Lock(master.Player.Id, Range, second.InstanceId);
            grounds.Of.IsExempt = client => client == master;

            Assert.IsTrue(fixture.Maps.EnterMap(elsewhere, Range, Arrival, 0));
            Assert.AreEqual(1, Sent<ChooseInstanceListPacket>(elsewhere).Count);

            Assert.IsTrue(fixture.Maps.EnterMap(master, Range, Arrival, 0));
            Assert.AreEqual(1, Sent<ChooseInstanceListPacket>(master).Count);

            fixture.Maps.SelectInstance(master, new SelectInstancePacket { InstanceId = fixture.Own.InstanceId });
            Assert.AreSame(fixture.Own, master.PendingTransfer.DestinationMap);
        }

        [TestMethod]
        public void ThePickerRefusesALockedOutPlayerACopyTheyDidNotLeave()
        {
            using var world = new WorldTestContext();
            var fixture = new Fixture(world, new MapInstanceConfig { Capacity = 2, MaxCopies = 3 });
            using var grounds = new Grounds(fixture);
            var client = fixture.Player();

            var second = fixture.Maps.OpenSharedCopy(Range);
            Assert.IsTrue(fixture.Maps.EnterMap(client, Range, Arrival, 0));
            WorldTestContext.Drain(client);

            // The picker was open on their screen before the lockout was theirs.
            grounds.Of.Lock(client.Player.Id, Range, second.InstanceId);
            fixture.Maps.SelectInstance(client, new SelectInstancePacket { InstanceId = fixture.Own.InstanceId });

            Assert.IsNull(client.PendingTransfer);
            Assert.IsNull(client.PendingInstanceChoice, "spent");
            Assert.AreEqual("You left a match in progress: for 15 more minutes you can only play in the instance you left.",
                Sent<SystemMessagePacket>(client).Single().TextMessage);

            Assert.IsTrue(fixture.Maps.EnterMap(client, Range, Arrival, 0));
            Assert.AreSame(second, client.PendingTransfer.DestinationMap);
        }

        [TestMethod]
        public void ASummonDoesNotBringALockedOutPlayerIntoAnotherCopy()
        {
            using var world = new WorldTestContext();
            var fixture = new Fixture(world, new MapInstanceConfig { Capacity = 2, MaxCopies = 3 });
            using var grounds = new Grounds(fixture);
            var second = fixture.Maps.OpenSharedCopy(Range);
            var third = fixture.Maps.OpenSharedCopy(Range);
            var summoner = fixture.Fill(second, 1).Single();
            var traveller = fixture.Player();

            var instance = typeof(MapChannelManager).GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic);
            var previous = instance.GetValue(null);
            var move = typeof(SummonManager).GetMethod("MoveTo", BindingFlags.Static | BindingFlags.NonPublic);
            instance.SetValue(null, fixture.Maps);

            try
            {
                grounds.Of.Lock(traveller.Player.Id, Range, third.InstanceId);
                move.Invoke(null, new object[] { traveller, summoner });

                Assert.IsNull(traveller.PendingTransfer);
                StringAssert.Contains(Sent<SystemMessagePacket>(traveller).Single().TextMessage, "instance you left");

                // Into the one they left, as ever.
                grounds.Of.Lock(traveller.Player.Id, Range, second.InstanceId);
                move.Invoke(null, new object[] { traveller, summoner });

                Assert.AreSame(second, traveller.PendingTransfer.DestinationMap);
            }
            finally
            {
                instance.SetValue(null, previous);
            }
        }

        [TestMethod]
        public void ALoginGoesBackIntoTheCopyItLeft()
        {
            using var world = new WorldTestContext();
            var fixture = new Fixture(world, new MapInstanceConfig { Capacity = 2, MaxCopies = 3 });
            using var grounds = new Grounds(fixture);
            var second = fixture.Maps.OpenSharedCopy(Range);
            var client = fixture.Fill(second, 1).Single();
            var position = new Vector3(-250, 30, 75);

            fixture.Maps.MapLinks = () => new[] { Exit(1, world) };
            client.Player.Position = position;
            second.EmptySince = 5;

            fixture.Relog(client);

            Assert.AreSame(second, fixture.Maps.CopyLeftBy(client.Player.Id, Range));
            Assert.IsNull(fixture.Maps.CopyLeftBy(client.Player.Id, world.Map.MapInfo.MapContextId));

            fixture.Maps.PlaceLogin(client);

            Assert.AreSame(second, client.Player.MapChannel);
            Assert.AreEqual(Range, client.Player.MapContextId);
            Assert.AreEqual(position, client.Player.Position, "where they were");
            Assert.IsNull(client.ArrivalNotice);
            Assert.AreEqual(0, second.EmptySince, "not closed under them");

            // The client is told the copy, and counts in it from now.
            client.State = ClientState.CharacterSelection;
            fixture.Maps.PassClientToMapInstance(client);

            Assert.AreEqual(second.InstanceId, Sent<WonkavatePacket>(client).Single().MapInstanceId);
            Assert.AreEqual(1, fixture.Maps.PopulationOf(second));
            Assert.AreEqual(0, fixture.Maps.PopulationOf(fixture.Own));

            // The map's own channel is a copy like any other.
            var first = fixture.Fill(fixture.Own, 1).Single();
            fixture.Relog(first);
            fixture.Maps.PlaceLogin(first);

            Assert.AreSame(fixture.Own, first.Player.MapChannel);
            Assert.IsNull(first.ArrivalNotice);
        }

        [TestMethod]
        public void ALoginWithNoCopyToGoBackToEntersTheWorldOutsideTheMap()
        {
            using var world = new WorldTestContext();
            var fixture = new Fixture(world, new MapInstanceConfig { Capacity = 2, MaxCopies = 4 });
            using var grounds = new Grounds(fixture);
            var outside = world.Map.MapInfo.MapContextId;
            var second = fixture.Maps.OpenSharedCopy(Range);
            var third = fixture.Maps.OpenSharedCopy(Range);

            // A team's teleporter stays on the map, a link somewhere else is none of its, and of
            // two ways out the first is the door.
            fixture.Maps.MapLinks = () => new[]
            {
                Exit(7, world, new Vector3(1, 1, 1)),
                new MapLink { Id = 2, MapContextId = Range, DestMapContextId = Range, DestPosition = new Vector3(9, 9, 9) },
                new MapLink { Id = 3, MapContextId = outside, DestMapContextId = Range, Position = new Vector3(8, 8, 8) },
                Exit(5, world)
            };

            void IsOutside(Client client, string why, string because)
            {
                Assert.AreSame(world.Map, client.Player.MapChannel, because);
                Assert.AreEqual(outside, client.Player.MapContextId);
                Assert.AreEqual(Door, client.Player.Position);
                Assert.AreEqual(Door, client.Player.ArrivalPosition, "put there, as on any arrival");
                Assert.AreEqual(outside, client.Player.ArrivalMapContextId);
                Assert.AreEqual(1.25, client.Player.Rotation, 0.0001);
                StringAssert.Contains(client.ArrivalNotice, why);
                StringAssert.Contains(client.ArrivalNotice, "outside its door");
            }

            // The server has been restarted since: nobody is known to have been anywhere, and
            // the map's first copy is not where they were.
            var unknown = fixture.Fill(second, 1).Single();
            fixture.Relog(unknown, remember: false);
            fixture.Maps.PlaceLogin(unknown);
            IsOutside(unknown, "has closed", "nothing is known of where they were");

            // Their copy filled while they were away.
            var crowded = fixture.Fill(second, 1).Single();
            fixture.Relog(crowded);
            fixture.Fill(second, 2);
            fixture.Maps.PlaceLogin(crowded);
            IsOutside(crowded, "is full", "no room, and no other copy either");

            // Their copy closed while they were away.
            var late = fixture.Fill(third, 1).Single();
            fixture.Relog(late);
            Assert.IsTrue(fixture.Maps.CloseSharedCopy(third));
            fixture.Maps.PlaceLogin(late);
            IsOutside(late, "has closed", "the copy is gone");

            // A lockout that names another copy than the one they were last in.
            var barred = fixture.Fill(fixture.Own, 1).Single();
            fixture.Relog(barred);
            grounds.Of.Lock(barred.Player.Id, Range, second.InstanceId);
            fixture.Maps.PlaceLogin(barred);
            IsOutside(barred, "you can only play in the instance you left", "shut to them");

            // Said once they are in the world, and once.
            Assert.AreEqual(0, Packets(late).Count);
            MapChannelManager.ShowArrivalNotice(late);
            Assert.AreEqual("The instance you were in has closed. You are outside its door.", Sent<SystemMessagePacket>(late).Single().TextMessage);
            Assert.IsNull(late.ArrivalNotice);
            MapChannelManager.ShowArrivalNotice(late);
            Assert.AreEqual(0, Packets(late).Count);
        }

        [TestMethod]
        public void ALoginIsPlacedOnlyOnAMapOfSeveralCopiesWithAWayOut()
        {
            using var world = new WorldTestContext();
            var outside = world.Map.MapInfo.MapContextId;

            // One copy: where the character's row says, whatever links there are.
            var single = new Fixture(world, policy: null);
            var plain = single.Fill(single.Own, 1).Single();
            single.Maps.MapLinks = () => new[] { Exit(1, world) };
            single.Relog(plain, remember: false);
            single.Maps.RememberCopy(plain);
            single.Maps.PlaceLogin(plain);

            Assert.AreSame(single.Own, plain.Player.MapChannel);
            Assert.IsNull(plain.ArrivalNotice);
            Assert.IsNull(single.Maps.CopyLeftBy(plain.Player.Id, Range), "nothing is noted of a map of one copy");

            // Any other map of a world that has one in copies.
            var fixture = new Fixture(world, new MapInstanceConfig { Capacity = 2, MaxCopies = 3 });
            var elsewhere = fixture.Player();
            fixture.Maps.MapLinks = () => new MapLink[0];
            fixture.Maps.RememberCopy(elsewhere);
            fixture.Maps.PlaceLogin(elsewhere);

            Assert.AreSame(world.Map, elsewhere.Player.MapChannel);
            Assert.IsNull(elsewhere.ArrivalNotice);

            // No link out and none in: nowhere to put them but where they were.
            var stuck = fixture.Fill(fixture.Own, 1).Single();
            fixture.Relog(stuck, remember: false);
            fixture.Maps.PlaceLogin(stuck);

            Assert.AreSame(fixture.Own, stuck.Player.MapChannel);
            Assert.AreEqual(Range, stuck.Player.MapContextId);
            Assert.IsNull(stuck.ArrivalNotice);

            // No link out, but a door in: at the door, on its side.
            fixture.Maps.MapLinks = () => new[]
            {
                new MapLink { Id = 4, MapContextId = 999, DestMapContextId = Range, Position = new Vector3(1, 2, 3) },
                new MapLink { Id = 6, MapContextId = outside, DestMapContextId = Range, Position = new Vector3(60, 3, -20) }
            };
            fixture.Maps.PlaceLogin(stuck);

            Assert.AreSame(world.Map, stuck.Player.MapChannel, "the first whose map is loaded");
            Assert.AreEqual(outside, stuck.Player.MapContextId);
            Assert.AreEqual(new Vector3(60, 3, -20), stuck.Player.Position);
            Assert.AreEqual(0d, stuck.Player.Rotation);
            Assert.IsNotNull(stuck.ArrivalNotice);
        }

        [TestMethod]
        public void LeavingAMapOrTheWorldNotesTheCopyLeft()
        {
            using var world = new WorldTestContext();
            var fixture = new Fixture(world, new MapInstanceConfig { Capacity = 2, MaxCopies = 3 });
            using var grounds = new Grounds(fixture);
            var second = fixture.Maps.OpenSharedCopy(Range);
            var third = fixture.Maps.OpenSharedCopy(Range);
            var client = fixture.Fill(second, 1).Single();

            var instance = typeof(MapChannelManager).GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic);
            var previous = instance.GetValue(null);
            instance.SetValue(null, fixture.Maps);

            try
            {
                ManifestationManager.Instance.RemovePlayerCharacter(client);
                Assert.AreSame(second, fixture.Maps.CopyLeftBy(client.Player.Id, Range));

                // The last one left is the one that counts.
                client.Player.MapChannel = third;
                ManifestationManager.Instance.RemovePlayerCharacter(client);
                Assert.AreSame(third, fixture.Maps.CopyLeftBy(client.Player.Id, Range));

                // Leaving a map of one copy changes nothing.
                client.Player.MapChannel = world.Map;
                ManifestationManager.Instance.RemovePlayerCharacter(client);
                Assert.AreSame(third, fixture.Maps.CopyLeftBy(client.Player.Id, Range));
            }
            finally
            {
                instance.SetValue(null, previous);
            }
        }

        [TestMethod]
        public void WithEveryCopyFullAndNoMoreAllowedTheDoorStaysShut()
        {
            using var world = new WorldTestContext();
            var fixture = new Fixture(world, new MapInstanceConfig { Capacity = 1, MaxCopies = 2 });
            var client = fixture.Player();

            fixture.Fill(fixture.Own, 1);
            fixture.Fill(fixture.Maps.OpenSharedCopy(Range), 1);

            Assert.IsNull(fixture.Maps.OpenSharedCopy(Range));
            Assert.IsTrue(fixture.Maps.EnterMap(client, Range, Arrival, 0), "told so: nothing went wrong");

            Assert.AreEqual(2, fixture.Maps.CopiesOf(Range).Count);
            Assert.IsNull(client.PendingTransfer);
            Assert.IsNull(client.PendingInstanceChoice);

            var packets = Packets(client);
            Assert.AreEqual(1, packets.OfType<SystemMessagePacket>().Count());
            Assert.IsFalse(packets.OfType<ChooseInstanceListPacket>().Any());
        }

        [TestMethod]
        public void ACopyClosesOnceItHasStoodEmptyLongEnough()
        {
            using var world = new WorldTestContext();
            var fixture = new Fixture(world, new MapInstanceConfig { Capacity = 2, MaxCopies = 3, IdleCloseSeconds = 10 });

            var second = fixture.Maps.OpenSharedCopy(Range);
            var third = fixture.Maps.OpenSharedCopy(Range);
            var guest = fixture.Fill(third, 1).Single();

            fixture.Maps.SharedInstanceWorker();
            fixture.Now += 9999;
            fixture.Maps.SharedInstanceWorker();

            Assert.AreEqual(3, fixture.Maps.CopiesOf(Range).Count);

            fixture.Now += 1;
            fixture.Maps.SharedInstanceWorker();

            CollectionAssert.AreEqual(new[] { fixture.Own, third }, fixture.Maps.CopiesOf(Range));
            Assert.IsNull(fixture.Maps.FindByContextAndInstance(Range, second.InstanceId));

            // The last one out starts the clock, not the opening.
            third.ClientList.Remove(guest);
            fixture.Now += 60000;
            fixture.Maps.SharedInstanceWorker();
            Assert.AreEqual(2, fixture.Maps.CopiesOf(Range).Count);

            fixture.Now += 10000;
            fixture.Maps.SharedInstanceWorker();

            CollectionAssert.AreEqual(new[] { fixture.Own }, fixture.Maps.CopiesOf(Range));
            Assert.AreSame(fixture.Own, fixture.Maps.FindByContextId(Range), "the map's own channel stands, empty as it is");
        }

        [TestMethod]
        public void ACopySomebodyIsOnTheWayToIsNotClosed()
        {
            using var world = new WorldTestContext();
            var fixture = new Fixture(world, new MapInstanceConfig { Capacity = 2, MaxCopies = 3, IdleCloseSeconds = 0 });
            var client = fixture.Player();

            var second = fixture.Maps.OpenSharedCopy(Range);
            Assert.IsTrue(fixture.Maps.EnterMap(client, Range, Arrival, 0));
            fixture.Maps.SelectInstance(client, new SelectInstancePacket { InstanceId = second.InstanceId });

            fixture.Maps.SharedInstanceWorker();
            fixture.Now += 5000;
            fixture.Maps.SharedInstanceWorker();

            Assert.IsFalse(fixture.Maps.CloseSharedCopy(second));
            Assert.AreEqual(2, fixture.Maps.CopiesOf(Range).Count);

            fixture.Maps.MapLoaded(client);
            Assert.IsTrue(second.ClientList.Contains(client));
        }

        [TestMethod]
        public void OnlyAnEmptySharedCopyCanBeClosed()
        {
            using var world = new WorldTestContext();
            var fixture = new Fixture(world, new MapInstanceConfig { Capacity = 2, MaxCopies = 3 });
            var second = fixture.Maps.OpenSharedCopy(Range);
            var guest = fixture.Fill(second, 1).Single();
            var owned = fixture.Maps.GetOrCreatePrivateInstance(Range, 77);

            Assert.IsFalse(fixture.Maps.CloseSharedCopy(fixture.Own));
            Assert.IsFalse(fixture.Maps.CloseSharedCopy(owned));
            Assert.IsFalse(fixture.Maps.CloseSharedCopy(second));
            Assert.IsFalse(fixture.Maps.CloseSharedCopy(null));

            second.ClientList.Remove(guest);

            Assert.IsTrue(fixture.Maps.CloseSharedCopy(second));
            Assert.IsFalse(fixture.Maps.CloseSharedCopy(second), "once");
            Assert.AreSame(owned, fixture.Maps.FindOwnedPrivateInstance(Range, 77), "a private instance is not one of the map's copies");
            Assert.AreEqual(1, fixture.Maps.CopiesOf(Range).Count);
        }

        [TestMethod]
        public void TheWorkerTicksEverySharedCopyOnce()
        {
            using var world = new WorldTestContext();
            var fixture = new Fixture(world, new MapInstanceConfig { Capacity = 2, MaxCopies = 3 });
            var second = fixture.Maps.OpenSharedCopy(Range);
            var third = fixture.Maps.OpenSharedCopy(Range);

            fixture.Maps.MapChannelWorker(100);

            Assert.AreEqual(100L, fixture.Own.MapChannelElapsed);
            Assert.AreEqual(100L, second.MapChannelElapsed);
            Assert.AreEqual(100L, third.MapChannelElapsed);
        }

        [TestMethod]
        public void ASummonBringsThePlayerIntoTheCopyTheSummonerStandsIn()
        {
            using var world = new WorldTestContext();
            var fixture = new Fixture(world, new MapInstanceConfig { Capacity = 1, MaxCopies = 3 });
            var second = fixture.Maps.OpenSharedCopy(Range);
            var summoner = fixture.Fill(second, 1).Single();
            var traveller = fixture.Player();
            summoner.Player.Position = new Vector3(120, 4, -60);

            var instance = typeof(MapChannelManager).GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic);
            var previous = instance.GetValue(null);
            instance.SetValue(null, fixture.Maps);

            try
            {
                typeof(SummonManager).GetMethod("MoveTo", BindingFlags.Static | BindingFlags.NonPublic)
                    .Invoke(null, new object[] { traveller, summoner });
            }
            finally
            {
                instance.SetValue(null, previous);
            }

            Assert.AreSame(second, traveller.PendingTransfer.DestinationMap, "full or not");
            Assert.AreEqual(new Vector3(120, 4, -60), traveller.PendingTransfer.DestinationPosition);
        }

        [TestMethod]
        public void TheInstanceCommandListsOpensGoesToAndClosesCopies()
        {
            using var world = new WorldTestContext();
            var fixture = new Fixture(world, new MapInstanceConfig { Capacity = 4, MaxCopies = 2, IdleCloseSeconds = 30 });
            var client = fixture.Fill(fixture.Own, 1).Single();
            CellManager.Instance.AddToWorld(client);
            typeof(Client).GetProperty(nameof(Client.AccountEntry)).SetValue(client, new GameAccountEntry { Level = (byte)GmLevel.Admin });

            var commands = new ChatCommandsManager(null);
            commands.RegisterChatCommands();

            List<string> Say(string command)
            {
                WorldTestContext.Drain(client);
                commands.ProcessCommand(client, command);
                return Packets(client).OfType<SystemMessagePacket>().Select(message => message.TextMessage).ToList();
            }

            var instance = typeof(MapChannelManager).GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic);
            var previous = instance.GetValue(null);
            instance.SetValue(null, fixture.Maps);

            try
            {
                var listed = Say(".instance");
                Assert.AreEqual(2, listed.Count);
                StringAssert.Contains(listed[0], "1 of 2 copies, 4 players each");
                StringAssert.Contains(listed[1], "#1 instance 1: 1 player(s), Low (you are here)");

                StringAssert.Contains(Say(".instance pick").Single(), ".instance open first");

                StringAssert.Contains(Say(".instance open").Single(), "Opened #2");
                var second = fixture.Maps.CopiesOf(Range)[1];
                StringAssert.Contains(Say(".instance open").Single(), "already has its 2 copies");

                Assert.AreEqual(3, Say(".instance").Count);
                StringAssert.Contains(Say(".instance go 3").Single(), "no copy #3");
                StringAssert.Contains(Say(".instance go 1").Single(), "You are in that copy");
                StringAssert.Contains(Say(".instance close 1").Single(), "never closed");
                StringAssert.Contains(Say(".instance nonsense").Single(), "usage:");

                // The picker, from where they stand: a row for each copy.
                WorldTestContext.Drain(client);
                commands.ProcessCommand(client, ".instance pick");
                Assert.AreEqual(2, Sent<ChooseInstanceListPacket>(client).Single().Rows.Count);
                fixture.Maps.SelectInstance(client, new SelectInstancePacket { InstanceId = fixture.Own.InstanceId });
                Assert.IsNull(client.PendingTransfer, "the copy they are in");

                Assert.AreEqual(0, Say(".instance go 2").Count);
                Assert.AreSame(second, client.PendingTransfer.DestinationMap);
                fixture.Maps.MapLoaded(client);
                Assert.AreSame(second, client.Player.MapChannel);

                StringAssert.Contains(Say(".instance close 2").Single(), "1 player(s) in it");
                StringAssert.Contains(Say(".instance")[2], "(you are here)");

                Assert.AreEqual(0, Say(".instance go 1").Count);
                fixture.Maps.MapLoaded(client);
                StringAssert.Contains(Say(".instance close 2").Single(), "Closed #2");
                Assert.AreEqual(1, fixture.Maps.CopiesOf(Range).Count);
            }
            finally
            {
                instance.SetValue(null, previous);
            }
        }

        [TestMethod]
        public void TheConfiguredMapsAreReadByTheirContextIds()
        {
            try
            {
                MapInstancePolicies.Apply(new Dictionary<string, MapInstanceConfig>
                {
                    ["2374"] = new MapInstanceConfig { Capacity = 12, MaxCopies = 4, IdleCloseSeconds = 30 },
                    ["wilderness"] = new MapInstanceConfig(),
                    ["0"] = new MapInstanceConfig(),
                    ["1220"] = null
                });

                Assert.AreEqual(12, MapInstancePolicies.For(2374).Capacity);
                Assert.IsNull(MapInstancePolicies.For(1220));
                Assert.IsNull(MapInstancePolicies.For(0));

                MapInstancePolicies.Apply(null);
                Assert.IsNull(MapInstancePolicies.For(2374));
            }
            finally
            {
                MapInstancePolicies.Apply(null);
            }
        }

        [TestMethod]
        public void EdmundRangeRunsInCopiesWithoutAnEntryInTheFile()
        {
            var entry = new RasaGame::Rasa.Config.Config().MapInstances["2374"];

            Assert.AreEqual(32, entry.Capacity);
            Assert.AreEqual(8, entry.MaxCopies);
            Assert.AreEqual(300, entry.IdleCloseSeconds);
            Assert.AreEqual(RangeTemplate, MapTemplates.Of(Range));
            Assert.AreEqual(0u, MapTemplates.Of(1));
        }

        [TestMethod]
        public void TheDoorIsTwoLinksThatDoNotLandInEachOther()
        {
            Assert.AreEqual(2, EdmundRangeDoor.InsertStatements.Length);
            StringAssert.Contains(EdmundRangeDoor.InsertStatements[0], "(9001, 20000009, 9.2683, 40.5004, 136.06, 4.0, 2374, -62.5, 364.2, -412.0,");
            StringAssert.Contains(EdmundRangeDoor.InsertStatements[1], "(9002, 2374, -62.5, 364.2, -421.5, 3.5, 20000009, 9.2683, 40.5004, 136.06,");

            // Arriving in the range is outside the way back out of it, so nobody stands in a
            // link they did not come by.
            var arrival = new Vector3(-62.5f, 364.2f, -412f);
            var exit = new MapLink { Position = new Vector3(-62.5f, 364.2f, -421.5f), Radius = 3.5f };

            Assert.IsFalse(MapLinkManager.Contains(exit, arrival));
            Assert.IsTrue(MapLinkManager.Contains(exit, exit.Position));
        }

        [TestMethod]
        public void TheDoorMigrationPutsBothLinksInTheWorldDatabaseAndTakesThemOut()
        {
            var directory = Path.Combine(AppContext.BaseDirectory, "TestDatabases", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);

            try
            {
                using var context = PersistenceIntegrationTests.CreateContext(typeof(SqliteWorldContext), Path.Combine(directory, "database"));
                var migrator = context.GetService<IMigrator>();

                List<string> Links()
                {
                    var rows = new List<string>();
                    var connection = context.Database.GetDbConnection();

                    if (connection.State != System.Data.ConnectionState.Open)
                        connection.Open();

                    using var command = connection.CreateCommand();
                    command.CommandText = "select id, map_context_id, dest_map_context_id, kind, enabled, comment from map_link where id in (9001, 9002) order by id";

                    using var reader = command.ExecuteReader();

                    while (reader.Read())
                        rows.Add(string.Join("|", Enumerable.Range(0, reader.FieldCount).Select(i => reader.GetValue(i).ToString())));

                    return rows;
                }

                migrator.Migrate();

                CollectionAssert.AreEqual(new[]
                {
                    "9001|20000009|2374|1|1|afs_arena -> edmundrange2",
                    "9002|2374|20000009|1|1|edmundrange2 -> afs_arena"
                }, Links());

                migrator.Migrate("20261105000000_Add_control_points");
                Assert.AreEqual(0, Links().Count);

                migrator.Migrate();
                Assert.AreEqual(2, Links().Count);
            }
            finally
            {
                SqliteConnection.ClearAllPools();
                Directory.Delete(directory, true);
            }
        }

        [TestMethod]
        public void TheDoorMigrationIsTheSameTwoRowsOnMySql()
        {
            using var context = PersistenceIntegrationTests.CreateContext(typeof(MySqlWorldContext), "unused");
            var script = context.GetService<IMigrator>()
                .GenerateScript("20261105000000_Add_control_points", "20261106000000_Add_edmund_range_door");

            foreach (var insert in EdmundRangeDoor.InsertStatements)
                StringAssert.Contains(script, insert);

            StringAssert.Contains(script, "'20261106000000_Add_edmund_range_door'");
        }

        [TestMethod]
        public void ThePickerPacketsAreWhatTheClientReadsAndWrites()
        {
            var packet = new ChooseInstanceListPacket(new List<ChooseInstanceListPacket.Row>
            {
                new ChooseInstanceListPacket.Row { Ordinal = 2, InstanceId = 9, MapTemplateId = RangeTemplate, StartGroup = 0, Status = MapInstanceStatus.Medium },
                new ChooseInstanceListPacket.Row { Ordinal = 1, InstanceId = 1, MapTemplateId = RangeTemplate, StartGroup = 0, Status = MapInstanceStatus.Full }
            });

            Assert.AreEqual(GameOpcode.ChooseInstanceList, packet.Opcode);

            using var stream = new MemoryStream();
            using (var binary = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true))
            using (var writer = new PythonWriter(binary))
                packet.Write(writer);

            stream.Position = 0;
            using var reader = new PythonReader(new BinaryReader(stream));

            Assert.AreEqual(1, reader.ReadTuple());
            Assert.AreEqual(2, reader.ReadList());
            Assert.AreEqual(5, reader.ReadTuple());
            CollectionAssert.AreEqual(new[] { 2, 9, (int)RangeTemplate, 0, 2 }, Enumerable.Range(0, 5).Select(_ => reader.ReadInt()).ToArray());
            Assert.AreEqual(5, reader.ReadTuple());
            CollectionAssert.AreEqual(new[] { 1, 1, (int)RangeTemplate, 0, 4 }, Enumerable.Range(0, 5).Select(_ => reader.ReadInt()).ToArray());

            var picked = Read(new SelectInstancePacket(), w => { w.WriteTuple(2); w.WriteInt(9); w.WriteInt(0); });
            Assert.AreEqual(9u, picked.InstanceId);
            Assert.AreEqual(0, picked.StartGroup);

            var bare = Read(new SelectInstancePacket(), w => { w.WriteTuple(2); w.WriteNoneStruct(); w.WriteNoneStruct(); });
            Assert.AreEqual(0u, bare.InstanceId);
            Assert.IsNull(bare.StartGroup);

            Assert.AreEqual(0u, Read(new SelectInstancePacket(), w => { w.WriteTuple(2); w.WriteInt(-4); w.WriteInt(0); }).InstanceId);

            Read(new SelectInstanceCancelPacket(), w => w.WriteTuple(0));
        }

        /// <summary>Where the way out of the range leads, in the tests of a login.</summary>
        private static readonly Vector3 Door = new Vector3(30, 2, 40);

        /// <summary>A way out of the range to the fixture world's map.</summary>
        private static MapLink Exit(uint id, WorldTestContext world, Vector3? destination = null) => new MapLink
        {
            Id = id,
            MapContextId = Range,
            Position = new Vector3(-62, 364, -421),
            Radius = 3,
            DestMapContextId = world.Map.MapInfo.MapContextId,
            DestPosition = destination ?? Door,
            DestRotation = 1.25f,
            Enabled = true
        };

        private static List<PythonPacket> Packets(Client client) =>
            WorldTestContext.Drain(client).Select(packet => packet.Message).OfType<CallMethodMessage>().Select(message => message.Packet).ToList();

        private static List<T> Sent<T>(Client client) where T : PythonPacket => Packets(client).OfType<T>().ToList();

        private static T Read<T>(T packet, Action<PythonWriter> write) where T : ClientPythonPacket
        {
            using var stream = new MemoryStream();
            using (var binary = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true))
            using (var writer = new PythonWriter(binary))
                write(writer);

            stream.Position = 0;
            using var reader = new PythonReader(new BinaryReader(stream));
            packet.Read(reader);
            return packet;
        }

        /// <summary>
        /// Battlegrounds of a test's own in place of the server's, on the fixture's clock and
        /// with nobody a game master, for the lockouts the door goes by.
        /// </summary>
        private sealed class Grounds : IDisposable
        {
            private static readonly FieldInfo InstanceField = typeof(Battlegrounds).GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic);

            private readonly object _previous = InstanceField.GetValue(null);

            internal Battlegrounds Of { get; }

            internal Grounds(Fixture fixture)
            {
                Of = new Battlegrounds { Now = () => fixture.Now, IsExempt = _ => false };
                InstanceField.SetValue(null, Of);
            }

            public void Dispose() => InstanceField.SetValue(null, _previous);
        }

        /// <summary>
        /// A world of two maps: the fixture's, where the players stand, and the range, which the
        /// policy is for. Its own clock.
        /// </summary>
        private sealed class Fixture
        {
            private readonly WorldTestContext _world;

            internal long Now = 1000;
            internal MapChannelManager Maps { get; }
            internal MapChannel Own { get; }

            internal Fixture(WorldTestContext world, MapInstanceConfig policy)
            {
                _world = world;
                Own = new MapChannel
                {
                    MapInfo = new MapInfo(Range, "adv_wargame_edmundrange2", 24, 0),
                    ClientList = new List<Client>(),
                    PlayerLimit = 128
                };
                Maps = new MapChannelManager(null, () => Now, (_, _, _) => { }, privateInstances: new PrivateMapInstanceService(),
                    refreshStats: (_, _) => { }, assignPlayer: _ => { }, enterMapChannels: _ => { })
                {
                    InstancePolicy = id => id == Range ? policy : null
                };
                Maps.MapChannelArray.Add(world.Map.MapInfo.MapContextId, world.Map);
                Maps.MapChannelArray.Add(Range, Own);
            }

            /// <summary>A player in the world on the fixture's map, with nothing waiting to be read.</summary>
            internal Client Player()
            {
                var client = _world.CreateClient();

                CellManager.Instance.AddToWorld(client);
                WorldTestContext.Drain(client);

                return client;
            }

            /// <summary>Puts that many players on a channel's list, as if they had arrived.</summary>
            internal List<Client> Fill(MapChannel map, int players)
            {
                var added = new List<Client>();

                for (var i = 0; i < players; i++)
                {
                    var client = _world.CreateClient();

                    _world.Map.ClientList.Remove(client);
                    client.Player.MapChannel = map;
                    client.Player.MapContextId = map.MapInfo.MapContextId;
                    map.ClientList.Add(client);
                    added.Add(client);
                }

                return added;
            }

            /// <summary>
            /// Takes a player out of the world and brings them to where a login stands before it
            /// is placed: on the own channel of the map they were on, and on no list.
            /// </summary>
            internal void Relog(Client client, bool remember = true)
            {
                var map = client.Player.MapChannel;

                if (remember)
                    Maps.RememberCopy(client);

                map.ClientList.Remove(client);
                client.Player.MapChannel = Maps.MapChannelArray[map.MapInfo.MapContextId];
                WorldTestContext.Drain(client);
            }
        }
    }
}
