namespace Rasa.Structures.Char
{
    public sealed class CharacterMissionItemEntry
    {
        public uint CharacterId { get; set; }
        public uint MissionId { get; set; }
        public string AssignmentId { get; set; }
        public uint Generation { get; set; }
        public string ItemKey { get; set; }
        public uint ItemId { get; set; }
        public uint Quantity { get; set; }
    }

    public sealed class CharacterMissionItemReceiptEntry
    {
        public uint CharacterId { get; set; }
        public uint MissionId { get; set; }
        public string AssignmentId { get; set; }
        public uint Generation { get; set; }
        public string OperationKey { get; set; }
        public string Payload { get; set; }
    }

    public sealed class CharacterMissionItemQuarantineEntry
    {
        public uint CharacterId { get; set; }
        public uint MissionId { get; set; }
        public string AssignmentId { get; set; }
        public string Reason { get; set; }
    }
}
