using System.Collections.Generic;
using System.Linq;

namespace Rasa.Repositories.World
{
    using Context.World;
    using Structures.World;

    public interface IControlPointRepository
    {
        List<ControlPointEntry> GetControlPoints();
        List<ControlPointLinkEntry> GetLinks();

        /// <summary>Moves a control point (".cp move"). False if there is no such row.</summary>
        bool UpdatePosition(uint id, double x, double y, double z, double rotation);
    }

    public class ControlPointRepository : IControlPointRepository
    {
        private readonly WorldContext _worldContext;

        public ControlPointRepository(WorldContext worldContext)
        {
            _worldContext = worldContext;
        }

        public List<ControlPointEntry> GetControlPoints() =>
            _worldContext.CreateNoTrackingQuery(_worldContext.ControlPointEntries).OrderBy(e => e.Id).ToList();

        public List<ControlPointLinkEntry> GetLinks() =>
            _worldContext.CreateNoTrackingQuery(_worldContext.ControlPointLinkEntries).ToList();

        public bool UpdatePosition(uint id, double x, double y, double z, double rotation)
        {
            var row = _worldContext.CreateTrackingQuery(_worldContext.ControlPointEntries).FirstOrDefault(e => e.Id == id);

            if (row == null)
                return false;

            row.PosX = x;
            row.PosY = y;
            row.PosZ = z;
            row.Rotation = rotation;
            _worldContext.SaveChanges();

            return true;
        }
    }
}
