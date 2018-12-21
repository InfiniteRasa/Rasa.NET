namespace Rasa.Packets.MapChannel.Client
{
    using Data;
    using Memory;

    public class CompleteNPCMissionPacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.CompleteNPCMission;
        
        public uint EntityId { get; set; }          // npcId
        public uint MissionId { get; set; }         // missionId
        public bool SelectionIdx { get; set; }      // selectionIdx
        public bool Rating { get; set; }            // rating

        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
            EntityId = (uint)pr.ReadLong();
            MissionId = pr.ReadUInt();
            SelectionIdx = pr.ReadBool();
            Rating = pr.ReadBool();
        }
    }
}
