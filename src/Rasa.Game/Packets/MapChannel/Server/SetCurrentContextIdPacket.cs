using System;

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
            Console.WriteLine("SetCurrentContextId Read\n{0}", pr.ToString());   // ToDo just for testing, remove later
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteInt(MapContextId);
            Console.WriteLine("SetCurrentContextId Write\n{0}", pw.ToString());   // just for testing, remove later
        }
    }
}
