namespace Rasa.Packets.Mission.Server
{
    using Data;
    using Memory;

    public class MissionDiscardedPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.MissionDiscarded;
        public uint MissionId { get; }

        public MissionDiscardedPacket(uint missionId)
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
