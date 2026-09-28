namespace Rasa.Packets.Mission.Server
{
    using Data;
    using Memory;

    public class MissionFailedPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.MissionFailed;
        public uint MissionId { get; }

        public MissionFailedPacket(uint missionId)
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
