namespace Rasa.Packets.MapChannel.Client
{
    using Data;
    using Memory;

    public class OpenAuctionHousePacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.OpenAuctionHouse;

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(0);
        }
    }
}