namespace Rasa.Packets.Mission.Server
{
    using Data;
    using Memory;

    public class MissionClearedPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.MissionCleared;
        public uint MissionId { get; }

        public MissionClearedPacket(uint missionId)
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
