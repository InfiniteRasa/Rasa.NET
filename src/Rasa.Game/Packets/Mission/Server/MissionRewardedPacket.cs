namespace Rasa.Packets.Mission.Server
{
    using Data;
    using Memory;

    public class MissionRewardedPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.MissionRewarded;
        public uint MissionId { get; }

        public MissionRewardedPacket(uint missionId)
        {
            MissionId = missionId;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteUInt(MissionId);
        }
    }
}
