namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    public class PerformRecoveryPacket : PythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.PerformRecovery;

        public int ActionId { get; set; }
        public int ActionArgId { get; set; }
        public int Args { get; set; }

        public override void Read(PythonReader pr)
        {
        }

        public override void Write(PythonWriter pw)
        {
            if (Args != 0)
            {
                pw.WriteTuple(3);
                pw.WriteInt(ActionId);
                pw.WriteInt(ActionArgId);
                pw.WriteInt(Args);
            }
            else
            {
                pw.WriteTuple(2);
                pw.WriteInt(ActionId);
                pw.WriteInt(ActionArgId);
            }
        }
    }
}
