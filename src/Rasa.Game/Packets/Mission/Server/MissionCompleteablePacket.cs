namespace Rasa.Packets.Mission.Server
{
    using Data;
    using Memory;
    using Structures;

    public class MissionCompleteablePacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.MissionCompleteable;

        public uint MissionId { get; }
        public bool IsCompleteable { get; }

        public MissionCompleteablePacket(uint missionId, bool isCompleteable)
        {
            MissionId = missionId;
            IsCompleteable = isCompleteable;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(2);
            pw.WriteUInt(MissionId);
            MissionWire.WriteBool(pw, IsCompleteable);
        }
    }
}
