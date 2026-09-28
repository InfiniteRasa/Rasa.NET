using System;
using System.Collections.Generic;
using System.Linq;

namespace Rasa.Managers
{
    using Data;
    using Memory;
    using Packets;
    using Packets.Inventory.Server;
    using Packets.MapChannel.Server;
    using Rasa.Game;
    using Rasa.Packets.MapChannel.Client;
    using Repositories.Char;
    using Repositories.UnitOfWork;
    using Structures;
    using Structures.Char;

    /// <summary>
    /// The auction house: listing items, browsing and buying them out, showing the seller their
    /// own auctions, taking a listing back down, and returning what ran out. There is no
    /// bidding - the client offers a buyout price and nothing else.
    ///
    /// A listed item keeps its items row and its character_inventory row - the row's type just
    /// becomes AuctionInventory - so the item survives a restart exactly as a lockbox item does,
    /// and its entity is created on the client at login by the usual inventory load. The auction
    /// table holds only what the auction adds on top: seller, price, deposit and start time.
    /// </summary>
    public class AuctionHouseManager
    {
        /*    AuctionHouse Packets:
         *  - AuctionCreationFailed     => done
         *  - AuctionCreationSuccess    => done
         *  - QuerySuccess             => done
         *  - QueryFailed              => done
         *  - AuctionStatusSuccess      => done
         *  - AuctionStatusFailed       => not used by client
         *  - AuctionBuyoutFailed      => done
         *  - AuctionBuyoutSuccess     => done
         *  - AuctionSold              => done
         *  - CancelAuctionFailed       => done
         *  - CancelAuctionSuccess      => done
         *  - AuctionExpired           => done
         *
         *  AuctionHouse Handlers:
         *  - RequestAuctionBuyout      => done
         *  - RequestAuctionStatus      => done
         *  - RequestCancelAuction      => done
         *  - RequestCancelAuctioneer   => done
         *  - RequestCreateAuction      => done
         *  - RequestQueryAuctions      => done
         *
         */

        private static AuctionHouseManager _instance;
        private static readonly object InstanceLock = new object();
        private static readonly object AuctionSyncRoot = new object();

        private readonly IGameUnitOfWorkFactory _gameUnitOfWorkFactory;
        private readonly ManifestationManager _currencyManager;
        private readonly MissionApplication _missionManager;
        private readonly Action<PythonPacket> _beforeBuyoutPublication;

        private sealed class BuyoutRejection : Exception
        {
            internal PlayerMessage FailureMessage { get; }

            internal BuyoutRejection(PlayerMessage message)
            {
                FailureMessage = message;
            }
        }

        private sealed class BuyoutResult
        {
            internal PlayerMessage? FailureMessage { get; init; }
            internal AuctionEntry Auction { get; init; }
            internal Client Seller { get; init; }
            internal int BuyerCredits { get; init; }
            internal int SellerCredits { get; init; }
            internal uint InboxSlot { get; init; }
            internal MissionProgressPublicationPlan ProgressPlan { get; init; }
        }

        private sealed class CancelResult
        {
            internal PlayerMessage? FailureMessage { get; init; }
            internal Item Item { get; init; }
            internal uint Slot { get; init; }
        }

        private sealed class ExpiryResult
        {
            internal Item Item { get; init; }
            internal uint SellerId { get; init; }
            internal uint InboxSlot { get; init; }
        }

        private sealed class ExpiryDeferred : Exception
        {
        }

        /// <summary>
        /// Deposit charged to list an item, in tenths of a percent, by the duration the seller
        /// picked. These are the client's own figures: it reads them from auctionhouse.durationdata
        /// and shows the result in the Deposit field before the player presses Create, so the
        /// server has to charge the same or the window lies. 12h costs 0.5%, three days 2.5%.
        /// </summary>
        private static readonly uint[] DepositTenthsOfPercent = { 5, 10, 20, 25 };

        /// <summary>Hours each duration id runs for: 12 hours, then one, two and three days.</summary>
        private static readonly uint[] DurationHours = { 12, 24, 48, 72 };

        /// <summary>How many rows may fail in a row before a sweep is taken to be pointless.</summary>
        private const int MaxConsecutiveExpiryFailures = 5;

        public static AuctionHouseManager Instance
        {
            get
            {
                // ReSharper disable once InvertIf
                if (_instance == null)
                {
                    lock (InstanceLock)
                    {
                        if (_instance == null)
                            _instance = new AuctionHouseManager(Server.GameUnitOfWorkFactory);
                    }
                }

                return _instance;
            }
        }

        private AuctionHouseManager(IGameUnitOfWorkFactory gameUnitOfWorkFactory)
            : this(gameUnitOfWorkFactory, null)
        {
        }

        internal AuctionHouseManager(
            IGameUnitOfWorkFactory gameUnitOfWorkFactory,
            MissionApplication missionManager,
            Action<PythonPacket> beforeBuyoutPublication = null)
        {
            _gameUnitOfWorkFactory = gameUnitOfWorkFactory;
            _currencyManager = new ManifestationManager(gameUnitOfWorkFactory);
            _missionManager = missionManager;
            _beforeBuyoutPublication = beforeBuyoutPublication;
        }

        #region Handlers

