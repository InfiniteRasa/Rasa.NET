namespace Rasa.Structures
{
    using Memory;
	
    public class XPInfo : IPythonDataStruct
    {
        public uint Total = 0;
        public uint Gained = 0;
        public uint BaseGained = 0;
        public int GroupMod = 1;
        public int StreakMod = 1;
        public int BoosterMod = 1;
        public bool WasCritKill = false;
        public bool WasTeamCritKill = false;

        public XPInfo()
        {
        }

        public XPInfo(uint total, uint gained, uint baseGained)
        {
            Total = total;
            Gained = gained;
            BaseGained = baseGained;
        }

        public void Read(PythonReader pr)
        {
        }

        public void Write(PythonWriter pw)
        {
            pw.WriteTuple(8);
            pw.WriteUInt(Total);
            pw.WriteUInt(Gained);
            pw.WriteUInt(BaseGained);
            pw.WriteInt(GroupMod);
            pw.WriteInt(StreakMod);
            pw.WriteInt(BoosterMod);
            pw.WriteBool(WasCritKill);
            pw.WriteBool(WasTeamCritKill);
        }
    }
}
