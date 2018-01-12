namespace Rasa.Packets.MapChannel.Client
{
    using Data;
    using Memory;

    public class OpenAuctionHousePacket : PythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.OpenAuctionHouse;
        
        public override void Read(PythonReader pr)
        {
        }

        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(0);
        }
    }
}