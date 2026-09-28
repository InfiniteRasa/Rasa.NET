namespace Rasa.Packets.Mission.Server
{
    using Data;
    using Memory;

    public class UpdateObjectiveItemCounterPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.UpdateObjectiveItemCounter;

        public uint MissionId { get; }
        public uint ObjectiveId { get; }
        public uint ItemClassId { get; }
        public uint CounterValue { get; }
        public uint TargetValue { get; }

        public UpdateObjectiveItemCounterPacket(
            uint missionId,
            uint objectiveId,
            uint itemClassId,
            uint counterValue,
            uint targetValue)
        {
            MissionId = missionId;
            ObjectiveId = objectiveId;
            ItemClassId = itemClassId;
            CounterValue = counterValue;
            TargetValue = targetValue;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(5);
            pw.WriteUInt(MissionId);
            pw.WriteUInt(ObjectiveId);
            pw.WriteUInt(ItemClassId);
            pw.WriteUInt(CounterValue);
            pw.WriteUInt(TargetValue);
        }
    }
}
