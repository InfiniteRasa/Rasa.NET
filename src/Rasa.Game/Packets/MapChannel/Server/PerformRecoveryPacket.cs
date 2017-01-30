namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    public class PerformRecoveryPacket : PythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.PerformRecovery;

        public int ActionId { get; set; }
        public int ActionArgId { get; set; }
        public int AmmoCount { get; set; }

        public override void Read(PythonReader pr)
        {
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(3);
            pw.WriteInt(ActionId);
            pw.WriteInt(ActionArgId);
            pw.WriteInt(AmmoCount);
        }
    }
}
