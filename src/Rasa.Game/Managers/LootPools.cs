using System;
using System.Collections.Generic;
using System.Linq;

namespace Rasa.Managers
{
    using Repositories.UnitOfWork;
    using Structures.World;

    /// <summary>
    /// One item of a loot pool: its template, its chance on a kill in percent, and how many it
    /// gives. Whatever it is made from - a row somebody wrote into the table by hand as much
    /// as a file the API checked - it is put right as the editor would: a chance from 0 to
    /// 100, at least one, a maximum no less than the minimum and no more than the table's
    /// limit.
    /// </summary>
    public sealed class LootPoolItem
    {
        public LootPoolItem(uint templateId, double chance, uint minimum, uint maximum)
        {
            TemplateId = templateId;
            Chance = LootPools.TidyChance(chance);
            Minimum = Math.Clamp(minimum, 1u, LootGroupItemEntry.QuantityLimit);
            Maximum = Math.Clamp(maximum, Minimum, LootGroupItemEntry.QuantityLimit);
        }

        public uint TemplateId { get; }

        /// <summary>A percent, 0 to 100, kept to four decimal places.</summary>
        public double Chance { get; }

        public uint Minimum { get; }
        public uint Maximum { get; }
    }

    /// <summary>A loot pool: a named list of items.</summary>
    public sealed class LootPool
    {
        public LootPool(uint id, string name, string note, IEnumerable<LootPoolItem> items)
        {
            Id = id;
            Name = name ?? "";
            Note = note ?? "";
            Items = (items ?? Enumerable.Empty<LootPoolItem>()).ToList();
        }

        public uint Id { get; }
        public string Name { get; }
        public string Note { get; }
        public IReadOnlyList<LootPoolItem> Items { get; }
    }

    /// <summary>
    /// Every loot pool and which creature rows have which, as one thing that is never changed
    /// once made: a new set takes the place of the old one whole (<see cref="LootPools.Use"/>),
    /// so a kill on the world loop rolls the old set or the new one and never half of each.
    /// </summary>
    public sealed class LootPoolSet
    {
        public static LootPoolSet Empty { get; } = new LootPoolSet(Array.Empty<LootPool>(), Array.Empty<(uint, uint)>());

        private readonly Dictionary<uint, List<LootPool>> _byCreature = new Dictionary<uint, List<LootPool>>();

        /// <param name="assignments">Creature row and pool, a pair for each pool a creature has. One naming a pool that is not in the set is left out.</param>
        public LootPoolSet(IEnumerable<LootPool> pools, IEnumerable<(uint CreatureId, uint PoolId)> assignments)
        {
            Pools = (pools ?? Enumerable.Empty<LootPool>()).OrderBy(pool => pool.Id).ToList();

            var byId = new Dictionary<uint, LootPool>();

            foreach (var pool in Pools)
                byId[pool.Id] = pool;

            var kept = new List<(uint, uint)>();

            foreach (var (creatureId, poolId) in (assignments ?? Enumerable.Empty<(uint, uint)>()).Distinct().OrderBy(pair => pair.Item1).ThenBy(pair => pair.Item2))
            {
                if (!byId.TryGetValue(poolId, out var pool))
                    continue;

                if (!_byCreature.TryGetValue(creatureId, out var list))
                    _byCreature[creatureId] = list = new List<LootPool>();

                list.Add(pool);
                kept.Add((creatureId, poolId));
            }

            Assignments = kept;
        }

        public IReadOnlyList<LootPool> Pools { get; }
        public IReadOnlyList<(uint CreatureId, uint PoolId)> Assignments { get; }

        /// <summary>How many item rows the pools have between them.</summary>
        public int ItemCount => Pools.Sum(pool => pool.Items.Count);

        /// <summary>How many creature rows have a pool.</summary>
        public int CreatureCount => _byCreature.Count;

        /// <summary>The pools a creature row has; null for one with none.</summary>
        public IReadOnlyList<LootPool> For(uint creatureId) =>
            _byCreature.TryGetValue(creatureId, out var pools) ? pools : null;
    }

