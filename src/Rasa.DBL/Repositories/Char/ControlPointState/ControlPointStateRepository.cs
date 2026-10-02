using System.Collections.Generic;
using System.Linq;

namespace Rasa.Repositories.Char.ControlPointState
{
    using Context.Char;
    using Structures.Char;

    /// <summary>Who holds each control point that has changed hands. Every call saves at once.</summary>
    public interface IControlPointStateRepository
    {
        List<ControlPointStateEntry> GetStates();

        /// <summary>Adds the point's row, or updates it.</summary>
        void SaveState(uint controlPointId, byte owner, long changedAt);
    }

    public class ControlPointStateRepository : IControlPointStateRepository
    {
        private readonly CharContext _charContext;

        public ControlPointStateRepository(CharContext charContext)
        {
            _charContext = charContext;
        }

        public List<ControlPointStateEntry> GetStates() =>
            _charContext.CreateNoTrackingQuery(_charContext.ControlPointStateEntries).OrderBy(e => e.ControlPointId).ToList();

        public void SaveState(uint controlPointId, byte owner, long changedAt)
        {
            var row = _charContext.CreateTrackingQuery(_charContext.ControlPointStateEntries).FirstOrDefault(e => e.ControlPointId == controlPointId);

            if (row == null)
                _charContext.ControlPointStateEntries.Add(new ControlPointStateEntry { ControlPointId = controlPointId, Owner = owner, ChangedAt = changedAt });
            else
            {
                row.Owner = owner;
                row.ChangedAt = changedAt;
            }

            _charContext.SaveChanges();
        }
    }
}
