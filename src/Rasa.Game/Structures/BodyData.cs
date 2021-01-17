namespace Rasa.Structures
{
    using Data;
    using Memory;
    using Char;

    public class BodyData : IPythonDataStruct
    {
        public EntityClass GenderClassId { get; set; }
        public double Scale { get; set; }

        public BodyData(CharacterEntry entry)
        {
            GenderClassId = entry.Gender == 1 ? EntityClass.HumanBaseFemale : EntityClass.HumanBaseMale;
            Scale = entry.Scale;
        }

        public void Read(PythonReader pr)
        {
            pr.ReadTuple();
            GenderClassId = (EntityClass)pr.ReadUInt();
            Scale = pr.ReadDouble();
        }

        public void Write(PythonWriter pw)
        {
            pw.WriteTuple(2);
            pw.WriteUInt((uint)GenderClassId);
            pw.WriteDouble(Scale);
        }
    }
}
