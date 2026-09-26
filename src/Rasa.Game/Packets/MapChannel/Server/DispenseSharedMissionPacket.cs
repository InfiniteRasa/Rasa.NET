namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;
    using Structures;

    public sealed class DispenseSharedMissionPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.DispenseSharedMission;
        public ulong SourceActorId { get; }
        public uint MissionId { get; }
        public MissionInfo MissionInfo { get; }

        public DispenseSharedMissionPacket(ulong sourceActorId, uint missionId, MissionInfo missionInfo)
        {
            SourceActorId = sourceActorId;
            MissionId = missionId;
            MissionInfo = missionInfo;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(3);
            pw.WriteULong(SourceActorId);
            pw.WriteUInt(MissionId);
            MissionInfo.WriteOffer(pw);
        }
    }
}
