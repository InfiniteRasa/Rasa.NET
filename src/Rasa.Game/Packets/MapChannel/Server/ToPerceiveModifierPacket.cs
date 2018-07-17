namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    public class ToPerceiveModifierPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.ToPerceiveModifier;

        public int Mod { get; set; }

        public ToPerceiveModifierPacket(int mod)
        {
            Mod = mod;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteInt(Mod);
        }
    }
}