        /// <summary>
        /// Buys an auction outright. The item goes to the buyer's inbox, not their pack: the
        /// client has a Pick Up Items tab for exactly this, and it is the only delivery that
        /// works when the pack is full. The seller is paid into their character row, so the
        /// money reaches them whether or not they are logged in.
        /// </summary>
        public void RequestAuctionBuyout(Client client, RequestAuctionBuyoutPacket packet)
        {
            var item = EntityManager.Instance.GetItem(packet.ItemId);

            if (item == null)
            {
                BuyoutFailed(client, packet.ItemId, PlayerMessage.PmAuctionItemNotFound);
                return;
            }

            BuyoutResult result;
            lock (AuctionSyncRoot)
                result = ConsumeBuyoutLocked(client, packet, item);

            if (result.FailureMessage.HasValue)
            {
                BuyoutFailed(client, packet.ItemId, result.FailureMessage.Value);
                return;
            }

            client.Player.Credits[CurencyType.Credits] = result.BuyerCredits;
            if (result.Seller != null)
                result.Seller.Player.Credits[CurencyType.Credits] =
                    result.SellerCredits;

            item.OwnerId = client.Player.Id;
            item.OwnerSlotId = result.InboxSlot;
            client.Player.Inventory.InboxItems.Add(item.EntityId);
            var seller = OnlineSeller(result.Auction.SellerId);
            seller?.Player.Inventory.AuctionItems.Remove(item.EntityId);

            PublishBuyoutPacket(
                client,
                client.Player.EntityId,
                new UpdateCreditsPacket(
                    CurencyType.Credits,
                    result.BuyerCredits,
                    0),
                $"auction item {item.Id} buyer credits");
            if (result.Seller != null)
                MissionApplication.TryPublish(
                    () => result.Seller.CallMethod(
                        result.Seller.Player.EntityId,
                        new UpdateCreditsPacket(
                            CurencyType.Credits,
                            result.SellerCredits,
                            0)),
                    $"auction item {item.Id} seller credits");
            MissionApplication.TryPublish(
                () => ItemManager.Instance.SendItemDataToClient(
                    client,
                    item,
                    false),
                $"auction item {item.Id} entity data");
            PublishBuyoutPacket(
                client,
                (ulong)SysEntity.ClientInventoryManagerId,
                new AddInboxItemPacket(item.EntityId),
                $"auction item {item.Id} inbox delivery");
            if (seller != null)
            {
                MissionApplication.TryPublish(
                    () => seller.CallMethod(
                        SysEntity.ClientInventoryManagerId,
                        new RemoveAuctionItemPacket(item.EntityId)),
                    $"auction item {item.Id} seller inventory removal");
                MissionApplication.TryPublish(
                    () => seller.CallMethod(
                        SysEntity.ClientAuctionHouseManagerId,
                        new AuctionSoldPacket(
                            item.EntityId,
                            result.Auction.Price)),
                    $"auction item {item.Id} sold result");
            }
            PublishBuyoutPacket(
                client,
                (ulong)SysEntity.ClientAuctionHouseManagerId,
                new AuctionBuyoutSuccessPacket(item.EntityId),
                $"auction item {item.Id} buyout result");
            result.ProgressPlan.Publish(client);
        }

