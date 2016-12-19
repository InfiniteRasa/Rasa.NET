using System;

namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;
    public class SetSkyTimePacket : PythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.SetSkyTime;

        public int RunningTime { get; set; }    // number of seconds the map has been up

        public override void Read(PythonReader pr)
        {
            Console.WriteLine("SetSkyTime Read\n{0}", pr.ToString());   // ToDo just for testing, remove later
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteInt(RunningTime);
            Console.WriteLine("SetSkyTime Write\n{0}", pw.ToString());   // just for testing, remove later
        }
    }
}
