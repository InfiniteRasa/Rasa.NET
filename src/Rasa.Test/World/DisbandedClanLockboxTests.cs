using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Rasa.Data;
    using Rasa.Game;
    using Rasa.Managers;
    using Rasa.Packets.Clan.Client;
    using Rasa.Repositories.UnitOfWork;
    using Rasa.Structures.Char;
    using Rasa.Test.Missions;

    // A disbanded clan's lockbox is kept: its clan_inventory rows and the items they name stay in
    // the database, under the id of a clan that no longer exists, so what was in it can be given
    // back by hand. A clan disbanded by its leader, and one that goes with its only member when
    // that character is deleted, alike. Nothing loads them, and no new clan is given that id.
    [TestClass]
    [DoNotParallelize]
    public class DisbandedClanLockboxTests
    {
        [TestMethod]
        public void ADisbandedClansLockboxRowsAndItemsAreKept()
        {
            using var context = MissionTestContext.WithCompletableMission(429);
            var clan = context.CreateClanForPlayer();
            var stored = InLockbox(context, clan.Id, 0, 7);

            WithLiveClan(context, clan, clans =>
                clans.DisbandClan(context.Client, new DisbandClanPacket { ClanId = clan.Id }));

            Assert.AreEqual(0u, context.Client.Player.ClanId);
            AssertKept(context, clan.Id, stored);
        }

        [TestMethod]
        public void AClanThatGoesWithItsDeletedLeaderKeepsItsLockboxToo()
        {
            using var context = MissionTestContext.WithCompletableMission(429);
            var clan = context.CreateClanForPlayer();
            var stored = InLockbox(context, clan.Id, 0, 7);

            WithLiveClan(context, clan, clans =>
            {
                using var unit = context.CreateChar();
                var character = unit.Characters.Get(context.Client.Player.Id);

                Assert.IsNotNull(clans.RemoveDeletedCharacter(unit, character, "Leader"), "the clan went with them");
            });

            AssertKept(context, clan.Id, stored);
        }

        [TestMethod]
        public void NoClanLoadsThemAndANewClanIsNotGivenTheirId()
        {
            using var context = MissionTestContext.WithCompletableMission(429);
            var clan = context.CreateClanForPlayer();
            var stored = InLockbox(context, clan.Id, 0, 7);

            WithLiveClan(context, clan, clans =>
                clans.DisbandClan(context.Client, new DisbandClanPacket { ClanId = clan.Id }));

            // The lockboxes as the server loads them at start: the kept rows are not read, nor
            // taken for a broken lockbox and deleted.
            WithLiveClan(context, null, clans =>
            {
                using var unit = context.CreateChar();
                clans.InitCurrentClanInventories(unit.Clans.GetClans());
            });

            AssertKept(context, clan.Id, stored);

            using var write = context.CreateChar();
            var next = write.Clans.CreateClan("After", false);

            Assert.AreNotEqual(clan.Id, next.Id, "a disbanded clan's id is not handed out again");
            Assert.AreEqual(0, write.ClanInventories.GetItems(next.Id).Count, "the new clan's lockbox is its own");
        }

        #region Fixture

        /// <summary>A lockbox item: an items row and the clan_inventory row in that slot. The item ids.</summary>
        private static uint[] InLockbox(MissionTestContext context, uint clanId, params uint[] slots)
        {
            var ids = new List<uint>();

            using var seed = context.Open();

            foreach (var slot in slots)
            {
                var item = new ItemEntry { ItemTemplateId = 17131, StackSize = 1, CrafterName = "" };
                seed.ItemEntries.Add(item);
                seed.SaveChanges();
                seed.ClanInventoryEntries.Add(new ClanInventoryEntry(clanId, slot, item.ItemId));
                seed.SaveChanges();
                ids.Add(item.ItemId);
            }

            return ids.ToArray();
        }

        private static void AssertKept(MissionTestContext context, uint clanId, uint[] stored)
        {
            using var unit = context.CreateChar();

            Assert.IsNull(unit.Clans.GetClanById(clanId), "the clan is gone");
            Assert.AreEqual(0, unit.ClanMembers.GetAllClanMembersByClanId(clanId).Count);
            CollectionAssert.AreEquivalent(stored, unit.ClanInventories.GetItems(clanId).Select(row => row.ItemId).ToArray(), "the lockbox rows");

            foreach (var id in stored)
                Assert.IsNotNull(unit.Items.GetItem(id), $"item {id}");
        }

        /// <summary>A ClanManager over the context's database, holding the clan as the server does, for the length of the body.</summary>
        private static void WithLiveClan(MissionTestContext context, ClanEntry clan, Action<ClanManager> body)
        {
            var instance = typeof(ClanManager).GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic);
            var previous = instance.GetValue(null);

            lock (Server.Clients)
                Server.Clients.Add(context.Client);

            try
            {
                var clans = (ClanManager)typeof(ClanManager).GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null,
                    new[] { typeof(IGameUnitOfWorkFactory) }, null).Invoke(new object[] { context });

                if (clan != null)
                {
                    using var unit = context.CreateChar();
                    var members = unit.ClanMembers.GetAllClanMembersByClanId(clan.Id);

                    clans.Clans[clan.Id] = new Lazy<ClanEntry>(() => clan);
                    clans.ClanMembers[clan.Id] = new Lazy<List<ClanMemberEntry>>(() => members);
                }

                instance.SetValue(null, clans);
                body(clans);
            }
            finally
            {
                instance.SetValue(null, previous);

                lock (Server.Clients)
                    Server.Clients.Remove(context.Client);
            }
        }

        #endregion
    }
}