    /// <summary>
    /// What creatures drop: the loot pools of the world database (loot_group, loot_group_item,
    /// creature_loot_group), read when the server starts and replaced while it runs by the REST
    /// API's POST /updatelootpools, which gametools' Loot Table Editor sends.
    ///
    /// A creature that has pools drops what they roll, and the handful of credits every corpse
    /// has: each item of each of its pools is rolled on its own, at its chance, and one that
    /// comes up gives between its minimum and its maximum, never more than a stack. An item in
    /// two of its pools is rolled twice. A creature with no pool drops what it did before
    /// there were pools (LootDispenserManager.CreateLoot), and a mission's authored loot is the
    /// mission's still.
    /// </summary>
    public static class LootPools
    {
        /// <summary>A chance is kept to this many parts in a percent: four decimal places.</summary>
        public const int ChanceScale = 10000;

        private static volatile LootPoolSet _current = LootPoolSet.Empty;

        /// <summary>The pools in force.</summary>
        public static LootPoolSet Current => _current;

        /// <summary>Puts a set in force, from any thread; kills from now on roll it.</summary>
        public static void Use(LootPoolSet set)
        {
            _current = set ?? LootPoolSet.Empty;
        }

        /// <summary>Reads the pools from the world database. A world that has no such tables has no pools.</summary>
        public static void Init(IGameUnitOfWorkFactory factory)
        {
            try
            {
                using var unitOfWork = factory.CreateWorld();
                var repository = unitOfWork.LootGroups;
                var set = FromRows(repository.GetGroups(), repository.GetItems(), repository.GetCreatureGroups());

                Use(set);

                Logger.WriteLog(LogType.Initialize, $"Loaded {set.Pools.Count} loot pools with {set.ItemCount} items, on {set.CreatureCount} creatures");
            }
            catch (NotSupportedException)
            {
                Use(LootPoolSet.Empty);
            }
        }

        /// <summary>The set the three tables' rows make. A row with a chance or a quantity that is none is put right (<see cref="LootPoolItem"/>).</summary>
        public static LootPoolSet FromRows(IEnumerable<LootGroupEntry> groups, IEnumerable<LootGroupItemEntry> items, IEnumerable<CreatureLootGroupEntry> creatureGroups)
        {
            var itemsByGroup = (items ?? Enumerable.Empty<LootGroupItemEntry>())
                .GroupBy(row => row.GroupId)
                .ToDictionary(rows => rows.Key, rows => rows.OrderBy(row => row.ItemTemplateId)
                    .Select(row => new LootPoolItem(row.ItemTemplateId, row.Chance, row.MinQuantity, row.MaxQuantity)).ToList());

            var pools = (groups ?? Enumerable.Empty<LootGroupEntry>()).Select(group =>
                new LootPool(group.Id, group.Name, group.Comment, itemsByGroup.TryGetValue(group.Id, out var rows) ? rows : null));

            return new LootPoolSet(pools, (creatureGroups ?? Enumerable.Empty<CreatureLootGroupEntry>()).Select(row => (row.CreatureId, row.GroupId)));
        }

        /// <summary>A chance as it is kept: a number from 0 to 100 to four decimal places; 0 for what is no number.</summary>
        public static double TidyChance(double chance)
        {
            if (double.IsNaN(chance) || chance <= 0)
                return 0;

            return chance >= 100 ? 100 : Math.Round(chance * ChanceScale, MidpointRounding.AwayFromZero) / ChanceScale;
        }

        /// <summary>
        /// What these pools give for one kill: template and how many, an entry for every item
        /// that came up. <paramref name="next"/> is Random.Next: a whole number from its first
        /// argument up to, not including, its second.
        /// </summary>
        public static List<(uint TemplateId, uint Quantity)> Roll(IEnumerable<LootPool> pools, Func<int, int, int> next)
        {
            var drops = new List<(uint, uint)>();

            if (pools == null)
                return drops;

            foreach (var pool in pools)
                foreach (var item in pool.Items)
                {
                    var parts = (int)Math.Round(item.Chance * ChanceScale, MidpointRounding.AwayFromZero);

                    if (parts <= 0 || (parts < 100 * ChanceScale && next(0, 100 * ChanceScale) >= parts))
                        continue;

                    drops.Add((item.TemplateId, item.Minimum >= item.Maximum
                        ? item.Minimum
                        : (uint)next((int)item.Minimum, (int)item.Maximum + 1)));
                }

            return drops;
        }
    }
}
