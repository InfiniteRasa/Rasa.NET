using System;
using System.Collections.Generic;
using System.Linq;

namespace Rasa.Managers
{
    using Data;
    using Game;
    using Packets.Communicator.Server;
    using Packets.Inventory.Client;
    using Packets.Inventory.Server;
    using Structures;
    using Structures.Char;

    /// <summary>
    /// Item wagering: the wager slot of the prestige window (client/ui/prestigewindow.py,
    /// client/prestige.py). One item, a character_inventory row of type WagerInventory in slot 0,
    /// kept out of the backpack for a bonus on the prestige a kill generates (PvpPrestige).
    ///
    /// The client's, from its messages, its help (missiontext 20780) and gameconstants.py:
    ///  - WagerItem(slot) swaps the wager slot with a backpack slot: the prestige window sends the
    ///    slot of the item dropped on it, the backpack window the slot the wagered item was dropped
    ///    on, or None. RemoveWageredItem() is the right click: back to the backpack;
    ///  - what may be wagered: equipment from the backpack, one of a kind (not a stack), tradable,
    ///    not bound, not broken, of Uncommon, Rare or Epic quality - the client's Modified,
    ///    Experimental and Prototype, worth +20%, +35% and +50% (quality.wageringPrestigeBonus) -
    ///    and with a level requirement within ITEM_WAGERING_LEVEL_RANGE (4) of the character's
    ///    level. Not while in combat;
    ///  - "Once wagered, the item will be locked upon entering combat": the lock is game effect
    ///    LOCK_WAGERED_ITEM_EFFECT, whose client class puts the padlock on the slot. "Once locked,
    ///    the item cannot be removed unless it is replaced with a higher quality item or falls
    ///    outside of the level range of your character": a locked item goes back only in exchange
    ///    for one of a higher quality, or of the same quality and the same or a higher level;
    ///  - a wagered item cannot be removed "while you are engaging in PvP" (Pvp.IsEngaged);
    ///  - a character who leaves the level range is told (WageredItemOutLeveled), the bonus stops
    ///    and the item is free again.
    ///
    /// Ours:
    ///  - the lock is kept on the character (character.wager_locked), so a relog is no way out of
    ///    it, and the effect goes on again at every arrival and revive. It is told to its holder's
    ///    client alone (GameEffect.OwnerOnly): the client class shows the padlock on whichever
    ///    client hears of the effect;
    ///  - an item that replaces a locked one is locked in its place;
    ///  - "an item that is wagered will be lost to your opponent if you are defeated": the defeat
    ///    is the clan's. When a clan feud is won, every item wagered by a member of the losing clan,
    ///    in the world or not, locked or not, goes to the winning clan's lockbox, to its lowest free
    ///    unlocked slot. With the lockbox full it goes to the inbox of the character who made the
    ///    challenge, if their clan won, or of the one who accepted it - or of the winning clan's
    ///    highest-ranking member when that character is not in the clan any more or the feud was
    ///    not started by a challenge. With no room there either the item stays where it is. A tie
    ///    and a cancelled feud forfeit nothing (<see cref="ForfeitWagers"/>);
    ///  - a clan that disbands while at feud has lost its feuds (ClanFeuds.ClanDisbanded): its
    ///    members' wagered items go to the clan of the oldest of them;
    ///  - leaving the clan, or being kicked from it, while it is at feud saves nothing: whoever has
    ///    an item wagered as they go stays at stake in the clan's feuds (ClanFeuds.MemberRemoved),
    ///    and what is in their slot when a feud ends in the clan's defeat is forfeit with the rest;
    ///  - what was wagered in a PvP match is on the match's record (PvpRecords, the
    ///    pvp_match_wager table): the item each of its players has in the slot when it ends
    ///    (<see cref="WageredItemsOf"/>), and for the losers of a feud what became of it
    ///    (<see cref="WagerForfeits.Items"/>). A duel, a squad wargame and a battleground's
    ///    match take nobody's item: theirs are all on record as kept.
    /// </summary>
    public partial class InventoryManager
    {
        /// <summary>gameeffectdata.LOCK_WAGERED_ITEM_EFFECT.</summary>
        public const int LockWageredItemEffectTypeId = 10000087;

        /// <summary>ITEM_WAGERING_LEVEL_RANGE.</summary>
        public const int WagerLevelRange = 4;

        /// <summary>The one slot of the wager inventory.</summary>
        public const uint WagerSlot = 0;

        #region The numbers

        /// <summary>quality.wageringPrestigeBonus: the percent an item of this quality adds; 0 for one that cannot be wagered.</summary>
        public static int WagerBonusPercent(int qualityId) => (LootQuality)qualityId switch
        {
            LootQuality.Uncommon => 20,
            LootQuality.Rare => 35,
            LootQuality.Epic => 50,
            _ => 0
        };

