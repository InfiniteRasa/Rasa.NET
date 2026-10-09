using System;
using System.Collections.Generic;
using System.Linq;

namespace Rasa.Managers
{
    using Api;
    using Data;
    using Repositories.UnitOfWork;
    using Structures;
    using Structures.World;

    /// <summary>
    /// The creature classes' flags for gametools' endpoints (Api.MonsterFlagsEndpoint,
    /// Api.UpdateMonsterFlagsEndpoint): read from the world database, and written to it and
    /// then to the classes the server has loaded.
    ///
    /// It runs on the REST API's threads, not the world loop, so it touches the world in one
    /// way only: a class's list of flags is put in place of the old one whole and never changed
    /// where it is, and whoever is reading it on the world loop has the old list or the new.
    /// The table of classes itself is filled when the server starts and not changed after.
    /// </summary>
    public sealed class MonsterFlagStore : IMonsterFlagStore
    {
        private readonly IGameUnitOfWorkFactory _factory;
        private readonly Func<IReadOnlyDictionary<EntityClasses, EntityClass>> _classes;
        private readonly object _writing = new object();

        public MonsterFlagStore(IGameUnitOfWorkFactory factory, Func<IReadOnlyDictionary<EntityClasses, EntityClass>> classes = null)
        {
            _factory = factory;
            _classes = classes ?? (() => EntityClassManager.Instance.LoadedEntityClasses);
        }

        private static bool IsCreature(EntityClass entityClass) =>
            entityClass?.Augmentations != null && entityClass.Augmentations.Contains(AugmentationType.Creature);

        public IReadOnlyList<MonsterFlags> Read()
        {
            Dictionary<uint, List<uint>> stored;

            using (var unitOfWork = _factory.CreateWorld())
                stored = unitOfWork.Creatures.GetClassFlags()
                    .GroupBy(row => row.ClassId)
                    .ToDictionary(rows => rows.Key, rows => rows.Select(row => row.FlagId).ToList());

            return _classes().Values
                .Where(IsCreature)
                .OrderBy(entityClass => entityClass.ClassId)
                .Select(entityClass => new MonsterFlags(entityClass.ClassId, entityClass.ClassName,
                    stored.TryGetValue(entityClass.ClassId, out var flags) ? flags : null))
                .ToList();
        }

        public StoreAnswer Replace(IReadOnlyList<MonsterFlags> classes)
        {
            var answer = new StoreAnswer();
            var loaded = _classes();

            foreach (var entry in classes)
            {
                if (answer.Full)
                    break;

                if (!loaded.TryGetValue((EntityClasses)entry.ClassId, out var entityClass))
                    answer.Problems.Add($"class {entry.ClassId} is not a class the server has");
                else if (!IsCreature(entityClass))
                    answer.Problems.Add($"class {entry.ClassId} ({entityClass.ClassName}) is not a creature's");

                foreach (var flag in entry.Flags)
                    if (flag > int.MaxValue || !Enum.IsDefined(typeof(CreatureFlag), (int)flag))
                        answer.Problems.Add($"class {entry.ClassId}: {flag} is not a creature flag");
            }

            if (!answer.Done || classes.Count == 0)
                return answer;

            lock (_writing)
            {
                using (var unitOfWork = _factory.CreateWorld())
                    unitOfWork.Creatures.ReplaceClassFlags(classes.ToDictionary(
                        entry => entry.ClassId,
                        entry => (IReadOnlyCollection<uint>)entry.Flags));

                foreach (var entry in classes)
                    loaded[(EntityClasses)entry.ClassId].CreatureFlags = entry.Flags.Select(flag => (CreatureFlag)flag).ToList();
            }

            return answer;
        }
    }

