using System;
using System.Collections.Generic;
using System.Linq;

namespace Rasa.Managers
{
    using Data;
    using Game;
    using Repositories.Char;
    using Structures;

    /// <summary>
    /// The inbox as the place for what a full backpack cannot take.
    ///
    /// The inbox is the Pick Up Items tab of the auction house window: InventoryType.InboxInventory
    /// rows, up to Inventory.MaxInboxItems per character, taken into the backpack with
    /// RequestTakeItemFromInboxInventory at any auctioneer. The auction house already sends what
    /// a player bought or did not sell there. It now also takes:
    ///  - a crafting result that does not fit (KraftwerksManager.HandOver), instead of leaving it
    ///    on a job held only in memory;
    ///  - a mission reward item that does not fit (InventoryPlan, overflowToInbox), instead of
    ///    refusing the turn-in;
    ///  - an item a GM gives that does not fit (.giveitem), and items a GM mails to a player who
    ///    may be offline (.mailitem / mailitem).
    /// Each is asked about Character Unique first, as every way in is (InventoryManager.Unique);
    /// the inbox counts as held.
    /// The client keeps an overflow inventory too (AddOverflowItem and the rest), but the 1.16.5.0
    /// client never shows it or lets anything out of it.
    ///
    /// Mission items bound to an assignment never go to the inbox (TryMoveToInbox refuses them):
    /// they have to be carried. Vendor purchases and loot are still refused with a full backpack:
    /// the player chose those and can make room.
    /// </summary>
    public partial class InventoryManager
    {
        /// <summary>Inbox slots a character has free, lowest first, from its rows.</summary>
        internal static List<uint> FreeInboxSlots(ICharUnitOfWork unitOfWork, uint accountId, uint characterId)
        {
            var used = unitOfWork.CharacterInventories.GetItems(accountId)
                .Where(row => row.CharacterId == characterId && row.InventoryType == (uint)InventoryType.InboxInventory)
                .Select(row => row.SlotId)
                .ToHashSet();

            return Enumerable.Range(0, Inventory.MaxInboxItems).Select(slot => (uint)slot).Where(slot => !used.Contains(slot)).ToList();
        }

        /// <summary>
        /// An item that has its item row but no place yet - made for this player, or what is left
        /// of it after a partial fit - put in the inbox of the player in the world. False when the
        /// inbox is full, with nothing changed.
        /// </summary>
        public bool SendToInbox(Client client, Item item)
        {
            if (client?.Player == null || client.AccountEntry == null || item == null || item.Id == 0 || item.StackSize == 0)
                return false;

            using var unitOfWork = _gameUnitOfWorkFactory.CreateChar();
            var free = FreeInboxSlots(unitOfWork, client.AccountEntry.Id, client.Player.Id);

            if (free.Count == 0)
                return false;

            unitOfWork.CharacterInventories.AddInvItem(client.AccountEntry.Id, client.Player.Id, (uint)InventoryType.InboxInventory, free[0], item.Id);

            item.OwnerId = client.Player.Id;
            item.OwnerSlotId = free[0];

            if (client.State == ClientState.Ingame)
                PublishInboxDelivery(client, item);

            return true;
        }

        /// <summary>The player is told where what did not fit went.</summary>
        public static void TellSentToInbox(Client client, int stacks)
        {
            if (client == null || stacks <= 0)
                return;

            CommunicatorManager.Instance.SystemMessage(client,
                $"Your backpack is full: {stacks} item{(stacks == 1 ? " was" : "s were")} sent to your pick-up box. "
                + "Collect them from the Pick Up Items tab at any auctioneer.");
        }

