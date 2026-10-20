using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Numerics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Rasa.Data;
    using Rasa.Game;
    using Rasa.Memory;
    using Rasa.Loot;
    using Rasa.Managers;
    using Rasa.Packets;
    using Rasa.Packets.LootDispenser.Client;
    using Rasa.Packets.LootDispenser.Server;
    using Rasa.Packets.MapChannel.Client;
    using Rasa.Packets.MapChannel.Server;
    using Rasa.Packets.Protocol;
    using Rasa.Structures;
    using Rasa.Structures.World;
    using Rasa.Test.Database;
    using Rasa.Test.Missions.Wilderness;

    /// <summary>
    /// Lootable containers the server puts down (WorldContainers): their fixed items and a roll of
    /// their loot profile, a copy each and one a squad, closed again at their reset, and locked to
    /// each player until decoded.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class WorldContainerTests
    {
        private const string Migration = "20270112000000_Add_world_containers";
        private const string Before = "20270111000000_Targets_of_opportunity_givers";

        private const uint Crate = 26721;           // UsableTreasureDispHumCrateV05
        private const uint ItemDispenser = 11096;   // UsableItemDisp_item_bag_hum_v01: no treasure dispenser
        private const uint Junk = 28;               // stacks to 50000
        private const uint Rifle = 13713;           // stacks to 1
        private const uint Rolled = 44921;          // stacks to 100

        private long _now = 1_000_000;

        [TestInitialize]
        public void SetUp()
        {
            WorldContainers.Now = () => _now;
            WorldContainers.Roll = (minimum, maximum) => minimum;
            WorldContainers.ProfileOf = key => key == "test_container" ? Profile() : null;
        }

        [TestCleanup]
        public void TearDown()
        {
            WorldContainers.Reset();
            WorldContainers.Load(Array.Empty<WorldContainerEntry>(), Array.Empty<WorldContainerItemEntry>());
        }

        [TestMethod]
        public void ARowIsKeptOnlyIfItIsATreasureDispenserWithItemsAndAProfileThatExist()
        {
            using var harness = WildernessRuntimeTestHarness.Create();

            var kept = WorldContainers.Load(new[]
            {
                Row(1, Crate), Row(2, ItemDispenser), Row(3, 999999), Row(4, Crate, profile: "no_such_profile"),
                Row(5, Crate), Row(6, Crate), Row(7, Crate, profile: "test_container", cipher: 51), Row(8, Crate, reset: 60)
            }, new[]
            {
                Item(1, Junk, 3), Item(5, Junk, 0), Item(6, 999999, 1)
            });

            Assert.AreEqual(2, kept);
            CollectionAssert.AreEqual(new uint[] { 1, 8 }, WorldContainers.All.Select(p => p.Id).ToArray());
            Assert.AreEqual(WorldContainers.DefaultResetSeconds * 1000L, WorldContainers.All[0].ResetMs, "0 is the default");
            Assert.AreEqual(60_000L, WorldContainers.All[1].ResetMs);
        }

        [TestMethod]
        public void OpeningGivesTheFixedItemsInStacksAndAProfileRollAndOpensTheWindow()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var live = PlaceCrate(harness, profile: "test_container", items: new[] { Item(1, Junk, 3), Item(1, Rifle, 2) });
            var client = harness.Client;

            UseAndRecover(harness, client, live);

            var loot = Opened(live, client);
            CollectionAssert.AreEquivalent(new[] { (Junk, 3U), (Rifle, 1U), (Rifle, 1U), (Rolled, 2U) },
                loot.LootItems.Select(item => (item.ItemTemplateId, item.ItemQuantity)).ToList());
            Assert.AreEqual(0, loot.Credits, "no credits");

            var sent = Sent(client);
            Assert.IsTrue(sent.Any(entry => entry.EntityId == live.Object.EntityId && entry.Packet is ForceStatePacket force && force.State == UseObjectState.TdStateOpened),
                "opened, to the opener");
            Assert.IsTrue(sent.Any(entry => entry.Packet is LootCorpsePacket), "the window open");
            Assert.AreEqual(UseObjectState.TdStateClosed, live.Object.StateId, "the object itself is closed: each player has their own copy");

            LootDispenserManager.Instance.RequestLootAllFromCorpse(client, new RequestLootAllFromCorpsePacket { EntityId = loot.EntityId });

            Assert.AreEqual(0, loot.Remaining().Count, "all of it taken");
            Assert.AreEqual(3U, WildernessAliaBranchesTests.HeldQuantity(harness, Junk));
            Assert.AreEqual(2U, WildernessAliaBranchesTests.HeldQuantity(harness, Rifle));
            Assert.AreEqual(2U, WildernessAliaBranchesTests.HeldQuantity(harness, Rolled));
        }

        [TestMethod]
        public void EachPlayerHasTheirOwnCopyAndItIsClosedAndFullAgainAtItsReset()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var live = PlaceCrate(harness, items: new[] { Item(1, Junk, 3) }, reset: 60);
            var first = harness.Client;
            var second = Visitor(harness, 2, live);

            UseAndRecover(harness, first, live);
            var firstLoot = Opened(live, first);
            Assert.IsFalse(WorldContainers.IsOpenedTo(live, second.Player.Id), "still closed to someone else");
            Assert.IsFalse(Sent(second).Any(entry => entry.Packet is ForceStatePacket), "nothing said to them");

            UseAndRecover(harness, second, live);
            var secondLoot = Opened(live, second);
            Assert.AreNotSame(firstLoot, secondLoot, "their own copy");
            Assert.AreEqual(3U, secondLoot.LootItems.Single().ItemQuantity);

            // Used again before the reset: no second copy, and the window again.
            Sent(first);
            UseAndRecover(harness, first, live);
            Assert.AreSame(firstLoot, Opened(live, first));
            Assert.IsTrue(Sent(first).Any(entry => entry.Packet is LootCorpsePacket), "the window again");

            _now += 60_000 - 1;
            WorldContainers.Worker(harness.Map);
            Assert.IsTrue(WorldContainers.IsOpenedTo(live, first.Player.Id), "not yet");

            _now += 1;
            WorldContainers.Worker(harness.Map);
            Assert.IsFalse(WorldContainers.IsOpenedTo(live, first.Player.Id));
            Assert.IsFalse(harness.Map.LootDispensers.ContainsKey(firstLoot.EntityId), "its dispenser gone");
            Assert.IsTrue(Sent(first).Any(entry => entry.EntityId == live.Object.EntityId && entry.Packet is ForceStatePacket force && force.State == UseObjectState.TdStateClosed),
                "closed again, to them");

            UseAndRecover(harness, first, live);
            Assert.AreNotSame(firstLoot, Opened(live, first), "full again");
        }

        [TestMethod]
        public void ASquadOpensOneCopyTogetherAndNoneOfThemCanOpenAnotherBeforeItsReset()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var live = PlaceCrate(harness, items: new[] { Item(1, Junk, 3) });
            var lead = harness.Client;
            var mate = Visitor(harness, 2, live);
            var loner = Visitor(harness, 3, live);
            var squad = new Squad(PartyLootMethod.FreeForAll, lead, mate);

            try
            {
                UseAndRecover(harness, mate, live);

                var loot = Opened(live, mate);
                Assert.AreSame(loot, Opened(live, lead), "the squad's one copy");
                CollectionAssert.IsSubsetOf(new[] { lead.Player.EntityId, mate.Player.EntityId }, loot.Looters.ToArray());
                Assert.IsTrue(Sent(lead).Any(entry => entry.EntityId == live.Object.EntityId && entry.Packet is ForceStatePacket force && force.State == UseObjectState.TdStateOpened),
                    "opened to the squad mate who did not open it");
                Assert.IsFalse(WorldContainers.IsOpenedTo(live, loner.Player.Id));

                // Out of the squad, it is still opened to them: no second copy alone.
                squad.Dispose();
                Sent(lead);
                UseAndRecover(harness, lead, live);
                Assert.AreSame(loot, Opened(live, lead));
            }
            finally
            {
                squad.Dispose();
            }
        }

        [TestMethod]
        public void ALockedContainerIsLockedToEachPlayerUntilTheyOrASquadMateDecodeIt()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var live = PlaceCrate(harness, items: new[] { Item(1, Junk, 3) }, cipher: 5);
            var lead = harness.Client;
            var mate = Visitor(harness, 2, live);
            var loner = Visitor(harness, 3, live);

            // Its lock is told on sight, each player's own.
            DynamicObjectManager.Instance.CreateDynamicObjectOnClient(loner, live.Object);
            var told = Sent(loner).Select(entry => entry.Packet).OfType<LockInfoPacket>().Single();
            Assert.AreEqual(5, told.CipherLevel);
            Assert.AreEqual(UseObjectState.TdStateClosed, told.LockStateId);

            DynamicObjectManager.Instance.RequestUseObjectPacket(lead, Use(live.Object));
            Assert.IsTrue(Sent(lead).Select(entry => entry.Packet).OfType<UserActionFailedPacket>().Any(failed => failed.MsgId == PlayerMessage.PmUseObjectUsableLocked),
                "locked");
            Assert.IsFalse(WorldContainers.IsOpenedTo(live, lead.Player.Id));

            var squad = new Squad(PartyLootMethod.Individual, lead, mate);

            try
            {
                WorldContainers.Unlocked(lead, live);

                Assert.IsTrue(WorldContainers.LockFor(live, lead.Player.Id).Unlocked);
                Assert.IsTrue(WorldContainers.LockFor(live, mate.Player.Id).Unlocked, "and for the squad mate");
                Assert.IsFalse(WorldContainers.LockFor(live, loner.Player.Id).Unlocked, "not for anyone else");
                Assert.IsTrue(Sent(mate).Select(entry => entry.Packet).OfType<LockInfoPacket>().Any(info => info.Unlocked), "the mate is told");

                UseAndRecover(harness, lead, live);
                Assert.IsNotNull(Opened(live, lead));
                Assert.IsFalse(WorldContainers.IsOpenedTo(live, mate.Player.Id), "Individual: the opener's alone");

                // Locked again with the reset.
                WorldContainers.CloseAll(live);
                Assert.IsFalse(WorldContainers.LockFor(live, lead.Player.Id).Unlocked);
            }
            finally
            {
                squad.Dispose();
            }
        }

        [TestMethod]
        public void ACipherToolDecodesAContainerForThePlayerAndTheirSquadAndNoOneElse()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var live = PlaceCrate(harness, items: new[] { Item(1, Junk, 3) }, cipher: 2);
            var lead = harness.Client;
            harness.MoveTo(live.Object.Position + new Vector3(1, 0, 0));
            var mate = Visitor(harness, 2, live);
            var loner = Visitor(harness, 3, live);
            var squad = new Squad(PartyLootMethod.FreeForAll, lead, mate);
            var classes = EntityClassManager.Instance.LoadedEntityClasses;

            try
            {
                ArmCipher(lead, skill: 2);
                ArmCipher(loner, skill: 1);

                Sent(loner);
                ToolActionManager.Instance.RequestToolAction(loner, CipherRequest(live.Object.EntityId));
                Assert.IsTrue(Sent(loner).Select(entry => entry.Packet).OfType<UserActionFailedPacket>()
                    .Any(failed => failed.MsgId == PlayerMessage.PmCipherFailSkillTooLow), "too low a level for it");

                Sent(lead);
                ToolActionManager.Instance.RequestToolAction(lead, CipherRequest(live.Object.EntityId));
                Assert.IsFalse(Sent(lead).Select(entry => entry.Packet).OfType<UserActionFailedPacket>().Any(), "taken");
                var action = harness.Map.PerformRecovery.Single(queued => queued.ActionId == ActionId.ToolCipher);
                harness.Map.PerformRecovery.Remove(action);

                lock (Server.Clients)
                    Server.Clients.Add(lead);
                try
                {
                    ToolActionManager.Instance.PerformRecovery(harness.Map, action);
                }
                finally
                {
                    lock (Server.Clients)
                        Server.Clients.Remove(lead);
                }

                Assert.IsTrue(WorldContainers.LockFor(live, lead.Player.Id).Unlocked, "decoded");
                Assert.IsTrue(WorldContainers.LockFor(live, mate.Player.Id).Unlocked, "for the squad");
                Assert.IsFalse(WorldContainers.LockFor(live, loner.Player.Id).Unlocked, "and for no one else");
                Assert.IsNull(live.Object.Lock, "the object itself has no lock: each player has their own");

                // Decoded already: nothing left to decode.
                Sent(lead);
                ToolActionManager.Instance.RequestToolAction(lead, CipherRequest(live.Object.EntityId));
                Assert.IsTrue(Sent(lead).Select(entry => entry.Packet).OfType<UserActionFailedPacket>()
                    .Any(failed => failed.MsgId == PlayerMessage.PmTargetInvalid));

                UseAndRecover(harness, lead, live);
                Assert.AreSame(Opened(live, lead), Opened(live, mate), "opened, to the squad");
            }
            finally
            {
                squad.Dispose();
                classes.Remove(CipherToolClass);
            }
        }

        [TestMethod]
        public void TakenAwayItsDispensersGoWithIt()
        {
            using var harness = WildernessRuntimeTestHarness.Create();
            var live = PlaceCrate(harness, items: new[] { Item(1, Junk, 3) });

            UseAndRecover(harness, harness.Client, live);
            var loot = Opened(live, harness.Client);

            Assert.AreEqual(1, WorldContainers.Forget(harness.Map));
            Assert.IsFalse(harness.Map.LootDispensers.ContainsKey(loot.EntityId));
            Assert.IsFalse(live.Object.IsInWorld);
            Assert.AreEqual(0, WorldContainers.On(harness.Map).Count);
        }

        [TestMethod]
        public void TheTablesGoInEmptyAndComeOut()
        {
            using var database = new OrdinaryLootTestDatabase();
            var migrator = database.Context.GetService<IMigrator>();

            migrator.Migrate(Migration);
            Assert.AreEqual(0, database.Context.WorldContainerEntries.Count());
            Assert.AreEqual(0, database.Context.WorldContainerItemEntries.Count());

            database.Context.Database.ExecuteSqlRaw(
                "INSERT INTO world_container (id, map_context_id, class_id, pos_x, pos_y, pos_z, rotation, loot_profile_key, cipher_level, reset_seconds, comment) " +
                "VALUES (1, 1220, 26721, 1.5, 2.5, 3.5, 0.25, 'test_container', 5, 600, 'a crate')");
            database.Context.Database.ExecuteSqlRaw(
                "INSERT INTO world_container_item (container_id, item_template_id, quantity) VALUES (1, 28, 3)");

            var row = new Rasa.Repositories.World.WorldUsableRepository(database.Context).GetContainers().Single();
            Assert.AreEqual((1220U, 26721U, "test_container", 5U, 600U), (row.MapContextId, row.ClassId, row.LootProfileKey, row.CipherLevel, row.ResetSeconds));
            var item = new Rasa.Repositories.World.WorldUsableRepository(database.Context).GetContainerItems().Single();
            Assert.AreEqual((1U, 28U, 3U), (item.ContainerId, item.ItemTemplateId, item.Quantity));

            migrator.Migrate(Before);
            Assert.AreEqual(0, database.Context.Database
                .SqlQueryRaw<int>("select count(*) as Value from sqlite_master where type = 'table' and name like 'world_container%'").AsEnumerable().Single());
        }

        [TestMethod]
        public void OnMySqlTheTablesGoInAndComeOut()
        {
            using var context = PersistenceIntegrationTests.CreateContext(typeof(Rasa.Context.World.MySqlWorldContext), "unused");
            var migrator = context.GetService<IMigrator>();
            var up = migrator.GenerateScript(Before, Migration);
            var down = migrator.GenerateScript(Migration, Before);

            StringAssert.Contains(up, "CREATE TABLE `world_container`");
            StringAssert.Contains(up, "`loot_profile_key` varchar(64) NULL");
            StringAssert.Contains(up, "CREATE TABLE `world_container_item`");
            StringAssert.Contains(up, "PRIMARY KEY (`container_id`, `item_template_id`)");
            StringAssert.Contains(down, "DROP TABLE `world_container_item`");
            StringAssert.Contains(down, "DROP TABLE `world_container`");
        }

        private const EntityClasses CipherToolClass = (EntityClasses)990791;

        /// <summary>A cipher tool of a sure decode (a chance of 100) in hand, and this Specialist Tools level.</summary>
        private static void ArmCipher(Client client, int skill)
        {
            var classes = EntityClassManager.Instance.LoadedEntityClasses;

            if (!classes.ContainsKey(CipherToolClass))
                classes.Add(CipherToolClass, new EntityClass((uint)CipherToolClass, "fixture cipher", 0, 0, new List<AugmentationType>(), true));

            classes[CipherToolClass].WeaponClassInfo = new WeaponClassInfo(new WeaponClassEntry
            {
                Id = (uint)CipherToolClass,
                AttackActionId = (uint)ActionId.ToolCipher,
                AttackActionArgId = 1,
                AmmoClassId = 0,
                ClipSize = 8,
                MinDamage = 100,
                MaxDamage = 588
            });

            var tool = new Item
            {
                ItemTemplate = new ItemTemplate(new ItemTemplateItemClassEntry
                {
                    ItemTemplateId = (uint)CipherToolClass,
                    ItemClass = (uint)CipherToolClass
                })
            };

            EntityManager.Instance.RegisterEntity(tool.EntityId, EntityType.Item);
            EntityManager.Instance.RegisterItem(tool.EntityId, tool);
            client.Player.Inventory.EquippedInventory[13] = tool.EntityId;
            client.Player.WeaponReady = true;
            client.Player.Skills[SkillId.SpecialistTools] = new SkillsData(SkillId.SpecialistTools, -1, skill);
        }

        private static RequestToolActionPacket CipherRequest(ulong targetId)
        {
            using var stream = new MemoryStream();
            using (var binary = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true))
            using (var writer = new PythonWriter(binary))
            {
                writer.WriteTuple(3);
                writer.WriteInt((int)ActionId.ToolCipher);
                writer.WriteUInt(1);
                writer.WriteULong(targetId);
            }

            stream.Position = 0;
            using var reader = new PythonReader(new BinaryReader(stream));
            var packet = new RequestToolActionPacket();
            packet.Read(reader);
            return packet;
        }

        private static OrdinaryLootProfile Profile() =>
            new("test_container", new[]
            {
                new OrdinaryLootGroup("all", LootGroupKind.Consumable, 10000, 1, 1, new[] { new OrdinaryLootCandidate(Rolled, 1, 2, 2) })
            });

        private static WorldContainerEntry Row(uint id, uint classId, string profile = null, uint cipher = 0, uint reset = 0) => new()
        {
            Id = id, MapContextId = 1220, ClassId = classId, PosX = 886.11, PosY = 305.8, PosZ = 347.81,
            LootProfileKey = profile, CipherLevel = cipher, ResetSeconds = reset
        };

        private static WorldContainerItemEntry Item(uint container, uint template, uint quantity) => new()
        {
            ContainerId = container, ItemTemplateId = template, Quantity = quantity
        };

        private static WorldContainers.Live PlaceCrate(WildernessRuntimeTestHarness harness, string profile = null,
            WorldContainerItemEntry[] items = null, uint cipher = 0, uint reset = 0)
        {
            Assert.AreEqual(1, WorldContainers.Load(new[] { Row(1, Crate, profile, cipher, reset) }, items ?? Array.Empty<WorldContainerItemEntry>()));
            Assert.AreEqual(1, WorldContainers.Place(harness.Map));
            harness.Drain();
            return WorldContainers.LiveOf(harness.Map, 1);
        }

        private static Client Visitor(WildernessRuntimeTestHarness harness, uint characterId, WorldContainers.Live live)
        {
            var client = harness.Context.CreateAdditionalClient(characterId, manager: harness.Manager);

            foreach (var kind in new[] { Attributes.Health, Attributes.Chi, Attributes.Power, Attributes.Armor })
                client.Player.Attributes[kind] = new ActorAttributes(kind, 100, 100, 100, 0, 0);

            client.SetWorldPosition(live.Object.Position + new Vector3(1, 0, 0), 0);
            CellManager.Instance.AddToWorld(client);
            Sent(client);
            return client;
        }

        private static RequestUseObjectPacket Use(DynamicObject obj) =>
            new() { ActionId = ActionId.UseObject, ActionArgId = 1, EntityId = obj.EntityId };

        /// <summary>A use, through the server's request handling, run past its windup.</summary>
        private static void UseAndRecover(WildernessRuntimeTestHarness harness, Client client, WorldContainers.Live live)
        {
            if (client == harness.Client)
                harness.MoveTo(live.Object.Position + new Vector3(1, 0, 0));

            DynamicObjectManager.Instance.RequestUseObjectPacket(client, Use(live.Object));
            ActorActionManager.Instance.DoWork(harness.Map, WorldContainers.UseWindupMs);
        }

        private static LootDispenser Opened(WorldContainers.Live live, Client client)
        {
            lock (live)
            {
                Assert.IsTrue(live.Openings.TryGetValue(client.Player.Id, out var opening), $"opened to {client.Player.Id}");
                return opening.Loot;
            }
        }

        private static List<(ulong EntityId, PythonPacket Packet)> Sent(Client client) =>
            WorldTestContext.Drain(client).Select(packet => packet.Message).OfType<CallMethodMessage>()
                .Select(message => (message.EntityId, message.Packet)).ToList();

        /// <summary>A squad of these, with this loot method, for as long as it is not disposed.</summary>
        private sealed class Squad : IDisposable
        {
            private readonly Client[] _clients;
            private bool _disposed;

            internal Party Party { get; }

            internal Squad(PartyLootMethod method, params Client[] clients)
            {
                _clients = clients;
                Party = new Party(PartyManager.Instance.GetPartyId, clients[0].AccountEntry.Id,
                    clients.Select(client => new PartyMember(client)).ToList())
                {
                    LootMethod = method, LootThreshold = PartyLootThreshold.Prototype
                };
                PartyManager.Instance.Parties.Add(Party.Id, Party);
                foreach (var client in clients)
                    client.Player.PartyId = Party.Id;
                lock (Server.Clients)
                    Server.Clients.AddRange(clients);
            }

            public void Dispose()
            {
                if (_disposed)
                    return;

                _disposed = true;
                foreach (var client in _clients)
                    client.Player.PartyId = 0;
                lock (Server.Clients)
                    foreach (var client in _clients)
                        Server.Clients.Remove(client);
                PartyManager.Instance.Parties.Remove(Party.Id);
                PartyManager.Instance.FreePartyId(Party.Id);
            }
        }
    }
}
