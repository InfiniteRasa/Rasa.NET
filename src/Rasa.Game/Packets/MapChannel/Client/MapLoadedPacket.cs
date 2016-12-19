using System;

namespace Rasa.Packets.MapChannel.Client
{
    using Data;
    using Memory;

    public class MapLoadedPacket : PythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.MapLoaded;
        
        public override void Read(PythonReader pr)
        {
            Console.WriteLine("MapLoaded Read\n{0}", pr.ToString());   // ToDo just for testing, remove later
        }

        public override void Write(PythonWriter pw)
        {
        }
    }
}