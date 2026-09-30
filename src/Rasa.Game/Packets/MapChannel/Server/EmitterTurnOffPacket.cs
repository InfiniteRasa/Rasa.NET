namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    /// <summary>Emitter.Recv_TurnOff(): the emitter stops playing; it stays where it is, ready to be turned on.</summary>
    public class EmitterTurnOffPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.TurnOff;

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(0);
        }
    }
}
