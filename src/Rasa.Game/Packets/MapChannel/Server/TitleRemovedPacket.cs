namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    public class TitleRemovedPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.TitleRemoved;

        public uint TitleId { get; set; }

        public TitleRemovedPacket(uint titleId)
        {
            TitleId = titleId;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteUInt(TitleId);
        }
    }
}
