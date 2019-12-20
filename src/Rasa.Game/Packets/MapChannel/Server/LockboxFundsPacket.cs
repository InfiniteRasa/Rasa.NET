namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    public class LockboxFundsPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.LockboxFunds;

        public int Amount { get; set; }

        public LockboxFundsPacket(int amount)
        {
            Amount = amount;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteInt(Amount);
        }
    }
}
