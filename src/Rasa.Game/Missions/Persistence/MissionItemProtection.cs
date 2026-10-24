using System;
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

    /// <summary>
    /// A character unit of work made the first time it is needed, for a scan that asks
    /// MissionItemProtection of several items - a reload looking through the pack for ammo -
    /// and may then save with it: one unit of work for all of it, where the factory overload of
    /// IsProtected makes one for each item, and none at all when no item needs a row read.
    /// </summary>
    internal sealed class LazyCharUnit : IDisposable
    {
        private readonly IGameUnitOfWorkFactory _factory;
        private ICharUnitOfWork _unit;

        internal LazyCharUnit(IGameUnitOfWorkFactory factory)
        {
            _factory = factory;
        }

        /// <summary>The unit of work, made now if it has not been.</summary>
        internal ICharUnitOfWork Unit => _unit ??= _factory.CreateChar();

        /// <summary>MissionItemProtection.IsProtected, through this unit of work when a row has to be read.</summary>
        internal bool IsProtected(Item item)
        {
            if (item?.MissionOwnership != null)
                return true;
            if (item == null || item.Id == 0)
                return false;
            return MissionItemProtection.IsProtected(item, Unit);
        }

        public void Dispose()
        {
            _unit?.Dispose();
        }
    }
}
