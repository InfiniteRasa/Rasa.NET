using System.Collections.Generic;

namespace Rasa.Repositories.World
{
    using Context.World;
    using Structures.World;
    using System.Linq;

    public interface IFootlockerRepository
    {
        List<FootlockerEntry> GetFootlockers();

        /// <summary>Adds a row; its id is the row's own. 0 if it was not added.</summary>
        uint AddFootlocker(FootlockerEntry entry);

        /// <summary>Stands a row's object somewhere else. False if there is no such row.</summary>
        bool UpdatePosition(uint id, double x, double y, double z, double rotation);

        /// <summary>False if there is no such row.</summary>
        bool DeleteFootlocker(uint id);
    }
    public class FootlockerRepository : IFootlockerRepository
    {
        private readonly WorldContext _worldContext;

        public FootlockerRepository(WorldContext worldContext)
        {
            _worldContext = worldContext;
        }

        public List<FootlockerEntry> GetFootlockers()
        {
            var query = _worldContext.CreateNoTrackingQuery(_worldContext.FootlockerEntries);
            var entries = query.ToList();


            return entries;
        }

        public uint AddFootlocker(FootlockerEntry entry)
        {
            _worldContext.FootlockerEntries.Add(entry);
            _worldContext.SaveChanges();

            return entry.Id;
        }

        public bool UpdatePosition(uint id, double x, double y, double z, double rotation)
        {
            var row = _worldContext.CreateTrackingQuery(_worldContext.FootlockerEntries).FirstOrDefault(e => e.Id == id);

            if (row == null)
                return false;

            row.PosX = x;
            row.PosY = y;
            row.PosZ = z;
            row.Rotation = rotation;
            _worldContext.SaveChanges();

            return true;
        }

        public bool DeleteFootlocker(uint id)
        {
            var row = _worldContext.CreateTrackingQuery(_worldContext.FootlockerEntries).FirstOrDefault(e => e.Id == id);

            if (row == null)
                return false;

            _worldContext.FootlockerEntries.Remove(row);
            _worldContext.SaveChanges();

            return true;
        }
    }
}
