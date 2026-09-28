using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Rasa.Managers
{
    using Data;
    using Game;
    using Packets.Communicator.Server;
    using Packets.Inventory.Server;
    using Packets.Manifestation.Server;
    using Packets.MapChannel.Server;
    using Packets.Trade.Client;
    using Packets.Trade.Server;
    using Repositories.UnitOfWork;
    using Structures;

    /// <summary>
    /// Player-to-player trade, client/trade.py.
    ///
    ///     Client -> server (ActorMethod)
    /// - RequestTrade                        => implemented
    /// - RequestAcceptTradeRequest           => implemented
    /// - RequestCancelTrade                  => implemented
    /// - RequestChangeEnergyUnitAmount       => implemented (credits)
    /// - RequestConfirmTrade                 => implemented
    /// - RequestUnconfirmTrade               => implemented
    /// - RequestAddItemToTrade               => implemented
    /// - RequestRemoveItemFromTrade          => implemented
    ///
    ///     Server -> client (ClientTradeManagerId)
    /// - TradeInvite, TradeCreate, TradeDestroy, TradeCompleted,
    ///   TradeEnergyUnitsChange, TradeConfirmChange => implemented
    /// - TradeAddItem, TradeRemoveItem       => implemented
    /// - TradeUpdateItem                     => never sent; the client marks it DEPRECATED and
    ///   its handler draws nothing. An offer is a whole stack - RequestAddItemToTrade carries no
    ///   quantity - so there is no stack count to update.
    ///
    /// Nothing is escrowed. An offered item stays in its owner's inventory and only moves at the
    /// exchange, so a trade that is cancelled, times out or loses a player cannot strand
    /// anything. The cost is that every offer has to be re-checked at the exchange, which
    /// Complete does.
    ///
    /// All handlers and RemovePlayer run on the MainLoop, so the session table needs no lock.
    /// </summary>
    public class TradeManager
    {
        /// <summary>
        /// The client offers trade only inside distance-squared 36 (client/trade.py InTradingRange)
        /// and cancels itself when a partner leaves it. The server's copy of a position trails the
        /// client's by up to a movement update, so it allows a little more before refusing.
        /// </summary>
        /// <summary>shared/gameconstants.py DEFAULT_TRADE_INVENTORY_SIZE - five slots a side.</summary>
        private const int TradeSlots = 5;

        private const float TradeRange = 6.0f;
        private const float ServerRangeSlack = 2.0f;

        /// <summary>
        /// The client has no way to decline an invite - the pending indicator only accepts - so an
        /// ignored one would leave the target "busy" until the inviter revoked it. Unaccepted
        /// invites older than this are treated as gone the next time anyone runs into them.
        /// </summary>
        private const long PendingInviteTimeoutMs = 60 * 1000;

        private static TradeManager _instance;
        private static readonly object InstanceLock = new object();

        /// <summary>Both participants of every pending or open trade map to their session.</summary>
        private readonly Dictionary<Client, TradeSession> _sessions = new Dictionary<Client, TradeSession>();

        public static TradeManager Instance
        {
            get
            {
                // ReSharper disable once InvertIf
                if (_instance == null)
                {
                    lock (InstanceLock)
                    {
                        if (_instance == null)
                            _instance = new TradeManager();
                    }
                }

                return _instance;
            }
        }

        private TradeManager()
        {
        }

        #region Handlers

        internal void RequestTrade(Client client, RequestTradePacket packet)
        {
            if (_sessions.TryGetValue(client, out var existing))
            {
                if (existing.Accepted)
                {
                    Refuse(client, PlayerMessage.PmTradeYouAreTooBusy);
                    return;
                }

                // An unaccepted invite in either direction is superseded by a new request.
                Withdraw(existing, client);
            }

            var target = FindIngamePlayer((ulong)packet.TargetEntityId);

            if (target == null || target == client || !InRange(client, target))
            {
                Refuse(client, PlayerMessage.PmTradeYouAreTooBusy);
                return;
            }

            if (_sessions.TryGetValue(target, out var targetSession) && !targetSession.Accepted && Expired(targetSession))
                End(targetSession, notifyPartnerOf: null, message: PlayerMessage.PmTradeCancelled);

            // Busy, or ignoring the requester. Both read as "too busy" so an ignore is not revealed.
            if (_sessions.ContainsKey(target) || target.Player.IgnoredPlayers.Contains(client.AccountEntry.Id))
            {
                Refuse(client, PlayerMessage.PmTradeTheyAreTooBusy);
                return;
            }

            var session = new TradeSession(client, target);
            _sessions[client] = session;
            _sessions[target] = session;

            target.CallMethod(SysEntity.ClientTradeManagerId, new TradeInvitePacket());
        }

        internal void RequestAcceptTradeRequest(Client client, RequestAcceptTradeRequestPacket packet)
        {
            if (!_sessions.TryGetValue(client, out var session) || session.Accepted || session.IsInitiator(client))
            {
                // The invite is gone - revoked, or the inviter left. TradeDestroy is what clears the
                // indicator the player just clicked.
                client.CallMethod(SysEntity.ClientTradeManagerId, new TradeDestroyPacket());
                return;
            }

            if (Expired(session) || !StillValid(session))
            {
                End(session, notifyPartnerOf: null, message: PlayerMessage.PmTradeCancelled);
                return;
            }

            session.Accepted = true;
            session.ConfigurationId = 1;

            session.Initiator.CallMethod(SysEntity.ClientTradeManagerId,
                new TradeCreatePacket(session.Target.Player.EntityId, session.ConfigurationId, true));
            session.Target.CallMethod(SysEntity.ClientTradeManagerId,
                new TradeCreatePacket(session.Initiator.Player.EntityId, session.ConfigurationId, false));
        }

        internal void RequestCancelTrade(Client client, RequestCancelTradePacket packet)
        {
            if (!_sessions.TryGetValue(client, out var session))
                return;

            End(session, notifyPartnerOf: client);
        }

        internal void RequestChangeEnergyUnitAmount(Client client, RequestChangeEnergyUnitAmountPacket packet)
        {
            if (!TryGetOpenSession(client, out var session))
                return;

            var funds = client.Player.Credits.TryGetValue(CurencyType.Credits, out var c) ? c : 0;
            var amount = (int)Math.Clamp(packet.Amount, 0, Math.Max(funds, 0));

            if (amount == session.CreditsOf(client))
                return;

            session.SetCredits(client, amount);
            TermsChanged(session, client);

            var change = new TradeEnergyUnitsChangePacket(client.Player.EntityId, amount, session.ConfigurationId);
            session.Initiator.CallMethod(SysEntity.ClientTradeManagerId, change);
            session.Target.CallMethod(SysEntity.ClientTradeManagerId, change);
        }

        internal void RequestConfirmTrade(Client client, RequestConfirmTradePacket packet)
        {
            if (!TryGetOpenSession(client, out var session))
                return;

            if (packet.ConfigurationId != session.ConfigurationId)
            {
                // Confirmed terms that have since changed. Tell the client it is not confirmed:
                // it hid its Accept button on click and only restores it on this message.
                client.CallMethod(SysEntity.ClientTradeManagerId, new TradeConfirmChangePacket(client.Player.EntityId, false));
                return;
            }

            SetConfirmed(session, client, true);

            if (session.InitiatorConfirmed && session.TargetConfirmed)
                Complete(session);
        }

        internal void RequestUnconfirmTrade(Client client, RequestUnconfirmTradePacket packet)
        {
            if (!TryGetOpenSession(client, out var session))
                return;

            SetConfirmed(session, client, false);
        }

        internal void RequestAddItemToTrade(Client client, RequestAddItemToTradePacket packet)
        {
            if (!TryGetOpenSession(client, out var session) || !StillValid(session))
                return;

            var offered = session.ItemsOf(client);
            var entityId = (ulong)packet.ItemEntityId;

            if (offered.Any(item => item.EntityId == entityId))
                return;

            if (offered.Count >= TradeSlots)
            {
                Decline(client, PlayerMessage.PmTradeNotEnoughRoom);
                return;
            }

            var item = EntityManager.Instance.GetItem(entityId);

            // Ownership is checked against the player's own inventory rather than the item's
            // OwnerId: an equipped item has the same owner and must not be tradeable from the
            // window, and a client that names an entity id it does not hold is not making a
            // mistake.
            if (item == null || !HoldsInPersonalInventory(client, entityId))
            {
                Logger.WriteLog(LogType.Security,
                    $"AccountId = {client.AccountEntry.Id} offered item {entityId}, which is not in their inventory.");

                Decline(client, PlayerMessage.PmTradeItemCanNotBeTraded);
                return;
            }

            if (item.ItemTemplate.BoundToCharacter ||
                Game.Missions.Persistence.MissionItemProtection.IsProtected(item, Server.GameUnitOfWorkFactory))
            {
                Decline(client, PlayerMessage.PmTradeItemCanNotBeTraded);
                return;
            }

            // Taken as it stands: the stack size and condition here are what the partner is shown
            // and what Complete holds the offer to.
            offered.Add(new TradeSession.OfferedItem(item));

            // The partner has never seen this item, and their client resolves it with
            // _entitymanager.GetEntity before it draws anything (ui/tradewindow.py:162). Without
            // this the handler throws on a None entity and the trade window stops updating.
            ItemManager.Instance.SendItemDataToClient(session.PartnerOf(client), item, false);

            TermsChanged(session, client);

            var added = new TradeAddItemPacket(client.Player.EntityId, entityId, session.ConfigurationId);
            session.Initiator.CallMethod(SysEntity.ClientTradeManagerId, added);
            session.Target.CallMethod(SysEntity.ClientTradeManagerId, added);
        }

        internal void RequestRemoveItemFromTrade(Client client, RequestRemoveItemFromTradePacket packet)
        {
            if (!TryGetOpenSession(client, out var session))
                return;

            var entityId = (ulong)packet.ItemEntityId;
            var offered = session.ItemsOf(client);
            var taken = offered.FirstOrDefault(item => item.EntityId == entityId);

            if (taken == null || !offered.Remove(taken))
                return;

            TermsChanged(session, client);

            var removed = new TradeRemoveItemPacket(client.Player.EntityId, entityId, session.ConfigurationId);
            session.Initiator.CallMethod(SysEntity.ClientTradeManagerId, removed);
            session.Target.CallMethod(SysEntity.ClientTradeManagerId, removed);

            // Take the copy back off the partner's client. It was only ever sent so their trade
            // window could draw it, and leaving it behind leaves an item entity they can see in
            // no inventory.
            Unreplicate(session.PartnerOf(client), entityId);
        }

        /// <summary>
        /// Destroys, on one player's client, the copies of items the other had offered.
        /// </summary>
        private static void ReturnOfferedCopies(TradeSession session, Client viewer, List<TradeSession.OfferedItem> offeredByPartner)
        {
            if (viewer.State == ClientState.Disconnected)
                return;

            foreach (var offered in offeredByPartner)
                Unreplicate(viewer, offered.EntityId);
        }

        /// <summary>True when the entity is sitting in this player's personal inventory.</summary>
        private static bool HoldsInPersonalInventory(Client client, ulong entityId)
        {
            return client.Player.Inventory.PersonalInventory.Contains(entityId);
        }

        /// <summary>
        /// Destroys an item copy on a client that was only given it to draw a trade window.
        /// Never called on the owner: they hold the real thing.
        /// </summary>
        private static void Unreplicate(Client client, ulong entityId)
        {
            if (client.Player.Inventory.PersonalInventory.Contains(entityId))
                return;

            DestroyCopyOnClient(client, entityId);
        }

        /// <summary>
        /// Takes an item entity off one client's screen and nothing else. This used to go through
        /// EntityManager.DestroyPhysicalEntity, which is not "this client's copy": it unregisters
        /// the item from the server's tables and frees its entity id. The item still sat in its
        /// owner's inventory, so the next thing to touch that slot found no item behind the id,
        /// and the freed id could be handed to the next entity created while the slot still
        /// pointed at it. Every cancel, every removed offer and every completed hand-over did it.
        /// </summary>
        private static void DestroyCopyOnClient(Client client, ulong entityId)
        {
            client.CallMethod(SysEntity.ClientMethodId, new DestroyPhysicalEntityPacket(entityId));
        }

        #endregion

        /// <summary>
        /// Ends any trade the player is part of. Called from MapChannelManager.RemovePlayer, which
        /// covers /logout, the inactivity logout and dropped connections.
        /// </summary>
        public void RemovePlayer(Client client)
        {
            if (_sessions.TryGetValue(client, out var session))
                End(session, notifyPartnerOf: client);
        }

        private void Complete(TradeSession session)
        {
            var a = session.Initiator;
            var b = session.Target;

            // Everything is re-checked at the moment of exchange: either player may have moved,
            // spent credits elsewhere, dropped an offered item or lost their connection since
            // confirming.
            if (!StillValid(session)
                || session.InitiatorCredits > CreditsOnHand(a)
                || session.TargetCredits > CreditsOnHand(b)
                || !StillHolds(a, session.InitiatorItems)
                || !StillHolds(b, session.TargetItems))
            {
                End(session, notifyPartnerOf: null, message: PlayerMessage.PmTradeCancelled);
                return;
            }

            // Where each incoming item will land. Worked out before anything is written, because
            // a player whose inventory filled up since they confirmed is the one case that has to
            // stop the trade rather than lose an item.
            if (!TryReserveSlots(a, b, session.InitiatorItems, out var toB)
                || !TryReserveSlots(b, a, session.TargetItems, out var toA))
            {
                Decline(a, PlayerMessage.PmTradeNotEnoughRoom);
                Decline(b, PlayerMessage.PmTradeNotEnoughRoom);
                End(session, notifyPartnerOf: null, message: PlayerMessage.PmTradeCancelled);
                return;
            }

            var net = session.InitiatorCredits - session.TargetCredits;
            var initiatorCredits = CreditsOnHand(a) - net;
            var targetCredits = CreditsOnHand(b) + net;

            // One transaction for the whole exchange. Several of the repository methods call
            // SaveChanges for themselves, so without it a trade of four items and two balances
            // commits in six pieces and a failure halfway through leaves the earlier ones done -
            // which for a trade means an item or a pile of credits existing twice, or not at all.
            try
            {
                using var unitOfWork = Server.GameUnitOfWorkFactory.CreateChar();
                using var transaction = unitOfWork.BeginTransaction();

                if (toB.Concat(toA).Any(move =>
                    Game.Missions.Persistence.MissionItemProtection.IsProtected(move.Item, unitOfWork)))
                    throw new GameplayRejectionException("Assignment-owned items cannot be traded.");
                foreach (var move in toB.Concat(toA))
                    unitOfWork.CharacterInventories.MoveInvItem(
                        move.To.AccountEntry.Id, move.To.Player.Id, (uint)InventoryType.Personal, move.Slot, move.Item.Id);

                if (net != 0)
                {
                    unitOfWork.Characters.UpdateCharacterCredits(a.Player.Id, initiatorCredits);
                    unitOfWork.Characters.UpdateCharacterCredits(b.Player.Id, targetCredits);
                }

                unitOfWork.Complete();
                transaction.Commit();
            }
            catch (Exception e)
            {
                // Nothing was committed, so nothing has moved: both players still hold what they
                // started with and the trade simply does not happen.
                Logger.WriteLog(LogType.Error, $"Trade between {a.Player.FamilyName} and {b.Player.FamilyName} failed to commit: {e}");

                End(session, notifyPartnerOf: null, message: PlayerMessage.PmTradeCancelled);
                return;
            }

            // Only now that the database has it does the in-memory state follow.
            foreach (var move in toB.Concat(toA))
                HandOver(move);

            if (net != 0)
            {
                SetCredits(a, initiatorCredits);
                SetCredits(b, targetCredits);
            }

            Logger.WriteLog(LogType.Security,
                $"Trade completed: {a.Player.FamilyName} gave {session.InitiatorCredits} credits and "
                + $"{session.InitiatorItems.Count} item(s), {b.Player.FamilyName} gave {session.TargetCredits} credits and "
                + $"{session.TargetItems.Count} item(s)");

            Forget(session);

            foreach (var participant in new[] { a, b })
            {
                participant.CallMethod(SysEntity.ClientTradeManagerId, new TradeCompletedPacket());
                participant.CallMethod(SysEntity.CommunicatorId,
                    new DisplayClientMessagePacket(PlayerMessage.PmTradeCompleted, new Dictionary<string, string>(), MsgFilterId.GeneralSystemMessages));
            }
        }

        /// <summary>One offered item and where it is going.</summary>
        private class ItemMove
        {
            public Item Item { get; set; }
            public Client From { get; set; }
            public Client To { get; set; }
            public uint FromSlot { get; set; }
            public uint Slot { get; set; }
        }

        /// <summary>
        /// Everything offered is still in the offering player's inventory, and is still what was
        /// offered. An item can leave between the confirmation and the exchange - equipped, moved
        /// to the home inventory, used up - and it can also shrink where it lies, which is the
        /// same offer in name only: every path that spends a stack (destroying part of it, a
        /// partial sale to a vendor, a reload, a crafting job, an ability's item cost) writes
        /// straight to StackSize without a word to the trade, so the confirmations stand while
        /// what they were given for goes. The exchange then hands over whatever is left.
        ///
        /// So the size and condition each item was offered at are compared with the item as it
        /// stands, and a trade whose terms have quietly moved is cancelled rather than completed.
        /// </summary>
        private static bool StillHolds(Client client, List<TradeSession.OfferedItem> items)
        {
            foreach (var offered in items)
            {
                var item = EntityManager.Instance.GetItem(offered.EntityId);

                if (item == null || item.MissionOwnership != null || !HoldsInPersonalInventory(client, offered.EntityId))
                    return false;

                if (offered.Matches(item))
                    continue;

                Logger.WriteLog(LogType.Security,
                    $"{client.Player.FamilyName} offered item {offered.EntityId} as {offered.StackSize} at {offered.CurrentHitPoints} hp "
                    + $"and would have handed over {item.StackSize} at {item.CurrentHitPoints}; the trade is cancelled.");

                return false;
            }

            return true;
        }

        /// <summary>
        /// Finds a free personal slot for each incoming item, in the receiver's own category
        /// bands, without writing anything. Returns false when they do not all fit, or when the
        /// giver has stopped holding one of them.
        /// </summary>
        private static bool TryReserveSlots(Client giver, Client receiver, List<TradeSession.OfferedItem> items, out List<ItemMove> moves)
        {
            moves = new List<ItemMove>();

            var taken = new HashSet<uint>();

            foreach (var offered in items)
            {
                var entityId = offered.EntityId;
                var item = EntityManager.Instance.GetItem(entityId);

                if (item == null)
                    return false;

                var category = (int)item.ItemTemplate.InventoryCategory - 1;

                if (category < 0 || category >= 5)
                    return false;

                var slot = FindFreeSlot(receiver, (uint)(category * 50), taken);

                if (slot == null)
                    return false;

                taken.Add(slot.Value);

                var fromSlot = giver.Player.Inventory.PersonalInventory.IndexOf(entityId);

                if (fromSlot < 0)
                    return false;

                moves.Add(new ItemMove
                {
                    Item = item,
                    From = giver,
                    To = receiver,
                    FromSlot = (uint)fromSlot,
                    Slot = slot.Value
                });
            }

            return true;
        }

        /// <summary>
        /// The receiver's first empty slot in the item's own 50-slot category band, skipping any
        /// already promised to an earlier item in the same trade.
        /// </summary>
        private static uint? FindFreeSlot(Client receiver, uint categoryOffset, HashSet<uint> taken)
        {
            for (var i = categoryOffset; i < categoryOffset + 50; i++)
                if (receiver.Player.Inventory.PersonalInventory[(int)i] == 0 && !taken.Contains(i))
                    return i;

            return null;
        }

        /// <summary>
        /// Moves one item between the two players in memory and on both clients. The database
        /// row has already been moved, inside the transaction.
        /// </summary>
        private static void HandOver(ItemMove move)
        {
            move.From.Player.Inventory.PersonalInventory[(int)move.FromSlot] = 0;
            move.From.CallMethod(SysEntity.ClientInventoryManagerId,
                new InventoryRemoveItemPacket(InventoryType.Personal, move.Item.EntityId));

            move.Item.OwnerId = move.To.Player.Id;
            move.Item.OwnerSlotId = move.Slot;

            move.To.Player.Inventory.PersonalInventory[(int)move.Slot] = move.Item.EntityId;
            move.To.CallMethod(SysEntity.ClientInventoryManagerId,
                new InventoryAddItemPacket(InventoryType.Personal, move.Item.EntityId, move.Slot));

            // The giver keeps an entity for something they no longer own; the receiver already
            // has a copy from when it was offered, so only the giver's has to go - and only on
            // the giver's screen, since the item itself is now the receiver's.
            DestroyCopyOnClient(move.From, move.Item.EntityId);
        }

        /// <summary>Applies a new credit total in memory and tells the player.</summary>
        private static void SetCredits(Client client, int amount)
        {
            client.Player.Credits[CurencyType.Credits] = amount;
            client.CallMethod(client.Player.EntityId,
                new UpdateCreditsPacket(CurencyType.Credits, amount, 0));
        }

        /// <summary>
        /// Any change to the terms withdraws both confirmations, and tells a partner who had
        /// already confirmed why their accept just disappeared.
        /// </summary>
        private void TermsChanged(TradeSession session, Client changedBy)
        {
            session.ConfigurationId++;

            var partner = session.PartnerOf(changedBy);

            if (session.IsConfirmed(partner))
                partner.CallMethod(SysEntity.CommunicatorId,
                    new DisplayClientMessagePacket(PlayerMessage.PmTradeChangeAfterConfirm, new Dictionary<string, string>(), MsgFilterId.GeneralSystemMessages));

            SetConfirmed(session, session.Initiator, false);
            SetConfirmed(session, session.Target, false);
        }

        private static void SetConfirmed(TradeSession session, Client client, bool confirmed)
        {
            if (session.IsConfirmed(client) == confirmed)
                return;

            session.SetConfirmed(client, confirmed);

            var change = new TradeConfirmChangePacket(client.Player.EntityId, confirmed);
            session.Initiator.CallMethod(SysEntity.ClientTradeManagerId, change);
            session.Target.CallMethod(SysEntity.ClientTradeManagerId, change);
        }

        /// <summary>
        /// Removes the session and closes it on both clients. TradeDestroy also clears the
        /// inviter's "trade requested" indicator and the invitee's pending-request indicator.
        /// </summary>
        private void End(TradeSession session, Client notifyPartnerOf, PlayerMessage? message = null)
        {
            Forget(session);

            // Each side was given a copy of whatever the other put up, so their window could draw
            // it. A trade that ends without completing has to take those back, or both players are
            // left holding entities for items they never received.
            ReturnOfferedCopies(session, session.Initiator, session.TargetItems);
            ReturnOfferedCopies(session, session.Target, session.InitiatorItems);

            foreach (var participant in new[] { session.Initiator, session.Target })
            {
                if (participant.State == ClientState.Disconnected)
                    continue;

                participant.CallMethod(SysEntity.ClientTradeManagerId, new TradeDestroyPacket());

                var tell = message ?? (notifyPartnerOf != null && participant != notifyPartnerOf ? PlayerMessage.PmTradeCancelled : (PlayerMessage?)null);

                if (tell.HasValue)
                    participant.CallMethod(SysEntity.CommunicatorId,
                        new DisplayClientMessagePacket(tell.Value, new Dictionary<string, string>(), MsgFilterId.GeneralSystemMessages));
            }
        }

        /// <summary>
        /// Drops an unaccepted invite because one of its players started another request. That
        /// player's client has already raised the "trade requested" indicator for the new
        /// request, and TradeDestroy clears it by name, so only the other side is told.
        /// </summary>
        private void Withdraw(TradeSession session, Client requester)
        {
            Forget(session);

            var other = session.PartnerOf(requester);

            if (other.State == ClientState.Disconnected)
                return;

            other.CallMethod(SysEntity.ClientTradeManagerId, new TradeDestroyPacket());

            // The inviter learns their invite lapsed; an invitee whose inviter moved on needs no message.
            if (!session.IsInitiator(requester))
                other.CallMethod(SysEntity.CommunicatorId,
                    new DisplayClientMessagePacket(PlayerMessage.PmTradeTheyAreTooBusy, new Dictionary<string, string>(), MsgFilterId.GeneralSystemMessages));
        }

        private static bool Expired(TradeSession session)
        {
            return Environment.TickCount64 - session.CreatedTick > PendingInviteTimeoutMs;
        }

        private void Forget(TradeSession session)
        {
            if (_sessions.TryGetValue(session.Initiator, out var s1) && s1 == session)
                _sessions.Remove(session.Initiator);

            if (_sessions.TryGetValue(session.Target, out var s2) && s2 == session)
                _sessions.Remove(session.Target);
        }

        private bool TryGetOpenSession(Client client, out TradeSession session)
        {
            return _sessions.TryGetValue(client, out session) && session.Accepted;
        }

        /// <summary>
        /// Says why, and nothing else. Refusing one item out of an offer must not take the trade
        /// window down with it - the players are still trading, they just cannot trade that.
        /// </summary>
        private static void Decline(Client client, PlayerMessage message)
        {
            client.CallMethod(SysEntity.CommunicatorId,
                new DisplayClientMessagePacket(message, new Dictionary<string, string>(), MsgFilterId.GeneralSystemMessages));
        }

        /// <summary>Ends the trade on this client, with the reason. For refusals that stop it.</summary>
        private static void Refuse(Client client, PlayerMessage message)
        {
            // The requester's client already shows a "trade requested" indicator; TradeDestroy
            // removes it along with the message saying why.
            client.CallMethod(SysEntity.ClientTradeManagerId, new TradeDestroyPacket());
            client.CallMethod(SysEntity.CommunicatorId,
                new DisplayClientMessagePacket(message, new Dictionary<string, string>(), MsgFilterId.GeneralSystemMessages));
        }

        private static bool StillValid(TradeSession session)
        {
            var a = session.Initiator;
            var b = session.Target;

            return a.State == ClientState.Ingame && b.State == ClientState.Ingame && InRange(a, b);
        }

        private static bool InRange(Client a, Client b)
        {
            if (a.Player.MapChannel == null || a.Player.MapChannel != b.Player.MapChannel)
                return false;

            return Vector3.Distance(a.Player.Position, b.Player.Position) <= TradeRange + ServerRangeSlack;
        }

        private static int CreditsOnHand(Client client)
        {
            return client.Player.Credits.TryGetValue(CurencyType.Credits, out var credits) ? credits : 0;
        }

        private static Client FindIngamePlayer(ulong entityId)
        {
            return Server.Clients.Find(c => c.State == ClientState.Ingame && c.Player.EntityId == entityId);
        }
    }
}
