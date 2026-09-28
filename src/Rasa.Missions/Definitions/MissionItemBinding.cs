using Rasa.Missions.Scenes;
using System.Text.Json.Serialization;

namespace Rasa.Missions.Definitions
{
    public enum MissionItemCleanupDisposition { Remove, Retain }

    public sealed record MissionItemBinding(
        [property: JsonRequired] string ItemKey,
        [property: JsonRequired] uint ItemTemplateId,
        [property: JsonRequired] MissionItemScope Scope,
        [property: JsonRequired] uint MaximumQuantity,
        [property: JsonRequired] MissionItemCleanupDisposition Completion,
        [property: JsonRequired] MissionItemCleanupDisposition Failure,
        [property: JsonRequired] MissionItemCleanupDisposition Abandonment);
}
