namespace Rasa.Structures
{
    using Memory;

    public class ClanData : IPythonDataStruct
    {
        public uint Id { get; set; }
        public string Name { get; set; }

        public ClanData()
        {
        }

        public ClanData(ClanEntry entry)
        {
            Id = entry.Id;
            Name = entry.Name;
        }

        public void Read(PythonReader pr)
        {
            pr.ReadTuple();
            Id = pr.ReadUInt();
            Name = pr.ReadUnicodeString();
        }

        public void Write(PythonWriter pw)
        {
            pw.WriteTuple(2);
            pw.WriteUInt(Id);
            pw.WriteUnicodeString(Name);
        }
    }
}
