namespace Rasa.Structures
{
    using Data;
    using Memory;

    public class Curency : IPythonDataStruct
    {
        public CurencyType CurencyType { get; set; }
        public int Amount { get; set; }

        public Curency(CurencyType curencyType, int amount)
        {
            CurencyType = curencyType;
            Amount = amount;
        }

        public void Read(PythonReader pr)
        {
        }

        public void Write(PythonWriter pw)
        {
            pw.WriteTuple(2);
            pw.WriteInt((int)CurencyType);   // credit type
            pw.WriteInt(Amount);             // credit ammount
        }
    }
}
