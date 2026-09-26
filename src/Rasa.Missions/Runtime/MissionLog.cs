using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Rasa.Structures
{
    using Data;

    public class MissionLog
    {
        public uint MissionId { get; }
        public string AssignmentId { get; }
        public string ContentRevision { get; }
        public uint Generation { get; }
        public MissionState State { get; set; }
        public bool Completeable { get; set; }
        public IReadOnlyDictionary<uint, MissionObjectiveLog> Objectives { get; }

        public MissionLog(
            uint missionId,
            MissionState state,
            bool completeable,
            IReadOnlyDictionary<uint, MissionObjectiveLog> objectives = null,
            string assignmentId = null,
            uint generation = 0,
            string contentRevision = null)
        {
            MissionId = missionId;
            AssignmentId = assignmentId;
            Generation = generation;
            ContentRevision = contentRevision;
            State = state;
            Completeable = state == MissionState.Active && completeable;
            Objectives = new ReadOnlyDictionary<uint, MissionObjectiveLog>(
                new Dictionary<uint, MissionObjectiveLog>(
                    objectives ?? new Dictionary<uint, MissionObjectiveLog>()));
        }

        public bool MatchesAssignment(string assignmentId, uint generation, string contentRevision) =>
            !string.IsNullOrEmpty(AssignmentId) && Generation != 0 &&
            AssignmentId == assignmentId && Generation == generation && ContentRevision == contentRevision;
    }
}