    /// <summary>
    /// The loot pools for gametools' endpoints (Api.LootPoolsEndpoint,
    /// Api.UpdateLootPoolsEndpoint): read from the world database, and written to it and then
    /// put in force (LootPools.Use). It runs on the REST API's threads; a LootPoolSet is never
    /// changed once made, so the world loop has the old one or the new.
    /// </summary>
    public sealed class LootPoolStore : ILootPoolStore
    {
        private readonly IGameUnitOfWorkFactory _factory;
        private readonly Func<uint, uint?> _stackOf;
        private readonly object _writing = new object();

        /// <param name="stackOf">The stack size of an item template, or null for one the server has not got; the server's own item data when left out.</param>
        public LootPoolStore(IGameUnitOfWorkFactory factory, Func<uint, uint?> stackOf = null)
        {
            _factory = factory;
            _stackOf = stackOf ?? StackOf;
        }

        /// <summary>From the item templates and classes, which are filled when the server starts and not changed after.</summary>
        private static uint? StackOf(uint templateId)
        {
            // Asked of the table first: GetItemTemplateById writes an error line for every
            // one it has not got, and a file can name a great many.
            if (!ItemManager.Instance.ItemTemplateItemClass.ContainsKey(templateId))
                return null;

            var template = ItemManager.Instance.GetItemTemplateById(templateId);

            if (template == null)
                return null;

            return EntityClassManager.Instance.GetClassInfo(template.Class)?.ItemClassInfo?.StackSize;
        }

        public LootPoolSet Read()
        {
            // Three tables, three queries: not while a replacement is half written between them.
            lock (_writing)
            {
                using var unitOfWork = _factory.CreateWorld();
                var repository = unitOfWork.LootGroups;

                return LootPools.FromRows(repository.GetGroups(), repository.GetItems(), repository.GetCreatureGroups());
            }
        }

        public StoreAnswer Replace(LootPoolSet set)
        {
            var answer = new StoreAnswer();

            set ??= LootPoolSet.Empty;

            lock (_writing)
            {
                using var unitOfWork = _factory.CreateWorld();

                // The creature rows as the database has them: the table the server loaded
                // can be read again by a GM while this runs.
                var creatures = unitOfWork.Creatures.Get().Select(creature => creature.Id).ToHashSet();

                foreach (var pool in set.Pools)
                {
                    if (answer.Full)
                        break;

                    if (pool.Items.Count == 0)
                        answer.Remark($"group {pool.Id} ({pool.Name}) has no items");

                    foreach (var item in pool.Items)
                    {
                        var stack = _stackOf(item.TemplateId);

                        if (stack == null)
                            answer.Problems.Add($"group {pool.Id} ({pool.Name}): item template {item.TemplateId} is not one the server has");
                        else if (item.Maximum > Math.Max(1u, stack.Value))
                            answer.Remark($"group {pool.Id} ({pool.Name}): item {item.TemplateId} asks for up to {item.Maximum}, and a stack is {Math.Max(1u, stack.Value)}; a stack is the most it gives");

                        if (answer.Full)
                            break;
                    }
                }

                foreach (var creatureId in set.Assignments.Select(pair => pair.CreatureId).Distinct())
                {
                    if (answer.Full)
                        break;

                    if (!creatures.Contains(creatureId))
                        answer.Problems.Add($"creature {creatureId} is not a creature row the server has");
                }

                if (!answer.Done)
                    return answer;

                unitOfWork.LootGroups.ReplaceAll(
                    set.Pools.Select(pool => new LootGroupEntry { Id = pool.Id, Name = pool.Name, Comment = pool.Note }).ToList(),
                    set.Pools.SelectMany(pool => pool.Items.Select(item => new LootGroupItemEntry
                    {
                        GroupId = pool.Id,
                        ItemTemplateId = item.TemplateId,
                        Chance = item.Chance,
                        MinQuantity = item.Minimum,
                        MaxQuantity = item.Maximum
                    })).ToList(),
                    set.Assignments.Select(pair => new CreatureLootGroupEntry { CreatureId = pair.CreatureId, GroupId = pair.PoolId }).ToList());

                LootPools.Use(set);
            }

            return answer;
        }
    }
}
