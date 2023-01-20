using System.Collections.Generic;

namespace Rasa.Structures
{
    using Char;
    using Memory;

    public class ClanData : IPythonDataStruct
    {
        public uint Id { get; set; }
        public string Name { get; set; }
        public Dictionary<uint, string> RankTitles { get; set; } = new Dictionary<uint, string>();
        public bool IsPvP { get; set; }

        public ClanData()
        {
        }

        public ClanData(ClanEntry entry)
        {
            Id = entry.Id;
            Name = entry.Name;
            IsPvP = entry.IsPvP;
            RankTitles.Add(0, entry.RankTitle0);
            RankTitles.Add(1, entry.RankTitle1);
            RankTitles.Add(2, entry.RankTitle2);
            RankTitles.Add(3, entry.RankTitle3);
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
        }
    }
}
