namespace Rasa.Packets.MapChannel.Client
{
    using Data;
    using Memory;

    public class ChangeShowHelmetPacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.ChangeShowHelmet;

        public bool ShowHelmet { get; set; }

        public override void Read(PythonReader pr)
        {
        }
    }
}
