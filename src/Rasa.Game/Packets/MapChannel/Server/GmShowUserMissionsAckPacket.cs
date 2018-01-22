namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;
    using Structures;

    public class GmShowUserMissionsAckPacket : PythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.GmShowUserMissionsAck;

        public uint UserId { get; set; }
        public string DisplayName { get; set; }
        public MissionInfo MissionInfo { get; set; }

        public GmShowUserMissionsAckPacket(uint userId, string displayName, MissionInfo missionInfo)
        {
            UserId = userId;
            DisplayName = displayName;
            MissionInfo = missionInfo;
        }

        public override void Read(PythonReader pr)
        {
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(3);
            pw.WriteUInt(UserId);
            pw.WriteString(DisplayName);
            pw.WriteStruct(MissionInfo);
        }
    }
}
