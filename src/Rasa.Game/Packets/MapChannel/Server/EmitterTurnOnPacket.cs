namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    /// <summary>Emitter.Recv_TurnOn(): the emitter starts playing what EmitterInfo gave it.</summary>
    public class EmitterTurnOnPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.TurnOn;

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(0);
        }
    }
}
