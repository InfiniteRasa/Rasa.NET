namespace Rasa.Structures
{
    public class AbilityDrawerData
    {
        public int AbilityId { get; set; }
        public int AbilitySlotId { get; set; }
        public uint AbilityLevel { get; set; }

        public AbilityDrawerData(int abilitySlotId, int abilityId, uint abilityLevel)
        {
            AbilityId = abilityId;
            AbilitySlotId = abilitySlotId;
            AbilityLevel = abilityLevel;
        }
    }
}
