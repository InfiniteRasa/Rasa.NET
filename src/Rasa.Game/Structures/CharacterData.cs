namespace Rasa.Structures
{
    using Data;
    using Memory;

    public class CharacterData : IPythonDataStruct
    {
        public string Name { get; set; }
        public uint MapContextId { get; set; }
        public int ExpPoints { get; set; }
        public byte ExpLevel { get; set; }
        public int Body { get; set; }
        public int Mind { get; set; }
        public int Spirit { get; set; }
        public uint Class { get; set; }
        public uint CloneCredits { get; set; }
        public Race RaceId { get; set; }

        public CharacterData(CharacterEntry entry)
        {
            Name = entry.Name;
            MapContextId = entry.MapContextId;
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
            MapContextId = pr.ReadUInt();
            ExpPoints = pr.ReadInt();
            ExpLevel = (byte) pr.ReadInt();
            Body = pr.ReadInt();
            Mind = pr.ReadInt();
            Spirit = pr.ReadInt();
            Class = pr.ReadUInt();
            CloneCredits = pr.ReadUInt();
            RaceId = (Race) pr.ReadInt();
        }

        public void Write(PythonWriter pw)
        {
            pw.WriteTuple(10);
            pw.WriteUnicodeString(Name);
            pw.WriteUInt(MapContextId);
            pw.WriteInt(ExpPoints);
            pw.WriteInt(ExpLevel);
            pw.WriteInt(Body);
            pw.WriteInt(Mind);
            pw.WriteInt(Spirit);
            pw.WriteUInt(Class);
            pw.WriteUInt(CloneCredits);
            pw.WriteInt((int) RaceId);
        }
    }
}
