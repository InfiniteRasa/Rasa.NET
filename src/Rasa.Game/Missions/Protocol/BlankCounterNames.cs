using System.Collections.Generic;
using Rasa.Structures;

namespace Rasa.Managers
{
    /// <summary>
    /// The generic counters whose name the client has blank.
    ///
    /// The client names a generic counter by the text in its missionobjective row's counter slot
    /// (clientlanguagemanager.BuildObjectiveGenericCounterText), in two places: the mission
    /// tracker's bar under the objective, and the big text Recv_UpdateObjectiveCounter puts up,
    /// OBJECTIVE_COUNTER_UPDATED_WITH_TARGET, "%(counterName)s: %(curValue)s/%(targetValue)s"
    /// (playermessagelanguage 641). For these 47 missiontextlanguage ids that text is empty in
    /// every language the client ships (eleven spaces in German): a bar under its objective's own
    /// title, where the name is not missed, and a big text that reads ": 3/5" - Survey Says (508)
    /// objective 7 "Retrieve the survey data", and in the Wilderness also Childhood's End (682)
    /// objective 3 and Traveling Medicine Show (700 and 820) objective 5.
    ///
    /// Such a counter's value goes to the client in the mission status snapshot instead
    /// (MissionApplication.PublishMissionStatus), which moves the tracker's bar and puts up no big
    /// text (missionlog.Recv_MissionStatusInfo).
    /// </summary>
    internal static class BlankCounterNames
    {
        /// <summary>missiontextlanguage ids in a missionobjective counter slot whose text is blank.</summary>
        internal static readonly IReadOnlySet<uint> TextIds = new HashSet<uint>
        {
            1404, 1532, 1538, 1734, 1803, 2220, 2585, 2700, 2715, 2939, 3172, 3294, 4091, 6168, 6169, 6177,
            6200, 7517, 11793, 11839, 12030, 12033, 12036, 12430, 13685, 14626, 14704, 15219, 15467, 15527,
            15616, 16205, 16224, 17307, 17373, 17514, 17589, 17590, 17601, 18630, 18885, 19545, 20586,
            20587, 20602, 21682, 21870
        };

        /// <summary>Whether the client shows this objective's counter with no name.</summary>
        internal static bool Has(MissionObjectiveDefinition objective, uint counterId) =>
            objective != null &&
            counterId < (uint)objective.ClientCounterTextIds.Count &&
            objective.ClientCounterTextIds[(int)counterId] is uint textId &&
            TextIds.Contains(textId);
    }
}
