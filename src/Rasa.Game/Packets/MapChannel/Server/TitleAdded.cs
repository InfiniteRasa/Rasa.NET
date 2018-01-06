namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;
    //ToDo: This packet send notify to character when gets a new Title (Need System Title work, exploring zones, kill xxx mobs)
    public class TitleAddedPacket : PythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.TitleAdded;

        public int Titles { get; set; }

        public TitleAddedPacket(int titleId)
        {
            Titles = titleId;
        }

        public override void Read(PythonReader pr)
        {
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteInt(Titles);
        }
    }
}