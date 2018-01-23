namespace Rasa.Packets.MapChannel.Client
{
    using Data;
    using Memory;

    public class GetCustomizationChoicesPacket : PythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.GetCustomizationChoices;

        public long EntityId { get; set; }

        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
            EntityId = pr.ReadLong();
        }

        public override void Write(PythonWriter pw)
        {
        }
    }
}
