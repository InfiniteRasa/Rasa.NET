namespace Rasa.Packets.MapChannel.Client
{
    using Data;
    using Memory;

    public class StoreUserClientInformation : PythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.StoreUserClientInformation;

        

        public override void Read(PythonReader pr)
        {
            Logger.WriteLog(LogType.Debug, $"StoreUserClientInformation:\n{pr.ToString()}");
        }

        public override void Write(PythonWriter pw)
        {
        }
    }
}