        private BuyoutResult ConsumeBuyoutLocked(
            Client client,
            RequestAuctionBuyoutPacket packet,
            Item item)
        {
            AuctionEntry auction = null;
            Client seller = null;
            var buyerAfter = 0;
            var sellerAfter = 0;
            var inboxSlot = 0u;
            var progressPlan =
                MissionProgressPublicationPlan.Empty;

            try
            {
                using var unitOfWork = _gameUnitOfWorkFactory.CreateChar();
                unitOfWork.ExecuteTransaction(() =>
                {
                    auction = unitOfWork.Auctions.GetAuctionByItemId(item.Id);
                    if (auction == null)
                        throw new BuyoutRejection(
                            PlayerMessage.PmAuctionItemNotFound);

                    if (auction.SellerId == client.Player.Id)
                        throw new BuyoutRejection(
                            PlayerMessage.PmAuctionCannotPurchaseOwnItem);

                    if (packet.Price != auction.Price)
                        throw new BuyoutRejection(
                            PlayerMessage.PmAuctionPendingTransaction);

                    var buyer = unitOfWork.Characters.Find(client.Player.Id);
                    var durableItem =
                        unitOfWork.CharacterInventories.FindByItemId(item.Id);
                    if (buyer == null ||
                        buyer.AccountId != client.AccountEntry.Id ||
                        !client.Player.Credits.TryGetValue(
                            CurencyType.Credits, out var runtimeBuyerCredits) ||
                        buyer.Credit != runtimeBuyerCredits ||
                        durableItem == null ||
                        durableItem.CharacterId != auction.SellerId ||
                        durableItem.InventoryType !=
                        (uint)InventoryType.AuctionInventory ||
                        item.OwnerId != auction.SellerId)
                        throw new BuyoutRejection(
                            PlayerMessage.PmAuctionPendingTransaction);

                    if (buyer.Credit < auction.Price)
                        throw new BuyoutRejection(
                            PlayerMessage.PmAuctionInsufficientFunds);

                    var durableSeller =
                        unitOfWork.Characters.Find(auction.SellerId);
                    if (durableSeller == null)
                        throw new BuyoutRejection(
                            PlayerMessage.PmAuctionPendingTransaction);

                    seller = OnlineSeller(auction.SellerId);
                    if (seller != null &&
                        (!seller.Player.Credits.TryGetValue(
                             CurencyType.Credits, out var runtimeSellerCredits) ||
                         runtimeSellerCredits != durableSeller.Credit))
                        throw new BuyoutRejection(
                            PlayerMessage.PmAuctionPendingTransaction);

                    buyerAfter = checked(buyer.Credit - (int)auction.Price);
                    sellerAfter = checked(
                        durableSeller.Credit + (int)auction.Price);

                    if (!unitOfWork.Auctions.DeleteAuction(item.Id))
                        throw new BuyoutRejection(
                            PlayerMessage.PmAuctionPendingTransaction);

                    unitOfWork.Characters.UpdateCharacterCredits(
                        buyer.Id, buyerAfter);
                    unitOfWork.Characters.UpdateCharacterCredits(
                        durableSeller.Id, sellerAfter);

                    if (!InventoryManager.Instance.TryMoveToInbox(
                            unitOfWork,
                            buyer.AccountId,
                            buyer.Id,
                            item.Id,
                            out inboxSlot))
                        throw new BuyoutRejection(
                            PlayerMessage.PmAuctionNoBuyoutInboxFull);

                    progressPlan = (_missionManager ?? MissionApplication.Instance)
                        .PlanProgress(
                            client,
                            new[]
                            {
                                MissionProgressEvent.ItemAcquired(
                                    (uint)item.ItemTemplate.Class,
                                    item.StackSize)
                            },
                            unitOfWork);
                });
            }
            catch (BuyoutRejection rejection)
            {
                return new BuyoutResult
                {
                    FailureMessage = rejection.FailureMessage
                };
            }
            catch (Exception error) when (
                GameplayRejectionException.IsExpected(error) ||
                error is OverflowException ||
                error is System.Data.Common.DbException ||
                error is Microsoft.EntityFrameworkCore.DbUpdateException)
            {
                Logger.WriteLog(LogType.Error,
                    $"Auction buyout for item {item.Id} failed atomically: {error.Message}");
                return new BuyoutResult
                {
                    FailureMessage = PlayerMessage.PmAuctionPendingTransaction
                };
            }

            return new BuyoutResult
            {
                Auction = auction,
                Seller = seller,
                BuyerCredits = buyerAfter,
                SellerCredits = sellerAfter,
                InboxSlot = inboxSlot,
                ProgressPlan = progressPlan
            };
        }

        private void PublishBuyoutPacket(
            Client client,
            ulong entityId,
            PythonPacket packet,
            string description)
        {
            MissionApplication.TryPublish(
                () =>
                {
                    _beforeBuyoutPublication?.Invoke(packet);
                    client.CallMethod(entityId, packet);
                },
                description);
        }

        /// <summary>
        /// Fills the "My Auctions" tab. The client asks for this every time the tab is opened,
        /// and replaces its whole auction dictionary with what comes back.
        /// </summary>
        public void RequestAuctionStatus(Client client, RequestAuctionStatusPacket packet)
        {
            using var unitOfWork = _gameUnitOfWorkFactory.CreateChar();
            var auctions = unitOfWork.Auctions.GetAuctionsBySeller(client.Player.Id);
            var now = DateTime.UtcNow;
            var rows = new List<AuctionStatus>();

            foreach (var auction in auctions)
            {
                var entityId = FindAuctionedEntityId(client, auction.ItemId);

                if (entityId == 0)
                {
                    // The client looks the row up by entity id; one it has never been told about
                    // renders as a blank line it cannot cancel. Better to leave it out and say so.
                    Logger.WriteLog(LogType.Error, $"Auction for item {auction.ItemId} has no entity on character {client.Player.Id}; not listed.");
                    continue;
                }

                rows.Add(new AuctionStatus(entityId, auction.Price, auction.RemainingHours(now)));
            }

            client.CallMethod(SysEntity.ClientAuctionHouseManagerId, new AuctionStatusSuccessPacket(rows));
        }

        /// <summary>Takes a listing down and puts the item back in the seller's pack. The deposit is not refunded.</summary>
        public void RequestCancelAuction(Client client, RequestCancelAuctionPacket packet)
        {
            CancelResult result;
            lock (AuctionSyncRoot)
                result = ConsumeCancellationLocked(client, packet.ItemEntityId);

            if (result.FailureMessage.HasValue)
            {
                FailCancel(client, packet.ItemEntityId, result.FailureMessage.Value);
                return;
            }

            client.Player.Inventory.AuctionItems.Remove(packet.ItemEntityId);
            client.CallMethod(SysEntity.ClientInventoryManagerId,
                new RemoveAuctionItemPacket(packet.ItemEntityId));

            result.Item.OwnerId = client.Player.Id;
            result.Item.OwnerSlotId = result.Slot;
            client.Player.Inventory.PersonalInventory[(int)result.Slot] =
                result.Item.EntityId;
            client.CallMethod(SysEntity.ClientInventoryManagerId,
                new InventoryAddItemPacket(
                    InventoryType.Personal, result.Item.EntityId, result.Slot));
            client.CallMethod(SysEntity.ClientAuctionHouseManagerId,
                new CancelAuctionSuccessPacket(packet.ItemEntityId));
        }

