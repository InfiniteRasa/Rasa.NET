using System.Collections.Generic;

namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;
    using Structures;

    public class QuerySuccessPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.QuerySuccess;

        public List<AuctionItem> AuctionItemList = new List<AuctionItem>();

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteList(AuctionItemList.Count);
            foreach (var auctionItem in AuctionItemList)
                pw.WriteStruct(auctionItem);
        }
    }
}