        /// <summary>The level an item asks for (Item.GetReqLevel); 0 when it asks for none.</summary>
        public static int WagerLevelOf(Item item)
        {
            var requirements = item?.ItemTemplate?.ItemInfo?.Requirements;

            return requirements != null && requirements.TryGetValue(RequirementsType.ReqXpLevel, out var level) ? level : 0;
        }

        /// <summary>prestige.CheckWageredItemLevel: whether the item's level is within the range of a character of this level.</summary>
        public static bool InWagerRange(Item item, int characterLevel) => Math.Abs(WagerLevelOf(item) - characterLevel) <= WagerLevelRange;

        /// <summary>The item in the player's wager slot, or null.</summary>
        public static Item WageredItemOf(Manifestation player)
        {
            var entityId = player?.Inventory?.WagerItem ?? 0;

            return entityId == 0 ? null : EntityManager.Instance.GetItem(entityId);
        }

        /// <summary>The percent the player's wagered item adds to the prestige a kill generates: its quality's, while the player is within its level range.</summary>
        public static int WagerBonusOf(Manifestation player)
        {
            var item = WageredItemOf(player);

            if (item?.ItemTemplate == null || !InWagerRange(item, player.Level))
                return 0;

            return WagerBonusPercent(item.ItemTemplate.QualityId);
        }

        /// <summary>Whether the wagered item is held in its slot: locked, and still within the level range.</summary>
        public static bool IsWagerLocked(Manifestation player)
        {
            var item = WageredItemOf(player);

            return item != null && player.WagerLocked && InWagerRange(item, player.Level);
        }

        #endregion

        #region Client requests

        /// <summary>WagerItem(slot): the wager slot swapped with that backpack slot.</summary>
        public void WagerItem(Client client, WagerItemPacket packet)
        {
            var player = client?.Player;

            if (player == null || client.AccountEntry == null || packet == null)
                return;

            var personal = player.Inventory.PersonalInventory;

            if (packet.Slot is uint slot && slot < personal.Count && personal[(int)slot] != 0)
                Wager(client, slot);
            else
                Unwager(client, packet.Slot);
        }

        /// <summary>RemoveWageredItem(): the wagered item back to the backpack, wherever it fits.</summary>
        public void RemoveWageredItem(Client client)
        {
            if (client?.Player == null || client.AccountEntry == null)
                return;

            Unwager(client, null);
        }

        private void Wager(Client client, uint slot)
        {
            var player = client.Player;
            var inventory = player.Inventory;
            var item = EntityManager.Instance.GetItem(inventory.PersonalInventory[(int)slot]);

            if (item?.ItemTemplate == null)
                return;

            if (player.State == CharacterState.Dead)
            {
                WagerSay(client, PlayerMessage.PmCannotDoThatWhileDead);
                return;
            }

            if (player.InCombat)
            {
                WagerSay(client, PlayerMessage.PmWagerItemCanNotWagerInCombat);
                return;
            }

            if (WagerRefusal(player, item) is PlayerMessage refusal)
            {
                if (refusal == PlayerMessage.PmWagerItemInvalidLevel)
                    WagerSay(client, refusal, ("level", WagerLevelRange.ToString()));
                else
                    WagerSay(client, refusal);

                return;
            }

            var wagered = WageredItemOf(player);

            // A slot naming an entity that is gone holds nothing.
            if (wagered == null)
                inventory.WagerItem = 0;

            if (wagered != null && IsWagerLocked(player))
            {
                var held = WagerBonusPercent(wagered.ItemTemplate.QualityId);
                var offered = WagerBonusPercent(item.ItemTemplate.QualityId);

                if (offered < held)
                {
                    WagerSay(client, PlayerMessage.PmWagerItemCanNotBeReplacedByLowerQuality);
                    return;
                }

                if (offered == held && WagerLevelOf(item) < WagerLevelOf(wagered))
                {
                    WagerSay(client, PlayerMessage.PmWagerItemCanNotBeReplacedByLowerLevel);
                    return;
                }
            }

            using (var unitOfWork = _gameUnitOfWorkFactory.CreateChar())
            {
                // As AddItemBySlot checks before a move: the row is found by item id alone.
                if (!unitOfWork.CharacterInventories.IsHeldBy(item.Id, client.AccountEntry.Id, player.Id))
                {
                    Logger.WriteLog(LogType.Security,
                        $"AccountId = {client.AccountEntry.Id} ({player.FamilyName}) wagered item {item.Id} (entity {item.EntityId}), but its inventory row is not theirs; refused.");
                    return;
                }

                unitOfWork.ExecuteTransaction(() =>
                {
                    if (wagered != null)
                        unitOfWork.CharacterInventories.MoveInvItem(client.AccountEntry.Id, player.Id, (uint)InventoryType.Personal, slot, wagered.Id);

                    unitOfWork.CharacterInventories.MoveInvItem(client.AccountEntry.Id, player.Id, (uint)InventoryType.WagerInventory, WagerSlot, item.Id);
                });
            }

            // Out of the backpack, and the one it replaces into the slot it left.
            inventory.PersonalInventory[(int)slot] = 0;
            client.CallMethod(SysEntity.ClientInventoryManagerId, new InventoryRemoveItemPacket(InventoryType.Personal, item.EntityId));

            if (wagered != null)
            {
                client.CallMethod(SysEntity.ClientInventoryManagerId, new RemoveWagerItemPacket(wagered.EntityId));

                wagered.OwnerId = player.Id;
                wagered.OwnerSlotId = slot;
                inventory.PersonalInventory[(int)slot] = wagered.EntityId;
                client.CallMethod(SysEntity.ClientInventoryManagerId, new InventoryAddItemPacket(InventoryType.Personal, wagered.EntityId, slot));
            }

            item.OwnerId = player.Id;
            item.OwnerSlotId = WagerSlot;
            inventory.WagerItem = item.EntityId;
            client.CallMethod(SysEntity.ClientInventoryManagerId, new AddWagerItemPacket(item.EntityId));

            // A lock left from an item the character has out-levelled does not pass to the new one.
            if (player.WagerLocked && (wagered == null || !InWagerRange(wagered, player.Level)))
                SetWagerLocked(client, false);

            Logger.WriteLog(LogType.Debug, $"Wager: {player.FamilyName} ({player.Id}) wagered item {item.Id}{(wagered != null ? $" in place of {wagered.Id}" : "")}.");
        }

