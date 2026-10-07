using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rasa.Test.World
{
    using Rasa.Data;
    using Rasa.Game;
    using Rasa.Managers;
    using Rasa.Memory;
    using Rasa.Packets;
    using Rasa.Packets.Communicator.Server;
    using Rasa.Packets.Inventory.Client;
    using Rasa.Packets.Inventory.Server;
    using Rasa.Packets.Protocol;
    using Rasa.Structures;
    using Rasa.Structures.Char;
    using Rasa.Test.Missions;

    // An item dropped on one of the clan lockbox's tab buttons (clanlockboxwindow.OnDNDDropTab):
    // inventory.AddItemToClanInventoryTab(entityId, tabId, None, quantity) leaves the slot None,
    // and _SendServerRequest sends whatever the item's own inventory makes of that:
    //
    //   from the pack           "ClanLockbox_DepositItemInTab", (srcSlot, the tab shown, quantity)
    //   from the lockbox        "ClanLockbox_MoveItem", (srcSlot, None, quantity)
    //   from the weapon drawer  "RequestEquipWeapon", (None, CLANINVENTORY, drawerSlot)
    //   from an equipment slot  "RequestEquipArmor", (None, CLANINVENTORY, equipmentSlot)
    //
    // The tab that was dropped on is in none of them. The footlocker's window resolves a slot of
    // the tab before it sends anything (inventory.AddItemToHomeInventoryTab), so only the clan
    // lockbox sends a None.
    [TestClass]
    [DoNotParallelize]
    public class LockboxTabDropTests
    {
        private const uint Ammo = 9401, Filler = 9402, ItemClass = 3147;
        private const int ClanInventory = 15, PersonalInventory = 1, TabSize = 100;

        [TestMethod]
        public void ALockboxItemDroppedOnATabButtonIsRead()
        {
            var packet = Read<ClanLockbox_MoveItemPacket>(GameOpcode.ClanLockbox_MoveItem, pw =>
            {
                pw.WriteTuple(3);
                pw.WriteUInt(7);
                pw.WriteNoneStruct();
                pw.WriteInt(3);
            });

            Assert.AreEqual(7u, packet.SrcSlot);
            Assert.IsTrue(packet.NoSlotNamed);
            Assert.AreEqual(3, packet.Quantity);
        }

        [TestMethod]
        public void ALockboxItemDroppedOnASlotIsReadAsBefore()
        {
            var packet = Read<ClanLockbox_MoveItemPacket>(GameOpcode.ClanLockbox_MoveItem, pw =>
            {
                pw.WriteTuple(3);
                pw.WriteUInt(7);
                pw.WriteUInt(105);
                pw.WriteInt(3);
            });

            Assert.AreEqual(7u, packet.SrcSlot);
            Assert.AreEqual(105u, packet.DestSlot);
            Assert.IsFalse(packet.NoSlotNamed);
        }

        [TestMethod]
        public void ADrawerWeaponDroppedOnATabButtonIsRead()
        {
            var packet = Read<RequestEquipWeaponPacket>(GameOpcode.RequestEquipWeapon, pw =>
            {
                pw.WriteTuple(3);
                pw.WriteNoneStruct();
                pw.WriteInt(ClanInventory);
                pw.WriteUInt(2);
            });

            Assert.IsTrue(packet.NoSlotNamed);
            Assert.AreEqual(InventoryType.ClanInventory, packet.InventoryType);
            Assert.AreEqual(2u, packet.DestSlot);
        }

        [TestMethod]
        public void AWornPieceDroppedOnATabButtonIsRead()
        {
            var packet = Read<RequestEquipArmorPacket>(GameOpcode.RequestEquipArmor, pw =>
            {
                pw.WriteTuple(3);
                pw.WriteNoneStruct();
                pw.WriteInt(ClanInventory);
                pw.WriteUInt(15);
            });

            Assert.IsTrue(packet.NoSlotNamed);
            Assert.AreEqual(InventoryType.ClanInventory, packet.SrcInventory);
            Assert.AreEqual(15u, packet.DestSlot);
        }

        [TestMethod]
        public void EquippingFromAPackSlotIsReadAsBefore()
        {
            var weapon = Read<RequestEquipWeaponPacket>(GameOpcode.RequestEquipWeapon, pw =>
            {
                pw.WriteTuple(3);
                pw.WriteUInt(4);
                pw.WriteInt(PersonalInventory);
                pw.WriteUInt(2);
            });
            var armor = Read<RequestEquipArmorPacket>(GameOpcode.RequestEquipArmor, pw =>
            {
                pw.WriteTuple(3);
                pw.WriteUInt(4);
                pw.WriteInt(PersonalInventory);
                pw.WriteUInt(15);
            });

            Assert.AreEqual(4u, weapon.SrcSlot);
            Assert.IsFalse(weapon.NoSlotNamed);
            Assert.AreEqual(InventoryType.Personal, weapon.InventoryType);
            Assert.AreEqual(4u, armor.SrcSlot);
            Assert.IsFalse(armor.NoSlotNamed);
            Assert.AreEqual(InventoryType.Personal, armor.SrcInventory);
        }

        [TestMethod]
        [DataRow(3, 100, DisplayName = "from the first tab to the second")]
        [DataRow(140, 0, DisplayName = "from the second tab to the first")]
        public void WithTwoTabsALockboxItemDroppedOnATabButtonGoesToTheOtherTab(int from, int to)
        {
            // The request does not say which tab: with two there is only one it can be.
            using var fixture = new Fixture(tabs: 2);
            var stored = fixture.InLockbox(Ammo, 3, from);

            fixture.DropOnATabButton(from);

            fixture.AssertHolders(from, 0);
            fixture.AssertHolders(to, stored.EntityId);
            Assert.AreEqual((uint)to, stored.OwnerSlotId);
            fixture.AssertLockboxRow(stored, to);

            var other = fixture.Sent(fixture.Other);

            Assert.IsTrue(other.Any(packet => packet is InventoryRemoveItemPacket remove && remove.EntityId == stored.EntityId));
            Assert.IsTrue(other.Any(packet => packet is InventoryAddItemPacket add && add.Type == InventoryType.ClanInventory && add.EntityId == stored.EntityId && add.SlotId == to));
        }

        [TestMethod]
        public void ItGoesToTheFirstFreeSlotOfTheOtherTab()
        {
            using var fixture = new Fixture(tabs: 2);
            var stored = fixture.InLockbox(Ammo, 3, 3);

            fixture.InLockbox(Filler, 1, 100);
            fixture.InLockbox(Filler, 1, 101);
            fixture.DropOnATabButton(3);

            fixture.AssertHolders(102, stored.EntityId);
            fixture.AssertLockboxRow(stored, 102);
        }

        [TestMethod]
        public void WithTheOtherTabFullItStaysAndThePlayerIsTold()
        {
            using var fixture = new Fixture(tabs: 2);
            var stored = fixture.InLockbox(Ammo, 3, 3);

            for (var slot = TabSize; slot < 2 * TabSize; slot++)
                fixture.InLockbox(Filler, 1, slot);

            fixture.Sent(fixture.Taker);
            fixture.DropOnATabButton(3);

            fixture.AssertHolders(3, stored.EntityId);
            fixture.AssertLockboxRow(stored, 3);
            Assert.AreEqual(PlayerMessage.PmYourFootlockerIsFull, ((DisplayClientMessagePacket)fixture.Sent(fixture.Taker).Single()).MsgId);
            Assert.AreEqual(0, fixture.Sent(fixture.Other).Count);
        }

        [TestMethod]
        [DataRow(1u, DisplayName = "one tab: there is no other")]
        [DataRow(3u, DisplayName = "three tabs: it could be either of two")]
        [DataRow(5u, DisplayName = "five tabs")]
        public void WithNoTabItCanOnlyBeTheItemStays(uint tabs)
        {
            using var fixture = new Fixture(tabs);
            var stored = fixture.InLockbox(Ammo, 3, 3);

            fixture.Sent(fixture.Taker);
            fixture.DropOnATabButton(3);

            fixture.AssertHolders(3, stored.EntityId);
            fixture.AssertLockboxRow(stored, 3);
            Assert.AreEqual(0, fixture.Sent(fixture.Taker).Count);
            Assert.AreEqual(0, fixture.Sent(fixture.Other).Count);
        }

        [TestMethod]
        public void ATabButtonDropFromAnEmptySlotDoesNothing()
        {
            using var fixture = new Fixture(tabs: 2);

            fixture.DropOnATabButton(3);

            Assert.IsTrue(fixture.Taker.Player.Inventory.ClanInventory.All(entityId => entityId == 0));
            Assert.AreEqual(0, fixture.Sent(fixture.Taker).Count);
        }

        [TestMethod]
        [DataRow(InventoryType.ClanInventory, DisplayName = "as the client sends it")]
        [DataRow(InventoryType.Personal, DisplayName = "naming the pack, which the client does not")]
        public void ADrawerWeaponOrWornPieceDroppedOnATabButtonStaysWhereItIs(InventoryType named)
        {
            // Neither handler moves an item between the lockbox and the drawer or an equipment
            // slot; with a slot they answer nothing, and with none they do the same.
            using var fixture = new Fixture(tabs: 2);
            var player = fixture.Taker.Player;
            var carried = fixture.Make(Ammo, 1);

            player.Inventory.WeaponDrawer = Enumerable.Repeat(0UL, 5).ToList();
            player.Inventory.EquippedInventory = Enumerable.Repeat(0UL, 22).ToList();
            player.Inventory.WeaponDrawer[2] = carried.EntityId;
            player.Inventory.EquippedInventory[15] = carried.EntityId;

            fixture.Inventory.RequestEquipWeapon(fixture.Taker, new RequestEquipWeaponPacket { NoSlotNamed = true, InventoryType = named, DestSlot = 2 });
            fixture.Inventory.RequestEquipArmor(fixture.Taker, new RequestEquipArmorPacket { NoSlotNamed = true, SrcInventory = named, DestSlot = 15 });

            Assert.AreEqual(carried.EntityId, player.Inventory.WeaponDrawer[2]);
            Assert.AreEqual(carried.EntityId, player.Inventory.EquippedInventory[15]);
            Assert.AreEqual(0UL, player.Inventory.PersonalInventory[0], "taken off into the first pack slot");
            Assert.IsTrue(player.Inventory.ClanInventory.All(entityId => entityId == 0));
            Assert.AreEqual(0, fixture.Sent(fixture.Taker).Count);
        }

        /// <summary>The packet a method call decodes to, as it arrives from the client.</summary>
        private static T Read<T>(GameOpcode method, Action<PythonWriter> arguments) where T : ClientPythonPacket
        {
            var message = Message(method, arguments);

            Assert.IsTrue(message.ReadPacket(), "a throw or a false here closes the connection");

            return (T)message.Packet;
        }

        /// <summary>A method call as it arrives from the client: the arguments between the payload's markers.</summary>
        private static CallServerMethodMessage Message(GameOpcode method, Action<PythonWriter> arguments)
        {
            byte[] payload;

            using (var buffer = new MemoryStream())
            {
                using (var binary = new BinaryWriter(buffer, Encoding.UTF8, leaveOpen: true))
                {
                    binary.Write((byte)0x4F);

                    using (var writer = new PythonWriter(binary))
                        arguments(writer);

                    binary.Write((byte)0x66);
                }

                payload = buffer.ToArray();
            }

            using var stream = new MemoryStream();

            using (var binary = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true))
            using (var writer = new ProtocolBufferWriter(binary, ProtocolBufferFlags.DontFragment))
            {
                writer.WriteUInt((uint)method);
                writer.WriteArray(payload);
            }

            stream.Position = 0;

            using var input = new BinaryReader(stream);
            using var reader = new ProtocolBufferReader(input, ProtocolBufferFlags.DontFragment);
            var message = new CallServerMethodMessage { Subtype = CallServerMethodSubtype.UserMethodById };

            message.Read(reader);

            return message;
        }

        /// <summary>The leader of a clan and one other member, both in the world with the lockbox.</summary>
        private sealed class Fixture : IDisposable
        {
            private readonly ClanManager _clans = ClanManager.Instance;
            private readonly System.Collections.Concurrent.ConcurrentDictionary<uint, Lazy<ClanEntry>> _previousClans;
            private readonly System.Collections.Concurrent.ConcurrentDictionary<uint, Lazy<List<ClanMemberEntry>>> _previousMembers;

            internal MissionTestContext Context { get; } = MissionTestContext.WithCompletableMission(429);
            internal InventoryManager Inventory { get; }
            internal ClanEntry Clan { get; }
            internal Client Taker { get; }
            internal Client Other { get; }

            internal Fixture(uint tabs)
            {
                _previousClans = _clans.Clans;
                _previousMembers = _clans.ClanMembers;

                Inventory = new InventoryManager(Context, Context.Manager);
                Taker = Context.Client;
                Other = Context.CreateAdditionalClient(2);
                Clan = Context.CreateClanForPlayer();
                Clan.PurashedTabs = tabs;

                using (var unit = Context.CreateChar())
                    Assert.IsTrue(unit.ClanMembers.InsertClanMemberData(Clan.Id, Other.Player.Id, ClanRank.Member, ""));

                _clans.ClanMembers[Clan.Id].Value.Add(new ClanMemberEntry { ClanId = Clan.Id, CharacterId = Other.Player.Id, Rank = ClanRank.Member, Note = "" });
                Other.Player.ClanId = Clan.Id;

                foreach (var client in new[] { Taker, Other })
                {
                    client.Player.Inventory.ResetClanInventory();

                    lock (Server.Clients)
                        Server.Clients.Add(client);

                    WorldTestContext.Drain(client);
                }
            }

            /// <summary>An item in a lockbox slot, as both members hold it.</summary>
            internal Item InLockbox(uint templateId, uint quantity, int slot)
            {
                var item = Make(templateId, quantity);

                item.OwnerId = 0;
                item.OwnerSlotId = (uint)slot;
                Taker.Player.Inventory.ClanInventory[slot] = item.EntityId;
                Other.Player.Inventory.ClanInventory[slot] = item.EntityId;

                using var unit = Context.CreateChar();
                unit.ClanInventories.AddInvItem(Clan.Id, (uint)slot, item.Id);

                return item;
            }

            /// <summary>The lockbox item in that slot, dropped on a tab button by the leader.</summary>
            internal void DropOnATabButton(int slot)
            {
                Inventory.ClanLockbox_MoveItem(Taker, new ClanLockbox_MoveItemPacket { SrcSlot = (uint)slot, NoSlotNamed = true, Quantity = 1 });
            }

            internal List<PythonPacket> Sent(Client client) => WorldTestContext.Drain(client)
                .Select(packet => packet.Message).OfType<CallMethodMessage>().Select(message => message.Packet).ToList();

            /// <summary>What both members have in that lockbox slot.</summary>
            internal void AssertHolders(int slot, ulong entityId)
            {
                Assert.AreEqual(entityId, Taker.Player.Inventory.ClanInventory[slot], $"the leader's slot {slot}");
                Assert.AreEqual(entityId, Other.Player.Inventory.ClanInventory[slot], $"the other member's slot {slot}");
            }

            internal void AssertLockboxRow(Item item, int slot)
            {
                using var unit = Context.CreateChar();

                Assert.AreEqual((uint)slot, unit.ClanInventories.GetItems(Clan.Id).Single(entry => entry.ItemId == item.Id).SlotId);
            }

            internal Item Make(uint templateId, uint quantity)
            {
                Context.AddRewardTemplate(templateId, ItemClass);

                var template = EntityClassManager.Instance.LoadedEntityClasses[(EntityClasses)ItemClass].ItemTemplates[templateId];
                var item = ItemManager.StageItem(template, quantity, "");

                using (var unit = Context.CreateChar())
                    item.Id = unit.Items.CreateItem(item);

                EntityManager.Instance.RegisterEntity(item.EntityId, EntityType.Item);
                EntityManager.Instance.RegisterItem(item.EntityId, item);

                return item;
            }

            public void Dispose()
            {
                lock (Server.Clients)
                {
                    Server.Clients.Remove(Taker);
                    Server.Clients.Remove(Other);
                }

                _clans.Clans = _previousClans;
                _clans.ClanMembers = _previousMembers;
                Taker.Player.ClanId = 0;
                Context.Dispose();
            }
        }
    }
}
