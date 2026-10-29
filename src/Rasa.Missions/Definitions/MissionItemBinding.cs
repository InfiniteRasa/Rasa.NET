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
        [property: JsonRequired] MissionItemCleanupDisposition Abandonment,
        [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] uint TurnInQuantity = 0,
        [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] MissionItemDropDefinition Drop = null,
        // An assignment-issued item whose held count the client shows on this objective, while it is open, as the
        // objective's item counter ("n/x" with the item's name; IssuedItemProgress). 0: none.
        [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] uint CounterObjectiveId = 0);
}
