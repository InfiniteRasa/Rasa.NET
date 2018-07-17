namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    public class TitleChangedPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.TitleChanged;

        public uint TitleId { get; set; }

        public TitleChangedPacket(uint titleId)
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