        private CancelResult ConsumeCancellationLocked(
            Client client,
            ulong itemEntityId)
        {
            var item = EntityManager.Instance.GetItem(itemEntityId);

            if (item == null ||
                !client.Player.Inventory.AuctionItems.Contains(itemEntityId))
                return new CancelResult
                {
                    FailureMessage = PlayerMessage.PmAuctionItemNotFound
                };

            var slotId = FindFreePersonalSlot(client, item);
            if (slotId < 0)
            {
                Logger.WriteLog(LogType.Debug, $"Character {client.Player.Id} cancelled an auction with no free inventory slot for the item.");
                return new CancelResult
                {
                    FailureMessage = PlayerMessage.PmAuctionInternalError
                };
            }

            try
            {
                using var unitOfWork = _gameUnitOfWorkFactory.CreateChar();
                unitOfWork.ExecuteTransaction(() =>
                {
                    var auction = unitOfWork.Auctions.GetAuctionByItemId(item.Id);
                    var durableItem =
                        unitOfWork.CharacterInventories.FindByItemId(item.Id);
                    if (auction == null ||
                        auction.SellerId != client.Player.Id ||
                        durableItem == null ||
                        durableItem.AccountId != client.AccountEntry.Id ||
                        durableItem.CharacterId != client.Player.Id ||
                        durableItem.InventoryType !=
                        (uint)InventoryType.AuctionInventory ||
                        item.OwnerId != client.Player.Id)
                        throw new BuyoutRejection(
                            PlayerMessage.PmAuctionItemNotFound);

                    if (!unitOfWork.Auctions.DeleteAuction(item.Id))
                        throw new BuyoutRejection(
                            PlayerMessage.PmAuctionPendingTransaction);

                    unitOfWork.CharacterInventories.MoveInvItem(
                        client.AccountEntry.Id,
                        client.Player.Id,
                        (uint)InventoryType.Personal,
                        (uint)slotId,
                        item.Id);
                });
            }
            catch (BuyoutRejection rejection)
            {
                return new CancelResult
                {
                    FailureMessage = rejection.FailureMessage
                };
            }
            catch (Exception error) when (
                error is System.Data.Common.DbException ||
                error is Microsoft.EntityFrameworkCore.DbUpdateException)
            {
                Logger.WriteLog(LogType.Error,
                    $"Auction cancellation for item {item.Id} failed atomically: {error.Message}");
                return new CancelResult
                {
                    FailureMessage = PlayerMessage.PmAuctionPendingTransaction
                };
            }

            return new CancelResult
            {
                Item = item,
                Slot = (uint)slotId
            };
        }

        /// <summary>
        /// The client sends this when the auction window closes. The server keeps no auction
        /// house session - every request carries the auctioneer it came from - so there is
        /// nothing to tear down, and answering it at all would be wrong: the client has already
        /// forgotten the auctioneer by the time it sends this.
        /// </summary>
        public void RequestCancelAuctioneer(Client client)
        {
            Logger.WriteLog(LogType.Debug, $"Character {client.Player.Id} closed the auction house.");
        }

        public void RequestCreateAuction(Client client, RequestCreateAuctionPacket packet)
        {
            var item = EntityManager.Instance.GetItem(packet.ItemEntityId);
            var slotId = FindPersonalSlotOf(client, packet.ItemEntityId);

            if (item == null || slotId < 0)
            {
                Fail(client, packet.ItemEntityId, PlayerMessage.PmAuctionCouldNotFindItem);
                return;
            }

            if (packet.Price == 0)
            {
                Fail(client, packet.ItemEntityId, PlayerMessage.PmAuctionInvalidPriceSet);
                return;
            }

            if (packet.Duration >= DurationHours.Length)
            {
                // Not a duration the window offers, so the deposit the player was shown is not
                // one this server can reproduce.
                Fail(client, packet.ItemEntityId, PlayerMessage.PmAuctionInvalidPriceSet);
                Logger.WriteLog(LogType.Error, $"Character {client.Player.Id} asked for auction duration {packet.Duration}, which does not exist.");
                return;
            }

            using var unitOfWork = _gameUnitOfWorkFactory.CreateChar();
            if (!CanBeAuctioned(item) || Game.Missions.Persistence.MissionItemProtection.IsProtected(item, unitOfWork))
            {
                Fail(client, packet.ItemEntityId, PlayerMessage.PmAuctionItemCannotBeAuctioned);
                return;
            }

            var itemClassInfo = EntityClassManager.Instance.GetItemClassInfo(item);

            if (itemClassInfo != null && item.CurrentHitPoints < itemClassInfo.MaxHitPoints)
            {
                Fail(client, packet.ItemEntityId, PlayerMessage.PmAuctionItemNeedsRepair);
                return;
            }

            if (client.Player.Inventory.AuctionItems.Count >= Inventory.MaxAuctionItems)
            {
                Fail(client, packet.ItemEntityId, PlayerMessage.PmAuctionMaxAuctions);
                return;
            }

            var deposit = CalculateDeposit(item, packet.Price, packet.Duration);

            if (client.Player.Credits[CurencyType.Credits] < deposit)
            {
                Fail(client, packet.ItemEntityId, PlayerMessage.PmAuctionNotEnoughCreditsForDeposit);
                return;
            }

            if (!_currencyManager.LossCredits(client, (int)deposit))
            {
                Fail(client, packet.ItemEntityId, PlayerMessage.PmAuctionNotEnoughCreditsForDeposit);
                return;
            }

            var auction = new AuctionEntry(item.Id, client.Player.Id, client.Player.Name, packet.Price,
                deposit, DurationHours[packet.Duration]);

            // Written before anything is charged or moved: if the item is somehow already listed,
            // the player keeps their credits and their item.
            if (!unitOfWork.Auctions.CreateAuction(auction))
            {
                if (!_currencyManager.GainCredits(client, (int)deposit))
                    Logger.WriteLog(LogType.Error,
                        $"Auction listing for item {item.Id} could not refund character {client.Player.Id}.");
                Fail(client, packet.ItemEntityId, PlayerMessage.PmAuctionInternalError);
                return;
            }

            // Out of the pack, into the auction inventory. RemoveItemBySlot does no database work
            // of its own, so the row is moved here.
            InventoryManager.Instance.RemoveItemBySlot(client, InventoryType.Personal, (uint)slotId);

            var auctionSlot = NextAuctionSlot(client);
            client.Player.Inventory.AuctionItems.Add(item.EntityId);
            item.OwnerSlotId = auctionSlot;

            unitOfWork.CharacterInventories.MoveInvItem(client.AccountEntry.Id, client.Player.Id,
                (uint)InventoryType.AuctionInventory, auctionSlot, item.Id);

            client.CallMethod(SysEntity.ClientInventoryManagerId, new AddAuctionItemPacket(item.EntityId));
            client.CallMethod(SysEntity.ClientAuctionHouseManagerId, new AuctionCreationSuccessPacket(item.EntityId));
        }

