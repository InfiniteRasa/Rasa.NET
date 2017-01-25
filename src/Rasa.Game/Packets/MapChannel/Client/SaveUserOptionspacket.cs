using System;

namespace Rasa.Packets.MapChannel.Client
{
    using Data;
    using Memory;

    public class SaveUserOptionsPacket :PythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.SaveUserOptions;

        public double Ping { get; set; }

        public override void Read(PythonReader pr)
        {
            Console.WriteLine("SaveUserOptions\n{0}", pr.ToString());
        }

        public override void Write(PythonWriter pw)
        {
        }
    }
}
