namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    public class NPCInfoPacket : PythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.NPCInfo;

        public int NpcPackageId { get; set; }

        public NPCInfoPacket(int npcPackageId)
        {
            NpcPackageId = npcPackageId;
        }

        public override void Read(PythonReader pr)
        {
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteInt(NpcPackageId);
        }
    }
}
