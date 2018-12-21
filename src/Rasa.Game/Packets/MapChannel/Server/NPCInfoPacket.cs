namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    public class NPCInfoPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.NPCInfo;

        public uint NpcPackageId { get; set; }

        public NPCInfoPacket(uint npcPackageId)
        {
            NpcPackageId = npcPackageId;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteUInt(NpcPackageId);
        }
    }
}
