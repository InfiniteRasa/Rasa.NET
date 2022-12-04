using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Options;
using Rasa.Data;
using Rasa.Memory;

namespace Rasa.Structures
{
    public class XPInfo : IPythonDataStruct
    {
        public int Total = 0;
        public int Gained = 0;
        public int BaseGained = 0;
        public int GroupMod = 1;
        public int StreakMod = 1;
        public int BoosterMod = 1;
        public bool WasCritKill = false;
        public bool WasTeamCritKill = false;

        public XPInfo()
        {
        }

        public XPInfo(int total, int gained, int baseGained)
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
            pw.WriteInt(Total);
            pw.WriteInt(Gained);
            pw.WriteInt(BaseGained);
            pw.WriteInt(GroupMod);
            pw.WriteInt(StreakMod);
            pw.WriteInt(BoosterMod);
            pw.WriteBool(WasCritKill);
            pw.WriteBool(WasTeamCritKill);
        }
    }
}
