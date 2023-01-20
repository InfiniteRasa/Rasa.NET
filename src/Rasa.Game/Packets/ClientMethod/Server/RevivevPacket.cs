namespace Rasa.Packets.ClientMethod.Server
{
    using Data;
    using Memory;

    public class RevivedPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.Revived;

        public ulong SourceId { get; set; }

        public RevivedPacket(ulong sourceId)
        {
            SourceId = sourceId;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteULong(SourceId);
        }
    }
}
