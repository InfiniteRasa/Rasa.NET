using System.Numerics;

namespace Rasa.Structures
{
    public sealed class MissionIndicator
    {
        /// <summary>
        /// Indicator ids from here up have no name on the client. Below it an id is a
        /// missionobjectiveindicatorlanguage id, the name the client's map and radar show for the
        /// indicator; the client's own ids end far below (441, and 10000001..10000005).
        /// </summary>
        public const uint UnnamedFrom = 1_000_000_000;

        public Vector3 Position { get; init; }
        public double Radius { get; init; }
        public uint IndicatorId { get; init; }
        /// <summary>
        /// The id sent to the client: null, sent as None, for an indicator without a client name.
        /// The client then names it by its objective (mapwindow.py and radarwindow.py,
        /// OnMissionMarkerHighlighted).
        /// </summary>
        public uint? ClientNameId => IndicatorId < UnnamedFrom ? (uint?)IndicatorId : null;
        public bool Show3DEffect { get; init; }
    }
}
