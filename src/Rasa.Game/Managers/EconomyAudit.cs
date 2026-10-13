using System;
using System.Collections.Generic;
using System.Linq;

namespace Rasa.Managers
{
    using Data;
    using Game;
    using Repositories.UnitOfWork;
    using Structures;
    using Structures.Char;

    /// <summary>
    /// The economy log: every item and every sum of credits or prestige that comes to or leaves
    /// a character through the economy, kept in the character database's economy_log table
    /// (<see cref="IStore"/>). One event - a trade, an auction bought out, a vendor's sale - is
    /// one transfer id and a row for each thing that moved, on each side it moved: an item
    /// handed from one player to another is a row for the giver (its stack, negative) and a row
    /// for the receiver (positive), and the item's row id is on both, so an item can be followed
    /// from owner to owner. A sum's row has what the character held afterwards.
    ///
    /// What is on it: trades; the auction house (listed with its deposit, bought out, taken
    /// down, run out); wagers; the clan bank; the account lockbox's credits and its tabs; a
    /// clan's creation fee; vendors (bought, sold, bought back, repaired); mission rewards;
    /// credits and prestige looted; prestige for a PvP kill; transfer credit slips cashed;
    /// a crafting station's cost.
    ///
    /// What is not: what a game master gives, which the game master audit log has (GmAudit),
    /// and the clan lockbox's items, which have their own log (clan_lockbox_log).
    ///
    /// Rows are written once the event has happened - after its own commit - in one insert an
    /// event. Nothing waits on the log or fails with it: a store that cannot be written costs
    /// the rows, says so in the server log, and the event stands.
    /// </summary>
    public class EconomyAudit
    {
        private static EconomyAudit _instance;
        private static readonly object InstanceLock = new object();

        public static EconomyAudit Instance
        {
            get
            {
                if (_instance == null)
                    lock (InstanceLock)
                        _instance ??= new EconomyAudit();

                return _instance;
            }
        }

        /// <summary>Where the log is kept. The call may throw; it is not retried.</summary>
        public interface IStore
        {
            /// <summary>Adds an event's rows, all or none. Returns how many.</summary>
            int Add(IReadOnlyCollection<EconomyLogEntry> entries);
        }

        /// <summary>Where the rows go; null keeps none. Set by <see cref="Load"/>.</summary>
        public IStore Store { get; private set; }

        /// <summary>The wall clock rows are dated by, UTC. Replaceable for tests.</summary>
        public Func<DateTime> UtcNow { get; set; } = () => DateTime.UtcNow;

        /// <summary>An event's transfer id. Replaceable for tests.</summary>
        public Func<string> NewTransferId { get; set; } = () => Guid.NewGuid().ToString("N");

        /// <summary>From now on rows are kept in <paramref name="store"/>.</summary>
        public void Load(IStore store)
        {
            Store = store;
        }

        /// <summary>
        /// Puts an event on the log: <paramref name="lines"/> are its rows, made by
        /// <see cref="ItemLine(Client, Item, int, uint)"/> and <see cref="MoneyLine(Client, CurencyType, long)"/>;
        /// null lines, items of no stack and sums of nothing are left out. Returns the transfer
        /// id; null when nothing was written - no store, nothing moved, or the store failed.
        /// </summary>
        public string Record(EconomyLogKind kind, uint referenceId, params EconomyLogEntry[] lines) =>
            Record(kind, referenceId, (IEnumerable<EconomyLogEntry>)lines);

        /// <inheritdoc cref="Record(EconomyLogKind, uint, EconomyLogEntry[])"/>
        public string Record(EconomyLogKind kind, uint referenceId, IEnumerable<EconomyLogEntry> lines)
        {
            var store = Store;

            if (store == null || lines == null)
                return null;

            try
            {
                var rows = lines.Where(line => line != null && (line.Quantity != 0 || line.Amount != 0)).ToList();

                if (rows.Count == 0)
                    return null;

                var transferId = NewTransferId();
                var createdAt = UtcNow();

                foreach (var row in rows)
                {
                    row.TransferId = transferId;
                    row.CreatedAt = createdAt;
                    row.Kind = (byte)kind;
                    row.ReferenceId = referenceId;
                }

                store.Add(rows);

                return transferId;
            }
            catch (Exception e)
            {
                Logger.WriteLog(LogType.Error,
                    $"Economy log: a {kind} ({referenceId}) could not be recorded: {e.GetBaseException().Message}");

                return null;
            }
        }

