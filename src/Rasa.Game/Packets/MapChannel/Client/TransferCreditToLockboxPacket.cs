namespace Rasa.Packets.MapChannel.Client
{
    using Data;
    using Memory;

    public class TransferCreditToLockboxPacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.TransferCreditToLockbox;

        public int Ammount { get; set; }

        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
            Ammount = pr.ReadInt();
        }
    }
}
