namespace Rasa.Packets.MapChannel.Client
{
    using Data;
    using Memory;

    public class PurchaseLockboxTabPacket : PythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.PurchaseLockboxTab;

        public uint TabId { get; set; }

        public override void Read(PythonReader pr)
        {
            Logger.WriteLog(LogType.Debug, $"PurchaseLockboxTab:\n {pr.ToString()}");
            pr.ReadTuple();
            TabId = pr.ReadUInt();
        }

        public override void Write(PythonWriter pw)
        {
        }
    }
}
