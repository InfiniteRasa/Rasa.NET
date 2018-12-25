namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    public class QueryFailedPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.QueryFailed;

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(1);
            pw.WriteUInt(1043);     // generated/playerMessage.py => PmAuctionNoResultsFound    = 1064;
                                    //                               PmAuctionItemNotFound      = 1043;
        }
    }
}
