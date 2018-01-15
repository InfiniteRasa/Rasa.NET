namespace Rasa.Structures
{
    using Data;
    using Memory;

    public class AppearanceData : IPythonDataStruct
    {
        public EquipmentSlots SlotId { get; set; }
        public int ClassId { get; set; }
        public Color Color { get; set; }

        public void Read(PythonReader pr)
        {
            SlotId = (EquipmentSlots)pr.ReadInt();

            pr.ReadTuple();

            ClassId = pr.ReadInt();
            Color = pr.ReadStruct<Color>();
        }

        public void Write(PythonWriter pw)
        {
            pw.WriteInt((int)SlotId);

            pw.WriteTuple(2);
            pw.WriteInt(ClassId);
            pw.WriteStruct(Color);
        }
    }
}
