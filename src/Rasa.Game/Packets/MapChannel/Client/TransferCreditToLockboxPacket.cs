namespace Rasa.Packets.MapChannel.Client
{
    using Data;
    using Memory;

    public class TransferCreditToLockboxPacket : PythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.TransferCreditToLockbox;

        public int Ammount { get; set; }

        public override void Read(PythonReader pr)
        {
            Logger.WriteLog(LogType.Debug, $"TransferCreditToLockbox: \n {pr.ToString()}");
            pr.ReadTuple();
            Ammount = pr.ReadInt();
        }

        public override void Write(PythonWriter pw)
        {
        }
    }
}
