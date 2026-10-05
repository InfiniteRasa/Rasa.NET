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

        /// <summary>Adds the point's row, or updates it: every column of it.</summary>
        void SaveState(ControlPointStateEntry state);
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

        public void SaveState(ControlPointStateEntry state)
        {
            var row = _charContext.CreateTrackingQuery(_charContext.ControlPointStateEntries).FirstOrDefault(e => e.ControlPointId == state.ControlPointId);

            if (row == null)
                _charContext.ControlPointStateEntries.Add(new ControlPointStateEntry
                {
                    ControlPointId = state.ControlPointId,
                    Owner = state.Owner,
                    ChangedAt = state.ChangedAt,
                    ClanId = state.ClanId,
                    ClanPaidAt = state.ClanPaidAt
                });
            else
            {
                row.Owner = state.Owner;
                row.ChangedAt = state.ChangedAt;
                row.ClanId = state.ClanId;
                row.ClanPaidAt = state.ClanPaidAt;
            }

            _charContext.SaveChanges();
        }
    }
}
