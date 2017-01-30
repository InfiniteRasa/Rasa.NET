using System;

namespace Rasa.Packets.MapChannel.Client
{
    using Data;
    using Memory;

    public class RequestWeaponDrawPacket : PythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.RequestWeaponDraw;

        public int KeepAliveDelay { get; set; }

        public override void Read(PythonReader pr)
        {
            Console.WriteLine("RequestWeaponDraw\n{0}", pr.ToString());
        }

        public override void Write(PythonWriter pw)
        {
        }
    }
}
