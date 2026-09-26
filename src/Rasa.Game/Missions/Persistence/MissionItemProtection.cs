using Rasa.Repositories.Char;
using Rasa.Repositories.UnitOfWork;
using Rasa.Structures;

namespace Rasa.Game.Missions.Persistence
{
    internal static class MissionItemProtection
    {
        internal static bool IsProtected(Item item, ICharUnitOfWork unit) =>
            item?.MissionOwnership != null || item?.Id > 0 && unit.CharacterMissionItems.GetOwner(item.Id) != null;

        internal static bool IsProtected(Item item, IGameUnitOfWorkFactory factory)
        {
            if (item?.MissionOwnership != null)
                return true;
            if (item == null || item.Id == 0)
                return false;
            using var unit = factory.CreateChar();
            return IsProtected(item, unit);
        }
    }
}
