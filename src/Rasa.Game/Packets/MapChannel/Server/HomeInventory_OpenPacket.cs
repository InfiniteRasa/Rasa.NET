namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;

    public class HomeInventory_OpenPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.HomeInventory_Open;
        
        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(0);
        }
    }
}
