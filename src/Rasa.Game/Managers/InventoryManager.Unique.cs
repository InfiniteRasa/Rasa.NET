using System;
using System.Collections.Generic;
using System.Linq;

namespace Rasa.Managers
{
    using Data;
    using Game;
    using Packets.Communicator.Server;
    using Repositories.Char;
    using Structures;

    /// <summary>
    /// Character Unique ("Item is unique per character", itemtemplate.has_character_unique_flag):
    /// a character holds at most one item of such a template. A stack is one item, so a unique
    /// stackable is one stack of any size, and an item of a template already held is refused
    /// rather than merged into it.
    ///
    /// What a character holds is HoldsTemplate. Every way an item comes to a character asks it
    /// first, through MayReceive, Receivable or HoldsTemplateStored, and refuses with
    /// PmItemCharacterUnique, the client's own message:
    ///  - a vendor's item, bought or bought back;
    ///  - an item off a corpse, taken alone or with Loot All (a held one, or a second of one
    ///    template, stays on the corpse; walking over it says nothing), and a squad roll, which
    ///    a member holding one is not in;
    ///  - a trade, as an item is offered and again as it completes, counting what each side is
    ///    giving away;
    ///  - an auction bought out;
    ///  - a fabrication, refused before its ingredients go while one is held or being made, and
    ///    any finished job, which stays on the station while one is held;
    ///  - a harvest;
    ///  - a mission's reward: a chosen item refuses the turn-in, so another can be chosen, and a
    ///    fixed one is left out of the reward;
    ///  - the clan lockbox, taken out or swapped out, and the footlocker the same: the footlocker
    ///    is the account's, so another character may have put a second one there;
    ///  - a clan feud's forfeit wager, which stays with its owner rather than reach the inbox of a
    ///    recipient who holds one, logged in or not;
    ///  - .giveitem, and .mailitem, for a character logged in or not.
    /// Not checked: an item a mission issues and takes back again (MissionItemPlanner), which is
    /// the mission's; an inbox item taken into the pack, which was checked on its way in; and an
    /// auction cancelled or run out, as an item listed is still held.
    /// </summary>
    public partial class InventoryManager
    {
        /// <summary>Whether the template is Character Unique.</summary>
        public static bool IsCharacterUnique(ItemTemplate template) => template?.HasCharacterUniqueFlag == true;

        /// <summary>Tells the player "Item is unique per character".</summary>
        public static void TellCharacterUnique(Client client) =>
            client?.CallMethod(SysEntity.CommunicatorId,
                new DisplayClientMessagePacket(PlayerMessage.PmItemCharacterUnique, new Dictionary<string, string>(), MsgFilterId.GeneralSystemMessages));

        /// <summary>
        /// Whether the character may take these items without holding two of a Character Unique
        /// template: none of one they hold - other than the items in <paramref name="leaving"/>,
        /// which go as these come - and no two of one among them.
        /// </summary>
        public bool MayReceive(Manifestation player, IEnumerable<ItemTemplate> incoming, ICollection<ulong> leaving = null)
        {
            var seen = new HashSet<uint>();

            foreach (var template in incoming ?? Enumerable.Empty<ItemTemplate>())
            {
                if (!IsCharacterUnique(template))
                    continue;

                if (!seen.Add(template.ItemTemplateId) || HoldsTemplate(player, template.ItemTemplateId, leaving))
                    return false;
            }

            return true;
        }

        /// <summary>Whether the character may take this item (MayReceive).</summary>
        public bool MayReceive(Manifestation player, ItemTemplate template, ICollection<ulong> leaving = null) =>
            MayReceive(player, new[] { template }, leaving);

        /// <summary>
        /// Those of these items the character may take, in order: one of a Character Unique
        /// template they hold, or of one taken earlier in the list, is left out, and
        /// <paramref name="refused"/> says whether any was.
        /// </summary>
        public List<T> Receivable<T>(Manifestation player, IEnumerable<T> items, Func<T, ItemTemplate> templateOf, out bool refused)
        {
            var taken = new HashSet<uint>();
            var result = new List<T>();

            refused = false;

            foreach (var item in items ?? Enumerable.Empty<T>())
            {
                var template = templateOf(item);

                if (IsCharacterUnique(template) &&
                    (!taken.Add(template.ItemTemplateId) || HoldsTemplate(player, template.ItemTemplateId)))
                {
                    refused = true;
                    continue;
                }

                result.Add(item);
            }

            return result;
        }

        /// <summary>
        /// HoldsTemplate from the database, for a character who may not be logged in: an item of
        /// the template in a row of theirs that HoldsTemplate counts, or in the account's
        /// footlocker unbound or bound to them.
        /// </summary>
        public static bool HoldsTemplateStored(ICharUnitOfWork unitOfWork, uint accountId, uint characterId, uint itemTemplateId)
        {
            var held = new[]
            {
                InventoryType.Personal, InventoryType.EquipedInventory, InventoryType.WeaponDrawerInventory,
                InventoryType.InboxInventory, InventoryType.WagerInventory, InventoryType.AuctionInventory
            }.Select(type => (uint)type).ToHashSet();

            foreach (var row in unitOfWork.CharacterInventories.GetItems(accountId))
            {
                var footlocker = row.InventoryType == (uint)InventoryType.HomeInventory;

                if (!footlocker && (row.CharacterId != characterId || !held.Contains(row.InventoryType)))
                    continue;

                var item = unitOfWork.Items.GetItem(row.ItemId);

                if (item == null || item.ItemTemplateId != itemTemplateId)
                    continue;

                if (!footlocker || item.BoundCharacterId == 0 || item.BoundCharacterId == characterId)
                    return true;
            }

            return false;
        }
    }
}
