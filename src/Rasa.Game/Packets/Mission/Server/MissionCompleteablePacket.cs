namespace Rasa.Packets.Mission.Server
{
    using Data;
    using Memory;

    public class MissionCompleteablePacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.MissionCompleteable;

        public int MissionId { get; set; }
        public bool IsCompleteable { get; set; }

        public MissionCompleteablePacket(int missionId, bool isCompleteable)
        {
            MissionId = missionId;
            IsCompleteable = isCompleteable;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(2);
            pw.WriteInt(MissionId);
            pw.WriteBool(IsCompleteable);
        }
    }
}
