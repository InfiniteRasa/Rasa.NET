namespace Rasa.Packets.Communicator.Client
{
    using Data;
    using Memory;

    public class RequestLOSReportPacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.RequestLOSReport;

        public override void Read(PythonReader pr)
        {
            Logger.WriteLog(LogType.Debug, pr.ToString());
        }
    }
}
