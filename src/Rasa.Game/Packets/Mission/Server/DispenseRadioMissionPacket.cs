namespace Rasa.Packets.Mission.Server
{
    using Data;
    using Memory;
    using Structures;

    public class DispenseRadioMissionPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.DispenseRadioMission;
        public uint MissionId { get; }
        public MissionInfo MissionInfo { get; }
        public bool ForceDialog { get; }

        public DispenseRadioMissionPacket(uint missionId, MissionInfo missionInfo, bool forceDialog)
        {
            MissionId = missionId;
            MissionInfo = missionInfo;
            ForceDialog = forceDialog;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(3);
            pw.WriteUInt(MissionId);
            MissionInfo.WriteOffer(pw);
            MissionWire.WriteBool(pw, ForceDialog);
        }
    }
}