        /// <summary>
        /// The browse tab's search. Every query carries a category - the client keeps its search
        /// button disabled until one is picked - plus a level range and one exact quality, since
        /// its rarity box has no "any" entry.
        /// </summary>
        public void RequestQueryAuctions(Client client, RequestQueryAuctionsPacket packet)
        {
            if (!AuctionCategory.Names.TryGetValue(packet.CategoryId, out var category))
            {
                QueryFailed(client, PlayerMessage.PmAuctionNoResultsFound);
                Logger.WriteLog(LogType.Error, $"Auction search for category {packet.CategoryId}, which is not a category the client offers.");
                return;
            }

            using var unitOfWork = _gameUnitOfWorkFactory.CreateChar();
            var now = DateTime.UtcNow;
            var matches = new List<(AuctionEntry Auction, Item Item, int Level)>();

            foreach (var auction in unitOfWork.Auctions.GetAuctions())
            {
                if (auction.SellerId == client.Player.Id)
                    continue;               // the browse tab is for other people's auctions

                var item = FindAuctionedItem(auction);

                if (item?.ItemTemplate == null)
                    continue;

                if (item.ItemTemplate.QualityId != (int)packet.QualityId)
                    continue;

                var level = LevelRequirementOf(item);

                if (level < packet.MinLevel || level > packet.MaxLevel)
                    continue;

                if (!MatchesCategory(item, category))
                    continue;

                matches.Add((auction, item, level));
            }

            if (matches.Count == 0)
            {
                QueryFailed(client, PlayerMessage.PmAuctionNoResultsFound);
                return;
            }

            var reply = new QuerySuccessPacket();

            // The envelope: tuple + list header. Rows are added while they still fit.
            //
            // Every match used to go into this one reply. A reply is written into a single pool
            // block, so past about a hundred rows the write threw inside Send, LengthedSocket
            // logged that it was skipping the packet, and the searcher was left looking at an
            // empty browse tab - the busier the category, the more certainly it returned nothing.
            var size = PythonSize.Of(pw => reply.Write(pw));

            // Cheapest first, because that is the half of a full category a buyer wants, and
            // because an item priced out of the page today comes into it as the ones under it
            // sell. By auction id - which is the order they arrive in - the newest listings would
            // be the ones nobody could ever see.
            foreach (var match in matches.OrderBy(m => m.Auction.Price))
            {
                var modules = match.Item.ItemTemplate.ItemInfo?.ModuleIds ?? new List<int>();

                var row = new AuctionItem
                {
                    ItemId = (uint)match.Item.EntityId,
                    Sellername = match.Auction.SellerName,
                    BidPrice = 0,
                    BuyoutPrice = match.Auction.Price,
                    RemainingDuration = match.Auction.RemainingHours(now),
                    ItemTemplateId = match.Item.ItemTemplate.ItemTemplateId,
                    StackSize = match.Item.StackSize,
                    LootModuleId1 = modules.Count > 0 ? (uint)modules[0] : 0,
                    LootModuleId2 = modules.Count > 1 ? (uint)modules[1] : 0,
                    LootModuleId3 = modules.Count > 2 ? (uint)modules[2] : 0,
                    LootModuleId4 = modules.Count > 3 ? (uint)modules[3] : 0,
                    QualitiId = (uint)match.Item.ItemTemplate.QualityId,
                    LevelRequirement = (uint)match.Level
                };

                var rowSize = PythonSize.Of(row);

                if (size + rowSize + PythonSize.ListHeaderSlack > PythonSize.PayloadBudget)
                    break;

                size += rowSize;

                // The buyer has never seen this item, so its entity has to exist on their client
                // before the row can draw a name, an icon or a tooltip. Only for the rows that go:
                // a search of a busy category used to create an entity on the searcher's client
                // for every match, including the hundreds that were never in the reply.
                ItemManager.Instance.SendItemDataToClient(client, match.Item, false);

                reply.AuctionItemList.Add(row);
            }

            if (reply.AuctionItemList.Count < matches.Count)
                Logger.WriteLog(LogType.Debug,
                    $"Auction search by {client.Player.FamilyName} matched {matches.Count} auctions in {category}; the {reply.AuctionItemList.Count} cheapest fit the reply.");

            client.CallMethod(SysEntity.ClientAuctionHouseManagerId, reply);
        }

