namespace Rasa.Packets.Mission.Server
{
    using Data;
    using Memory;

    public class MissionCompletedPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.MissionCompleted;
        public uint MissionId { get; }

        public MissionCompletedPacket(uint missionId)
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
