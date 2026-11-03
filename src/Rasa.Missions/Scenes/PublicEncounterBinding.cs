using System.Text.Json.Serialization;

namespace Rasa.Missions.Scenes
{
    public sealed record PublicEncounterBinding(
        uint MissionId, uint SpawnId, string Role, string ScriptKey,
        string OwnerLossPolicy = "Reset", bool IncludeEligibleParty = false,
        [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] bool AllowPartyJoin = false,
        [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] bool ManualCombat = false,
        // ManualCombat only while a run holds the actor: unreserved, it is an ordinary creature
        // that fights and can be killed by anyone (PublicActorLeaseService.BindCombatGate).
        [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] bool FreeWhenUnreserved = false);
}
