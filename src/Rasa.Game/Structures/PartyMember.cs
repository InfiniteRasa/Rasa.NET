namespace Rasa.Structures
{
    using Game;
    using Memory;
    public class PartyMember : IPythonDataStruct
    {
        public ulong MemberId { get; set; }
        public string MemberName { get; set; }
        public uint MemberClassId { get; set; }
        public uint MemberLevel { get; set; }
        public bool IsAfk { get; set; }

        public PartyMember(Client client)
        {
            MemberId = client.Player.EntityId;
            MemberName = client.Player.FamilyName;
            MemberClassId = client.Player.Class;
            MemberLevel = client.Player.Level;
            IsAfk = false;
        }
        public PartyMember(ulong memberId, string memberName, uint memberClassId, uint memberLevel, bool isAfk)
        {
            MemberId = memberId;
            MemberName = memberName;
            MemberClassId = memberClassId;
            MemberLevel = memberLevel;
            IsAfk = isAfk;
        }

        public void Read(PythonReader pr)
        {
            Logger.WriteLog(LogType.Debug, $"PartyMember: {pr.ToString()}");
        }

        public void Write(PythonWriter pw)
        {
            pw.WriteTuple(5);
            pw.WriteULong(MemberId);
            pw.WriteUnicodeString(MemberName);
            pw.WriteUInt(MemberClassId);
            pw.WriteUInt(MemberLevel);
            pw.WriteBool(IsAfk);
        }
    }
}
