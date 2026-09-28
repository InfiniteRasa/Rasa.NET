namespace Rasa.Packets.Mission.Server
{
    using Data;
    using Memory;
    using Structures;

    public class ObjectiveRevealedPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.ObjectiveRevealed;

        public uint MissionId { get; }
        public uint ObjectiveId { get; }
        public MissionInfo MissionInfo { get; }

        public ObjectiveRevealedPacket(uint missionId, uint objectiveId, MissionInfo missionInfo)
        {
            MissionId = missionId;
            ObjectiveId = objectiveId;
            MissionInfo = missionInfo;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(3);
            pw.WriteUInt(MissionId);
            pw.WriteUInt(ObjectiveId);
            pw.WriteStruct(MissionInfo);
        }
    }
}
