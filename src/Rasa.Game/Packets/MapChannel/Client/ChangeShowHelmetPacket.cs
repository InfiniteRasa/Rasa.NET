using System;

namespace Rasa.Packets.MapChannel.Client
{
    using Data;
    using Memory;

    public class ChangeShowHelmetPacket : PythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.ChangeShowHelmet;

        public override void Read(PythonReader pr)
        {
            Console.WriteLine("ChangeShowHelmet\n{0}", pr.ToString());
        }
        public override void Write(PythonWriter pw)
        {
        }
    }
}
