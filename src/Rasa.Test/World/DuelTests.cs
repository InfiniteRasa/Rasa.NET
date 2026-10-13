using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Rasa.Data;
    using Rasa.Game;
    using Rasa.Managers;
    using Rasa.Memory;
    using Rasa.Packets;
    using Rasa.Packets.MapChannel.Server;
    using Rasa.Packets.Wargame.Client;
    using Rasa.Packets.Wargame.Server;
    using Rasa.Structures;
    using Rasa.Structures.Char;
    using Rasa.Test.Missions;

    // Duel Wargames (Duels): a challenge, its answer, the duel - the two enemies (Pvp) until one
    // has made the kills, surrenders, leaves or the time is up - and the client's refusals.
    [TestClass]
    [DoNotParallelize]
    public class DuelTests
    {
        [TestMethod]
        public void AnAcceptedChallengeStartsADuelBetweenEnemies()
        {
            using var world = new WorldTestContext();
            var (red, blue) = Duelists(world);

            Duels.Instance.ChallengeUserToWargameByName(red, "blue", 0, 0);

            var challenge = Duels.Instance.Pending.Single();
            Assert.AreEqual(challenge.WargameId, Drain(red).OfType<ChallengingToWargameDuelPacket>().Single().WargameId);
            var invite = Drain(blue).OfType<ChallengedToWargameDuelPacket>().Single();
            Assert.AreEqual("Red", invite.AgressorName);
            Assert.IsFalse(Pvp.AreEnemies(red.Player, blue.Player), "not until it is accepted");

            Duels.Instance.WargameChallengeResponse(blue, true);

            Assert.AreEqual(0, Duels.Instance.Pending.Count);
            var duel = Duels.Instance.Running.Single();
            Assert.AreEqual(1, duel.MaxKills, "one kill unless more were asked for");
            Assert.IsTrue(Pvp.AreEnemies(red.Player, blue.Player));

            var packets = Drain(blue);
            CollectionAssert.AreEqual(new List<uint> { red.Player.Id }, packets.OfType<WargameStartedPacket>().Single().EnemyUserIds.ToList());
            Assert.AreEqual(1, packets.OfType<SetWargameMaxKillsPacket>().Single().MaxKills);
            Assert.AreEqual(Duels.DefaultMinutes * 60_000, packets.OfType<DisplayWargameTimerPacket>().Single().TimeMs, 1000);
            Assert.IsTrue(packets.OfType<WargameDataPacket>().Any(p => p.Wargames.TryGetValue(duel.WargameId, out var side) && !side));
            Assert.IsTrue(packets.OfType<TargetCategoryPacket>().Any(p => p.TargetCategory == TargetCategory.Hostile));
            Assert.IsTrue(Messages(packets).Contains(PlayerMessage.PmWargameYouAccepted));
            Assert.IsTrue(Messages(Drain(red)).Contains(PlayerMessage.PmWargameYourChallengeAccepted));
        }

        [TestMethod]
        public void ADefeatEndsTheDuelInVictoryAndDefeat()
        {
            using var world = new WorldTestContext();
            var (red, blue) = Duelists(world);
            StartDuel(red, blue);
            var duel = Duels.Instance.Running.Single();
            blue.Player.Attributes[Attributes.Health].Current = 40;

            Shoot(world, red, blue, 100);

            Assert.AreEqual(0, Duels.Instance.Running.Count);
            Assert.IsFalse(Pvp.AreEnemies(red.Player, blue.Player));
            Assert.AreEqual(1000, (int)blue.Player.Attributes[Attributes.Health].Current, "defeated, not dead");

            var redPackets = Drain(red);
            Assert.AreEqual(1, redPackets.OfType<WargameScoreboardPacket>().Single().YourKills);
            Assert.AreEqual(GameOpcode.WargameVictory, redPackets.OfType<WargameResultPacket>().Single().Opcode);

            var bluePackets = Drain(blue);
            Assert.AreEqual(1, bluePackets.OfType<WargameScoreboardPacket>().Single().TheirKills);
            Assert.AreEqual(GameOpcode.WargameDefeat, bluePackets.OfType<WargameResultPacket>().Single().Opcode);
            Assert.IsTrue(bluePackets.OfType<TargetCategoryPacket>().Last().TargetCategory == TargetCategory.Friendly);
            Assert.AreEqual(duel.WargameId, bluePackets.OfType<WargameResultPacket>().Single().WargameId);
        }

        [TestMethod]
        public void ADuelForTwoKillsGoesOnAfterTheFirst()
        {
            using var world = new WorldTestContext();
            var (red, blue) = Duelists(world);
            StartDuel(red, blue, maxKills: 2);

            blue.Player.Attributes[Attributes.Health].Current = 40;
            Shoot(world, red, blue, 100);
            Assert.AreEqual(1, Duels.Instance.Running.Single().ChallengerKills);

            // Back on their feet under PvP Safety: nothing lands until it is off.
            foreach (var effect in blue.Player.ActiveEffects.Values.Where(e => e.TypeId == Pvp.SafetyTypeId).ToList())
                GameEffectManager.Instance.DettachEffect(world.Map, blue.Player, effect);

            blue.Player.Attributes[Attributes.Health].Current = 40;
            Shoot(world, red, blue, 100);
            Assert.AreEqual(0, Duels.Instance.Running.Count);
        }

        [TestMethod]
        public void TheClientsRefusalsAreSaid()
        {
            using var world = new WorldTestContext();
            var (red, blue) = Duelists(world);
            var green = Named(world, "Green", 20);
            Find(red, blue, green);

            Assert.AreEqual(PlayerMessage.PmWargameCannotChallengeYourself, Refused(red, "red"));
            Assert.AreEqual(PlayerMessage.PmWargameNoTargetByName, Refused(red, "nobody"));

            blue.Player.PartyId = 7;
            red.Player.PartyId = 7;
            Assert.AreEqual(PlayerMessage.PmWargameFailInSameParty, Refused(red, "blue"));
            red.Player.PartyId = 0;
            Assert.AreEqual(PlayerMessage.PmWargameFailTargetInSquad, Refused(red, "blue"));
            red.Player.PartyId = 8;
            blue.Player.PartyId = 0;
            Assert.AreEqual(PlayerMessage.PmWargameTargetNotInSquad, Refused(red, "blue"));
            red.Player.PartyId = 0;

            var elsewhere = new MapChannel { MapInfo = new MapInfo(1148, "adv_foreas_concordia_divide", 1556, 0), ClientList = new List<Client>() };
            blue.Player.MapChannel = elsewhere;
            Assert.AreEqual(PlayerMessage.PmWargameChallengeNotOnSameMap, Refused(red, "blue"));
            blue.Player.MapChannel = world.Map;

            Duels.Instance.ChallengeUserToWargameByName(red, "blue", 0, 0);
            Drain(red);
            Assert.AreEqual(PlayerMessage.PmWargameFailWaitForResponse, Refused(red, "green"));
            Assert.AreEqual(PlayerMessage.PmWargameAcceptOrDeclineFirst, Refused(blue, "green"));
            Assert.AreEqual(PlayerMessage.PmWargameAlreadyChallenged, Refused(green, "blue"));
            Assert.AreEqual(PlayerMessage.PmWargameAlreadyChallenging, Refused(green, "red"));

            Duels.Instance.WargameChallengeResponse(blue, true);
            Drain(red);
            Assert.AreEqual(PlayerMessage.PmWargameYouAlreadyWargaming, Refused(red, "green"));
            Assert.AreEqual(PlayerMessage.PmWargameTargetAlreadyWargaming, Refused(green, "blue"));
            Assert.AreEqual(1, Duels.Instance.Running.Count);
        }

        [TestMethod]
        public void ADeclinedChallengeIsRefusedToBoth()
        {
            using var world = new WorldTestContext();
            var (red, blue) = Duelists(world);
            Duels.Instance.ChallengeUserToWargameByName(red, "blue", 0, 0);
            Drain(red);
            Drain(blue);

            Duels.Instance.WargameChallengeResponse(blue, false);

            Assert.AreEqual(0, Duels.Instance.Pending.Count + Duels.Instance.Running.Count);
            var redPackets = Drain(red);
            Assert.IsFalse(redPackets.OfType<WargameChallengeRefusedPacket>().Single().YourPartyRefused);
            Assert.IsTrue(Messages(redPackets).Contains(PlayerMessage.PmWargameRefused));
            var bluePackets = Drain(blue);
            Assert.IsTrue(bluePackets.OfType<WargameChallengeRefusedPacket>().Single().YourPartyRefused);
            Assert.IsTrue(Messages(bluePackets).Contains(PlayerMessage.PmWargameYouRefused));
        }

        [TestMethod]
        public void ARevokedOrLapsedChallengeIsTakenBack()
        {
            using var world = new WorldTestContext();
            var (red, blue) = Duelists(world);

            Duels.Instance.ChallengeUserToWargameByName(red, "blue", 0, 0);
            Drain(blue);
            Duels.Instance.WargameChallengeRevoked(red);

            Assert.AreEqual(0, Duels.Instance.Pending.Count);
            var bluePackets = Drain(blue);
            Assert.AreEqual(1, bluePackets.OfType<RevokeWargameChallengePacket>().Count());
            Assert.IsTrue(Messages(bluePackets).Contains(PlayerMessage.PmWargameRevoked));

            var now = 1_000_000L;
            Duels.Instance.Now = () => now;

            try
            {
                Duels.Instance.ChallengeUserToWargameByName(red, "blue", 0, 0);
                Drain(blue);
                now += (long)Duels.ChallengeTimeout.TotalMilliseconds;
                Duels.Instance.Worker();

                Assert.AreEqual(0, Duels.Instance.Pending.Count);
                bluePackets = Drain(blue);
                Assert.AreEqual(1, bluePackets.OfType<RevokeWargameChallengePacket>().Count());
                Assert.IsTrue(Messages(bluePackets).Contains(PlayerMessage.PmWargameChallengeTimedOut));
            }
            finally
            {
                Duels.Instance.Now = () => Environment.TickCount64;
            }
        }

        [TestMethod]
        public void SurrenderingGivesTheDuelToTheOtherSide()
        {
            using var world = new WorldTestContext();
            var (red, blue) = Duelists(world);

            Duels.Instance.SurrenderWargame(red);
            Assert.IsTrue(Messages(Drain(red)).Contains(PlayerMessage.PmWargameNotInSquadOrDuel));

            StartDuel(red, blue);
            Duels.Instance.SurrenderWargame(red);

            Assert.AreEqual(0, Duels.Instance.Running.Count);
            Assert.AreEqual(GameOpcode.WargameDefeat, Drain(red).OfType<WargameResultPacket>().Single().Opcode);
            Assert.AreEqual(GameOpcode.WargameVictory, Drain(blue).OfType<WargameResultPacket>().Single().Opcode);
        }

        [TestMethod]
        public void LeavingForfeitsTheDuel()
        {
            using var world = new WorldTestContext();
            var (red, blue) = Duelists(world);
            StartDuel(red, blue);

            Duels.Instance.PlayerLeft(red);

            Assert.AreEqual(0, Duels.Instance.Running.Count);
            Assert.AreEqual(1, Drain(red).OfType<RemoveFromWargamePacket>().Count());
            var bluePackets = Drain(blue);
            Assert.IsTrue(Messages(bluePackets).Contains(PlayerMessage.PmWargamePlayerLeft));
            Assert.AreEqual(GameOpcode.WargameVictory, bluePackets.OfType<WargameResultPacket>().Single().Opcode);
        }

        [TestMethod]
        public void WhenTheTimeIsUpMoreKillsWinAndEqualIsATie()
        {
            using var world = new WorldTestContext();
            var (red, blue) = Duelists(world);
            var now = 5_000_000L;
            Duels.Instance.Now = () => now;

            try
            {
                StartDuel(red, blue, minutes: 2);
                now += 2 * 60_000L;
                Duels.Instance.Worker();

                Assert.AreEqual(0, Duels.Instance.Running.Count);
                Assert.AreEqual(GameOpcode.WargameTied, Drain(red).OfType<WargameResultPacket>().Single().Opcode);
                Assert.AreEqual(GameOpcode.WargameTied, Drain(blue).OfType<WargameResultPacket>().Single().Opcode);
            }
            finally
            {
                Duels.Instance.Now = () => Environment.TickCount64;
            }
        }

        [TestMethod]
        public void DuelistsTooFarApartForTenSecondsTieWhateverTheKills()
        {
            using var world = new WorldTestContext();
            var (red, blue) = Duelists(world);
            var now = 5_000_000L;
            var store = new MemoryPvpStore();
            var records = new PvpRecords();
            var previous = Duels.Instance.Records;

            records.Load(store);
            Duels.Instance.Records = records;
            Duels.Instance.Now = () => now;

            try
            {
                StartDuel(red, blue, maxKills: 3);
                Assert.IsTrue(Duels.Instance.Kill(red, blue));
                Drain(red);
                Drain(blue);

                // 200 m is not too far.
                blue.Player.Position = red.Player.Position + new Vector3(Duels.MaxApartMetres, 0, 0);
                Duels.Instance.Worker();
                now += 60_000;
                Duels.Instance.Worker();
                Assert.AreEqual(1, Duels.Instance.Running.Count);

                blue.Player.Position = red.Player.Position + new Vector3(150, 0, 150);
                Duels.Instance.Worker();
                now += 9_999;
                Duels.Instance.Worker();
                Assert.AreEqual(1, Duels.Instance.Running.Count, "not yet ten seconds");

                now += 1;
                Duels.Instance.Worker();

                Assert.AreEqual(0, Duels.Instance.Running.Count);
                Assert.AreEqual(GameOpcode.WargameTied, Drain(red).OfType<WargameResultPacket>().Single().Opcode, "a tie, though Red was a kill up");
                Assert.AreEqual(GameOpcode.WargameTied, Drain(blue).OfType<WargameResultPacket>().Single().Opcode);
                Assert.IsFalse(Pvp.AreEnemies(red.Player, blue.Player));

                var match = store.Matches.Single();
                Assert.AreEqual("distance", match.Reason);
                Assert.AreEqual((byte)PvpMatchOutcome.Tied, match.Outcome);
                Assert.AreEqual((byte)0, match.WinnerSide);
                Assert.AreEqual((1, 0), (match.Side1Kills, match.Side2Kills));
            }
            finally
            {
                Duels.Instance.Records = previous;
                Duels.Instance.Now = () => Environment.TickCount64;
            }
        }

        [TestMethod]
        public void ComingBackWithinRangeStartsTheTenSecondsAgain()
        {
            using var world = new WorldTestContext();
            var (red, blue) = Duelists(world);
            var now = 5_000_000L;
            Duels.Instance.Now = () => now;

            try
            {
                StartDuel(red, blue);
                var far = red.Player.Position + new Vector3(0, 0, 250);
                var near = red.Player.Position + new Vector3(0, 0, 50);

                blue.Player.Position = far;
                Duels.Instance.Worker();
                now += 9_000;
                Duels.Instance.Worker();

                blue.Player.Position = near;
                now += 500;
                Duels.Instance.Worker();

                blue.Player.Position = far;
                now += 500;
                Duels.Instance.Worker();
                now += 9_000;
                Duels.Instance.Worker();
                Assert.AreEqual(1, Duels.Instance.Running.Count, "nine seconds since they parted again");

                now += 1_000;
                Duels.Instance.Worker();
                Assert.AreEqual(0, Duels.Instance.Running.Count);
                Assert.AreEqual(GameOpcode.WargameTied, Drain(blue).OfType<WargameResultPacket>().Single().Opcode);
            }
            finally
            {
                Duels.Instance.Now = () => Environment.TickCount64;
            }
        }

        [TestMethod]
        public void WhatADuelistHasWageredIsOnTheDuelsRecordAndStaysTheirs()
        {
            using var world = new WorldTestContext();
            var (red, blue) = Duelists(world);
            var store = new MemoryPvpStore();
            var records = new PvpRecords { WageredItems = ids => InventoryManager.Instance.WageredItemsOf(ids) };
            var previous = Duels.Instance.Records;

            // Blue has an item in the wager slot, as the world holds it.
            var item = new Item { Id = 4401, ItemTemplateId = 9001, StackSize = 1 };

            EntityManager.Instance.RegisterEntity(item.EntityId, EntityType.Item);
            EntityManager.Instance.RegisterItem(item.EntityId, item);
            blue.Player.Inventory.WagerItem = item.EntityId;

            records.Load(store);
            Duels.Instance.Records = records;

            try
            {
                StartDuel(red, blue);
                Assert.IsTrue(Duels.Instance.Kill(red, blue));
                Assert.AreEqual(0, Duels.Instance.Running.Count);

                var match = store.Matches.Single();
                var wager = store.Wagers[match.Id].Values.Single();

                Assert.AreEqual(blue.Player.Id, wager.CharacterId, "the one who had something wagered");
                Assert.AreEqual((byte)2, wager.Side);
                Assert.AreEqual((4401u, 9001u, 1u), (wager.ItemId, wager.ItemTemplateId, wager.StackSize));
                Assert.AreEqual((byte)PvpWagerResult.Kept, wager.Result, "beaten, and it is theirs still: a duel takes nothing");
                Assert.AreEqual((0u, 0u), (wager.RecipientClanId, wager.RecipientCharacterId));
                Assert.AreEqual(item.EntityId, blue.Player.Inventory.WagerItem);

                // Walked out of: the one who left is asked all the same.
                StartDuel(blue, red);
                Duels.Instance.PlayerLeft(blue);

                Assert.AreEqual(2, store.Matches.Count);
                Assert.AreEqual((byte)1, store.Wager(store.Matches[1].Id, blue.Player.Id).Side, "the challenger this time");
                Assert.IsNull(store.Wager(store.Matches[1].Id, red.Player.Id));
            }
            finally
            {
                Duels.Instance.Records = previous;
                blue.Player.Inventory.WagerItem = 0;
                EntityManager.Instance.UnregisterItem(item.EntityId);
                EntityManager.Instance.UnregisterEntity(item.EntityId);
                EntityManager.Instance.FreeEntity(item.EntityId);
            }
        }

        [TestMethod]
        public void ADuelThatEndsIsPutOnRecord()
        {
            using var world = new WorldTestContext();
            var (red, blue) = Duelists(world);
            var noon = new DateTime(2026, 10, 2, 12, 0, 0, DateTimeKind.Utc);
            var utc = noon;
            var now = 5_000_000L;
            var store = new MemoryPvpStore();
            var records = new PvpRecords { UtcNow = () => utc };
            var previous = Duels.Instance.Records;

            records.Load(store);
            Duels.Instance.Records = records;
            Duels.Instance.Now = () => now;
            red.Player.ClanId = 31;

            try
            {
                // Two kills to win it: nothing is written until it is over.
                StartDuel(red, blue, maxKills: 2);

                var wargameId = Duels.Instance.Running.Single().WargameId;

                Assert.IsTrue(Duels.Instance.Kill(red, blue));
                Assert.AreEqual(0, store.Matches.Count);

                Assert.IsTrue(Duels.Instance.Kill(blue, red));
                utc = noon.AddMinutes(3);
                Assert.IsTrue(Duels.Instance.Kill(red, blue));
                Assert.AreEqual(0, Duels.Instance.Running.Count);

                var match = store.Matches.Single();

                Assert.AreEqual((byte)PvpMatchKind.Duel, match.Kind);
                Assert.AreEqual(4, match.Kind);
                Assert.AreEqual(wargameId, match.WargameId);
                Assert.AreEqual(world.Map.MapInfo.MapContextId, match.MapContextId);
                Assert.AreEqual(noon, match.StartedAt);
                Assert.AreEqual(noon.AddMinutes(3), match.EndedAt);
                Assert.AreEqual((byte)PvpMatchOutcome.Won, match.Outcome);
                Assert.AreEqual((byte)1, match.WinnerSide, "the challenger is side 1");
                Assert.AreEqual("kills", match.Reason);
                Assert.AreEqual("Red", match.Side1Name);
                Assert.AreEqual("Blue", match.Side2Name);
                Assert.AreEqual((2, 1), (match.Side1Score, match.Side2Score));
                Assert.AreEqual((2, 1), (match.Side1Kills, match.Side2Kills));
                Assert.AreEqual((0u, 0u), (match.Side1ClanId, match.Side2ClanId), "a duel is nobody's clan's");

                Assert.AreEqual(2, store.Rows[match.Id].Count);

                var winner = store.Row(match.Id, red.Player.Id);

                Assert.AreEqual((byte)1, winner.Side);
                Assert.AreEqual("Red", winner.FamilyName);
                Assert.AreEqual(red.Player.Name, winner.Name);
                Assert.AreEqual(31u, winner.ClanId);
                Assert.AreEqual((2, 1), (winner.Kills, winner.Deaths));
                Assert.IsTrue(winner.PresentAtEnd);

                var loser = store.Row(match.Id, blue.Player.Id);

                Assert.AreEqual((byte)2, loser.Side);
                Assert.AreEqual((1, 2), (loser.Kills, loser.Deaths), "each one's deaths are the other's kills");
                Assert.IsTrue(loser.PresentAtEnd);

                // Surrendered: the other's, with nothing scored.
                StartDuel(blue, red);
                Duels.Instance.SurrenderWargame(blue);

                Assert.AreEqual(2, store.Matches.Count);
                Assert.AreEqual("surrender", store.Matches[1].Reason);
                Assert.AreEqual("Blue", store.Matches[1].Side1Name, "the challenger this time");
                Assert.AreEqual((byte)2, store.Matches[1].WinnerSide);
                Assert.AreEqual((0, 0), (store.Matches[1].Side1Score, store.Matches[1].Side2Score));

                // Walked out of: forfeit, and the one who left is on it as not there at the end.
                StartDuel(red, blue);
                Duels.Instance.PlayerLeft(blue);

                Assert.AreEqual(3, store.Matches.Count);
                Assert.AreEqual("forfeit", store.Matches[2].Reason);
                Assert.AreEqual((byte)PvpMatchOutcome.Won, store.Matches[2].Outcome);
                Assert.AreEqual((byte)1, store.Matches[2].WinnerSide);
                Assert.IsFalse(store.Row(store.Matches[2].Id, blue.Player.Id).PresentAtEnd);
                Assert.IsTrue(store.Row(store.Matches[2].Id, red.Player.Id).PresentAtEnd);

                // Time up and level: nobody's.
                StartDuel(red, blue, minutes: 2, maxKills: 5);
                Duels.Instance.Kill(red, blue);
                Duels.Instance.Kill(blue, red);
                now += 2 * 60_000L;
                Duels.Instance.Worker();

                Assert.AreEqual(4, store.Matches.Count);
                Assert.AreEqual("time", store.Matches[3].Reason);
                Assert.AreEqual((byte)PvpMatchOutcome.Tied, store.Matches[3].Outcome);
                Assert.AreEqual((byte)0, store.Matches[3].WinnerSide);
                Assert.AreEqual((1, 1), (store.Matches[3].Side1Score, store.Matches[3].Side2Score));

                // And a record of its own each time.
                Assert.AreEqual(4, store.Matches.Select(m => m.Id).Distinct().Count());
                Assert.IsTrue(store.Matches.All(m => m.Kind == (byte)PvpMatchKind.Duel));
            }
            finally
            {
                Duels.Instance.Records = previous;
                Duels.Instance.Now = () => Environment.TickCount64;
            }
        }

        [TestMethod]
        public void TheChallengeAndAnswerReadAsTheClientSendsThem()
        {
            var full = Read(new ChallengeUserToWargameByNamePacket(), w =>
            {
                w.WriteTuple(3);
                w.WriteUnicodeString("Blue");
                w.WriteInt(5);
                w.WriteInt(3);
            });
            Assert.AreEqual("Blue", full.TargetName);
            Assert.AreEqual(5, full.TimeMins);
            Assert.AreEqual(3, full.MaxKills);

            var accept = Read(new WargameChallengeResponsePacket(), w =>
            {
                w.WriteTuple(2);
                w.WriteInt(1);
                w.WriteNoneStruct();
            });
            Assert.IsTrue(accept.Accepted);
            Assert.IsNull(accept.Message);

            var decline = Read(new WargameChallengeResponsePacket(), w =>
            {
                w.WriteTuple(2);
                w.WriteInt(0);
                w.WriteInt((int)PlayerMessage.PmWargameRefused);
            });
            Assert.IsFalse(decline.Accepted);
            Assert.AreEqual((int)PlayerMessage.PmWargameRefused, decline.Message);
        }

        private static (Client Red, Client Blue) Duelists(WorldTestContext world)
        {
            var red = Named(world, "Red", 0);
            var blue = Named(world, "Blue", 10);
            Find(red, blue);
            CellManager.Instance.AddToWorld(red);
            CellManager.Instance.AddToWorld(blue);
            Drain(red);
            Drain(blue);
            return (red, blue);
        }

        private static Client Named(WorldTestContext world, string name, float x)
        {
            var client = world.CreateClient(x, 0);
            client.Player.FamilyName = name;
            client.Player.Attributes[Attributes.Health] = new ActorAttributes(Attributes.Health, 1000, 1000, 1000, 0, 0);
            client.Player.Attributes[Attributes.Armor] = new ActorAttributes(Attributes.Armor, 0, 0, 0, 0, 0);
            return client;
        }

        /// <summary>The duelists are who /duel finds by name; whatever is left of a duel goes when the test does.</summary>
        private static void Find(params Client[] clients)
        {
            Duels.Instance.FindByName = name => clients.FirstOrDefault(c => string.Equals(c.Player.FamilyName, name, StringComparison.OrdinalIgnoreCase));
            Cleanup = clients;
        }

        private static Client[] Cleanup = Array.Empty<Client>();

        [TestCleanup]
        public void EndWhatIsLeft()
        {
            foreach (var client in Cleanup)
                Duels.Instance.PlayerLeft(client);

            Cleanup = Array.Empty<Client>();
            Duels.Instance.FindByName = name => null;
        }

        private static void StartDuel(Client red, Client blue, int minutes = 0, int maxKills = 0)
        {
            Duels.Instance.ChallengeUserToWargameByName(red, blue.Player.FamilyName, minutes, maxKills);
            Duels.Instance.WargameChallengeResponse(blue, true);
            Assert.AreEqual(1, Duels.Instance.Running.Count);
            Drain(red);
            Drain(blue);
        }

        private static PlayerMessage Refused(Client client, string name)
        {
            Drain(client);
            var before = Duels.Instance.Pending.Count;

            Duels.Instance.ChallengeUserToWargameByName(client, name, 0, 0);

            Assert.AreEqual(before, Duels.Instance.Pending.Count, $"{name} was challenged");
            return Messages(Drain(client)).Single();
        }

        private static void Shoot(WorldTestContext world, Client shooter, Client target, int damage)
        {
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

        private static List<PythonPacket> Drain(Client client) => MissionTestContext.Drain(client).ToList();

        private static List<PlayerMessage> Messages(IEnumerable<PythonPacket> packets) =>
            packets.OfType<DisplayWargameMessagePacket>().Select(p => p.Message).ToList();

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
    }
}
