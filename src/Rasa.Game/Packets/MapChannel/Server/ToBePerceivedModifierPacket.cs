namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    public class ToBePerceivedModifierPacket : PythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.ToBePerceivedModifier;

        public int Mod { get; set; }

        public ToBePerceivedModifierPacket(int mod)
        {
            Mod = mod;
        }

        public override void Read(PythonReader pr)
        {
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteInt(Mod);
        }
    }
}
