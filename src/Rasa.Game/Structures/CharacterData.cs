namespace Rasa.Structures
{
    using Data;
    using Memory;

    public class CharacterData : IPythonDataStruct
    {
        public string Name { get; set; }
        public uint ContextId { get; set; }
        public uint ExpPoints { get; set; }
        public byte ExpLevel { get; set; }
        public uint Body { get; set; }
        public uint Mind { get; set; }
        public uint Spirit { get; set; }
        public uint Class { get; set; }
        public uint CloneCredits { get; set; }
        public Race RaceId { get; set; }

        public CharacterData(CharacterEntry entry)
        {
            Name = entry.Name;
            ContextId = entry.ContextId;
            ExpPoints = entry.Experience;
            ExpLevel = entry.Level;
            Body = entry.Body;
            Mind = entry.Mind;
            Spirit = entry.Spirit;
            Class = entry.Class;
            CloneCredits = entry.CloneCredits;
            RaceId = (Race) entry.Race;
        }

        public void Read(PythonReader pr)
        {
            pr.ReadTuple();
            Name = pr.ReadUnicodeString();
            ContextId = pr.ReadUInt();
            ExpPoints = pr.ReadUInt();
            ExpLevel = (byte) pr.ReadInt();
            Body = pr.ReadUInt();
            Mind = pr.ReadUInt();
            Spirit = pr.ReadUInt();
            Class = pr.ReadUInt();
            CloneCredits = pr.ReadUInt();
            RaceId = (Race) pr.ReadInt();
        }

        public void Write(PythonWriter pw)
        {
            pw.WriteTuple(10);
            pw.WriteUnicodeString(Name);
            pw.WriteUInt(ContextId);
            pw.WriteUInt(ExpPoints);
            pw.WriteInt(ExpLevel);
            pw.WriteUInt(Body);
            pw.WriteUInt(Mind);
            pw.WriteUInt(Spirit);
            pw.WriteUInt(Class);
            pw.WriteUInt(CloneCredits);
            pw.WriteInt((int) RaceId);
        }
    }
}
