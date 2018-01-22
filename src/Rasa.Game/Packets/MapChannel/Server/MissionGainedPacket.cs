namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;
    using Structures;

    public class MissionGainedPacket : PythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.MissionGained;

        public int MissionId { get; set; }
        public MissionInfo MissionInfo { get; set; }

        public MissionGainedPacket(int missionId, MissionInfo missionInfo)
        {
            MissionId = missionId;
            MissionInfo = missionInfo;
        }

        public override void Read(PythonReader pr)
        {
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(2);
            pw.WriteInt(MissionId);         // missionId
            pw.WriteStruct(MissionInfo);    // MissionInfo
        }
    }
}
