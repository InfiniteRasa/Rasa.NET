namespace Rasa.Packets.Mission.Server
{
    using Data;
    using Memory;

    public class UpdateObjectiveCounterPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.UpdateObjectiveCounter;

        public uint MissionId { get; }
        public uint ObjectiveId { get; }
        public uint CounterId { get; }
        public uint CounterValue { get; }
        public uint InitialValue { get; }
        public uint TargetValue { get; }

        public UpdateObjectiveCounterPacket(
            uint missionId,
            uint objectiveId,
            uint counterId,
            uint counterValue,
            uint initialValue,
            uint targetValue)
        {
            MissionId = missionId;
            ObjectiveId = objectiveId;
            CounterId = counterId;
            CounterValue = counterValue;
            InitialValue = initialValue;
            TargetValue = targetValue;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(6);
            pw.WriteUInt(MissionId);
            pw.WriteUInt(ObjectiveId);
            pw.WriteUInt(CounterId);
            pw.WriteUInt(CounterValue);
            pw.WriteUInt(InitialValue);
            pw.WriteUInt(TargetValue);
        }
    }
}