        private void Unwager(Client client, uint? slot)
        {
            var player = client.Player;
            var inventory = player.Inventory;
            var wagered = WageredItemOf(player);

            if (wagered?.ItemTemplate == null)
            {
                inventory.WagerItem = 0;
                return;
            }

            if (player.State == CharacterState.Dead)
            {
                WagerSay(client, PlayerMessage.PmCannotDoThatWhileDead);
                return;
            }

            if (IsWagerLocked(player))
            {
                WagerSay(client, PlayerMessage.PmWagerItemCanNotBeRemovedInCombat);
                return;
            }

            if (Pvp.IsEngaged(player))
            {
                WagerSay(client, PlayerMessage.PmWagerItemCanNotBeRemovedDuringWarGame);
                return;
            }

            // The slot asked for when it is free and one of the item's own tab, else the first free one there.
            var start = ((int)wagered.ItemTemplate.InventoryCategory - 1) * PersonalCategorySize;

            if (start < 0 || start + PersonalCategorySize > inventory.PersonalInventory.Count)
                return;

            var destination = slot is uint asked && asked >= start && asked < start + PersonalCategorySize && inventory.PersonalInventory[(int)asked] == 0
                ? (int)asked
                : Enumerable.Range(start, PersonalCategorySize).Cast<int?>().FirstOrDefault(index => inventory.PersonalInventory[index.Value] == 0) ?? -1;

            if (destination < 0)
            {
                WagerSay(client, PlayerMessage.PmInventoryFull);
                return;
            }

            using (var unitOfWork = _gameUnitOfWorkFactory.CreateChar())
                unitOfWork.CharacterInventories.MoveInvItem(client.AccountEntry.Id, player.Id, (uint)InventoryType.Personal, (uint)destination, wagered.Id);

            inventory.WagerItem = 0;
            client.CallMethod(SysEntity.ClientInventoryManagerId, new RemoveWagerItemPacket(wagered.EntityId));

            wagered.OwnerId = player.Id;
            wagered.OwnerSlotId = (uint)destination;
            inventory.PersonalInventory[destination] = wagered.EntityId;
            client.CallMethod(SysEntity.ClientInventoryManagerId, new InventoryAddItemPacket(InventoryType.Personal, wagered.EntityId, (uint)destination));

            if (player.WagerLocked)
                SetWagerLocked(client, false);

            Logger.WriteLog(LogType.Debug, $"Wager: {player.FamilyName} ({player.Id}) took back item {wagered.Id}.");
        }

