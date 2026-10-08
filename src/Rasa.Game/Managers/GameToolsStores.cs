using System;
using System.Collections.Generic;
using System.Linq;

namespace Rasa.Managers
{
    using Api;
    using Data;
    using Repositories.UnitOfWork;
    using Structures;

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
                if (answer.Full) break;
                if (!loaded.TryGetValue((EntityClasses)entry.ClassId, out var entityClass))
                    answer.Problems.Add($"class {entry.ClassId} is not a class the server has");
                else if (!IsCreature(entityClass))
                    answer.Problems.Add($"class {entry.ClassId} ({entityClass.ClassName}) is not a creature's");
                foreach (var flag in entry.Flags)
                    if (flag > int.MaxValue || !Enum.IsDefined(typeof(CreatureFlag), (int)flag))
                        answer.Problems.Add($"class {entry.ClassId}: {flag} is not a creature flag");
            }
            if (!answer.Done || classes.Count == 0) return answer;
            lock (_writing)
            {
                using (var unitOfWork = _factory.CreateWorld())
                    unitOfWork.Creatures.ReplaceClassFlags(classes.ToDictionary(
                        entry => entry.ClassId, entry => (IReadOnlyCollection<uint>)entry.Flags));
                foreach (var entry in classes)
                    loaded[(EntityClasses)entry.ClassId].CreatureFlags = entry.Flags.Select(flag => (CreatureFlag)flag).ToList();
            }
            return answer;
        }
    }
}
