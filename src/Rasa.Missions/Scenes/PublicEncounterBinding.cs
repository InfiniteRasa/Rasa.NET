using System.Text.Json.Serialization;

namespace Rasa.Missions.Scenes
{
    public sealed record PublicEncounterBinding(
        uint MissionId, uint SpawnId, string Role, string ScriptKey,
        string OwnerLossPolicy = "Reset", bool IncludeEligibleParty = false,
        [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] bool AllowPartyJoin = false);
}
