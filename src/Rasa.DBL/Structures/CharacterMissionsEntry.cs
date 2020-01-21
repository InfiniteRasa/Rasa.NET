namespace Rasa.Structures
{
    public partial class CharacterMissionsEntry
    {
        public int CharacterSlot { get; set; }
        public uint AccountId { get; set; }
        public int MissionId { get; set; }
        public short MissionState { get; set; }
    }
}
