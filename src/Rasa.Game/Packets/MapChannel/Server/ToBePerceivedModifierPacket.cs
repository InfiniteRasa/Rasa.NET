namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    public class ToBePerceivedModifierPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.ToBePerceivedModifier;

        /// <summary>A multiplier, 1.0 for none: the client's radar works the range out from it (radarwindow.py).</summary>
        public double Mod { get; set; }

        public ToBePerceivedModifierPacket(double mod)
        {
            Mod = mod;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteDouble(Mod);
        }
    }
}
