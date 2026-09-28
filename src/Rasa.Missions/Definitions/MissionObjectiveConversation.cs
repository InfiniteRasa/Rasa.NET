namespace Rasa.Structures
{
    using Data;

    public sealed class MissionObjectiveConversation
    {
        public uint NpcPackageId { get; }
        public uint PlayerFlagId { get; }
        public MissionObjectiveConversationType Type { get; }

        public MissionObjectiveConversation(
            uint npcPackageId,
            uint playerFlagId,
            MissionObjectiveConversationType type)
        {
            NpcPackageId = npcPackageId;
            PlayerFlagId = playerFlagId;
            Type = type;
        }
    }
}
