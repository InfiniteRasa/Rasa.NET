using System;

namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;
    public class SetControlledActorIdPacket : PythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.SetControlledActorId;

        public uint EntetyId { get; set; }
        public override void Read(PythonReader pr)
        {
            Console.WriteLine("SetControlledActorId Read\n{0}", pr.ToString());   // ToDo just for testing, remove later
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteInt((int)EntetyId);
            Console.WriteLine("SetControlledActorId Write\n{0}", pw.ToString());   // just for testing, remove later
        }
    }
}
