namespace Rasa.Packets.MapChannel.Client
{
    using Data;
    using Memory;

    public class AssignNPCMissionPacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.AssignNPCMission;

        public ulong NpcEntityId { get; set; }
        public uint MissionId { get; set; }

        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
            NpcEntityId = pr.ReadULong();
            MissionId = (uint)pr.ReadInt();
        }
    }
}
