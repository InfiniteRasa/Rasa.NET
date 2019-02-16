using System;
using System.Collections.Generic;
using System.Text;
using Rasa.Data;
using Rasa.Memory;

namespace Rasa.Packets.MapChannel.Client
{
    public class ClanChangeRankTitlePacket : ClientPythonPacket
    {
        public override GameOpcode Opcode => GameOpcode.ClanChangeRankTitle;

        public uint Rank { get; set; }
        public string Title { get; set; }

        public override void Read(PythonReader pr)
        {
            pr.ReadTuple();
            Rank = pr.ReadUInt();
            Title = pr.ReadUnicodeString();
        }
    }
}
