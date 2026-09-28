using System;
using System.Linq;
using Rasa.Context.Char;

namespace Rasa.Repositories.Char.CharacterMissionItem
{
    internal static class MissionItemMutationGuard
    {
        internal static void RequireUnbound(CharContext context, uint itemId)
        {
            if (context.CharacterMissionItemEntries.Any(entry => entry.ItemId == itemId))
                throw new InvalidOperationException("Assignment-owned items require a transactional mission item plan.");
        }
    }
}
