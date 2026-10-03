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
    using Rasa.Packets.Communicator.Server;
    using Rasa.Packets.Trade.Client;
    using Rasa.Packets.Trade.Server;
    using Rasa.Structures;
    using Rasa.Structures.Char;
    using Rasa.Test.Missions;

    // A wargame closed to outsiders (Pvp.MayHelp, WARGAME_BLOCK_INTERACTTIONS): a player in a duel
    // or a squad wargame is healed, repaired, revived, buffed and traded with only by players on
    // their own side of it. A team's match is in BattlegroundTests.
    [TestClass]
    [DoNotParallelize]
    public class PvpOutsideHelpTests
    {
        private const uint FirstAccountId = 940_000;

        private readonly List<Client> _clients = new List<Client>();
        private readonly List<uint> _parties = new List<uint>();

        [TestCleanup]
        public void EndWhatIsLeft()
        {
            foreach (var client in _clients)
            {
                SquadWargames.Instance.PlayerLeft(client);
                Duels.Instance.PlayerLeft(client);
                client.Player.PartyId = 0;
            }

            foreach (var id in _parties)
                if (PartyManager.Instance.Parties.Remove(id))
                    PartyManager.Instance.FreePartyId(id);

            lock (Server.Clients)
                foreach (var client in _clients)
                    Server.Clients.Remove(client);

            Duels.Instance.FindByName = name => null;
        }

        [TestMethod]
        public void ADuellistIsHelpedByNobodyButThemselves()
        {
            using var world = new WorldTestContext();
            var (red, blue, bystander) = Duel(world);

            Assert.IsTrue(Pvp.MayHelp(red.Player, red.Player));
            Assert.IsFalse(Pvp.MayHelp(bystander.Player, red.Player), "an outsider");
            Assert.IsFalse(Pvp.MayHelp(blue.Player, red.Player), "the other side");
            Assert.IsTrue(Pvp.MayHelp(red.Player, bystander.Player), "the rule is about who is helped");
            Assert.IsTrue(Pvp.MayHelp(null, red.Player), "nobody's doing: the world's");
            Assert.IsFalse(Pvp.MayTrade(red.Player, bystander.Player));
            Assert.IsFalse(Pvp.MayTrade(bystander.Player, red.Player));

            Duels.Instance.PlayerLeft(red);

            Assert.IsTrue(Pvp.MayHelp(bystander.Player, red.Player), "over with the duel");
            Assert.IsTrue(Pvp.MayTrade(red.Player, bystander.Player));
        }

        [TestMethod]
        public void AnOutsidersHealAndRepairDoNothingForADuellist()
        {
            using var world = new WorldTestContext();
            var (red, _, bystander) = Duel(world);
            var health = red.Player.Attributes[Attributes.Health];
            var armor = red.Player.Attributes[Attributes.Armor];

            health.Current = 400;
            armor.Current = 100;

            Assert.AreEqual(0, ActorManager.Instance.Heal(red.Player, 200, bystander.Player.EntityId));
            Assert.AreEqual(400, health.Current);
            Assert.AreEqual(0, ActorManager.Instance.RestoreArmor(red.Player, 200, bystander.Player.EntityId));
            Assert.AreEqual(100, armor.Current);

            Assert.AreEqual(200, ActorManager.Instance.Heal(red.Player, 200, red.Player.EntityId), "their own");
            Assert.AreEqual(100, ActorManager.Instance.Heal(red.Player, 100), "and what nobody did: regeneration, a hospital");
            Assert.AreEqual(200, ActorManager.Instance.RestoreArmor(red.Player, 200, red.Player.EntityId));

            // The other way round is nobody's business: a duellist may heal a bystander.
            var theirs = bystander.Player.Attributes[Attributes.Health];
            theirs.Current = 400;
            Assert.AreEqual(200, ActorManager.Instance.Heal(bystander.Player, 200, red.Player.EntityId));

            Duels.Instance.PlayerLeft(red);
            health.Current = 400;
            Assert.AreEqual(200, ActorManager.Instance.Heal(red.Player, 200, bystander.Player.EntityId), "after the duel");
        }

        [TestMethod]
        public void AnOutsidersBuffDoesNotGoOnADuellistNorARevive()
        {
            using var world = new WorldTestContext();
            var (red, _, bystander) = Duel(world);

            var theirs = Buff(world, bystander.Player);
            GameEffectManager.Instance.Attach(world.Map, red.Player, theirs);
            Assert.IsFalse(red.Player.ActiveEffects.ContainsKey(theirs.EffectId));

            var own = Buff(world, red.Player);
            GameEffectManager.Instance.Attach(world.Map, red.Player, own);
            Assert.IsTrue(red.Player.ActiveEffects.ContainsKey(own.EffectId), "their own goes on");

            var given = Buff(world, red.Player);
            GameEffectManager.Instance.Attach(world.Map, bystander.Player, given);
            Assert.IsTrue(bystander.Player.ActiveEffects.ContainsKey(given.EffectId), "and theirs on a bystander");

            // Dead in a wargame closed to the reviver: no offer.
            var state = red.Player.State;
            red.Player.State = CharacterState.Dead;

            try
            {
                Drain(red);
                Assert.IsFalse(PlayerDeath.OfferRevive(world.Map, bystander.Player, red.Player, 100));
                Assert.AreEqual(0, Drain(red).Count);
            }
            finally
            {
                red.Player.State = state;
            }
        }

        [TestMethod]
        public void ADuellistTradesWithNobody()
        {
            using var world = new WorldTestContext();
            var (red, _, bystander) = Duel(world);
            var trades = (TradeManager)Activator.CreateInstance(typeof(TradeManager), nonPublic: true);

            trades.RequestTrade(bystander, new RequestTradePacket { TargetEntityId = (long)red.Player.EntityId });

            var refused = Drain(bystander);
            Assert.AreEqual(1, refused.OfType<TradeDestroyPacket>().Count());
            Assert.AreEqual(PlayerMessage.PmTradeYouAreTooBusy, refused.OfType<DisplayClientMessagePacket>().Single().MsgId,
                "You may not trade with that player at this time.");
            Assert.AreEqual(0, Drain(red).OfType<TradeInvitePacket>().Count());

            trades.RequestTrade(red, new RequestTradePacket { TargetEntityId = (long)bystander.Player.EntityId });
            Assert.AreEqual(PlayerMessage.PmTradeYouAreTooBusy, Drain(red).OfType<DisplayClientMessagePacket>().Single().MsgId, "nor the other way");
            Assert.AreEqual(0, Drain(bystander).OfType<TradeInvitePacket>().Count());

            Duels.Instance.PlayerLeft(red);
            Drain(red);
            Drain(bystander);

            trades.RequestTrade(bystander, new RequestTradePacket { TargetEntityId = (long)red.Player.EntityId });
            Assert.AreEqual(1, Drain(red).OfType<TradeInvitePacket>().Count(), "after the duel");
            trades.RequestCancelTrade(bystander, new RequestCancelTradePacket());
        }

        [TestMethod]
        public void ATradeLeftOpenIsClosedByADuel()
        {
            using var world = new WorldTestContext();
            var red = Named(world, "Red", 0);
            var blue = Named(world, "Blue", 3);
            var bystander = Named(world, "Bystander", 2);
            var trades = (TradeManager)Activator.CreateInstance(typeof(TradeManager), nonPublic: true);

            Duels.Instance.FindByName = name => _clients.FirstOrDefault(c => string.Equals(c.Player.FamilyName, name, StringComparison.OrdinalIgnoreCase));

            trades.RequestTrade(bystander, new RequestTradePacket { TargetEntityId = (long)red.Player.EntityId });
            Assert.AreEqual(1, Drain(red).OfType<TradeInvitePacket>().Count());

            Duels.Instance.ChallengeUserToWargameByName(red, "Blue", 0, 0);
            Duels.Instance.WargameChallengeResponse(blue, true);
            Assert.AreEqual(1, Duels.Instance.Running.Count);
            Drain(red);
            Drain(bystander);

            trades.RequestAcceptTradeRequest(red, new RequestAcceptTradeRequestPacket());

            Assert.AreEqual(0, Drain(red).OfType<TradeCreatePacket>().Count(), "not opened");
            Assert.AreEqual(0, Drain(bystander).OfType<TradeCreatePacket>().Count());
        }

        [TestMethod]
        public void InASquadWargameASquadHelpsItsOwnAndNobodyElseDoes()
        {
            using var world = new WorldTestContext();
            var (redLead, redMate) = Squad(world, "Red", 0);
            var (blueLead, _) = Squad(world, "Blue", 20);
            var bystander = Named(world, "Bystander", 5);

            Assert.IsTrue(Pvp.MayHelp(bystander.Player, redLead.Player), "before it begins");

            Duels.Instance.ChallengeUserToWargameByName(redLead, "BlueLead", 0, 0);
            Assert.IsTrue(SquadWargames.Instance.WargameChallengeResponse(blueLead, true));

            Assert.IsTrue(Pvp.MayHelp(redMate.Player, redLead.Player), "their squad");
            Assert.IsTrue(Pvp.MayHelp(redLead.Player, redMate.Player));
            Assert.IsFalse(Pvp.MayHelp(blueLead.Player, redLead.Player), "the other squad");
            Assert.IsFalse(Pvp.MayHelp(bystander.Player, redLead.Player), "an outsider");
            Assert.IsTrue(Pvp.MayTrade(redLead.Player, redMate.Player));
            Assert.IsFalse(Pvp.MayTrade(redLead.Player, bystander.Player));

            var health = redLead.Player.Attributes[Attributes.Health];
            health.Current = 400;

            Assert.AreEqual(0, ActorManager.Instance.Heal(redLead.Player, 200, bystander.Player.EntityId));
            Assert.AreEqual(200, ActorManager.Instance.Heal(redLead.Player, 200, redMate.Player.EntityId));

            CollectionAssert.AreEquivalent(new[] { redLead.Player, redMate.Player },
                AbilityManager.SquadWithin(world.Map, redLead.Player, 30), "a squad ability still reaches the squad");
        }

        [TestMethod]
        public void AClanFeudIsNotClosed()
        {
            using var world = new WorldTestContext();
            var red = Named(world, "Red", 0);

            Assert.AreEqual(0, Pvp.ClosedWargamesOf(red.Player).Count);
            Assert.IsTrue(Pvp.MayHelp(Named(world, "Other", 2).Player, red.Player));
        }

        #region Fixture

        /// <summary>Red and Blue in a duel, and a bystander next to them.</summary>
        private (Client Red, Client Blue, Client Bystander) Duel(WorldTestContext world)
        {
            var red = Named(world, "Red", 0);
            var blue = Named(world, "Blue", 3);
            var bystander = Named(world, "Bystander", 2);

            Duels.Instance.FindByName = name => _clients.FirstOrDefault(c => string.Equals(c.Player.FamilyName, name, StringComparison.OrdinalIgnoreCase));
            Duels.Instance.ChallengeUserToWargameByName(red, "Blue", 0, 0);
            Duels.Instance.WargameChallengeResponse(blue, true);
            Assert.AreEqual(1, Duels.Instance.Running.Count);

            foreach (var client in new[] { red, blue, bystander })
                Drain(client);

            return (red, blue, bystander);
        }

        /// <summary>A squad of two - a leader and a mate - in the world and on the map, found by name.</summary>
        private (Client Lead, Client Mate) Squad(WorldTestContext world, string name, float x)
        {
            var lead = Named(world, name + "Lead", x);
            var mate = Named(world, name + "Mate", x + 3);

            var id = PartyManager.Instance.GetPartyId;
            PartyManager.Instance.Parties[id] = new Party(id, lead.AccountEntry.Id, new List<PartyMember> { new PartyMember(lead), new PartyMember(mate) });
            lead.Player.PartyId = id;
            mate.Player.PartyId = id;
            _parties.Add(id);

            Duels.Instance.FindByName = n => _clients.FirstOrDefault(c => string.Equals(c.Player.FamilyName, n, StringComparison.OrdinalIgnoreCase));
            return (lead, mate);
        }

        private Client Named(WorldTestContext world, string name, float x)
        {
            var client = world.CreateClient(x, 0);
            client.Player.FamilyName = name;
            client.Player.Attributes[Attributes.Health] = new ActorAttributes(Attributes.Health, 1000, 1000, 1000, 0, 0);
            client.Player.Attributes[Attributes.Armor] = new ActorAttributes(Attributes.Armor, 500, 500, 500, 0, 0);

            var accountId = FirstAccountId + (uint)_clients.Count;
            typeof(Client).GetProperty(nameof(Client.AccountEntry))!.SetValue(client,
                new GameAccountEntry { Id = accountId, SelectedSlot = 0, Characters = new List<CharacterEntry>() });

            _clients.Add(client);

            lock (Server.Clients)
                Server.Clients.Add(client);

            CellManager.Instance.AddToWorld(client);
            Drain(client);

            return client;
        }

        private static GameEffect Buff(WorldTestContext world, Actor source) => new GameEffect
        {
            TypeId = 10000056,
            EffectId = GameEffectManager.Instance.NextEffectId(world.Map),
            EffectLevel = 1,
            SourceId = source.EntityId,
            Source = source,
            SourceLevel = 1,
            IsBuff = true,
            ExpiresTick = Environment.TickCount64 + 60_000
        };

        private static List<PythonPacket> Drain(Client client) => MissionTestContext.Drain(client).ToList();

        #endregion
    }
}
