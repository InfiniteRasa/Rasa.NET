namespace Rasa.Structures
{
    using Memory;

    public class BodyData : IPythonDataStruct
    {
        public int GenderClassId { get; set; }
        public double Scale { get; set; }

        public BodyData(CharacterEntry entry)
        {
            GenderClassId = entry.Gender == 1 ? 691 : 692;
            Scale = entry.Scale;
        }

        public void Read(PythonReader pr)
        {
            pr.ReadTuple();
            GenderClassId = pr.ReadInt();
            Scale = pr.ReadDouble();
        }

        public void Write(PythonWriter pw)
        {
            pw.WriteTuple(2);
            pw.WriteInt(GenderClassId);
            pw.WriteDouble(Scale);
        }
    }
}