        /// <summary>
        /// Returns every auction past its duration to the seller's inbox. Called on a timer and
        /// again when a character logs in, which is what catches auctions that ran out while the
        /// server was down.
        /// </summary>
        public void ExpireAuctions()
        {
            List<AuctionEntry> auctions;
            using (var unitOfWork = _gameUnitOfWorkFactory.CreateChar())
                auctions = unitOfWork.Auctions.GetAuctions();

            ExpireAll(auctions);
        }

        /// <summary>Expires just this character's auctions, on their way into the world.</summary>
        public void ExpireAuctionsFor(Client client)
        {
            List<AuctionEntry> auctions;
            using (var unitOfWork = _gameUnitOfWorkFactory.CreateChar())
                auctions =
                    unitOfWork.Auctions.GetAuctionsBySeller(client.Player.Id);

            ExpireAll(auctions);
        }

        #endregion

        #region Helper Functions

        /// <summary>
        /// What listing an item costs, by the client's own formula:
        ///
        ///     minDepositPrice = max(buyoutPrice, itemValue)
        ///     deposit         = max(int(depositRatio * minDepositPrice), 1)
        ///
        /// where itemValue is the vendor buyback price times the stack size, and depositRatio is
        /// the duration's tenths of a percent over a thousand. Done in whole numbers here: the
        /// client's float arithmetic floors to the same credit, and a deposit that came out one
        /// higher than the window showed would be rejected as unaffordable by a player who had
        /// exactly enough.
        /// </summary>
        public static uint CalculateDeposit(Item item, uint price, uint durationId)
        {
            if (durationId >= DepositTenthsOfPercent.Length)
                return 0;

            var stackSize = Math.Max(item.StackSize, 1u);
            var itemValue = (uint)Math.Max(item.ItemTemplate.SellPrice, 0) * stackSize;
            var basis = Math.Max(price, itemValue);
            var deposit = (ulong)basis * DepositTenthsOfPercent[durationId] / 1000;

            return deposit < 1 ? 1 : (uint)Math.Min(deposit, uint.MaxValue);
        }

        /// <summary>
        /// The same three rules the create window filters the item list by, so the server agrees
        /// with what the player was offered.
        ///
        /// Note ItemInfo.Tradable is misnamed: ItemManager fills it from the template's
        /// NotTradableFlag, and ItemInfoPacket writes it into the client's notTradable field, so
        /// the two inversions cancel on the wire and true here means *not* tradable.
        /// </summary>
        public static bool CanBeAuctioned(Item item)
        {
            if (item?.ItemTemplate == null || item.MissionOwnership != null)
                return false;

            if (item.ItemTemplate.BoundToCharacter)
                return false;

            if (!item.ItemTemplate.HasSellableFlag)
                return false;

            return item.ItemTemplate.ItemInfo == null || !item.ItemTemplate.ItemInfo.Tradable;
        }

        /// <summary>
        /// Expires everything in a list that has run out, with what one row throws kept to that
        /// row.
        ///
        /// A sweep walks auctions in one loop and the timer that drives it only catches at the
        /// top (Timer.ReportFault), so an exception from any one row used to abandon the rest of
        /// the pass. Rows come back oldest first, so the same row was reached at the same point
        /// of every sweep, and everything listed after it stopped expiring for as long as the
        /// server ran.
        ///
        /// Everything expiry can expect - a seller who is gone, an item that is gone, a full
        /// inbox - is answered in Expire without throwing, so a throw here is something
        /// unforeseen. A few in a row are taken to be the database rather than the rows, and the
        /// pass gives up instead of writing a line per auction; the next one is five minutes
        /// away.
        /// </summary>
        private void ExpireAll(List<AuctionEntry> auctions)
        {
            var now = DateTime.UtcNow;
            var consecutiveFailures = 0;

            foreach (var auction in auctions)
            {
                if (auction.RemainingHours(now) > 0)
                    continue;

                try
                {
                    ExpiryResult result;
                    lock (AuctionSyncRoot)
                        result = ConsumeExpiryLocked(auction.ItemId);

                    if (result?.Item != null)
                    {
                        result.Item.OwnerId = result.SellerId;
                        result.Item.OwnerSlotId = result.InboxSlot;
                        InventoryManager.Instance.PublishInboxDelivery(
                            OnlineSeller(result.SellerId), result.Item);
                        RemoveFromSellersAuctionList(
                            result.SellerId, result.Item.EntityId, null);
                    }

                    consecutiveFailures = 0;
                }
                catch (Exception e)
                {
                    Logger.WriteLog(LogType.Error, $"Auction on item {auction.ItemId} could not be expired: {e}");

                    if (++consecutiveFailures < MaxConsecutiveExpiryFailures)
                        continue;

                    Logger.WriteLog(LogType.Error,
                        $"{consecutiveFailures} auctions in a row failed to expire; the rest of this pass is left to the next one.");

                    return;
                }
            }
        }

