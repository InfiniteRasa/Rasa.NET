namespace Rasa.Packets.Mission.Server
{
    using Data;
    using Memory;

    public class ObjectiveCompletedPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.ObjectiveCompleted;

        public uint MissionId { get; }
        public uint ObjectiveId { get; }

        public ObjectiveCompletedPacket(uint missionId, uint objectiveId)
        {
            MissionId = missionId;
            ObjectiveId = objectiveId;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(2);
            pw.WriteUInt(MissionId);
            pw.WriteUInt(ObjectiveId);
        }
    }
}
