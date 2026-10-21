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
        /// <summary>
        /// The key of a ServerEvent source that offers its mission to a character arriving on the
        /// source's map: the game server raises it for every character who comes onto a map.
        /// </summary>
        public const string MapArrivalKey = "map.arrival";

        /// <summary>
        /// The key of a ServerEvent source that offers its mission by radio when the character is
        /// rewarded for another mission, as Headquarters calls after a turn-in. The id of that
        /// mission follows the prefix: "mission.rewarded:867". The key carries the id, so one
        /// mission can have more than one such source and the offer row needs no new column.
        /// </summary>
        public const string MissionRewardedPrefix = "mission.rewarded:";

        public static string MissionRewardedKey(uint missionId) =>
            MissionRewardedPrefix + missionId.ToString(System.Globalization.CultureInfo.InvariantCulture);

        /// <summary>
        /// Reads the mission id of a mission-rewarded key. Only the exact form MissionRewardedKey
        /// writes is accepted: digits only, no sign, no leading zero, and a nonzero id.
        /// </summary>
        public static bool TryParseMissionRewarded(string key, out uint missionId)
        {
            missionId = 0;
            if (key == null || !key.StartsWith(MissionRewardedPrefix, StringComparison.Ordinal))
                return false;
            var digits = key.Substring(MissionRewardedPrefix.Length);
            if (digits.Length == 0 || !digits.All(character => character is >= '0' and <= '9') ||
                !uint.TryParse(digits, System.Globalization.NumberStyles.None,
                    System.Globalization.CultureInfo.InvariantCulture, out var parsed) ||
                parsed == 0 || MissionRewardedKey(parsed) != key)
                return false;
            missionId = parsed;
            return true;
        }

        public static MissionOfferSourceDefinition MissionRewarded(uint afterMissionId, uint? mapContextId = null,
            MissionRequirement requirement = null) =>
            new(MissionOfferSourceKind.ServerEvent, MissionRewardedKey(afterMissionId), mapContextId, false, requirement);

        [System.Text.Json.Serialization.JsonIgnore]
        public string ValidationError =>
            Kind is not (MissionOfferSourceKind.ServerEvent or MissionOfferSourceKind.Scene) ? "unknown radio source kind" :
            string.IsNullOrWhiteSpace(Key) || Key.Length > 96 ? "radio source key must contain 1-96 characters" :
            MapContextId == 0 ? "radio source map must be nonzero" :
            Key.StartsWith(MissionRewardedPrefix, StringComparison.Ordinal) &&
                (Kind != MissionOfferSourceKind.ServerEvent || !TryParseMissionRewarded(Key, out _))
                ? "a mission-rewarded radio source must be a ServerEvent with a nonzero mission id" : null;
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
