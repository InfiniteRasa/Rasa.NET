namespace Rasa.Structures
{
    using Data;
    using Memory;
    using Char;

    public class BodyData : IPythonDataStruct
    {
        public EntityClasses GenderClassId { get; set; }
        public double Scale { get; set; }

        public BodyData(CharacterEntry entry)
        {
            GenderClassId = entry.Gender == 1 ? EntityClasses.HumanBaseFemale : EntityClasses.HumanBaseMale;
            Scale = entry.Scale;
        }

        public void Read(PythonReader pr)
        {
            pr.ReadTuple();
            GenderClassId = (EntityClasses)pr.ReadUInt();
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
