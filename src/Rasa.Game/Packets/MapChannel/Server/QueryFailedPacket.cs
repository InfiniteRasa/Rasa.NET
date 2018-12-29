namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    public class QueryFailedPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.QueryFailed;

        public PlayerMessage PlayerMessage { get; set; }

        public QueryFailedPacket(PlayerMessage playerMessage)
        {
            PlayerMessage = playerMessage;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteUInt((uint)PlayerMessage);
        }
    }
}
