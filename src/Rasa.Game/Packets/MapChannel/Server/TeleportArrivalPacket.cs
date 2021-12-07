namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    public class TeleportArrivalPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.TeleportArrival;

        public uint DelayMs { get; set; }
        public uint DelayEffectMs { get; set; }
        public uint DoFade { get; set; }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(3);
            pw.WriteNoneStruct();
            pw.WriteNoneStruct();
            pw.WriteNoneStruct();
        }
    }
}
