namespace Rasa.Structures
{
    using Data;

    public class Curency
    {
        public CurencyType CurencyType { get; set; }
        public int Amount { get; set; }

        public Curency(CurencyType curencyType, int amount)
        {
            CurencyType = curencyType;
            Amount = amount;
        }
    }
}
