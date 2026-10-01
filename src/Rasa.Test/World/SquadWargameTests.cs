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
    using Rasa.Packets.Wargame.Server;
    using Rasa.Structures;
    using Rasa.Structures.Char;
    using Rasa.Test.Missions;

    // Squad Wargames (SquadWargames): a squad leader's challenge to another, its answer, and the
    // wargame - the two squads enemies (Pvp) until a side has made the kills, a leader surrenders,
    // a side is left empty or the time is up.
    [TestClass]
    [DoNotParallelize]
    public class SquadWargameTests
    {
        private const uint FirstAccountId = 930_000;

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
        public void AnAcceptedChallengeMakesTheTwoSquadsEnemies()
        {
            using var world = new WorldTestContext();
            var (redLead, redMate, blueLead, blueMate) = Squads(world);

            Duels.Instance.ChallengeUserToWargameByName(redLead, "BlueLead", 0, 0);

            Assert.AreEqual(0, Duels.Instance.Pending.Count, "a squad wargame, not a duel");
            var challenge = SquadWargames.Instance.Pending.Single();
            Assert.AreEqual(SquadWargames.DefaultMaxKills, challenge.MaxKills);

            var made = Drain(redMate).OfType<ChallengingToWargameSquadPacket>().Single();
            Assert.AreEqual("BlueLead", made.TargetName);
            CollectionAssert.AreEquivalent(new[] { Account(blueLead), Account(blueMate) }, made.TargetSquadInfo.Select(m => m.UserId).ToList());
            Assert.IsTrue(Messages(Drain(redLead)).Contains(PlayerMessage.PmWargameSquadChallenged));

            var received = Drain(blueMate);
            Assert.AreEqual("RedLead", received.OfType<ChallengedToWargameSquadPacket>().Single().AgressorName);
            Assert.AreEqual(2, received.OfType<ChallengedToWargameSquadPacket>().Single().AgressorSquadInfo.Count);
            Assert.IsTrue(Messages(received).Contains(PlayerMessage.PmWargameSquadChallengeReceived));
            Assert.IsFalse(Pvp.AreEnemies(redMate.Player, blueMate.Player), "not until it is accepted");

            Assert.IsTrue(SquadWargames.Instance.WargameChallengeResponse(blueMate, true));
            Assert.AreEqual(1, SquadWargames.Instance.Pending.Count, "only the squad leader answers");

            Assert.IsTrue(SquadWargames.Instance.WargameChallengeResponse(blueLead, true));

            var war = SquadWargames.Instance.Running.Single();
            Assert.AreEqual(challenge.WargameId, war.WargameId);
            Assert.IsTrue(Pvp.AreEnemies(redMate.Player, blueMate.Player));
            Assert.IsTrue(Pvp.AreEnemies(redLead.Player, blueMate.Player));
            Assert.IsFalse(Pvp.AreEnemies(redLead.Player, redMate.Player), "squadmates");

            var started = Drain(blueMate);
            CollectionAssert.AreEquivalent(new[] { Account(redLead), Account(redMate) }, started.OfType<WargameStartedPacket>().Single().EnemyUserIds.ToList());
            Assert.AreEqual(SquadWargames.DefaultMaxKills, started.OfType<SetWargameMaxKillsPacket>().Single().MaxKills);
            Assert.AreEqual(Duels.DefaultMinutes * 60_000, started.OfType<DisplayWargameTimerPacket>().Single().TimeMs, 1000);
            Assert.IsTrue(started.OfType<WargameDataPacket>().Any(p => p.Wargames.TryGetValue(war.WargameId, out var side) && !side));
            Assert.IsTrue(Messages(Drain(redMate)).Contains(PlayerMessage.PmWargameYourChallengeAccepted));
        }

        [TestMethod]
        public void KillsCountForTheSquadAndItsLosersAreDefeatedNotKilled()
        {
            using var world = new WorldTestContext();
            var (redLead, redMate, blueLead, blueMate) = Squads(world);
            Start(redLead, blueLead, maxKills: 2);

            blueLead.Player.Attributes[Attributes.Health].Current = 40;
            Shoot(world, redMate, blueLead, 100);

            Assert.AreNotEqual(CharacterState.Dead, blueLead.Player.State, "defeated, not dead");
            Assert.AreEqual(1000, (int)blueLead.Player.Attributes[Attributes.Health].Current);
            Assert.AreEqual(1, SquadWargames.Instance.Running.Single().Challenger.Kills);

            var score = Drain(blueMate).OfType<WargameScoreboardPacket>().Single();
            Assert.AreEqual(0, score.YourKills);
            Assert.AreEqual(1, score.TheirKills);
            Assert.AreEqual(Account(blueLead), score.VictimUserId);
            Assert.AreEqual(Account(redMate), score.KillerUserId);
            Assert.AreEqual(1, Drain(redLead).OfType<WargameScoreboardPacket>().Single().YourKills, "the squad's kill");

            blueMate.Player.Attributes[Attributes.Health].Current = 40;
            Shoot(world, redLead, blueMate, 100);

            Assert.AreEqual(0, SquadWargames.Instance.Running.Count);
            Assert.IsFalse(Pvp.AreEnemies(redMate.Player, blueMate.Player));

            foreach (var winner in new[] { redLead, redMate })
                Assert.AreEqual(GameOpcode.WargameVictory, Drain(winner).OfType<WargameResultPacket>().Single().Opcode);

            foreach (var loser in new[] { blueLead, blueMate })
                Assert.AreEqual(GameOpcode.WargameDefeat, Drain(loser).OfType<WargameResultPacket>().Single().Opcode);
        }

        [TestMethod]
        public void OnlyTheSquadLeaderSurrenders()
        {
            using var world = new WorldTestContext();
            var (redLead, redMate, blueLead, blueMate) = Squads(world);
            Start(redLead, blueLead);

            Assert.IsTrue(SquadWargames.Instance.SurrenderWargame(blueMate));
            Assert.IsTrue(Messages(Drain(blueMate)).Contains(PlayerMessage.PmWargameNotAbleToSurrender));
            Assert.AreEqual(1, SquadWargames.Instance.Running.Count);

            Assert.IsTrue(SquadWargames.Instance.SurrenderWargame(blueLead));

            Assert.AreEqual(0, SquadWargames.Instance.Running.Count);
            Assert.AreEqual(GameOpcode.WargameVictory, Drain(redMate).OfType<WargameResultPacket>().Single().Opcode);
            Assert.AreEqual(GameOpcode.WargameDefeat, Drain(blueMate).OfType<WargameResultPacket>().Single().Opcode);
            Assert.IsFalse(SquadWargames.Instance.SurrenderWargame(blueLead), "nothing left to surrender: a duel's /surrender");
        }

        [TestMethod]
        public void AMemberWhoLeavesIsOutAndASquadLeftEmptyLoses()
        {
            using var world = new WorldTestContext();
            var (redLead, redMate, blueLead, blueMate) = Squads(world);
            Start(redLead, blueLead);

            SquadWargames.Instance.PlayerLeft(redMate);

            var war = SquadWargames.Instance.Running.Single();
            Assert.IsFalse(war.Involves(redMate));
            Assert.IsFalse(Pvp.AreEnemies(redMate.Player, blueMate.Player));
            Assert.AreEqual(war.WargameId, Drain(redMate).OfType<RemoveFromWargamePacket>().Single().WargameId);
            Assert.IsTrue(Messages(Drain(blueMate)).Contains(PlayerMessage.PmWargamePlayerLeft));

            // The blue squad falls apart: one leaves, and a squad of one is disbanded.
            PartyManager.Instance.LeaveParty(blueMate);

            Assert.AreEqual(0, SquadWargames.Instance.Running.Count);
            Assert.AreEqual(GameOpcode.WargameVictory, Drain(redLead).OfType<WargameResultPacket>().Single().Opcode);
            Assert.IsFalse(Pvp.AreEnemies(redLead.Player, blueLead.Player));
        }

        [TestMethod]
        public void ADeclinedOrRevokedChallengeIsOffForBothSquads()
        {
            using var world = new WorldTestContext();
            var (redLead, redMate, blueLead, blueMate) = Squads(world);

            Duels.Instance.ChallengeUserToWargameByName(redLead, "BlueLead", 5, 3);
            DrainAll(redLead, redMate, blueLead, blueMate);

            Assert.IsTrue(SquadWargames.Instance.WargameChallengeResponse(blueLead, false));
            Assert.AreEqual(0, SquadWargames.Instance.Pending.Count);
            Assert.IsTrue(Drain(redMate).OfType<WargameChallengeRefusedPacket>().Single() is { YourPartyRefused: false });
            Assert.IsTrue(Drain(blueMate).OfType<WargameChallengeRefusedPacket>().Single() is { YourPartyRefused: true });

            Duels.Instance.ChallengeUserToWargameByName(redLead, "BlueLead", 5, 3);
            DrainAll(redLead, redMate, blueLead, blueMate);

            Assert.IsTrue(SquadWargames.Instance.WargameChallengeRevoked(redMate), "a member's revoke is the squad's to ignore");
            Assert.AreEqual(1, SquadWargames.Instance.Pending.Count);

            Assert.IsTrue(SquadWargames.Instance.WargameChallengeRevoked(redLead));
            Assert.AreEqual(0, SquadWargames.Instance.Pending.Count);
            Assert.AreEqual(1, Drain(blueMate).OfType<RevokeWargameChallengePacket>().Count());
            Assert.IsTrue(Messages(Drain(redLead)).Contains(PlayerMessage.PmWargameChallengeRevoked));
        }

        [TestMethod]
        public void TheClientsSquadRefusalsAreSaid()
        {
            using var world = new WorldTestContext();
            var (redLead, redMate, blueLead, blueMate) = Squads(world);
            var (greenLead, _) = Squad(world, "Green", 40);

            Assert.AreEqual(PlayerMessage.PmWargameYouNotSquadLeader, Refused(redMate, "BlueLead"));
            Assert.AreEqual(PlayerMessage.PmWargameTargetNotSquadLeader, Refused(redLead, "BlueMate"));
            Assert.AreEqual(PlayerMessage.PmWargameFailInSameParty, Refused(redLead, "RedMate"));

            Duels.Instance.ChallengeUserToWargameByName(redLead, "BlueLead", 0, 0);
            DrainAll(redLead, blueLead);

            Assert.AreEqual(PlayerMessage.PmWargameFailWaitForResponse, Refused(redLead, "GreenLead"));
            Assert.AreEqual(PlayerMessage.PmWargameAcceptOrDeclineFirst, Refused(blueLead, "GreenLead"));
            Assert.AreEqual(PlayerMessage.PmWargameAlreadyChallenged, Refused(greenLead, "BlueLead"));
            Assert.AreEqual(PlayerMessage.PmWargameAlreadyChallenging, Refused(greenLead, "RedLead"));

            SquadWargames.Instance.WargameChallengeResponse(blueLead, true);
            DrainAll(redLead, blueLead);

            Assert.AreEqual(PlayerMessage.PmWargameYouAlreadyWargaming, Refused(redLead, "GreenLead"));
            Assert.AreEqual(PlayerMessage.PmWargameTargetAlreadyWargaming, Refused(greenLead, "BlueLead"));
            Assert.IsTrue(Wargames.IsWargaming(blueMate), "keeps them out of other squads (PartyManager)");
        }

        private (Client RedLead, Client RedMate, Client BlueLead, Client BlueMate) Squads(WorldTestContext world)
        {
            var (redLead, redMate) = Squad(world, "Red", 0);
            var (blueLead, blueMate) = Squad(world, "Blue", 20);
            return (redLead, redMate, blueLead, blueMate);
        }

        /// <summary>A squad of two - a leader and a mate - in the world and on the map, found by name.</summary>
        private (Client Lead, Client Mate) Squad(WorldTestContext world, string name, float x)
        {
            var lead = Named(world, name + "Lead", x);
            var mate = Named(world, name + "Mate", x + 3);

            var id = PartyManager.Instance.GetPartyId;
            PartyManager.Instance.Parties[id] = new Party(id, Account(lead), new List<PartyMember> { new PartyMember(lead), new PartyMember(mate) });
            lead.Player.PartyId = id;
            mate.Player.PartyId = id;
            _parties.Add(id);

            foreach (var client in new[] { lead, mate })
            {
                CellManager.Instance.AddToWorld(client);
                Drain(client);
            }

            Duels.Instance.FindByName = n => _clients.FirstOrDefault(c => string.Equals(c.Player.FamilyName, n, StringComparison.OrdinalIgnoreCase));
            return (lead, mate);
        }

        private Client Named(WorldTestContext world, string name, float x)
        {
            var client = world.CreateClient(x, 0);
            client.Player.FamilyName = name;
            client.Player.Attributes[Attributes.Health] = new ActorAttributes(Attributes.Health, 1000, 1000, 1000, 0, 0);
            client.Player.Attributes[Attributes.Armor] = new ActorAttributes(Attributes.Armor, 0, 0, 0, 0, 0);

            var accountId = FirstAccountId + (uint)_clients.Count;
            typeof(Client).GetProperty(nameof(Client.AccountEntry))!.SetValue(client,
                new GameAccountEntry { Id = accountId, SelectedSlot = 0, Characters = new List<CharacterEntry>() });

            _clients.Add(client);

            lock (Server.Clients)
                Server.Clients.Add(client);

            return client;
        }

        private static uint Account(Client client) => client.AccountEntry.Id;

        private static void Start(Client challenger, Client target, int minutes = 0, int maxKills = 0)
        {
            Duels.Instance.ChallengeUserToWargameByName(challenger, target.Player.FamilyName, minutes, maxKills);
            Assert.IsTrue(SquadWargames.Instance.WargameChallengeResponse(target, true));
            Assert.AreEqual(1, SquadWargames.Instance.Running.Count);

            foreach (var member in SquadWargames.Instance.Running.Single().Everyone)
                Drain(member);
        }

        private static PlayerMessage Refused(Client client, string name)
        {
            Drain(client);
            var before = SquadWargames.Instance.Pending.Count + Duels.Instance.Pending.Count;

            Duels.Instance.ChallengeUserToWargameByName(client, name, 0, 0);

            Assert.AreEqual(before, SquadWargames.Instance.Pending.Count + Duels.Instance.Pending.Count, $"{name} was challenged");
            return Messages(Drain(client)).Single();
        }

        private static void Shoot(WorldTestContext world, Client shooter, Client target, int damage)
        {
            foreach (var effect in target.Player.ActiveEffects.Values.Where(e => e.TypeId == Pvp.SafetyTypeId).ToList())
                GameEffectManager.Instance.DettachEffect(world.Map, target.Player, effect);

            MissileManager.Instance.MissileTrigger(world.Map, new Missile
            {
                Source = shooter.Player,
                TargetActor = target.Player,
                TargetEntityId = target.Player.EntityId,
                DamageA = damage,
                DamageType = DamageType.Physical,
                ActionId = ActionId.WeaponAttack,
                ActionArgId = 1,
                CritChance = 0
            });
        }

        private static void DrainAll(params Client[] clients)
        {
            foreach (var client in clients)
                Drain(client);
        }

        private static List<PythonPacket> Drain(Client client) => MissionTestContext.Drain(client).ToList();

        private static List<PlayerMessage> Messages(IEnumerable<PythonPacket> packets) =>
            packets.OfType<DisplayWargameMessagePacket>().Select(p => p.Message).ToList();
    }
}
