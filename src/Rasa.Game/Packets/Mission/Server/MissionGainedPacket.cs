namespace Rasa.Packets.Mission.Server
{
    using Data;
    using Memory;
    using Structures;

    public class MissionGainedPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.MissionGained;

        public uint MissionId { get; set; }
        public MissionInfo MissionInfo { get; set; }

        public MissionGainedPacket(uint missionId, MissionInfo missionInfo)
        {
            MissionId = missionId;
            MissionInfo = missionInfo;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(2);
            pw.WriteUInt(MissionId);         // missionId
            pw.WriteStruct(MissionInfo);    // MissionInfo
        }
    }
}
