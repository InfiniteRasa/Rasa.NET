namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    public class TitleRemovedPacket : PythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.TitleRemoved;

        public override void Read(PythonReader pr)
        {
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteNoneStruct();
        }
    }
}
