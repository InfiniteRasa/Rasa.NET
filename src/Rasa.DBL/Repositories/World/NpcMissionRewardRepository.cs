using System.Collections.Generic;
using System.Linq;

namespace Rasa.Repositories.World
{
    using Context.World;
    using Structures.World;

    public interface INpcMissionRewardRepository
    {
        List<NpcMissionRewardEntry> Get(uint missionId);
    }

    public class NpcMissionRewardRepository : INpcMissionRewardRepository
    {
        private readonly WorldContext _worldContext;

        public NpcMissionRewardRepository(WorldContext worldContext)
        {
            _worldContext = worldContext;
        }
        public List<NpcMissionRewardEntry> Get(uint missionId)
        {
            var query = _worldContext.CreateNoTrackingQuery(_worldContext.NpcMissionRewardEntries);
            var entryes = query.Where(e => e.Id == missionId).ToList();

            return entryes;
        }
    }
}
