extern alias RasaGame;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;

using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Rasa.Data;
    using Rasa.Game;
    using Rasa.Managers;
    using Rasa.Networking;
    using Rasa.Packets.Party.Server;
    using Rasa.Structures;
    using Rasa.Structures.Char;
    using Rasa.Test.Missions;
    using ClientState = RasaGame::Rasa.Data.ClientState;

    // A connection that ends between maps. A map link, a summon, a teleport or a dropship takes
    // the client off its map's list when the loading screen starts, and only MapLoaded puts it on
    // the next map's; in between it is on no list a map worker looks at, with its character still
    // registered. One that closes there - the client crashed or was closed, the network went, or
    // the load ran past the transfer deadline, which closes it too - leaves the world as any other
    // does: out of the squad, with its health, death penalties and cooldowns saved.
    [TestClass]
    [DoNotParallelize]
    public class LoadingScreenDisconnectTests
    {
        private const int Lead = 1, Mate = 2;
        private const ActionId Cooling = (ActionId)194;

        private readonly List<Client> _online = new List<Client>();
        private readonly List<uint> _parties = new List<uint>();
        private Rasa.Repositories.UnitOfWork.IGameUnitOfWorkFactory _factoryBefore;

        [TestInitialize]
        public void Initialize() => _factoryBefore = Server.GameUnitOfWorkFactory;

        [TestCleanup]
        public void Cleanup()
        {
            Server.GameUnitOfWorkFactory = _factoryBefore;

            foreach (var id in _parties)
                if (PartyManager.Instance.Parties.Remove(id))
                    PartyManager.Instance.FreePartyId(id);

            _parties.Clear();

            lock (Server.Clients)
                foreach (var client in _online)
                    Server.Clients.Remove(client);

            _online.Clear();
        }

        [TestMethod]
        [DataRow(false, DisplayName = "the connection is lost on the loading screen")]
        [DataRow(true, DisplayName = "the load runs past the transfer deadline")]
        public void ASquadLeaderWhoseConnectionEndsBetweenMapsLeavesTheWorld(bool timesOut)
        {
            using var context = MissionTestContext.WithCompletableMission(429);
            long now = 1000;
            var maps = Maps(context, () => now, out var destination);
            var (lead, mate, party) = Squad(context);
            var player = lead.Player;

            var left = Wounded(context, lead);
            player.ActionReuseUntil[Cooling] = Environment.TickCount64 + 600_000;

            Assert.IsTrue(maps.ChangeMap(lead, destination.MapInfo.MapContextId, new Vector3(400, 5, 0), 0));
            Assert.IsFalse(context.Map.ClientList.Contains(lead), "off the map it left");
            Assert.IsFalse(destination.ClientList.Contains(lead), "and not yet on the one it is going to");
            Assert.IsTrue(party.Find(Lead).IsOnline, "a map change is not leaving the squad");
            MissionTestContext.Drain(mate);

            if (timesOut)
            {
                now += 61_000;
                Assert.IsTrue(maps.CheckTransferTimeout(lead));
            }
            else
                lead.Close(false);

            Assert.AreEqual(ClientState.Disconnected, lead.State);

            // The main loop, for each connection it drops.
            maps.CleanupDisconnected(lead);

            // Out of the squad: shown offline, the lead passed on, the others told.
            Assert.IsFalse(party.Find(Lead).IsOnline, "no longer shown as online");
            Assert.AreEqual((uint)Mate, party.PartyLeaderId, "a squad whose leader is away cannot invite");
            Assert.AreEqual((uint)Lead, MissionTestContext.Drain(mate).OfType<RemoveSquadMemberPacket>().Single().UserId);
            Assert.AreEqual(0u, player.PartyId);

            // Saved as they were: health, the death penalties, the cooldown.
            using (var database = context.Open())
            {
                var row = database.CharacterEntries.AsNoTracking().Single(entry => entry.Id == player.Id);

                Assert.AreEqual(left.Health, row.CurrentHealth);
                Assert.AreEqual(left.Armor, row.CurrentArmor);
                Assert.AreEqual(left.Power, row.CurrentPower);
                Assert.AreEqual(2u, row.RezTraumaStacks, "the penalties were set aside for the loading screen, and are kept");
                Assert.AreEqual(left.RezTraumaEndsAt, row.RezTraumaEndsAt, 2000);
                Assert.AreEqual(left.NoHealEndsAt, row.NoHealEndsAt, 2000);
            }

            using (var unit = context.CreateChar())
                Assert.AreEqual((uint)Cooling, unit.CharacterActionReuses.Get(player.Id).Single().ActionId);

            // And out of the world.
            Assert.IsFalse(EntityManager.Instance.Players.ContainsKey(player.EntityId));
            Assert.IsNull(player.MapChannel);
            Assert.IsFalse(context.Map.ClientList.Contains(lead));
            Assert.IsFalse(destination.ClientList.Contains(lead));

            // Clearing up after the same connection again changes nothing.
            maps.CleanupDisconnected(lead);
            Assert.AreEqual(0, MissionTestContext.Drain(mate).OfType<RemoveSquadMemberPacket>().Count());

            // Back in the world on a new connection, they are a member logging in - shown their
            // squad - and not one arriving from another map, who has it already.
            var back = context.CreateAdditionalClient(3);

            typeof(Client).GetProperty(nameof(Client.AccountEntry)).SetValue(back, lead.AccountEntry);
            back.State = ClientState.Ingame;

            lock (Server.Clients)
            {
                Server.Clients.Remove(lead);
                Server.Clients.Add(back);
            }

            _online.Add(back);
            PartyManager.Instance.PlayerEnteredWorld(back);

            var shown = MissionTestContext.Drain(back).ToList();

            Assert.AreEqual(1, shown.OfType<PartyMemberListPacket>().Count(), "sent the squad");
            Assert.IsTrue(shown.OfType<SetCurrentPartyIdPacket>().Any());
            Assert.IsTrue(party.Find(Lead).IsOnline);
            Assert.AreEqual(1, MissionTestContext.Drain(mate).OfType<AddSquadMemberPacket>().Count());
        }

        [TestMethod]
        public void ACharacterThatNeverArrivedIsNotSavedOverWhatItLeftWith()
        {
            using var context = MissionTestContext.WithCompletableMission(429);
            var maps = Maps(context, () => 1000, out _);
            var client = context.Client;
            var player = client.Player;

            // Left the world wounded, and is on the login loading screen: queued for the map, its
            // attributes not worked out yet.
            using (var unit = context.CreateChar())
                unit.Characters.UpdateCharacterVitals(player.Id, 250, 10, 5, 1, long.MaxValue / 2, 0);

            CellManager.Instance.RemoveFromWorld(client);
            context.Map.ClientList.RemoveAll(member => member == client);
            player.Attributes.Clear();
            client.State = ClientState.Loading;
            context.Map.QueuedClients.Enqueue(client);
            Server.GameUnitOfWorkFactory = context;

            client.State = ClientState.Disconnected;
            player.Disconected = true;
            maps.CleanupDisconnected(client);

            using var database = context.Open();
            var row = database.CharacterEntries.AsNoTracking().Single(entry => entry.Id == player.Id);

            Assert.AreEqual(250, row.CurrentHealth);
            Assert.AreEqual(1u, row.RezTraumaStacks);
            Assert.IsFalse(context.Map.QueuedClients.Contains(client));
        }

        private MapChannelManager Maps(MissionTestContext context, Func<long> clock, out MapChannel destination)
        {
            destination = DropshipTravelTests.CreateDestination();

            var maps = new MapChannelManager(null, clock, updateCharacter: (_, _, _) => { },
                refreshStats: (_, _) => { }, assignPlayer: _ => { }, enterMapChannels: _ => { });

            maps.MapChannelArray.Add(context.Map.MapInfo.MapContextId, context.Map);
            maps.MapChannelArray.Add(destination.MapInfo.MapContextId, destination);

            // RelogVitals and ActionReuse save through the server's factory.
            Server.GameUnitOfWorkFactory = context;

            return maps;
        }

        /// <summary>A squad of two in the world on the context's map: its leader, with a connection that can be closed, and a mate.</summary>
        private (Client Lead, Client Mate, Party Party) Squad(MissionTestContext context)
        {
            var lead = context.Client;
            var mate = context.CreateAdditionalClient(Mate);

            foreach (var client in new[] { lead, mate })
            {
                client.State = ClientState.Ingame;

                if (!context.Map.ClientList.Contains(client))
                    context.Map.ClientList.Add(client);

                lock (Server.Clients)
                    Server.Clients.Add(client);

                _online.Add(client);
            }

            typeof(Client).GetProperty(nameof(Client.Socket), BindingFlags.Instance | BindingFlags.Public)
                .SetValue(lead, new LengthedSocket(SizeType.Dword, false));

            var id = PartyManager.Instance.GetPartyId;
            var party = new Party(id, lead.AccountEntry.Id, new List<PartyMember> { new PartyMember(lead), new PartyMember(mate) });

            PartyManager.Instance.Parties[id] = party;
            lead.Player.PartyId = id;
            mate.Player.PartyId = id;
            _parties.Add(id);

            return (lead, mate, party);
        }

        /// <summary>Wounded, with two stacks of Rez Trauma and the no-heal period running; what it would leave the world with.</summary>
        private static SavedVitals Wounded(MissionTestContext context, Client client)
        {
            var player = client.Player;

            foreach (var attribute in new[] { Attributes.Body, Attributes.Mind, Attributes.Spirit, Attributes.Chi, Attributes.Aware, Attributes.Speed, Attributes.Regen })
                player.Attributes[attribute] = new ActorAttributes(attribute, 10, 10, 10, 0, 0);

            player.Attributes[Attributes.Health] = new ActorAttributes(Attributes.Health, 1000, 1000, 1000, 0, 0);
            player.Attributes[Attributes.Armor] = new ActorAttributes(Attributes.Armor, 100, 100, 100, 0, 0);
            player.Attributes[Attributes.Power] = new ActorAttributes(Attributes.Power, 50, 50, 50, 0, 0);

            PlayerDeath.RestorePenalties(context.Map, player, 2, 100_000, 20_000);

            // Short of whatever the penalties left of its maximums.
            player.Attributes[Attributes.Health].Current = Math.Max(1, player.Attributes[Attributes.Health].CurrentMax / 2);
            player.Attributes[Attributes.Armor].Current = player.Attributes[Attributes.Armor].CurrentMax / 2;

            var left = RelogVitals.Capture(player, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());

            Assert.AreNotEqual(CharacterEntry.VitalNotSaved, left.Health);
            Assert.AreEqual(2, left.RezTraumaStacks);
            Assert.IsTrue(left.NoHealEndsAt > 0);

            return left;
        }
    }
}
