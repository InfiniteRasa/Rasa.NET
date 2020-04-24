namespace Rasa.Packets.Game.Server
{
    using Data;
    using Memory;

    public class SetCurrentContextIdPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.SetCurrentContextId;

        public uint ContextId { get; set; }

        public SetCurrentContextIdPacket(uint contextId)
        {
            ContextId = contextId;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteUInt(ContextId);
        }
    }
}