        /// <summary>
        /// Moves one expired auction's item back to its seller's inbox and deletes the row. An
        /// inbox that is full leaves the auction standing rather than destroying the item - it
        /// is simply retried on the next sweep, once the seller has made room.
        ///
        /// Every way this can fail to return the item is answered here rather than thrown, so
        /// that one auction cannot take the sweep down with it.
        /// </summary>
        private ExpiryResult ConsumeExpiryLocked(uint itemId)
        {
            ExpiryResult result = null;
            try
            {
                using var unitOfWork = _gameUnitOfWorkFactory.CreateChar();
                unitOfWork.ExecuteTransaction(() =>
                {
                    var auction = unitOfWork.Auctions.GetAuctionByItemId(itemId);
                    if (auction == null ||
                        auction.RemainingHours(DateTime.UtcNow) > 0)
                        return;

                    var accountId = AccountOf(auction.SellerId, unitOfWork);

                    // Asked before the item is looked for, because this is the case where there
                    // is nothing to look for it on behalf of. A character deleted with auctions
                    // running leaves the rows behind, and an item cannot be returned to account
                    // zero. Consuming the listing is the only safe terminal transition.
                    if (accountId == null)
                    {
                        Logger.WriteLog(LogType.Error,
                            $"Auction on item {auction.ItemId} has expired but its seller {auction.SellerName} ({auction.SellerId}) no longer exists; the listing is removed.");

                        if (!unitOfWork.Auctions.DeleteAuction(auction.ItemId))
                            throw new ExpiryDeferred();
                        return;
                    }

                    var item = FindAuctionedItem(auction);
                    var durableItem =
                        unitOfWork.CharacterInventories.FindByItemId(itemId);
                    if (item == null ||
                        durableItem == null ||
                        durableItem.AccountId != accountId.Value ||
                        durableItem.CharacterId != auction.SellerId ||
                        durableItem.InventoryType !=
                        (uint)InventoryType.AuctionInventory ||
                        item.OwnerId != auction.SellerId)
                    {
                        Logger.WriteLog(LogType.Error,
                            $"Auction on item {auction.ItemId} has expired but its item or auction ownership is gone; row left in place.");
                        return;
                    }

                    if (!unitOfWork.Auctions.DeleteAuction(auction.ItemId))
                        throw new ExpiryDeferred();

                    if (!InventoryManager.Instance.TryMoveToInbox(
                            unitOfWork,
                            accountId.Value,
                            auction.SellerId,
                            item.Id,
                            out var inboxSlot))
                    {
                        Logger.WriteLog(LogType.Debug,
                            $"Auction on item {auction.ItemId} has expired but {auction.SellerName}'s inbox is full; it will be retried.");
                        throw new ExpiryDeferred();
                    }

                    result = new ExpiryResult
                    {
                        Item = item,
                        SellerId = auction.SellerId,
                        InboxSlot = inboxSlot
                    };
                });
            }
            catch (ExpiryDeferred)
            {
                return null;
            }

            return result;
        }

        /// <summary>
        /// Takes the item out of the seller's own auction list and tells their client, so a
        /// seller who is standing at an auction house sees the listing go. A price means it
        /// sold; no price means it ran out.
        /// </summary>
        private static void RemoveFromSellersAuctionList(uint sellerId, ulong entityId, uint? soldFor)
        {
            var seller = OnlineSeller(sellerId);

            if (seller == null)
                return;

            seller.Player.Inventory.AuctionItems.Remove(entityId);
            seller.CallMethod(SysEntity.ClientInventoryManagerId, new RemoveAuctionItemPacket(entityId));
            seller.CallMethod(SysEntity.ClientAuctionHouseManagerId, soldFor.HasValue
                ? (Rasa.Packets.PythonPacket)new AuctionSoldPacket(entityId, soldFor.Value)
                : new AuctionExpiredPacket(entityId));
        }

        private static Client OnlineSeller(uint sellerId) =>
            Server.Clients.Find(c => c?.Player != null && c.Player.Id == sellerId && c.State == ClientState.Ingame);

        /// <summary>
        /// The live Item behind an auction row. A seller who is logged in has it in their
        /// auction list; otherwise it is whichever registered item carries that database id.
        /// </summary>
        private static Item FindAuctionedItem(AuctionEntry auction)
        {
            var seller = OnlineSeller(auction.SellerId);

            if (seller != null)
                foreach (var entityId in seller.Player.Inventory.AuctionItems)
                {
                    var held = EntityManager.Instance.GetItem(entityId);

                    if (held != null && held.Id == auction.ItemId)
                        return held;
                }

            foreach (var entry in EntityManager.Instance.Items)
                if (entry.Value != null && entry.Value.Id == auction.ItemId)
                    return entry.Value;

            return null;
        }

        /// <summary>
        /// Which account the seller's rows belong to, or null when the seller's character is
        /// gone. Their character_inventory rows are keyed by account, and an offline seller has
        /// no Client to read it from.
        ///
        /// Characters.Find rather than Characters.Get: Get throws on a missing row, so the null
        /// this and PaySeller were written to return was never reached - the throw went up
        /// through the expiry sweep and the buyout handler instead.
        /// </summary>
        private static uint? AccountOf(uint sellerId, ICharUnitOfWork unitOfWork)
        {
            var seller = OnlineSeller(sellerId);

            if (seller != null)
                return seller.AccountEntry.Id;

            return unitOfWork.Characters.Find(sellerId)?.AccountId;
        }

