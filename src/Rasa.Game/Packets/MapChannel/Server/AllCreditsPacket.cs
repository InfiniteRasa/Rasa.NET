using System.Collections.Generic;

namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;
    using Structures;

    public class AllCreditsPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.AllCredits;

        public Dictionary<CurencyType, uint> Credits { get; set; }
        public uint Prestige { get; set; }

        public AllCreditsPacket(Dictionary<CurencyType, uint> creadits)
        {
            Credits = creadits;
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteList(Credits.Count);
            foreach (var curency in Credits)
            {
                pw.WriteTuple(2);
                pw.WriteInt((int)curency.Key);
                pw.WriteUInt(curency.Value);
            }
        }
    }
}