        /// <summary>Why this backpack item may not be wagered by this player, as the client's message for it; null when it may.</summary>
        private PlayerMessage? WagerRefusal(Manifestation player, Item item)
        {
            var template = item.ItemTemplate;

            if (template.InventoryCategory != InventoryCategory.Equipment || template.EquipableInfo == null && template.WeaponInfo == null)
                return PlayerMessage.PmWagerItemCanNotWagerNonEquipable;

            var stackSize = EntityClassManager.Instance.GetClassInfo(template.Class)?.ItemClassInfo?.StackSize ?? 1;

            if (stackSize > 1 || item.StackSize > 1)
                return PlayerMessage.PmWagerItemCanNotWagerStackable;

            if (IsProtected(item))
                return PlayerMessage.PmWagerItemCanNotBeWagered;

            if (template.NotTradable)
                return PlayerMessage.PmWagerItemCanNotWagerNonTradable;

            if (item.IsBound)
                return PlayerMessage.PmWagerItemCanNotWagerCharacterBound;

            if (Durability.IsBroken(item))
                return PlayerMessage.PmWagerItemNeedsRepair;

            if (WagerBonusPercent(template.QualityId) == 0)
                return PlayerMessage.PmWagerItemInvalidQuality;

            if (!InWagerRange(item, player.Level))
                return PlayerMessage.PmWagerItemInvalidLevel;

            return null;
        }

        private static void WagerSay(Client client, PlayerMessage message, params (string Key, string Value)[] args)
        {
            client?.CallMethod(SysEntity.CommunicatorId,
                new DisplayClientMessagePacket(message, args.ToDictionary(a => a.Key, a => a.Value), MsgFilterId.GeneralSystemMessages));
        }

        #endregion

        #region The lock

        /// <summary>
        /// The player has entered combat (ManifestationManager.EnterCombat): a wagered item that
        /// gives its bonus is locked in its slot from here on.
        /// </summary>
        public static void WagerEnteredCombat(Client client)
        {
            var player = client?.Player;

            if (player?.Inventory == null || player.Inventory.WagerItem == 0 || player.WagerLocked)
                return;

            var item = WageredItemOf(player);

            if (item == null || !InWagerRange(item, player.Level))
                return;

            Instance.SetWagerLocked(client, true);
        }

        /// <summary>
        /// The player's level has changed: one that has left the wagered item's range is told the
        /// item gives no bonus any more, and the item is theirs to take back.
        /// </summary>
        public static void WagerLevelChanged(Client client, int previousLevel)
        {
            var player = client?.Player;

            if (player?.Inventory == null || player.Inventory.WagerItem == 0)
                return;

            var item = WageredItemOf(player);

            if (item == null || InWagerRange(item, player.Level))
                return;

            if (InWagerRange(item, previousLevel))
                client.CallMethod(SysEntity.ClientPrestigeSystemId, new WageredItemOutLeveledPacket());

            if (player.WagerLocked)
                Instance.SetWagerLocked(client, false);
        }

        /// <summary>Locks or frees the wager slot: kept on the character, and the padlock shown or taken off.</summary>
        internal void SetWagerLocked(Client client, bool locked)
        {
            var player = client?.Player;

            if (player == null)
                return;

            player.WagerLocked = locked;

            try
            {
                using var unitOfWork = _gameUnitOfWorkFactory.CreateChar();
                unitOfWork.Characters.UpdateWagerLocked(player.Id, locked);
            }
            catch (Exception e)
            {
                Logger.WriteLog(LogType.Error, $"Wager: the lock of {player.FamilyName} ({player.Id}) could not be saved: {e.Message}");
            }

            SyncWagerLock(player);
        }

        /// <summary>
        /// The lock effect on the player as their wager slot says: on while a living player's item
        /// is locked, off otherwise. Called wherever either changes, and on every arrival and
        /// revive - a map change and a death take every effect off.
        /// </summary>
        public static void SyncWagerLock(Manifestation player)
        {
            var mapChannel = player?.MapChannel;

            if (mapChannel == null)
                return;

            var current = player.ActiveEffects.Values.FirstOrDefault(e => e.TypeId == LockWageredItemEffectTypeId);
            var wanted = player.State != CharacterState.Dead && IsWagerLocked(player);

            if (wanted && current == null)
                GameEffectManager.Instance.Attach(mapChannel, player, new GameEffect
                {
                    TypeId = LockWageredItemEffectTypeId,
                    EffectId = GameEffectManager.Instance.NextEffectId(mapChannel),
                    EffectLevel = 1,
                    SourceId = player.EntityId,
                    Source = player,
                    SourceLevel = player.Level,
                    IsBuff = true,
                    OwnerOnly = true,
                    AnnounceOnAttach = true,
                    ShowsDuration = false
                });
            else if (!wanted && current != null)
                GameEffectManager.Instance.DettachEffect(mapChannel, player, current);
        }

