namespace Rasa.Structures
{
    using Data;

    public class ActorAttributes
    {
        public Attributes AttributeId { get; set; }
        public int NormalMax { get; set; }
        public int CurrentMax { get; set; }
        public int Current { get; set; }
        public int RefreshAmount { get; set; }
        public int RefreshPeriod { get; set; }

        public ActorAttributes(Attributes attributeId, int normalMax, int currentMax, int current, int refreshAmmount, int refreshPeriod)
        {
            AttributeId = attributeId;
            NormalMax = normalMax;
            CurrentMax = currentMax;
            Current = current;
            RefreshAmount = refreshAmmount;
            RefreshPeriod = refreshPeriod;
        }
    }
}
