using System;
using System.Collections.Generic;
using System.Linq;

namespace Rasa.Managers
{
    using Game;
    using Repositories.UnitOfWork;
    using Structures;

    /// <summary>
    /// The rounds in a weapon's clip, and when they are written to the database.
    ///
    /// A shot used to be a transaction of its own: the weapon's row read, the count written and
    /// the commit waited for, on the main loop, before the shot went - five times a second for
    /// each player holding down the trigger of a machine gun, and every other player on every
    /// map waiting with them. The count in memory (Item.CurrentAmmo) is what the server goes by
    /// while it runs; the row is only read when an item is made from it. So a shot now takes its
    /// rounds out of the clip in memory and is noted here, and the row is written:
    ///
    ///  - when the weapon is reloaded, in the transaction that takes the rounds from the pack
    ///    (ManifestationManager.WeaponReload);
    ///  - when it is put away, when another is taken in hand, and when it leaves the weapon
    ///    drawer for anywhere else - the pack, a trade, the lockbox, the bin;
    ///  - when its holder leaves the map or the game, a dropped connection included;
    ///  - every <see cref="SweepMs"/>, for whoever is still firing;
    ///  - when the server shuts down.
    ///
    /// What a crash costs is the rounds fired since the last of those: they are back in the
    /// clip, a clip's worth a weapon at the very most. Nothing can be made of it. The client has
    /// no way to take rounds out of a clip again; they can only be fired.
    ///
    /// A weapon made from its row while a count is still waiting to be written - its holder back
    /// before the save, an item handed to the auction house - is given the waiting count and not
    /// the row's (<see cref="Loaded"/>), so the row is never taken for more than it is.
    ///
    /// The shot had a second purpose: it refused to fire if the row was not what the server
    /// thought it was. That is asked when the row is written instead, and at a reload
    /// (<see cref="RowCountOf"/>): a row whose count changed underneath is logged, and written
    /// over with the count the server has been firing from; a row that is gone, or is another
    /// item's by now, is left alone and the count forgotten.
    /// </summary>
    public static class WeaponClips
    {
        /// <summary>How long a count may wait to be written while its weapon goes on being fired.</summary>
        public const long SweepMs = 15000;

        /// <summary>The clock the sweep runs on; a test's to replace.</summary>
        public static Func<long> Now { get; set; } = () => Environment.TickCount64;

        private sealed class Waiting
        {
            /// <summary>The weapon, whose CurrentAmmo is the count to write.</summary>
            public Item Item;

            /// <summary>What its row holds: the count it had when the first of these rounds was spent.</summary>
            public uint RowCount;

            /// <summary>Whose hands it was fired from (<see cref="SaveFor"/>).</summary>
            public uint CharacterId;

            public IGameUnitOfWorkFactory Factory;
        }

        /// <summary>The weapons whose count in memory is not their row's, by item id.</summary>
        private static readonly Dictionary<uint, Waiting> Unsaved = new Dictionary<uint, Waiting>();
        private static readonly object Sync = new object();
        private static long _sweptAt = long.MinValue;

        /// <summary>How many weapons have a count waiting to be written.</summary>
        public static int UnsavedCount
        {
            get
            {
                lock (Sync)
                    return Unsaved.Count;
            }
        }

        /// <summary>Whether this weapon's count is waiting to be written.</summary>
        public static bool IsUnsaved(Item weapon)
        {
            lock (Sync)
                return weapon != null && Unsaved.TryGetValue(weapon.Id, out var waiting) && ReferenceEquals(waiting.Item, weapon);
        }

        /// <summary>
        /// An item was made from its row, or met its row again: its clip is the row's count,
        /// unless a count for that row is still waiting to be written - then it is that one, and
        /// this item is the one it is written from.
        /// </summary>
        public static void Loaded(Item item, uint rowCount)
        {
            if (item == null)
                return;

            lock (Sync)
            {
                if (item.Id != 0 && Unsaved.TryGetValue(item.Id, out var waiting))
                {
                    if (!ReferenceEquals(waiting.Item, item))
                    {
                        item.CurrentAmmo = waiting.Item.CurrentAmmo;
                        waiting.Item = item;
                    }

                    return;
                }
            }

            item.CurrentAmmo = rowCount;
        }

        /// <summary>
        /// Takes rounds out of a weapon's clip - a shot, a use of a tool - and notes that its
        /// row is behind. Nothing is written here. An item with no row (a polymorphed player's
        /// creature weapon) only loses the rounds.
        /// </summary>
        public static void Spend(Client client, Item weapon, uint rounds, IGameUnitOfWorkFactory factory)
        {
            if (weapon == null || rounds == 0)
                return;

            var before = weapon.CurrentAmmo;

            weapon.CurrentAmmo = before > rounds ? before - rounds : 0;

            if (weapon.Id == 0 || factory == null)
                return;

            lock (Sync)
            {
                if (Unsaved.TryGetValue(weapon.Id, out var waiting))
                {
                    waiting.Item = weapon;
                    waiting.CharacterId = client?.Player?.Id ?? waiting.CharacterId;
                    waiting.Factory = factory;
                    return;
                }

                Unsaved[weapon.Id] = new Waiting
                {
                    Item = weapon,
                    RowCount = before,
                    CharacterId = client?.Player?.Id ?? 0,
                    Factory = factory
                };
            }
        }