        /// <summary>
        /// An item that came to (<paramref name="quantity"/> positive) or left (negative) the
        /// character <paramref name="client"/> plays; <paramref name="otherCharacterId"/> is the
        /// player on the other side. Null when there is no character or no item.
        /// </summary>
        public static EconomyLogEntry ItemLine(Client client, Item item, int quantity, uint otherCharacterId = 0)
        {
            var player = client?.Player;

            if (player == null)
                return null;

            return ItemLine(player.Id, client.AccountEntry?.Id ?? 0, item, quantity, otherCharacterId, player.MapContextId);
        }

        /// <summary>
        /// An item that came to or left a character who need not be playing (an auction's
        /// seller); <paramref name="accountId"/> 0 where it is not to hand. Null when there is no
        /// character or no item.
        /// </summary>
        public static EconomyLogEntry ItemLine(uint characterId, uint accountId, Item item, int quantity,
            uint otherCharacterId = 0, uint mapContextId = 0)
        {
            if (item == null)
                return null;

            return ItemLine(characterId, accountId, item.Id, item.ItemTemplate?.ItemTemplateId ?? item.ItemTemplateId, quantity,
                otherCharacterId, mapContextId);
        }

        /// <summary>An item by its row and template, for one that is not loaded. Null when there is no character.</summary>
        public static EconomyLogEntry ItemLine(uint characterId, uint accountId, uint itemId, uint itemTemplateId, int quantity,
            uint otherCharacterId = 0, uint mapContextId = 0)
        {
            if (characterId == 0)
                return null;

            return new EconomyLogEntry
            {
                CharacterId = characterId,
                AccountId = accountId,
                OtherCharacterId = otherCharacterId,
                ItemId = itemId,
                ItemTemplateId = itemTemplateId,
                Quantity = quantity,
                MapContextId = mapContextId
            };
        }

        /// <summary>
        /// A sum that came to (<paramref name="amount"/> positive) or left (negative) the
        /// character <paramref name="client"/> plays, with what they hold of it now - so it is
        /// made once the purse has been changed. Null when there is no character.
        /// </summary>
        public static EconomyLogEntry MoneyLine(Client client, CurencyType currency, long amount)
        {
            var player = client?.Player;

            if (player == null)
                return null;

            return MoneyLine(client, currency, amount, player.Credits.GetValueOrDefault(currency));
        }

        /// <summary>As <see cref="MoneyLine(Client, CurencyType, long)"/>, with the balance given and the player on the other side.</summary>
        public static EconomyLogEntry MoneyLine(Client client, CurencyType currency, long amount, long balance, uint otherCharacterId = 0)
        {
            var player = client?.Player;

            if (player == null)
                return null;

            return MoneyLine(player.Id, client.AccountEntry?.Id ?? 0, currency, amount, balance, otherCharacterId, player.MapContextId);
        }

        /// <summary>A sum that came to or left a character who need not be playing. Null when there is no character.</summary>
        public static EconomyLogEntry MoneyLine(uint characterId, uint accountId, CurencyType currency, long amount, long balance,
            uint otherCharacterId = 0, uint mapContextId = 0)
        {
            if (characterId == 0)
                return null;

            return new EconomyLogEntry
            {
                CharacterId = characterId,
                AccountId = accountId,
                OtherCharacterId = otherCharacterId,
                Currency = (byte)currency,
                Amount = amount,
                Balance = balance,
                MapContextId = mapContextId
            };
        }

        /// <summary>The live server's store: the character database.</summary>
        public sealed class ServerStore : IStore
        {
            private readonly IGameUnitOfWorkFactory _factory;

            public ServerStore(IGameUnitOfWorkFactory factory)
            {
                _factory = factory;
            }

            public int Add(IReadOnlyCollection<EconomyLogEntry> entries)
            {
                using var unitOfWork = _factory.CreateChar();
                return unitOfWork.EconomyLogs.Add(entries);
            }
        }
    }
}
