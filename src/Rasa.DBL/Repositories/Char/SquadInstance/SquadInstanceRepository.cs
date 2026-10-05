using System.Collections.Generic;
using System.Linq;

namespace Rasa.Repositories.Char.SquadInstance
{
    using Context.Char;
    using Structures.Char;

    /// <summary>
    /// The squad instances that stand, the spawn pools of each that are dead, and the instance
    /// each character last went into. Every call saves at once.
    /// </summary>
    public interface ISquadInstanceRepository
    {
        SquadInstanceEntry Get(uint id);

        /// <summary>The instance of a map that is this character's, or null.</summary>
        SquadInstanceEntry Find(uint mapContextId, uint ownerCharacterId);

        List<SquadInstanceEntry> GetAll();

        /// <summary>Adds the owner's instance of the map; the one there already, if there is one.</summary>
        SquadInstanceEntry Create(uint mapContextId, uint ownerCharacterId, long createdAt);

        /// <summary>Makes an instance another character's. False when it is gone, or they have one of that map.</summary>
        bool SetOwner(uint id, uint ownerCharacterId);

        void SetCreatedAt(uint id, long createdAt);

        /// <summary>Deletes an instance, its pools and who was last in it. False when it was gone.</summary>
        bool Delete(uint id);

        /// <summary>
        /// Deletes every instance but those named - made before a time, when one is given - with
        /// their pools and who was last in them. Returns how many instances went.
        /// </summary>
        int DeleteAllExcept(ICollection<uint> keep, long? createdBefore);

        /// <summary>The dead pools of an instance, and when each died.</summary>
        Dictionary<uint, long> GetPools(uint instanceId);

        void SetPoolCleared(uint instanceId, uint spawnpoolId, long clearedAt);

        void RemovePool(uint instanceId, uint spawnpoolId);

        void RemovePools(uint instanceId);

        /// <summary>The instance a character last went into; 0 for none.</summary>
        uint GetVisitorInstance(uint characterId);

        void SetVisitorInstance(uint characterId, uint instanceId);
    }

    public class SquadInstanceRepository : ISquadInstanceRepository
    {
        private readonly CharContext _charContext;

        public SquadInstanceRepository(CharContext charContext)
        {
            _charContext = charContext;
        }

        public SquadInstanceEntry Get(uint id) =>
            _charContext.CreateNoTrackingQuery(_charContext.SquadInstanceEntries).FirstOrDefault(e => e.Id == id);

        public SquadInstanceEntry Find(uint mapContextId, uint ownerCharacterId) =>
            _charContext.CreateNoTrackingQuery(_charContext.SquadInstanceEntries)
                .Where(e => e.MapContextId == mapContextId && e.OwnerCharacterId == ownerCharacterId)
                .OrderBy(e => e.Id)
                .FirstOrDefault();

        public List<SquadInstanceEntry> GetAll() =>
            _charContext.CreateNoTrackingQuery(_charContext.SquadInstanceEntries).OrderBy(e => e.Id).ToList();

        public SquadInstanceEntry Create(uint mapContextId, uint ownerCharacterId, long createdAt)
        {
            if (mapContextId == 0 || ownerCharacterId == 0)
                throw new System.ArgumentOutOfRangeException(mapContextId == 0 ? nameof(mapContextId) : nameof(ownerCharacterId));

            var existing = Find(mapContextId, ownerCharacterId);

            if (existing != null)
                return existing;

            var entry = new SquadInstanceEntry { MapContextId = mapContextId, OwnerCharacterId = ownerCharacterId, CreatedAt = createdAt };

            _charContext.SquadInstanceEntries.Add(entry);
            _charContext.SaveChanges();

            return entry;
        }

        public bool SetOwner(uint id, uint ownerCharacterId)
        {
            var row = _charContext.CreateTrackingQuery(_charContext.SquadInstanceEntries).FirstOrDefault(e => e.Id == id);

            if (row == null || ownerCharacterId == 0)
                return false;

            if (row.OwnerCharacterId == ownerCharacterId)
                return true;

            if (Find(row.MapContextId, ownerCharacterId) != null)
                return false;

            row.OwnerCharacterId = ownerCharacterId;
            _charContext.SaveChanges();

            return true;
        }

