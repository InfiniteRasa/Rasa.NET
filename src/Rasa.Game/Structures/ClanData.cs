using System.Collections.Generic;

namespace Rasa.Structures
{
    using Memory;

    public class ClanData : IPythonDataStruct
    {
        public uint Id { get; set; }
        public string Name { get; set; }
        public Dictionary<uint, string> RankTitles { get; set; } = new Dictionary<uint, string>(); // TODO: Verify the type of this data is.
        public bool IsPvP { get; set; }

        public ClanData()
        {
        }

        public ClanData(ClanEntry entry)
        {
            Id = entry.Id;
            Name = entry.Name;
            IsPvP = entry.IsPvP;

            // TODO: Remove hardcoded values
            RankTitles.Add(0, "Rank 0");
            RankTitles.Add(1, "Rank 1");
            RankTitles.Add(2, "Rank 2");
            RankTitles.Add(3, "Rank 3");
        }

        public void Read(PythonReader pr)
        {
            pr.ReadTuple();
            Id = pr.ReadUInt();
            Name = pr.ReadUnicodeString();

            var rankTitleCount = pr.ReadDictionary();
            for (var i = 0; i < rankTitleCount; i++)
            {
                var rankId = pr.ReadUInt();
                var rankTitle = pr.ReadString();

                RankTitles.Add(rankId, rankTitle);
            }

            IsPvP = pr.ReadBool();
        }

        public void Write(PythonWriter pw)
        {
            pw.WriteTuple(4);
            pw.WriteUInt(Id);
            pw.WriteUnicodeString(Name);

            pw.WriteDictionary(RankTitles.Count);
            foreach (var rankTitle in RankTitles)
            {
                pw.WriteUInt(rankTitle.Key);
                pw.WriteString(rankTitle.Value);
            }

            pw.WriteBool(IsPvP);

            System.Console.WriteLine($"{pw.ToString()}");
        }
    }
}
