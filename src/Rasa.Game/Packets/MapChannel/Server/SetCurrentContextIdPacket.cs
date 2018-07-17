namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    public class SetCurrentContextIdPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.SetCurrentContextId;

        public uint MapContextId { get; set; }

        public SetCurrentContextIdPacket(uint mapContextId)
        {
            MapContextId = mapContextId;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteUInt(MapContextId);
        }
    }
}