        /// <summary>The experience level an item asks for, or zero when it asks for none.</summary>
        private static int LevelRequirementOf(Item item)
        {
            if (item.ItemTemplate?.ItemInfo?.Requirements == null)
                return 0;

            return item.ItemTemplate.ItemInfo.Requirements.TryGetValue(RequirementsType.ReqXpLevel, out var level)
                ? level
                : 0;
        }

        /// <summary>
        /// Whether an item belongs to one of the browse tab's categories. The category name is a
        /// path through the item class naming scheme, so "Weapon_Pistol" wants a class whose
        /// name starts with Weapon and carries Pistol as one of its underscore-separated words -
        /// Weapon_Avatar_Pistol_Physical_CMN_01_to_04 and not Weapon_Avatar_Rifle_Physical.
        /// </summary>
        public static bool MatchesCategory(Item item, string category)
        {
            if (string.IsNullOrEmpty(category) || item?.ItemTemplate == null)
                return false;

            if (!EntityClassManager.Instance.LoadedEntityClasses.TryGetValue(item.ItemTemplate.Class, out var entityClass))
                return false;

            var className = entityClass?.ClassName;

            if (string.IsNullOrEmpty(className))
                return false;

            var parts = category.Split('_');
            var words = className.Split('_');

            if (words.Length == 0 || !words[0].Equals(parts[0], StringComparison.OrdinalIgnoreCase))
                return false;

            for (var i = 1; i < parts.Length; i++)
                if (!words.Any(word => word.Equals(parts[i], StringComparison.OrdinalIgnoreCase)))
                    return false;

            return true;
        }

        private static void BuyoutFailed(Client client, ulong itemEntityId, PlayerMessage message)
        {
            client.CallMethod(SysEntity.ClientAuctionHouseManagerId,
                new AuctionBuyoutFailedPacket(itemEntityId, message));
        }

        private static void QueryFailed(Client client, PlayerMessage message)
        {
            client.CallMethod(SysEntity.ClientAuctionHouseManagerId, new QueryFailedPacket(message));
        }

        /// <summary>Tells the create window why it could not list the item.</summary>
        private static void Fail(Client client, ulong itemEntityId, PlayerMessage message)
        {
            client.CallMethod(SysEntity.ClientAuctionHouseManagerId,
                new AuctionCreationFailedPacket(itemEntityId, message));
        }

        /// <summary>
        /// Tells the My Auctions tab why it could not take a listing down. A separate packet from
        /// Fail: the client routes the two to different handlers, and a cancel answered with
        /// AuctionCreationFailed puts the message in the wrong window.
        /// </summary>
        private static void FailCancel(Client client, ulong itemEntityId, PlayerMessage message)
        {
            client.CallMethod(SysEntity.ClientAuctionHouseManagerId,
                new CancelAuctionFailedPacket(itemEntityId, message));
        }

        /// <summary>Which personal inventory slot an entity sits in, or -1 if it is not in the pack.</summary>
        private static int FindPersonalSlotOf(Client client, ulong entityId)
        {
            var inventory = client.Player.Inventory.PersonalInventory;

            for (var i = 0; i < inventory.Count; i++)
                if (inventory[i] == entityId)
                    return i;

            return -1;
        }

        /// <summary>
        /// A free personal slot in the item's own category block, or -1 when that block is full.
        /// Categories are fifty slots each and an item only goes in its own, the same rule
        /// AddItemToInventory follows.
        /// </summary>
        private static int FindFreePersonalSlot(Client client, Item item)
        {
            var categoryOffset = (int)item.ItemTemplate.InventoryCategory - 1;

            if (categoryOffset < 0 || categoryOffset >= 5)
                return -1;

            categoryOffset *= 50;

            var inventory = client.Player.Inventory.PersonalInventory;

            for (var i = 0; i < 50; i++)
                if (categoryOffset + i < inventory.Count && inventory[categoryOffset + i] == 0)
                    return categoryOffset + i;

            return -1;
        }

        /// <summary>
        /// The lowest slot number no auctioned item is using. Cancelling takes an item out of
        /// the middle of the list, so the next free slot is not the same thing as the count.
        /// </summary>
        private static uint NextAuctionSlot(Client client)
        {
            var used = new HashSet<uint>();

            foreach (var entityId in client.Player.Inventory.AuctionItems)
            {
                var item = EntityManager.Instance.GetItem(entityId);

                if (item != null)
                    used.Add(item.OwnerSlotId);
            }

            for (var slot = 0u; slot < Inventory.MaxAuctionItems; slot++)
                if (!used.Contains(slot))
                    return slot;

            return (uint)Inventory.MaxAuctionItems;
        }

        /// <summary>The entity id of one of this character's auctioned items, by its database id.</summary>
        private static ulong FindAuctionedEntityId(Client client, uint itemId)
        {
            foreach (var entityId in client.Player.Inventory.AuctionItems)
            {
                var item = EntityManager.Instance.GetItem(entityId);

                if (item != null && item.Id == itemId)
                    return entityId;
            }

            return 0;
        }

        #endregion
    }
}
