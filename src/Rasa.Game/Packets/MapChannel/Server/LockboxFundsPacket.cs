namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    public class LockboxFundsPacket : PythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.LockboxFunds;

        public uint Amount { get; set; }

        public LockboxFundsPacket(long amount)
        {
            Amount = (uint)amount;
        }

        public override void Read(PythonReader pr)
        {
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteUInt(Amount);
        }
    }
}
