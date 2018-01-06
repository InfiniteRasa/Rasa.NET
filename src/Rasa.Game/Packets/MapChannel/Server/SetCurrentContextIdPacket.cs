namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    public class SetCurrentContextIdPacket : PythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.SetCurrentContextId;

        public int MapContextId { get; set; }

        public override void Read(PythonReader pr)
        {
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteInt(MapContextId);
        }
    }
}
