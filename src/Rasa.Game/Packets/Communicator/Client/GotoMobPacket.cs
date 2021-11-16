using System.Collections.Generic;

namespace Rasa.Packets.Communicator.Client
{
    using Data;
    using Memory;

    public class GotoMobPacket : ClientPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.GotoMob;

        public string ArgString { get; set; }
        public uint ExactMobNameId { get; set; }
        public List<uint> PartialMobNameIds = new List<uint>();
        
        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
            ArgString = pr.ReadUnicodeString();
            ExactMobNameId = pr.ReadUInt();

            var listLen = pr.ReadList();
            for (var i = 0; i < listLen; i++)
            {
                pr.ReadTuple();
                PartialMobNameIds.Add(pr.ReadUInt());
            }

        }
    }
}
