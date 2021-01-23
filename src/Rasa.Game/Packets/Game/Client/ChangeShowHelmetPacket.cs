namespace Rasa.Packets.Game.Client
{
    using Data;
    using Memory;

    public class ChangeShowHelmetPacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.ChangeShowHelmet;

        public bool ShowHelmet { get; set; }

        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
            ShowHelmet = pr.ReadBool();
        }
    }
}
