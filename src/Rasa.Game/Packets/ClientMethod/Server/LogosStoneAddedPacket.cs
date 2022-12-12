namespace Rasa.Packets.ClientMethod.Server
{
    using Data;
    using Memory;

    public class LogosStoneAddedPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.LogosStoneAdded;

        public uint LogosId { get; set; }

        public LogosStoneAddedPacket(uint logosId)
        {
            LogosId = logosId;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteUInt(LogosId);
        }
    }
}