        public void SetCreatedAt(uint id, long createdAt)
        {
            var row = _charContext.CreateTrackingQuery(_charContext.SquadInstanceEntries).FirstOrDefault(e => e.Id == id);

            if (row == null)
                return;

            row.CreatedAt = createdAt;
            _charContext.SaveChanges();
        }

        public bool Delete(uint id) => Delete(new[] { id }) > 0;

        public int DeleteAllExcept(ICollection<uint> keep, long? createdBefore)
        {
            var ids = _charContext.CreateNoTrackingQuery(_charContext.SquadInstanceEntries)
                .Where(e => createdBefore == null || e.CreatedAt < createdBefore)
                .Select(e => e.Id)
                .ToList()
                .Where(id => keep == null || !keep.Contains(id))
                .ToList();

            return Delete(ids);
        }

        private int Delete(ICollection<uint> ids)
        {
            if (ids.Count == 0)
                return 0;

            var instances = _charContext.CreateTrackingQuery(_charContext.SquadInstanceEntries).Where(e => ids.Contains(e.Id)).ToList();

            _charContext.SquadInstancePoolEntries.RemoveRange(
                _charContext.CreateTrackingQuery(_charContext.SquadInstancePoolEntries).Where(e => ids.Contains(e.InstanceId)).ToList());
            _charContext.SquadInstanceVisitorEntries.RemoveRange(
                _charContext.CreateTrackingQuery(_charContext.SquadInstanceVisitorEntries).Where(e => ids.Contains(e.InstanceId)).ToList());
            _charContext.SquadInstanceEntries.RemoveRange(instances);
            _charContext.SaveChanges();

            return instances.Count;
        }

        public Dictionary<uint, long> GetPools(uint instanceId) =>
            _charContext.CreateNoTrackingQuery(_charContext.SquadInstancePoolEntries)
                .Where(e => e.InstanceId == instanceId)
                .ToDictionary(e => e.SpawnpoolId, e => e.ClearedAt);

        public void SetPoolCleared(uint instanceId, uint spawnpoolId, long clearedAt)
        {
            if (instanceId == 0 || spawnpoolId == 0)
                return;

            var row = _charContext.CreateTrackingQuery(_charContext.SquadInstancePoolEntries)
                .FirstOrDefault(e => e.InstanceId == instanceId && e.SpawnpoolId == spawnpoolId);

            if (row == null)
                _charContext.SquadInstancePoolEntries.Add(new SquadInstancePoolEntry { InstanceId = instanceId, SpawnpoolId = spawnpoolId, ClearedAt = clearedAt });
            else
                row.ClearedAt = clearedAt;

            _charContext.SaveChanges();
        }

        public void RemovePool(uint instanceId, uint spawnpoolId)
        {
            var row = _charContext.CreateTrackingQuery(_charContext.SquadInstancePoolEntries)
                .FirstOrDefault(e => e.InstanceId == instanceId && e.SpawnpoolId == spawnpoolId);

            if (row == null)
                return;

            _charContext.SquadInstancePoolEntries.Remove(row);
            _charContext.SaveChanges();
        }

        public void RemovePools(uint instanceId)
        {
            var rows = _charContext.CreateTrackingQuery(_charContext.SquadInstancePoolEntries).Where(e => e.InstanceId == instanceId).ToList();

            if (rows.Count == 0)
                return;

            _charContext.SquadInstancePoolEntries.RemoveRange(rows);
            _charContext.SaveChanges();
        }

        public uint GetVisitorInstance(uint characterId) =>
            _charContext.CreateNoTrackingQuery(_charContext.SquadInstanceVisitorEntries)
                .Where(e => e.CharacterId == characterId)
                .Select(e => e.InstanceId)
                .FirstOrDefault();

        public void SetVisitorInstance(uint characterId, uint instanceId)
        {
            if (characterId == 0)
                return;

            var row = _charContext.CreateTrackingQuery(_charContext.SquadInstanceVisitorEntries).FirstOrDefault(e => e.CharacterId == characterId);

            if (row == null)
            {
                if (instanceId == 0)
                    return;

                _charContext.SquadInstanceVisitorEntries.Add(new SquadInstanceVisitorEntry { CharacterId = characterId, InstanceId = instanceId });
            }
            else if (instanceId == 0)
                _charContext.SquadInstanceVisitorEntries.Remove(row);
            else if (row.InstanceId == instanceId)
                return;
            else
                row.InstanceId = instanceId;

            _charContext.SaveChanges();
        }
    }
}
