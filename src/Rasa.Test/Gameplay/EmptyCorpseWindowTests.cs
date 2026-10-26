using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rasa.Data;
using Rasa.Packets.LootDispenser.Client;
using Rasa.Packets.LootDispenser.Server;
using Rasa.Test.Gameplay;
using LootFixture = Rasa.Test.Gameplay.LootConsolidationTests.LootFixture;

namespace Rasa.Test.Gameplay
{
    [TestClass, DoNotParallelize]
    public class EmptyCorpseWindowTests
    {
        [TestMethod]
        [DataRow(0)]
        [DataRow(7)]
        public void EmptyOrCreditOnlyCorpsesDoNotSendTheBackpackOpeningResponse(int credits)
        {
            using var f = new LootFixture();
            f.Loot.LootItems.Single().Taken = true;
            f.Loot.Credits = credits;
            f.Manager.RequestCorpseLooting(f.Client,new RequestCorpseLootingPacket { EntityId=f.Loot.EntityId });
            Assert.IsFalse(f.Drain().OfType<LootCorpsePacket>().Any());
            Assert.AreEqual(100+credits,f.Client.Player.Credits[CurencyType.Credits]);
            Assert.AreEqual(0UL,f.Loot.CurrentLooter);
            using var db = f.Storage.Open();
            Assert.AreEqual(100+credits,db.CharacterEntries.AsNoTracking().Single().Credit);
            f.Manager.RequestCorpseLooting(f.Client,new RequestCorpseLootingPacket { EntityId=f.Loot.EntityId });
            Assert.IsFalse(f.Drain().OfType<LootCorpsePacket>().Any());
            Assert.AreEqual(100+credits,f.Client.Player.Credits[CurencyType.Credits],"Repeated clicks cannot pay twice.");
        }
        [TestMethod]
        public void ItemsReservedForSomeoneElseDoNotOpenAnEmptyWindowOrGetClaimed()
        {
            using var f = new LootFixture();
            f.Loot.LootItems.Single().ReservedFor = f.Client.Player.EntityId+100;
            f.Loot.Credits=0;
            f.Manager.RequestCorpseLooting(f.Client,new RequestCorpseLootingPacket { EntityId=f.Loot.EntityId });
            Assert.IsFalse(f.Drain().OfType<LootCorpsePacket>().Any());
            Assert.IsFalse(f.Loot.LootItems.Single().Taken);
            Assert.AreEqual(0UL,f.Loot.CurrentLooter);
        }
        [TestMethod]
        public void ACorpseWithATakeableItemStillOpensItsNormalLootWindow()
        {
            using var f = new LootFixture();
            f.Manager.RequestCorpseLooting(f.Client,new RequestCorpseLootingPacket { EntityId=f.Loot.EntityId });
            Assert.AreEqual(1,f.Drain().OfType<LootCorpsePacket>().Count());
            Assert.AreEqual(f.Client.Player.EntityId,f.Loot.CurrentLooter);
        }
        [TestMethod]
        public void FailedCreditOnlyClaimsDoNotOpenTheBackpackOrPublishAnUncommittedPurse()
        {
            using var f = new LootFixture();
            f.Loot.LootItems.Single().Taken=true;
            f.Storage.BeforeSave = _ => throw new DbUpdateException("credit-only save failure");
            f.Manager.RequestCorpseLooting(f.Client,new RequestCorpseLootingPacket { EntityId=f.Loot.EntityId });
            Assert.IsFalse(f.Drain().OfType<LootCorpsePacket>().Any());
            Assert.AreEqual(100,f.Client.Player.Credits[CurencyType.Credits]);
            Assert.AreEqual(7,f.Loot.Credits);
            using var db = f.Storage.Open();
            Assert.AreEqual(100,db.CharacterEntries.AsNoTracking().Single().Credit);
        }
    }
}
