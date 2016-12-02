namespace Rasa.Structures
{
    using Memory;

    public class AppearanceData : IPythonDataStruct
    {
        public int SlotId { get; set; }
        public int ClassId { get; set; }
        public Color Color { get; set; }

        public void Read(PythonReader pr)
        {
            SlotId = pr.ReadInt();

            pr.ReadTuple();

            ClassId = pr.ReadInt();
            Color = pr.ReadStruct<Color>();
        }

        public void Write(PythonWriter pw)
        {
            pw.WriteInt(SlotId);

            pw.WriteTuple(2);
            pw.WriteInt(ClassId);
            pw.WriteStruct(Color);
        }
    }
}