        #endregion

        #region Showing the slot

        /// <summary>
        /// The wager slot to the player's client afresh: ResetWagerInventory, then AddWagerItem for
        /// the item in it. As with the inbox (<see cref="ShowInbox"/>), the client forgets its
        /// wagered item only on the way back to the login screen and drops the item's entity with
        /// the rest of a map. <paramref name="sendItemData"/> is false where the caller has just
        /// sent the item's data itself (the login load).
        /// </summary>
        internal void ShowWager(Client client, bool sendItemData)
        {
            var inventory = client?.Player?.Inventory;

            if (inventory == null)
                return;

            var item = WageredItemOf(client.Player);

            if (item == null)
                inventory.WagerItem = 0;

            client.CallMethod(SysEntity.ClientInventoryManagerId, new ResetWagerInventoryPacket());

            if (item == null)
                return;

            if (sendItemData)
                ItemManager.Instance.SendItemDataToClient(client, item, false);

            client.CallMethod(SysEntity.ClientInventoryManagerId, new AddWagerItemPacket(item.EntityId));
        }

        #endregion

        #region Forfeits

        /// <summary>Whether the character has an item in their wager slot: the loaded player's when they are in the world, else their row.</summary>
        public bool HoldsWager(uint characterId)
        {
            if (characterId == 0)
                return false;

            var player = EntityManager.Instance.Players.Values.FirstOrDefault(p => p.Id == characterId)
                         ?? ConnectedPlayers().FirstOrDefault(c => c.Player.Id == characterId)?.Player;

            if (WageredItemOf(player) != null)
                return true;

            if (_gameUnitOfWorkFactory == null)
                return false;

            using var unitOfWork = _gameUnitOfWorkFactory.CreateChar();

            return unitOfWork.CharacterInventories.GetByType(new[] { characterId }, (uint)InventoryType.WagerInventory).Count > 0;
        }

        /// <summary>
        /// The items these characters have wagered now, for a PvP match's record (PvpRecords): a
        /// row for each who has one, with the item, its template, quality and stack. The slot of
        /// a player who is loaded is read first; the rest are read from their rows.
        /// </summary>
        public List<PvpMatchWagerEntry> WageredItemsOf(IReadOnlyCollection<uint> characterIds)
        {
            var result = new List<PvpMatchWagerEntry>();
            var wanted = (characterIds ?? Array.Empty<uint>()).Where(id => id != 0).Distinct().ToList();

            if (wanted.Count == 0)
                return result;

            // Whoever of them is loaded with an item in the slot; a character can be loaded more than once, the one that holds it counts.
            var unread = wanted.ToHashSet();

            foreach (var player in ConnectedPlayers().Select(c => c.Player).Concat(EntityManager.Instance.Players.Values))
            {
                if (player == null || !unread.Contains(player.Id))
                    continue;

                var item = WageredItemOf(player);

                if (item == null)
                    continue;

                unread.Remove(player.Id);
                result.Add(new PvpMatchWagerEntry
                {
                    CharacterId = player.Id,
                    ItemId = item.Id,
                    ItemTemplateId = item.ItemTemplate?.ItemTemplateId ?? item.ItemTemplateId,
                    QualityId = item.ItemTemplate?.QualityId ?? 0,
                    StackSize = item.StackSize
                });
            }

            if (unread.Count == 0 || _gameUnitOfWorkFactory == null)
                return result;

            using var unitOfWork = _gameUnitOfWorkFactory.CreateChar();

            foreach (var row in unitOfWork.CharacterInventories.GetByType(unread, (uint)InventoryType.WagerInventory))
            {
                if (!unread.Remove(row.CharacterId))
                    continue;

                var itemData = unitOfWork.Items.GetItem(row.ItemId);
                var template = itemData == null ? null : ItemManager.Instance.GetItemTemplateById(itemData.ItemTemplateId);

                result.Add(new PvpMatchWagerEntry
                {
                    CharacterId = row.CharacterId,
                    ItemId = row.ItemId,
                    ItemTemplateId = itemData?.ItemTemplateId ?? 0,
                    QualityId = template?.QualityId ?? 0,
                    StackSize = itemData?.StackSize ?? 0
                });
            }

            return result;
        }

        /// <summary>What became of the wagered items of a clan that lost a feud.</summary>
        public sealed class WagerForfeits
        {
            /// <summary>Put in the winning clan's lockbox.</summary>
            public int ToLockbox { get; set; }

            /// <summary>Sent to the inbox of <see cref="RecipientCharacterId"/>, the lockbox being full.</summary>
            public int Mailed { get; set; }

            /// <summary>Left with their owners: no room in the lockbox or the inbox.</summary>
            public int Kept { get; set; }

