using System;
using System.Collections.Generic;
using System.Linq;
using Rasa.Missions.Runtime;
using Rasa.Structures;

namespace Rasa.Missions.Definitions
{
    [Flags]
    public enum MissionChannel { Npc = 1, Radio = 2, Mixed = Npc | Radio }

    public enum MissionOfferSourceKind { ServerEvent, Scene, Party }

    public static class MissionChannelValidation
    {
        public static IEnumerable<string> Errors(Mission mission)
        {
            if (!Enum.IsDefined(typeof(MissionChannel), mission.AcceptanceChannel) ||
                !Enum.IsDefined(typeof(MissionChannel), mission.CompletionChannel))
                yield return "mission acceptance/completion channel is invalid";
            if (mission.AcceptanceChannel.HasFlag(MissionChannel.Npc) && !mission.MissionGiver.HasValue ||
                mission.CompletionChannel.HasFlag(MissionChannel.Npc) && !mission.MissionReciver.HasValue)
                yield return "an NPC channel requires its corresponding giver or receiver ID";
            if (mission.RadioSources.Any(source => source == null || source.ValidationError != null) ||
                mission.RadioSources.GroupBy(source => (source?.Kind, source?.Key)).Any(group => group.Count() > 1))
                yield return "radio sources are invalid or duplicated";
            if (mission.RadioSources.Count > 0 && !mission.AcceptanceChannel.HasFlag(MissionChannel.Radio))
                yield return "radio sources require a radio acceptance channel";
            if (mission.AcceptanceChannel.HasFlag(MissionChannel.Radio) && mission.RadioSources.Count == 0)
                yield return "radio acceptance requires an authored source";
        }
    }

    public sealed record MissionOfferSourceDefinition(MissionOfferSourceKind Kind, string Key,
        uint? MapContextId = null, bool OwnedPrivateMap = false, MissionRequirement Requirement = null)
    {
        [System.Text.Json.Serialization.JsonIgnore]
        public string ValidationError =>
            Kind is not (MissionOfferSourceKind.ServerEvent or MissionOfferSourceKind.Scene) ? "unknown radio source kind" :
            string.IsNullOrWhiteSpace(Key) || Key.Length > 96 ? "radio source key must contain 1-96 characters" :
            MapContextId == 0 ? "radio source map must be nonzero" : null;
    }

    public sealed record MissionOfferSourceIdentity(MissionOfferSourceKind Kind, string Key, string InstanceId,
        uint Generation = 0, string AssignmentId = null, uint AssignmentGeneration = 0,
        MissionPartyOfferSource Party = null)
    {
        public static MissionOfferSourceIdentity ServerEvent(string key, string eventId = null) =>
            new(MissionOfferSourceKind.ServerEvent, key, eventId ?? key);
    }

    public sealed record MissionPartyOfferSource(uint PartyId, Guid PartyLifetime,
        Guid SourceMembership, Guid RecipientMembership, uint SourceCharacterId, uint SourceAccountId,
        ulong SourceEntityId, Guid SourceSessionId, Guid SourcePlayerEpoch, Guid SourceMapEpoch);
}
