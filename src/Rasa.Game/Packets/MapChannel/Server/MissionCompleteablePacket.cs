namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;
    using Structures;

    public class MissionCompleteablePacket : PythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.MissionCompleteable;

        public int MissionId { get; set; }
        public MissionInfo MissionInfo { get; set; }

        public MissionCompleteablePacket(int missionId, MissionInfo missionInfo)
        {
            MissionId = missionId;
            MissionInfo = missionInfo;
        }

        public override void Read(PythonReader pr)
        {
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(2);
            pw.WriteInt(MissionId);
            pw.WriteStruct(MissionInfo);
        }
    }
}
