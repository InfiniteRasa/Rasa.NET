using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.Gameplay
{
    using Rasa.Data;
    using Rasa.Game;
    using Rasa.Managers;
    using Rasa.Missions;
    using Rasa.Packets.Manifestation.Server;
    using Rasa.Packets.MapChannel.Server;
    using Rasa.Structures;
    using Rasa.Test.Missions;

    /// <summary>
    /// A kill's experience and adrenaline are split evenly between the player with the kill and
    /// every member of their squad within 200 m of the corpse: the experience between those
    /// below the level cap, the adrenaline between those alive.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class SquadKillShareTests
    {
        private Func<Client, Vector3, List<Client>> _sharers;

        [TestInitialize]
        public void KeepTheSquad() => _sharers = KillShares.SharersOf;

        [TestCleanup]
        public void RestoreTheSquad() => KillShares.SharersOf = _sharers;

        [TestMethod]
        [DataRow(100u, 1, 100u)]
        [DataRow(100u, 2, 50u)]
        [DataRow(101u, 2, 50u)]
        [DataRow(100u, 3, 33u)]
        [DataRow(110u, 6, 18u)]
        [DataRow(2u, 3, 1u)]
        [DataRow(0u, 3, 0u)]
        [DataRow(100u, 0, 100u)]
        public void AShareIsTheKillDividedByThoseWhoShareIt(uint experience, int sharers, uint share)
        {
            Assert.AreEqual(share, KillShares.ShareOf(experience, sharers));
        }

        [TestMethod]
        public void APlayerInNoSquadIsPaidTheWholeKill()
        {
            using var context = Start();
            var bystander = context.CreateAdditionalClient(2);
            var creature = AddCreature(context);

            try
            {
                Creatures(context).HandleCreatureKill(context.Map, creature, context.Client.Player);

                var gained = Gains(context.Client).Single();
                Assert.IsTrue(gained.Gained >= 90 && gained.Gained <= 110, $"a level 1 kill is 100 give or take 10, not {gained.Gained}");
                Assert.AreEqual(gained.Gained, gained.BaseGained);
                Assert.AreEqual(1, gained.GroupMod, "no group bonus");
                Assert.AreEqual(gained.Gained, context.Client.Player.Experience);
                Assert.AreEqual(gained.Gained, Saved(context, context.Client));

                Assert.IsEmpty(Gains(bystander), "someone standing by is no part of it");
                Assert.AreEqual(0u, bystander.Player.Experience);
            }
            finally
            {
                CellManager.Instance.RemoveCreatureFromWorld(context.Map, creature);
            }
        }

        [TestMethod]
        public void TheSquadWithinTwoHundredMetresOfTheCorpseSplitsTheKillEvenly()
        {
            using var context = Start();
            var near = context.CreateAdditionalClient(2);
            var edge = context.CreateAdditionalClient(3);
            var far = context.CreateAdditionalClient(4);
            var stranger = context.CreateAdditionalClient(5);
            var creature = AddCreature(context);

            // The corpse is where the killer stands. Measured from it, not from the killer.
            near.Player.Position = creature.Position + new Vector3(150, 0, 0);
            edge.Player.Position = creature.Position + new Vector3(0, 0, PartyManager.LootShareRange);
            far.Player.Position = creature.Position + new Vector3(PartyManager.LootShareRange + 1, 0, 0);
            stranger.Player.Position = creature.Position + new Vector3(2, 0, 0);

            using var squad = new Squad(context.Client, near, edge, far);

            try
            {
                Creatures(context).HandleCreatureKill(context.Map, creature, context.Client.Player);

                var killers = Gains(context.Client).Single();
                Assert.IsTrue(killers.Gained >= 30 && killers.Gained <= 36, $"a third of 90 to 110, not {killers.Gained}");

                foreach (var sharer in new[] { context.Client, near, edge })
                {
                    Assert.AreEqual(killers.Gained, sharer.Player.Experience, $"character {sharer.Player.Id}");
                    Assert.AreEqual(killers.Gained, Saved(context, sharer), "and saved");
                }

                foreach (var sharer in new[] { near, edge })
                {
                    var gained = Gains(sharer).Single();
                    Assert.AreEqual(killers.Gained, gained.Gained);
                    Assert.AreEqual(killers.Gained, gained.BaseGained);
                    Assert.AreEqual(killers.Gained, gained.Total);
                    Assert.AreEqual(1, gained.GroupMod, "no group bonus");
                    Assert.IsFalse(gained.WasCritKill || gained.WasTeamCritKill);
                }

                Assert.IsEmpty(Gains(far), "past 200 m of the corpse");
                Assert.AreEqual(0u, far.Player.Experience);
                Assert.IsEmpty(Gains(stranger), "near, and in no squad of the killer's");
                Assert.AreEqual(0u, stranger.Player.Experience);
            }
            finally
            {
                CellManager.Instance.RemoveCreatureFromWorld(context.Map, creature);
            }
        }

        [TestMethod]
        public void AFinishingMovePaysTheSquadTwiceAndTellsTheOthersTheirTeamDidIt()
        {
            using var context = Start();
            var mate = context.CreateAdditionalClient(2);
            var creature = AddCreature(context);
            Vector3 asked = default;

            KillShares.SharersOf = (killer, corpse) =>
            {
                asked = corpse;
                return new List<Client> { killer, mate };
            };
            creature.Position = new Vector3(7, 0, 3);

            try
            {
                Creatures(context).HandleCreatureKill(context.Map, creature, context.Client.Player, CritKill.Own);

                Assert.AreEqual(creature.Position, asked, "measured from the corpse");

                var killers = Gains(context.Client);
                var mates = Gains(mate);
                Assert.HasCount(2, killers);
                Assert.HasCount(2, mates);

                var share = killers[0].Gained;
                Assert.IsTrue(share >= 45 && share <= 55, $"half of 90 to 110, not {share}");
                Assert.IsTrue(killers.Concat(mates).All(gain => gain.Gained == share), "the kill and the finish are each split the same");
                Assert.AreEqual(share * 2, context.Client.Player.Experience);
                Assert.AreEqual(share * 2, mate.Player.Experience);

                Assert.IsFalse(killers[0].WasCritKill || killers[0].WasTeamCritKill);
                Assert.IsTrue(killers[1].WasCritKill, "by Crit Killing");
                Assert.IsFalse(killers[1].WasTeamCritKill);

                Assert.IsFalse(mates[0].WasCritKill || mates[0].WasTeamCritKill);
                Assert.IsTrue(mates[1].WasTeamCritKill, "by Team Crit Killing");
                Assert.IsFalse(mates[1].WasCritKill);
            }
            finally
            {
                CellManager.Instance.RemoveCreatureFromWorld(context.Map, creature);
            }
        }

        [TestMethod]
        public void AnAwardIsPaidInEqualPartsAndWhatDoesNotDivideIsLost()
        {
            using var context = Start();
            var second = context.CreateAdditionalClient(2);
            var third = context.CreateAdditionalClient(3);
            var manifestations = new ManifestationManager(context);
            var sharers = new[] { context.Client, second, third };

            Assert.AreEqual(33u, KillShares.AwardExperience(manifestations, sharers, context.Client, 100));

            foreach (var sharer in sharers)
            {
                Assert.AreEqual(33u, sharer.Player.Experience);
                Assert.AreEqual(33u, Saved(context, sharer));
                Assert.AreEqual(33u, Gains(sharer).Single().Gained);
            }

            // A finish somebody else opened is the team's to all of them.
            Assert.AreEqual(50u, KillShares.AwardExperience(manifestations, new[] { context.Client, second }, context.Client, 100, CritKill.Team));
            Assert.IsTrue(Gains(context.Client).Single().WasTeamCritKill);
            Assert.IsTrue(Gains(second).Single().WasTeamCritKill);
            Assert.IsEmpty(Gains(third));

            // Nothing to pay, nobody to pay.
            Assert.AreEqual(0u, KillShares.AwardExperience(manifestations, sharers, context.Client, 0));
            Assert.AreEqual(0u, KillShares.AwardExperience(manifestations, Array.Empty<Client>(), context.Client, 100));
            Assert.IsEmpty(Gains(context.Client));
        }

        [TestMethod]
        public void AMemberAtTheLevelCapTakesNoShareOfTheExperience()
        {
            using var context = Start();
            var veteran = context.CreateAdditionalClient(2);
            var mate = context.CreateAdditionalClient(3);
            var manifestations = new ManifestationManager(context);
            veteran.Player.Level = ManifestationManager.MaxPlayerLevel;

            // With the one at the cap alone: the whole kill.
            Assert.AreEqual(100u, KillShares.AwardExperience(manifestations, new[] { context.Client, veteran }, context.Client, 100));
            Assert.AreEqual(100u, context.Client.Player.Experience);
            Assert.AreEqual(100u, Gains(context.Client).Single().Gained);

            // With another who can gain: halves, not thirds.
            Assert.AreEqual(50u, KillShares.AwardExperience(manifestations, new[] { context.Client, veteran, mate }, context.Client, 100));
            Assert.AreEqual(150u, context.Client.Player.Experience);
            Assert.AreEqual(50u, mate.Player.Experience);

            Assert.AreEqual(0u, veteran.Player.Experience);
            Assert.IsEmpty(Gains(veteran));
        }

        [TestMethod]
        public void AKillerAtTheLevelCapLeavesTheWholeKillToTheirSquad()
        {
            using var context = Start();
            var mate = context.CreateAdditionalClient(2);
            var manifestations = new ManifestationManager(context);
            context.Client.Player.Level = ManifestationManager.MaxPlayerLevel;

            Assert.AreEqual(100u, KillShares.AwardExperience(manifestations, new[] { context.Client, mate }, context.Client, 100, CritKill.Own));

            Assert.AreEqual(100u, mate.Player.Experience);
            Assert.IsTrue(Gains(mate).Single().WasTeamCritKill, "the mate's team finished it");
            Assert.AreEqual(0u, context.Client.Player.Experience);
            Assert.IsEmpty(Gains(context.Client));

            // Nobody below the cap: nobody is paid.
            mate.Player.Level = ManifestationManager.MaxPlayerLevel;
            Assert.AreEqual(0u, KillShares.AwardExperience(manifestations, new[] { context.Client, mate }, context.Client, 100));
            Assert.AreEqual(100u, mate.Player.Experience);
            Assert.IsEmpty(Gains(mate));
        }

        [TestMethod]
        public void AKillsAdrenalineIsSplitEvenlyBetweenTheSquadAlive()
        {
            using var context = Start();
            var mate = context.CreateAdditionalClient(2);
            var veteran = context.CreateAdditionalClient(3);
            var fallen = context.CreateAdditionalClient(4);
            var manifestations = new ManifestationManager(context);
            var all = new[] { context.Client, mate, veteran, fallen };

            foreach (var client in all)
                Bar(client, 100);

            // A bar of 100 is worth 20 a kill. Level is nothing to adrenaline; being dead is.
            veteran.Player.Level = ManifestationManager.MaxPlayerLevel;
            fallen.Player.State = CharacterState.Dead;

            // Alone: all of it.
            KillShares.AwardAdrenaline(manifestations, new[] { context.Client }, false);
            Assert.AreEqual(20, Chi(context.Client));
            Assert.AreEqual(1, Told(context.Client), "and told of it");

            // Three alive of four: a third of 20 each, rounded down, and the dead one none.
            KillShares.AwardAdrenaline(manifestations, all, false);
            Assert.AreEqual(26, Chi(context.Client));
            Assert.AreEqual(6, Chi(mate));
            Assert.AreEqual(6, Chi(veteran), "at the level cap and still in it");
            Assert.AreEqual(0, Chi(fallen));
            Assert.AreEqual(1, Told(mate));
            Assert.AreEqual(0, Told(fallen));

            // A finishing move is worth twice, in one award.
            KillShares.AwardAdrenaline(manifestations, new[] { context.Client, mate }, true);
            Assert.AreEqual(46, Chi(context.Client));
            Assert.AreEqual(26, Chi(mate));
            Assert.AreEqual(1, Told(mate), "one number over the bar, not two");

            // Each by their own bar: a bigger bar is a bigger share.
            Bar(mate, 300);
            KillShares.AwardAdrenaline(manifestations, new[] { context.Client, mate }, false);
            Assert.AreEqual(56, Chi(context.Client));
            Assert.AreEqual(30, Chi(mate));
        }

        [TestMethod]
        public void AKillGivesTheSquadInRangeItsAdrenalineAndTheRestNone()
        {
            using var context = Start();
            var mate = context.CreateAdditionalClient(2);
            var far = context.CreateAdditionalClient(3);
            var creature = AddCreature(context);

            foreach (var client in new[] { context.Client, mate, far })
                Bar(client, 100);

            far.Player.Position = creature.Position + new Vector3(PartyManager.LootShareRange + 1, 0, 0);

            using var squad = new Squad(context.Client, mate, far);

            try
            {
                Creatures(context).HandleCreatureKill(context.Map, creature, context.Client.Player);

                Assert.AreEqual(10, Chi(context.Client));
                Assert.AreEqual(10, Chi(mate));
                Assert.AreEqual(0, Chi(far), "past 200 m of the corpse");
            }
            finally
            {
                CellManager.Instance.RemoveCreatureFromWorld(context.Map, creature);
            }
        }

        [TestMethod]
        public void TheKillerIsAlwaysOneOfThoseWhoShareAndEachIsThereOnce()
        {
            using var context = Start();
            var mate = context.CreateAdditionalClient(2);
            var killer = context.Client;

            KillShares.SharersOf = (_, _) => new List<Client> { mate, null, killer, mate };
            CollectionAssert.AreEqual(new[] { killer, mate }, KillShares.SharersFor(killer, Vector3.Zero));

            KillShares.SharersOf = (_, _) => null;
            CollectionAssert.AreEqual(new[] { killer }, KillShares.SharersFor(killer, Vector3.Zero));

            // A squad that cannot be read costs the killer nothing.
            KillShares.SharersOf = (_, _) => throw new InvalidOperationException("no squads");
            CollectionAssert.AreEqual(new[] { killer }, KillShares.SharersFor(killer, Vector3.Zero));

            Assert.IsEmpty(KillShares.SharersFor(null, Vector3.Zero));
        }

        private static MissionTestContext Start() =>
            MissionTestContext.WithProgressMission(
                MissionProgressRule.CompleteOnExactSubject(MissionProgressEventKind.CreatureKilled, 82));

        private static CreatureManager Creatures(MissionTestContext context) =>
            new CreatureManager(context, new ManifestationManager(context), context.Manager);

        /// <summary>The experience lines the client was sent since last asked.</summary>
        private static List<XPInfo> Gains(Client client) =>
            MissionTestContext.Drain(client).OfType<ExperienceChangedPacket>().Select(packet => packet.XPInfo).ToList();

        /// <summary>Gives the player an empty adrenaline bar of this size.</summary>
        private static void Bar(Client client, int size) =>
            client.Player.Attributes[Attributes.Chi] = new ActorAttributes(Attributes.Chi, size, size, 0, 0, 0);

        private static int Chi(Client client) => client.Player.Attributes[Attributes.Chi].Current;

        /// <summary>How many times the client was told of its adrenaline since last asked.</summary>
        private static int Told(Client client) =>
            MissionTestContext.Drain(client).OfType<UpdateChiPacket>().Count();

        private static uint Saved(MissionTestContext context, Client client)
        {
            using var unit = context.CreateChar();

            return unit.Characters.Find(client.Player.Id).Experience;
        }

        /// <summary>A level 1 creature standing where the killer does, which no mission counts.</summary>
        private static Creature AddCreature(MissionTestContext context)
        {
            var creature = new Creature
            {
                DbId = 9001,
                EntityClass = EntityClasses.HumanBaseMale,
                MapContextId = context.Map.MapInfo.MapContextId,
                Position = context.Client.Player.Position,
                Level = 1,
                State = CharacterState.Idle,
                AppearanceData = new Dictionary<EquipmentData, AppearanceData>(),
                Attributes = Enum.GetValues<Attributes>().ToDictionary(
                    id => id, id => new ActorAttributes(id, 100, 100, 0, 0, 0)),
                SpawnPool = new SpawnPool
                {
                    MapContextId = context.Map.MapInfo.MapContextId,
                    AliveCreatures = 1,
                    RespawnTime = 1000,
                    UpdateTimer = 1000
                }
            };

            CellManager.Instance.AddToWorld(context.Map, creature);
            return creature;
        }

        /// <summary>A real squad of these players, the first its leader, found the way the server finds its members.</summary>
        private sealed class Squad : IDisposable
        {
            private readonly uint _id;
            private readonly Client[] _clients;

            internal Squad(params Client[] clients)
            {
                _clients = clients;
                _id = PartyManager.Instance.GetPartyId;
                PartyManager.Instance.Parties[_id] = new Party(_id, clients[0].AccountEntry.Id,
                    clients.Select(client => new PartyMember(client)).ToList());

                foreach (var client in clients)
                    client.Player.PartyId = _id;

                lock (Server.Clients)
                    Server.Clients.AddRange(clients);
            }

            public void Dispose()
            {
                lock (Server.Clients)
                    foreach (var client in _clients)
                        Server.Clients.Remove(client);

                foreach (var client in _clients)
                    client.Player.PartyId = 0;

                PartyManager.Instance.Parties.Remove(_id);
                PartyManager.Instance.FreePartyId(_id);
            }
        }
    }
}
