using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.Json.Serialization;

namespace Rasa.Missions.Definitions
{
    public enum MissionDialogueKind { Completion, Reminder, Ambient, Choice }

    public sealed record MissionDialogueTopicDefinition
    {
        public uint ObjectiveId { get; }
        public uint NpcPackageId { get; }
        public uint PlayerFlagId { get; }
        public MissionDialogueKind Kind { get; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public uint? DialogObjectiveId { get; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public uint? TransitionId { get; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public IReadOnlyDictionary<int, uint> Choices { get; }

        [JsonConstructor]
        public MissionDialogueTopicDefinition(uint objectiveId, uint npcPackageId, uint playerFlagId,
            MissionDialogueKind kind = MissionDialogueKind.Completion, uint? transitionId = null,
            IReadOnlyDictionary<int, uint> choices = null, uint? dialogObjectiveId = null)
        {
            ObjectiveId = objectiveId;
            NpcPackageId = npcPackageId;
            PlayerFlagId = playerFlagId;
            Kind = kind;
            TransitionId = transitionId;
            DialogObjectiveId = dialogObjectiveId;
            Choices = choices == null ? null :
                new ReadOnlyDictionary<int, uint>(new Dictionary<int, uint>(choices));
        }
    }
}
