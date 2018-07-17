namespace Rasa.Packets.MapChannel.Client
{
    using Data;
    using Memory;

    public class AssignNPCMissionPacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.AssignNPCMission;

        public long NpcEntityId { get; set; }
        public int MissionId { get; set; }

        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
            NpcEntityId = pr.ReadLong();
            MissionId = pr.ReadInt();
        }
    }
}
