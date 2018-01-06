namespace Rasa.Structures
{
    public class SpawnPoolSlot
    {
        public uint CreatureId { get; set; }
        public short CountMin { get; set; }
        public short CountMax { get; set; }

        public SpawnPoolSlot(uint creatureId, short countMin, short countMax)
        {
            CreatureId = creatureId;
            CountMin = countMin;
            CountMax = countMax;
        }
    }
}
