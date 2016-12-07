using System;

namespace Rasa.Packets.Game.Client
{
    using Data;
    using Memory;
    public class StoreUserClientInformationPacket : PythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.StoreUserClientInformation;
        
        public override void Read(PythonReader pr)
        {
            Console.WriteLine("StoreUserClientInformation Read\n{0}", pr.ToString());   // ToDo just for testing, remove later
        }

        public override void Write(PythonWriter pw)
        {
            Console.WriteLine("StoreUserClientInformation Write\n{0}", pw.ToString());   // just for testing, remove later
        }
    }
}