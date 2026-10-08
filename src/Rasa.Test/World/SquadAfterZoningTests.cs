extern alias RasaGame;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using ClientState = RasaGame::Rasa.Data.ClientState;
    using Rasa.Data;
    using Rasa.Game;
    using Rasa.Managers;
    using Rasa.Packets.Game.Server;
    using Rasa.Packets.MapChannel.Client;
    using Rasa.Packets.MapChannel.Server;
    using Rasa.Packets.Party.Server;
    using Rasa.Packets.Protocol;
    using Rasa.Structures;
    using Rasa.Structures.Char;

    // Whose manifestation is a squad member's, on each other member's client.
    //
    // The client draws a squad member as one - the squad's name and bars over their head, the live
    // row in the squad window - when it is told AddSquadMember(userId, entityId) and has the
    // entity, or is given the entity afterwards (party.py EnableSquadMemberInUI, and its one-shot
    // OnSquadMemberCreated). The name and the bars go when the entity is destroyed
    // (overheadwindow.py OnEntityDestroying), and making the entity again does not bring them
    // back; the row is not told, and stays as it was.
    //
    // The server destroys and makes a player's entity on another's client whenever one of them
    // changes map, and when one takes a waypoint out of the other's sight on the map they share.
    // So it says whose the entity is where it makes it, takes the member's row out where it
    // destroys it, and tells the members on other maps the id a new arrival is known by.
    [TestClass]
    [DoNotParallelize]
    public class SquadAfterZoningTests
    {
        private static readonly Vector3 Pad = new Vector3(400, 5, 0);

        #region A map change

        [TestMethod]
        public void ASquadThatZonesTogetherIsToldWhoseTheNewEntitiesAre()
        {
            using var world = new WorldTestContext();
            var lead = PlayerDeathTests.Player(world, 0, 0);
            var mate = PlayerDeathTests.Player(world, 10, 0);
            using var squad = new TestSquad(lead, mate);
            var cast = new Cast { [lead] = "lead", [mate] = "mate" };
            var destination = DropshipTravelTests.CreateDestination();
            var maps = Maps(world, destination);

            Zone(maps, mate, destination, Pad);
            Assert.IsTrue(maps.ChangeMap(lead, destination, Pad + new Vector3(5, 0, 0), 0));
            Drain(lead, mate);

            maps.MapLoaded(lead);

            // Made, and then whose: the client lights the entity it has (party.py Recv_AddSquadMember).
            cast.Expect(lead, "create mate", "squad- mate", "squad+ mate");
            cast.Expect(mate, "create lead", "squad- lead", "squad+ lead");
        }

        [TestMethod]
        public void TheOnesLeftBehindLoseTheMembersRowAndTheMemberTheirs()
        {
            using var world = new WorldTestContext();
            var lead = PlayerDeathTests.Player(world, 0, 0);
            var mate = PlayerDeathTests.Player(world, 10, 0);
            using var squad = new TestSquad(lead, mate);
            var cast = new Cast { [lead] = "lead", [mate] = "mate" };
            var destination = DropshipTravelTests.CreateDestination();
            var maps = Maps(world, destination);

            Drain(lead, mate);
            Assert.IsTrue(maps.ChangeMap(mate, destination, Pad, 0));

            // The entity goes and the row with it: nothing else stops the squad window showing the
            // health it last had (partystatuswindow.py HandleEnablePartyMember).
            cast.Expect(lead, "destroy mate", "squad- mate");
            cast.Expect(mate, "destroy lead", "squad- lead");
        }

        [TestMethod]
        public void MembersOnOtherMapsAreToldTheArrivalsEntityAndTheArrivalTheirs()
        {
            using var world = new WorldTestContext();
            var lead = PlayerDeathTests.Player(world, 0, 0);
            var mate = PlayerDeathTests.Player(world, 10, 0);
            var third = PlayerDeathTests.Player(world, 600, 0);
            using var squad = new TestSquad(lead, mate, third);
            var cast = new Cast { [lead] = "lead", [mate] = "mate", [third] = "third" };
            var destination = DropshipTravelTests.CreateDestination();
            var maps = Maps(world, destination);

            Assert.IsTrue(maps.ChangeMap(mate, destination, Pad, 0));
            Drain(lead, mate, third);

            maps.MapLoaded(mate);

            // The id alone, for when the entity comes (party.py OnSquadMemberCreated) and for the
            // talking mark the squad window draws by it until then (party.py OnStartSpeaking).
            cast.Expect(lead, "squad- mate", "squad+ mate");
            cast.Expect(third, "squad- mate", "squad+ mate");

            var told = cast.Told(mate);

            CollectionAssert.AreEquivalent(new[] { "squad- lead", "squad+ lead", "squad- third", "squad+ third" }, told);
            Assert.IsTrue(told.IndexOf("squad- lead") < told.IndexOf("squad+ lead"), "taken out, then put in");
            Assert.IsTrue(told.IndexOf("squad- third") < told.IndexOf("squad+ third"));

            // And the next to arrive is announced where they are made, to those there, once.
            Zone(maps, lead, destination, Pad + new Vector3(5, 0, 0));

            cast.Expect(mate, "create lead", "squad- lead", "squad+ lead");
            cast.Expect(third, "squad- lead", "squad+ lead");
        }

        [TestMethod]
        public void AMemberWhoseSpotIsHeldIsNotToldAndNotAnnounced()
        {
            using var world = new WorldTestContext();
            var lead = PlayerDeathTests.Player(world, 0, 0);
            var mate = PlayerDeathTests.Player(world, 10, 0);
            var third = PlayerDeathTests.Player(world, 600, 0);
            using var squad = new TestSquad(lead, mate, third);
            var cast = new Cast { [lead] = "lead", [mate] = "mate", [third] = "third" };
            var destination = DropshipTravelTests.CreateDestination();
            var maps = Maps(world, destination);

            // Out of the world with their spot held, as PartyManager.RemovePlayer leaves them.
            squad.Party.Find(third.AccountEntry.Id).EntityId = 0;
            third.Player.PartyId = 0;
            Drain(lead, mate, third);

            Zone(maps, mate, destination, Pad);

            cast.Expect(third);
            cast.Expect(mate, "destroy lead", "squad- lead", "squad- lead", "squad+ lead");
        }

        [TestMethod]
        public void AMemberLoggingInIsAnnouncedByTheSquadStateAndNotByTheCells()
        {
            using var world = new WorldTestContext();
            var lead = PlayerDeathTests.Player(world, 0, 0);
            var third = PlayerDeathTests.Player(world, 600, 0);
            var mate = world.CreateClient(10, 0);
            var destination = DropshipTravelTests.CreateDestination();

            Zone(Maps(world, destination), third, destination, Pad);

            using var squad = new TestSquad(lead, mate, third);
            var cast = new Cast { [lead] = "lead", [mate] = "mate", [third] = "third" };

            // A login: the account has a spot in the squad and the player is not in it yet.
            squad.Party.Find(mate.AccountEntry.Id).EntityId = 0;
            mate.Player.PartyId = 0;
            Drain(lead, mate, third);

            CellManager.Instance.AddToWorld(mate);

            cast.Expect(lead, "create mate");
            cast.Expect(mate, "create lead");
            cast.Expect(third);

            PartyManager.Instance.PlayerEnteredWorld(mate);

            cast.Expect(lead, "squad+ mate");
            cast.Expect(third, "squad+ mate");
            Assert.AreEqual(1, WorldTestContext.Drain(mate).Select(p => p.Message).OfType<CallMethodMessage>()
                .Count(m => m.Packet is SquadMemberListPacket), "and they are sent the squad's");
        }

        #endregion

        #region On one map

        [TestMethod]
        public void ASquadMateTakenOutOfSightByAWaypointIsAnnouncedWhenTheyAreGivenBack()
        {
            using var world = new WorldTestContext();
            var lead = PlayerDeathTests.Player(world, 0, 0);
            var mate = PlayerDeathTests.Player(world, 10, 0);
            using var squad = new TestSquad(lead, mate);
            var cast = new Cast { [lead] = "lead", [mate] = "mate" };
            var objects = WaypointTravelTests.CreateManager(world);

            WaypointTravelTests.AddWaypoint(objects, world.Map, 10, Vector3.Zero);
            WaypointTravelTests.AddWaypoint(objects, world.Map, 20, new Vector3(600, 5, 0));
            lead.Player.GainedWaypoints.Add(new CharacterTeleporterEntry(lead.Player.Id, 20, (byte)WaypointType.Waypoint));
            Drain(lead, mate);

            // Held until the client has acknowledged the teleport, and for that long the two are
            // not kept on each other's clients (FarAllies.IsPresent): the entities go.
            objects.SelectWaypoint(lead, new SelectWaypointPacket { MapInstanceId = 1, WaypointId = 20 });

            Assert.AreEqual(ClientState.Teleporting, lead.State);
            cast.Expect(lead, "destroy mate", "squad- mate");
            cast.Expect(mate, "destroy lead", "squad- lead");

            // And are made again from across the map once they are there (FarAllies.Worker).
            objects.TeleportAcknowledge(lead);
            FarAllies.Sync(lead);
            FarAllies.Sync(mate);

            cast.Expect(lead, "create mate", "squad- mate", "squad+ mate");
            cast.Expect(mate, "create lead", "squad- lead", "squad+ lead");
        }

        [TestMethod]
        public void SquadMatesWhoWalkApartAndBackKeepTheirEntitiesAndAreToldNothing()
        {
            using var world = new WorldTestContext();
            var lead = PlayerDeathTests.Player(world, 0, 0);
            var mate = PlayerDeathTests.Player(world, 10, 0);
            using var squad = new TestSquad(lead, mate);
            var cast = new Cast { [lead] = "lead", [mate] = "mate" };

            Drain(lead, mate);

            foreach (var x in new[] { 600f, 10f })
            {
                mate.Player.Position = new Vector3(x, 0, 0);
                CellManager.Instance.UpdateVisibility(mate);
                FarAllies.Sync(lead);
                FarAllies.Sync(mate);
            }

            cast.Expect(lead);
            cast.Expect(mate);
        }

        [TestMethod]
        public void AMemberWhoHasLeftTheSquadIsTakenAwayAsAnyPlayerIs()
        {
            using var world = new WorldTestContext();
            var lead = PlayerDeathTests.Player(world, 0, 0);
            var mate = PlayerDeathTests.Player(world, 600, 0);
            using var squad = new TestSquad(lead, mate);
            var cast = new Cast { [lead] = "lead", [mate] = "mate" };

            FarAllies.Sync(lead);
            FarAllies.Sync(mate);
            cast.Expect(lead, "create mate", "squad- mate", "squad+ mate");
            cast.Expect(mate, "create lead", "squad- lead", "squad+ lead");

            // Out of the squad: their client and the others' have been told so by the squad
            // (RemovePartyMember), which takes the row out for good.
            mate.Player.PartyId = 0;
            FarAllies.Sync(lead);
            FarAllies.Sync(mate);

            cast.Expect(lead, "destroy mate");
            cast.Expect(mate, "destroy lead");
        }

        [TestMethod]
        public void APlayerOfAnotherSquadOrOfNoneIsOnlyMadeAndDestroyed()
        {
            using var world = new WorldTestContext();
            var lead = PlayerDeathTests.Player(world, 0, 0);
            var mate = PlayerDeathTests.Player(world, 10, 0);
            var rival = PlayerDeathTests.Player(world, 600, 0);
            var second = PlayerDeathTests.Player(world, 600, 10);
            var loner = PlayerDeathTests.Player(world, 600, 20);
            using var squad = new TestSquad(lead, mate);
            using var theirs = new TestSquad(rival, second);
            var cast = new Cast { [lead] = "lead", [mate] = "mate", [rival] = "rival", [second] = "second", [loner] = "loner" };

            TestSquad.GiveAccount(loner);
            Drain(lead, mate, rival, second, loner);

            // Into their sight and out of it again.
            foreach (var (x, seen) in new[] { (600f, "create"), (0f, "destroy") })
            {
                lead.Player.Position = new Vector3(x, 0, 0);
                CellManager.Instance.UpdateVisibility(lead);
                FarAllies.Sync(lead);
                FarAllies.Sync(mate);

                CollectionAssert.AreEquivalent(new[] { $"{seen} rival", $"{seen} second", $"{seen} loner" }, cast.Told(lead));
                cast.Expect(rival, $"{seen} lead");
                cast.Expect(second, $"{seen} lead");
                cast.Expect(loner, $"{seen} lead");
                cast.Expect(mate);    // held from across the map throughout
            }
        }

        #endregion

        #region Fixture

        /// <summary>
        /// A squad of these players, the first its leader: each with an account, in the squad's
        /// list and on the server's, as PartyManager has them once they have joined.
        /// </summary>
        internal sealed class TestSquad : IDisposable
        {
            private static uint _lastAccount = 4100;
            private readonly Client[] _members;

            internal Party Party { get; }

            internal TestSquad(params Client[] members)
            {
                _members = members;

                foreach (var client in members)
                    GiveAccount(client);

                lock (Server.Clients)
                    Server.Clients.AddRange(members);

                var id = PartyManager.Instance.GetPartyId;

                Party = new Party(id, members[0].AccountEntry.Id, members.Select(client => new PartyMember(client)).ToList());
                PartyManager.Instance.Parties[id] = Party;

                foreach (var client in members)
                    client.Player.PartyId = id;
            }

            public void Dispose()
            {
                if (PartyManager.Instance.Parties.Remove(Party.Id))
                    PartyManager.Instance.FreePartyId(Party.Id);

                lock (Server.Clients)
                    foreach (var client in _members)
                        Server.Clients.Remove(client);
            }

            internal static void GiveAccount(Client client)
            {
                typeof(Client).GetProperty(nameof(Client.AccountEntry)).SetValue(client, new GameAccountEntry
                {
                    Id = ++_lastAccount,
                    FamilyName = "Fixture",
                    Characters = new List<CharacterEntry>()
                });
            }
        }

        /// <summary>The players of a test by name, to say what each has been told of the others.</summary>
        internal sealed class Cast : Dictionary<Client, string>
        {
            /// <summary>
            /// What the client has been told since it was last read, in order, of the other
            /// players' entities and of whose they are: "create", "destroy", "squad+"
            /// (AddSquadMember) and "squad-" (RemoveSquadMember), each with the player's name.
            /// </summary>
            internal List<string> Told(Client client)
            {
                var told = new List<string>();

                foreach (var packet in WorldTestContext.Drain(client).Select(p => p.Message).OfType<CallMethodMessage>().Select(m => m.Packet))
                {
                    switch (packet)
                    {
                        case CreatePhysicalEntityPacket made when Other(client, made.EntityId) is { } who:
                            told.Add($"create {who}");
                            break;
                        case DestroyPhysicalEntityPacket gone when Other(client, gone.EntityId) is { } who:
                            told.Add($"destroy {who}");
                            break;
                        case AddSquadMemberPacket add:
                            told.Add($"squad+ {Whose(add.UserId, add.EntityId)}");
                            break;
                        case RemoveSquadMemberPacket remove:
                            told.Add($"squad- {Whose(remove.UserId, remove.EntityId)}");
                            break;
                    }
                }

                return told;
            }

            /// <summary>That, and nothing else, in that order.</summary>
            internal void Expect(Client client, params string[] expected)
            {
                var told = Told(client);

                CollectionAssert.AreEqual(expected, told, $"{this[client]} was told: {string.Join(", ", told)}");
            }

            private string Other(Client client, ulong entityId) =>
                this.Where(entry => entry.Key != client && entry.Key.Player.EntityId == entityId).Select(entry => entry.Value).SingleOrDefault();

            /// <summary>The player a squad packet names, which is by account and by entity both.</summary>
            private string Whose(uint userId, ulong entityId) =>
                this.Where(entry => entry.Key.AccountEntry?.Id == userId && entry.Key.Player.EntityId == entityId)
                    .Select(entry => entry.Value).SingleOrDefault() ?? $"?({userId}, {entityId})";
        }

        private static MapChannelManager Maps(WorldTestContext world, MapChannel destination)
        {
            var maps = new MapChannelManager(null, () => 1000, (_, _, _) => { },
                refreshStats: (_, _) => { }, assignPlayer: _ => { }, enterMapChannels: _ => { });

            maps.MapChannelArray.Add(world.Map.MapInfo.MapContextId, world.Map);
            maps.MapChannelArray.Add(destination.MapInfo.MapContextId, destination);

            return maps;
        }

        /// <summary>Through to the other map and arrived.</summary>
        private static void Zone(MapChannelManager maps, Client client, MapChannel destination, Vector3 position)
        {
            Assert.IsTrue(maps.ChangeMap(client, destination, position, 0), "on their way");
            maps.MapLoaded(client);
            Assert.AreEqual(ClientState.Ingame, client.State, "arrived");
        }

        private static void Drain(params Client[] clients)
        {
            foreach (var client in clients)
                WorldTestContext.Drain(client);
        }

        #endregion
    }
}