        /// <summary>
        /// A GM's gift of new items into a player's inbox, online or not. The character is the one
        /// in the world, else the one selected at character selection, else the first. Fails, with
        /// nothing made, when the inbox has no room for all of it.
        /// </summary>
        public Moderation.Result MailItems(string familyName, uint itemTemplateId, uint quantity, string from)
        {
            var template = ItemManager.Instance.GetItemTemplateById(itemTemplateId);
            var classInfo = template == null ? null : EntityClassManager.Instance.GetClassInfo(template.Class);

            if (template == null || classInfo?.ItemClassInfo == null || classInfo.ItemClassInfo.StackSize == 0)
                return new Moderation.Result(false, $"There is no item template {itemTemplateId}.");

            var stackSize = classInfo.ItemClassInfo.StackSize;

            if (quantity == 0)
                quantity = stackSize;

            var stacks = (int)((quantity + stackSize - 1) / stackSize);

            if (stacks > Inventory.MaxInboxItems)
                return new Moderation.Result(false, $"That is {stacks} stacks; an inbox holds {Inventory.MaxInboxItems}.");

            // Character Unique: one stack, of any size, and none to a character who holds one.
            if (IsCharacterUnique(template) && stacks > 1)
                return new Moderation.Result(false, $"Item template {itemTemplateId} is Character Unique: at most one stack of {stackSize}.");

            var online = Moderation.Online(familyName).FirstOrDefault(c => c.Player != null && c.Player.Id != 0 && c.State == ClientState.Ingame);

            using var unitOfWork = _gameUnitOfWorkFactory.CreateChar();

            uint accountId, characterId;
            string name;

            if (online != null)
            {
                accountId = online.AccountEntry.Id;
                characterId = online.Player.Id;
                name = $"{online.Player.Name} {online.AccountEntry.FamilyName}";
            }
            else
            {
                var account = Moderation.FindStoredAccount(familyName);

                if (account == null)
                    return new Moderation.Result(false, $"There is no family named {familyName}.");

                var characters = unitOfWork.Characters.GetByAccountId(account.Id);

                if (characters.Count == 0)
                    return new Moderation.Result(false, $"{account.FamilyName} has no characters.");

                var character = characters.TryGetValue(account.SelectedSlot, out var selected) ? selected : characters.OrderBy(c => c.Key).First().Value;

                accountId = account.Id;
                characterId = character.Id;
                name = $"{character.Name} {account.FamilyName}";
            }

            if (IsCharacterUnique(template) && (online != null
                    ? HoldsTemplate(online.Player, itemTemplateId)
                    : HoldsTemplateStored(unitOfWork, accountId, characterId, itemTemplateId)))
                return new Moderation.Result(false, $"Item template {itemTemplateId} is Character Unique, and {name} already has one.");

            var free = FreeInboxSlots(unitOfWork, accountId, characterId);

            if (free.Count < stacks)
                return new Moderation.Result(false, $"{name}'s inbox has room for {free.Count} more stack(s); this needs {stacks}.");

            var left = quantity;

            for (var i = 0; i < stacks; i++)
            {
                var amount = Math.Min(left, stackSize);
                left -= amount;

                var item = ItemManager.StageItem(template, amount, from ?? "");
                item.Id = unitOfWork.Items.CreateItem(item);

                if (online != null)
                {
                    // Registered: the player is here to be shown it.
                    EntityManager.Instance.RegisterEntity(item.EntityId, EntityType.Item);
                    EntityManager.Instance.RegisterItem(item.EntityId, item);
                }
                else
                {
                    // Only the rows: the item becomes an entity when the character next loads.
                    EntityManager.Instance.FreeEntity(item.EntityId);
                }

                unitOfWork.CharacterInventories.AddInvItem(accountId, characterId, (uint)InventoryType.InboxInventory, free[i], item.Id);
                item.OwnerId = characterId;
                item.OwnerSlotId = free[i];

                if (online != null)
                    PublishInboxDelivery(online, item);
            }

            if (online != null)
                CommunicatorManager.Instance.SystemMessage(online,
                    $"A game master sent you {(stacks == 1 ? "an item" : $"{stacks} items")}. Collect {(stacks == 1 ? "it" : "them")} from the Pick Up Items tab at any auctioneer.");

            Logger.WriteLog(LogType.Command, $"{from ?? "The console"} mailed {quantity} x template {itemTemplateId} to {name} (character {characterId}).");

            return new Moderation.Result(true, $"Sent {quantity} x {NameOf(template)} to {name}'s inbox ({stacks} stack{(stacks == 1 ? "" : "s")}).");
        }

        private static string NameOf(ItemTemplate template) =>
            EntityClassManager.Instance.GetClassInfo(template.Class)?.ClassName ?? $"template {template.ItemTemplateId}";
    }
}