            /// <summary>Who the mailed ones went to; 0 when nobody could be found.</summary>
            public uint RecipientCharacterId { get; set; }

            public int Taken => ToLockbox + Mailed;

            /// <summary>
            /// Item by item, for the feud's record (PvpRecords): whose it was, which item, and
            /// what became of it. The side is the caller's to say.
            /// </summary>
            public List<PvpMatchWagerEntry> Items { get; } = new List<PvpMatchWagerEntry>();
        }

        /// <summary>
        /// A clan feud is won (ClanFeuds.End): the items wagered by the members of the losing clan,
        /// and by <paramref name="departed"/> - those who left it while the feud ran - go to the
        /// winning clan's lockbox, or to the inbox of <paramref name="recipientCharacterId"/>
        /// - the winning clan's highest-ranking member when that character is not one of its
        /// members - when the lockbox has no free unlocked slot. Each item moves in one
        /// transaction; one that cannot be moved stays with its owner.
        /// </summary>
        public WagerForfeits ForfeitWagers(uint loserClanId, uint winnerClanId, uint recipientCharacterId, string loserClanName, string winnerClanName,
            IEnumerable<uint> departed = null)
        {
            var result = new WagerForfeits();

            if (loserClanId == 0 || winnerClanId == 0 || loserClanId == winnerClanId || _gameUnitOfWorkFactory == null)
                return result;

            using var unitOfWork = _gameUnitOfWorkFactory.CreateChar();

            var members = unitOfWork.ClanMembers.GetAllClanMembersByClanId(loserClanId).Select(m => m.CharacterId).ToHashSet();
            var losers = members.Concat(departed ?? Enumerable.Empty<uint>()).Where(id => id != 0).Distinct().ToList();
            var rows = unitOfWork.CharacterInventories.GetByType(losers, (uint)InventoryType.WagerInventory);

            if (rows.Count == 0)
                return result;

            var winners = unitOfWork.ClanMembers.GetAllClanMembersByClanId(winnerClanId);
            var recipient = winners.Any(m => m.CharacterId == recipientCharacterId)
                ? recipientCharacterId
                : winners.OrderByDescending(m => m.Rank).ThenBy(m => m.CharacterId).Select(m => m.CharacterId).FirstOrDefault();
            var recipientEntry = recipient != 0 ? unitOfWork.Characters.Find(recipient) : null;

            result.RecipientCharacterId = recipientEntry != null ? recipient : 0;

            var unlocked = ClanLockboxTab.UnlockedSlots(unitOfWork.Clans.GetClanById(winnerClanId)?.PurashedTabs ?? ClanLockboxTab.FreeTab);
            var used = unitOfWork.ClanInventories.GetItems(winnerClanId).Select(row => row.SlotId).ToHashSet();
            var clients = ConnectedPlayers();
            var winnersOnline = clients.Where(c => c.Player.ClanId == winnerClanId && c.State == ClientState.Ingame
                                                   && c.Player.Inventory.ClanInventory.Count == ClanLockboxTab.TotalSlots).ToList();
            var touchedLockbox = false;

            foreach (var row in rows)
            {
                // Forfeit to the winners whatever comes of it; where it went is filled in as it goes.
                var wager = new PvpMatchWagerEntry
                {
                    CharacterId = row.CharacterId,
                    ItemId = row.ItemId,
                    Result = (byte)PvpWagerResult.NotMoved,
                    RecipientClanId = winnerClanId
                };

                result.Items.Add(wager);

                try
                {
                    var itemData = unitOfWork.Items.GetItem(row.ItemId);
                    var template = itemData == null ? null : ItemManager.Instance.GetItemTemplateById(itemData.ItemTemplateId);

                    wager.ItemTemplateId = itemData?.ItemTemplateId ?? 0;
                    wager.QualityId = template?.QualityId ?? 0;
                    wager.StackSize = itemData?.StackSize ?? 0;

                    if (template == null)
                    {
                        Logger.WriteLog(LogType.Error, $"Wager: character {row.CharacterId}'s wagered item {row.ItemId} has no item row or template; left.");
                        result.Kept++;
                        continue;
                    }

                    // The owner's own copy of it, if they are in the world.
                    var owner = EntityManager.Instance.Players.Values.FirstOrDefault(p => p.Id == row.CharacterId)
                                ?? clients.FirstOrDefault(c => c.Player.Id == row.CharacterId)?.Player;
                    var ownerClient = owner == null ? null : clients.FirstOrDefault(c => ReferenceEquals(c.Player, owner));
                    var item = WageredItemOf(owner);

                    if (item != null && item.Id != row.ItemId)
                        item = null;

                    uint? lockboxSlot = null;

                    for (uint slot = 0; slot < unlocked; slot++)
                        if (!used.Contains(slot))
                        {
                            lockboxSlot = slot;
                            break;
                        }

                    if (lockboxSlot is uint bankSlot)
                    {
                        unitOfWork.ExecuteTransaction(() =>
                        {
                            unitOfWork.CharacterInventories.DeleteInvItemByItemId(row.ItemId);
                            unitOfWork.ClanInventories.AddInvItem(winnerClanId, bankSlot, row.ItemId);

                            // AddInvItem logs a failed insert and carries on; the item must not lose its only row.
                            if (!unitOfWork.ClanInventories.GetItems(winnerClanId).Any(stored => stored.ItemId == row.ItemId && stored.SlotId == bankSlot))
                                throw new InvalidOperationException("the lockbox row was not written");

                            unitOfWork.Characters.UpdateWagerLocked(row.CharacterId, false);
                        });

                        used.Add(bankSlot);
                        wager.Result = (byte)PvpWagerResult.ClanLockbox;

                        // A lockbox item is an entity for as long as the server runs (ClanManager.InitCurrentClanInventories).
                        if (item != null)
                            TakeWager(owner, ownerClient, item);
                        else
                            item = CreateLoadedItem(itemData, template, 0, bankSlot);

                        item.OwnerId = 0;
                        item.OwnerSlotId = bankSlot;

                        foreach (var member in winnersOnline)
                        {
                            ItemManager.Instance.SendItemDataToClient(member, item, false);
                            member.Player.Inventory.ClanInventory[(int)bankSlot] = item.EntityId;
                            member.CallMethod(SysEntity.ClientInventoryManagerId, new InventoryAddItemPacket(InventoryType.ClanInventory, item.EntityId, bankSlot));
                        }

                        var ownerEntry = unitOfWork.Characters.Find(row.CharacterId);
                        var stored = unitOfWork.ClanLockboxLogs.Add(ClanLockboxLogEntry.ForItem(winnerClanId, InventoryTransactionType.Deposit, row.CharacterId,
                            ownerEntry?.Name ?? "", ownerEntry == null ? "" : unitOfWork.GameAccounts.Find(ownerEntry.AccountId)?.FamilyName ?? "",
                            template.ItemTemplateId, Math.Max(1u, itemData.StackSize)));

                        // On each lockbox's entity, the only place the client handles it (ToClanLockboxes).
                        if (stored != null)
                        {
                            var logged = Packets.Clan.Server.ClanLockboxLogsPacket.Update(new List<ClanLockboxLogEntry> { stored });

                            foreach (var lockbox in ClanLockboxes())
                                foreach (var member in winnersOnline)
                                    member.CallMethod(lockbox.EntityId, logged);
                        }

                        touchedLockbox = true;
                        result.ToLockbox++;
                    }
                    else
                    {
                        var moved = false;
                        uint inboxSlot = 0;

                        // Character Unique: not to a recipient who holds one, logged in or not;
                        // it stays with its owner, as one that does not fit does.
                        if (recipientEntry != null && IsCharacterUnique(template))
                        {
                            var online = clients.FirstOrDefault(c => c.Player.Id == recipient)?.Player;
                            var holds = online != null
                                ? HoldsTemplate(online, template.ItemTemplateId)
                                : HoldsTemplateStored(unitOfWork, recipientEntry.AccountId, recipient, template.ItemTemplateId);

                            if (holds)
                            {
                                Logger.WriteLog(LogType.Debug, $"Wager: item {row.ItemId} is Character Unique and character {recipient} holds one; left with its owner.");
                                result.Kept++;
                                continue;
                            }
                        }

                        if (recipientEntry != null)
                            unitOfWork.ExecuteTransaction(() =>
                            {
                                moved = TryMoveToInbox(unitOfWork, recipientEntry.AccountId, recipient, row.ItemId, out inboxSlot);

                                if (moved)
                                    unitOfWork.Characters.UpdateWagerLocked(row.CharacterId, false);
                            });

                        if (!moved)
                        {
                            result.Kept++;
                            continue;
                        }

                        wager.Result = (byte)PvpWagerResult.PickUpBox;
                        wager.RecipientCharacterId = recipient;

                        var recipientClient = clients.FirstOrDefault(c => c.Player.Id == recipient && c.State == ClientState.Ingame);

                        if (item != null)
                            TakeWager(owner, ownerClient, item);

                        if (recipientClient != null)
                        {
                            item ??= CreateLoadedItem(itemData, template, recipient, inboxSlot);
                            item.OwnerId = recipient;
                            item.OwnerSlotId = inboxSlot;
                            PublishInboxDelivery(recipientClient, item);
                        }
                        else if (item != null)
                        {
                            // Rows only: it becomes an entity again when the recipient next loads.
                            if (ownerClient != null)
                                EntityManager.Instance.DestroyPhysicalEntity(ownerClient, item.EntityId, EntityType.Item);
                            else
                                EntityManager.Instance.ReleaseEntity(item.EntityId, EntityType.Item);
                        }

                        result.Mailed++;
                    }

                    // Off the loser; to the recipient when it was mailed, and to the clan - no
                    // character - when it went to the lockbox (which has its own log as well).
                    var stack = (int)Math.Max(1u, itemData.StackSize);
                    var mailedTo = wager.Result == (byte)PvpWagerResult.PickUpBox ? recipient : 0;
                    EconomyAudit.Instance.Record(EconomyLogKind.Wager, winnerClanId,
                        EconomyAudit.ItemLine(row.CharacterId, ownerClient?.AccountEntry?.Id ?? 0, row.ItemId, template.ItemTemplateId,
                            -stack, mailedTo, owner?.MapContextId ?? 0),
                        mailedTo == 0 ? null : EconomyAudit.ItemLine(recipient, recipientEntry.AccountId, row.ItemId, template.ItemTemplateId,
                            stack, row.CharacterId));

                    if (ownerClient != null && ownerClient.State == ClientState.Ingame)
                        CommunicatorManager.Instance.SystemMessage(ownerClient, members.Contains(row.CharacterId)
                            ? $"Your clan lost its feud with {winnerClanName}: your wagered item is forfeit to them."
                            : $"{loserClanName}, which you left while it was at feud, lost to {winnerClanName}: your wagered item is forfeit to them.");
                }
                catch (Exception e)
                {
                    Logger.WriteLog(LogType.Error, $"Wager: forfeiting item {row.ItemId} of character {row.CharacterId} failed: {e.Message}");
                    result.Kept++;
                }
            }

            if (touchedLockbox)
                foreach (var member in winnersOnline)
                    member.CallMethod(SysEntity.ClientInventoryManagerId,
                        new InventoryReloadPacket(InventoryType.ClanInventory, member.Player.Inventory.ClanInventory, 500));

            if (result.Taken > 0)
            {
                var holder = recipientEntry?.Name ?? "a clan member";
                var text = $"Your clan won its feud with {loserClanName}: {Count(result.Taken, "wagered item")} forfeited by its members "
                           + (result.Mailed == 0
                               ? "went to the clan lockbox."
                               : result.ToLockbox == 0
                                   ? $"could not fit the clan lockbox and went to {holder}'s pick-up box."
                                   : $"went to the clan lockbox ({result.ToLockbox}) and, with the lockbox full, to {holder}'s pick-up box ({result.Mailed}).");

                foreach (var member in clients.Where(c => c.Player.ClanId == winnerClanId && c.State == ClientState.Ingame))
                    CommunicatorManager.Instance.SystemMessage(member, text);

                if (result.Mailed > 0)
                {
                    var recipientClient = clients.FirstOrDefault(c => c.Player.Id == recipient && c.State == ClientState.Ingame);

                    if (recipientClient != null)
                        CommunicatorManager.Instance.SystemMessage(recipientClient,
                            $"The clan lockbox is full: {Count(result.Mailed, "forfeited item")} went to your pick-up box. Collect {(result.Mailed == 1 ? "it" : "them")} from the Pick Up Items tab at any auctioneer.");
                }
            }

            Logger.WriteLog(LogType.Debug,
                $"Wager: clan {loserClanId} forfeited to clan {winnerClanId}: {result.ToLockbox} to the lockbox, {result.Mailed} mailed to character {result.RecipientCharacterId}, {result.Kept} kept.");

            return result;
        }

        private static string Count(int count, string noun) => $"{count} {noun}{(count == 1 ? "" : "s")}";

        /// <summary>The wagered item out of a loaded player's slot, the lock with it; their client told when there is one.</summary>
        private static void TakeWager(Manifestation owner, Client ownerClient, Item item)
        {
            owner.Inventory.WagerItem = 0;
            owner.WagerLocked = false;

            ownerClient?.CallMethod(SysEntity.ClientInventoryManagerId, new RemoveWagerItemPacket(item.EntityId));

            SyncWagerLock(owner);
        }

        /// <summary>Every connection with a character loaded.</summary>
        private static List<Client> ConnectedPlayers()
        {
            lock (Server.Clients)
                return Server.Clients.Where(c => c?.Player != null && c.Player.Id != 0).ToList();
        }

        #endregion
    }
}
