using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace Rasa.Test.Missions
{
    using Rasa.Context.World;
    using Rasa.Repositories.World;
    using Rasa.Structures.World;

    /// <summary>
    /// The mission content of a test harness's World, which a test can stop at an earlier
    /// migration. A World stopped before Add_mission_reward_item_modules has reward items with
    /// no module columns, and its items are read with no modules, as a migrated World's rows
    /// start.
    /// </summary>
    internal sealed class MissionContentOfThisWorld : MissionContentRepository, IMigratedMissionContentRepository
    {
        private readonly SqliteWorldContext _context;

        internal MissionContentOfThisWorld(SqliteWorldContext context) : base(context) => _context = context;

        List<MissionRewardItemEntry> IMissionContentRepository.GetRewardItems()
        {
            const string table = MissionRewardItemEntry.TableName;

            var hasModules = _context.Database
                .SqlQueryRaw<int>("SELECT COUNT(*) AS Value FROM pragma_table_info({0}) WHERE name = 'module_1'", table)
                .AsEnumerable().Single() > 0;

            return hasModules
                ? GetRewardItems()
                : _context.MissionRewardItemEntries
                    .FromSqlRaw($"SELECT mission_id, content_revision, reward_id, item_id, kind, item_template_id, quantity, " +
                        $"0 AS module_1, 0 AS module_2, 0 AS module_3, 0 AS module_4 FROM {table}")
                    .AsNoTracking().ToList();
        }
    }
}