        /// <summary>What the weapon's row holds, as far as the server knows: its clip, or what its clip was before the rounds not yet written were spent.</summary>
        public static uint RowCountOf(Item weapon)
        {
            if (weapon == null)
                return 0;

            lock (Sync)
                return Unsaved.TryGetValue(weapon.Id, out var waiting) && ReferenceEquals(waiting.Item, weapon) ? waiting.RowCount : weapon.CurrentAmmo;
        }

        /// <summary>The weapon's row was just written with its clip by something else - a reload: nothing is waiting for that row any more.</summary>
        public static void Saved(Item weapon)
        {
            if (weapon == null)
                return;

            lock (Sync)
                Unsaved.Remove(weapon.Id);
        }

        /// <summary>Writes this weapon's clip to its row if it is waiting to be: it is being put away, changed for another, or taken out of the drawer.</summary>
        public static void Save(Item weapon)
        {
            if (weapon == null)
                return;

            Waiting waiting;

            lock (Sync)
            {
                if (!Unsaved.TryGetValue(weapon.Id, out waiting) || !ReferenceEquals(waiting.Item, weapon))
                    return;
            }

            Write(new[] { waiting });
        }

        /// <summary>Writes every clip this player's weapons have waiting: they are leaving the map or the game.</summary>
        public static void SaveFor(Client client)
        {
            var characterId = client?.Player?.Id ?? 0;

            if (characterId == 0)
                return;

            List<Waiting> theirs;

            lock (Sync)
                theirs = Unsaved.Values.Where(waiting => waiting.CharacterId == characterId).ToList();

            Write(theirs);
        }

        /// <summary>Writes every clip that is waiting: the sweep, and the server shutting down.</summary>
        public static void SaveAll()
        {
            List<Waiting> all;

            lock (Sync)
                all = Unsaved.Values.ToList();

            Write(all);
        }

        /// <summary>The sweep: every <see cref="SweepMs"/>, all that is waiting, in one transaction. Every tick of the main loop.</summary>
        public static void Worker()
        {
            var now = Now();

            if (_sweptAt != long.MinValue && now - _sweptAt < SweepMs)
                return;

            _sweptAt = now;
            SaveAll();
        }

        /// <summary>Forgets everything that is waiting and writes none of it; a test's.</summary>
        internal static void Reset()
        {
            lock (Sync)
                Unsaved.Clear();

            _sweptAt = long.MinValue;
        }

        /// <summary>
        /// The rows written, one transaction to a database. A count stays waiting if its write
        /// fails, to be tried again by the next thing that would have written it.
        /// </summary>
        private static void Write(IReadOnlyCollection<Waiting> batch)
        {
            if (batch.Count == 0)
                return;

            foreach (var group in batch.GroupBy(waiting => waiting.Factory))
            {
                // The counts as they are now: what is written, and what is compared with once it
                // has been, in case a shot was fired in between.
                var writes = group.Select(waiting => (Waiting: waiting, Item: waiting.Item, Count: waiting.Item.CurrentAmmo)).ToList();
                var gone = new List<Waiting>();

                try
                {
                    using var unitOfWork = group.Key.CreateChar();

                    unitOfWork.ExecuteTransaction(() =>
                    {
                        gone.Clear();

                        foreach (var write in writes)
                        {
                            var row = unitOfWork.Items.GetItem(write.Item.Id);

                            // Destroyed, sold or used up since: there is no clip to keep. Nor if
                            // the row is another item's by now - a row's id can be given out
                            // again once it is deleted - which is not this weapon's to write.
                            var templateId = write.Item.ItemTemplate?.ItemTemplateId ?? write.Item.ItemTemplateId;

                            if (row == null || (templateId != 0 && row.ItemTemplateId != templateId))
                            {
                                gone.Add(write.Waiting);
                                continue;
                            }

                            if (row.AmmoCount != write.Waiting.RowCount)
                                Logger.WriteLog(LogType.Error,
                                    $"Weapon clip of item {write.Item.Id}: its row held {row.AmmoCount} where {write.Waiting.RowCount} was expected; written over with {write.Count}.");

                            unitOfWork.Items.UpdateAmmo(new Item { Id = write.Item.Id, CurrentAmmo = write.Count });
                        }
                    });
                }
                // Whatever it is: a save that fails must not take the shot, the move or the tick
                // it was made from with it.
                catch (Exception error)
                {
                    Logger.WriteLog(LogType.Error, $"Could not save {writes.Count} weapon clip(s); they stay waiting: {error.Message}");
                    continue;
                }

                lock (Sync)
                {
                    foreach (var write in writes)
                    {
                        if (!Unsaved.TryGetValue(write.Item.Id, out var current) || !ReferenceEquals(current, write.Waiting))
                            continue;

                        // Fired again while it was being written: the row is behind once more.
                        if (!gone.Contains(write.Waiting) && ReferenceEquals(current.Item, write.Item) && current.Item.CurrentAmmo != write.Count)
                        {
                            current.RowCount = write.Count;
                            continue;
                        }

                        Unsaved.Remove(write.Item.Id);
                    }
                }
            }
        }
    }
}
