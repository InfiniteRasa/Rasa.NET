using System.Collections.Generic;

namespace Rasa.Missions.Content
{
    public enum MissionAudioEvent { Accepted, Completed }

    public sealed class MissionAudioDefinition
    {
        public uint? OfferAudioSetId { get; set; }
        public Dictionary<MissionAudioEvent, uint> Events { get; set; } = new();
        public Dictionary<uint, uint> Announcements { get; set; } = new();
    }
}
