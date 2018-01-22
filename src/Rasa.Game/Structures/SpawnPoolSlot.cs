namespace Rasa.Structures
{
    public class SpawnPoolSlot
    {
        public int CreatureId { get; set; }
        public short CountMin { get; set; }
        public short CountMax { get; set; }

        public SpawnPoolSlot(int creatureId, short countMin, short countMax)
        {
            CreatureId = creatureId;
            CountMin = countMin;
            CountMax = countMax;
        }
    }
}
