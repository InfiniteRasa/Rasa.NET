using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Rasa.Data;
    using Rasa.Game;
    using Rasa.Managers;
    using Rasa.Packets;
    using Rasa.Packets.Party.Both;
    using Rasa.Packets.Party.Client;
    using Rasa.Packets.Party.Server;
    using Rasa.Packets.Protocol;
    using Rasa.Structures;

    /// <summary>
    /// A member arriving in a squad is told its loot method and threshold where they differ from
    /// the ones the member last heard (PartyManager.SendLootRules). They went only to the members
    /// there when the leader changed them: one who joined afterwards, or logged back in, looted by
    /// rules nobody had mentioned. The client keeps neither and prints a line for each
    /// (party.py Recv_ChangePartyLootMethod/Threshold), so nothing is said of rules a new squad has.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class SquadLootRulesTests
    {
        private readonly List<Client> _online = new List<Client>();

        [TestCleanup]
        public void Cleanup()
        {
            lock (Server.Clients)
                foreach (var client in _online)
                    Server.Clients.Remove(client);

            _online.Clear();
        }

        [TestMethod]
        [DataRow(PartyLootMethod.FreeForAll, PartyLootThreshold.Experimental, true, true, DisplayName = "both set")]
        [DataRow(PartyLootMethod.Rotation, PartyLootThreshold.Modified, true, false, DisplayName = "the method set")]
        [DataRow(PartyLootMethod.Individual, PartyLootThreshold.Junk, false, true, DisplayName = "the threshold set")]
        [DataRow(PartyLootMethod.Individual, PartyLootThreshold.Modified, false, false, DisplayName = "a new squad's")]
        public void APlayerJoiningIsToldTheSquadsLootRules(PartyLootMethod method, PartyLootThreshold threshold, bool toldMethod, bool toldThreshold)
        {
            using var world = new WorldTestContext();
            var lead = Named(PlayerDeathTests.Player(world, 0, 0), "Lead");
            var mate = Named(PlayerDeathTests.Player(world, 5, 0), "Mate");
            var joiner = Named(PlayerDeathTests.Player(world, 10, 0), "Joiner");
            using var squad = new SquadAfterZoningTests.TestSquad(lead, mate);
            Online(joiner);

            squad.Party.LootMethod = method;
            squad.Party.LootThreshold = threshold;
            Drain(lead, mate, joiner);

            PartyManager.Instance.InviteUserToPartyByName(lead, new InviteUserToPartyByNamePacket { FamilyName = "Joiner" });
            PartyManager.Instance.PartyInvitationResponse(joiner, new PartyInvitationResponsePacket { Response = true });

            Assert.AreEqual(squad.Party.Id, joiner.Player.PartyId, "joined");

            var told = Sent(joiner);
            AssertTold(told, toldMethod ? method : null, toldThreshold ? threshold : null);

            if (toldMethod || toldThreshold)
                Assert.IsGreaterThan(told.FindLastIndex(p => p is SetCurrentPartyIdPacket), told.FindIndex(IsLootRule), "after the squad is theirs on the client");

            Assert.IsFalse(Sent(lead).Any(IsLootRule), "the members there heard it already");
            Assert.IsFalse(Sent(mate).Any(IsLootRule));
        }

        [TestMethod]
        public void SquadsMergingAreToldWhereTheRulesDifferFromTheSquadTheyLeft()
        {
            using var world = new WorldTestContext();
            var lead = Named(PlayerDeathTests.Player(world, 0, 0), "Lead");
            var mate = Named(PlayerDeathTests.Player(world, 5, 0), "Mate");
            var otherLead = Named(PlayerDeathTests.Player(world, 10, 0), "Other");
            var otherMate = Named(PlayerDeathTests.Player(world, 15, 0), "Another");
            using var squad = new SquadAfterZoningTests.TestSquad(lead, mate);
            using var other = new SquadAfterZoningTests.TestSquad(otherLead, otherMate);

            // Same threshold, different method; and the arrivals' squad was not on a new squad's
            // method, so a new squad's method here is news to them.
            squad.Party.LootMethod = PartyLootMethod.Individual;
            squad.Party.LootThreshold = PartyLootThreshold.Prototype;
            other.Party.LootMethod = PartyLootMethod.FreeForAll;
            other.Party.LootThreshold = PartyLootThreshold.Prototype;
            Drain(lead, mate, otherLead, otherMate);

            // "Other is already in a squad" - Accept - the invitation goes to that squad's leader.
            PartyManager.Instance.InviteSquad(lead, new InviteSquadPacket { FamilyName = "Another" });
            PartyManager.Instance.PartyInvitationResponse(otherLead, new PartyInvitationResponsePacket { Response = true });

            Assert.AreEqual(squad.Party.Id, otherMate.Player.PartyId, "merged");
            Assert.AreEqual(squad.Party.Id, otherLead.Player.PartyId);

            AssertTold(Sent(otherLead), PartyLootMethod.Individual, null);
            AssertTold(Sent(otherMate), PartyLootMethod.Individual, null);
            Assert.IsFalse(Sent(lead).Any(IsLootRule));
        }

        [TestMethod]
        public void AMemberLoggingBackInIsToldAndOneChangingMapIsNot()
        {
            using var world = new WorldTestContext();
            var lead = PlayerDeathTests.Player(world, 0, 0);
            var mate = PlayerDeathTests.Player(world, 5, 0);
            using var squad = new SquadAfterZoningTests.TestSquad(lead, mate);

            squad.Party.LootMethod = PartyLootMethod.Rotation;
            squad.Party.LootThreshold = PartyLootThreshold.Normal;

            // The end of a map change: still online in the squad, and its client has the squad.
            Drain(lead, mate);
            PartyManager.Instance.PlayerEnteredWorld(mate);

            Assert.IsFalse(Sent(mate).Any(IsLootRule), "a map change is not a login");

            // A login: the account has a spot in the squad and the player is not in it yet.
            squad.Party.Find(mate.AccountEntry.Id).EntityId = 0;
            mate.Player.PartyId = 0;
            Drain(lead, mate);

            PartyManager.Instance.PlayerEnteredWorld(mate);

            Assert.AreEqual(squad.Party.Id, mate.Player.PartyId);
            AssertTold(Sent(mate), PartyLootMethod.Rotation, PartyLootThreshold.Normal);
            Assert.IsFalse(Sent(lead).Any(IsLootRule));
        }

        [TestMethod]
        public void ANewSquadIsOnTheRulesTheJoinersAreMeasuredAgainst()
        {
            var party = new Party(1, 1, new List<PartyMember>());

            Assert.AreEqual(Party.NewSquadLootMethod, party.LootMethod);
            Assert.AreEqual(Party.NewSquadLootThreshold, party.LootThreshold);
            Assert.AreEqual(PartyLootMethod.Individual, Party.NewSquadLootMethod, "what the client assumes");
            Assert.AreEqual(LootRolls.DefaultThreshold, LootRolls.ThresholdOf(party));
        }

        private static void AssertTold(List<PythonPacket> told, PartyLootMethod? method, PartyLootThreshold? threshold)
        {
            var methods = told.OfType<ChangePartyLootMethodPacket>().Select(p => p.PartyLootMethod).ToList();
            var thresholds = told.OfType<ChangePartyLootThresholdPacket>().Select(p => p.PartyLootThreshold).ToList();

            CollectionAssert.AreEqual(method.HasValue ? new[] { method.Value } : Array.Empty<PartyLootMethod>(), methods, "the method");
            CollectionAssert.AreEqual(threshold.HasValue ? new[] { threshold.Value } : Array.Empty<PartyLootThreshold>(), thresholds, "the threshold");
        }

        private static bool IsLootRule(PythonPacket packet) => packet is ChangePartyLootMethodPacket || packet is ChangePartyLootThresholdPacket;

        private Client Named(Client client, string familyName)
        {
            client.Player.FamilyName = familyName;
            return client;
        }

        private void Online(Client client)
        {
            SquadAfterZoningTests.TestSquad.GiveAccount(client);

            lock (Server.Clients)
                Server.Clients.Add(client);

            _online.Add(client);
        }

        private static List<PythonPacket> Sent(Client client) =>
            WorldTestContext.Drain(client).Select(p => p.Message).OfType<CallMethodMessage>().Select(m => m.Packet).ToList();

        private static void Drain(params Client[] clients)
        {
            foreach (var client in clients)
                WorldTestContext.Drain(client);
        }
    }
}
