namespace Rasa.Packets.Game.Client
{
    using Data;
    using Memory;

    public class GetCustomizationChoicesPacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.GetCustomizationChoices;

        public long EntityId { get; set; }

        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
            EntityId = pr.ReadLong();
        }
    }
}
