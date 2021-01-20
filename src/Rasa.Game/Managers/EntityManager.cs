using System;
using System.Linq;

namespace Rasa.Managers
{
    using Pooling;

    public class EntityManager : IEntityManager
    {
        private readonly object _lock = new object();
        private readonly ObjectPool<ulong> _entityIdPool;

        ulong _entityIdIterator = 1000;

        public EntityManager()
        {
            _entityIdPool = new ObjectPool<ulong>(() => NewEntityId());
        }

        public ulong GetEntityId()
        {
            return _entityIdPool.Get();
        }

        public void ReturnEntityId(ulong entityId)
        {
            _entityIdPool.Return(entityId);
        }

        private ulong NewEntityId()
        {
            lock (_lock)
            {
                _entityIdIterator += 1;
            }

            return _entityIdIterator;
        }
    }
}
