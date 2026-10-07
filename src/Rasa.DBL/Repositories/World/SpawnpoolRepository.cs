using System.Collections.Generic;
using System.Linq;

namespace Rasa.Repositories.World
{
    using Context.World;
    using Structures.World;

    public interface ISpawnpoolRepository
    {
        List<SpawnPoolEntry> Get();

        /// <summary>The pools that have a pose (spawnpool_pose).</summary>
        List<SpawnPoolPoseEntry> GetPoses();
    }
    public class SpawnpoolRepository : ISpawnpoolRepository
    {
        private readonly WorldContext _worldContext;

        public SpawnpoolRepository(WorldContext worldContext)
        {
            _worldContext = worldContext;
        }

        public List<SpawnPoolEntry> Get()
        {
            var query = _worldContext.CreateNoTrackingQuery(_worldContext.SpawnPoolEntries);
            var spawnPoolEntries = query.ToList();

            return spawnPoolEntries;
        }

        public List<SpawnPoolPoseEntry> GetPoses()
        {
            return _worldContext.CreateNoTrackingQuery(_worldContext.SpawnPoolPoseEntries).ToList();
        }
    }
}
