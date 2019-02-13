using System.Collections.Generic;

namespace Rasa.Structures
{
    using Memory;

    public class ClanData : IPythonDataStruct
    {
        public uint Id { get; set; }
        public string Name { get; set; }
        public List<string> RankTitles { get; set; } = new List<string>(); // TODO: Verify the type of this data is.
        public bool IsPvP { get; set; }

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

            var listLength = pr.ReadList();
            for (var i = 0; i < listLength; i++)
            {
                RankTitles.Add(pr.ReadString());
            }

            IsPvP = pr.ReadBool();
        }

        public void Write(PythonWriter pw)
        {
            pw.WriteTuple(4);
            pw.WriteUInt(Id);
            pw.WriteUnicodeString(Name);
            pw.WriteList(RankTitles.Count);

            foreach(string title in RankTitles)
            {
                pw.WriteString(title);
            }

            pw.WriteBool(IsPvP);
        }
    }
}
