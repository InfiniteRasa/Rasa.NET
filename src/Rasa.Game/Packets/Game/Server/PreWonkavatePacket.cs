namespace Rasa.Packets.Game.Server
{
    using Data;
    using Memory;

    public class PreWonkavatePacket : PythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.PreWonkavate;

        public int Something { get; set; }  // ToDo variable unknown

        public PreWonkavatePacket(int something)
        {
            Something = something;
        }

        public override void Read(PythonReader pr)
        {
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteInt(Something);
        }
    }
}
