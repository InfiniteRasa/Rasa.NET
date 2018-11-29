namespace Rasa.Structures
{
    using Data;

    public class ResistanceData
    {
        public DamageType ResistanceType { get; set; }
        public int ResistanceAmmount { get; set; }

        public ResistanceData(DamageType resistType, int ammount)
        {
            ResistanceType = resistType;
            ResistanceAmmount = ammount;
        }
    }
}
